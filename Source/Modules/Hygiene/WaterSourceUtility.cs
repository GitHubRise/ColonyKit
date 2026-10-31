using ColonyKit.Modules.Hygiene.Dbh;
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
		/// <summary>
		/// 找水格时 GenRadial 搜索半径的上限。GenRadial 的径向表固定 20000 格，最远只排到
		/// 79.81228 格（<see cref="GenRadial.MaxRadialPatternRadius"/>）；半径达到或超过它，
		/// <see cref="GenRadial.NumCellsInRadius"/> 就会打印
		/// "Not enough squares to get to radius ..."（例如盖在 250×250 地图中间的小人，
		/// 到四边都超过 80 格，半径 80 就会触发），并返回整张表。
		/// 搜索循环本来就在 12000 格处截断（约 62 格），所以统一按 60 格封顶，行为和以前一致。
		/// </summary>
		public const float MaxWaterSearchRadius = 60f;
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

		/// <summary>
		/// 给「带容器去装水」找一个可用的装水点：取水点、水龙头、水井、同网有存水的水箱，
		/// 最后退回附近地表水。返回 Thing 或可站格。
		/// </summary>
		public static LocalTargetInfo FindFillSource(Pawn pawn, IntVec3 near, float range = 9999f)
		{
			Map map = pawn?.Map;
			if (map == null)
			{
				return LocalTargetInfo.Invalid;
			}
			Thing best = null;
			float bestDistance = float.MaxValue;
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				if (!IsFillSource(thing))
				{
					continue;
				}
				if (thing.IsForbidden(pawn)
					|| !pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
				{
					continue;
				}
				float distance = thing.Position.DistanceTo(near);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = thing;
				}
			}
			if (best != null)
			{
				return new LocalTargetInfo(best);
			}
			IntVec3 stand = FindSurfaceWaterStandCell(pawn, near, range);
			return stand.IsValid ? new LocalTargetInfo(stand) : LocalTargetInfo.Invalid;
		}

		private static bool IsFillSource(Thing thing)
		{
			if (thing is Building_WaterSpot || thing is Building_WaterPoint)
			{
				return true;
			}
			if (thing.TryGetComp<CompWaterInlet>() != null)
			{
				return true;
			}
			CompWaterStorage storage = thing.TryGetComp<CompWaterStorage>();
			return storage != null
				&& storage.WaterStorage > 1f
				&& thing.TryGetComp<CompPipe>()?.pipeNet != null;
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
			// 半径超过径向表上限，每次找水都会刷红字；见 MaxWaterSearchRadius。
			float radius = Mathf.Min(range, MaxWaterSearchRadius);
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
