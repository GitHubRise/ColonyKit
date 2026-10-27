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
	[StaticConstructorOnStartup]
	public class CompHeatStore : ThingComp
	{
		public static readonly Vector2 BarSize = new Vector2(0.7f, 0.2f);

		public static readonly Material HeatBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.red);

		public static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.2f, 0.2f));

		/// <summary>由 <see cref="HygienePipeMapComp"/> 在选中水网设施或铺水网时置位，画完即复位。</summary>
		public bool DrawOverlay;

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

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			// 和 DBH 一样：选中任意水网物件时，把全图热水罐/热水器的水温条一起亮出来。
			parent.Map?.GetComponent<HygienePipeMapComp>()?.RequestHeaterBars();
		}

		public override void PostDraw()
		{
			base.PostDraw();
			// 同 CompWaterStorage：选中热水罐/热水器本身时也画，不依赖 OnGUI 的置位。
			if (!DrawOverlay && !(Find.Selector != null && Find.Selector.IsSelected(parent)))
			{
				return;
			}
			GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
			{
				center = parent.DrawPos + Vector3.up * 0.1f,
				size = BarSize,
				fillPercent = heaterTempInt,
				filledMat = HeatBarFilledMat,
				unfilledMat = BarUnfilledMat,
				margin = 0.05f,
				rotation = Rot4.East
			});
			DrawOverlay = false;
		}
	}
}
