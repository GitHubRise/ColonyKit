using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 打水入库：在水龙头/免费取水点每 60 tick 灌 1 瓶 1 L 矿泉水，
	/// 灌到 job.count 瓶后走到仓库格把瓶子放进仓库。水龙头每瓶从水网抽 1 L；
	/// 免费取水点不耗水。
	/// </summary>
	public class JobDriver_BottleWaterToStockpile : JobDriver
	{
		private const TargetIndex SourceInd = TargetIndex.A;

		private const TargetIndex StoreInd = TargetIndex.B;

		private const int FillTicks = 60;

		private int remainingTicks = FillTicks;

		private int filled;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(SourceInd), job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(SourceInd);
			yield return Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch);

			Toil fill = ToilMaker.MakeToil("CK_BottleWaterFill");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.tickIntervalAction = delta =>
			{
				int wanted = Mathf.Max(1, job.count);
				if (filled >= wanted)
				{
					ReadyForNextToil();
					return;
				}
				remainingTicks -= delta;
				if (remainingTicks > 0)
				{
					return;
				}
				remainingTicks = FillTicks;
				if (!TryDrawOne(job.GetTarget(SourceInd).Thing))
				{
					ReadyForNextToil();
					return;
				}
				ThingDef def = MineralWaterUtility.Def;
				if (def == null)
				{
					ReadyForNextToil();
					return;
				}
				Thing bottle = ThingMaker.MakeThing(def);
				if (pawn.inventory?.innerContainer == null
					|| !pawn.inventory.innerContainer.TryAdd(bottle))
				{
					GenPlace.TryPlaceThing(bottle, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					ReadyForNextToil();
					return;
				}
				filled++;
			};
			fill.WithProgressBar(
				SourceInd,
				() => job.count <= 0 ? 1f : Mathf.Clamp01((float)filled / Mathf.Max(1, job.count)),
				interpolateBetweenActorAndTarget: true);
			yield return fill;

			if (filled <= 0)
			{
				yield break;
			}

			yield return Toils_Goto.GotoCell(StoreInd, PathEndMode.Touch);

			Toil drop = ToilMaker.MakeToil("CK_BottleWaterDrop");
			drop.defaultCompleteMode = ToilCompleteMode.Instant;
			drop.initAction = DropBottles;
			yield return drop;
		}

		private void DropBottles()
		{
			ThingDef def = MineralWaterUtility.Def;
			if (def == null || pawn.inventory?.innerContainer == null)
			{
				ReadyForNextToil();
				return;
			}
			IntVec3 cell = job.GetTarget(StoreInd).Cell;
			if (!cell.IsValid || !cell.InBounds(pawn.Map))
			{
				cell = pawn.Position;
			}
			List<Thing> bottles = new List<Thing>();
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				if (thing.def == def)
				{
					bottles.Add(thing);
				}
			}
			for (int i = 0; i < bottles.Count; i++)
			{
				Thing bottle = bottles[i];
				if (pawn.inventory.innerContainer.TryDrop(
					bottle, cell, pawn.Map, ThingPlaceMode.Near, bottle.stackCount, out _))
				{
					continue;
				}
				pawn.inventory.innerContainer.TryDrop(
					bottle, pawn.Position, pawn.Map, ThingPlaceMode.Near, bottle.stackCount, out _);
			}
		}

		private bool TryDrawOne(Thing source)
		{
			if (source is Building_WaterPoint point)
			{
				return point.TryDrawMineralBottle(out _);
			}
			return source is Building_WaterSpot;
		}
	}
}
