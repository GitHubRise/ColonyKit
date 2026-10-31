using ColonyKit.Modules.Robotics;
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
			HygieneSettings hygieneSettings = HygieneSettings.Current;
			ModLog.Info(
				"Hygiene",
				"module loaded (DBH shower migration); " +
				$"DBH active={hygieneSettings.DbhDetected}, " +
				$"policy={hygieneSettings.dbhPolicy}, run={hygieneSettings.ShouldRunHygiene}");

			ThingDef shower = DefDatabase<ThingDef>.GetNamedSilentFail("CK_Shower");
			bool showerPower = false;
			if (shower?.comps != null)
			{
				foreach (CompProperties comp in shower.comps)
				{
					if (comp is CompProperties_Power)
					{
						showerPower = true;
						break;
					}
				}
			}
			ModLog.Info(
				"Hygiene",
				$"shower size={(shower == null ? "missing" : shower.size.ToString())}, power={showerPower}, " +
				$"wallAttached={(shower?.building?.isAttachment ?? false)}, placeWorkers={shower?.PlaceWorkers?.Count ?? 0}; " +
				$"water defs: pipe={DefExists("CK_DbhWaterPipe")}, tank={DefExists("CK_DbhWaterTank")}, " +
				$"giant={DefExists("CK_DbhWaterTankGiant")}, pump={DefExists("CK_DbhWaterPump")}, " +
				$"well={DefExists("CK_DbhWaterWell")}, heater={DefExists("CK_DbhWaterHeater")}; " +
				$"waterPoint={DefExists("CK_WaterPoint")}, bucket={DefExists("CK_WaterBucket")}, ice={DefExists("CK_IceBlock")}, " +
				$"fillPointJob={JobExists("CK_FillWaterPoint")}, drinkCarriedJob={JobExists("CK_DrinkFromCarried")}, " +
				$"fillPointWorkGiver={WorkGiverExists("CK_FillWaterPoint")}; " +
				$"hygiene menu={DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("CK_Hygiene") != null}; " +
				$"latrine={DefExists("CK_PitLatrine")}, emptyLatrineJob={DefDatabase<JobDef>.GetNamedSilentFail("CK_EmptyLatrine") != null}");
			HygieneUtility.ApplyWaterRecipeSettings();
		}

		private static bool DefExists(string defName)
		{
			return DefDatabase<ThingDef>.GetNamedSilentFail(defName) != null;
		}

		private static bool JobExists(string defName)
		{
			return DefDatabase<JobDef>.GetNamedSilentFail(defName) != null;
		}

		private static bool WorkGiverExists(string defName)
		{
			return DefDatabase<WorkGiverDef>.GetNamedSilentFail(defName) != null;
		}

		public override void ResetRuntimeState()
		{
			// 模块开关/策略变化后立即把现有需求增删到正确状态。
			HygieneUtility.RefreshAllPawns();
		}

		public override void OnGameLoaded(Game game)
		{
			if (game == null)
			{
				return;
			}
			// 让存档里已有的需求按当前策略/豁免重新评估；新需求由 ShouldHaveNeed 补丁负责。
			HygieneUtility.RefreshAllPawns();
			Agriculture.AgricultureSelftest.RunIfRequested(game);
			WaterSelftest.RunIfRequested(game);
			int humanlikes = 0;
			int withNeed = 0;
			int withBladder = 0;
			int robots = 0;
			int robotThirst = 0;
			int robotBladder = 0;
			foreach (Map map in game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (NannyBotUtility.IsNannyBot(pawn))
					{
						HygieneUtility.EnsureRobotNeeds(pawn);
						robots++;
						if (pawn.needs?.TryGetNeed<Need_Thirst>() != null)
						{
							robotThirst++;
						}
						if (pawn.needs?.TryGetNeed<Need_Bladder>() != null)
						{
							robotBladder++;
						}
						continue;
					}
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
			ModLog.Info(
				"Hygiene",
				$"need injection check: humanlikes={humanlikes}, withHygiene={withNeed}, withBladder={withBladder}; " +
				$"robots={robots}, robotThirst={robotThirst}, robotBladder={robotBladder}; " +
				$"policy={HygieneSettings.Current.dbhPolicy}, run={HygieneSettings.Current.ShouldRunHygiene}");
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
				|| (nd.defName != "CK_Hygiene" && nd.defName != "CK_Bladder"
					&& nd.defName != "CK_Thirst"))
			{
				return;
			}

			Pawn pawn = PawnField(__instance);
			if (pawn != null && HygieneUtility.ShouldInject(pawn, nd))
			{
				__result = true;
			}
		}
	}
}
