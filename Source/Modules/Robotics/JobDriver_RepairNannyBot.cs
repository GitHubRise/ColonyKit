using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>维修保姆机器人：持续工作 400 tick，恢复 250 点耐久。</summary>
	public class JobDriver_RepairNannyBot : JobDriver
	{
		private const TargetIndex RobotInd = TargetIndex.A;
		private const int WorkTicks = 400;
		private const float RepairAmount = 250f;

		private int workDone;

		private Pawn Robot => job.GetTarget(RobotInd).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(RobotInd), job, 1, -1, null, errorOnFailed);
		}

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			Pawn robot = Robot;
			CompNannyBot comp = robot?.TryGetComp<CompNannyBot>();
			comp?.Notify_RepairStarted();
			AddFinishAction(condition => comp?.Notify_RepairFinished());

			// 维修优先：把机器人正在进行的充电或家务立刻打断，修完再重新找活。
			if (robot != null && robot.CurJob != null)
			{
				robot.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(RobotInd);

			yield return Toils_Goto.GotoThing(RobotInd, PathEndMode.Touch);

			Toil repair = ToilMaker.MakeToil("CK_RepairNannyBot");
			repair.defaultCompleteMode = ToilCompleteMode.Never;
			repair.FailOnDespawnedNullOrForbidden(RobotInd);
			repair.initAction = () => workDone = 0;
			repair.tickIntervalAction = delta =>
			{
				Pawn robot = Robot;
				CompNannyBot comp = robot?.TryGetComp<CompNannyBot>();
				if (comp == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}

				workDone += delta;
				if (workDone >= WorkTicks)
				{
					comp.Repair(RepairAmount);
					ReadyForNextToil();
				}
			};
			repair.WithProgressBar(
				RobotInd,
				() => Mathf.Clamp01((float)workDone / WorkTicks),
				interpolateBetweenActorAndTarget: true);
			yield return repair;
		}
	}
}
