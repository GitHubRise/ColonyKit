using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 没有活干时回到充电桩旁边待命。
	/// 回程必须用 <see cref="LocomotionUrgency.Jog"/>：原版对 Walk 有「每格至少 50 tick」的下限，
	/// 机器人 MoveSpeed 4 时 Walk 只有约 1.2 格/秒，比干活时的 Jog 慢三倍，看起来像卡住。
	/// </summary>
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
			job.locomotionUrgency = LocomotionUrgency.Jog;
			job.expiryInterval = 600;
			return job;
		}
	}
}
