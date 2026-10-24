using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 2×2 双向转向履带：四边都能进，物品先对齐中央车道，再向建筑朝向的出口移动。
	/// 反向 Gizmo 切换出口到对侧。
	/// </summary>
	public class Building_ConveyorHub : Building_ConveyorBase
	{
		public override IntVec3 OutputCell
		{
			get
			{
				switch (OutputDirection.AsInt)
				{
					case 0:
						return Position + new IntVec3(1, 0, 2); // North
					case 1:
						return Position + new IntVec3(2, 0, 1); // East
					case 2:
						return Position + new IntVec3(1, 0, -1); // South
					default:
						return Position + new IntVec3(-1, 0, 1); // West
				}
			}
		}

		protected override bool TryNextCell(IntVec3 from, out IntVec3 next, out bool leavesConveyor)
		{
			leavesConveyor = false;
			next = from;

			int laneX = Position.x + 1;
			int laneZ = Position.z + 1;
			int dir = OutputDirection.AsInt;

			if (dir == 0 || dir == 2) // North / South
			{
				if (from.x != laneX)
				{
					next = new IntVec3(from.x + (laneX > from.x ? 1 : -1), 0, from.z);
				}
				else
				{
					next = new IntVec3(from.x, 0, from.z + (dir == 0 ? 1 : -1));
				}
			}
			else // East / West
			{
				if (from.z != laneZ)
				{
					next = new IntVec3(from.x, 0, from.z + (laneZ > from.z ? 1 : -1));
				}
				else
				{
					next = new IntVec3(from.x + (dir == 1 ? 1 : -1), 0, from.z);
				}
			}

			leavesConveyor = !GenAdj.OccupiedRect(this).Contains(next);
			return next != from;
		}
	}
}
