using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 1×1 双向履带：只支持正向/反向，不做转向；成本远低于 2×2 全向履带。
	/// 相邻的 1×1/2×2 履带可以互相接货，形成直线传送。
	/// </summary>
	public class Building_BidirectionalConveyor : Building_ConveyorBase
	{
		public override IntVec3 OutputCell => Position + OutputDirection.FacingCell;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			AutoOrient();
		}

		protected override void Tick()
		{
			AutoOrient();
			base.Tick();
		}

		/// <summary>
		/// 1×1 不转向，但允许按相邻履带自动对齐主轴：
		/// 横向一排自动朝东，纵向一排自动朝北；十字/拐角仍交给 2×2 全向履带。
		/// </summary>
		private void AutoOrient()
		{
			if (!Spawned || Map == null)
			{
				return;
			}

			bool north = HasNeighbor(Rot4.North);
			bool south = HasNeighbor(Rot4.South);
			bool east = HasNeighbor(Rot4.East);
			bool west = HasNeighbor(Rot4.West);

			if ((north || south) && !(east || west))
			{
				if (Rotation != Rot4.North)
				{
					Rotation = Rot4.North;
				}
			}
			else if ((east || west) && !(north || south))
			{
				if (Rotation != Rot4.East)
				{
					Rotation = Rot4.East;
				}
			}
		}

		private bool HasNeighbor(Rot4 dir)
		{
			IntVec3 cell = Position + dir.FacingCell;
			return cell.InBounds(Map) && cell.GetEdifice(Map) is Building_ConveyorBase;
		}

		protected override bool TryNextCell(IntVec3 from, out IntVec3 next, out bool leavesConveyor)
		{
			next = OutputCell;
			leavesConveyor = true;
			return next != from;
		}
	}
}
