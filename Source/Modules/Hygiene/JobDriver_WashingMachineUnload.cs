using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>洗完把衣服取出来送进仓库（DBH JobDriver_WashingMachineUnload 的移植）。</summary>
	public class JobDriver_WashingMachineUnload : JobDriver
	{
		private const TargetIndex WasherInd = TargetIndex.A;

		private const TargetIndex ClothesInd = TargetIndex.B;

		private const TargetIndex StorageInd = TargetIndex.C;

		public Building_WashingMachine Washer => job.GetTarget(WasherInd).Thing as Building_WashingMachine;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(WasherInd);
			this.FailOnBurningImmobile(WasherInd);
			yield return Toils_Reserve.Reserve(WasherInd);
			yield return Toils_Goto.GotoThing(WasherInd, PathEndMode.Touch);
			yield return Toils_General.Wait(10)
				.FailOnDestroyedNullOrForbidden(WasherInd)
				.FailOnCannotTouch(WasherInd, PathEndMode.Touch)
				.FailOn(() => Washer == null || !Washer.MustUnload);
			yield return new Toil
			{
				initAction = () =>
				{
					Thing thing = Washer?.UnloadWashing();
					if (thing == null)
					{
						EndJobWith(JobCondition.Incompletable);
						return;
					}
					StoragePriority priority = StoreUtility.CurrentStoragePriorityOf(thing);
					if (StoreUtility.TryFindBestBetterStoreCellFor(
						thing, pawn, Map, priority, pawn.Faction, out IntVec3 cell))
					{
						job.SetTarget(StorageInd, cell);
						job.SetTarget(ClothesInd, thing);
						job.count = thing.stackCount;
					}
					else
					{
						EndJobWith(JobCondition.Incompletable);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return Toils_Reserve.Reserve(ClothesInd);
			yield return Toils_Reserve.Reserve(StorageInd);
			yield return Toils_Goto.GotoThing(ClothesInd, PathEndMode.ClosestTouch);
			yield return Toils_Haul.StartCarryThing(ClothesInd);
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageInd);
			yield return carryToCell;
			yield return Toils_Haul.PlaceHauledThingInCell(StorageInd, carryToCell, storageMode: true);
		}
	}
}
