using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>把腐熟好的有机肥取出来送进仓库（DBH JobDriver_UnloadComposter 的移植）。</summary>
	public class JobDriver_UnloadComposter : JobDriver
	{
		private const TargetIndex BarrelInd = TargetIndex.A;

		private const TargetIndex ProductInd = TargetIndex.B;

		private const TargetIndex StorageCellInd = TargetIndex.C;

		private const int Duration = 200;

		public Building_Composter Barrel => job.GetTarget(TargetIndex.A).Thing as Building_Composter;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Barrel, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(BarrelInd);
			this.FailOnBurningImmobile(BarrelInd);
			yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);
			yield return Toils_General.Wait(Duration)
				.FailOnDestroyedNullOrForbidden(BarrelInd)
				.FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
				.FailOn(() => Barrel == null || !Barrel.Fermented)
				.WithProgressBarToilDelay(BarrelInd);
			yield return new Toil
			{
				initAction = () =>
				{
					Thing product = Barrel?.TakeOutProduct();
					if (product == null)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					GenPlace.TryPlaceThing(product, pawn.Position, Map, ThingPlaceMode.Near);
					StoragePriority priority = StoreUtility.CurrentStoragePriorityOf(product);
					if (StoreUtility.TryFindBestBetterStoreCellFor(
						product, pawn, Map, priority, pawn.Faction, out IntVec3 cell))
					{
						job.SetTarget(TargetIndex.C, cell);
						job.SetTarget(TargetIndex.B, product);
						job.count = product.stackCount;
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return Toils_Reserve.Reserve(ProductInd);
			yield return Toils_Reserve.Reserve(StorageCellInd);
			yield return Toils_Goto.GotoThing(ProductInd, PathEndMode.ClosestTouch);
			yield return Toils_Haul.StartCarryThing(ProductInd);
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
			yield return carryToCell;
			yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carryToCell, storageMode: true);
		}
	}
}
