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

		protected override bool TryNextCell(IntVec3 from, out IntVec3 next, out bool leavesConveyor)
		{
			next = OutputCell;
			leavesConveyor = true;
			return next != from;
		}
	}
}
