using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterFiltration : CompProperties
	{
		public CompProperties_WaterFiltration()
		{
			compClass = typeof(CompWaterFiltration);
		}
	}

	/// <summary>
	/// DBH CompWaterFiltration 的移植：接在水网上的净水设备。
	/// 只要它「开着、通电、没坏」，所在的整张水网拉出来的水都按已处理水算。
	/// </summary>
	public class CompWaterFiltration : ThingComp
	{
		public CompPowerTrader powerComp;

		public CompBreakdownable breakdownableComp;

		public bool WorkingNow
		{
			get
			{
				if (!FlickUtility.WantsToBeOn(parent))
				{
					return false;
				}
				if (powerComp != null && !powerComp.PowerOn)
				{
					return false;
				}
				return breakdownableComp == null || !breakdownableComp.BrokenDown;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.GetComp<CompPowerTrader>();
			breakdownableComp = parent.GetComp<CompBreakdownable>();
		}
	}
}
