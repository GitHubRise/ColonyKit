using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 直接装一瓶 1 L 矿泉水：不需要水桶/脸盆，装满一瓶就完成一次账单。
	/// 在免费取水点装满即完成（不耗水）；在水龙头装则从水网抽走 1 升。
	/// </summary>
	public class JobDriver_FillWaterBottle : JobDriver
	{
		private const TargetIndex SpotInd = TargetIndex.A;

		private const int FillTicks = 60;

		private int remainingTicks;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(SpotInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(SpotInd);
			yield return Toils_Goto.GotoThing(SpotInd, PathEndMode.Touch);

			Toil fill = ToilMaker.MakeToil("CK_FillWaterBottle");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.initAction = () => remainingTicks = FillTicks;
			fill.tickIntervalAction = delta =>
			{
				remainingTicks -= delta;
				if (remainingTicks > 0)
				{
					return;
				}
				// 水龙头要从水网里真的抽 1 升；免费取水点装满即可，不耗水。
				Building_WaterPoint tap = job.GetTarget(SpotInd).Thing as Building_WaterPoint;
				if (tap != null && !tap.TryDrawMineralBottle(out _))
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MineralWater");
				if (def == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				Thing bottle = ThingMaker.MakeThing(def);
				if (pawn.inventory?.innerContainer == null
					|| !pawn.inventory.innerContainer.TryAdd(bottle))
				{
					GenPlace.TryPlaceThing(bottle, pawn.Position, pawn.Map, ThingPlaceMode.Near);
				}
				if (job.bill != null)
				{
					job.bill.Notify_IterationCompleted(pawn, null);
				}
				ReadyForNextToil();
			};
			fill.WithProgressBar(
				SpotInd,
				() => 1f - (float)remainingTicks / FillTicks,
				interpolateBetweenActorAndTarget: true);
			yield return fill;
		}
	}
}
