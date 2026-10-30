using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 地表水（小池塘、河流）取水工具：找水格、找相邻可站格、判断水质。
	/// 取水 Job 的 TargetA 存「可站格」，司机再用 <see cref="AdjacentWaterCell"/> 找回水格，
	/// 这样既能用 GotoCell 正常寻路，也能沿用同一套目标索引。
	/// </summary>
	internal static class WaterSourceUtility
	{
		public static bool IsSurfaceWater(Map map, IntVec3 cell)
		{
			return map != null && cell.InBounds(map) && cell.GetTerrain(map).IsWater;
		}

		/// <summary>水格旁边最近的可站格；找不到返回 <see cref="IntVec3.Invalid"/>。</summary>
		public static IntVec3 FindStandCell(Pawn pawn, IntVec3 waterCell)
		{
			Map map = pawn?.Map;
			if (map == null || !IsSurfaceWater(map, waterCell))
			{
				return IntVec3.Invalid;
			}
			foreach (IntVec3 direction in GenAdj.CardinalDirections)
			{
				IntVec3 cell = waterCell + direction;
				if (cell.InBounds(map)
					&& cell.Standable(map)
					&& !cell.IsForbidden(pawn)
					&& pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
				{
					return cell;
				}
			}
			return IntVec3.Invalid;
		}

		/// <summary>在 near 附近找一处地表水的可站格（供小人取水用，带可达判断）。</summary>
		public static IntVec3 FindSurfaceWaterStandCell(Pawn pawn, IntVec3 near, float range)
		{
			return FindSurfaceWaterStandCell(pawn?.Map, near, range, pawn);
		}

		/// <summary>在 near 附近找一处地表水的可站格；pawn 为空时只做地形判断（检视页用）。</summary>
		public static IntVec3 FindSurfaceWaterStandCell(Map map, IntVec3 near, float range, Pawn pawn = null)
		{
			if (map == null || !near.InBounds(map))
			{
				return IntVec3.Invalid;
			}
			// 和 ClosestSanitation 一样先按到地图边缘的距离收半径，避免
			// GenRadial 超出地图边界时打印 "Not enough squares" 并截断。
			float edgeDistance = Mathf.Min(
				Mathf.Min(near.x, map.Size.x - 1 - near.x),
				Mathf.Min(near.z, map.Size.z - 1 - near.z));
			float radius = Mathf.Min(Mathf.Min(range, 80f), edgeDistance - 0.01f);
			if (radius <= 0.01f)
			{
				return IntVec3.Invalid;
			}

			IntVec3 best = IntVec3.Invalid;
			float bestDistance = float.MaxValue;
			int scanned = 0;
			int waterCells = 0;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(near, radius, true))
			{
				if (++scanned > 12000)
				{
					break;
				}
				if (!IsSurfaceWater(map, cell))
				{
					continue;
				}
				// 大河边水格很多，别为每一格都做 4 次寻路；只试最近的几十格。
				if (++waterCells > 64)
				{
					break;
				}
				IntVec3 stand = pawn != null ? FindStandCell(pawn, cell) : FindStandCellAny(map, cell);
				if (!stand.IsValid)
				{
					continue;
				}
				float distance = stand.DistanceTo(near);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = stand;
				}
			}
			return best;
		}

		private static IntVec3 FindStandCellAny(Map map, IntVec3 waterCell)
		{
			foreach (IntVec3 direction in GenAdj.CardinalDirections)
			{
				IntVec3 cell = waterCell + direction;
				if (cell.InBounds(map)
					&& cell.Standable(map)
					&& !cell.Fogged(map))
				{
					return cell;
				}
			}
			return IntVec3.Invalid;
		}

		/// <summary>从可站格反查相邻的水格；找不到返回 Invalid。</summary>
		public static IntVec3 AdjacentWaterCell(Map map, IntVec3 standCell)
		{
			if (map == null || !standCell.InBounds(map))
			{
				return IntVec3.Invalid;
			}
			foreach (IntVec3 direction in GenAdj.CardinalDirections)
			{
				IntVec3 cell = standCell + direction;
				if (IsSurfaceWater(map, cell))
				{
					return cell;
				}
			}
			return IntVec3.Invalid;
		}

		/// <summary>地表水水质：被污水污染过就算已污染，否则未处理。</summary>
		public static ContaminationLevel SurfaceQuality(Map map, IntVec3 waterCell)
		{
			MapComponent_SewagePollution pollution = map?.GetComponent<MapComponent_SewagePollution>();
			if (pollution != null && pollution.PollutionAt(waterCell) > 0.5f)
			{
				return ContaminationLevel.Contaminated;
			}
			return ContaminationLevel.Untreated;
		}
	}
}
