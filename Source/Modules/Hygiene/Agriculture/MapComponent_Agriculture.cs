using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>
	/// 肥力网格：DBH 的 Fertilizer/Irrigation GridLayer 的轻量版，只存有效格子（稀疏字典），
	/// 覆盖层由 SectionLayer_Agriculture 画；肥力效果在 FertilityGrid 计算时叠加（见文件末尾的补丁）。
	/// 施肥用原版 Designation 标记格子，省掉自定义 Area 与它的存档网格。
	/// </summary>
	public class MapComponent_Agriculture : MapComponent
	{
		private const int DecayInterval = 250;

		/// <summary>施肥格 → 剩余肥量（0–100）。</summary>
		private Dictionary<int, float> fertilizer = new Dictionary<int, float>();

		/// <summary>灌溉格 → 剩余水量（0–100），每天早上洒水器加满、当天蒸发完。</summary>
		private Dictionary<int, float> irrigation = new Dictionary<int, float>();

		private int lastDecayTick;

		private bool overlayLogged;

		private bool overlayWarned;

		/// <summary>待重画覆盖层的格子（变化时收集，tick 末尾按 section 归并重画）。</summary>
		private readonly HashSet<IntVec3> dirtyCells = new HashSet<IntVec3>();

		/// <summary>所有地图的实例，给 FertilityGrid 补丁反查用（FertilityGrid 的 map 字段不公开）。</summary>
		private static readonly List<MapComponent_Agriculture> instances = new List<MapComponent_Agriculture>();

		public MapComponent_Agriculture(Map map) : base(map)
		{
			instances.RemoveAll(c => c.map == map);
			instances.Add(this);
		}

		public static MapComponent_Agriculture Get(Map map)
		{
			return map?.GetComponent<MapComponent_Agriculture>();
		}

		/// <summary>按 FertilityGrid 反查所属地图的组件。</summary>
		public static MapComponent_Agriculture ForGrid(FertilityGrid grid)
		{
			for (int i = 0; i < instances.Count; i++)
			{
				MapComponent_Agriculture candidate = instances[i];
				if (candidate.map != null && candidate.map.fertilityGrid == grid)
				{
					return candidate;
				}
			}
			return null;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref fertilizer, "ckFertilizer", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref irrigation, "ckIrrigation", LookMode.Value, LookMode.Value);
			if (fertilizer == null)
			{
				fertilizer = new Dictionary<int, float>();
			}
			if (irrigation == null)
			{
				irrigation = new Dictionary<int, float>();
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			// 读档后把已有施肥/灌溉的 section 重画一遍，否则覆盖层要等下一次变化才出现。
			foreach (int index in fertilizer.Keys)
			{
				MarkDirty(map.cellIndices.IndexToCell(index));
			}
			foreach (int index in irrigation.Keys)
			{
				MarkDirty(map.cellIndices.IndexToCell(index));
			}
		}

		private void MarkDirty(IntVec3 cell)
		{
			if (cell.InBounds(map))
			{
				dirtyCells.Add(cell);
			}
		}

		/// <summary>把收集到的格子按 section 归并后各重画一次。</summary>
		private void FlushOverlay()
		{
			if (dirtyCells.Count == 0)
			{
				return;
			}
			HashSet<Section> done = new HashSet<Section>();
			foreach (IntVec3 cell in dirtyCells)
			{
				try
				{
					Section section = map.mapDrawer.SectionAt(cell);
					if (section != null && done.Add(section))
					{
						SectionLayer layer = section.GetLayer(typeof(SectionLayer_Agriculture));
						layer.Regenerate();
						if (!overlayLogged)
						{
							overlayLogged = true;
							ModLog.Info("Hygiene",
								$"agriculture overlay: rebuilt section at {cell}, visible={layer.Visible}");
						}
					}
				}
				catch (System.Exception ex)
				{
					// 地图网格还没建好时 SectionAt 会抛 NRE；等下一次变化再试即可。
					if (!overlayWarned)
					{
						overlayWarned = true;
						ModLog.Warn("Hygiene", "agriculture overlay regen deferred: " + ex.Message);
					}
					break;
				}
			}
			dirtyCells.Clear();
		}

		public float FertilizerAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return 0f;
			}
			fertilizer.TryGetValue(map.cellIndices.CellToIndex(cell), out float value);
			return value;
		}

		public float IrrigationAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return 0f;
			}
			irrigation.TryGetValue(map.cellIndices.CellToIndex(cell), out float value);
			return value;
		}

		public void AddFertilizer(IntVec3 cell)
		{
			if (cell.InBounds(map))
			{
				fertilizer[map.cellIndices.CellToIndex(cell)] = 100f;
				MarkDirty(cell);
			}
		}

		public void AddIrrigation(IntVec3 cell, float amount)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			int index = map.cellIndices.CellToIndex(cell);
			irrigation.TryGetValue(index, out float current);
			irrigation[index] = Mathf.Min(100f, current + amount);
			MarkDirty(cell);
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();
			int now = Find.TickManager.TicksGame;
			if (now - lastDecayTick >= DecayInterval)
			{
				int steps = (now - lastDecayTick) / DecayInterval;
				lastDecayTick = now - (now - lastDecayTick) % DecayInterval;
				Decay(fertilizer, steps, FertilizerDecayPerStep);
				Decay(irrigation, steps, IrrigationDecayPerStep);
			}
			FlushOverlay();
		}

		/// <summary>施肥寿命按天算：100 点均摊到 lifespan 天里。</summary>
		private static float FertilizerDecayPerStep
		{
			get
			{
				float days = Mathf.Max(1f, HygieneSettings.Current.fertilizerLifespanDays);
				return 100f / (days * 60000f) * DecayInterval;
			}
		}

		/// <summary>灌溉当天蒸发完：100 点均摊到 1 天。</summary>
		private static float IrrigationDecayPerStep => 100f / 60000f * DecayInterval;

		private void Decay(Dictionary<int, float> grid, int steps, float perStep)
		{
			if (grid.Count == 0 || steps <= 0)
			{
				return;
			}
			List<int> dead = null;
			List<int> keys = new List<int>(grid.Keys);
			for (int i = 0; i < keys.Count; i++)
			{
				int key = keys[i];
				float value = grid[key] - perStep * steps;
				MarkDirty(map.cellIndices.IndexToCell(key));
				if (value <= 0f)
				{
					dead = dead ?? new List<int>();
					dead.Add(key);
				}
				else
				{
					grid[key] = value;
				}
			}
			if (dead != null)
			{
				for (int i = 0; i < dead.Count; i++)
				{
					grid.Remove(dead[i]);
				}
			}
		}
	}

	/// <summary>把施肥/灌溉折进原版的 FertilityGrid 计算（DBH 同款后置补丁）。</summary>
	[HarmonyPatch(typeof(FertilityGrid), "CalculateFertilityAt")]
	internal static class Patch_FertilityGrid_Agriculture
	{
		private static void Postfix(FertilityGrid __instance, IntVec3 loc, ref float __result)
		{
			MapComponent_Agriculture comp = MapComponent_Agriculture.ForGrid(__instance);
			if (comp == null)
			{
				return;
			}
			HygieneSettings settings = HygieneSettings.Current;
			if (comp.FertilizerAt(loc) > 0f)
			{
				// 和 DBH 一样保留 0.5 的下限：贫瘠/沙地施肥后也能种。
				__result = Mathf.Max(__result * settings.fertilizerStrength, 0.5f);
			}
			float irrigation = comp.IrrigationAt(loc);
			if (irrigation > 0f)
			{
				__result *= GenMath.LerpDoubleClamped(0f, 100f, 1f, settings.irrigationStrength, irrigation);
			}
		}
	}
}
