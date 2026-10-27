using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 专用索敌：28 格内交战，超出后脱战并返回组装舱。</summary>
	public class JobGiver_MkVCombat : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			CompMkVCombat combat = pawn.TryGetComp<CompMkVCombat>();
			if (combat == null)
			{
				return null;
			}

			Pawn target = FindTarget(pawn, combat.EngageRange);
			if (target != null)
			{
				Verb verb = pawn.TryGetAttackVerb(target, false);
				if (verb == null)
				{
					combat.Clear();
					return null;
				}

				combat.SetEngaged(target);
				float distance = pawn.Position.DistanceTo(target.Position);
				if (distance > verb.verbProps.range)
				{
					Job move = JobMaker.MakeJob(JobDefOf.Goto, target.Position);
					move.locomotionUrgency = LocomotionUrgency.Jog;
					move.expiryInterval = 300;
					return move;
				}

				Job attack = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
				attack.expiryInterval = 300;
				return attack;
			}

			if (combat.state == MkVCombatState.Engaged)
			{
				combat.SetReturning();
			}
			if (combat.state == MkVCombatState.Returning)
			{
				return ReturnJob(pawn, combat);
			}
			return null;
		}

		private static Pawn FindTarget(Pawn pawn, float range)
		{
			Pawn best = null;
			float bestDistance = range;
			foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (other == null || other == pawn || other.Dead || other.Downed || !other.Spawned)
				{
					continue;
				}
				if (!other.HostileTo(Faction.OfPlayer) || other.Position.Fogged(pawn.Map))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(other.Position);
				if (distance > range || distance >= bestDistance)
				{
					continue;
				}
				if (!GenSight.LineOfSight(pawn.Position, other.Position, pawn.Map, true))
				{
					continue;
				}
				if (!pawn.CanReserve(other))
				{
					continue;
				}
				best = other;
				bestDistance = distance;
			}
			return best;
		}

		private static Job ReturnJob(Pawn pawn, CompMkVCombat combat)
		{
			Building bay = combat.HomeBay;
			if (bay == null || !bay.Spawned || bay.Map != pawn.Map)
			{
				combat.Clear();
				return null;
			}
			IntVec3 cell = bay.InteractionCell;
			if (!cell.IsValid || !cell.InBounds(pawn.Map))
			{
				cell = bay.Position;
			}
			if (pawn.Position.DistanceTo(cell) <= combat.ReturnRange)
			{
				combat.Clear();
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			job.expiryInterval = 600;
			return job;
		}
	}
}
