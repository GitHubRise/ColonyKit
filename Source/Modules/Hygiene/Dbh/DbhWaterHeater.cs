using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterHeater : CompProperties
	{
		/// <summary>Boiler heating capacity, matching DBH's default 250.</summary>
		public float Capacity = 250f;

		public CompProperties_WaterHeater()
		{
			compClass = typeof(CompWaterHeater);
		}
	}

	/// <summary>DBH 热水子集：通电/有燃料时给连通管网提供热水。</summary>
	public class CompWaterHeater : ThingComp
	{
		private CompFlickable flicker;
		private CompPowerTrader power;
		private CompRefuelable fuel;
		private CompBreakdownable breakdown;

		public CompProperties_WaterHeater Props => (CompProperties_WaterHeater)props;

		public bool WorkingNow
		{
			get
			{
				if (flicker != null && !flicker.SwitchIsOn) return false;
				if (power != null && !power.PowerOn) return false;
				if (fuel != null && !fuel.HasFuel) return false;
				if (breakdown != null && breakdown.BrokenDown) return false;
				return true;
			}
		}

		public float Capacity => WorkingNow ? Props.Capacity : 0f;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			flicker = parent.GetComp<CompFlickable>();
			power = parent.GetComp<CompPowerTrader>();
			fuel = parent.GetComp<CompRefuelable>();
			breakdown = parent.TryGetComp<CompBreakdownable>();
		}

		public override string CompInspectStringExtra()
		{
			return WorkingNow
				? "CK.Hygiene.HeaterOn".Translate()
				: "CK.Hygiene.HeaterOff".Translate();
		}
	}
}
