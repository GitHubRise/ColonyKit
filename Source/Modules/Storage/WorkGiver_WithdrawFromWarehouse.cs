using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 出库：把立体库内的物品送到更高优先级的库存区。
	/// 立体库默认 Low 优先级，所以普通库存区会被视为「更好」而自动取货；
	/// 如果玩家把仓库优先级调高，出库会停止，避免和入库互相打架。
	/// </summary>
	public class WorkGiver_WithdrawFromWarehouse : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

		public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_VerticalWarehouse>();
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			if (!(thing is Building_VerticalWarehouse warehouse)
				|| !warehouse.HasPower
				|| !warehouse.HasAnyConveyorConnection
				|| !warehouse.Spawned
				|| warehouse.Container.Count == 0)
			{
				return false;
			}

			if (!pawn.CanReserveAndReach(
				warehouse,
				PathEndMode.InteractionCell,
				Danger.Deadly,
				1,
				1,
				null,
				forced))
			{
				return false;
			}

			return FindWithdrawable(pawn, warehouse, out _, out _);
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			Building_VerticalWarehouse warehouse = thing as Building_VerticalWarehouse;
			if (warehouse == null
				|| !FindWithdrawable(pawn, warehouse, out Thing stored, out IntVec3 cell))
			{
				return null;
			}

			return JobMaker.MakeJob(
				CKDefOf.CK_WithdrawFromWarehouse,
				warehouse,
				cell,
				stored);
		}

		private static bool FindWithdrawable(
			Pawn pawn,
			Building_VerticalWarehouse warehouse,
			out Thing stored,
			out IntVec3 cell)
		{
			stored = null;
			cell = IntVec3.Invalid;

			StoragePriority current = warehouse.GetStoreSettings().Priority;
			foreach (Thing candidate in warehouse.Container)
			{
				if (candidate == null || candidate.Destroyed || candidate.stackCount <= 0)
				{
					continue;
				}

				if (!StoreUtility.TryFindBestBetterStoreCellFor(
					candidate,
					pawn,
					pawn.Map,
					current,
					pawn.Faction,
					out IntVec3 destination))
				{
					continue;
				}

				if (!pawn.CanReach(destination, PathEndMode.OnCell, Danger.Deadly))
				{
					continue;
				}

				stored = candidate;
				cell = destination;
				return true;
			}
			return false;
		}
	}
}
