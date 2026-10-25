using System;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 开发用探针：启动参数带 <c>-colonykit-nannybot-test</c> 时，在新地图上放一座充电桩，
	/// 让桩组装一台机器人，并把种族/需求/工作能力写进 Player.log。
	/// 正常游戏不会触发。
	/// </summary>
	internal static class NannyBotSelftest
	{
		public const string CommandLineKey = "-colonykit-nannybot-test";

		private static bool ran;

		private static IntVec3 FindFreeRect(Map map, IntVec3 center, int radius, int width, int height)
		{
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dz = -radius; dz <= radius; dz++)
				{
					IntVec3 root = center + new IntVec3(dx, 0, dz);
					CellRect rect = new CellRect(root.x, root.z, width, height);
					if (rect.Contains(center))
					{
						continue;
					}
					bool free = true;
					foreach (IntVec3 cell in rect.Cells)
					{
						if (!cell.InBounds(map) || !cell.Standable(map)
							|| cell.GetEdifice(map) != null)
						{
							free = false;
							break;
						}
					}
					if (free)
					{
						return root;
					}
				}
			}
			return IntVec3.Invalid;
		}

		public static void RunIfRequested(Game game)
		{
			if (ran || game == null || !GenCommandLine.CommandLineArgPassed(CommandLineKey))
			{
				return;
			}
			ran = true;

			try
			{
				Map map = Find.AnyPlayerHomeMap ?? (game.Maps.Count > 0 ? game.Maps[0] : null);
				if (map == null)
				{
					ModLog.Warn("Robotics", "selftest skipped: no map.");
					return;
				}

				IntVec3 cell = CellFinder.RandomClosewalkCellNear(
					map.Center,
					map,
					12,
					c => c.Standable(map) && !c.Fogged(map));
				if (!cell.IsValid)
				{
					cell = map.Center;
				}

				Thing station = ThingMaker.MakeThing(NannyBotDefOf.CK_NannyBotStation);
				GenSpawn.Spawn(station, cell, map, Rot4.South);
				CompNannyBotStation comp = station.TryGetComp<CompNannyBotStation>();
				if (comp == null)
				{
					ModLog.Error("Robotics", "selftest failed: station comp missing.");
					return;
				}

				bool spawned = comp.TrySpawnRobot(true);
				Pawn robot = comp.robot;
				StringBuilder builder = new StringBuilder();
				builder.Append("selftest station=").Append(station.Position)
					.Append(" spawned=").Append(spawned)
					.Append(" robot=").Append(robot != null);
				if (robot != null)
				{
					builder.Append(" def=").Append(robot.def.defName)
						.Append(" kind=").Append(robot.kindDef?.defName)
						.Append(" name=").Append(robot.Name?.ToStringFull)
						.Append(" faction=").Append(robot.Faction?.def?.defName)
						.Append(" thinkTree=").Append(robot.RaceProps.thinkTreeMain?.defName)
						.Append(" spawned=").Append(robot.Spawned)
						.Append(" needs=").Append(robot.needs?.AllNeeds?.Count ?? 0)
						.Append(" energy=").Append(NannyBotUtility.Energy(robot)?.CurLevel.ToString("0.00") ?? "none")
						.Append(" moving=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Moving).ToString("0.00"))
						.Append(" manipulation=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation).ToString("0.00"))
						.Append(" consciousness=").Append(robot.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness).ToString("0.00"))
						.Append(" playerSettings=").Append(robot.playerSettings != null);

					CompNannyBot botComp = NannyBotUtility.BotComp(robot);
					builder.Append(" sweep=").Append(botComp?.sweep)
						.Append(" scrub=").Append(botComp?.scrub)
						.Append(" haul=").Append(botComp?.haul);

					IntVec3 filthCell = IntVec3.Invalid;
					Filth filth = null;
					bool madeFilth = false;
					for (int dx = -30; dx <= 30 && !madeFilth; dx++)
					{
						for (int dz = -30; dz <= 30 && !madeFilth; dz++)
						{
							IntVec3 candidate = cell + new IntVec3(dx, 0, dz);
							if (!candidate.InBounds(map) || !candidate.Standable(map))
							{
								continue;
							}
							TerrainDef terrain = map.terrainGrid.TerrainAt(candidate);
							if (!FilthMaker.TerrainAcceptsFilth(
								terrain,
								ThingDefOf.Filth_Dirt,
								FilthSourceFlags.Pawn))
							{
								continue;
							}
							filthCell = candidate;
							madeFilth = FilthMaker.TryMakeFilth(
								filthCell,
								map,
								ThingDefOf.Filth_Dirt,
								out filth,
								1,
								FilthSourceFlags.Pawn);
						}
					}
					builder.Append(" filthCell=").Append(filthCell)
						.Append(" terrain=").Append(filthCell.IsValid
							? map.terrainGrid.TerrainAt(filthCell).defName
							: "none")
						.Append(" madeFilth=").Append(madeFilth);
					if (madeFilth)
					{
						// 新污渍默认 600 tick 内不可清理；测试里直接把年龄拨老。
						AccessTools.FieldRefAccess<Filth, int>("growTick")(filth) =
							Find.TickManager.TicksGame - 1000;
						ThinkResult cleanResult = new JobGiver_NannyBotClean()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" cleanJob=")
							.Append(cleanResult.Job?.def?.defName ?? "none");
					}

					IntVec3 stockCell = CellFinder.RandomClosewalkCellNear(
						cell,
						map,
						8,
						c => c.Standable(map) && c != cell);
					IntVec3 itemCell = CellFinder.RandomClosewalkCellNear(
						cell,
						map,
						8,
						c => c.Standable(map) && c != cell && c != stockCell);
					if (stockCell.IsValid && itemCell.IsValid)
					{
						Zone_Stockpile stockpile = new Zone_Stockpile(
							StorageSettingsPreset.DefaultStockpile,
							map.zoneManager);
						map.zoneManager.RegisterZone(stockpile);
						stockpile.AddCell(stockCell);
						Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
						steel.stackCount = 25;
						GenSpawn.Spawn(steel, itemCell, map);
						ThinkResult haulResult = new JobGiver_NannyBotHaul()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" haulJob=")
							.Append(haulResult.Job?.def?.defName ?? "none");
					}

					IntVec3 fireCell = cell + new IntVec3(1, 0, 0);
					if (fireCell.InBounds(map))
					{
						Fire fire = ThingMaker.MakeThing(ThingDefOf.Fire) as Fire;
						if (fire != null)
						{
							GenSpawn.Spawn(fire, fireCell, map);
							ThinkResult fireResult = new JobGiver_NannyBotFirefight()
								.TryIssueJobPackage(robot, new JobIssueParams());
							builder.Append(" fireJob=")
								.Append(fireResult.Job?.def?.defName ?? "none");
							if (!fire.Destroyed)
							{
								fire.Destroy();
							}
						}
					}

					IntVec3 generatorCell = FindFreeRect(map, cell, 12, 2, 2);
					if (generatorCell.IsValid)
					{
						Thing generator = ThingMaker.MakeThing(ThingDefOf.WoodFiredGenerator);
						GenSpawn.Spawn(generator, generatorCell, map, Rot4.North);
						generator.SetFaction(Faction.OfPlayer);
						CompRefuelable fuel = generator.TryGetComp<CompRefuelable>();
						if (fuel != null && fuel.Fuel > 0f)
						{
							fuel.ConsumeFuel(fuel.Fuel);
						}
						IntVec3 woodCell = CellFinder.RandomClosewalkCellNear(
							generatorCell,
							map,
							4,
							c => c.Standable(map) && c.GetEdifice(map) == null);
						Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
						wood.stackCount = 75;
						GenSpawn.Spawn(wood, woodCell, map);
						ThinkResult refuelResult = new JobGiver_NannyBotRefuel()
							.TryIssueJobPackage(robot, new JobIssueParams());
						builder.Append(" refuelJob=")
							.Append(refuelResult.Job?.def?.defName ?? "none");
						wood.Destroy();
						generator.Destroy();
					}

					RoboticsSettings settings = NannyBotUtility.Settings;
					builder.Append(" batteryCap=").Append(settings.batteryCapacity)
						.Append(" chargeThreshold=").Append(settings.chargeThreshold.ToString("0.00"))
						.Append(" workRadius=").Append(settings.workRadius.ToString("0"));

					if (botComp != null)
					{
						botComp.durability = 500f;
						Pawn colonist = null;
						foreach (Pawn candidate in map.mapPawns.FreeColonistsSpawned)
						{
							colonist = candidate;
							break;
						}
						WorkGiver_RepairNannyBot repair = new WorkGiver_RepairNannyBot();
						bool canRepair = colonist != null && repair.HasJobOnThing(colonist, robot);
						Job repairJob = canRepair ? repair.JobOnThing(colonist, robot) : null;
						builder.Append(" durability=").Append(botComp.Durability.ToString("0"))
							.Append(" repairJob=").Append(repairJob?.def?.defName ?? "none");
					}
				}
				ModLog.Force("Robotics", builder.ToString());
			}
			catch (Exception exception)
			{
				ModLog.Error("Robotics", "selftest threw", exception);
			}
		}
	}
}
