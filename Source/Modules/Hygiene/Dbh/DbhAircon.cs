using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_AirconBaseUnit : CompProperties
	{
		public float BaseCapacity = 100f;

		public int PowerModes = 8;

		public CompProperties_AirconBaseUnit()
		{
			compClass = typeof(CompAirconBaseUnit);
		}
	}

	public class CompProperties_CompAirconUnit : CompProperties
	{
		public float Capacity = 100f;

		public int CoolingRate = 250;

		public float energyPerSecond = -21f;

		public CompProperties_CompAirconUnit()
		{
			compClass = typeof(CompAirconIndoorUnit);
		}
	}

	/// <summary>
	/// 多联机空调室外机（DBH CompAirconBaseUnit 的移植）：功率档位决定制冷量，
	/// 室外机把热排到室外，室内机的「需求」通过同一张空气网汇总。
	/// </summary>
	public class CompAirconBaseUnit : ThingComp
	{
		public CompBreakdownable breakdownableComp;

		public float efficiency = 1f;

		public float FanRotation;

		public float FanSpeed;

		public bool LowPowerMode;

		public CompPowerTrader powerComp;

		public CompPipe PipeComp;

		public int PowerMode = 1;

		public CompProperties_AirconBaseUnit Props => (CompProperties_AirconBaseUnit)props;

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

		public float Capacity => WorkingNow ? Props.BaseCapacity * PowerMode * efficiency : 0f;

		public float PowerUsage => 200f * PowerMode;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			PipeComp = parent.GetComp<CompPipe>();
			powerComp = parent.GetComp<CompPowerTrader>();
			breakdownableComp = parent.GetComp<CompBreakdownable>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref PowerMode, "PowerMode", 1);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (powerComp == null)
			{
				return;
			}
			powerComp.PowerOutput = LowPowerMode ? -20f : -PowerUsage;
			if (!parent.IsHashIntervalTick(250))
			{
				return;
			}
			float ambient = parent.AmbientTemperature;
			float factor = ambient < 20f ? 1f : (ambient > 120f ? 0f : Mathf.InverseLerp(120f, 20f, ambient));
			efficiency = 1f - (1f / 130f) * Mathf.Max(0f, ambient - 30f);
			if (efficiency < 0f)
			{
				efficiency = 0f;
			}
			// 排热：把室内机搬出来的热量（按比例）排到室外。
			if (powerComp.PowerOn && !LowPowerMode && PipeComp?.pipeNet != null)
			{
				float energy = 21f * factor * PipeComp.pipeNet.CoolingCap * 4.1666665f * PowerMode;
				GenTemperature.PushHeat(parent.Position, parent.Map, energy);
				FanSpeed = Mathf.Lerp(FanSpeed, 1f + PowerMode, 0.2f);
			}
			else
			{
				FanSpeed = Mathf.Lerp(FanSpeed, 0f, 0.2f);
			}
			LowPowerMode = PipeComp?.pipeNet == null
				|| !PipeComp.pipeNet.Airvents.Any(v => v.FullPower);
			FanRotation = (FanRotation + FanSpeed) % 360f;
		}

		public override string CompInspectStringExtra()
		{
			if (PipeComp?.pipeNet == null)
			{
				return null;
			}
			return "CK.Hygiene.CoolingCapacity".Translate(
				PipeComp.pipeNet.IndoorUnitCapacitySum.ToString("F0"),
				PipeComp.pipeNet.OutdoorUnitCapacitySum.ToString("F0"),
				PipeComp.pipeNet.CoolingCap.ToStringPercent("0.0"))
				+ "\n" + "CK.Hygiene.CoolingMode".Translate(Props.BaseCapacity * PowerMode, PowerUsage);
		}

		public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo g in base.CompGetGizmosExtra())
			{
				yield return g;
			}
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.CoolingLower".Translate(),
				action = () =>
				{
					if (PowerMode > 1)
					{
						PowerMode--;
					}
				},
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower")
			};
			yield return new Command_Action
			{
				defaultLabel = "CK.Hygiene.CoolingRaise".Translate(),
				action = () =>
				{
					if (PowerMode < Props.PowerModes)
					{
						PowerMode++;
					}
				},
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise")
			};
		}
	}

	/// <summary>多联机空调室内机（DBH CompAirconIndoorUnit 的移植）：只报需求，真正制冷的是 CompRoomUnit。</summary>
	public class CompAirconIndoorUnit : ThingComp
	{
		public bool Blocked;

		public float CoolingUsed;

		public bool FullPower = true;

		public CompBreakdownable breakdownableComp;

		public CompPipe PipeComp;

		public CompPowerTrader powerComp;

		public CompThermostat Thermostat;

		public CompProperties_CompAirconUnit Props => (CompProperties_CompAirconUnit)props;

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

		public float Capacity => WorkingNow ? Props.Capacity : 0f;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.GetComp<CompPowerTrader>();
			PipeComp = parent.GetComp<CompPipe>();
			Thermostat = parent.GetComp<CompThermostat>();
			breakdownableComp = parent.GetComp<CompBreakdownable>();
		}
	}

	/// <summary>房间空调（DBH CompRoomUnit 的移植）：按管网 CoolingCap 给房间降温。</summary>
	public class CompRoomUnit : CompAirconIndoorUnit
	{
		public override void CompTick()
		{
			base.CompTick();
			if (!parent.IsHashIntervalTick(Props.CoolingRate) || PipeComp?.pipeNet == null || !WorkingNow)
			{
				return;
			}
			float energyLimit = Props.energyPerSecond * PipeComp.pipeNet.CoolingCap * 4.1666665f;
			float target = Thermostat?.targetTemperature ?? 21f;
			CoolingUsed = GenTemperature.ControlTemperatureTempChange(parent.Position, parent.Map, energyLimit, target);
			bool cooled = !Mathf.Approximately(CoolingUsed, 0f);
			if (cooled)
			{
				Room room = parent.Position.GetRoom(parent.Map);
				if (room != null)
				{
					room.Temperature += CoolingUsed;
				}
			}
			FullPower = cooled;
		}

		public override string CompInspectStringExtra()
		{
			if (PipeComp?.pipeNet == null)
			{
				return null;
			}
			StringBuilder builder = new StringBuilder();
			builder.Append("CK.Hygiene.CoolingCapacity".Translate(
				PipeComp.pipeNet.IndoorUnitCapacitySum.ToString("F0"),
				PipeComp.pipeNet.OutdoorUnitCapacitySum.ToString("F0"),
				PipeComp.pipeNet.CoolingCap.ToStringPercent("0.0")));
			if (PipeComp.pipeNet.CoolingCap < 1f)
			{
				builder.AppendLine();
				builder.Append("CK.Hygiene.LowCoolingCap".Translate());
			}
			builder.AppendLine();
			builder.Append("CK.Hygiene.CoolingUnits".Translate(Props.Capacity));
			return builder.ToString().TrimEndNewlines();
		}
	}
}
