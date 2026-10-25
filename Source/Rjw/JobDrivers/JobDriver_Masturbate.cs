#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	public class JobDriver_Masturbate : JobDriver_SexBaseInitiator
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true; // 无需预定。
		}
		public virtual IntVec3 cell => (IntVec3)job.GetTarget(iCell);

		[SyncMethod]
		protected override void SetupDurationTicks()
		{
			// 欲求不满时自慰更快。
			duration = (int)(xxx.is_frustrated(pawn) ? 2500.0f * Rand.Range(0.2f, 0.7f) : 2500.0f * Rand.Range(0.2f, 0.4f));
			ticks_left = duration;
		}

		protected override void SetupOrgasmTicks()
		{
			orgasmstick = 180;
			sex_ticks = duration;
			orgasmStartTick = sex_ticks;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			setup_ticks();

			//this.FailOn(() => PawnUtility.PlayerForcedJobNowOrSoon(pawn));
			this.FailOn(() => pawn.health.Downed);
			this.FailOn(() => pawn.IsBurning());
			this.FailOn(() => pawn.IsFighting());
			this.FailOn(() => pawn.Drafted);

			Toil findfapspot = new Toil
			{
				initAction = delegate
				{
					pawn.pather.StartPath(cell, PathEndMode.OnCell);
				},
				defaultCompleteMode = ToilCompleteMode.PatherArrival
			};
			yield return findfapspot;

			//ModLog.Message(" Making new toil for QuickFap.");

			Toil SexToil = Toils_General.Wait(duration);
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.handlingFacing = true;
			SexToil.initAction = delegate
			{
				Start();
			};
			SexToil.tickAction = delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
				SexTick(pawn, null);
				SexUtility.reduce_rest(pawn, 1);
				if (ticks_left <= 0)
					ReadyForNextToil();
			};
			SexToil.AddFinishAction(delegate
			{
				End();
			});
			yield return SexToil;

			yield return new Toil
			{
				initAction = delegate
				{
					SexUtility.Aftersex(Sexprops);
					if (!SexUtility.ConsiderCleaning(pawn)) return;

					LocalTargetInfo own_cum = pawn.PositionHeld.GetFirstThing<Filth>(pawn.Map);

					Job clean = JobMaker.MakeJob(JobDefOf.Clean);
					clean.AddQueuedTarget(TargetIndex.A, own_cum);

					pawn.jobs.jobQueue.EnqueueFirst(clean);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
