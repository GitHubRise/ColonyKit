using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 喝水桶/脸盆里的水：可以是背包里随身带的，也可以是地上放着的（job.targetA 指定）。
	/// 每 tick 喝一点、按喝掉的水量 × 每升解渴补水，喝完或解渴就结束；收尾按水质做污染判定。
	/// </summary>
	public class JobDriver_DrinkFromCarried : JobDriver
	{
		private const float LitersPerTick = 0.02f;

		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		private bool drank;

		private CompBasinWater TargetContainer =>
			job.GetTarget(TargetIndex.A).Thing?.TryGetComp<CompBasinWater>();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterQuality, "waterQuality", ContaminationLevel.Treated);
			Scribe_Values.Look(ref drank, "drank", false);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!job.GetTarget(TargetIndex.A).HasThing)
			{
				return true;
			}
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (job.GetTarget(TargetIndex.A).HasThing)
			{
				this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
				yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
			}
			Toil drink = ToilMaker.MakeToil("CK_DrinkFromCarried");
			drink.defaultCompleteMode = ToilCompleteMode.Never;
			drink.tickIntervalAction = delta =>
			{
				CompBasinWater container = TargetContainer
					?? WaterContainerUtility.CarriedContainer(pawn, requireWater: true);
				Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
				if (container == null || thirst == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				float taken = container.Drain(Mathf.Min(LitersPerTick * delta, container.Water));
				if (taken <= 0.0001f)
				{
					ReadyForNextToil();
					return;
				}
				drank = true;
				if (container.Contamination > waterQuality)
				{
					waterQuality = container.Contamination;
				}
				thirst.Drink(taken * MineralWaterUtility.ThirstPerLiter);
				if (thirst.CurLevel >= 0.999f || container.IsEmpty)
				{
					ReadyForNextToil();
				}
			};
			drink.AddFinishAction(() =>
			{
				if (drank)
				{
					SanitationUtil.ContaminationCheckDrinking(pawn, waterQuality);
					HygieneUtility.AddDrinkSatiety(pawn);
				}
			});
			yield return drink;
		}
	}
}
