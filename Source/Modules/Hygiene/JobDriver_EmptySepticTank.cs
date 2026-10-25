using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobDriver_EmptySepticTank : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override System.Collections.Generic.IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil empty = new Toil
			{
				defaultDuration = 600,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			empty.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			empty.tickAction = () =>
			{
				CompSewageHandler comp = job.targetA.Thing.TryGetComp<CompSewageHandler>();
				if (comp == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				comp.Empty(25f);
				if (comp.sewageBuffer <= 0.01f)
				{
					ReadyForNextToil();
				}
			};
			yield return empty;
		}
	}
}
