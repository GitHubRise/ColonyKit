using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 从仓库里的 1 L 瓶装水往水袋里灌：每 30 tick 消耗 1 瓶、水袋 +1 L，
	/// 灌到 30 L（水袋默认目标）或瓶子用完为止。矿泉水按未处理水记，纯净水/凉白开按干净水记。
	/// </summary>
	public class JobDriver_FillWaterPouch : JobDriver
	{
		private const TargetIndex BottleInd = TargetIndex.A;

		private const int TicksPerLiter = 30;

		private int ticksUntilNext;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(BottleInd), job, 10, job.count, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(BottleInd);
			yield return Toils_Goto.GotoThing(BottleInd, PathEndMode.ClosestTouch);

			Toil fill = ToilMaker.MakeToil("CK_FillWaterPouch");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.tickIntervalAction = delta =>
			{
				if (WaterPouchUtility.GetWater(pawn) >= WaterPouchUtility.FillTarget - 0.01f)
				{
					ReadyForNextToil();
					return;
				}
				Thing stack = job.GetTarget(BottleInd).Thing;
				if (stack == null || stack.Destroyed || stack.stackCount <= 0)
				{
					ReadyForNextToil();
					return;
				}
				ticksUntilNext -= delta;
				if (ticksUntilNext > 0)
				{
					return;
				}
				ticksUntilNext = TicksPerLiter;
				Thing one = stack.SplitOff(1);
				if (one == null)
				{
					ReadyForNextToil();
					return;
				}
				ContaminationLevel quality = QualityOf(one);
				one.Destroy(DestroyMode.Vanish);
				WaterPouchUtility.AddWater(pawn, 1f, quality);
			};
			yield return fill;
		}

		private static ContaminationLevel QualityOf(Thing thing)
		{
			if (MineralWaterUtility.IsPurifiedWater(thing) || MineralWaterUtility.IsBoiledWater(thing))
			{
				return ContaminationLevel.Treated;
			}
			return ContaminationLevel.Untreated;
		}
	}
}
