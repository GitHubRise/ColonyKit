using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene.GroundWater
{
	/// <summary>
	/// 逐格地下水脉：每格一个 0–100 的「可抽水量」，另存每格压着几口井，
	/// 用来让同一条水脉上的多口井分水（移植 DBH 的 <c>WaterGrid</c> / <c>WaterUserGrid</c>）。
	/// </summary>
	/// <remarks>
	/// 网格是纯地形 + 噪声算出来的，可由地图种子重算，所以不进存档；
	/// 只有井的占用计数在运行时维护。
	/// </remarks>
	internal sealed class GroundWaterGrid : ICellBoolGiver
	{
		private readonly Map map;
		private readonly float[] values;
		private readonly int[] users;
		private readonly CellBoolDrawer drawer;
		private readonly Color color;
		private readonly Color colorClear;

		public GroundWaterGrid(Map map, Color color)
		{
			this.map = map;
			this.color = color;
			colorClear = new Color(color.r, color.g, color.b, 0f);
			values = new float[map.cellIndices.NumGridCells];
			users = new int[values.Length];
			drawer = new CellBoolDrawer(this, map.Size.x, map.Size.z, 1f);
		}

		public CellBoolDrawer Drawer => drawer;

		/// <summary>整张网格里最富的一格；用来判断「这张图到底有没有地下水」。</summary>
		public float MaxValue { get; private set; }

		public bool HasAnyWater => MaxValue > 0.5f;

		public Color Color => color;

		public bool GetCellBool(int index)
		{
			return values[index] > 0.5f;
		}

		public Color GetCellExtraColor(int index)
		{
			float t = Mathf.Clamp01(values[index] / 100f);
			return Color.Lerp(colorClear, color, t);
		}

		public float ValueAt(IntVec3 cell)
		{
			return cell.InBounds(map) ? values[map.cellIndices.CellToIndex(cell)] : 0f;
		}

		public void Set(IntVec3 cell, float value)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			values[map.cellIndices.CellToIndex(cell)] = value;
		}

		public void Clear()
		{
			System.Array.Clear(values, 0, values.Length);
			System.Array.Clear(users, 0, users.Length);
			MaxValue = 0f;
			drawer.SetDirty();
		}

		/// <summary>以 center 为中心铺一个径向衰减的水脉（地形加成用）。</summary>
		public void AddRadial(IntVec3 center, float radius, float depth)
		{
			int count = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < count; i++)
			{
				IntVec3 cell = center + GenRadial.RadialPattern[i];
				if (!cell.InBounds(map))
				{
					continue;
				}
				float distance = (center - cell).LengthHorizontal;
				if (!(distance < radius))
				{
					continue;
				}
				float value = (1f - distance / radius) * depth;
				int index = map.cellIndices.CellToIndex(cell);
				if (values[index] < value)
				{
					values[index] = value;
				}
			}
		}

		/// <summary>把中心周围的水脉压回低值（海洋格、真空海冰等）。</summary>
		public void RemoveRadial(IntVec3 center, float radius, float floor)
		{
			int count = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < count; i++)
			{
				IntVec3 cell = center + GenRadial.RadialPattern[i];
				if (!cell.InBounds(map))
				{
					continue;
				}
				int index = map.cellIndices.CellToIndex(cell);
				if (values[index] > floor)
				{
					values[index] = floor;
				}
			}
		}

		/// <summary>噪声写好之后重算最大值，供 <see cref="HasAnyWater"/> 使用。</summary>
		public void RefreshMax()
		{
			float max = 0f;
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] > max)
				{
					max = values[i];
				}
			}
			MaxValue = max;
			drawer.SetDirty();
		}

		public void AddUser(IntVec3 cell)
		{
			if (cell.InBounds(map))
			{
				users[map.cellIndices.CellToIndex(cell)]++;
			}
		}

		public void RemoveUser(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			int index = map.cellIndices.CellToIndex(cell);
			if (users[index] > 0)
			{
				users[index]--;
			}
		}

		/// <summary>
		/// 这片格子的总可抽水量：每格按「水脉 ÷ 压在这格上的井数」累加，
		/// 所以两口井挤在同一条水脉上时各自减半。
		/// </summary>
		public float CapacityFor(List<IntVec3> cells)
		{
			if (cells == null || cells.Count == 0)
			{
				return 0f;
			}
			float sum = 0f;
			for (int i = 0; i < cells.Count; i++)
			{
				IntVec3 cell = cells[i];
				if (!cell.InBounds(map))
				{
					continue;
				}
				int index = map.cellIndices.CellToIndex(cell);
				int share = users[index] > 0 ? users[index] : 1;
				sum += values[index] / share;
			}
			return sum;
		}

		public void MarkForDraw()
		{
			if (map == Find.CurrentMap)
			{
				drawer.MarkForDraw();
			}
		}

		public void DrawUpdate()
		{
			drawer.CellBoolDrawerUpdate();
		}
	}
}
