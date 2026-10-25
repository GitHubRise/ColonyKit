using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	internal static class SanitationUtil
	{
		public static void GiveNastyGutBug(Pawn pawn, ContaminationLevel level)
		{
			// 肠道疾病/污水系统后续迁移；保留 DBH 调用点。
		}

		public static void WaterTempCheck(Pawn pawn, bool cold, bool bath)
		{
			if (pawn?.needs?.mood?.thoughts?.memories == null)
			{
				return;
			}
			ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail(
				cold ? "CK_ShowerCold" : "CK_ShowerHot");
			if (thought != null)
			{
				pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
			}
		}

		public static void ContaminationCheckWater(Pawn pawn, ContaminationLevel level)
		{
			if (pawn?.health == null || level < ContaminationLevel.Contaminated)
			{
				return;
			}
			HediffDef badHygiene = HygieneDefOf.CK_BadHygiene;
			if (badHygiene != null)
			{
				HealthUtility.AdjustSeverity(pawn, badHygiene, 0.15f);
			}
		}

		public static void ApplyBathroomThought(Pawn pawn, Building_AssignableFixture fixture)
		{
			// 浴室隐私/分配系统后续迁移。
		}

		public static void CheckForBlockage(Building building)
		{
			building?.GetComp<CompBlockage>()?.CheckForBlockage();
		}
	}

	internal static class PrivacyUtil
	{
		public static bool BathingPrivacyLOS(Pawn source, float radius = 6f)
		{
			if (source == null || !source.Spawned || source.Map == null)
			{
				return false;
			}
			foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
				source.Position, source.Map, radius, true))
			{
				if (thing is Pawn other && other != source && source.CaresIfSeenBathingBy(other))
				{
					ThoughtDef privacy = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_WashPrivacy");
					if (privacy != null)
					{
						source.needs?.mood?.thoughts?.memories?.TryGainMemory(privacy);
					}
					return true;
				}
			}
			return false;
		}

		public static bool CaresIfSeenBathingBy(this Pawn source, Pawn target)
		{
			if (target == null || target.RaceProps.Animal || target.RaceProps.IsMechanoid)
			{
				return false;
			}
			if (LovePartnerRelationUtility.LovePartnerRelationExists(source, target))
			{
				return false;
			}
			if (source.Map == null || !source.Map.IsPlayerHome)
			{
				return false;
			}
			if (!GenSight.LineOfSight(source.Position, target.Position, source.Map))
			{
				return false;
			}
			return source.gender != target.gender;
		}
	}

	internal static class ShowerCleanup
	{
		public static void ClearCumAround(Pawn pawn, IntVec3 cell)
		{
			// RJW 污渍清理后续接。
		}

		public static void TryQueueRoomCleaning(Pawn pawn)
		{
		}
	}
}
