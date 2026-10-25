#nullable disable

using Verse;
using Verse.AI;
using RimWorld;

namespace rjw.Modules.Attraction.StandardPreferences
{
	/// <summary>
	/// 对裸体目标为裸体主义者和动物施加加成。
	/// </summary>
	public sealed class P_Nudity : PredicatedAttractionPreference
	{
		[StandardPreference]
		public static void ApplyTo(ref AttractionRequest request)
		{
			// 只在目标未死亡时添加。
			if (request.Target.Dead) return;

			request.SetPreference(new P_Nudity());
		}

		private P_Nudity() : base(
			AttractionMode.Physical,
			nameof(P_Nudity),
			FactorOperation.Add,
			IsConsideredNude,
			whenTrue: FixedValue(0.2f))
		{ }

		private static bool IsConsideredNude(ref AttractionRequest request)
		{
			var pawn = request.Pawn;
			var target = request.Target;

			return IsAnimalOrNudist(pawn) && IsTargetNude(target) && request.Purpose.IsForSex();
		}

		private static bool IsAnimalOrNudist(Pawn pawn)
		{
			if (pawn.IsAnimal()) return true;
			if (pawn.story?.traits?.HasTrait(TraitDefOf.Nudist) is true) return true;
			if (ModsConfig.IdeologyActive && pawn.Ideo?.IdeoPrefersNudity() is true) return true;
			return false;
		}

		private static bool IsTargetNude(Pawn pawn)
		{
			if (xxx.is_human(pawn))
			{
				return pawn.apparel?.PsychologicallyNude ?? false;
			}

			if (xxx.is_animal(pawn))
			{
				return true;
			}

			return false;
		}
	}
}
