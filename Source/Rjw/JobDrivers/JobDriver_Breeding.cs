#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	/// <summary>
	/// 这是动物骑乘配种对象的驱动。
	/// </summary>
	public class JobDriver_Breeding : JobDriver_Rape
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, BreederHelper.max_animals_at_once, 0);
		}

		[SyncMethod]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			setup_ticks();
			var PartnerJob = xxx.gettin_raped;

			//--ModLog.Message("JobDriver_Breeding::MakeNewToils() - setting fail conditions");
			this.FailOnDespawnedNullOrForbidden(iTarget);
			this.FailOn(() => !pawn.CanReserve(Partner, BreederHelper.max_animals_at_once, 0)); // 若在动物到达前有其他人预定了目标，则失败。
			this.FailOn(() => !pawn.CanReach(Partner, PathEndMode.Touch, Danger.Some)); // 若动物无法到达目标，则失败。
			this.FailOn(() => pawn.Drafted);

			if (!(pawn.IsAnimal() && Partner.IsAnimal()))
			{
				yield return Toils_Sex.RapeTargetAlert(pawn, Partner);
			}

			// 通往目标的路径
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			var StartPartnerJob = new Toil();
			StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			StartPartnerJob.socialMode = RandomSocialMode.Off;
			StartPartnerJob.initAction = delegate
			{
				if (Partner.jobs.curDriver is JobDriver_SexBaseRecieverRaped) return;

				var bed = Partner.CurrentBed();

				Partner.jobs.StartJob(
					JobMaker.MakeJob(PartnerJob, pawn),
					lastJobEndCondition: JobCondition.InterruptForced
				);

				if (bed is not null)
					if (Partner.jobs.curDriver is JobDriver_SexBaseRecieverRaped driver)
						driver.Set_bed(bed);
			};
			yield return StartPartnerJob;

			// 接受配种时显示开心爱心，而非反感触碰的爱心。
			var fleckDef = xxx.is_zoophile(pawn) || xxx.is_animal(pawn) ? FleckDefOf.Heart : xxx.mote_noheart;

			// 配种目标
			var SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.defaultDuration = duration;
			SexToil.handlingFacing = true;
			SexToil.initAction = delegate
			{
				Partner.pather.StopDead();
				Partner.jobs.curDriver.asleep = false;
				Start();
			};
			SexToil.tickAction = delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, fleckDef);
				SexTick(pawn, Partner);
				if (!Partner.Dead)
					SexUtility.reduce_rest(Partner, 1);
				SexUtility.reduce_rest(pawn, 2);
				if (ticks_left <= 0)
					ReadyForNextToil();
			};
			SexToil.FailOn(() => Partner.CurJob?.def != PartnerJob);
			SexToil.AddFinishAction(End);
			yield return SexToil;

			yield return new Toil
			{
				initAction = delegate
				{
					//ModLog.Message("JobDriver_Breeding::MakeNewToils() - Calling aftersex");
					//// 尝试添加一些互动和社交日志
					Sexprops.isRape = !(pawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, Partner) ||
					 	(xxx.is_animal(pawn) && (pawn.GetStatValue(StatDefOf.Wildness) - pawn.RaceProps.petness + 0.18f) > Rand.Range(0.36f, 1.8f)));
					SexUtility.ProcessSex(Sexprops);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
