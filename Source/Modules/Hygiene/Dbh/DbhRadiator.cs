using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_Radiator : CompProperties_HeatStore
	{
		/// <summary>每个加热周期能推出的热量。</summary>
		public float Energy = 21f;

		/// <summary>加热周期（tick）。</summary>
		public int HeatingRate = 250;

		public CompProperties_Radiator()
		{
			compClass = typeof(CompRadiator);
		}
	}

	/// <summary>
	/// DBH CompRadiator 的移植：吃热水管里的热量给房间升温。
	/// 温度控制用原版 CompTempControl（DBH 的 DBHCompTempControl 就是它的副本）；
	/// 没有温控器时按 21.8℃ 作为目标。
	/// </summary>
	public class CompRadiator : CompHeatStore
	{
		public CompTempControl tempControl;

		public CompPipe pipe;

		public float HeatUsed;

		public float targetTemperature = 21.8f;

		public new CompProperties_Radiator Props => (CompProperties_Radiator)props;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			tempControl = parent.GetComp<CompTempControl>();
			pipe = parent.GetComp<CompPipe>();
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!parent.Spawned || !parent.IsHashIntervalTick(Props.HeatingRate))
			{
				return;
			}
			float ambient = parent.AmbientTemperature;
			float factor = ambient < 19f ? 1f : (ambient > 120f ? 0f : Mathf.InverseLerp(120f, 20f, ambient));
			float energyLimit = Props.Energy * factor * HeaterTemp * 4.1666665f;
			if (Mathf.Approximately(energyLimit, 0f))
			{
				HeatUsed = 0f;
				return;
			}
			float target = tempControl?.targetTemperature ?? targetTemperature;
			HeatUsed = GenTemperature.ControlTemperatureTempChange(parent.Position, parent.Map, energyLimit, target);
			Room room = parent.GetRoom();
			if (!Mathf.Approximately(HeatUsed, 0f) && room != null)
			{
				room.Temperature += HeatUsed;
			}
		}

		public override string CompInspectStringExtra()
		{
			if (ParentHolder is MinifiedThing || !parent.Spawned)
			{
				return base.CompInspectStringExtra();
			}
			string text = "CK.Hygiene.RadiatorHeating".Translate(
				pipe?.pipeNet?.HeatStoreCapacitySum.ToString("F0") ?? "0",
				pipe?.pipeNet?.HeatingCap.ToStringPercent("0.0") ?? "0%");
			return text + "\n" + "CK.Hygiene.RadiatorCap".Translate(HeaterTemp.ToStringPercent("0.0"));
		}
	}
}
