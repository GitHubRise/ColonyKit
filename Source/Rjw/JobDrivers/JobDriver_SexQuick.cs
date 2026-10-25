#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobDriver_SexQuick : JobDriver_SexBaseInitiator
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, xxx.max_rapists_per_prisoner, 0, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			//ModLog.Message("" + this.GetType().ToString() + "::MakeNewToils() called");
			setup_ticks();
			var PartnerJob = xxx.getting_quickie;

			this.FailOnDespawnedNullOrForbidden(iTarget);
			this.FailOn(() => !Partner.health.capacities.CanBeAwake);
			this.FailOn(() => pawn.Drafted);

			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			Toil findQuickieSpot = new Toil();
			findQuickieSpot.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			findQuickieSpot.initAction = delegate
			{
				//需要提前拿到它，以判断当前位置是否足够好
				var all_pawns = pawn.Map.mapPawns.AllPawnsSpawned.Where(x
				=> x.Position.DistanceToSquared(pawn.Position) < 10000
				&& xxx.is_human(x)
				&& x != pawn
				&& x != Partner
				).ToList();

				FloatRange temperature = pawn.ComfortableTemperatureRange();
				float cellTemp = pawn.Position.GetTemperature(pawn.Map);

				if (Partner.IsPrisonerInPrisonCell() || (!CasualSex_Helper.MightBeSeen(all_pawns, pawn.Position, pawn, Partner) && (cellTemp > temperature.min && cellTemp < temperature.max)))
				{
					ReadyForNextToil();
				}
				else
				{
					var spot = CasualSex_Helper.FindSexLocation(pawn, Partner);
					pawn.pather.StartPath(spot, PathEndMode.OnCell);
					//有时会因呕吐之类的情况出错
					//有时伙伴为 null ？？？不知道怎么修
					var partnerJobs = Partner?.jobs;
					partnerJobs?.StopAll();
					Job job = JobMaker.MakeJob(JobDefOf.Goto, spot);
					partnerJobs?.StartJob(job, JobCondition.InterruptForced);

					float pawnSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
					float partnerSpeed = Partner.GetStatValue(StatDefOf.MoveSpeed);

					// 使用 Goto 时，如果伙伴速度更快并先到达地点，他们会改做别的工作，而不是等待发起的小人
					// 这是个草率的方案，但我现在实在想不出更好的
					if (partnerSpeed > pawnSpeed)
					{
						// 让伙伴在原地等待 1 小时直到快速性爱开始，如有需要仍可被玩家打断
						// 作为参考，GoToMindControlled 会等待 0.8 小时（2000 tick）
						int waitTicks = 2500;
						Job partnerWait = JobMaker.MakeJob(JobDefOf.Wait, waitTicks);
						partnerJobs.jobQueue.EnqueueLast(partnerWait);
					}
				}
			};
			yield return findQuickieSpot;

			Toil WaitForPartner = new Toil();
			WaitForPartner.defaultCompleteMode = ToilCompleteMode.Delay;
			WaitForPartner.initAction = delegate
			{
				ticksLeftThisToil = 5000;
			};
			WaitForPartner.tickIntervalAction = delegate(int delta)
			{
				pawn.GainComfortFromCellIfPossible(delta);
				if (pawn.Position.DistanceToSquared(Partner.Position) <= 1f)
				{
					ReadyForNextToil();
				}
			};
			yield return WaitForPartner;

			Toil StartPartnerJob = new Toil();
			StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			StartPartnerJob.socialMode = RandomSocialMode.Off;
			StartPartnerJob.initAction = delegate
			{
				Job gettingQuickie = JobMaker.MakeJob(PartnerJob, pawn, Partner);
				Partner.jobs.StartJob(gettingQuickie, JobCondition.InterruptForced);
			};
			yield return StartPartnerJob;

			Toil SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.socialMode = RandomSocialMode.Off;
			SexToil.defaultDuration = duration;
			SexToil.handlingFacing = true;
			SexToil.FailOn(() => Partner.CurJob?.def != PartnerJob);
			SexToil.initAction = delegate
			{
				Partner.pather.StopDead();
				Partner.jobs.curDriver.asleep = false;

				Start();
				Sexprops.usedCondom = CondomUtility.TryUseCondom(pawn) || CondomUtility.TryUseCondom(Partner);
				if (Sexprops.usedCondom)
				{
					var receiverDriver = Partner?.jobs?.curDriver as JobDriver_SexBaseReciever;
					if (receiverDriver is not null && receiverDriver.Sexprops != null)
						receiverDriver.Sexprops.usedCondom = true;
				}
			};
			SexToil.AddPreTickAction(delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
				SexTick(pawn, Partner);
				SexUtility.reduce_rest(Partner, 1);
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
