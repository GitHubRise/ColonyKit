using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// “Mk-V 专属武器”加成：只有 Mk-V 拿着带这个扩展的武器时才生效，其他种族/型号照常
	/// 使用武器本身的面板数值。目前挂在电弧发射器上，基础射程 36 格，Mk-V 手上再加 15 格。
	/// </summary>
	public class DefModExtension_MkvExclusiveWeapon : DefModExtension
	{
		/// <summary>Mk-V 持有时额外增加的射程（格）。</summary>
		public float rangeBonus;
	}
}
