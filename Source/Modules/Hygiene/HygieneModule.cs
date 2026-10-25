using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>淋浴/卫生模块：需求、淋浴建筑与自动洗澡 Job。</summary>
	public sealed class HygieneModule : ColonyKitModule
	{
		public override string Id => "hygiene";

		public override string DisplayNameKey => "ColonyKit.Module.Hygiene.Name";

		public override string DescriptionKey => "ColonyKit.Module.Hygiene.Desc";

		public override int InitOrder => 100;

		public override void OnDefsLoaded()
		{
			ModLog.Info(
				"Hygiene",
				"module loaded (DBH shower migration); " +
				$"DBH active={ModsConfig.IsActive("dubwise.dubsbadhygiene")}");

			ThingDef shower = DefDatabase<ThingDef>.GetNamedSilentFail("CK_Shower");
			ModLog.Info(
				"Hygiene",
				$"shower size={(shower == null ? "missing" : shower.size.ToString())}; " +
				$"water defs: pipe={DefExists("CK_DbhWaterPipe")}, tank={DefExists("CK_DbhWaterTank")}, " +
				$"giant={DefExists("CK_DbhWaterTankGiant")}, pump={DefExists("CK_DbhWaterPump")}, " +
				$"well={DefExists("CK_DbhWaterWell")}, heater={DefExists("CK_DbhWaterHeater")}; " +
				$"hygiene menu={DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("CK_Hygiene") != null}");
		}

		private static bool DefExists(string defName)
		{
			return DefDatabase<ThingDef>.GetNamedSilentFail(defName) != null;
		}

		public override void OnGameLoaded(Game game)
		{
			if (game == null)
			{
				return;
			}
			int humanlikes = 0;
			int withNeed = 0;
			int withBladder = 0;
			foreach (Map map in game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
					{
						continue;
					}
					humanlikes++;
					if (HygieneUtility.EnsureNeed(pawn) != null)
					{
						withNeed++;
					}
					if (HygieneUtility.EnsureBladder(pawn) != null)
					{
						withBladder++;
					}
				}
			}
			ModLog.Info("Hygiene", $"need injection check: humanlikes={humanlikes}, withHygiene={withNeed}, withBladder={withBladder}");
		}
	}

	[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
	internal static class Patch_NeedsTracker_Hygiene
	{
		private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> PawnField =
			AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");

		private static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
		{
			if (__result || nd == null
				|| (nd.defName != "CK_Hygiene" && nd.defName != "CK_Bladder"))
			{
				return;
			}
			if (!ModuleRegistry.IsEnabled("hygiene")
				|| ModsConfig.IsActive("dubwise.dubsbadhygiene"))
			{
				return;
			}

			Pawn pawn = PawnField(__instance);
			if (pawn != null
				&& pawn.RaceProps.Humanlike
				&& !pawn.RaceProps.IsMechanoid)
			{
				__result = true;
			}
		}
	}
}
