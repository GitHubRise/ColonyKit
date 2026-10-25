#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobDriver_JoinInBed : JobDriver_SexBaseInitiator
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, xxx.max_sex_partners_for(pawn, Target as Pawn), 0, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			//ModLog.Message("" + this.GetType().ToString() + "::MakeNewToils() called");

			setup_ticks();

			this.FailOnDespawnedOrNull(iTarget);
			this.FailOnDespawnedOrNull(iBed);
			this.FailOn(() => !Partner.health.capacities.CanBeAwake);
			this.FailOn(() => !(Partner.InBed() || Bed_Utility.in_same_bed(Partner, pawn) || Partner.jobs?.curDriver is JobDriver_SexBaseReciever));
			this.FailOn(() => pawn.Drafted);
			yield return Toils_Reserve.Reserve(iTarget, xxx.max_sex_partners_for(pawn, Target as Pawn), 0);
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			Toil StartPartnerJob = new Toil();
			StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			StartPartnerJob.socialMode = RandomSocialMode.Off;
			StartPartnerJob.initAction = delegate
			{
				// 接受者已经在同一场性行为里时，不要重启它的工作：
				// 重启会新建一份接收者驱动，把已有的 parteners（其他参与者）全部丢掉，
				// 这正是群交里"性爱被重复开始"的原因。
				if (Partner.jobs?.curDriver is JobDriver_SexBaseReciever)
					return;

				Job gettin_loved = JobMaker.MakeJob(xxx.gettin_loved, pawn, Bed);
				Partner.jobs.StartJob(gettin_loved, JobCondition.InterruptForced);
			};
			yield return StartPartnerJob;

			Toil SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.socialMode = RandomSocialMode.Off;
			SexToil.handlingFacing = true;
			SexToil.initAction = delegate
			{
				Start();
				Sexprops.usedCondom = CondomUtility.TryUseCondom(pawn) || CondomUtility.TryUseCondom(Partner);
				if (Sexprops.usedCondom)
				{
					var receiverDriver = Partner?.jobs?.curDriver as JobDriver_SexBaseReciever;
					if (receiverDriver is not null && receiverDriver.Sexprops != null)
						receiverDriver.Sexprops.usedCondom = true;
				}
			};
			SexToil.FailOn(() => Partner.CurJob?.def != xxx.gettin_loved);
			SexToil.AddPreTickAction(delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
				SexTick(pawn, Partner);
				SexUtility.reduce_rest(Partner, 1);
				SexUtility.reduce_rest(pawn, 2);
				if (ticks_left <= 0)
					ReadyForNextToil();
			});
			SexToil.AddFinishAction(End);
			yield return SexToil;

			yield return new Toil
			{
				initAction = () => SexUtility.ProcessSex(Sexprops),
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
