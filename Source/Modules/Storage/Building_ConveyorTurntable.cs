using Verse;

namespace ColonyKit.Modules.Storage
{
	/// <summary>
	/// 1×1 转向盘：物品可从四边任意方向进入，按建筑朝向/反向决定出口，实现直角拐弯。
	/// 与双向履带的区别是它可以接四边来货；出口仍由 Rotation 决定。
	/// </summary>
	public class Building_ConveyorTurntable : Building_ConveyorBase
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
