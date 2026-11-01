using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 专用索敌：优先打带范围武器的敌人，其次打高伤、高命中、脆弱的目标；
	/// 收到敌袭预警时把索敌范围放大，并主动朝敌人方向推进。
	/// </summary>
	public class JobGiver_MkVCombat : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			CompMkVCombat combat = pawn.TryGetComp<CompMkVCombat>();
			if (combat == null || pawn.Drafted)
			{
				return null;
			}
			if (!NannyBotUtility.Settings.mkvCombatEnabled)
			{
				combat.Clear();
				return null;
			}

			bool alert = NannyBotUtility.Settings.aiRaidAdvance && MkVAlert.IsActive(pawn.Map);
			float range = alert
				? Mathf.Max(combat.EngageRange, MkVAlert.AlertEngageRange)
				: combat.EngageRange;
			Pawn target = FindTarget(pawn, range);
			if (target != null)
			{
				combat.SetEngaged(target);
				float distance = pawn.Position.DistanceTo(target.Position);
				if (distance <= 1.9f)
				{
					Job melee = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
					melee.expiryInterval = 300;
					return melee;
				}

				Verb verb = pawn.TryGetAttackVerb(target, false);
				if (verb == null)
				{
					combat.Clear();
					return null;
				}
				// 用 EffectiveRange 而不是 verbProps.range：电弧发射器在 Mk-V 手上还有 15 格专属加成。
				if (distance > verb.EffectiveRange)
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

			// 预警但敌人还在雾里：主动朝他们的方向推进，靠近后自然进入上面的索敌分支。
			if (alert)
			{
				IntVec3 hostileCell = MkVAlert.NearestHostileCell(pawn.Map, pawn.Position);
				if (hostileCell.IsValid && pawn.Position.DistanceTo(hostileCell) > 12f)
				{
					IntVec3 stand = CellFinder.StandableCellNear(hostileCell, pawn.Map, 6f);
					if (!stand.IsValid)
					{
						stand = hostileCell;
					}
					if (pawn.CanReach(stand, PathEndMode.OnCell, Danger.Deadly))
					{
						Job advance = JobMaker.MakeJob(JobDefOf.Goto, stand);
						advance.locomotionUrgency = LocomotionUrgency.Jog;
						advance.expiryInterval = 300;
						return advance;
					}
				}
			}

			if (combat.state == MkVCombatState.Engaged)
			{
				if (!NannyBotUtility.Settings.mkvReturnToBay)
				{
					combat.Clear();
					return null;
				}
				combat.SetReturning();
			}
			if (combat.state == MkVCombatState.Returning)
			{
				if (!NannyBotUtility.Settings.mkvReturnToBay)
				{
					combat.Clear();
					return null;
				}
				return ReturnJob(pawn, combat);
			}
			return null;
		}

		/// <summary>在范围内挑威胁分最高的敌人；同分时 MkVThreat 已经按距离排过序。</summary>
		private static Pawn FindTarget(Pawn pawn, float range)
		{
			Pawn best = null;
			int bestScore = int.MinValue;
			foreach (MkVThreat.Threat threat in MkVThreat.Rank(pawn, false))
			{
				if (threat.distance > range)
				{
					continue;
				}
				Pawn other = threat.pawn;
				if (other == null || other.Dead || other.Downed || !other.Spawned)
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
				if (threat.score > bestScore)
				{
					best = other;
					bestScore = threat.score;
				}
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
