using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 从随身水袋喝水：每 tick 消耗 0.02 L、按「每升解渴」（默认 0.65）回复，
	/// 喝到不渴或水袋见底为止。水质按袋里最脏的一档做一次污染判定。
	/// </summary>
	public class JobDriver_DrinkFromPouch : JobDriver
	{
		private const float LitersPerTick = 0.02f;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil drink = ToilMaker.MakeToil("CK_DrinkFromPouch");
			drink.defaultCompleteMode = ToilCompleteMode.Never;
			drink.initAction = () =>
			{
				SanitationUtil.ContaminationCheckDrinking(pawn, WaterPouchUtility.GetQuality(pawn));
			};
			drink.tickIntervalAction = delta =>
			{
				Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
				float water = WaterPouchUtility.GetWater(pawn);
				if (thirst == null || thirst.CurLevel >= 0.98f || water < LitersPerTick)
				{
					ReadyForNextToil();
					return;
				}
				float liters = Mathf.Min(LitersPerTick * delta, water);
				WaterPouchUtility.TakeWater(pawn, liters);
				thirst.Drink(liters * MineralWaterUtility.ThirstPerLiter);
			};
			yield return drink;
		}
	}
}
