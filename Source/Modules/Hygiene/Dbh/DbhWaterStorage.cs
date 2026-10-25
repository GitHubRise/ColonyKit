using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterStorage : CompProperties
	{
		public float WaterStorageCap = 10000f;

		public CompProperties_WaterStorage()
		{
			compClass = typeof(CompWaterStorage);
		}
	}

	/// <summary>DBH CompWaterStorage 的水量/水质子集；进水由 PlumbingNet 的泵/井逻辑统一完成。</summary>
	public class CompWaterStorage : ThingComp
	{
		public float WaterStorage;
		public ContaminationLevel WaterQuality = ContaminationLevel.Untreated;
		private CompPowerTrader power;

		public CompProperties_WaterStorage Props => (CompProperties_WaterStorage)props;

		public float space => Mathf.Max(0f, Props.WaterStorageCap - WaterStorage);

		public override void CompTick()
		{
			base.CompTick();
			if (parent?.Map != null
				&& parent.Map.GetComponent<MapComponent_SewagePollution>()?.WaterPolluted == true)
			{
				WaterQuality = ContaminationLevel.Contaminated;
			}
		}

		public bool WorkingNow
		{
			get
			{
				if (!FlickUtility.WantsToBeOn(parent))
				{
					return false;
				}
				return power == null || power.PowerOn;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			power = parent.GetComp<CompPowerTrader>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref WaterStorage, "WaterStorage", 0f);
			Scribe_Values.Look(ref WaterQuality, "WaterQuality", ContaminationLevel.Untreated);
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.WaterLevel".Translate(WaterStorage.ToString("F0"), Props.WaterStorageCap.ToString("F0"));
		}
	}
}
