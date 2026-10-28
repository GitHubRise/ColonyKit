using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>把脏衣服（主要是死人衣服）塞进洗衣机（DBH JobDriver_WashingMachineLoad 的移植）。</summary>
	public class JobDriver_WashingMachineLoad : JobDriver
	{
		private const TargetIndex WasherInd = TargetIndex.A;

		private const TargetIndex ClothesInd = TargetIndex.B;

		public Building_WashingMachine Washer => job.GetTarget(WasherInd).Thing as Building_WashingMachine;

		public Thing Clothes => job.GetTarget(ClothesInd).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(WasherInd);
			this.FailOnBurningImmobile(WasherInd);
			AddFailCondition(() => Washer == null || !Washer.HasSpace);
			yield return Toils_Reserve.Reserve(WasherInd);
			Toil reserveClothes = Toils_Reserve.Reserve(ClothesInd);
			yield return reserveClothes;
			yield return Toils_Goto.GotoThing(ClothesInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(ClothesInd)
				.FailOnSomeonePhysicallyInteracting(ClothesInd);
			yield return Toils_Haul.StartCarryThing(ClothesInd)
				.FailOnDestroyedNullOrForbidden(ClothesInd);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveClothes, ClothesInd, TargetIndex.None);
			yield return Toils_Goto.GotoThing(WasherInd, PathEndMode.Touch);
			yield return Toils_General.Wait(10)
				.FailOnDestroyedNullOrForbidden(ClothesInd)
				.FailOnDestroyedNullOrForbidden(WasherInd);
			yield return new Toil
			{
				initAction = () =>
				{
					Washer?.LoadWashing(Clothes);
					pawn.carryTracker.innerContainer.Remove(Clothes);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
