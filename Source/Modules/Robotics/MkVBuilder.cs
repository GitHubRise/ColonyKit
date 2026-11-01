using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 自主建造：地图上缺研究台、缺做饭的地方时，自己挑地方下蓝图，木料不够就先就近标几棵树去砍，
	/// 剩下的交给「建造」家务（搬材料 → 盖框架）。只盖最基础的两样，不铺房间、不抢玩家的规划。
	/// </summary>
	internal static class MkVBuilder
	{
		public const string ResearchBenchDefName = "SimpleResearchBench";

		public const string CampfireDefName = "Campfire";

		private const int BenchSteelCost = 25;

		private const int BenchWoodCost = 75;

		private const int CampfireWoodCost = 20;

		private const int TreeDesignationLimit = 6;

		/// <summary>地图上有没有可用的研究台（蓝图/框架不算）。</summary>
		public static bool HasResearchBench(Map map)
		{
			if (map?.listerBuildings == null)
			{
				return false;
			}
			foreach (Building building in map.listerBuildings.allBuildingsColonist)
			{
				if (building is Building_ResearchBench)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>地图上有没有能做饭的建筑（篝火、炉灶都算）。</summary>
		public static bool HasKitchen(Map map)
		{
			if (map?.listerBuildings == null)
			{
				return false;
			}
			foreach (Building building in map.listerBuildings.allBuildingsColonist)
			{
				if (building.def?.building != null && building.def.building.isMealSource)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>给 AI 候选用的描述；没有缺的东西时返回 null。</summary>
		public static string DescribeMissing(Pawn pawn)
		{
			Map map = pawn?.Map;
			if (map == null || !NannyBotUtility.Settings.construct)
			{
				return null;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp != null && comp.GetEffectivePriority(RobotChore.Construct) <= 0)
			{
				return null;
			}
			if (!HasResearchBench(map) && !HasPending(map, ResearchBenchDefName))
			{
				return "在基地旁盖一座研究台（没研究台就没法研究）";
			}
			if (!HasKitchen(map) && !HasPending(map, CampfireDefName))
			{
				return "搭一座篝火当厨房（现在没有能做饭的地方）";
			}
			return null;
		}

		/// <summary>真的去下蓝图；成功时给出对应的搬材料 Job（材料已经在附近时）。</summary>
		public static bool TryBuildMissing(Pawn pawn, out Job job)
		{
			job = null;
			Map map = pawn?.Map;
			if (map == null || !NannyBotUtility.Settings.construct
				|| !NannyBotUtility.CanWork(pawn))
			{
				return false;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp != null && comp.GetEffectivePriority(RobotChore.Construct) <= 0)
			{
				// 这台机器人把「建造」优先级设成了 0：只下蓝图不派人手也没用，干脆不动。
				return false;
			}

			if (!HasResearchBench(map) && !HasPending(map, ResearchBenchDefName))
			{
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ResearchBenchDefName);
				if (def != null
					&& CountMaterial(map, ThingDefOf.Steel) >= BenchSteelCost
					&& EnsureWood(pawn, BenchWoodCost))
				{
					Blueprint_Build blueprint = Place(pawn, def, ThingDefOf.WoodLog);
					if (blueprint != null)
					{
						job = ConstructJobFor(pawn, blueprint);
						ModLog.Info("Robotics", "Mk-V " + pawn.LabelShort + " 在基地旁下了研究台蓝图。");
						return true;
					}
				}
			}

			if (!HasKitchen(map) && !HasPending(map, CampfireDefName))
			{
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(CampfireDefName);
				if (def != null && EnsureWood(pawn, CampfireWoodCost))
				{
					Blueprint_Build blueprint = Place(pawn, def, null);
					if (blueprint != null)
					{
						job = ConstructJobFor(pawn, blueprint);
						ModLog.Info("Robotics", "Mk-V " + pawn.LabelShort + " 搭了一座篝火的蓝图。");
						return true;
					}
				}
			}
			return false;
		}

		private static Job ConstructJobFor(Pawn pawn, Blueprint_Build blueprint)
		{
			WorkGiver_ConstructDeliverResourcesToBlueprints giver = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("ConstructDeliverResourcesToBlueprints")?.Worker
				as WorkGiver_ConstructDeliverResourcesToBlueprints;
			if (giver == null || !giver.HasJobOnThing(pawn, blueprint, true))
			{
				return null;
			}
			return giver.JobOnThing(pawn, blueprint, true);
		}

		private static Blueprint_Build Place(Pawn pawn, ThingDef def, ThingDef stuff)
		{
			IntVec3 cell = FindPlacement(pawn, def);
			if (!cell.IsValid)
			{
				return null;
			}
			return GenConstruct.PlaceBlueprintForBuild(
				def, cell, pawn.Map, Rot4.North, Faction.OfPlayer, stuff);
		}

		/// <summary>仅供 -colonykit-nannybot-test 使用：看看现在能不能给这个 Def 找到落脚点。</summary>
		internal static IntVec3 FindPlacementForTest(Pawn pawn, ThingDef def)
		{
			return FindPlacement(pawn, def);
		}

		private static IntVec3 FindPlacement(Pawn pawn, ThingDef def)
		{
			Map map = pawn.Map;
			IntVec3 center = BaseCenter(map, pawn.Position);
			IntVec3 best = IntVec3.Invalid;
			float bestDistance = float.MaxValue;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 26f, true))
			{
				if (!CanPlace(pawn, cell, def))
				{
					continue;
				}
				float distance = cell.DistanceToSquared(pawn.Position);
				if (distance < bestDistance)
				{
					best = cell;
					bestDistance = distance;
				}
			}
			return best;
		}

		private static bool CanPlace(Pawn pawn, IntVec3 cell, ThingDef def)
		{
			Map map = pawn.Map;
			CellRect rect = GenAdj.OccupiedRect(cell, Rot4.North, def.Size);
			if (!rect.InBounds(map))
			{
				return false;
			}
			foreach (IntVec3 c in rect)
			{
				if (!c.Standable(map) || c.Fogged(map) || c.GetEdifice(map) != null)
				{
					return false;
				}
				if (c.GetFirstPawn(map) != null || c.IsForbidden(pawn))
				{
					return false;
				}
				if (FireNear(c, map, 1.5f))
				{
					return false;
				}
			}
			return pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly);
		}

		/// <summary>木料够不够；不够就顺手标几棵树（交给「种植/砍伐」家务去砍），这轮先不下蓝图。</summary>
		private static bool EnsureWood(Pawn pawn, int needed)
		{
			Map map = pawn.Map;
			if (CountMaterial(map, ThingDefOf.WoodLog) >= needed)
			{
				return true;
			}
			DesignateTrees(pawn);
			return false;
		}

		/// <summary>仓库里的 + 散在地上的都算（原版 resourceCounter 只统计仓库格）。</summary>
		private static int CountMaterial(Map map, ThingDef def)
		{
			if (map == null || def == null)
			{
				return 0;
			}
			int count = map.resourceCounter.GetCount(def);
			foreach (Thing thing in map.listerThings.ThingsOfDef(def))
			{
				if (thing != null && thing.Spawned && !thing.Destroyed && thing.stackCount > 0)
				{
					count += thing.stackCount;
				}
			}
			return count;
		}

		private static void DesignateTrees(Pawn pawn)
		{
			Map map = pawn.Map;
			int count = 0;
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Plant))
			{
				if (!(thing is Plant plant) || plant.Destroyed || !plant.Spawned
					|| !plant.def.plant.IsTree
					|| plant.def.plant.harvestedThingDef != ThingDefOf.WoodLog)
				{
					continue;
				}
				if (map.designationManager.DesignationOn(plant, DesignationDefOf.CutPlant) != null)
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, plant.Position)
					|| !pawn.CanReach(plant, PathEndMode.Touch, Danger.Deadly))
				{
					continue;
				}
				map.designationManager.AddDesignation(
					new Designation(plant, DesignationDefOf.CutPlant));
				count++;
				if (count >= TreeDesignationLimit)
				{
					break;
				}
			}
		}

		private static bool HasPending(Map map, string defName)
		{
			if (map?.listerThings == null || string.IsNullOrEmpty(defName))
			{
				return false;
			}
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
			{
				if (thing?.def?.entityDefToBuild?.defName == defName)
				{
					return true;
				}
			}
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
			{
				if (thing?.def?.entityDefToBuild?.defName == defName)
				{
					return true;
				}
			}
			return false;
		}

		private static IntVec3 BaseCenter(Map map, IntVec3 fallback)
		{
			if (map?.areaManager?.Home == null)
			{
				return fallback;
			}
			long x = 0;
			long z = 0;
			int count = 0;
			foreach (IntVec3 cell in map.areaManager.Home.ActiveCells)
			{
				x += cell.x;
				z += cell.z;
				count++;
				if (count >= 400)
				{
					break;
				}
			}
			if (count == 0)
			{
				return fallback;
			}
			return new IntVec3((int)(x / count), 0, (int)(z / count));
		}

		private static bool FireNear(IntVec3 cell, Map map, float radius)
		{
			if (map?.listerThings == null)
			{
				return false;
			}
			foreach (Thing fire in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
			{
				if (fire != null && fire.Spawned && !fire.Destroyed
					&& fire.Position.InHorDistOf(cell, radius))
				{
					return true;
				}
			}
			return false;
		}
	}
}
