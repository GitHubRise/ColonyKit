using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 在殖民地内巡逻站岗，遇敌由战斗 ThinkNode 接管。</summary>
	public class JobGiver_MkVGuard : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			if (!Rand.Chance(0.35f))
			{
				return null;
			}

			List<IntVec3> candidates = new List<IntVec3>();
			foreach (IntVec3 cell in pawn.Map.areaManager.Home.ActiveCells)
			{
				if (cell.Standable(pawn.Map) && !cell.IsForbidden(pawn))
				{
					candidates.Add(cell);
					if (candidates.Count >= 256)
					{
						break;
					}
				}
			}
			if (candidates.Count == 0 || !candidates.TryRandomElement(out IntVec3 guardCell))
			{
				return null;
			}
			if (!pawn.CanReach(guardCell, PathEndMode.OnCell, Danger.Deadly))
			{
				return null;
			}

			JobDef guard = DefDatabase<JobDef>.GetNamedSilentFail("CK_MkVGuard");
			return guard == null ? null : JobMaker.MakeJob(guard, guardCell);
		}
	}
}
