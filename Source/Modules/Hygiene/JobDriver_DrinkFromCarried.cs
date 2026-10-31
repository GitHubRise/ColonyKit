using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 直接喝随身容器（水桶/脸盆）里的水：口渴的小人不用跑回洗手池，
	/// 每 tick 喝一点、消耗一点水，喝完或解渴就结束；收尾按水质做污染判定。
	/// </summary>
	public class JobDriver_DrinkFromCarried : JobDriver
	{
		private const float LitersPerTick = 0.02f;

		private ContaminationLevel waterQuality = ContaminationLevel.Treated;

		private bool drank;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterQuality, "waterQuality", ContaminationLevel.Treated);
			Scribe_Values.Look(ref drank, "drank", false);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil drink = ToilMaker.MakeToil("CK_DrinkFromCarried");
			drink.defaultCompleteMode = ToilCompleteMode.Never;
			drink.tickIntervalAction = delta =>
			{
				CompBasinWater container = WaterContainerUtility.CarriedContainer(pawn, requireWater: true);
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
				thirst.Drink(0.006f * delta);
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
