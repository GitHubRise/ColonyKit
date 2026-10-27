using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 双人浴缸泡澡：发起者在出发时邀请一名异性，两人各跑一份本 Job，
	/// 共用同一缸水；最后离开的人才放水。
	/// </summary>
	public class JobDriver_TakeBathTogether : JobDriver
	{
		private const TargetIndex BathInd = TargetIndex.A;
		private const TargetIndex PartnerInd = TargetIndex.B;

		private ContaminationLevel waterQuality = ContaminationLevel.Treated;
		private bool completed;

		private Building_DoubleBath Bath => job.GetTarget(BathInd).Thing as Building_DoubleBath;

		private Pawn Partner => job.GetTarget(PartnerInd).Thing as Pawn;

		/// <summary>发起者的 targetB 是同伴；被邀请者的 targetB 是发起者，二者都不为空。</summary>
		private bool IsInviter => Partner != null && Partner != pawn;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterQuality, "waterQuality", ContaminationLevel.Treated);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			// 双人共用一个建筑，RimWorld 的独占预约会让第二个人进不来；
			// 容量由 Building_DoubleBath 自己按当前 Job 统计。
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(BathInd);
			yield return MakeInviteToil();
			yield return Toils_Goto.GotoThing(BathInd, PathEndMode.Touch);

			Toil fill = new Toil
			{
				defaultDuration = 1000,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			fill.FailOnDestroyedNullOrForbidden(BathInd);
			fill.tickAction = () =>
			{
				Building_DoubleBath bath = Bath;
				if (bath == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (bath.IsFull)
				{
					ReadyForNextToil();
					return;
				}
				if (!bath.TryFillBath())
				{
					EndJobWith(JobCondition.Incompletable);
				}
			};
			yield return fill;

			Toil bathe = new Toil
			{
				defaultDuration = 1800,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			bathe.FailOnDestroyedNullOrForbidden(BathInd);
			bathe.initAction = () =>
			{
				Building_DoubleBath bath = Bath;
				if (bath != null)
				{
					bath.occupiedSinceTick = Find.TickManager.TicksGame;
				}
			};
			bathe.tickAction = () =>
			{
				Building_DoubleBath bath = Bath;
				if (bath == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
				need?.clean(bath.cleanedPerTick);
				pawn.GainComfortFromCellIfPossible(1);
				if (bath.contamination > waterQuality)
				{
					waterQuality = bath.contamination;
				}
				if (need != null && need.CurLevel >= 0.999f)
				{
					completed = true;
					ReadyForNextToil();
				}
			};
			bathe.AddFinishAction(FinishBath);
			yield return bathe;
		}

		private Toil MakeInviteToil()
		{
			Toil toil = ToilMaker.MakeToil("CK_InviteBathPartner");
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			toil.initAction = () =>
			{
				if (!IsInviter)
				{
					return;
				}
				Building_DoubleBath bath = Bath;
				Pawn partner = Partner;
				if (!Building_DoubleBath.CanInvite(pawn, partner, bath))
				{
					return;
				}
				JobDef def = Building_DoubleBath.TogetherJobDef;
				if (def == null)
				{
					return;
				}
				// 被邀请者直接接管一份同缸 Job；不想被打断的情况已在 CanInvite 里排除。
				partner.jobs.StartJob(JobMaker.MakeJob(def, bath, pawn), JobCondition.InterruptForced);
			};
			return toil;
		}

		private void FinishBath()
		{
			Building_DoubleBath bath = Bath;
			if (bath == null)
			{
				return;
			}
			bool last = !bath.AnyOtherBather(pawn);
			bool waterCold = bath.IsCold;
			if (last)
			{
				bath.occupiedSinceTick = -1;
			}
			if (!completed)
			{
				return;
			}
			pawn.filth?.CarriedFilthListForReading?.Clear();
			SanitationUtil.ApplyBathroomThought(pawn, bath);
			SanitationUtil.WaterTempCheck(pawn, waterCold, true);
			// 双人共浴的专属心情：+24，持续 2 天（两人各自的 Job 完成时都会拿到）。
			ThoughtDef together = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_BathTogether");
			if (together != null)
			{
				pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(together);
			}
			pawn.needs?.TryGetNeed<Need_Hygiene>()?.CleanContamination();
			SanitationUtil.ContaminationCheckWater(pawn, waterQuality);
			if (last)
			{
				SanitationUtil.CheckForBlockage(bath);
				bath.TryPullPlug();
			}
		}
	}
}
