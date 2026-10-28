using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobDriver_WashHands : JobDriver
	{
		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			Toil wash = new Toil
			{
				defaultDuration = 100,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			wash.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			wash.initAction = () =>
			{
				if (job.targetA.Thing is Building_Basin basin)
				{
					basin.occupiedSinceTick = Find.TickManager.TicksGame;
				}
			};
			wash.tickAction = () =>
			{
				Building_Basin basin = job.targetA.Thing as Building_Basin;
				if (basin == null || !basin.TryUseWater(out ContaminationLevel contam))
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (contam > waterQuality)
				{
					waterQuality = contam;
				}
				pawn.needs?.TryGetNeed<Need_Hygiene>()?.clean(0.002f);
			};
			wash.AddFinishAction(() =>
			{
				if (job.targetA.Thing is Building_Basin basin)
				{
					basin.occupiedSinceTick = -1;
				}
				// 水盆是一次一用的存水设施：洗完扣一次用量（DBH 的 UseBucket）。
				if (job.targetA.Thing is Building_WaterTub tub)
				{
					tub.UseOnce();
				}
				SanitationUtil.ApplyBathroomThought(pawn, job.targetA.Thing as Building_AssignableFixture);
				SanitationUtil.ContaminationCheckWater(pawn, waterQuality);
				SanitationUtil.CheckForBlockage(job.targetA.Thing as Building);
			});
			yield return wash;
		}
	}
}
