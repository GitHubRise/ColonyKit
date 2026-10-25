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
			// 堵塞系统后续迁移。
		}
	}

	internal static class PrivacyUtil
	{
		public static void BathingPrivacyLOS(Pawn pawn)
		{
			// 隐私系统后续迁移。
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
