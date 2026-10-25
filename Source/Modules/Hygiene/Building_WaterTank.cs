using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	public class Building_WaterTank : Building, IWaterStore
	{
		public const float CapacityValue = 500f;

		private float waterStored;

		public float WaterStored
		{
			get => waterStored;
			set => waterStored = Mathf.Clamp(value, 0f, CapacityValue);
		}

		public float Capacity => CapacityValue;

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
			return text + "CK.Hygiene.WaterLevel".Translate(WaterStored.ToString("F0"), CapacityValue.ToString("F0"));
		}
	}
}
