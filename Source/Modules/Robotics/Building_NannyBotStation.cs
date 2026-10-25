using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>充电桩建筑：充电时发光，供 CompGlower 查询。</summary>
	public class Building_NannyBotStation : Building, IThingGlower
	{
		public bool ShouldBeLitNow()
		{
			CompNannyBotStation comp = GetComp<CompNannyBotStation>();
			return comp != null
				&& comp.IsCharging
				&& NannyBotUtility.Settings.chargingGlow;
		}
	}
}
