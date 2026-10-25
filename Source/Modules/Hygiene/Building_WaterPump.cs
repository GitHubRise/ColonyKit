using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class Building_WaterPump : Building, IWaterStore
	{
		public const float CapacityValue = 1000f;
		public const float ProductionPerRare = 60f;

		private float waterStored;
		private CompPowerTrader power;

		public float WaterStored
		{
			get => waterStored;
			set => waterStored = Mathf.Clamp(value, 0f, CapacityValue);
		}

		public float Capacity => CapacityValue;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			power = GetComp<CompPowerTrader>();
		}

		public override void TickRare()
		{
			base.TickRare();
			if (power != null && !power.PowerOn)
			{
				return;
			}
			WaterStored += ProductionPerRare;
			WaterNetwork.FillConnectedTanks(this);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref waterStored, "waterStored", 0f);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "CK.Hygiene.WaterLevel".Translate(WaterStored.ToString("F0"), CapacityValue.ToString("F0"));
			if (power != null && !power.PowerOn)
			{
				text += "\n" + "CK.Hygiene.PumpNoPower".Translate();
			}
			return text;
		}
	}
}
