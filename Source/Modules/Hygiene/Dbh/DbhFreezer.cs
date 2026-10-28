using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// 冷库机组（DBH CompFreezerUnit 的移植）：给「机组朝向内侧」的那间房降温到零下，
	/// 周围被挡住就不工作（和 DBH 一样检查两格 Impassable）。
	/// </summary>
	public class CompFreezerUnit : CompAirconIndoorUnit
	{
		public override void CompTick()
		{
			base.CompTick();
			if (!parent.IsHashIntervalTick(Props.CoolingRate) || PipeComp?.pipeNet == null || !WorkingNow)
			{
				return;
			}
			Blocked = false;
			IntVec3 inner = parent.Position - IntVec3.North.RotatedBy(parent.Rotation);
			IntVec3 side = parent.Position + IntVec3.East.RotatedBy(parent.Rotation) - IntVec3.North.RotatedBy(parent.Rotation);
			if (inner.Impassable(parent.Map) || side.Impassable(parent.Map))
			{
				Blocked = true;
			}
			if (Blocked)
			{
				return;
			}
			float energyLimit = Props.energyPerSecond * PipeComp.pipeNet.CoolingCap * 4.1666665f;
			float target = Thermostat?.targetTemperature ?? -4f;
			CoolingUsed = GenTemperature.ControlTemperatureTempChange(inner, parent.Map, energyLimit, target);
			bool cooled = !Mathf.Approximately(CoolingUsed, 0f);
			if (cooled)
			{
				Room room = inner.GetRoom(parent.Map);
				if (room != null)
				{
					room.Temperature += CoolingUsed;
				}
			}
			FullPower = powerComp != null && powerComp.PowerOn && cooled;
		}

		public override string CompInspectStringExtra()
		{
			if (PipeComp?.pipeNet == null)
			{
				return null;
			}
			StringBuilder builder = new StringBuilder();
			if (Blocked)
			{
				builder.Append("CK.Hygiene.FreezerBlocked".Translate());
				builder.AppendLine();
			}
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
