using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 口渴/水袋自检：带 <c>-colonykit-thirst-test</c> 启动时跑一遍纯逻辑检查
	/// （Def、水袋加减与上限、食物补水、水袋喝水 Job、LordDuty 里的喝水/撤离节点）。
	/// 不改地图、不建建筑，几秒内结束，用来防止 XML 或设置被改坏。
	/// </summary>
	internal static class ThirstSelftest
	{
		public const string CommandLineKey = "-colonykit-thirst-test";

		private static bool ran;

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}
			ran = true;
			try
			{
				Run(game);
			}
			catch (System.Exception exception)
			{
				ModLog.Error("Hygiene", "thirst selftest crashed: " + exception);
			}
		}

		private static void Run(Game game)
		{
			Map map = Find.AnyPlayerHomeMap ?? game.Maps.FirstOrDefault();
			Pawn pawn = map?.mapPawns.FreeColonists.FirstOrDefault(p => p.RaceProps.Humanlike && !p.Downed);
			if (pawn == null)
			{
				ModLog.Info("Hygiene", "thirst selftest: pass=False reason=no colonist");
				return;
			}
			bool pass = true;
			pass &= DefDatabase<JobDef>.GetNamedSilentFail("CK_DrinkFromPouch") != null;
			pass &= DefDatabase<JobDef>.GetNamedSilentFail("CK_FillWaterPouch") != null;
			pass &= DefDatabase<JobDef>.GetNamedSilentFail("CK_BottleWaterToStockpile") != null;
			pass &= DefDatabase<WorkGiverDef>.GetNamedSilentFail("CK_BottleWater") != null;

			float start = WaterPouchUtility.GetWater(pawn);
			pass &= Mathf.Approximately(start, GameComponent_WaterPouches.StartWater);
			WaterPouchUtility.TakeWater(pawn, 5f);
			pass &= Mathf.Approximately(WaterPouchUtility.GetWater(pawn), start - 5f);
			WaterPouchUtility.AddWater(pawn, 10f, ContaminationLevel.Untreated);
			pass &= Mathf.Approximately(WaterPouchUtility.GetWater(pawn), start + 5f);
			pass &= WaterPouchUtility.GetQuality(pawn) == ContaminationLevel.Untreated;
			WaterPouchUtility.AddWater(pawn, 500f, ContaminationLevel.Contaminated);
			pass &= Mathf.Approximately(WaterPouchUtility.GetWater(pawn), GameComponent_WaterPouches.MaxWater);
			pass &= WaterPouchUtility.GetQuality(pawn) == ContaminationLevel.Contaminated;
			WaterPouchUtility.TakeWater(pawn, GameComponent_WaterPouches.MaxWater);
			WaterPouchUtility.AddWater(pawn, GameComponent_WaterPouches.StartWater, ContaminationLevel.Treated);

			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null)
			{
				ModLog.Info("Hygiene", "thirst selftest: pass=False reason=no thirst need");
				return;
			}
			float beforeThirst = 0.5f;
			thirst.CurLevel = beforeThirst;
			Thing meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
			float nutrition = meal.Ingested(pawn, 0.1f);
			float gained = thirst.CurLevel - beforeThirst;
			float expected = nutrition * HygieneSettings.Current.foodWaterFactor;
			bool foodOk = nutrition > 0f && Mathf.Abs(gained - expected) < 0.01f;
			pass &= foodOk;

			WaterPouchUtility.AddWater(pawn, 5f, ContaminationLevel.Treated);
			thirst.CurLevel = 0.2f;
			ThinkResult drinkResult = new JobGiver_DrinkWater().TryIssueJobPackage(pawn, default(JobIssueParams));
			bool pouchJob = drinkResult.Job?.def?.defName == "CK_DrinkFromPouch";
			pass &= pouchJob;

			ThinkTreeDef lordDuty = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("LordDuty");
			bool lordDrink = lordDuty?.thinkRoot?.ThisAndChildrenRecursive
				.Any(node => node is JobGiver_DrinkWater) == true;
			bool lordExit = lordDuty?.thinkRoot?.ThisAndChildrenRecursive
				.Any(node => node is ThinkNode_ConditionalThirstyNoWater) == true;
			pass &= lordDrink && lordExit;

			// 把自检改过的状态还回去，避免影响同一进程里的取水全流程自检。
			thirst.CurLevel = 0.9f;
			WaterPouchUtility.TakeWater(pawn, WaterPouchUtility.MaxWater);
			WaterPouchUtility.AddWater(pawn, GameComponent_WaterPouches.StartWater, ContaminationLevel.Treated);

			ModLog.Info(
				"Hygiene",
				$"thirst selftest: pass={pass} pouch={start}->{WaterPouchUtility.GetWater(pawn)} " +
				$"foodGain={gained:F3} expected={expected:F3} pouchJob={pouchJob} " +
				$"lordDrink={lordDrink} lordExit={lordExit}");
		}
	}
}
