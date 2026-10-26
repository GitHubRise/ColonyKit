using ColonyKit.Modules.Storage;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>搬运：优先送进立体库，其次送普通库存区；不搬尸体。</summary>
	public class JobGiver_NannyBotHaul : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Haul;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}

			if (NannyBotUtility.Settings.storage)
			{
				Job warehouseJob = TryWarehouseJob(pawn);
				if (warehouseJob != null)
				{
					return warehouseJob;
				}
			}

			Thing thing = GenClosest.ClosestThing_Global_Reachable(
				pawn.Position,
				pawn.Map,
				pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
				PathEndMode.ClosestTouch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing candidate) => CanHaul(pawn, candidate));

			if (thing == null)
			{
				return null;
			}

			Job job = HaulAIUtility.HaulToStorageJob(pawn, thing, false);
			if (job == null)
			{
				return null;
			}
			job.expiryInterval = 5000;
			return job;
		}

		private static Job TryWarehouseJob(Pawn pawn)
		{
			WorkGiver_HaulToWarehouse giver = new WorkGiver_HaulToWarehouse();
			Thing thing = GenClosest.ClosestThing_Global_Reachable(
				pawn.Position,
				pawn.Map,
				pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
				PathEndMode.ClosestTouch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing candidate) => CanHaul(pawn, candidate)
					&& giver.HasJobOnThing(pawn, candidate));

			if (thing == null)
			{
				return null;
			}

			Job job = giver.JobOnThing(pawn, thing);
			if (job != null)
			{
				job.expiryInterval = 5000;
			}
			return job;
		}

		private static bool CanHaul(Pawn pawn, Thing thing)
		{
			if (thing == null || thing is Corpse || thing.Destroyed || !thing.Spawned)
			{
				return false;
			}
			if (!NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
			{
				return false;
			}
			return HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing, false);
		}
	}
}
