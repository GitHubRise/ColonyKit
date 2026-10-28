using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 动物水槽（DBH Building_WaterTrough 的移植）：沿用摆放式水盆的「自带水 + 浮球补水 + 接雨水」逻辑，
	/// 只是标记成动物专用，找到它的人类不会去喝。
	/// </summary>
	public class Building_WaterTrough : Building_WaterTub
	{
		public override bool ForAnimalsOnly => true;
	}
}
