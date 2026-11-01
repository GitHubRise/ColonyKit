using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.GroundWater;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	public class CompProperties_WaterPumpingStation : CompProperties
	{
		public float Capacity = 2000f;

		public CompProperties_WaterPumpingStation()
		{
			compClass = typeof(CompWaterPumpingStation);
		}
	}

	public class CompWaterPumpingStation : ThingComp
	{
		private CompBreakdownable breakdown;
		private CompFlickable flicker;
		private CompRefuelable fuel;
		private CompPowerTrader power;

		public CompProperties_WaterPumpingStation Props => (CompProperties_WaterPumpingStation)props;

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

		// 设置里的水泵容量倍率同时作用于电动泵、泵站和风泵；默认 1 时行为不变。
		public float Capacity => WorkingNow ? Props.Capacity * HygieneSettings.Current.pumpCapacityMultiplier : 0f;

		public override void CompTick()
		{
			base.CompTick();
			if (!WorkingNow || parent?.Map == null || !parent.IsHashIntervalTick(600))
			{
				return;
			}
			DefDatabase<SoundDef>.GetNamedSilentFail("CK_WaterPump")?.PlayOneShot(parent);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			flicker = parent.GetComp<CompFlickable>();
			power = parent.GetComp<CompPowerTrader>();
			fuel = parent.GetComp<CompRefuelable>();
			breakdown = parent.TryGetComp<CompBreakdownable>();
		}

		public override string CompInspectStringExtra()
		{
			return "CK.Hygiene.PumpCapacity".Translate(Capacity.ToString("F0"));
		}
	}

	public class CompProperties_WaterInlet : CompProperties
	{
		public float Capacity = 2000f;

		/// <summary>抽水的覆盖半径（格）；产量取这个范围里的地下水脉。</summary>
		public float Radius = 7.9f;

		public bool Deep;

		public bool Piped = true;

		/// <summary>产量是否跟随地下水脉。低温采水节点这类自带水源的设施设 false。</summary>
		public bool GroundWater = true;

		public CompProperties_WaterInlet()
		{
			compClass = typeof(CompWaterInlet);
		}
	}

	/// <summary>
	/// 水井/取水口：产量 = 标称产量 × 覆盖范围内的地下水富集度。
	/// 富集度来自 <see cref="MapComponent_GroundWater"/> 的逐格网格，同一条水脉上的
	/// 多口井按格分摊，所以两口井挤在一起时各自产量减半（DBH 的 WaterUserGrid 规则）。
	/// </summary>
	public class CompWaterInlet : ThingComp
	{
		protected float capacity;

		private int cachedVersion = -1;

		private MapComponent_GroundWater groundWater;

		private List<IntVec3> coveredCells;

		public CompProperties_WaterInlet Props => (CompProperties_WaterInlet)props;

		/// <summary>这口井的地下水产量（升/天）。子类（水面进水口等）可以直接改它。</summary>
		public virtual float GroundWaterCapacity
		{
			get
			{
				RefreshCapacity();
				return capacity;
			}
			set
			{
				capacity = value;
			}
		}

		/// <summary>实际产量 / 标称产量，检视页用来显示「水脉：丰富 ×1.6」。</summary>
		public float Richness => Props.Capacity > 0.0001f ? GroundWaterCapacity / Props.Capacity : 0f;

		/// <summary>这口井到底走不走地下水网格（水面进水口等子类不走）。</summary>
		protected bool UsesGroundWater => Props.GroundWater && HygieneSettings.Current.groundWaterRequired;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			capacity = Props.Capacity;
			if (!UsesGroundWater)
			{
				return;
			}
			groundWater = parent.Map?.GetComponent<MapComponent_GroundWater>();
			coveredCells = CoveredCells();
			groundWater?.RegisterWell(coveredCells, Props.Deep);
			cachedVersion = -1;
			RefreshCapacity();
		}

		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			if (groundWater != null && coveredCells != null)
			{
				groundWater.DeregisterWell(coveredCells, Props.Deep);
				groundWater = null;
				coveredCells = null;
			}
			base.PostDeSpawn(map, mode);
		}

		private List<IntVec3> CoveredCells()
		{
			var cells = new List<IntVec3>();
			if (!parent.Spawned || parent.Map == null)
			{
				return cells;
			}
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.Radius, true))
			{
				if (cell.InBounds(parent.Map))
				{
					cells.Add(cell);
				}
			}
			return cells;
		}

		private void RefreshCapacity()
		{
			if (!UsesGroundWater)
			{
				if (capacity <= 0f)
				{
					capacity = Props.Capacity;
				}
				return;
			}
			if (groundWater == null)
			{
				groundWater = parent.Map?.GetComponent<MapComponent_GroundWater>();
				if (groundWater == null)
				{
					capacity = Props.Capacity;
					return;
				}
			}
			if (cachedVersion == groundWater.Version)
			{
				return;
			}
			cachedVersion = groundWater.Version;
			if (coveredCells == null)
			{
				coveredCells = CoveredCells();
			}
			capacity = Props.Capacity * groundWater.RichnessFor(coveredCells, Props.Deep);
		}

		public override string CompInspectStringExtra()
		{
			if (!Props.GroundWater)
			{
				return "CK.Hygiene.GroundWater".Translate(capacity.ToString("F0"));
			}
			string vein = Richness >= 1.5f ? "CK.Hygiene.WellVein.Rich"
				: Richness >= 0.75f ? "CK.Hygiene.WellVein.Normal"
				: Richness >= 0.3f ? "CK.Hygiene.WellVein.Poor"
				: "CK.Hygiene.WellVein.Barren";
			return "CK.Hygiene.GroundWater".Translate(GroundWaterCapacity.ToString("F0"))
				+ "\n" + "CK.Hygiene.WellVein.Line".Translate(vein.Translate(), Richness.ToString("0.00"));
		}

		public override void PostDrawExtraSelectionOverlays()
		{
			base.PostDrawExtraSelectionOverlays();
			if (Props.GroundWater)
			{
				(parent.Map?.GetComponent<MapComponent_GroundWater>())?.MarkForDraw(Props.Deep);
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (!Props.GroundWater)
			{
				yield break;
			}
			MapComponent_GroundWater map = parent.Map?.GetComponent<MapComponent_GroundWater>();
			if (map == null)
			{
				yield break;
			}
			yield return new Command_Toggle
			{
				defaultLabel = "CK.Hygiene.Gizmo.GroundWater".Translate(),
				defaultDesc = "CK.Hygiene.Gizmo.GroundWater.Desc".Translate(),
				isActive = () => Props.Deep ? map.showDeep : map.showWater,
				toggleAction = delegate
				{
					if (Props.Deep)
					{
						map.showDeep = !map.showDeep;
					}
					else
					{
						map.showWater = !map.showWater;
					}
				}
			};
		}
	}
}
