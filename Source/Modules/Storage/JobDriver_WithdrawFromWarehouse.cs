using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 出库 Job：走到立体库 → 从内部 ThingOwner 取出一堆 → 搬到目标库存格 → 放下。
	/// 出库目标由 <see cref="WorkGiver_WithdrawFromWarehouse"/> 选好，Job 只负责执行。
	/// </summary>
	public class JobDriver_WithdrawFromWarehouse : JobDriver
	{
		private const TargetIndex WarehouseInd = TargetIndex.A;
		private const TargetIndex CellInd = TargetIndex.B;
		private const TargetIndex ItemInd = TargetIndex.C;

		private Building_VerticalWarehouse Warehouse =>
			job.GetTarget(WarehouseInd).Thing as Building_VerticalWarehouse;

		private Thing Item => job.GetTarget(ItemInd).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(WarehouseInd), job, 1, 1, null, errorOnFailed))
			{
				return false;
			}
			if (!pawn.Reserve(job.GetTarget(CellInd), job, 1, 1, null, errorOnFailed))
			{
				return false;
			}
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(WarehouseInd);

			yield return Toils_Goto.GotoThing(WarehouseInd, PathEndMode.InteractionCell);

			Toil take = ToilMaker.MakeToil("TakeFromWarehouse");
			take.initAction = () =>
			{
				Building_VerticalWarehouse warehouse = Warehouse;
				Thing item = Item;
				if (warehouse == null || item == null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
					return;
				}

				// 库内堆可能超过全局上限；按全局上限切一块，剩余继续留在库里。
				int count = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.Min(item.stackCount, item.def.stackLimit));
				Thing taken = warehouse.Container.Take(item, count);
				if (taken == null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
					return;
				}

				// 库内物品是未生成的；先放到小人脚边再交给 CarryTracker，
				// 避免未生成 Thing 的 Position/TargetInfo 为空。
				if (!GenPlace.TryPlaceThing(taken, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing placed))
				{
					if (warehouse.Container is WarehouseThingOwner owner)
					{
						owner.TryAddForced(taken);
					}
					else
					{
						warehouse.Container.TryAdd(taken, false);
					}
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
					return;
				}

				if (!pawn.carryTracker.TryStartCarry(placed))
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
			};
			take.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return take;

			yield return Toils_Haul.CarryHauledThingToCell(CellInd);
			yield return Toils_Haul.PlaceHauledThingInCell(CellInd, null, storageMode: true);
		}
	}
}
