using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 口渴/水袋自检：带 <c>-colonykit-thirst-test</c> 启动时跑一遍纯逻辑检查
	/// （Def、水袋加减与上限、食物补水、水袋喝水 Job、LordDuty 里的喝水/撤离节点，
	/// 以及每升解渴、动物口渴速度、草食/肉食动物吃对东西的固定补水）。
	/// 会临时刷一只牛和一只座狼测速，测完立刻销毁，不建建筑。
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
			bool waterNearby = ClosestSanitation.FindBestDrink(pawn, false, 30f, true).IsValid;
			ThinkResult drinkResult = new JobGiver_DrinkWater().TryIssueJobPackage(pawn, default(JobIssueParams));
			bool pouchJob = drinkResult.Job?.def?.defName == "CK_DrinkFromPouch";
			// 附近有能喝的水时应该先去喝水，没水才喝水袋——两种都算对。
			bool drinkOrderOk = waterNearby ? !pouchJob : pouchJob;
			pass &= drinkOrderOk;

			// 地上的瓶装水：非敌对的人形口渴时会捡起来喝（新加的「地上有水也会过去喝」）。
			ThingDef mineralDef = MineralWaterUtility.Def;
			Thing groundBottle = mineralDef == null ? null : ThingMaker.MakeThing(mineralDef);
			bool groundBottleFirst = false;
			if (groundBottle != null)
			{
				GenSpawn.Spawn(groundBottle, CellFinder.RandomClosewalkCellNear(pawn.Position, map, 2), map);
				thirst.CurLevel = 0.2f;
				ThinkResult groundResult = new JobGiver_DrinkWater().TryIssueJobPackage(pawn, default(JobIssueParams));
				Job groundJob = groundResult.Job;
				groundBottleFirst = groundJob != null
					&& MineralWaterUtility.FindSpawned(pawn, 30f) == groundBottle
					&& (groundJob.targetA.Thing == groundBottle
						|| TargetDistance(pawn, groundJob.targetA) <= groundBottle.Position.DistanceTo(pawn.Position) + 0.01f);
				groundBottle.Destroy(DestroyMode.Vanish);
			}
			pass &= groundBottleFirst;

			// 每升解渴：一瓶 1 L 现在按设置值折算（默认 0.65）。
			HygieneSettings hygiene = HygieneSettings.Current;
			float perLiter = MineralWaterUtility.ThirstPerLiter;
			float bottleGain = mineralDef == null
				? perLiter
				: MineralWaterUtility.ThirstFor(ThingMaker.MakeThing(mineralDef));
			bool perLiterOk = Mathf.Abs(perLiter - hygiene.thirstPerLiter) < 0.0001f
				&& Mathf.Abs(bottleGain - perLiter) < 0.0001f;
			pass &= perLiterOk;

			// 动物：口渴速度倍率 + 草食/肉食吃对东西的固定补水。
			float animalRatio = -1f;
			float herbivoreGain = -1f;
			float carnivoreGain = -1f;
			Pawn muffalo = SpawnTestAnimal(map, "Muffalo", pawn);
			Pawn warg = SpawnTestAnimal(map, "Warg", pawn);
			Need_Thirst muffaloThirst = muffalo?.needs?.TryGetNeed<Need_Thirst>();
			Need_Thirst wargThirst = warg?.needs?.TryGetNeed<Need_Thirst>();
			bool animalNeedOk = muffaloThirst != null && wargThirst != null;
			pass &= animalNeedOk;
			if (muffaloThirst != null)
			{
				float savedRate = hygiene.animalThirstRate;
				hygiene.animalThirstRate = 1f;
				muffaloThirst.CurLevel = 1f;
				muffaloThirst.NeedInterval();
				float fullDrop = 1f - muffaloThirst.CurLevel;
				hygiene.animalThirstRate = 0.5f;
				muffaloThirst.CurLevel = 1f;
				muffaloThirst.NeedInterval();
				float halfDrop = 1f - muffaloThirst.CurLevel;
				hygiene.animalThirstRate = savedRate;
				if (fullDrop > 0.000001f)
				{
					animalRatio = halfDrop / fullDrop;
					pass &= Mathf.Abs(animalRatio - 0.5f) < 0.02f;
				}

				// 草在分类上必须算「植物类食物」，否则吃草的野牛拿不到固定补水。
				bool grassIsPlant = Patch_Thing_Ingested_FoodWater.IsPlantFood(
					ThingMaker.MakeThing(ThingDefOf.Plant_Grass));
				pass &= grassIsPlant;
				Thing hay = ThingMaker.MakeThing(ThingDefOf.Hay);
				muffaloThirst.CurLevel = 0.5f;
				hay.Ingested(muffalo, 0.15f);
				herbivoreGain = muffaloThirst.CurLevel - 0.5f;
				pass &= Mathf.Abs(herbivoreGain - hygiene.herbivorePlantThirst) < 0.005f;
			}
			if (wargThirst != null)
			{
				Thing meat = ThingMaker.MakeThing(FindMeatDef());
				wargThirst.CurLevel = 0.5f;
				meat.Ingested(warg, 0.2f);
				carnivoreGain = wargThirst.CurLevel - 0.5f;
				pass &= Mathf.Abs(carnivoreGain - hygiene.carnivoreMeatThirst) < 0.005f;
			}
			muffalo?.Destroy(DestroyMode.Vanish);
			warg?.Destroy(DestroyMode.Vanish);

			// 囚犯/访客/商队/敌人都自带 30 L；口渴时能自己喝水（附近有水就去喝，没水喝水袋）。
			float foreignWater = -1f;
			bool foreignOwnJob = false;
			Pawn foreign = SpawnForeignVisitor(map, pawn);
			if (foreign != null)
			{
				foreignWater = WaterPouchUtility.GetWater(foreign);
				Need_Thirst foreignThirst = foreign.needs?.TryGetNeed<Need_Thirst>();
				if (foreignThirst != null)
				{
					foreignThirst.CurLevel = 0.2f;
					ThinkResult foreignResult = new JobGiver_DrinkWater()
						.TryIssueJobPackage(foreign, default(JobIssueParams));
					string def = foreignResult.Job?.def?.defName;
					foreignOwnJob = def == "CK_DrinkFromPouch"
						|| def == "CK_DrinkFromBasin"
						|| def == "CK_DrinkFromGround"
						|| def == "CK_DrinkFromCarried"
						|| foreignResult.Job?.def == JobDefOf.Ingest;
				}
				foreign.Destroy(DestroyMode.Vanish);
			}
			pass &= foreignWater >= 1f && foreignOwnJob;

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
				$"waterNearby={waterNearby} drinkOrderOk={drinkOrderOk} groundBottleFirst={groundBottleFirst} " +
				$"perLiter={perLiter:F2} bottle={bottleGain:F2} animalRate={hygiene.animalThirstRate:F2} " +
				$"animalNeed={animalNeedOk} animalRatio={animalRatio:F2} " +
				$"herbivoreGain={herbivoreGain:F2} carnivoreGain={carnivoreGain:F2} " +
				$"foreignWater={foreignWater:F1} foreignOwnJob={foreignOwnJob} " +
				$"lordDrink={lordDrink} lordExit={lordExit}");
		}

		/// <summary>目标（物品或格子）到 pawn 的距离。</summary>
		private static float TargetDistance(Pawn pawn, LocalTargetInfo target)
		{
			IntVec3 cell = target.HasThing ? target.Thing.Position : target.Cell;
			return cell.DistanceTo(pawn.Position);
		}

		/// <summary>临时刷一只动物测口渴/吃食，测完由调用方销毁。</summary>
		private static Pawn SpawnTestAnimal(Map map, string kindDefName, Pawn near)
		{
			PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);
			if (map == null || kind == null || near == null)
			{
				return null;
			}
			Pawn animal = PawnGenerator.GeneratePawn(kind, null);
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(near.Position, map, 4);
			GenSpawn.Spawn(animal, cell, map);
			animal.needs?.AddOrRemoveNeedsAsAppropriate();
			return animal;
		}

		/// <summary>刷一个非玩家派系的人形（访客/商队/敌人），测「口渴先喝自己水袋」。</summary>
		private static Pawn SpawnForeignVisitor(Map map, Pawn near)
		{
			if (map == null || near == null)
			{
				return null;
			}
			Faction faction = Find.FactionManager.AllFactions
				.FirstOrDefault(f => f != null && !f.IsPlayer && f.def != null && f.def.humanlikeFaction);
			if (faction == null)
			{
				return null;
			}
			Pawn visitor = PawnGenerator.GeneratePawn(PawnKindDefOf.Villager, faction);
			IntVec3 cell = CellFinder.RandomClosewalkCellNear(near.Position, map, 4);
			GenSpawn.Spawn(visitor, cell, map);
			visitor.needs?.AddOrRemoveNeedsAsAppropriate();
			return visitor;
		}

		/// <summary>随便找一种生肉 def，用来测肉食动物吃肉补水。</summary>
		private static ThingDef FindMeatDef()
		{
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
			{
				if (def.category == ThingCategory.Item
					&& def.ingestible != null
					&& (def.ingestible.foodType & FoodTypeFlags.Meat) != 0)
				{
					return def;
				}
			}
			return ThingDefOf.Meat_Human;
		}
	}
}
