using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的警卫行为会从几种常见岗位中随机挑一个：
	/// 基地巡逻、守门、看守囚犯、看守仓库。到岗后短暂停留，
	/// 出现敌情时立即中断，交给战斗 ThinkNode。
	/// </summary>
	public class JobGiver_MkVGuard : ThinkNode_JobGiver
	{
		private enum GuardTask
		{
			Patrol,
			Door,
			Prisoner,
			Stockpile
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			// 地图上没有殖民者时，Mk-V 就是唯一的守卫：不再抽签，见到岗位就站。
			bool alone = !NannyBotUtility.AnyColonistPresent(pawn.Map);
			if (!alone && !Rand.Chance(0.8f))
			{
				return null;
			}

			JobDef guard = DefDatabase<JobDef>.GetNamedSilentFail("CK_MkVGuard");
			if (guard == null)
			{
				return null;
			}

			List<GuardTask> tasks = new List<GuardTask>();
			if (HasHomeCell(pawn))
			{
				tasks.Add(GuardTask.Patrol);
			}
			if (FindDoorCell(pawn, out _))
			{
				tasks.Add(GuardTask.Door);
			}
			if (FindPrisonerCell(pawn, out _))
			{
				tasks.Add(GuardTask.Prisoner);
			}
			if (FindStockpileCell(pawn, out _))
			{
				tasks.Add(GuardTask.Stockpile);
			}

			while (tasks.Count > 0)
			{
				GuardTask task = tasks.RandomElement();
				tasks.Remove(task);
				IntVec3 cell;
				string report;
				switch (task)
				{
					case GuardTask.Door:
						if (!FindDoorCell(pawn, out cell)) continue;
						report = "CK.Robotics.Job.Guard.Door".Translate();
						break;
					case GuardTask.Prisoner:
						if (!FindPrisonerCell(pawn, out cell)) continue;
						report = "CK.Robotics.Job.Guard.Prisoner".Translate();
						break;
					case GuardTask.Stockpile:
						if (!FindStockpileCell(pawn, out cell)) continue;
						report = "CK.Robotics.Job.Guard.Stockpile".Translate();
						break;
					default:
						if (!FindHomeCell(pawn, out cell)) continue;
						report = "CK.Robotics.Job.Guard.Patrol".Translate();
						break;
				}
				Job job = JobMaker.MakeJob(guard, cell);
				job.reportStringOverride = report;
				return job;
			}
			return null;
		}

		private static bool HasHomeCell(Pawn pawn)
		{
			return FindHomeCell(pawn, out _);
		}

		private static bool FindHomeCell(Pawn pawn, out IntVec3 cell)
		{
			List<IntVec3> candidates = new List<IntVec3>();
			foreach (IntVec3 candidate in pawn.Map.areaManager.Home.ActiveCells)
			{
				if (candidate.Standable(pawn.Map) && !candidate.IsForbidden(pawn))
				{
					candidates.Add(candidate);
					if (candidates.Count >= 256)
					{
						break;
					}
				}
			}
			return candidates.TryRandomElement(out cell) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly);
		}

		private static bool FindDoorCell(Pawn pawn, out IntVec3 cell)
		{
			List<IntVec3> candidates = new List<IntVec3>();
			foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
			{
				if (building.def.IsDoor)
				{
					foreach (IntVec3 adjacent in GenAdjFast.AdjacentCells8Way(building))
					{
						if (adjacent.Standable(pawn.Map) && !adjacent.IsForbidden(pawn))
						{
							candidates.Add(adjacent);
						}
					}
				}
			}
			return candidates.TryRandomElement(out cell) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly);
		}

		private static bool FindPrisonerCell(Pawn pawn, out IntVec3 cell)
		{
			List<Pawn> prisoners = pawn.Map.mapPawns.PrisonersOfColonySpawned;
			if (prisoners.Count == 0)
			{
				cell = IntVec3.Invalid;
				return false;
			}
			foreach (Pawn prisoner in prisoners)
			{
				IntVec3 near = CellFinder.StandableCellNear(prisoner.Position, pawn.Map, 3f);
				if (near.IsValid && !near.IsForbidden(pawn)
					&& pawn.CanReach(near, PathEndMode.OnCell, Danger.Deadly))
				{
					cell = near;
					return true;
				}
			}
			cell = IntVec3.Invalid;
			return false;
		}

		private static bool FindStockpileCell(Pawn pawn, out IntVec3 cell)
		{
			List<IntVec3> candidates = new List<IntVec3>();
			foreach (Zone zone in pawn.Map.zoneManager.AllZones)
			{
				if (zone is Zone_Stockpile)
				{
					foreach (IntVec3 candidate in zone.Cells)
					{
						if (candidate.Standable(pawn.Map) && !candidate.IsForbidden(pawn))
						{
							candidates.Add(candidate);
						}
					}
				}
			}
			return candidates.TryRandomElement(out cell) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly);
		}
	}
}
