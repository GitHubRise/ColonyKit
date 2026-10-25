using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using RimWorld;
using rjw.Modules.Shared.Logs;
using static RimWorld.LovePartnerRelationUtility;

using AllowedSex = rjw.RJWPreferenceSettings.AllowedSex;

using Benchy = rjw.Modules.Benchmark.Benchy;
using LudeonTK;

namespace rjw.Modules.Attraction
{
	[StaticConstructorOnStartup]
	public static class AttractionUtility
	{
		// TODO 列表：
		// 添加虫族专用的偏好。
		//   在见过整个虫族怀孕系统中若干奇怪的不一致之后，
		//   我大概会暂缓此事，直到我对那段
		//   代码做过重构、把这些问题理顺为止。
		// 对 `RaceGroupDef` 辅助类做一些改动。
		//   特别是，带有毛皮基因的人类需要 `Fur` 标签。
		//   这个也推迟。似乎有些 API 问题把它硬塞
		//     成只能配合 `PawnKindDef` 使用。公共 API 需要改成
		//     只基于 `Pawn`，因为 `PawnKindDef` 并不包含
		//     小人实际拥有哪些基因的具体信息（只包含可能拥有的）。这
		//     会使其成为破坏性改动。
		// 把约炮和随机强暴的逻辑改为使用加权随机选择。
		//   这涉及几个方法：
		//     `FindMates`——产生用于做爱的小人的生成器。
		//     `FindVictims`——产生用于强暴的小人的生成器。
		//     `FindRapists`——产生会强暴受害者的小人的生成器。
		//     `FindMate`、`FindVictim` 和 `FindRapist`，它们基本上是调用上面那些
		//       但之后会接 `.FirstOrDefault()`。
		// 将吸引力曲线移入配置定义。

		static AttractionUtility()
		{
			Type AP = typeof(AttractionPreference);
			StandardPreferenceApplicators = GenTypes.AllTypes
				.Where(t => !t.IsAbstract && AP.IsAssignableFrom(t))
				.SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				.Where(mi => mi.HasAttribute<StandardPreferenceAttribute>())
				.Select(ToDelegate)
				.OfType<ApplyPreference>()
				.ToArray();

			// 报告带标记的应用器的问题，且不会导致游戏崩溃。
			static ApplyPreference? ToDelegate(MethodInfo mi)
			{
				try
				{
					return (ApplyPreference)Delegate.CreateDelegate(typeof(ApplyPreference), mi);
				}
				catch (Exception e)
				{
					var name = $"{mi.DeclaringType.FullName}.{mi.Name}";
					Log.Error($"Failed to create `ApplyPreference` delegate for: {name}", e);
					return null;
				}
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(nameof(AttractionUtility));

		/// <summary>
		/// 曲线：将小人的 `Need_Sex.CurLevel` 映射为要施加的标量。
		/// </summary>
		private static readonly SimpleCurve needSexCurve = new()
		{
			// 欲求不满……
			new(0.0f, 1.6f),
			new(0.05f, 1.6f),
			// 性欲高涨……
			new(0.25f, 1.3f),
			// 中性……
			new(0.5f, 1.1f),
			// 满足……
			new(0.75f, 1f),
			new(1f, 1f)
		};

		/// <summary>
		/// 曲线：将小人已预约数占最大预约数的比例映射为
		/// 要施加的标量。
		/// </summary>
		static readonly SimpleCurve reservationsCurve = new()
		{
			new(0f, 1f),
			new(0.3f, 0.4f),
			new(1f, 0f)
		};

		/// <summary>
		/// <para>当调用 `Evaluate` 时未提供任何模式，就使用此项。</para>
		/// <para>这个顺序能最快命中主要的“不可行”检查，从而
		/// 尽早中止并节省一些处理时间。</para>
		/// </summary>
		private static readonly AttractionMode[] defaultModes = new[]
		{
			AttractionMode.Romantic,
			AttractionMode.Physical,
			AttractionMode.Social,
			AttractionMode.Age
		};

		/// <summary>
		/// 野人的野性值被硬编码为 75%；`Pawn.SpecialDisplayStats`
		/// 在 RimWorld 源码中将野性作为特例处理。
		/// </summary>
		public const float WildManWildness = 0.75f;

		/// <summary>
		/// <para>此委托用于注册标准偏好，以便在请求开始时
		/// 自动应用。</para>
		/// <para>创建一个与此委托签名匹配的静态方法，并为其添加
		/// <see cref="StandardPreferenceAttribute" /> 特性，该方法就会
		/// 自动注册到系统中。</para>
		/// <para>在你的应用器方法中，只需完成必要的检查，然后
		/// 决定将其加入请求或是不加入即可。简单！</para>
		/// </summary>
		/// <param name="request">请求。</param>
		public delegate void ApplyPreference(ref AttractionRequest request);

		/// <summary>
		/// 已注册的、用于应用小人标准偏好的函数列表。
		/// </summary>
		private static readonly ApplyPreference[] StandardPreferenceApplicators;

		/// <summary>
		/// 已注册的标准应用器数量。这个值很适合用来
		/// 初始化请求偏好的容量。
		/// </summary>
		public static int StandardApplicatorsCount => StandardPreferenceApplicators.Length;

		/// <summary>
		/// <para>从一般意义上评估两个小人之间的吸引力。</para>
		/// <para>如果你想把这种吸引力用于性行为，就应当
		/// 提供相应的目的。</para>
		/// </summary>
		/// <param name="pawn">进行观察的小人。</param>
		/// <param name="target">被观察的小人。</param>
		/// <returns>吸引力权重。</returns>
		public static float Evaluate(Pawn pawn, Pawn target)
		{
			var request = new AttractionRequest(pawn, target);
			return Evaluate(ref request, defaultModes);
		}

		/// <summary>
		/// 评估两个小人在指定目的下的吸引力。
		/// </summary>
		/// <param name="pawn">进行观察的小人。</param>
		/// <param name="target">被观察的小人。</param>
		/// <param name="purpose">该请求的最终目的。</param>
		/// <returns>吸引力权重。</returns>
		public static float Evaluate(Pawn pawn, Pawn target, AttractionPurpose purpose)
		{
			var request = new AttractionRequest(purpose, pawn, target);
			return Evaluate(ref request, defaultModes);
		}

		/// <summary>
		/// <para>评估一个吸引力请求，产生一个权重，表示
		/// 请求方小人对目标小人的感兴趣程度。</para>
		/// <para>此版本会遍历所有可能的“吸引模式”。</para>
		/// </summary>
		/// <param name="request">要评估的请求。</param>
		/// <returns>吸引力权重。</returns>
		public static float Evaluate(ref AttractionRequest request) =>
			Evaluate(ref request, defaultModes);

		/// <summary>
		/// <para>评估一个吸引力请求，产生一个权重，表示
		/// 请求方小人对目标小人的感兴趣程度。</para>
		/// <para>如果你只关注某些因素，此版本允许你只指定
		/// 特定的“吸引模式”。</para>
		/// </summary>
		/// <param name="request">要评估的请求。</param>
		/// <param name="modes">要遍历的模式。</param>
		/// <returns>吸引力权重。</returns>
		public static float Evaluate(ref AttractionRequest request, params AttractionMode[] modes)
		{
			//ModLog.Message($"Evaluate(Pawn: {request.Pawn}, Purpose: {request.Purpose}, finalize: {request.finalize})");
			if (modes.Length == 0) return 0f;
			if (!PreflightChecks(ref request)) return 0f;

			SetupRequest(ref request);

			request.preQuirkModifier?.Invoke(ref request);

			var factor = 1f;
			using (var evaluator = request.GetEvaluator())
			{
				foreach (var mode in modes)
				{
					factor *= evaluator.Evaluate(mode, ref request);
					// 若小于等于 0 则提前中止。
					if (factor <= 0f) return 0f;
				}
			}

			return Finalize(ref request, factor);
		}

		/// <summary>
		/// 为该请求设置偏好。
		/// </summary>
		/// <param name="request">要设置偏好的请求。</param>
		private static void SetupRequest(ref AttractionRequest request)
		{
			// 允许标准偏好为此请求自行完成设置。
			foreach (var applyTo in StandardPreferenceApplicators)
				applyTo(ref request);
		}

		/// <summary>
		/// 处理各种预检，确保请求不会允许
		/// 它本不该允许的事情。
		/// </summary>
		/// <param name="request">当前请求。</param>
		/// <returns>小人是否已获准“起飞”。</returns>
		private static bool PreflightChecks(ref AttractionRequest request)
		{
			// 目前这一切仅用于性互动。
			if (request.Purpose.IsNotForSex())
				return true;

			var pawn = request.Pawn;
			var target = request.Target;

			// 必须可达。
			if (pawn.Dead || pawn.Suspended || target.Suspended)
				return false;

			// 需要启用兽交。
			if (!RJWSettings.bestiality_enabled && request.Category.IsBestiality())
				return false;

			// 需要启用恋尸。
			if (!RJWSettings.necrophilia_enabled && target.Dead)
				return false;

			// 不得违反用户的性别设置。
			if (!IsGenderOk(pawn, target))
				return false;

			// 年龄必须够大，可以做爱。
			if (!IsAgeOk(pawn, target))
				return false;

			// 健康不足以进行性行为（除非小人不在乎）。
			if (!pawn.IsAnimal() && !xxx.is_healthy_enough(pawn))
				if (!xxx.is_psychopath(pawn))
					return false;

			// 不要阻止饥饿的小人进食（除非小人不在乎）。
			if (xxx.is_starved(target) && target.Faction == pawn.Faction)
				if (!xxx.is_psychopath(pawn) && !xxx.is_rapist(pawn))
					return false;

			// 濒死时不做爱（除非小人不在乎）。
			if (!request.ignoreBleeding && xxx.is_human(pawn) && !xxx.is_not_dying(target))
				if (!xxx.is_psychopath(pawn) && !xxx.is_rapist(pawn) && !xxx.is_bloodlust(pawn))
					return false;

			return true;
		}

		/// <summary>
		/// <para>根据年龄检查两个小人是否可以发生性行为。</para>
		/// <para>TODO：一旦 `SexAppraiser.would_fuck`
		/// 不再被广泛使用，就应将其改为私有。</para>
		/// </summary>
		/// <param name="pawn">第一个小人。</param>
		/// <param name="target">第二个小人。</param>
		/// <returns>他们是否可以进行。</returns>
		internal static bool IsAgeOk(Pawn pawn, Pawn target)
		{
			AgeCategory pawnAge = pawn.GetAgeCategory();
			AgeCategory targetAge = target.GetAgeCategory();

			if (pawnAge == AgeCategory.Child || targetAge == AgeCategory.Child)
			{
				return false;
			}

			if (pawnAge == AgeCategory.Adult && targetAge == AgeCategory.Adult)
			{
				return true;
			}

			if (pawnAge == AgeCategory.Youth && targetAge == AgeCategory.Youth)
			{
				return true;
			}

			// 这是一对“青年—成年”组合。为避免拆散两个青年组成的配对（一方后来成年）
			if (LovePartnerRelationExists(pawn, target))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// <para>检查 RJW 中有关性别限制的设置。</para>
		/// <para>这里只看性别，不看实际的性部位。</para>
		/// </summary>
		/// <param name="pawn">进行观察的小人。</param>
		/// <param name="target">被观察的小人。</param>
		/// <returns>该配对是否未违反任何规则。</returns>
		internal static bool IsGenderOk(Pawn pawn, Pawn target) =>
			(pawn.gender, target.gender) switch
			{
				(Gender.Male, Gender.Male) =>
					RJWPreferenceSettings.Malesex != AllowedSex.Nohomo,
				(Gender.Male, Gender.Female) =>
					RJWPreferenceSettings.Malesex != AllowedSex.Homo,
				(Gender.Female, Gender.Female) =>
					RJWPreferenceSettings.FeMalesex!= AllowedSex.Nohomo,
				(Gender.Female, Gender.Male) =>
					RJWPreferenceSettings.FeMalesex != AllowedSex.Homo,
				_ => true
			};
		
		internal static float Finalize(ref AttractionRequest request, float factor)
		{
			//ModLog.Message($"Finalize(factor: {factor})");
			if (factor <= 0f) return 0f;
			if (!request.finalize) return factor;

			var pawn = request.Pawn;
			var target = request.Target;

			if (request.Purpose.IsForSex())
			{
				// 没有该需求的东西（主要是动物）按中性性欲处理。
				var curLevel = pawn.needs.TryGetNeed<Need_Sex>()?.CurLevel ?? 0.5f;
				factor *= needSexCurve.Evaluate(curLevel);

				// 如果他们是花痴或服用过 hump shroom，则对该系数施加偏置。
				if (xxx.is_nympho(pawn) || pawn.health.hediffSet.HasHediff(RJWHediffDefOf.HumpShroomEffect))
					factor = 0.2f + 0.8f * factor;

				// 根据其他小人预约该目标的数量降低系数。
				var reservedCount = target.Dead ? 1f : ReservedCount(target);
				var reservedPercentage = reservedCount / xxx.max_rapists_per_prisoner;
				factor *= reservationsCurve.Evaluate(reservedPercentage);
			}

			return factor;
		}

		/// <summary>
		/// 将吸引力权重归一化到 0 与 1 之间，使其看起来更像
		/// 旧版的 `would_fuck` 值。
		/// </summary>
		/// <param name="weight">要归一化的权重。</param>
		/// <returns>归一化后的权重。</returns>
		[Obsolete]
		public static float Normalize(float weight)
		{
			if (weight <= 1f) return weight * 0.5f;
			return GenMath.LerpDouble(0f, 1f, 1f, 0.5f, 1f / weight);
		}

		/// <summary>
		/// 尝试将小人的身体标准化。
		/// </summary>
		/// <param name="pawn">要标准化其身体的小人。</param>
		/// <returns>标准化后的身体。</returns>
		public static StandardizedBody StandardizeBody(Pawn pawn)
		{
			if (pawn.story?.bodyType is { } bodyType)
			{
				// 与原版定义的快速直接匹配。
				if (bodyType == BodyTypeDefOf.Baby) return StandardizedBody.Baby;
				if (bodyType == BodyTypeDefOf.Child) return StandardizedBody.Child;
				if (bodyType == BodyTypeDefOf.Female) return StandardizedBody.Female;
				if (bodyType == BodyTypeDefOf.Male) return StandardizedBody.Male;
				if (bodyType == BodyTypeDefOf.Thin) return StandardizedBody.Thin;
				if (bodyType == BodyTypeDefOf.Hulk) return StandardizedBody.Hulk;
				if (bodyType == BodyTypeDefOf.Fat) return StandardizedBody.Fat;

				// 与标准原型的启发式匹配。
				var lowerName = bodyType.defName.ToLower();
				if (lowerName.Contains("baby")) return StandardizedBody.Baby;
				if (lowerName.Contains("child")) return StandardizedBody.Child;
				if (lowerName.Contains("girl")) return StandardizedBody.Child;
				if (lowerName.Contains("boy")) return StandardizedBody.Child;
				if (lowerName.Contains("female")) return StandardizedBody.Female;
				if (lowerName.Contains("male")) return StandardizedBody.Male;
				if (lowerName.Contains("thin")) return StandardizedBody.Thin;
				if (lowerName.Contains("hulk")) return StandardizedBody.Hulk;
				if (lowerName.Contains("fat")) return StandardizedBody.Fat;
			}

			if (xxx.is_mechanoid(pawn)) return StandardizedBody.Mechanoid;
			if (xxx.is_insect(pawn)) return StandardizedBody.Insectoid;
			if (pawn.IsAnimal()) return StandardizedBody.Animal;
			return StandardizedBody.Unknown;
		}

		/// <summary>
		/// 获取该小人相对目标小人的关系状态。
		/// </summary>
		/// <param name="pawn">小人。</param>
		/// <param name="target">另一个小人。</param>
		/// <returns>表示关系状态的值。</returns>
		public static AttractionStatus GetStatus(Pawn pawn, Pawn target)
		{
			var spouses = pawn.GetSpouses(false);
			if (spouses.Count == 0) return AttractionStatus.Open;
			if (spouses.Contains(target)) return AttractionStatus.Marital;
			return AttractionStatus.Extramarital;
		}

		/// <summary>
		/// <para>检查请求方小人是否愿意与人厮混。</para>
		/// <para>此处假定该小人已被检查为处于一段关系中。</para>
		/// </summary>
		/// <param name="request">请求。</param>
		/// <returns>请求方小人是否可以厮混。</returns>
		public static bool CanFoolAround(ref AttractionRequest request) =>
			CanFoolAround(request.Pawn, true);

		/// <summary>
		/// 检查指定小人是否愿意与人厮混。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="assumeInRelationship">若为 `true` 则跳过此项检查。</param>
		/// <returns>该小人是否可以厮混。</returns>
		public static bool CanFoolAround(Pawn pawn, bool assumeInRelationship = false)
		{
			// 放宽出轨规则的替代模式。
			if (RJWSettings.WildMode || RJWSettings.HippieMode) return true;

			// 如果没有情人或配偶，他们随时都可以厮混。
			if (!assumeInRelationship && !HasAnyLovePartner(pawn)) return true;

			// 他们愿意与任何人共享床铺吗？
			if(RJWHookupSettings.SharedBedMeansPoly)
			{
				var sharedBed
					= pawn.GetSpouseCount(false) > 0 ? HistoryEventDefOf.SharedBed_NonSpouse
					: HistoryEventDefOf.SharedBed;
				if (IdeoUtility.DoerWillingToDo(sharedBed, pawn)) return true;
			}

			// 他们愿意追求新人作为下一任配偶吗？
			var spouseCount = pawn.GetHistoryEventForLoveRelationCountPlusOne();
			if (IdeoUtility.DoerWillingToDo(spouseCount, pawn)) return true;

			// 到此为止，他们的理念不会允许，但还有其他模组
			// 将多角恋作为特质加入。人们会希望它们修改上述检查
			// 以真正实现该功能，但以防它们只是朴素实现，
			// 我们会检查该小人是否处于排他性的多角恋关系中。
			return !xxx.HasNonPolyPartner(pawn);
		}

		/// <summary>
		/// 检查请求的目标是否曾对请求方小人做过
		/// 使其活该遭到报复性强暴的事情。
		/// </summary>
		/// <param name="request">请求。</param>
		/// <returns>是否应遭报复性强暴。</returns>
		public static bool CanHaveRevengeRape(ref AttractionRequest request)
		{
			var pawn = request.Pawn;
			var target = request.Target;

			// 不涉及动物的报复性强暴……目前还没有。
			if (request.Category.InvolvesAnimals()) return false;

			// 如果目标对该小人出轨，就符合报复性强暴的条件。
			foreach (var rel in request.Relations)
				if (IsExLovePartnerRelation(rel))
					if (pawn.HasMemoryWith(target, ThoughtDefOf.CheatedOnMe))
						return true;

			// 如果目标没有关系追踪器，接下来的部分可能失败，
			// 例如机械族。
			if (target.relations is null) return false;

			// 为了增加趣味，我们还会检查他们是否是“另一个情人”。
			foreach (var rel in ExistingLovePartners(target, false))
				if (ExLovePartnerRelationExists(pawn, rel.otherPawn))
					if (pawn.HasMemoryWith(rel.otherPawn, ThoughtDefOf.CheatedOnMe))
						return true;

			return false;
		}

		/// <summary>
		/// 检查两个小人之间是否存在持续或残留的
		/// 敌意。
		/// </summary>
		/// <param name="pawn">第一个小人。</param>
		/// <param name="target">第二个小人。</param>
		/// <returns>是否存在敌意。</returns>
		public static bool HasHostility(Pawn pawn, Pawn target)
		{
			// 直接的个人敌意。
			if (target.HostileTo(pawn) || pawn.HostileTo(target)) return true;

			// 派系敌意。
			if (target.Faction is { } tFaction)
				if (pawn.HostileTo(tFaction))
					return true;
			
			if (pawn.Faction is { } pFaction)
				if (target.HostileTo(pFaction))
					return true;

			// 社交斗殴留下的残留敌意。
			if (!pawn.IsAnimal() && !target.IsAnimal())
				if (pawn.HasMemoryWith(target, ThoughtDefOf.HadAngeringFight))
					return true;

			return false;
		}

		/// <summary>
		/// 检查请求中的小人之间是否存在持续或残留的
		/// 敌意。
		/// </summary>
		/// <param name="request">请求。</param>
		/// <returns>是否存在敌意。</returns>
		public static bool HasHostility(ref AttractionRequest request) =>
			HasHostility(request.Pawn, request.Target);

		/// <summary>
		/// 获取两个小人的吸引力类别。
		/// </summary>
		/// <param name="observer">进行观察的小人。</param>
		/// <param name="target">被观察的小人</param>
		/// <returns>吸引力类别。</returns>
		public static AttractionCategory GetCategory(Pawn observer, Pawn target) =>
			(observer.IsAnimal(), target.IsAnimal()) switch
			{
				(false, false) => AttractionCategory.BetweenHumans,
				(true, true) => AttractionCategory.BetweenAnimals,
				(true, false) => AttractionCategory.AnimalToHuman,
				(false, true) => AttractionCategory.HumanToAnimal
			};

		private static int ReservedCount(Pawn pawn)
		{
			if (pawn is null) return 0;

			var reservations = pawn.Map.reservationManager.ReservationsReadOnly;
			if (reservations.Count == 0) return 0;

			var ret = 0;
			foreach (var t in reservations)
				if (t.Target.Pawn == pawn)
					ret += 1;
			return ret;
		}
		
		/// <summary>
		/// <para>调试工具：显示一个小人对当前地图上
		/// 所有其他小人的吸引力系数表。</para>
		/// <para>它出现在调试操作菜单的 RJW 分类下。</para>
		/// </summary>
		[DebugAction("RJW", actionType = DebugActionType.ToolMapForPawns)]
		internal static void AttractionFactorsForPawn(Pawn pawn)
		{
			var dataSource = pawn.Map.mapPawns.AllPawnsSpawned
				.Where(t => t != pawn)
				.OrderBy(t => xxx.get_pawnname(t))
				.SelectMany(ToData)
				.ToList();

			var table = new TableDataGetter<(Pawn pawn, Pawn target, AttractionPurpose purpose)>[]
			{
				new("pawn", (d) => xxx.get_pawnname(d.pawn)),
				new("target", (d) => xxx.get_pawnname(d.target)),
				new("purpose", (d) => d.purpose.ToString()),
				new("ageOK", (d) => IsAgeOk(d.pawn, d.target)),
				new("preflightOK", DoPreflight),
				new("finalized", (d) => DoEval(d, true)),
				new("combined", (d) => DoEval(d, false)),
				new("age", (d) => GetFactor(d, AttractionMode.Age)),
				new("physical", (d) => GetFactor(d, AttractionMode.Physical)),
				new("romantic", (d) => GetFactor(d, AttractionMode.Romantic)),
				new("social", (d) => GetFactor(d, AttractionMode.Social))
			};

			DebugTables.MakeTablesDialog(dataSource, table);

			// 辅助函数……

			IEnumerable<(Pawn pawn, Pawn target, AttractionPurpose purpose)> ToData(Pawn target)
			{
				yield return (pawn, target, AttractionPurpose.General);
				yield return (pawn, target, AttractionPurpose.ForFucking);
				yield return (pawn, target, AttractionPurpose.ForRape);
			}

			static string DoPreflight((Pawn pawn, Pawn target, AttractionPurpose purpose) d)
			{
				var request = new AttractionRequest(d.purpose, d.pawn, d.target);
				return PreflightChecks(ref request).ToString();
			}

			static string DoEval((Pawn pawn, Pawn target, AttractionPurpose purpose) d, bool finalize)
			{
				using var _benchy = Benchy.Watch("AttractionUtility.Evaluate");
				var request = new AttractionRequest(d.purpose, d.pawn, d.target)
				{
					finalize = finalize
				};
				return Evaluate(ref request).ToString("F3");
			}

			static string GetFactor((Pawn pawn, Pawn target, AttractionPurpose purpose) d, AttractionMode mode)
			{
				var request = new AttractionRequest(d.purpose, d.pawn, d.target)
				{
					finalize = false
				};

				// 我们希望跳过预检，因此自行进行评估。
				SetupRequest(ref request);

				using var evaluator = request.GetEvaluator();
				return evaluator.Evaluate(mode, ref request).ToString("F3");
			}
		}

		private enum PawnMenuType
		{
			Colonist,
			Prisoner,
			Slave,
			ColonyAnimal,
			Visitor,
			TamedAnimal,
			Wild
		}

		[DebugAction("RJW", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		internal static void ExplainAttraction()
		{
			
			var pawnsByType = Find.CurrentMap.mapPawns.AllPawnsSpawned
				.OrderBy((pawn) => xxx.get_pawnname(pawn))
				.GroupBy((pawn) => MenuType(pawn))
				.ToDictionary((g) => g.Key, (g) => g.ToList());
			var types = (PawnMenuType[])Enum.GetValues(typeof(PawnMenuType));

			PickObserver();

			// 辅助函数……

			void PickObserver()
			{
				var observerList = new List<DebugMenuOption>();
				foreach (var type in types)
				{
					if (!pawnsByType.TryGetValue(type, out var pawnList)) continue;
					if (pawnList.Count == 0) continue;
					foreach (var pawn in pawnList)
					{
						observerList.Add(new DebugMenuOption(
							PawnLabelWithType(pawn, type),
							DebugMenuOptionMode.Action,
							PickTarget(pawn)
						));
					}
				}

				if (observerList.Count > 0)
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(observerList));
				else
					Messages.Message("AttractionDebug_NoPawns".Translate(), MessageTypeDefOf.NegativeEvent, false);
			}

			Action PickTarget(Pawn selectedPawn) => () =>
			{
				var targetList = new List<DebugMenuOption>();
				foreach (var type in types)
				{
					if (!pawnsByType.TryGetValue(type, out var pawnList)) continue;
					if (pawnList.Count == 0) continue;
					foreach (var targetPawn in pawnList)
					{
						if (targetPawn == selectedPawn) continue;
						targetList.Add(new DebugMenuOption(
							PawnLabelWithType(targetPawn, type),
							DebugMenuOptionMode.Action,
							PickPurpose(selectedPawn, targetPawn)
						));
					}
				}

				if (targetList.Count > 0)
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(targetList));
				else
					Messages.Message("AttractionDebug_NoOtherPawns".Translate(), MessageTypeDefOf.NegativeEvent, false);
			};

			Action PickPurpose(Pawn observer, Pawn target) => () =>
			{
				var purposeList = new List<DebugMenuOption>();
				var purposes = (AttractionPurpose[])Enum.GetValues(typeof(AttractionPurpose));
				foreach (var purpose in purposes)
				{
					purposeList.Add(new DebugMenuOption(
						AttractionText.PurposeName(purpose),
						DebugMenuOptionMode.Action,
						() => ExplainAttraction(observer, target, purpose)
					));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(purposeList));
			};

			static string PawnLabelWithType(Pawn pawn, PawnMenuType type) =>
				$"{AttractionText.TranslateOr("AttractionDebug_PawnType_" + type, GenText.SplitCamelCase(type.ToString()))} {GetPawnLabel(pawn)}";

			static PawnMenuType MenuType(Pawn x)
			{
				if (x.IsHumanLike())
				{
					if (x.IsColonist) return PawnMenuType.Colonist;
					if (x.IsPrisonerOfColony) return PawnMenuType.Prisoner;
					if (x.IsSlaveOfColony) return PawnMenuType.Slave;
					if (x.IsWildMan()) return PawnMenuType.Wild;
					return PawnMenuType.Visitor;
				}
				if (x.IsTameAnimal())
				{
					if (x.Faction == Faction.OfPlayer) return PawnMenuType.ColonyAnimal;
					return PawnMenuType.TamedAnimal;
				}
				return PawnMenuType.Wild;
			}
		}

		/// <summary>
		/// 打开一个显示吸引力详细分解的新窗口
		/// </summary>
		public static void ExplainAttraction(Pawn observer, Pawn target, AttractionPurpose purpose)
		{
			var sb = new StringBuilder();
			sb.Append("AttractionDebug_Observer".Translate()).Append(": ").AppendLine(GetPawnLabel(observer));
			sb.Append("AttractionDebug_Target".Translate()).Append(": ").AppendLine(GetPawnLabel(target));
			sb.Append("AttractionDebug_Purpose".Translate()).Append(": ").AppendLine(AttractionText.PurposeName(purpose));
			sb.AppendLine();

			var request = new AttractionRequest(purpose, observer, target);
			SetupRequest(ref request);

			// 遍历各模式并显示各项说明。各偏好如果最终
			// 没有改变系数，就会淡化自己说明的颜色，
			// 即该系数。
			var modes = new AttractionMode[] {
				AttractionMode.Age,
				AttractionMode.Physical,
				AttractionMode.Romantic,
				AttractionMode.Social
			};
			var factors = new List<float>();
			using (var evaluator = request.GetEvaluator())
			{
				foreach (var mode in modes)
				{
					var textList = evaluator.Explain(mode, ref request, out var modeFactor);
					factors.Add(modeFactor);

					sb.AppendTagged(AttractionText.ModeName(mode).AsTipTitle());
					sb.Append(": ").AppendLine(FactorStr(modeFactor));
					foreach (var text in textList) sb.AppendLineTagged(text);
					sb.AppendLine();
				}
			}

			// 演示各系数是如何组合的。
			var combinedFactor = factors.Aggregate(1f, (acc, factor) => acc * factor);
			sb.AppendTagged(AttractionText.TranslateOr("AttractionDebug_Combined", "Combined").AsTipTitle());
			sb.Append(": ").Append(string.Join(" * ", factors.Select(FactorStr)));
			sb.Append(" = ").AppendLine(FactorStr(combinedFactor));
			sb.AppendLine();

			// 应用终结器以得到实际结果值。
			sb.AppendTagged(AttractionText.TranslateOr("AttractionDebug_Finalized", "Finalized").AsTipTitle());
			sb.Append(": ").Append(FactorStr(Finalize(ref request, combinedFactor)));

			Find.WindowStack.Add(new Dialog_MessageBox(sb.ToString().Trim()));

			static string FactorStr(float factor) =>
				factor.ToStringByStyle(ToStringStyle.FloatMaxThree);
		}

		private static string GetPawnLabel(Pawn pawn)
		{
			var name = xxx.get_pawnname(pawn);
			var sex = AttractionText.SexName(GenderHelper.GetSex(pawn));
			var race = GetRaceLabel(pawn);
			var age = pawn.ageTracker.AgeBiologicalYears;
			if (name.ToLower() == race.ToLower())
				return $"{name} ({sex}, {age})";
			return $"{name} ({sex} {race}, {age})";
		}

		private static string GetRaceLabel(Pawn pawn)
		{
			if (pawn.IsHumanLike() && ModsConfig.BiotechActive)
				if (pawn.genes is { } genes && genes.Xenotype != XenotypeDefOf.Baseliner)
					return genes.XenotypeLabelCap;
			if (pawn.IsHumanLike())
				return pawn.def.LabelCap;
			return GenText.ToTitleCaseSmart(pawn.KindLabel);
		}
	}
}