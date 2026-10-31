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

		private static readonly HttpClient Http = CreateHttpClient();
		private static readonly ConcurrentDictionary<int, AiRequestResult> Results =
			new ConcurrentDictionary<int, AiRequestResult>();
		private static readonly ConcurrentDictionary<int, bool> InFlight =
			new ConcurrentDictionary<int, bool>();
		private static readonly ConcurrentQueue<string> TestMessages = new ConcurrentQueue<string>();
		private static int inFlightCount;
		private static int warnedNoKeyPawn;

		internal class AiHttpRequest
		{
			public string url;
			public string key;
			public string body;
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
			// 正在做家务（屠宰/搬运/做饭/手工等）时先让它做完；只有身边有敌人、
			// 倒地殖民者或近处火焰才允许 AI 打断。
			if (CompNannyBot.IsWorkJob(pawn.CurJobDef) && !HasUrgentSituation(pawn))
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(settings.aiApiKey))
			{
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
			if (ticks < comp.AiNextTick)
			{
				return;
			}

			comp.AiNextTick = ticks + Mathf.Max(1, Mathf.RoundToInt(settings.aiInterval * 60f));
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
			string system = "你是一台 RimWorld Mk-V 机器人的机载 AI。只能从给出的 candidates 里选择一个 action，"
				+ "只回复 JSON：{\"action\":\"<候选 id>\",\"say\":\"<不超过 20 字的中文短句>\"}。"
				+ "不要编造 id。优先保护殖民者、避免无谓送死；没有合适动作时选 work。";
			string user = "当前状态和候选动作（JSON）：\n" + promptJson + "\n请选择一个 action。";

			StringBuilder body = new StringBuilder();
			body.Append("{\"model\":\"").Append(EscapeJson(model)).Append("\",\"messages\":[");
			body.Append("{\"role\":\"system\",\"content\":\"").Append(EscapeJson(system)).Append("\"},");
			body.Append("{\"role\":\"user\",\"content\":\"").Append(EscapeJson(user)).Append("\"}],");
			body.Append("\"temperature\":")
				.Append(temperature.ToString("0.##", CultureInfo.InvariantCulture));
			body.Append(",\"max_tokens\":").Append(maxTokens);
			body.Append(",\"stream\":false,\"response_format\":{\"type\":\"json_object\"}}");

			return new AiHttpRequest
			{
				url = url,
				key = (settings.aiApiKey ?? "").Trim(),
				body = body.ToString(),
				timeout = Mathf.Clamp(settings.aiTimeout,
					RoboticsSettings.MinAiTimeout, RoboticsSettings.MaxAiTimeout)
			};
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
					json.Append("\",\"label\":\"").Append(EscapeJson(candidate.label ?? ""));
					json.Append("\",\"dist\":").Append(candidate.dist).Append('}');
				}
			}
			json.Append("]}");
			return json.ToString();
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
				int attacks = 0;
				foreach (IAttackTarget target in map.attackTargetsCache.TargetsHostileToColony)
				{
					Thing thing = target?.Thing;
					if (thing == null || !thing.Spawned || thing.Destroyed
						|| !GenHostility.IsActiveThreatToPlayer(target))
					{
						continue;
					}
					float distance = thing.Position.DistanceTo(pawn.Position);
					if (distance > CandidateRange)
					{
						continue;
					}
					hostiles++;
					if (attacks < 5)
					{
						candidates.Add(new AiCandidate
						{
							id = "attack:" + thing.thingIDNumber,
							kind = "attack",
							label = thing.LabelShort,
							dist = Mathf.RoundToInt(distance)
						});
						attacks++;
					}
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
						label = "倒地的 " + colonist.LabelShort,
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
				candidates = candidates.ToArray()
			};
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
			foreach (IAttackTarget target in map.attackTargetsCache.TargetsHostileToColony)
			{
				Thing thing = target?.Thing;
				if (thing != null && thing.Spawned
					&& thing.Position.InHorDistOf(pawn.Position, 20f)
					&& GenHostility.IsActiveThreatToPlayer(target))
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
			try
			{
				using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, request.url))
				{
					message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.key);
					message.Content = new StringContent(request.body, Encoding.UTF8, "application/json");
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
				return AiRequestResult.Failure("响应为空");
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

			string content = ExtractJsonString(text, "content");
			if (string.IsNullOrEmpty(content))
			{
				return AiRequestResult.Failure("响应里没有 content：" + Truncate(text, 160));
			}
			content = StripCodeFence(content.Trim());
			string action = ExtractJsonString(content, "action");
			string say = ExtractJsonString(content, "say");
			if (string.IsNullOrWhiteSpace(action))
			{
				int start = content.IndexOf('{');
				int end = content.LastIndexOf('}');
				if (start >= 0 && end > start)
				{
					string inner = content.Substring(start, end - start + 1);
					action = ExtractJsonString(inner, "action");
					say = ExtractJsonString(inner, "say");
				}
			}
			if (string.IsNullOrWhiteSpace(action))
			{
				return AiRequestResult.Failure("模型没有返回 action：" + Truncate(content, 160));
			}
			return AiRequestResult.Success(action.Trim(), (say ?? "").Trim());
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
				string error = Truncate(result.error, 160);
				if (comp.AiLastError != error)
				{
					comp.AiLastError = error;
					ModLog.Warn("Robotics", "Mk-V AI 请求失败：" + error);
				}
				return;
			}

			comp.AiLastError = "";
			comp.AiLastAction = result.action ?? "";
			comp.AiLastSay = result.say ?? "";
			if (pawn.Drafted || pawn.InMentalState || pawn.Dead || !pawn.Spawned)
			{
				return;
			}
			// 远行队优先：请求返回时商队已经在组建，就不再执行这条过期动作。
			if (NannyBotUtility.IsFormingCaravan(pawn))
			{
				return;
			}
			if (NannyBotUtility.Settings.aiSpeech && !string.IsNullOrEmpty(comp.AiLastSay))
			{
				MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, comp.AiLastSay, 4f);
			}
			ExecuteAction(comp, pawn, comp.AiLastAction);
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
				bool ranged = pawn.equipment?.Primary?.def?.IsRangedWeapon == true;
				Job job = JobMaker.MakeJob(
					ranged ? JobDefOf.AttackStatic : JobDefOf.AttackMelee, target);
				StartJob(pawn, job);
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
