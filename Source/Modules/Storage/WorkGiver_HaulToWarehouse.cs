using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 入库：把地上待搬运的物品送进立体库。
	/// 只在没有更高优先级库存区可放时接手，避免和原版搬运抢货；
	/// 实际搬运/存入复用原版 <see cref="JobDefOf.HaulToContainer"/>。
	/// </summary>
	public class WorkGiver_HaulToWarehouse : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

		public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			if (thing == null || !thing.Spawned || thing.IsForbidden(pawn) || thing.stackCount <= 0)
			{
				return false;
			}

			if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced))
			{
				return false;
			}

			Building_VerticalWarehouse warehouse = FindWarehouse(pawn, thing);
			if (warehouse == null)
			{
				return false;
			}

			// 普通库存区只要优先级不低于仓库，就让原版搬运优先；
			// 仓库优先级更高（或库存区满了）时才由仓库接手。
			if (StoreUtility.TryFindBestBetterStoreCellFor(
				thing,
				pawn,
				pawn.Map,
				StoragePriority.Unstored,
				pawn.Faction,
				out IntVec3 stockpileCell))
			{
				SlotGroup group = stockpileCell.GetSlotGroup(pawn.Map);
				if (group != null && warehouse.GetStoreSettings().Priority <= group.Settings.Priority)
				{
					return false;
				}
			}

			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			Building_VerticalWarehouse warehouse = FindWarehouse(pawn, thing);
			if (warehouse == null)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, warehouse);
			job.count = thing.stackCount;
			return job;
		}

		private static Building_VerticalWarehouse FindWarehouse(Pawn pawn, Thing thing)
		{
			foreach (Building_VerticalWarehouse warehouse in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_VerticalWarehouse>())
			{
				if (warehouse.CanStore(thing)
					&& pawn.CanReserveAndReach(
						warehouse,
						PathEndMode.InteractionCell,
						Danger.Deadly,
						1,
						1,
						null,
						false))
				{
					return warehouse;
				}
			}
			return null;
		}
	}
}
