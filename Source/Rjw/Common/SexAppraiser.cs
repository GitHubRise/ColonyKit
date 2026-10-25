using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using rjw.Modules.Attraction;
using rjw.Modules.Shared.Extensions;

using ModifyPreferences = rjw.Modules.Attraction.AttractionRequest.ModifyPreferences;
using R_Orientation = rjw.Modules.Attraction.StandardPreferences.R_Orientation;
using R_Homewrecking = rjw.Modules.Attraction.StandardPreferences.R_Homewrecking;
using R_Relationship = rjw.Modules.Attraction.StandardPreferences.R_Relationship;
using Seeded = rjw.Modules.Rand.Seeded;

namespace rjw
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="Purpose">
	/// <para>观察者将如何评估目标。</para>
	/// <para>各项因素的算法会随此值不同而变化。</para>
	/// </param>
	/// <param name="PreQuirkModifier">
	/// 可选修饰项，可在癖好生效前自定义偏好。
	/// </param>
	/// <param name="PostQuirkModifier">
	/// 可选修饰项，可在癖好生效后自定义偏好。
	/// </param>
	/// <param name="IgnoreBleeding">
	/// <para>用于忽略流血预检查的选项。</para>
	/// <para>仅当用途为性行为时才会考虑。</para>
	/// </param>
	public readonly record struct AppraisalSettings(
		AttractionPurpose Purpose, // = AttractionPurpose.ForFucking，——这样不行，record 是用 AttractionPurpose.General 初始化的
		ModifyPreferences? PreQuirkModifier = null,
		[property: System.Obsolete()]
		ModifyPreferences? PostQuirkModifier = null,
		bool IgnoreBleeding = false
	);

	/// <summary>
	/// <para>一个参数对象，用于指定一次评估所需的一切。</para>
	/// <para>若要为 <see cref="AttractionRequest" /> 中的每个小人提供不同设置，
	/// 请使用对象初始化器语法来设置。</para>
	/// <para>使用 <see cref="SexAppraiser.CandidatesAsTargets" /> 或
	/// <see cref="SexAppraiser.CandidatesAsObservers" /> 来指定哪些设置
	/// 适用于哪个小人。</para>
	/// </summary>
	/// <param name="Pawn">
	/// 评估中心的小人。
	/// </param>
	/// <param name="Candidates">
	/// 候选者集合。
	/// </param>
	public readonly record struct AppraisalParams(
		Pawn Pawn,
		IEnumerable<Pawn> Candidates
		)
	{
		/// <summary>
		/// 为观察者构造 <see cref="AttractionRequest" /> 时
		/// 使用的设置。
		/// </summary>
		public AppraisalSettings ObserverSettings { get; init; } = new(AttractionPurpose.ForFucking);

		/// <summary>
		/// 为目标构造 <see cref="AttractionRequest" /> 时
		/// 使用的设置。
		/// </summary>
		public AppraisalSettings TargetSettings { get; init; } = new(AttractionPurpose.ForFucking);
	}

	/// <summary>
	/// 一个包含评估结果信息的类。
	/// </summary>
	public record AppraisalResult(
		Pawn Observer,
		AppraisalSettings ObserverSettings,
		Pawn Target,
		AppraisalSettings TargetSettings
		)
	{
		/// <summary>
		/// <para>观察者对目标的吸引力。</para>
		/// <para>首次访问该值会触发吸引力评估，
		/// 这可能会很昂贵。除非确有需要，否则避免访问该值。</para>
		/// </summary>
		public float ObserverAttraction
		{
			get
			{
				if (!observerAttraction.HasValue)
				{
					var request = new AttractionRequest(ObserverSettings.Purpose, Observer, Target)
					{
						preQuirkModifier = ObserverSettings.PreQuirkModifier,
						ignoreBleeding = ObserverSettings.IgnoreBleeding
					};
					observerAttraction = AttractionUtility.Evaluate(ref request);
				}
				return observerAttraction.Value;
			}
		}

		private float? observerAttraction = null;

		/// <summary>
		/// <para>目标对观察者的吸引力。</para>
		/// <para>首次访问该值会触发吸引力评估，
		/// 这可能会很昂贵。除非确有需要，否则避免访问该值。</para>
		/// </summary>
		public float TargetAttraction
		{
			get
			{
				if (!targetAttraction.HasValue)
				{
					var request = new AttractionRequest(TargetSettings.Purpose, Target, Observer)
					{
						preQuirkModifier = TargetSettings.PreQuirkModifier,
						ignoreBleeding = TargetSettings.IgnoreBleeding
					};
					targetAttraction = AttractionUtility.Evaluate(ref request);
				}
				return targetAttraction.Value;
			}
		}
		private float? targetAttraction = null;

		/// <summary>
		/// <para>两个小人之间的综合吸引力。</para>
		/// <para>访问时会同时调用 <see cref="ObserverAttraction" /> 和
		/// <see cref="TargetAttraction" />，如果此前尚未评估过，
		/// 这会为双方评估吸引力，可能开销很大。除非确有需要，
		/// 否则避免访问该值。</para>
		/// </summary>
		public float CombinedAttraction => ObserverAttraction + TargetAttraction;
	}

	/// <summary>
	/// 负责评判潜在伴侣的性感程度。
	/// </summary>
	public static class SexAppraiser
	{
		public const float base_sat_per_fuck = 0.40f;
		public const float base_attraction = 0.60f;
		public const float no_partner_ability = 0.8f;

		public const float MinimumAttractionForSex = 0.1f;

		/// <summary>
		/// <para>获取该小人在出现可乘之机时
		/// 实施强暴的概率。</para>
		/// <para>老实说，这或许应当只影响强暴工作的 MTB-小时，
		/// 而不是在开始工作前运行。</para>
		/// </summary>
		/// <param name="rapist">正在考虑强暴的小人。</param>
		/// <returns>0 到 1 之间的概率。</returns>
		public static float RapeChanceFor(Pawn rapist)
		{
			// 初始概率为 30%。
			var rapeChance = 0.3f;

			// 发情时更倾向于强暴。
			if (rapist.needs.TryGetNeed<Need_Sex>() is { } horniness)
				if (horniness.CurLevel <= horniness.thresh_horny())
					rapeChance += 0.25f;

			// 醉得什么都不在乎了……
			if (rapist.HasBeerGoggles(out var severity))
				rapeChance *= GenMath.LerpDouble(0f, 1f, 1f, 1.3f, severity);

			// 特质带来的系数增加。
			if (xxx.is_rapist(rapist))
				rapeChance *= 1.5f;
			if (xxx.is_nympho(rapist))
				rapeChance *= 1.25f;
			if (xxx.is_bloodlust(rapist))
				rapeChance *= 1.2f;
			if (xxx.is_psychopath(rapist))
				rapeChance *= 1.2f;

			// 特质带来的系数降低。
			if (xxx.is_masochist(rapist))
				rapeChance *= 0.8f;

			// 强暴者实在太无聊了……
			if (rapist.needs.joy?.CurLevel is < 0.1f)
				rapeChance *= 1.2f;

			rapeChance = Mathf.Clamp01(rapeChance);

			// ModLog.Message($"rjw::xxx rape chance for {xxx.get_pawnname(rapist)} is {rapeChance}");

			return rapeChance;
		}

		/// <summary>
		/// 返回该小人是否处于想强暴的心情。
		/// </summary>
		/// <param name="rapist">正在考虑强暴的小人。</param>
		/// <returns>该小人是否处于想强暴的心情。</returns>
		public static bool InMoodForRape(Pawn rapist)
		{
			using (Seeded.ForHour(rapist))
				return Rand.Chance(RapeChanceFor(rapist));
		}

		/// <summary>
		/// <para>为观察者寻找自愿性行为的伴侣。</para>
		/// <para>这使用加权随机算法，第一个结果未必是最佳选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="observer">寻找性行为的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> FindPartners(
			Pawn observer,
			IEnumerable<Pawn> candidates)
		{
			using (Seeded.ForHour(observer))
				return RNG.FindPartners(observer, candidates).ToList();
		}

		/// <summary>
		/// <para>尝试为观察者找到一名自愿性行为的伴侣，
		/// 人选来自给定的候选列表。</para>
		/// <para>这使用加权随机算法，可能不会选择最优选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="observer">寻找性行为的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <returns>是否找到了结果。</returns>
		public static bool FindPartner(
			Pawn observer,
			IEnumerable<Pawn> candidates,
			out AppraisalResult result)
		{
			using (Seeded.ForHour(observer))
				return RNG.FindPartners(observer, candidates).TryFirst(out result);
		}

		/// <summary>
		/// <para>为观察者卖淫寻找客户。</para>
		/// <para>这使用加权随机算法，第一个结果未必是最佳选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="whore">寻找工作的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> FindTricks(
			Pawn whore,
			IEnumerable<Pawn> candidates)
		{
			using (Seeded.ForHour(whore))
				return RNG.FindTricks(whore, candidates).ToList();
		}

		/// <summary>
		/// <para>尝试为观察者的卖淫活动找到一名客户，
		/// 人选来自给定的候选列表。</para>
		/// <para>这使用加权随机算法，可能不会选择最优选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="whore">寻找工作的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <returns>是否找到了结果。</returns>
		public static bool FindTrick(
			Pawn whore,
			IEnumerable<Pawn> candidates,
			out AppraisalResult result)
		{
			using (Seeded.ForHour(whore))
				return RNG.FindTricks(whore, candidates).TryFirst(out result);
		}

		/// <summary>
		/// <para>为目标寻找可供其使用的妓女。</para>
		/// <para>这基本上与 <see cref="FindTricks" /> 相反。在
		/// 结果中，观察者是妓女，而目标是给定的
		/// `client`。</para>
		/// <para>这使用加权随机算法，第一个结果未必是最佳选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="client">寻找性交对象的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> FindWhores(
			Pawn client,
			IEnumerable<Pawn> candidates)
		{
			using (Seeded.ForHour(client))
				return RNG.FindWhores(client, candidates).ToList();
		}

		/// <summary>
		/// <para>尝试为目标从给定候选列表中
		/// 找到一名可供其使用的妓女。</para>
		/// <para>这基本上与 <see cref="FindTrick" /> 相反。在
		/// 结果中，观察者是妓女，而目标是给定的
		/// `client`。</para>
		/// <para>这使用加权随机算法，可能不会选择最优选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="client">寻找性交对象的小人。</param>
		/// <param name="candidates">候选小人列表。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <returns>是否找到了结果。</returns>
		public static bool FindWhore(
			Pawn client,
			IEnumerable<Pawn> candidates,
			out AppraisalResult result)
		{
			using (Seeded.ForHour(client))
				return RNG.FindWhores(client, candidates).TryFirst(out result);
		}

		/// <summary>
		/// <para>寻找可供强暴者强暴的潜在受害者。</para>
		/// <para>这使用加权随机算法，第一个结果未必是最佳选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="rapist">寻找合适强暴对象的小人。</param>
		/// <param name="candidates">候选受害者列表。</param>
		/// <param name="ignoreBleeding">是否忽略正在流血的受害者。</param>
		/// <param name="ignoreOrienation">是否禁用性向检查。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> FindVictims(
			Pawn rapist,
			IEnumerable<Pawn> candidates,
			bool ignoreBleeding = false,
			bool ignoreOrienation = false)
		{
			using (Seeded.ForHour(rapist))
				return RNG.FindVictims(rapist, candidates, ignoreBleeding, ignoreOrienation).ToList();
		}

		/// <summary>
		/// <para>尝试从给定候选列表中，为强暴者找到
		/// 一名可供强暴的受害者。</para>
		/// <para>这使用加权随机算法，可能不会选择最优选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="rapist">寻找合适强暴对象的小人。</param>
		/// <param name="candidates">候选受害者列表。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <param name="ignoreBleeding">是否忽略正在流血的受害者。</param>
		/// <param name="ignoreOrienation">是否禁用性向检查。</param>
		/// <returns>是否找到了结果。</returns>
		public static bool FindVictim(
			Pawn rapist,
			IEnumerable<Pawn> candidates,
			out AppraisalResult result,
			bool ignoreBleeding = false,
			bool ignoreOrienation = false)
		{
			using (Seeded.ForHour(rapist))
				return RNG.FindVictims(rapist, candidates, ignoreBleeding, ignoreOrienation).TryFirst(out result);
		}

		/// <summary>
		/// <para>为特定受害者寻找可以强暴其的潜在强暴者。便于
		/// 召集一群强暴者进行轮奸。</para>
		/// <para>这基本上与 <see cref="FindVictims" /> 相反。在
		/// 结果中，观察者是强暴者，而目标是给定的
		/// `victim`。</para>
		/// <para>这使用加权随机算法，第一个结果未必是最佳选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="victim">将被侵害的小人。</param>
		/// <param name="candidates">候选强暴者列表。</param>
		/// <param name="ignoreBleeding">是否忽略正在流血的受害者。</param>
		/// <param name="ignoreOrienation">是否禁用性向检查。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> FindRapists(
			Pawn victim,
			IEnumerable<Pawn> candidates,
			bool ignoreBleeding = false,
			bool ignoreOrienation = false)
		{
			using (Seeded.ForHour(victim))
				return RNG.FindRapists(victim, candidates, ignoreBleeding, ignoreOrienation).ToList();
		}

		/// <summary>
		/// <para>尝试从给定候选列表中，为特定受害者找到一名
		/// 可以强暴其的强暴者。</para>
		/// <para>这基本上与 <see cref="FindVictim" /> 相反。在
		/// 结果中，观察者是强暴者，而目标是给定的
		/// `victim`。</para>
		/// <para>这使用加权随机算法，可能不会选择最优选项，
		/// 但仍会更偏好高于平均水平的伴侣。</para>
		/// </summary>
		/// <param name="victim">将被侵害的小人。</param>
		/// <param name="candidates">候选强暴者列表。</param>
		/// <param name="result">用于存储结果的变量。</param>
		/// <param name="ignoreBleeding">是否忽略受害者的流血。</param>
		/// <param name="ignoreOrienation">是否禁用性向检查。</param>
		/// <returns>是否找到了结果。</returns>
		public static bool FindRapist(
			Pawn victim,
			IEnumerable<Pawn> candidates,
			out AppraisalResult result,
			bool ignoreBleeding = false,
			bool ignoreOrienation = false)
		{
			using (Seeded.ForHour(victim))
				return RNG.FindRapists(victim, candidates, ignoreBleeding, ignoreOrienation).TryFirst(out result);
		}

		/// <summary>
		/// 根据给定的参数结构体创建评估结果列表，
		/// 将 <see cref="AppraisalParams.Pawn" /> 视为观察者，
		/// 将 <see cref="AppraisalParams.Candidates" /> 列表视为目标。
		/// </summary>
		/// <param name="appraisalParams">评估参数。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> CandidatesAsTargets(in AppraisalParams appraisalParams)
		{
			if (appraisalParams.Candidates is not List<Pawn> candidateList)
				candidateList = appraisalParams.Candidates.ToList();

			var resultList = new List<AppraisalResult>(candidateList.Count);
			foreach (var candidate in candidateList)
				if (candidate != appraisalParams.Pawn)
					resultList.Add(new(
						appraisalParams.Pawn, appraisalParams.ObserverSettings,
						candidate, appraisalParams.TargetSettings
					));

			return resultList;
		}

		/// <summary>
		/// 根据给定的参数结构体创建评估结果列表，
		/// 将 <see cref="AppraisalParams.Pawn" /> 视为目标，
		/// 将 <see cref="AppraisalParams.Candidates" /> 列表视为观察者。
		/// </summary>
		/// <param name="appraisalParams">评估参数。</param>
		/// <returns>结果列表。</returns>
		public static List<AppraisalResult> CandidatesAsObservers(in AppraisalParams appraisalParams)
		{
			if (appraisalParams.Candidates is not List<Pawn> candidateList)
				candidateList = appraisalParams.Candidates.ToList();

			var resultList = new List<AppraisalResult>(candidateList.Count);
			foreach (var candidate in candidateList)
				if (candidate != appraisalParams.Pawn)
					resultList.Add(new(
						candidate, appraisalParams.ObserverSettings,
						appraisalParams.Pawn, appraisalParams.TargetSettings
					));

			return resultList;
		}

		/// <summary>
		/// <para>给定一组 `AppraisalResult`，按从最佳到最差的
		/// 顺序返回它们。</para>
		/// <para>与 <see cref="RNG.FindResults" /> 和 <see cref="RNG.FindBiasedResults" /> 不同，
		/// 这不会引入任何随机性。</para>
		/// </summary>
		/// <param name="results">要处理的结果。</param>
		/// <returns>结果的枚举。</returns>
		public static IEnumerable<AppraisalResult> FindBestResults(IEnumerable<AppraisalResult> results)
		{
			return results
				.Where(ValidForObserver)
				.Where(ValidForTarget)
				.OrderByDescending(GetWeight);
		}

		/// <summary>
		/// 筛选器：检查观察者对目标的吸引力是否达到
		/// 参与考虑所需的最低值。
		/// </summary>
		static bool ValidForObserver(AppraisalResult r) =>
			r.ObserverAttraction >= MinimumAttractionForSex;

		/// <summary>
		/// <para>筛选器：检查目标对观察者的吸引力是否达到
		/// 参与考虑所需的最低值。</para>
		/// <para>注意：在强暴中，目标对观察者的吸引力无关紧要。</para>
		/// </summary>
		static bool ValidForTarget(AppraisalResult r) =>
			r.ObserverSettings.Purpose == AttractionPurpose.ForRape || r.TargetAttraction >= MinimumAttractionForSex;

		/// <summary>
		/// <para>获取给定结果的权重。</para>
		/// <para>注意：在强暴中，目标对观察者的吸引力无关紧要。</para>
		/// </summary>
		static float GetWeight(AppraisalResult r) =>
			r.ObserverSettings.Purpose == AttractionPurpose.ForRape ? r.ObserverAttraction : r.CombinedAttraction;

		/// <summary>
		/// 可将这些添加到吸引力请求中，以便在特殊场景下
		/// 禁用某些标准行为。
		/// </summary>
		public static class PreferenceCustomizers
		{
			/// <summary>
			/// 自定义小人偏好，使性向被忽略。
			/// </summary>
			public static void IgnoreOrientation(ref AttractionRequest request)
			{
				request.RemovePreference(nameof(R_Orientation));
			}

			/// <summary>
			/// 自定义小人偏好，使其当前的关系被忽略。
			/// </summary>
			public static void IgnoreRelationships(ref AttractionRequest request)
			{
				request.RemovePreference(nameof(R_Homewrecking));
				request.RemovePreference(nameof(R_Relationship));
			}
		}

		/// <summary>
		/// 这些方法会使用随机数。对于响应玩家操作的方法，你应当使用
		/// 确定性种子（通过 <see cref="Seeded" />），或使用
		/// 多人游戏 API 的 `SyncMethod` 特性。
		/// </summary>
		public static class RNG
		{
			/// <summary>
			/// <para>给定一组 `AppraisalResult`，按加权随机
			/// 顺序返回它们。除吸引力值带来的权重外，选项之间
			/// 没有其他偏置，因此任何结果都可能排在第一位。</para>
			/// </summary>
			/// <param name="results">要处理的结果。</param>
			/// <returns>结果的枚举。</returns>
			public static IEnumerable<AppraisalResult> FindResults(IEnumerable<AppraisalResult> results)
			{
				var options = results
					.Where(ValidForObserver)
					.Where(ValidForTarget)
					.ToHashSet();

				while (options.Count > 0)
				{
					var nextOption = options.RandomElementByWeight(GetWeight);
					options.Remove(nextOption);
					yield return nextOption;
				}
			}

			/// <summary>
			/// <para>给定一组 `AppraisalResult`，按加权随机
			/// 顺序返回它们，并优先偏向高于平均水平的选项。</para>
			/// </summary>
			/// <param name="results">要处理的结果。</param>
			/// <returns>结果的枚举。</returns>
			public static IEnumerable<AppraisalResult> FindBiasedResults(IEnumerable<AppraisalResult> results)
			{
				var options = results
					.Where(ValidForObserver)
					.Where(ValidForTarget)
					.ToList();

				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindBiasedResults(): {options.Count} valid options");

				// 如果为空，`Average` 操作会失败。
				if (options.Count == 0) yield break;

				// 将它们拆分为高于平均和低于平均的两块。
				var averageWeight = options.Select(GetWeight).Average();
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" FindBiasedResults(): averageWeight is {averageWeight}");
				var splitOptions = options.ToLookup((r) => GetWeight(r) >= averageWeight);

				// 首先，优先考虑高于平均水平的选项。
				var aboveAverage = splitOptions[true].ToHashSet();
				while (aboveAverage.Count > 0)
				{
					var nextOption = aboveAverage.RandomElementByWeight(GetWeight);
					aboveAverage.Remove(nextOption);
					yield return nextOption;
				}

				// 如果执行到这里，说明消费者还想要更多选项，因此退回到
				// 低于平均水平的选项。
				var belowAverage = splitOptions[false].ToHashSet();
				while (belowAverage.Count > 0)
				{
					var nextOption = belowAverage.RandomElementByWeight(GetWeight);
					belowAverage.Remove(nextOption);
					yield return nextOption;
				}
			}

			/// <summary>
			/// <see cref="SexAppraiser.FindPartners" /> 的非种子实现。
			/// </summary>
			public static IEnumerable<AppraisalResult> FindPartners(
				Pawn observer,
				IEnumerable<Pawn> candidates)
			{
				// 观察者和候选者都使用默认的 `ForFucking`。
				var appraisalParams = new AppraisalParams(observer, candidates);

				return FindBiasedResults(CandidatesAsTargets(in appraisalParams));
			}

			/// <summary>
			/// <see cref="SexAppraiser.FindTricks" /> 的非种子实现。
			/// </summary>
			public static IEnumerable<AppraisalResult> FindTricks(
				Pawn whore,
				IEnumerable<Pawn> candidates)
			{
				var appraisalParams = new AppraisalParams(whore, candidates)
				{
					// 忽略性服务提供者的关系，以免影响
					// 其选择。
					ObserverSettings = new(
						Purpose: AttractionPurpose.ForFucking,
						PreQuirkModifier: PreferenceCustomizers.IgnoreRelationships
					),
					// 候选者仍将使用通常的 `ForFucking` 偏好。
				};

				return FindBiasedResults(CandidatesAsTargets(in appraisalParams));
			}

			/// <summary>
			/// <see cref="SexAppraiser.FindWhores" /> 的非种子实现。
			/// </summary>
			public static IEnumerable<AppraisalResult> FindWhores(
				Pawn client,
				IEnumerable<Pawn> candidates)
			{
				var appraisalParams = new AppraisalParams(client, candidates)
				{
					// 忽略性服务提供者的关系，以免影响
					// 其选择。
					ObserverSettings = new(
						Purpose: AttractionPurpose.ForFucking,
						PreQuirkModifier: PreferenceCustomizers.IgnoreRelationships
					),
					// 候选者仍将使用通常的 `ForFucking` 偏好。
				};

				return FindBiasedResults(CandidatesAsObservers(in appraisalParams));
			}

			/// <summary>
			/// <see cref="SexAppraiser.FindVictims" /> 的非种子实现。
			/// </summary>
			public static IEnumerable<AppraisalResult> FindVictims(
				Pawn rapist,
				IEnumerable<Pawn> candidates,
				bool ignoreBleeding = false,
				bool ignoreOrienation = false)
			{
				var appraisalParams = new AppraisalParams(rapist, candidates)
				{
					// 强暴者总得强暴。
					ObserverSettings = new(
						Purpose: AttractionPurpose.ForRape,
						PreQuirkModifier: ignoreOrienation ? PreferenceCustomizers.IgnoreOrientation : null,
						IgnoreBleeding: ignoreBleeding
					),
					// 选择时不会用到受害者的吸引力，不过
					// 消费者仍可请求为受害者计算吸引力。在这种
					// 情况下，我们会使用 `General` 用途。
					TargetSettings = new(AttractionPurpose.General)
				};

				return FindBiasedResults(CandidatesAsTargets(in appraisalParams));
			}

			/// <summary>
			/// <see cref="SexAppraiser.FindRapists" /> 的非种子实现。
			/// </summary>
			public static IEnumerable<AppraisalResult> FindRapists(
				Pawn victim,
				IEnumerable<Pawn> candidates,
				bool ignoreBleeding = false,
				bool ignoreOrienation = false)
			{
				var appraisalParams = new AppraisalParams(victim, candidates)
				{
					// 强暴者总得强暴。
					ObserverSettings = new(
						Purpose: AttractionPurpose.ForRape,
						PreQuirkModifier: ignoreOrienation ? PreferenceCustomizers.IgnoreOrientation : null,
						IgnoreBleeding: ignoreBleeding
					),
					// 选择时不会用到受害者的吸引力，不过
					// 消费者仍可请求为受害者计算吸引力。在这种
					// 情况下，我们会使用 `General` 用途。
					TargetSettings = new(AttractionPurpose.General)
				};

				return FindBiasedResults(CandidatesAsObservers(in appraisalParams));
			}
		}
	}
}
