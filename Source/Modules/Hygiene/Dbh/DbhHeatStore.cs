using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_HeatStore : CompProperties
	{
		public float FallRate = 0.00016f;
		public bool ForRunningWater;
		public float RiseRate = 0.0016f;
		public float StoreCapacity = 100f;

		public CompProperties_HeatStore()
		{
			compClass = typeof(CompHeatStore);
		}
	}

	/// <summary>
	/// DBH CompHeatStore 的移植：热水罐保存 0–1 的归一化水温，
	/// 由管网 HeatingCap 决定目标温度，洗澡时按 tick 扣除。
	/// </summary>
	public class CompHeatStore : ThingComp
	{
		private CompFlickable flicker;
		private CompPipe pipeComp;
		private float heaterTempInt;

		public CompProperties_HeatStore Props => (CompProperties_HeatStore)props;

		public bool ForRunningWater => Props.ForRunningWater;

		public bool LowCap => heaterTempInt < 0.25f;

		public float HeaterTemp
		{
			get => heaterTempInt;
			set => heaterTempInt = Mathf.Clamp01(value);
		}

		/// <summary>阀门关闭的热水罐不参与管网供热能力。</summary>
		public float GetStoreCapacity =>
			flicker == null || flicker.SwitchIsOn ? Props.StoreCapacity : 0f;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			pipeComp = parent.GetComp<CompPipe>();
			flicker = parent.GetComp<CompFlickable>();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref heaterTempInt, "WaterTemp", 0f);
		}

		public override void CompTick()
		{
			base.CompTick();
			float target = 0f;
			float maxDelta = Props.FallRate;
			PlumbingNet net = pipeComp?.pipeNet;
			if (net != null && (flicker == null || flicker.SwitchIsOn))
			{
				target = net.HeatingCap;
				if (heaterTempInt < target)
				{
					maxDelta = Props.RiseRate;
				}
			}
			heaterTempInt = Mathf.MoveTowards(heaterTempInt, target, maxDelta);
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.HotWaterTemp".Translate((heaterTempInt * 100f).ToString("0"));
		}
	}
}
