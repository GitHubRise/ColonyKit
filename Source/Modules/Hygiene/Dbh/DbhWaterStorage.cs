using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterStorage : CompProperties
	{
		public float WaterStorageCap = 10000f;

		/// <summary>DBH：不靠管网、自己产水（雨水桶等）。</summary>
		public bool Auto;

		/// <summary>下雨时自动接水（雨水桶）。</summary>
		public bool AutoOnRain;

		/// <summary>每个 Tickrate 产多少水；AutoOnRain 时再乘雨量。</summary>
		public float AutoGenRate = 1f;

		public int Tickrate = 60;

		public CompProperties_WaterStorage()
		{
			compClass = typeof(CompWaterStorage);
		}
	}

	/// <summary>DBH CompWaterStorage 的水量/水质子集；进水由 PlumbingNet 的泵/井逻辑统一完成。</summary>
	[StaticConstructorOnStartup]
	public class CompWaterStorage : ThingComp
	{
		public static readonly Material WaterBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.cyan);

		public static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.2f, 0.2f));

		public static readonly Vector2 BarSize = new Vector2(0.7f, 0.2f);

		public float WaterStorage;
		public ContaminationLevel WaterQuality = ContaminationLevel.Untreated;

		/// <summary>由 <see cref="HygienePipeMapComp"/> 在选中水网设施或铺管时置位，画完即复位。</summary>
		public bool DrawOverlay;

		private CompPowerTrader power;

		public CompProperties_WaterStorage Props => (CompProperties_WaterStorage)props;

		public float space => Mathf.Max(0f, Props.WaterStorageCap - WaterStorage);

		public float CapPercent => Props.WaterStorageCap <= 0f
			? 0f
			: Mathf.Clamp01(WaterStorage / Props.WaterStorageCap);

		public override void CompTick()
		{
			base.CompTick();
			if (parent?.Map == null || ParentHolder is MinifiedThing)
			{
				return;
			}
			// DBH 的接雨水：下雨时按雨量给桶里加水（雨水桶只有这个进水途径）。
			float rainRate = parent.Map.weatherManager?.RainRate ?? 0f;
			if ((Props.Auto || (Props.AutoOnRain && rainRate > 0.01f))
				&& parent.IsHashIntervalTick(Mathf.Max(1, Props.Tickrate))
				&& WorkingNow)
			{
				float gain = Props.AutoGenRate;
				if (Props.AutoOnRain)
				{
					gain *= rainRate;
				}
				WaterStorage = Mathf.Min(WaterStorage + gain, Props.WaterStorageCap);
			}
			if (parent.Map.GetComponent<MapComponent_SewagePollution>()?.WaterPolluted == true)
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

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			// 和 DBH 一样：选中任意水网物件时，把全图水箱的水位条一起亮出来。
			parent.Map?.GetComponent<HygienePipeMapComp>()?.RequestTankBars();
		}

		public override void PostDraw()
		{
			base.PostDraw();
			// 选中水箱本身时也画：MapComponentOnGUI 的 DrawOverlay 只覆盖“选中管网/铺管”的路径，
			// 这里再兜一层，避免 OnGUI 与地图绘制的先后顺序让选中的箱子看不到条。
			if (!DrawOverlay && !(Find.Selector != null && Find.Selector.IsSelected(parent)))
			{
				return;
			}
			// 固定竖条（Rot4.East）：水箱本身不可旋转，旧存档里的建筑朝向不能再影响水位条方向。
			GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
			{
				center = parent.DrawPos + Vector3.up * 0.1f,
				size = BarSize,
				fillPercent = CapPercent,
				filledMat = WaterBarFilledMat,
				unfilledMat = BarUnfilledMat,
				margin = 0.05f,
				rotation = Rot4.East
			});
			DrawOverlay = false;
		}
	}
}
