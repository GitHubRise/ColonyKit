using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的 DeepSeek AI：主线程收集候选动作并派发请求，后台线程只做 HTTP/JSON，
	/// 结果回到主线程校验后再下 Job。AI 只能从候选 id 里选，不能执行任意代码。
	/// </summary>
	internal static class MkVAiService
	{
		private const int MaxConcurrentRequests = 2;
		private const float CandidateRange = 40f;

		/// <summary>写进日志的想法最多保留多少字，模型偶尔会吐长句。</summary>
		private const int MaxThoughtLength = 120;

		private static readonly HttpClient Http = CreateHttpClient();
		private static readonly ConcurrentDictionary<int, AiRequestResult> Results =
			new ConcurrentDictionary<int, AiRequestResult>();
		private static readonly ConcurrentDictionary<int, bool> InFlight =
			new ConcurrentDictionary<int, bool>();
		private static readonly ConcurrentQueue<string> TestMessages = new ConcurrentQueue<string>();
		private static int inFlightCount;
		private static int warnedNoKeyPawn;
		private static bool warnedNoKeyGlobal;

		internal class AiHttpRequest
		{
			public string url;
			public string key;
			public string body;

			/// <summary>模型没回 JSON 时用的第二次请求：更严格的格式要求 + 双倍 token。</summary>
			public string retryBody;

			public float timeout;
		}

		private static HttpClient CreateHttpClient()
		{
			try
			{
				// Unity 的旧 Mono 默认不启用 TLS 1.2，DeepSeek 必须用 TLS 1.2+。
				System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
			}
			catch
			{
				// 忽略：个别平台不支持该枚举时退回系统默认。
			}
			HttpClient client = new HttpClient
			{
				Timeout = Timeout.InfiniteTimeSpan
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("ColonyKit/0.0.3");
			return client;
		}

		public static void Tick(CompNannyBot comp)
		{
			// 临时医疗睡眠点的清理：只有 Mk-V 会铺，也由它们顺带打扫。
			MkVMedicalAid.Tick();

			Pawn pawn = comp?.Pawn;
			if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null
				|| !NannyBotUtility.IsMkV(pawn))
			{
				return;
			}

			int pawnId = pawn.thingIDNumber;
			// 已加入正在组建的远行队：商队职责是最高优先任务，AI 不再抢活；
			// 在途请求的结果直接丢掉，避免它回到基地后又执行一条过期的旧动作。
			if (NannyBotUtility.IsFormingCaravan(pawn))
			{
				Results.TryRemove(pawnId, out _);
				InFlight.TryRemove(pawnId, out _);
				comp.AiLastAction = "CK.Robotics.Inspect.Ai.Caravan".Translate();
				comp.AiLastError = "";
				return;
			}

			AiRequestResult result;
			if (Results.TryRemove(pawnId, out result))
			{
				InFlight.TryRemove(pawnId, out _);
				ApplyResult(comp, pawn, result);
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			if (!settings.aiEnabled || !comp.AiControl || pawn.Drafted
				|| pawn.InMentalState || comp.paused || comp.repairing)
			{
				return;
			}
			// 正在做家务（屠宰/搬运/做饭/手工等）时先让它做完；只有出现威胁才允许 AI 打断。
			bool urgent = HasUrgentSituation(pawn);
			if (CompNannyBot.IsWorkJob(pawn.CurJobDef) && !urgent)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(settings.aiApiKey))
			{
				if (!warnedNoKeyGlobal)
				{
					warnedNoKeyGlobal = true;
					Messages.Message(
						"CK.Robotics.Ai.NoKey".Translate(),
						MessageTypeDefOf.RejectInput,
						false);
				}
				if (warnedNoKeyPawn != pawnId)
				{
					warnedNoKeyPawn = pawnId;
					comp.AiLastError = "未配置 API Key";
					ModLog.Warn("Robotics", "Mk-V AI 已启用但没有填写 DeepSeek API Key。");
				}
				return;
			}
			if (InFlight.ContainsKey(pawnId))
			{
				return;
			}
			int ticks = Find.TickManager.TicksGame;
			if (urgent && comp.AiNextTick > ticks + 300)
			{
				// 刚发现敌人：把原定的 20 秒思考提前到最多 5 秒后。
				comp.AiNextTick = ticks + 300;
			}
			if (ticks < comp.AiNextTick)
			{
				return;
			}

			float decisionInterval = settings.aiInterval;
			if (pawn.Map.dangerWatcher.DangerRating == StoryDanger.High
				|| (settings.aiRaidAdvance && MkVAlert.IsActive(pawn.Map)))
			{
				// 袭击/敌袭预警期间更频繁地重新决策，最多 5 秒一次。
				decisionInterval = Mathf.Min(decisionInterval, 5f);
			}
			comp.AiNextTick = ticks + Mathf.Max(1, Mathf.RoundToInt(decisionInterval * 60f));
			if (Interlocked.Increment(ref inFlightCount) > MaxConcurrentRequests)
			{
				Interlocked.Decrement(ref inFlightCount);
				comp.AiNextTick = ticks + 60;
				return;
			}

			AiHttpRequest request;
			try
			{
				request = BuildRequest(pawn, settings);
			}
			catch (Exception exception)
			{
				Interlocked.Decrement(ref inFlightCount);
				comp.AiLastError = exception.Message;
				ModLog.Warn("Robotics", "Mk-V AI 构建请求失败：" + exception.Message);
				return;
			}

			InFlight[pawnId] = true;
			Task.Run(async () =>
			{
				AiRequestResult reply;
				try
				{
					reply = await SendAsync(request);
				}
				catch (Exception exception)
				{
					reply = AiRequestResult.Failure(exception.Message);
				}
				Results[pawnId] = reply;
				Interlocked.Decrement(ref inFlightCount);
			});
		}

		public static void TestConnection(RoboticsSettings settings)
		{
			if (settings == null || string.IsNullOrWhiteSpace(settings.aiApiKey))
			{
				TestMessages.Enqueue("AI 测试失败：未填写 API Key");
				return;
			}
			AiHttpRequest request;
			try
			{
				AiPrompt prompt = new AiPrompt
				{
					name = "Mk-V",
					hp = 1f,
					energy = 1f,
					pos = "0,0",
					drafted = false,
					hostiles = 0,
					colonists = 1,
					downed = 0,
					fires = 0,
					candidates = new[]
					{
						new AiCandidate { id = "work", kind = "work", label = "常规家务", dist = 0 }
					}
				};
				request = BuildHttpRequest(settings, SerializePrompt(prompt));
			}
			catch (Exception exception)
			{
				TestMessages.Enqueue("AI 测试失败：" + exception.Message);
				return;
			}
			Task.Run(async () =>
			{
				AiRequestResult reply = await SendAsync(request);
				TestMessages.Enqueue(reply.ok
					? "AI 测试成功：返回 action=" + reply.action
					: "AI 测试失败：" + Truncate(reply.error, 200));
			});
		}

		public static void DrainTestMessages()
		{
			string message;
			while (TestMessages.TryDequeue(out message))
			{
				Messages.Message(message, MessageTypeDefOf.NeutralEvent, false);
			}
		}

		/// <summary>仅供 -colonykit-ai-test 使用：验证响应 JSON 解析。</summary>
		internal static AiRequestResult ParseForTest(string responseBody)
		{
			return ParseResponse(responseBody);
		}

		/// <summary>仅供 -colonykit-ai-test 使用：阻塞发一次请求，验证 HTTP/JSON 链路。</summary>
		internal static AiRequestResult SendBlockingForTest(RoboticsSettings settings, string promptJson)
		{
			try
			{
				AiHttpRequest request = BuildHttpRequest(settings, promptJson);
				// 自检连本机 mock：绕过系统代理，避免本机地址被代理转发导致超时。
				using (HttpClient testClient = new HttpClient(new HttpClientHandler { UseProxy = false })
				{
					Timeout = Timeout.InfiniteTimeSpan
				})
				{
					// 不能在主线程直接 GetResult()：Unity 的 SynchronizationContext 会把
					// await 的后续排回主线程，主线程又在等结果，形成死锁。
					return Task.Run(async () => await SendCoreAsync(request, testClient))
						.GetAwaiter().GetResult();
				}
			}
			catch (Exception exception)
			{
				return AiRequestResult.Failure(exception.Message);
			}
		}

		private static AiHttpRequest BuildRequest(Pawn pawn, RoboticsSettings settings)
		{
			AiPrompt prompt = BuildPrompt(pawn, settings);
			return BuildHttpRequest(settings, SerializePrompt(prompt));
		}

		private static AiHttpRequest BuildHttpRequest(RoboticsSettings settings, string promptJson)
		{
			string baseUrl = (settings.aiBaseUrl ?? "").Trim();
			if (baseUrl.Length == 0)
			{
				baseUrl = RoboticsSettings.DefaultAiBaseUrl;
			}
			string url = baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
				? baseUrl
				: baseUrl.TrimEnd('/') + "/chat/completions";

			string model = string.IsNullOrWhiteSpace(settings.aiModel)
				? RoboticsSettings.DefaultAiModel
				: settings.aiModel.Trim();
			float temperature = Mathf.Clamp(
				settings.aiTemperature, 0f, RoboticsSettings.MaxAiTemperature);
			int maxTokens = Mathf.Clamp(settings.aiMaxTokens,
				RoboticsSettings.MinAiMaxTokens, RoboticsSettings.MaxAiMaxTokens);
			return new AiHttpRequest
			{
				url = url,
				key = (settings.aiApiKey ?? "").Trim(),
				body = BuildBody(model, temperature, maxTokens, promptJson, false),
				// 推理模型偶尔把 token 全花在思考过程上、正文一个字都不剩，这里翻倍再来一次。
				retryBody = BuildBody(model, temperature,
					Mathf.Min(maxTokens * 2, RoboticsSettings.MaxAiMaxTokens), promptJson, true),
				timeout = Mathf.Clamp(settings.aiTimeout,
					RoboticsSettings.MinAiTimeout, RoboticsSettings.MaxAiTimeout)
			};
		}

		private static string BuildBody(
			string model, float temperature, int maxTokens, string promptJson, bool retry)
		{
			string system = retry
				? "你是 RimWorld Mk-V 机器人的机载 AI。上一次回复不是合法 JSON。"
					+ "这次只输出一个 JSON 对象，不要解释、不要思考过程、不要 markdown："
					+ "{\"action\":\"<候选 id>\",\"say\":\"<不超过 20 字的中文短句>\","
					+ "\"think\":\"<不超过 60 字、第一人称「我」的中文内心独白>\"}。"
				: "你是一台 RimWorld Mk-V 机器人的机载 AI。只能从给出的 candidates 里选择一个 action，"
					+ "只回复 JSON：{\"action\":\"<候选 id>\",\"say\":\"<不超过 20 字的中文短句>\","
					+ "\"think\":\"<不超过 60 字、第一人称「我」的中文内心独白>\"}。"
					+ "不要输出思考过程、解释或 markdown，只输出这一个 JSON 对象。"
					+ "think 写你此刻看到了什么、为什么这样决定，用「我」像日记一样叙述；say 是你嘴上说的一句话。"
					+ "选择规则："
					+ "① 有敌人时优先攻击：先打带范围武器的目标（燃烧瓶、手雷、火箭筒、迫击炮、喷火器——候选里 tag 为「范围」，或 threat 最高），"
					+ "其次打武器伤害高、命中高但脆弱的敌人，threat 越大越优先；"
					+ "② alert=true 表示刚收到敌袭预警：立刻放下家务，选 advance 或攻击候选，朝敌人方向主动出击，用远程武器在射程外开火，不要贴脸；"
					+ "③ colonists=0 表示地图上没有殖民者：你是唯一的守卫，选 guard 主动承担护卫职责；"
					+ "④ 没有威胁也没有紧急家务时：先 research_robotics 研究机器人科技，其次 assemble 制造同类；"
					+ "⑤ 优先保护殖民者、避免无谓送死：倒地的殖民者优先救援，标「危急」的（失血/重伤）比家务重要；"
					+ "附近没有床时会就地铺一张临时医疗睡眠点，把人抱过去再包扎。没有合适动作时才选 work。不要编造 id。"
					+ "⑥ 交战时边打边撤：敌人贴近就先退到射程边缘、掩体后面的位置再开火，别站着当固定靶；脚下或身边起火时先挪开再打。";
			string user = retry
				? "当前状态和候选动作（JSON）：\n" + promptJson
					+ "\n只输出一个 JSON 对象，例如 {\"action\":\"work\",\"say\":\"待命\",\"think\":\"没有威胁，我先待命。\"}。"
				: "当前状态和候选动作（JSON）：\n" + promptJson + "\n请选择一个 action。";

			StringBuilder body = new StringBuilder();
			body.Append("{\"model\":\"").Append(EscapeJson(model)).Append("\",\"messages\":[");
			body.Append("{\"role\":\"system\",\"content\":\"").Append(EscapeJson(system)).Append("\"},");
			body.Append("{\"role\":\"user\",\"content\":\"").Append(EscapeJson(user)).Append("\"}],");
			body.Append("\"temperature\":")
				.Append(temperature.ToString("0.##", CultureInfo.InvariantCulture));
			body.Append(",\"max_tokens\":").Append(maxTokens);
			body.Append(",\"stream\":false,\"response_format\":{\"type\":\"json_object\"}}");
			return body.ToString();
		}

		private static string SerializePrompt(AiPrompt prompt)
		{
			StringBuilder json = new StringBuilder();
			json.Append("{\"name\":\"").Append(EscapeJson(prompt.name ?? ""));
			json.Append("\",\"hp\":").Append(prompt.hp.ToString("0.00", CultureInfo.InvariantCulture));
			json.Append(",\"energy\":").Append(prompt.energy.ToString("0.00", CultureInfo.InvariantCulture));
			json.Append(",\"pos\":\"").Append(EscapeJson(prompt.pos ?? ""));
			json.Append("\",\"drafted\":").Append(prompt.drafted ? "true" : "false");
			json.Append(",\"hostiles\":").Append(prompt.hostiles);
			json.Append(",\"colonists\":").Append(prompt.colonists);
			json.Append(",\"downed\":").Append(prompt.downed);
			json.Append(",\"fires\":").Append(prompt.fires);
			json.Append(",\"alert\":").Append(prompt.alert ? "true" : "false");
			json.Append(",\"candidates\":[");
			if (prompt.candidates != null)
			{
				for (int i = 0; i < prompt.candidates.Length; i++)
				{
					AiCandidate candidate = prompt.candidates[i];
					if (candidate == null)
					{
						continue;
					}
					if (i > 0)
					{
						json.Append(',');
					}
					json.Append("{\"id\":\"").Append(EscapeJson(candidate.id ?? ""));
					json.Append("\",\"kind\":\"").Append(EscapeJson(candidate.kind ?? ""));
					json.Append("\",\"label\":\"").Append(EscapeJson(candidate.label ?? "")).Append('"');
					if (candidate.threat != 0)
					{
						json.Append(",\"threat\":").Append(candidate.threat);
					}
					if (!string.IsNullOrEmpty(candidate.tag))
					{
						json.Append(",\"tag\":\"").Append(EscapeJson(candidate.tag)).Append('"');
					}
					json.Append(",\"dist\":").Append(candidate.dist).Append('}');
				}
			}
			json.Append("]}");
			return json.ToString();
		}

		/// <summary>仅供 -colonykit-nannybot-test 使用：验证范围武器优先的兜底改目标。</summary>
		internal static string PreferAreaTargetForTest(Pawn pawn, string action)
		{
			return PreferAreaTarget(pawn, action);
		}

		/// <summary>仅供 -colonykit-nannybot-test 使用：验证候选 JSON（threat / tag / alert）能被解析。</summary>
		internal static string SerializePromptForTest(AiPrompt prompt)
		{
			return SerializePrompt(prompt);
		}

		private static AiPrompt BuildPrompt(Pawn pawn, RoboticsSettings settings)
		{
			Map map = pawn.Map;
			List<AiCandidate> candidates = new List<AiCandidate>();
			int hostiles = 0;
			int colonists = 0;
			int downed = 0;
			int fires = 0;

			if (settings.aiAllowCombat)
			{
				// 按威胁排序：范围武器最优先，其次是高伤/高命中，再考虑血少、护甲薄和距离。
				// 顺序和 threat 分会一起发给模型，让它既有排序也有理由。
				List<MkVThreat.Threat> ranked = MkVThreat.Rank(pawn, false);
				hostiles = ranked.Count;
				for (int i = 0; i < ranked.Count && i < 8; i++)
				{
					MkVThreat.Threat threat = ranked[i];
					candidates.Add(new AiCandidate
					{
						id = "attack:" + threat.pawn.thingIDNumber,
						kind = "attack",
						label = threat.label,
						dist = Mathf.RoundToInt(threat.distance),
						threat = threat.score,
						tag = MkVThreat.Tag(threat.profile)
					});
				}
			}

			foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
			{
				colonists++;
				if (!colonist.Downed || colonist.Dead)
				{
					continue;
				}
				downed++;
				float distance = colonist.Position.DistanceTo(pawn.Position);
				if (distance <= CandidateRange && candidates.Count < 8)
				{
					candidates.Add(new AiCandidate
					{
						id = "rescue:" + colonist.thingIDNumber,
						kind = "rescue",
						label = MkVMedicalAid.IsCritical(colonist)
							? "危急的 " + colonist.LabelShort + "（失血/重伤）"
							: "倒地的 " + colonist.LabelShort,
						dist = Mathf.RoundToInt(distance)
					});
				}
			}

			int fireCandidates = 0;
			foreach (Thing fire in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
			{
				if (fire == null || !fire.Spawned || fire.Destroyed)
				{
					continue;
				}
				float distance = fire.Position.DistanceTo(pawn.Position);
				if (distance > CandidateRange)
				{
					continue;
				}
				fires++;
				if (fireCandidates < 2)
				{
					candidates.Add(new AiCandidate
					{
						id = "fire:" + fire.thingIDNumber,
						kind = "fire",
						label = "火焰",
						dist = Mathf.RoundToInt(distance)
					});
					fireCandidates++;
				}
			}

			AddChoreCandidate(candidates, pawn, "butcher", "屠宰尸体（屠宰台账单）");
			AddChoreCandidate(candidates, pawn, "cook", "做饭（炉灶账单）");
			AddChoreCandidate(candidates, pawn, "craft", "手工制作（工作台账单）");
			AddChoreCandidate(candidates, pawn, "haul", "搬运收割的粮食/木头等散落物");
			AddChoreCandidate(candidates, pawn, "doctor", "治疗受伤的殖民者/囚犯");
			AddChoreCandidate(candidates, pawn, "research", "推进当前研究");

			// 地图上没有任何殖民者：Mk-V 就是唯一的守卫，给它一个主动站岗的候选。
			bool alone = !NannyBotUtility.AnyColonistPresent(map);
			if (alone)
			{
				candidates.Add(new AiCandidate
				{
					id = "guard",
					kind = "guard",
					label = "守卫殖民地（地图上没有殖民者）",
					dist = 0
				});
			}

			// 刚收到威胁信、敌人可能还在雾里赶路：主动出击，向他们的方向推进。
			bool alert = settings.aiRaidAdvance && MkVAlert.IsActive(map);
			if (alert)
			{
				IntVec3 advanceCell = MkVAlert.NearestHostileCell(map, pawn.Position);
				if (advanceCell.IsValid)
				{
					candidates.Add(new AiCandidate
					{
						id = "advance",
						kind = "advance",
						label = "朝敌袭警报方向主动出击",
						dist = Mathf.RoundToInt(pawn.Position.DistanceTo(advanceCell))
					});
				}
			}

			AddSelfWorkCandidates(candidates, pawn, settings);

			candidates.Add(new AiCandidate
			{
				id = "return",
				kind = "return",
				label = "返回充电桩/组装舱",
				dist = 0
			});
			candidates.Add(new AiCandidate
			{
				id = "patrol",
				kind = "patrol",
				label = "在基地附近巡逻",
				dist = 0
			});
			candidates.Add(new AiCandidate
			{
				id = "work",
				kind = "work",
				label = "交还给常规家务 AI",
				dist = 0
			});

			return new AiPrompt
			{
				name = pawn.LabelShort,
				hp = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
				energy = NannyBotUtility.Energy(pawn)?.CurLevelPercentage ?? 1f,
				pos = pawn.Position.x + "," + pawn.Position.z,
				drafted = pawn.Drafted,
				hostiles = hostiles,
				colonists = colonists,
				downed = downed,
				fires = fires,
				alert = alert,
				candidates = candidates.ToArray()
			};
		}

		/// <summary>空闲时的自主动作：研究机器人科技，或者在组装舱里制造一台新的同类。</summary>
		private static void AddSelfWorkCandidates(
			List<AiCandidate> candidates, Pawn pawn, RoboticsSettings settings)
		{
			if (settings.aiSelfResearch)
			{
				ResearchProjectDef project = MkVResearch.NextProject();
				if (project != null)
				{
					candidates.Add(new AiCandidate
					{
						id = "research_robotics",
						kind = "research",
						label = MkVResearch.LabelFor(project),
						dist = 0
					});
				}
			}
			if (settings.aiSelfAssemble)
			{
				Building bay = FindAssemblyBay(pawn);
				if (bay != null)
				{
					candidates.Add(new AiCandidate
					{
						id = "assemble",
						kind = "assemble",
						label = "在组装舱制造一台新的 Mk-V",
						dist = Mathf.RoundToInt(pawn.Position.DistanceTo(bay.Position))
					});
				}
			}
		}

		/// <summary>找一台现在就能开机的组装舱：研究完成、通电、材料齐全、走得到。</summary>
		private static Building FindAssemblyBay(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return null;
			}
			ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MkVAssemblyBay");
			if (def == null)
			{
				return null;
			}
			List<Building> bays = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(def);
			for (int i = 0; i < bays.Count; i++)
			{
				Building bay = bays[i];
				CompMkVAssemblyBay comp = bay.TryGetComp<CompMkVAssemblyBay>();
				if (comp == null || !comp.CanStart(out _))
				{
					continue;
				}
				if (!pawn.CanReach(bay, PathEndMode.InteractionCell, Danger.Deadly))
				{
					continue;
				}
				return bay;
			}
			return null;
		}

		private static void AddChoreCandidate(
			List<AiCandidate> candidates, Pawn pawn, string chore, string label)
		{
			if (ChoreJob(chore, pawn) == null)
			{
				return;
			}
			candidates.Add(new AiCandidate
			{
				id = "chore:" + chore,
				kind = "chore",
				label = label,
				dist = 0
			});
		}

		private static Job ChoreJob(string chore, Pawn pawn)
		{
			JobIssueParams issue = new JobIssueParams();
			switch (chore)
			{
				case "butcher":
					return new JobGiver_NannyBotButcher().TryIssueJobPackage(pawn, issue).Job;
				case "cook":
					return new JobGiver_NannyBotCook().TryIssueJobPackage(pawn, issue).Job;
				case "craft":
					return new JobGiver_NannyBotCrafting().TryIssueJobPackage(pawn, issue).Job;
				case "haul":
					return new JobGiver_NannyBotHaul().TryIssueJobPackage(pawn, issue).Job;
				case "doctor":
					return new JobGiver_NannyBotDoctor().TryIssueJobPackage(pawn, issue).Job;
				case "research":
					return new JobGiver_NannyBotResearch().TryIssueJobPackage(pawn, issue).Job;
				default:
					return null;
			}
		}

		/// <summary>身边有敌人、倒地殖民者或近处火焰时，允许 AI 打断当前家务。</summary>
		private static bool HasUrgentSituation(Pawn pawn)
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return false;
			}
			// 全图高危险（袭击进行中）时允许打断家务，哪怕敌人还在远处集合。
			if (map.dangerWatcher.DangerRating == StoryDanger.High)
			{
				return true;
			}
			// 刚收到敌袭预警：先放下手里的活，主动出击（敌人可能还在雾里赶路）。
			if (NannyBotUtility.Settings.aiRaidAdvance && MkVAlert.IsActive(map))
			{
				return true;
			}
			float threatRadius = NannyBotUtility.Settings.aiAllowCombat
				? 9999f
				: Mathf.Max(20f, NannyBotUtility.Settings.mkvEngageRange + 10f);
			foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
			{
				if (other == null || other == pawn || other.Dead || other.Downed
					|| !other.Spawned || !other.HostileTo(Faction.OfPlayer)
					|| other.Position.Fogged(map))
				{
					continue;
				}
				if (other.Position.InHorDistOf(pawn.Position, threatRadius))
				{
					return true;
				}
			}
			foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
			{
				if (colonist.Downed && !colonist.Dead
					&& colonist.Position.InHorDistOf(pawn.Position, 20f))
				{
					return true;
				}
			}
			// 附近有失血/伤势危急的倒地者：先放下家务去救人（可能要在旁边铺临时医疗点）。
			if (MkVMedicalAid.HasCriticalPatientNearby(pawn, 30f))
			{
				return true;
			}
			foreach (Thing fire in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
			{
				if (fire != null && fire.Spawned
					&& fire.Position.InHorDistOf(pawn.Position, 8f))
				{
					return true;
				}
			}
			return false;
		}

		private static async Task<AiRequestResult> SendAsync(AiHttpRequest request)
		{
			return await SendCoreAsync(request, Http);
		}

		private static async Task<AiRequestResult> SendCoreAsync(AiHttpRequest request, HttpClient client)
		{
			AiRequestResult result = await SendOnceAsync(request, client, request.body);
			if (result == null || result.ok || !result.formatFailure || string.IsNullOrEmpty(request.retryBody))
			{
				return result;
			}
			// 模型没按 JSON 回（推理模型常把 token 花在思考过程上）：用更严格的格式要求 + 双倍 token 再问一次。
			ModLog.Warn("Robotics", "Mk-V AI 回复不是 JSON，按严格格式重试一次：" + Truncate(result.error, 120));
			AiRequestResult retry = await SendOnceAsync(request, client, request.retryBody);
			if (retry != null && retry.ok)
			{
				return retry;
			}
			if (retry != null && !string.IsNullOrEmpty(result.error))
			{
				retry.error = Truncate(result.error, 120) + "；重试后仍失败：" + Truncate(retry.error, 120);
			}
			return retry ?? result;
		}

		private static async Task<AiRequestResult> SendOnceAsync(
			AiHttpRequest request, HttpClient client, string body)
		{
			try
			{
				using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, request.url))
				{
					message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.key);
					message.Content = new StringContent(body, Encoding.UTF8, "application/json");
					using (CancellationTokenSource timeout = new CancellationTokenSource(
						TimeSpan.FromSeconds(Mathf.Clamp(request.timeout, 5f, 120f))))
					using (HttpResponseMessage response = await client.SendAsync(message, timeout.Token))
					{
						string text = await response.Content.ReadAsStringAsync();
						if (!response.IsSuccessStatusCode)
						{
							return AiRequestResult.Failure(
								"HTTP " + (int)response.StatusCode + "：" + Truncate(text, 220));
						}
						return ParseResponse(text);
					}
				}
			}
			catch (TaskCanceledException)
			{
				return AiRequestResult.Failure("请求超时");
			}
			catch (Exception exception)
			{
				return AiRequestResult.Failure(exception.Message);
			}
		}

		private static AiRequestResult ParseResponse(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return AiRequestResult.Failure("响应为空", true);
			}

			int errorIndex = text.IndexOf("\"error\"", StringComparison.Ordinal);
			if (errorIndex >= 0)
			{
				int length = Math.Min(600, text.Length - errorIndex);
				string errorMessage = ExtractJsonString(text.Substring(errorIndex, length), "message");
				if (!string.IsNullOrEmpty(errorMessage))
				{
					return AiRequestResult.Failure("API 错误：" + Truncate(errorMessage, 200));
				}
			}

			string finish = ExtractJsonString(text, "finish_reason");
			string content = ExtractJsonString(text, "content");
			string reasoning = ExtractJsonString(text, "reasoning_content");
			if (string.IsNullOrWhiteSpace(content))
			{
				// 新版 DeepSeek 推理模型（deepseek-flash / reasoner）有时会把整段输出
				// 放进 reasoning_content、content 留空甚至整个字段都不给；直接判失败会让
				// AI 一条动作也发不出来（日志里就是「响应里没有 content」刷屏）。
				// 这里退回把 reasoning_content 当作正文再解析一遍。
				content = reasoning;
			}
			if (string.IsNullOrEmpty(content))
			{
				return AiRequestResult.Failure(
					"响应里没有 content（finish_reason=" + (finish ?? "?") + "）："
					+ Truncate(Sanitize(text), 300), true);
			}
			content = StripCodeFence(content.Trim());
			string action = ExtractAction(content);
			string say = ExtractJsonString(content, "say");
			string think = ExtractJsonString(content, "think");
			if (string.IsNullOrWhiteSpace(action))
			{
				int start = content.IndexOf('{');
				int end = content.LastIndexOf('}');
				if (start >= 0 && end > start)
				{
					string inner = content.Substring(start, end - start + 1);
					action = ExtractAction(inner);
					say = ExtractJsonString(inner, "say");
					think = ExtractJsonString(inner, "think");
				}
			}
			if (string.IsNullOrWhiteSpace(action) && !string.IsNullOrEmpty(reasoning)
				&& !string.Equals(reasoning, content, StringComparison.Ordinal))
			{
				// content 只是总结、JSON 塞在思考过程里：再翻一遍 reasoning_content。
				string innerReasoning = StripCodeFence(reasoning.Trim());
				action = ExtractAction(innerReasoning);
				if (!string.IsNullOrWhiteSpace(action))
				{
					say = ExtractJsonString(innerReasoning, "say");
					think = ExtractJsonString(innerReasoning, "think");
				}
			}
			if (string.IsNullOrWhiteSpace(action))
			{
				string hint = finish == "length"
					? "（回复被截断，finish_reason=length：把设置页「最大回复长度」调大，推理模型建议 1024 以上）"
					: "";
				return AiRequestResult.Failure(
					"模型没有返回 action" + hint + "：" + Truncate(Sanitize(content), 160), true);
			}
			return AiRequestResult.Success(
				action.Trim(), (say ?? "").Trim(), (think ?? "").Trim());
		}

		/// <summary>
		/// 把换行/制表符压成空格：这些文本会进 Player.log 和检查面板，
		/// 带换行会触发原版「Inspect string contains empty lines」警告，还会把日志撑成好几行。
		/// </summary>
		internal static string Sanitize(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text ?? "";
			}
			StringBuilder builder = new StringBuilder(text.Length);
			bool lastWasSpace = false;
			for (int i = 0; i < text.Length; i++)
			{
				char current = text[i];
				if (char.IsWhiteSpace(current))
				{
					if (!lastWasSpace)
					{
						builder.Append(' ');
						lastWasSpace = true;
					}
					continue;
				}
				builder.Append(current);
				lastWasSpace = false;
			}
			return builder.ToString().Trim();
		}

		/// <summary>
		/// 取动作 id。提示词要的是 "action"，但模型经常照着 candidates 的字段名回 "id"
		/// （DeepSeek 实际吐过 {"id":"work","say":"..."}），这里按常见别名依次兜底，
		/// 免得一份语义完全正确的回复被当成「请求失败」。
		/// </summary>
		private static string ExtractAction(string json)
		{
			string[] keys = { "action", "act", "id", "choice", "command" };
			for (int i = 0; i < keys.Length; i++)
			{
				string value = ExtractJsonString(json, keys[i]);
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}
			return null;
		}

		/// <summary>从 JSON 文本里取第一个 "key":"字符串值"；找不到返回 null。</summary>
		private static string ExtractJsonString(string json, string key)
		{
			if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
			{
				return null;
			}
			string needle = "\"" + key + "\"";
			int index = json.IndexOf(needle, StringComparison.Ordinal);
			if (index < 0)
			{
				return null;
			}
			index += needle.Length;
			while (index < json.Length && char.IsWhiteSpace(json[index]))
			{
				index++;
			}
			if (index >= json.Length || json[index] != ':')
			{
				return null;
			}
			index++;
			while (index < json.Length && char.IsWhiteSpace(json[index]))
			{
				index++;
			}
			if (index >= json.Length || json[index] != '"')
			{
				return null;
			}
			return ReadJsonString(json, ref index);
		}

		private static string ReadJsonString(string text, ref int index)
		{
			StringBuilder builder = new StringBuilder();
			index++;
			while (index < text.Length)
			{
				char current = text[index++];
				if (current == '"')
				{
					return builder.ToString();
				}
				if (current != '\\' || index >= text.Length)
				{
					builder.Append(current);
					continue;
				}
				char escaped = text[index++];
				switch (escaped)
				{
					case '"':
						builder.Append('"');
						break;
					case '\\':
						builder.Append('\\');
						break;
					case '/':
						builder.Append('/');
						break;
					case 'b':
						builder.Append('\b');
						break;
					case 'f':
						builder.Append('\f');
						break;
					case 'n':
						builder.Append('\n');
						break;
					case 'r':
						builder.Append('\r');
						break;
					case 't':
						builder.Append('\t');
						break;
					case 'u':
						if (index + 4 <= text.Length
							&& int.TryParse(text.Substring(index, 4),
								System.Globalization.NumberStyles.HexNumber,
								System.Globalization.CultureInfo.InvariantCulture,
								out int code))
						{
							builder.Append((char)code);
							index += 4;
						}
						break;
					default:
						builder.Append(escaped);
						break;
				}
			}
			return builder.ToString();
		}

		private static string EscapeJson(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			StringBuilder builder = new StringBuilder(text.Length + 16);
			for (int i = 0; i < text.Length; i++)
			{
				char current = text[i];
				switch (current)
				{
					case '"':
						builder.Append("\\\"");
						break;
					case '\\':
						builder.Append("\\\\");
						break;
					case '\n':
						builder.Append("\\n");
						break;
					case '\r':
						builder.Append("\\r");
						break;
					case '\t':
						builder.Append("\\t");
						break;
					default:
						if (current < 0x20)
						{
							builder.Append("\\u").Append(((int)current).ToString("x4"));
						}
						else
						{
							builder.Append(current);
						}
						break;
				}
			}
			return builder.ToString();
		}

		private static string StripCodeFence(string content)
		{
			if (!content.StartsWith("```", StringComparison.Ordinal))
			{
				return content;
			}
			int firstLine = content.IndexOf('\n');
			if (firstLine >= 0)
			{
				content = content.Substring(firstLine + 1);
			}
			int end = content.LastIndexOf("```", StringComparison.Ordinal);
			if (end >= 0)
			{
				content = content.Substring(0, end);
			}
			return content.Trim();
		}

		private static void ApplyResult(CompNannyBot comp, Pawn pawn, AiRequestResult result)
		{
			if (result == null)
			{
				return;
			}
			if (!result.ok)
			{
				string error = Truncate(Sanitize(result.error), 160);
				if (comp.AiLastError != error)
				{
					comp.AiLastError = error;
					ModLog.Warn("Robotics", "Mk-V AI 请求失败：" + error);
				}
				return;
			}

			comp.AiLastError = "";
			comp.AiLastAction = result.action ?? "";
			comp.AiLastSay = Sanitize(result.say);
			comp.AiLastThink = string.IsNullOrEmpty(result.think)
				? comp.AiLastSay
				: Sanitize(result.think);
			// 放在「已征召/精神崩溃就跳过」之前：打仗时恰恰最想回看它在想什么。
			LogThought(comp, pawn);
			if (pawn.Drafted || pawn.InMentalState || pawn.Dead || !pawn.Spawned)
			{
				return;
			}
			// 远行队优先：请求返回时商队已经在组建，就不再执行这条过期动作。
			if (NannyBotUtility.IsFormingCaravan(pawn))
			{
				return;
			}
			// 范围武器优先：模型偶尔会挑近处的普通目标，这里按威胁分兜底换成带燃烧瓶/手雷的敌人。
			comp.AiLastAction = PreferAreaTarget(pawn, comp.AiLastAction);
			if (NannyBotUtility.Settings.aiSpeech && !string.IsNullOrEmpty(comp.AiLastSay))
			{
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, comp.AiLastSay, 4f);
			}
			ExecuteAction(comp, pawn, comp.AiLastAction);
		}

		/// <summary>
		/// 范围武器优先：目标是普通敌人、但视野里有带范围武器的敌人时，换成威胁分最高的那个。
		/// 范围敌人比当前目标远 20 格以上时仍先打眼前的，避免为了追投弹手把后背留给近战。
		/// </summary>
		private static string PreferAreaTarget(Pawn pawn, string action)
		{
			if (string.IsNullOrEmpty(action) || !action.StartsWith("attack:", StringComparison.Ordinal))
			{
				return action;
			}
			int separator = action.IndexOf(':');
			int chosenId;
			if (separator < 0 || !int.TryParse(action.Substring(separator + 1), out chosenId))
			{
				return action;
			}
			Thing chosen = FindThing(pawn.Map, chosenId);
			if (chosen is Pawn chosenPawn && MkVThreat.Describe(chosenPawn).area)
			{
				return action;
			}
			MkVThreat.Threat area = null;
			foreach (MkVThreat.Threat threat in MkVThreat.Rank(pawn, false))
			{
				if (threat.profile.area)
				{
					area = threat;
					break;
				}
			}
			if (area == null)
			{
				return action;
			}
			float chosenDistance = chosen != null
				? pawn.Position.DistanceTo(chosen.Position)
				: float.MaxValue;
			if (area.distance > chosenDistance + 20f)
			{
				return action;
			}
			ModLog.Debug("Robotics", "Mk-V "
				+ pawn.LabelShort + " 改为优先攻击范围武器目标：" + area.label);
			return "attack:" + area.pawn.thingIDNumber;
		}

		/// <summary>
		/// 把第一人称想法追加到这台 Mk-V 的「日志」页（选中机器人 → 左下角日志）。
		/// PlayLog 是全局 150 条的环形缓冲，同一句话不重复写，免得把社交/战斗记录挤掉。
		/// </summary>
		private static void LogThought(CompNannyBot comp, Pawn pawn)
		{
			if (!NannyBotUtility.Settings.aiThoughtLog)
			{
				return;
			}
			if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map == null)
			{
				return;
			}
			if (Current.Game == null || Current.ProgramState != ProgramState.Playing)
			{
				return;
			}
			string thought = Truncate(comp.AiLastThink, MaxThoughtLength);
			if (string.IsNullOrWhiteSpace(thought) || thought == comp.AiLastLoggedThink)
			{
				return;
			}
			comp.AiLastLoggedThink = thought;
			Find.PlayLog.Add(new PlayLogEntry_MkVThought(
				pawn, thought, DefDatabase<LogEntryDef>.GetNamedSilentFail("CK_MkVThought")));
		}

		private static void ExecuteAction(CompNannyBot comp, Pawn pawn, string action)
		{
			if (string.IsNullOrEmpty(action) || action == "work")
			{
				return;
			}
			if (action.StartsWith("chore:", StringComparison.Ordinal))
			{
				Job choreJob = ChoreJob(action.Substring("chore:".Length), pawn);
				if (choreJob != null)
				{
					StartJob(pawn, choreJob);
				}
				else
				{
					comp.AiLastError = "当前没有可做的动作：" + action;
				}
				return;
			}
			if (action == "return")
			{
				Building station = NannyBotUtility.FindStation(pawn, false);
				IntVec3 cell = station != null ? station.InteractionCell : pawn.Position;
				StartJob(pawn, JobMaker.MakeJob(JobDefOf.Goto, cell));
				return;
			}
			if (action == "advance")
			{
				// 敌袭预警：主动出击，但停在敌人前面的空地上；靠近后由战斗 ThinkNode 接手开火。
				IntVec3 hostileCell = MkVAlert.NearestHostileCell(pawn.Map, pawn.Position);
				if (!hostileCell.IsValid)
				{
					comp.AiLastError = "没有可追击的敌人";
					return;
				}
				IntVec3 stand = CellFinder.StandableCellNear(hostileCell, pawn.Map, 6f);
				if (!stand.IsValid)
				{
					stand = hostileCell;
				}
				Job advance = JobMaker.MakeJob(JobDefOf.Goto, stand);
				advance.locomotionUrgency = LocomotionUrgency.Jog;
				advance.expiryInterval = 600;
				StartJob(pawn, advance);
				return;
			}
			if (action == "guard")
			{
				// 地图上没有殖民者：主动站岗（守门 / 巡逻 / 看仓库）。
				Job guard = new JobGiver_MkVGuard()
					.TryIssueJobPackage(pawn, new JobIssueParams()).Job;
				if (guard != null)
				{
					StartJob(pawn, guard);
					return;
				}
				IntVec3 cell = CellFinder.RandomClosewalkCellNear(
					pawn.Position, pawn.Map, 12, c => c.Standable(pawn.Map));
				if (cell.IsValid)
				{
					StartJob(pawn, JobMaker.MakeJob(JobDefOf.Goto, cell));
				}
				return;
			}
			if (action == "research_robotics")
			{
				ResearchProjectDef project = MkVResearch.NextProject();
				if (project == null || !MkVResearch.Select(project))
				{
					comp.AiLastError = "没有可研究的机器人科技";
					return;
				}
				Job research = ChoreJob("research", pawn);
				if (research != null)
				{
					StartJob(pawn, research);
				}
				else
				{
					comp.AiLastError = "研究台不可用";
				}
				return;
			}
			if (action == "assemble")
			{
				Building bay = FindAssemblyBay(pawn);
				JobDef assembleDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_MkVAssemble");
				if (bay == null || assembleDef == null)
				{
					comp.AiLastError = "组装舱现在不能开工";
					return;
				}
				Job assemble = JobMaker.MakeJob(assembleDef, bay);
				assemble.expiryInterval = 1200;
				StartJob(pawn, assemble);
				return;
			}
			if (action == "patrol")
			{
				IntVec3 cell = CellFinder.RandomClosewalkCellNear(
					pawn.Position, pawn.Map, 10, c => c.Standable(pawn.Map));
				if (cell.IsValid)
				{
					StartJob(pawn, JobMaker.MakeJob(JobDefOf.Goto, cell));
				}
				return;
			}

			int separator = action.IndexOf(':');
			if (separator <= 0)
			{
				comp.AiLastError = "AI 返回了未知动作：" + action;
				return;
			}
			string kind = action.Substring(0, separator);
			int targetId;
			if (!int.TryParse(action.Substring(separator + 1), out targetId))
			{
				comp.AiLastError = "AI 返回了无效目标：" + action;
				return;
			}
			Thing target = FindThing(pawn.Map, targetId);
			if (target == null || target.Destroyed || !target.Spawned)
			{
				comp.AiLastError = "AI 目标已消失：" + action;
				return;
			}

			if (kind == "attack")
			{
				float distance = pawn.Position.DistanceTo(target.Position);
				Verb verb = pawn.TryGetAttackVerb(target, false);
				if (verb != null && MkVCombatTactics.NeedsReposition(pawn, target, verb))
				{
					// 着火 / 被贴脸 / 没掩体：先挪到有掩体、离敌人更远又能开火的位置。
					IntVec3 spot = MkVCombatTactics.FindFiringPosition(pawn, target, verb);
					if (spot.IsValid && spot != pawn.Position)
					{
						Job reposition = JobMaker.MakeJob(JobDefOf.Goto, spot);
						reposition.locomotionUrgency = LocomotionUrgency.Jog;
						reposition.expiryInterval = 120;
						StartJob(pawn, reposition);
						return;
					}
				}
				if (verb != null && distance > verb.EffectiveRange)
				{
					// 目标在射程外：先跑过去，下一轮再开火/砍。
					Job move = JobMaker.MakeJob(JobDefOf.Goto, target.Position);
					move.locomotionUrgency = LocomotionUrgency.Jog;
					move.expiryInterval = 600;
					StartJob(pawn, move);
				}
				else
				{
					Job attack = JobMaker.MakeJob(
						distance <= 1.9f ? JobDefOf.AttackMelee : JobDefOf.AttackStatic,
						target);
					// 打几发就重新评估：敌人逼近时能后撤，而不是站着当靶子。
					attack.maxNumStaticAttacks = 3;
					attack.endIfCantShootTargetFromCurPos = true;
					attack.expiryInterval = 600;
					StartJob(pawn, attack);
				}
			}
			else if (kind == "fire")
			{
				StartJob(pawn, JobMaker.MakeJob(JobDefOf.BeatFire, target));
			}
			else if (kind == "rescue")
			{
				Job job = new JobGiver_NannyBotRescue()
					.TryIssueJobPackage(pawn, new JobIssueParams()).Job;
				if (job != null)
				{
					StartJob(pawn, job);
				}
			}
			else
			{
				comp.AiLastError = "AI 返回了未知动作：" + action;
			}
		}

		private static void StartJob(Pawn pawn, Job job)
		{
			if (job == null || pawn?.jobs == null || pawn.Map == null)
			{
				return;
			}
			if (pawn.CurJob != null && pawn.CurJob.def == job.def)
			{
				return;
			}
			pawn.jobs.StartJob(job, JobCondition.InterruptForced);
		}

		private static Thing FindThing(Map map, int thingId)
		{
			if (map == null)
			{
				return null;
			}
			foreach (Thing thing in map.listerThings.AllThings)
			{
				if (thing.thingIDNumber == thingId)
				{
					return thing;
				}
			}
			foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (pawn.thingIDNumber == thingId)
				{
					return pawn;
				}
			}
			return null;
		}

		private static string Truncate(string text, int max)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= max)
			{
				return text ?? "";
			}
			return text.Substring(0, max) + "…";
		}
	}
}
