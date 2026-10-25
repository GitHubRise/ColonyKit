using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>DBH Building_PowerShower：电加热，洗得更快，耗水更多。</summary>
	public class Building_PowerShower : Building_Shower
	{
		public override float HeatUsedPerTick => 0f;

		public override float CleanedPerTick => 0.004f;

		public override float WaterUsedPerTick => 0.4f;

		public override FixtureQuality Quality => FixtureQuality.fine;
	}
}
