#nullable disable

using Verse;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 处理小人的恋爱关系。
	/// </summary>
	public sealed class R_Relationship : PredicatedAttractionPreference
	{
		const float MinimumCheatFactor = 0.25f;

		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 被某些设置禁用。
			if (RJWSettings.WildMode || RJWSettings.HippieMode) return;
			// 精神变态不在意恋爱关系的细微之处。
			if (xxx.is_psychopath(request.Pawn)) return;

			request.SetPreference(new R_Relationship());
		}

		private R_Relationship() : base(
			AttractionMode.Romantic,
			nameof(R_Relationship),
			FactorOperation.Multiply,
			IsInRelationship,
			whenTrue: WhenInRelationship,
			whenFalse: WhenNotInRelationship)
		{ }

		private static bool IsInRelationship(ref AttractionRequest request) =>
			request.HasLoveRelationWithTarget;

		private static float WhenInRelationship(ref AttractionRequest request, float factor)
		{
			var pawn = request.Pawn;
			if (request.Purpose.IsForRape() && pawn.story?.traits is { } traits)
			{
				return request.Target.gender switch
				{
					// 让我们检查配偶虐待相关的特质。
					Gender.Male when traits.HasTrait(TraitDefOf.DislikesMen) => 1.5f,
					Gender.Female when traits.HasTrait(TraitDefOf.DislikesWomen) => 1.5f,
					// 粗暴的小人更容易以错误的方式（和错误的位置）惹恼别人。
					_ when traits.HasTrait(TraitDefOf.Abrasive) => 1f,
					// 否则，该小人更可能把自己的愤怒之爱交给别人。
					_ => 0.5f
				};
			}
			return 1.5f;
		}

		private static float WhenNotInRelationship(ref AttractionRequest request, float factor)
		{
			var pawn = request.Pawn;
			// 目前动物在这里没有任何特殊处理，
			// 但未来谁知道呢。
			if (pawn.IsAnimal()) return 1f;
			// 小人总是可以呆看，而强暴时社交后果被忽略。
			if (request.Purpose is not AttractionPurpose.ForFucking) return 1f;
			// 该小人可能不介意与他人随意发生性行为。
			if (AttractionUtility.CanFoolAround(ref request)) return 1f;

			// 花痴可以配置为在性方面受挫时出轨。
			if (RJWHookupSettings.NymphosCanCheat)
				if (xxx.is_nympho(pawn) && xxx.is_frustrated(pawn))
					return 1f;

			// 如果小人喝醉了，他们可能根据严重度出轨。
			if (pawn.HasBeerGoggles(out var severity))
				return GenMath.LerpDouble(0f, 1f, MinimumCheatFactor, 1f, severity);

			// 否则，施加显著的惩罚。
			return MinimumCheatFactor;
		}
	}
}
