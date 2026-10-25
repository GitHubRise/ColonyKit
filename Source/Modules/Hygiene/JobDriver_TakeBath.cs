using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobDriver_TakeBath : JobDriver
	{
		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			Toil fill = new Toil
			{
				defaultDuration = 1000,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			fill.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			fill.tickAction = () =>
			{
				Building_Bath bath = job.targetA.Thing as Building_Bath;
				if (bath == null || !bath.TryFillBath())
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (bath.IsFull)
				{
					ReadyForNextToil();
				}
			};
			yield return fill;

			Toil bathe = new Toil
			{
				defaultDuration = 1800,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			bathe.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			bathe.tickAction = () =>
			{
				Building_Bath bath = job.targetA.Thing as Building_Bath;
				if (bath == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				pawn.needs?.TryGetNeed<Need_Hygiene>()?.clean(bath.cleanedPerTick);
				pawn.GainComfortFromCellIfPossible(1);
				if (bath.contamination > waterQuality)
				{
					waterQuality = bath.contamination;
				}
			};
			bathe.AddFinishAction(() =>
			{
				pawn.filth?.CarriedFilthListForReading?.Clear();
				Building_Bath bath = job.targetA.Thing as Building_Bath;
				if (bath != null)
				{
					SanitationUtil.ApplyBathroomThought(pawn, bath);
					SanitationUtil.WaterTempCheck(pawn, bath.IsCold, true);
					SanitationUtil.ContaminationCheckWater(pawn, waterQuality);
					SanitationUtil.CheckForBlockage(bath);
					bath.TryPullPlug();
				}
			});
			yield return bathe;
		}
	}
}
