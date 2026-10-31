using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 在带净水器的水龙头直接装一瓶 1 L 纯净水：不需要容器，消耗 1 L 已过滤的水。
	/// </summary>
	public class JobDriver_FillPurifiedWater : JobDriver
	{
		private const TargetIndex TapInd = TargetIndex.A;

		private const int FillTicks = 60;

		private int remainingTicks;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TapInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TapInd);
			yield return Toils_Goto.GotoThing(TapInd, PathEndMode.Touch);

			Toil fill = ToilMaker.MakeToil("CK_FillPurifiedWater");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.initAction = () => remainingTicks = FillTicks;
			fill.tickIntervalAction = delta =>
			{
				remainingTicks -= delta;
				if (remainingTicks > 0)
				{
					return;
				}
				Building_WaterPoint point = job.GetTarget(TapInd).Thing as Building_WaterPoint;
				if (point == null || !point.TryDrawPurifiedBottle(out _))
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("CK_PurifiedWater");
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
				ReadyForNextToil();
			};
			fill.WithProgressBar(
				TapInd,
				() => 1f - (float)remainingTicks / FillTicks,
				interpolateBetweenActorAndTarget: true);
			yield return fill;
		}
	}
}
