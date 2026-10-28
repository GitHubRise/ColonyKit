using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public enum BoilerMode
	{
		Low,
		Normal,
		High
	}

	public class CompProperties_WaterHeater : CompProperties
	{
		/// <summary>Boiler heating capacity, matching DBH's default 250.</summary>
		public float Capacity = 250f;

		/// <summary>
		/// DBH 的 ThermostatControl：电/燃气锅炉按热水罐温度自动启停；
		/// 柴火锅炉设成 false，只要烧着就一直出力。
		/// </summary>
		public bool ThermostatControl = true;

		public CompProperties_WaterHeater()
		{
			compClass = typeof(CompWaterHeater);
		}
	}

	/// <summary>DBH 热水子集：供热能力、功率档位与热水罐恒温控制。</summary>
	public class CompWaterHeater : ThingComp
	{
		private const float ThermostatOnTemp = 0.85f;
		private const float ThermostatOffTemp = 0.98f;

		private CompFlickable flicker;
		private CompPowerTrader power;
		private CompRefuelable fuel;
		private CompBreakdownable breakdown;
		private float basePowerOutput;
		private bool thermostatOn;

		public BoilerMode mode = BoilerMode.Normal;

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

		public float ModeMultiplier
		{
			get
			{
				switch (mode)
				{
					case BoilerMode.Low: return 0.5f;
					case BoilerMode.High: return 1.5f;
					default: return 1f;
				}
			}
		}

		public bool Running => WorkingNow && (thermostatOn || !Props.ThermostatControl);

		public virtual float Capacity => Running ? Props.Capacity * ModeMultiplier : 0f;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			flicker = parent.GetComp<CompFlickable>();
			power = parent.GetComp<CompPowerTrader>();
			fuel = parent.GetComp<CompRefuelable>();
			breakdown = parent.TryGetComp<CompBreakdownable>();
			basePowerOutput = power?.PowerOutput ?? 0f;
			if (Mathf.Approximately(basePowerOutput, 0f))
			{
				basePowerOutput = -250f;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref mode, "boilerMode", BoilerMode.Normal);
		}

		public override void CompTick()
		{
			base.CompTick();
			RefreshThermostat();
			UpdatePowerOutput();
			// 烧柴/烧油锅炉要真的烧燃料（CompRefuelable 只在被通知时扣）。
			if (fuel != null && Running)
			{
				fuel.Notify_UsedThisTick();
			}
		}

		/// <summary>热水罐达到恒温上限后停炉，低于下限再启动；LowCap 会强制启动。</summary>
		private void RefreshThermostat()
		{
			PlumbingNet net = parent.GetComp<CompPipe>()?.pipeNet;
			if (net == null)
			{
				thermostatOn = false;
				return;
			}
			bool any = false;
			float minTemp = 1f;
			for (int i = 0; i < net.HotWaterTanks.Count; i++)
			{
				CompHeatStore tank = net.HotWaterTanks[i];
				if (tank.GetStoreCapacity <= 0f)
				{
					continue;
				}
				any = true;
				minTemp = Mathf.Min(minTemp, tank.HeaterTemp);
				if (tank.LowCap)
				{
					thermostatOn = true;
					return;
				}
			}
			if (!any)
			{
				thermostatOn = false;
				return;
			}
			if (thermostatOn)
			{
				if (minTemp >= ThermostatOffTemp)
				{
					thermostatOn = false;
				}
			}
			else if (minTemp <= ThermostatOnTemp)
			{
				thermostatOn = true;
			}
		}

		private void UpdatePowerOutput()
		{
			if (power == null)
			{
				return;
			}
			float target = WorkingNow && thermostatOn
				? basePowerOutput * ModeMultiplier
				: 0f;
			if (!Mathf.Approximately(power.PowerOutput, target))
			{
				power.PowerOutput = target;
			}
		}

		public override string CompInspectStringExtra()
		{
			string status = !WorkingNow
				? "CK.Hygiene.HeaterOff".Translate()
				: (Running ? "CK.Hygiene.HeaterOn".Translate() : "CK.Hygiene.HeaterStandby".Translate());
			return status + "\n" + "CK.Hygiene.BoilerModeLine".Translate(ModeLabel(mode));
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.BoilerModeButton".Translate(ModeLabel(mode)),
				defaultDesc = "CK.Hygiene.BoilerModeButtonDesc".Translate(),
				action = () =>
				{
					mode = mode == BoilerMode.Low ? BoilerMode.Normal
						: mode == BoilerMode.Normal ? BoilerMode.High : BoilerMode.Low;
					UpdatePowerOutput();
				}
			};
		}

		private static string ModeLabel(BoilerMode mode)
		{
			switch (mode)
			{
				case BoilerMode.Low: return "CK.Hygiene.BoilerMode.Low".Translate();
				case BoilerMode.High: return "CK.Hygiene.BoilerMode.High".Translate();
				default: return "CK.Hygiene.BoilerMode.Normal".Translate();
			}
		}
	}
}
