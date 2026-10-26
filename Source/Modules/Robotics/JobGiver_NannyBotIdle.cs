using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>没有活干时回到充电桩旁边待命。</summary>
	public class JobGiver_NannyBotIdle : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.Downed
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return null;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp != null && (comp.repairing || NannyBotUtility.IsBeingRepaired(pawn)))
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
				|| pawn.Position.DistanceTo(cell) <= 1.9f)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Walk;
			job.expiryInterval = 600;
			return job;
		}
	}
}
