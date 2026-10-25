#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	public class JobDriver_BestialityForFemale : JobDriver_SexBaseInitiator
	{
		public IntVec3 SleepSpot => Bed.SleepPosOfAssignedPawn(pawn);
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, 1, 0, null, errorOnFailed);
		}

		[SyncMethod]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			setup_ticks();
			var PartnerJob = xxx.gettin_loved;

			this.FailOnDespawnedOrNull(iTarget);
			this.FailOnDespawnedNullOrForbidden(iBed);
			this.FailOn(() => !pawn.CanReserveAndReach(Partner, PathEndMode.Touch, Danger.Deadly));
			this.FailOn(() => pawn.Drafted);
			this.FailOn(() => Partner.IsFighting());
			this.FailOn(() => !Partner.CanReach(SleepSpot, PathEndMode.Touch, Danger.Deadly)); //改为 SleepSpot，因为伙伴只会移动到床上

            yield return Toils_Reserve.Reserve(iTarget, 1, 0);
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.Touch);

			var gotoBed = new Toil();
			gotoBed.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			gotoBed.initAction = delegate
			{
				pawn.pather.StartPath(SleepSpot, PathEndMode.OnCell);
				Partner.jobs.StopAll();
				Job job = JobMaker.MakeJob(JobDefOf.GotoMindControlled, SleepSpot);
				Partner.jobs.StartJob(job, JobCondition.InterruptForced);
			};
			gotoBed.FailOnDespawnedOrNull(iBed); //从 FailOnBedNoLongerUsable(iBed) 改来，因为小人无法使用不属于自己的床。
			gotoBed.AddFailCondition(() => Partner.Downed);
			yield return gotoBed;

			var waitInBed = new Toil();
			waitInBed.defaultCompleteMode = ToilCompleteMode.Delay;
			waitInBed.initAction = delegate
			{
				ticksLeftThisToil = 5000;
			};
			waitInBed.tickIntervalAction = delegate(int delta)
			{
				pawn.GainComfortFromCellIfPossible(delta);
				if (IsInOrByBed(Bed, Partner) && pawn.PositionHeld == Partner.PositionHeld)
				{
					ReadyForNextToil();
				}
			};
			waitInBed.FailOn(() => pawn.GetRoom(RegionType.Set_Passable) == null);
			yield return waitInBed;

			var StartPartnerJob = new Toil();
			StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			StartPartnerJob.socialMode = RandomSocialMode.Off;
			StartPartnerJob.initAction = delegate
			{
				var gettin_loved = JobMaker.MakeJob(PartnerJob, pawn, Bed);
				Partner.jobs.StartJob(gettin_loved, JobCondition.InterruptForced);
			};
			yield return StartPartnerJob;

			var SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.socialMode = RandomSocialMode.Off;
			SexToil.handlingFacing = true;
			SexToil.initAction = delegate
			{
				Start();

				// TODO：替换这种快速粗糙的做法
				CondomUtility.GetCondomFromRoom(pawn);
				// 优先尝试使用妓女的避孕套，其次用客户的
				Sexprops.usedCondom = CondomUtility.TryUseCondom(pawn) || CondomUtility.TryUseCondom(Partner);
				if (Sexprops.usedCondom)
				{
					var receiverDriver = Partner?.jobs?.curDriver as JobDriver_SexBaseReciever;
					if (receiverDriver is not null && receiverDriver.Sexprops != null)
						receiverDriver.Sexprops.usedCondom = true;
				}
			};
			SexToil.FailOn(() => Partner.Dead);
			SexToil.FailOn(() => Partner.CurJob?.def != PartnerJob);
			SexToil.AddPreTickAction(delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					if (xxx.is_zoophile(pawn))
						ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
					else
						ThrowMetaIconF(pawn.Position, pawn.Map, xxx.mote_noheart);
				SexTick(pawn, Partner);
				SexUtility.reduce_rest(Partner, 2);
				SexUtility.reduce_rest(pawn, 1);
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
