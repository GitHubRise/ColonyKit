using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 用脸盆在水井打水：脸盆在物品栏里就装水，不在就去地上找一只（自动取用），
	/// 持续 300 tick 把盆加满。
	/// </summary>
	public class JobDriver_FillBasin : JobDriver
	{
		private const int WorkTicks = 300;

		private const TargetIndex WellInd = TargetIndex.A;

		private const TargetIndex BasinInd = TargetIndex.B;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(WellInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			if (CarriedBasin(pawn) == null && job.GetTarget(BasinInd).HasThing)
			{
				return pawn.Reserve(job.GetTarget(BasinInd), job, 1, -1, null, errorOnFailed);
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(WellInd);

			int workDone = 0;
			Toil fill = ToilMaker.MakeToil("CK_FillBasin");
			fill.defaultCompleteMode = ToilCompleteMode.Never;
			fill.FailOnDespawnedNullOrForbidden(WellInd);
			fill.initAction = () => workDone = 0;
			fill.tickIntervalAction = delta =>
			{
				CompBasinWater basin = CarriedBasin(pawn);
				CompPipe pipe = (job.GetTarget(WellInd).Thing as ThingWithComps)?.GetComp<CompPipe>();
				if (basin == null || pipe?.pipeNet == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				basin.Water = System.Math.Min(basin.Props.Capacity, basin.Water + 0.1f * delta);
				workDone += delta;
				if (basin.IsFull || workDone >= WorkTicks)
				{
					ReadyForNextToil();
				}
			};
			fill.WithProgressBar(
				WellInd,
				() => (float)workDone / WorkTicks,
				interpolateBetweenActorAndTarget: true);

			Toil pickup = ToilMaker.MakeToil("CK_FillBasinPickup");
			pickup.initAction = () =>
			{
				Thing basin = job.GetTarget(BasinInd).Thing;
				if (basin == null || basin.Destroyed || !basin.Spawned)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				basin.DeSpawn();
				if (!pawn.inventory.innerContainer.TryAdd(basin))
				{
					GenPlace.TryPlaceThing(basin, pawn.Position, pawn.Map, ThingPlaceMode.Near);
					EndJobWith(JobCondition.Incompletable);
				}
			};

			Toil gotoWell = Toils_Goto.GotoThing(WellInd, PathEndMode.Touch);
			// 已经随身带着盆就跳过“去捡盆”，直接去井边。
			yield return Toils_Jump.JumpIf(gotoWell, () => CarriedBasin(pawn) != null);
			yield return Toils_Goto.GotoThing(BasinInd, PathEndMode.ClosestTouch);
			yield return pickup;
			yield return Toils_Jump.Jump(gotoWell);
			yield return gotoWell;
			yield return fill;
		}

		/// <summary>物品栏里还能装水的脸盆（优先非满的）。</summary>
		internal static CompBasinWater CarriedBasin(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			CompBasinWater fallback = null;
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater basin = thing.TryGetComp<CompBasinWater>();
				if (basin == null)
				{
					continue;
				}
				if (!basin.IsFull)
				{
					return basin;
				}
				fallback = basin;
			}
			return fallback;
		}

		/// <summary>物品栏里有水的脸盆（倒水用）。</summary>
		internal static CompBasinWater CarriedBasinWithWater(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				CompBasinWater basin = thing.TryGetComp<CompBasinWater>();
				if (basin != null && !basin.IsEmpty)
				{
					return basin;
				}
			}
			return null;
		}
	}
}
