using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>危险规避：附近有敌对目标或全图高危险时，停止家务并回到充电桩待命。</summary>
	public class JobGiver_NannyBotFlee : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.Downed
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId)
				|| !NannyBotUtility.Settings.dangerAvoidance)
			{
				return null;
			}
			if (!NannyBotUtility.HasNearbyThreat(pawn))
			{
				return null;
			}

			Building station = NannyBotUtility.FindStation(pawn, false);
			if (station == null)
			{
				return null;
			}

			IntVec3 cell = station.InteractionCell;
			if (!cell.IsValid || !cell.InBounds(pawn.Map)
				|| pawn.Position.DistanceTo(cell) <= 2f)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			job.expiryInterval = 600;
			return job;
		}
	}
}
