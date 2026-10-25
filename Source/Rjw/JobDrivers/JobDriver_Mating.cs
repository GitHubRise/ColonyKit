#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	/// <summary>
	/// 这是动物交配的驱动。
	/// </summary>
	public class JobDriver_Mating : JobDriver_Rape
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		[SyncMethod]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			setup_ticks();
			var partnerJob = xxx.gettin_loved;

			//--ModLog.Message("JobDriver_Mating::MakeNewToils() - setting fail conditions");
			this.FailOnDespawnedNullOrForbidden(iTarget);
			this.FailOn(() => !pawn.CanReserve(Partner, BreederHelper.max_animals_at_once, 0)); // 若在动物到达前有其他人预定了目标，则失败。
			this.FailOn(() => !pawn.CanReach(Partner, PathEndMode.Touch, Danger.Some)); // 若动物无法到达目标，则失败。
			this.FailOn(() => pawn.Drafted);

			// 通往目标的路径
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			var startPartnerJob = new Toil();
			startPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			startPartnerJob.socialMode = RandomSocialMode.Off;
			startPartnerJob.initAction = delegate
			{
				if (Partner.jobs.curDriver is JobDriver_SexBaseRecieverLoved) return;

				var bed = Partner.CurrentBed();

				Partner.jobs.StartJob(
					JobMaker.MakeJob(partnerJob, pawn),
					JobCondition.InterruptForced
				);

				if (bed is not null)
					if (Partner.jobs.curDriver is JobDriver_SexBaseRecieverLoved driver)
						driver.Set_bed(bed);
			};
			yield return startPartnerJob;

			// 交配目标
			var sexToil = new Toil();
			sexToil.defaultCompleteMode = ToilCompleteMode.Never;
			sexToil.defaultDuration = duration;
			sexToil.handlingFacing = true;
			sexToil.initAction = delegate
			{
				Partner.pather.StopDead();
				Partner.jobs.curDriver.asleep = false;

				Start();
				if (xxx.is_human(Partner))
					Sexprops.usedCondom = CondomUtility.TryUseCondom(Partner);
				if (Sexprops.usedCondom)
				{
					var receiverDriver = Partner?.jobs?.curDriver as JobDriver_SexBaseReciever;
					if (receiverDriver is not null && receiverDriver.Sexprops != null)
						receiverDriver.Sexprops.usedCondom = true;
				}
			};
			sexToil.tickAction = delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
				SexTick(pawn, Partner);
				if (!Partner.Dead)
					SexUtility.reduce_rest(Partner, 1);
				SexUtility.reduce_rest(pawn, 2);
				if (ticks_left <= 0)
					ReadyForNextToil();
			};
			sexToil.FailOn(() => Partner.CurJob?.def != partnerJob);
			sexToil.AddFinishAction(End);
			yield return sexToil;

			yield return new Toil
			{
				initAction = () => SexUtility.ProcessSex(Sexprops),
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
