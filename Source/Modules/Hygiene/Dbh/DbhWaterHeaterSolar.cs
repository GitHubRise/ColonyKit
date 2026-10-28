using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Dbh
{
	/// <summary>
	/// 太阳能加热器（DBH CompBoilerSolar 的移植）：出力 = 基础容量 × 天光 × 露天比例 × 气温效率。
	/// CK 没有天窗概念，屋顶遮挡直接按被覆盖的格子比例扣减。
	/// </summary>
	public class CompWaterHeaterSolar : CompWaterHeater
	{
		public float RoofedHeatOutputFactor
		{
			get
			{
				int total = 0;
				int roofed = 0;
				foreach (IntVec3 cell in parent.OccupiedRect())
				{
					total++;
					if (parent.Map.roofGrid.Roofed(cell))
					{
						roofed++;
					}
				}
				return total <= 0 ? 1f : (float)(total - roofed) / total;
			}
		}

		public float TempEfficiency
		{
			get
			{
				GenTemperature.TryGetDirectAirTemperatureForCell(parent.Position, parent.Map, out float temperature);
				return Mathf.InverseLerp(-10f, 10f, temperature);
			}
		}

		public override float Capacity =>
			Props.Capacity * parent.Map.skyManager.CurSkyGlow * RoofedHeatOutputFactor * TempEfficiency;

		public override string CompInspectStringExtra()
		{
			if (ParentHolder is MinifiedThing)
			{
				return base.CompInspectStringExtra();
			}
			string text = base.CompInspectStringExtra();
			text += "\n" + "CK.Hygiene.SolarHeaterOutput".Translate(
				Capacity.ToString("F0"),
				(RoofedHeatOutputFactor * TempEfficiency).ToStringPercent("0.0"));
			if (TempEfficiency < 1f)
			{
				text += "\n" + "CK.Hygiene.SolarHeaterCold".Translate();
			}
			if (RoofedHeatOutputFactor < 1f)
			{
				text += "\n" + "CK.Hygiene.SolarHeaterRoofed".Translate();
			}
			return text;
		}
	}

	/// <summary>地热加热器（DBH CompGeoBoiler 的移植）：盖在蒸汽间歇泉上，把它认领为采集者。</summary>
	public class CompWaterHeaterGeo : CompWaterHeater
	{
		private Building_SteamGeyser geyser;

		public override void CompTick()
		{
			base.CompTick();
			if (parent?.Map == null)
			{
				return;
			}
			if (geyser == null)
			{
				geyser = parent.Map.thingGrid.ThingAt(parent.Position, ThingDefOf.SteamGeyser) as Building_SteamGeyser;
			}
			if (geyser != null)
			{
				geyser.harvester = (Building)parent;
			}
		}
	}
}
