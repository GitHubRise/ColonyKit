using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace ColonyKit.Modules.Hygiene.GroundWater
{
	/// <summary>
	/// 地图级地下水文：两张逐格网格（浅层 <see cref="Water"/>、深层 <see cref="Deep"/>），
	/// 由「地形 + 降雨量 + Perlin 噪声」确定性地算出来，井的产量从网格里取。
	/// 移植自 Dubs Bad Hygiene 的 <c>MapComponent_Hygiene</c>（WaterGrid / DeepWaterGrid）。
	/// </summary>
	public class MapComponent_GroundWater : MapComponent
	{
		/// <summary>一格水脉值为 50 时视为「普通」，井按标称产量出水。</summary>
		public const float ReferencePerCell = 50f;

		/// <summary>水脉贫瘠时的产量下限：老存档里建在旱地上的井不至于直接断水。</summary>
		public const float MinRichness = 0.1f;

		/// <summary>水脉最富时的产量上限（两倍标称）。</summary>
		public const float MaxRichness = 2f;

		private static readonly Color WaterColor = new Color(0.32f, 0.62f, 1f);
		private static readonly Color DeepColor = new Color(0.36f, 0.45f, 0.95f);

		private bool generated;
		private int version;

		/// <summary>种子；0 表示还没定过，按世界种子 + 地图格算。</summary>
		public int seed;

		/// <summary>选中水井时是否常驻显示水脉。</summary>
		public bool showWater;

		public bool showDeep;

		internal GroundWaterGrid Water { get; private set; }

		internal GroundWaterGrid Deep { get; private set; }

		/// <summary>每次重算网格或有井增减时 +1，井用它判断缓存是否过期。</summary>
		public int Version => version;

		public MapComponent_GroundWater(Map map) : base(map)
		{
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			EnsureGenerated();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref seed, "ckGroundWaterSeed", 0);
		}

		public void EnsureGenerated()
		{
			if (generated)
			{
				return;
			}
			generated = true;
			// 网格是地形 + 噪声算出来的，只要种子固定就能重算出同一张图，不用进存档。
			if (seed == 0)
			{
				int worldSeed = Find.World != null ? Find.World.info.Seed : 0;
				int tile = map.Tile >= 0 ? map.Tile : map.uniqueID;
				seed = Gen.HashCombineInt(worldSeed, tile);
				if (seed == 0)
				{
					seed = 1;
				}
			}
			Regen();
		}

		public void Regen()
		{
			Water = Water ?? new GroundWaterGrid(map, WaterColor);
			Deep = Deep ?? new GroundWaterGrid(map, DeepColor);
			Water.Clear();
			Deep.Clear();
			RegenWater();
			RegenDeepWater();
			version++;
		}

		private bool IsSpaceBiome
		{
			get
			{
				BiomeDef biome = map.Biome;
				if (biome == null)
				{
					return true;
				}
				if (biome.inVacuum)
				{
					return true;
				}
				return biome.defName == "OuterSpaceBiome" || biome == BiomeDefOf.Space;
			}
		}

		/// <summary>
		/// 浅层水脉：肥沃土 +25、水面 +100，再叠降雨量缩放的 Perlin，海洋格清零。
		/// 干旱地图降雨少，整张图里会有大片 0 值格子，井就得建到水边或肥土上。
		/// </summary>
		private void RegenWater()
		{
			if (IsSpaceBiome)
			{
				Water.RefreshMax();
				return;
			}
			float radius = 8f;
			foreach (IntVec3 cell in map.AllCells)
			{
				TerrainDef terrain = cell.GetTerrain(map);
				float depth = 0f;
				if (terrain.fertility > 1f)
				{
					depth = 25f;
				}
				if (terrain.IsWater)
				{
					depth = 100f;
				}
				if (depth > 0f)
				{
					Water.AddRadial(cell, radius, depth);
				}
			}

			float min = GenMath.LerpDoubleClamped(50f, 2000f, 0.5f, -1f, map.TileInfo.rainfall);
			float max = GenMath.LerpDoubleClamped(50f, 2000f, 50f, 100f, map.TileInfo.rainfall);
			var noise = new Perlin(0.173f, 0.293f, 5f, 2, seed, QualityMode.Medium);
			foreach (IntVec3 cell in map.AllCells)
			{
				TerrainDef terrain = cell.GetTerrain(map);
				if (terrain.HasTag("Ocean"))
				{
					Water.RemoveRadial(cell, radius, 0f);
					continue;
				}
				float value = GenMath.LerpDoubleClamped(min, 1f, 0f, max, noise.GetValue(cell));
				value = Mathf.RoundToInt(value);
				if (value > Water.ValueAt(cell))
				{
					Water.Set(cell, value);
				}
			}
			Water.RefreshMax();
		}

		/// <summary>
		/// 深层水脉：单独一张 Perlin（深井半径更大），不受地表污染。
		/// 海冰/太空没有深层水，交给低温采水节点或地表水。
		/// </summary>
		private void RegenDeepWater()
		{
			if (IsSpaceBiome || map.Biome == BiomeDefOf.SeaIce)
			{
				Deep.RefreshMax();
				return;
			}
			float min = GenMath.LerpDoubleClamped(50f, 2000f, 0.5f, -1f, map.TileInfo.rainfall);
			float max = GenMath.LerpDoubleClamped(50f, 2000f, 50f, 100f, map.TileInfo.rainfall);
			var noise = new Perlin(0.066f, 0.186f, 5f, 2, seed, QualityMode.Medium);
			foreach (IntVec3 cell in map.AllCells)
			{
				float value = GenMath.LerpDoubleClamped(min, 0.5f, 0f, max, noise.GetValue(cell));
				value = Mathf.RoundToInt(value);
				if (value > Deep.ValueAt(cell))
				{
					Deep.Set(cell, value);
				}
			}
			Deep.RefreshMax();
		}

		public bool HasAnyWater(bool deep)
		{
			EnsureGenerated();
			return (deep ? Deep : Water).HasAnyWater;
		}

		public float ValueAt(IntVec3 cell, bool deep)
		{
			EnsureGenerated();
			return (deep ? Deep : Water).ValueAt(cell);
		}

		/// <summary>
		/// 一口井的富集度：覆盖范围内每格水脉（已按井数分摊）的平均值 ÷ 50，
		/// 夹在 0.1–2.0，井的产量 = 标称产量 × 富集度。
		/// </summary>
		public float RichnessFor(List<IntVec3> cells, bool deep)
		{
			EnsureGenerated();
			if (cells == null || cells.Count == 0)
			{
				return 0f;
			}
			float sum = (deep ? Deep : Water).CapacityFor(cells);
			float perCell = sum / cells.Count;
			return Mathf.Clamp(perCell / ReferencePerCell, MinRichness, MaxRichness);
		}

		public void RegisterWell(List<IntVec3> cells, bool deep)
		{
			EnsureGenerated();
			if (cells == null)
			{
				return;
			}
			GroundWaterGrid grid = deep ? Deep : Water;
			for (int i = 0; i < cells.Count; i++)
			{
				grid.AddUser(cells[i]);
			}
			version++;
		}

		public void DeregisterWell(List<IntVec3> cells, bool deep)
		{
			if (cells == null || Water == null)
			{
				return;
			}
			GroundWaterGrid grid = deep ? Deep : Water;
			for (int i = 0; i < cells.Count; i++)
			{
				grid.RemoveUser(cells[i]);
			}
			version++;
		}

		public void MarkForDraw(bool deep)
		{
			EnsureGenerated();
			(deep ? Deep : Water).MarkForDraw();
		}

		/// <summary>自检专用：直接改一格的浅层水脉并让所有井重算产量。</summary>
		internal void SetWaterForTest(IntVec3 cell, float value)
		{
			EnsureGenerated();
			Water.Set(cell, value);
			Water.RefreshMax();
			version++;
		}

		public override void MapComponentUpdate()
		{
			base.MapComponentUpdate();
			EnsureGenerated();
			if (showWater)
			{
				Water.MarkForDraw();
			}
			if (showDeep)
			{
				Deep.MarkForDraw();
			}
			Water.DrawUpdate();
			Deep.DrawUpdate();
		}

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();
			// 正在放水井时把水脉亮出来，玩家不用先建一口井才知道哪有水。
			if (!(Find.DesignatorManager?.SelectedDesignator is Designator_Build designator))
			{
				return;
			}
			if (!(designator.PlacingDef is ThingDef def) || def.comps == null)
			{
				return;
			}
			for (int i = 0; i < def.comps.Count; i++)
			{
				if (def.comps[i] is Dbh.CompProperties_WaterInlet inlet && inlet.GroundWater)
				{
					(Find.CurrentMap?.GetComponent<MapComponent_GroundWater>()).MarkForDraw(inlet.Deep);
				}
			}
		}
	}
}
