using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 1×1 双向履带：只沿建筑朝向/反向运输，不做转向；转向请用 1×1 转向盘。
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
		/// 自动方向优先级：
		/// 1. 站在立体库正中端口：默认朝仓库（入库）；
		/// 2. 上游履带正朝自己：沿用上游流向；
		/// 3. 只有一条轴线有邻居：横向朝东、纵向朝北；
		/// 4. 其他情况保留玩家旋转。
		/// </summary>
		private void AutoOrient()
		{
			if (!Spawned || Map == null)
			{
				return;
			}

			if (TryGetWarehousePort(out Rot4 away))
			{
				// 默认把端口履带朝向仓库，形成“入库”直线；反向 Gizmo 后变输出。
				Rotation = away.Opposite;
				return;
			}

			// 直线入库：若沿某条直线能走到立体库，且端口履带朝仓库，则整条线朝仓库。
			if (TryOrientInputLine())
			{
				return;
			}

			Rot4[] sides = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
			for (int i = 0; i < sides.Length; i++)
			{
				IntVec3 neighborCell = Position + sides[i].FacingCell;
				if (neighborCell.InBounds(Map)
					&& neighborCell.GetEdifice(Map) is Building_ConveyorBase upstream
					&& upstream != this
					&& upstream.OutputCell == Position)
				{
					Rotation = upstream.OutputDirection;
					return;
				}
			}

			bool north = HasNeighbor(Rot4.North);
			bool south = HasNeighbor(Rot4.South);
			bool east = HasNeighbor(Rot4.East);
			bool west = HasNeighbor(Rot4.West);

			if ((north || south) && !(east || west))
			{
				Rotation = Rot4.North;
			}
			else if ((east || west) && !(north || south))
			{
				Rotation = Rot4.East;
			}
		}

		private bool TryGetWarehousePort(out Rot4 away)
		{
			Rot4[] sides = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
			for (int i = 0; i < sides.Length; i++)
			{
				Rot4 side = sides[i];
				IntVec3 warehouseCell = Position + side.Opposite.FacingCell;
				if (warehouseCell.InBounds(Map)
					&& warehouseCell.GetEdifice(Map) is Building_VerticalWarehouse warehouse
					&& warehouse.IsPortCell(side, Position))
				{
					away = side;
					return true;
				}
			}
			away = Rot4.Invalid;
			return false;
		}

		/// <summary>沿直线找到立体库；若最后一段端口履带朝仓库，则本条履带也朝仓库。</summary>
		private bool TryOrientInputLine()
		{
			Rot4[] sides = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
			for (int i = 0; i < sides.Length; i++)
			{
				Rot4 dir = sides[i];
				IntVec3 cell = Position;
				Building_ConveyorBase lastConveyor = null;
				for (int step = 0; step < 32; step++)
				{
					cell += dir.FacingCell;
					if (!cell.InBounds(Map))
					{
						break;
					}

					Building edifice = cell.GetEdifice(Map);
					if (edifice is Building_VerticalWarehouse)
					{
						if (lastConveyor != null && lastConveyor.OutputDirection == dir)
						{
							Rotation = dir;
							return true;
						}
						break;
					}
					if (edifice is Building_ConveyorBase conveyor)
					{
						lastConveyor = conveyor;
						continue;
					}
					break;
				}
			}
			return false;
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
