using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterPumpingStation : CompProperties
	{
		public float Capacity = 2000f;

		public CompProperties_WaterPumpingStation()
		{
			compClass = typeof(CompWaterPumpingStation);
		}
	}

	public class CompWaterPumpingStation : ThingComp
	{
		private CompBreakdownable breakdown;
		private CompFlickable flicker;
		private CompRefuelable fuel;
		private CompPowerTrader power;

		public CompProperties_WaterPumpingStation Props => (CompProperties_WaterPumpingStation)props;

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
			return "CK.Hygiene.PumpCapacity".Translate(Capacity.ToString("F0"));
		}
	}

	public class CompProperties_WaterInlet : CompProperties
	{
		public float Capacity = 2000f;
		public float Radius = 9f;
		public bool Deep;
		public bool Piped = true;

		public CompProperties_WaterInlet()
		{
			compClass = typeof(CompWaterInlet);
		}
	}

	public class CompWaterInlet : ThingComp
	{
		public float GroundWaterCapacity;
		public CompProperties_WaterInlet Props => (CompProperties_WaterInlet)props;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			GroundWaterCapacity = Props.Capacity;
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.GroundWater".Translate(GroundWaterCapacity.ToString("F0"));
		}
	}
}
