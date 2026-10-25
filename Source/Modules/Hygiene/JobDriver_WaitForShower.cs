using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>没有空淋浴器时在门口等待；占用者离开后自动接手。</summary>
	public class JobDriver_WaitForShower : JobDriver
	{
		private const int WaitTicks = 2500;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			// 目标淋浴器正被别人占用，等待期间不能预约。
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

			Toil wait = new Toil
			{
				defaultDuration = WaitTicks,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			wait.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			wait.socialMode = RandomSocialMode.Quiet;
			wait.tickAction = () =>
			{
				Building_Shower shower = job.targetA.Thing as Building_Shower;
				if (shower == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (!shower.CanUse(pawn))
				{
					return;
				}

				JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeShower");
				if (def == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				Job showerJob = JobMaker.MakeJob(def, shower);
				pawn.jobs.jobQueue.EnqueueFirst(showerJob);
				EndJobWith(JobCondition.Succeeded);
			};
			yield return wait;
		}
	}
}
