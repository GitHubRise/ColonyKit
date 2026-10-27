using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>把粪渣搬进堆肥厂（DBH JobDriver_LoadComposter 的移植）。</summary>
	public class JobDriver_LoadComposter : JobDriver
	{
		private const TargetIndex BarrelInd = TargetIndex.A;

		private const TargetIndex MaterialInd = TargetIndex.B;

		private const int Duration = 200;

		public Building_Composter Barrel => job.GetTarget(TargetIndex.A).Thing as Building_Composter;

		public Thing Material => job.GetTarget(TargetIndex.B).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Barrel, job, 1, -1, null, errorOnFailed)
				&& pawn.Reserve(Material, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(BarrelInd);
			this.FailOnBurningImmobile(BarrelInd);
			AddEndCondition(() => Barrel != null && Barrel.SpaceLeft > 0 ? JobCondition.Ongoing : JobCondition.Succeeded);
			yield return Toils_General.DoAtomic(() =>
			{
				job.count = Barrel?.SpaceLeft ?? 0;
			});
			Toil reserve = Toils_Reserve.Reserve(MaterialInd);
			yield return reserve;
			yield return Toils_Goto.GotoThing(MaterialInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(MaterialInd)
				.FailOnSomeonePhysicallyInteracting(MaterialInd);
			yield return Toils_Haul.StartCarryThing(MaterialInd, false, true)
				.FailOnDestroyedNullOrForbidden(MaterialInd);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(
				reserve, MaterialInd, TargetIndex.None, takeFromValidStorage: true);
			yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);
			yield return Toils_General.Wait(Duration)
				.FailOnDestroyedNullOrForbidden(MaterialInd)
				.FailOnDestroyedNullOrForbidden(BarrelInd)
				.FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
				.WithProgressBarToilDelay(BarrelInd);
			yield return new Toil
			{
				initAction = () => Barrel?.AddMaterial(Material),
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
