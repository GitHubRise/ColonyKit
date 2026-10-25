using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 多墙体保温模块：让房间四周的连续墙体层数参与漏热计算。
	/// </summary>
	/// <remarks>
	/// 原版 <c>RoomTempTracker.WallEqualizationTempChangePerInterval</c> 只看房间边界外
	/// 第 2 格：那格是空地还是第二层墙，都按同一个“室外混合温度”结算，所以墙厚不影响保温。
	/// 本模块在 Harmony Postfix 里按实际连续层数乘一个保温系数，其余（屋顶/门/通风口）不动。
	/// </remarks>
	public sealed class InsulationModule : ColonyKitModule
	{
		public const string ModuleId = "insulation";

		public override string Id => ModuleId;

		public override string DisplayNameKey => "ColonyKit.Module.Insulation.Name";

		public override string DescriptionKey => "ColonyKit.Module.Insulation.Desc";

		public override int InitOrder => 110;
	}
}
