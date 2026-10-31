using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>在地表水喝水（DBH JobDriver_DrinkFromGround 的移植）：站在水边喝 1000 tick。</summary>
	public class JobDriver_DrinkFromGround : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil drink = new Toil
			{
				defaultDuration = 1000,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			drink.AddEndCondition(() =>
			{
				Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
				if (thirst == null)
				{
					return JobCondition.Incompletable;
				}
				return thirst.CurLevel < 1f ? JobCondition.Ongoing : JobCondition.Succeeded;
			});
			drink.tickAction = () =>
			{
				pawn.needs?.TryGetNeed<Need_Thirst>()?.Drink();
			};
			drink.AddFinishAction(() =>
			{
				// 直接喝地表水：按脚下格子的污染程度判定（CK 已经把污染网格搬过来了）。
				SanitationUtil.ContaminationFromCellForPawn(pawn, pawn.Position);
				HygieneUtility.AddDrinkSatiety(pawn);
			});
			yield return drink;
		}
	}
}
