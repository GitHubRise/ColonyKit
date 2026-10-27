using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>走到岗位后站岗，敌人出现时中断，交给战斗 ThinkNode。</summary>
	public class JobDriver_MkVGuard : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

			Toil guard = ToilMaker.MakeToil("MkVGuard");
			guard.defaultCompleteMode = ToilCompleteMode.Delay;
			guard.defaultDuration = 300;
			guard.tickIntervalAction = delta =>
			{
				if (pawn.IsHashIntervalTick(30)
					&& (pawn.mindState?.enemyTarget != null
						|| GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map)))
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			};
			yield return guard;
		}
	}
}
