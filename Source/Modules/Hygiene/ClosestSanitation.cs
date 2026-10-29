using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>找水喝（DBH ClosestSanitation.FindBestDrink 的移植）：先找洗手池/浴缸类设施，再退到地表水。</summary>
	internal static class ClosestSanitation
	{
		public static LocalTargetInfo FindBestDrink(Pawn pawn, bool urgent, float range = 30f)
		{
			if (pawn?.Map == null)
			{
				return LocalTargetInfo.Invalid;
			}
			Map map = pawn.Map;
			Thing best = null;
			float bestScore = float.MinValue;
			foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				float priority;
				bool animal = pawn.RaceProps.Animal;
				if (thing is Building_WaterTrough)
				{
					if (!animal)
					{
						continue;
					}
					priority = 12f;
				}
				else if (thing is Building_WaterTub tub)
				{
					if (tub.ForAnimalsOnly && !animal)
					{
						continue;
					}
					priority = animal ? 6f : 9f;
				}
				else if (thing is Building_Basin)
				{
					priority = animal ? 3f : 9f;
				}
				else if (thing is Building_HotTub)
				{
					if (animal)
					{
						continue;
					}
					priority = 5f;
				}
				else
				{
					continue;
				}
				if (thing.IsForbidden(pawn))
				{
					continue;
				}
				float distance = thing.Position.DistanceTo(pawn.Position);
				if (distance > range)
				{
					continue;
				}
				if (!pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some))
				{
					continue;
				}
				bool working;
				if (thing is Building_Basin basin)
				{
					working = basin.Working().Accepted;
				}
				else
				{
					working = ((Building_HotTub)thing).Working().Accepted;
				}
				if (!working)
				{
					continue;
				}
				float score = priority - distance / 1000f;
				if (score > bestScore)
				{
					bestScore = score;
					best = thing;
				}
			}
			if (best != null)
			{
				return new LocalTargetInfo(best);
			}
			return TryFindWaterCell(pawn, urgent ? 9999f : range);
		}

		/// <summary>找最近的可达地表水格子（找到就是站到水边喝）。</summary>
		public static LocalTargetInfo TryFindWaterCell(Pawn pawn, float range)
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return LocalTargetInfo.Invalid;
			}
			// 原版 GenRadial.RadialCellsAround 在半径超出地图边界时会打印
			// "Not enough squares to get to radius ..." 并截断搜索。
			// 先按当前位置到地图四条边的最小距离收紧半径，既避免红字，
			// 也保证扫到的每个格子都在 InBounds 内。
			float edgeDistance = Mathf.Min(
				Mathf.Min(pawn.Position.x, map.Size.x - 1 - pawn.Position.x),
				Mathf.Min(pawn.Position.z, map.Size.z - 1 - pawn.Position.z));
			float radius = Mathf.Min(Mathf.Min(range, 80f), edgeDistance - 0.01f);
			if (radius <= 0.01f)
			{
				return LocalTargetInfo.Invalid;
			}
			IntVec3 best = IntVec3.Invalid;
			float bestDistance = float.MaxValue;
			int scanned = 0;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
			{
				if (++scanned > 12000)
				{
					break;
				}
				if (!cell.InBounds(map) || !cell.GetTerrain(map).IsWater)
				{
					continue;
				}
				// 站在岸边喝：格子本身不可站，就找相邻可站格。
				IntVec3 stand = cell;
				if (!stand.Standable(map))
				{
					stand = IntVec3.Invalid;
					foreach (IntVec3 neighbor in GenAdj.CardinalDirections)
					{
						IntVec3 candidate = cell + neighbor;
						if (candidate.InBounds(map) && candidate.Standable(map) && !candidate.IsForbidden(pawn))
						{
							stand = candidate;
							break;
						}
					}
					if (!stand.IsValid)
					{
						continue;
					}
				}
				float distance = stand.DistanceTo(pawn.Position);
				if (distance >= bestDistance)
				{
					continue;
				}
				if (!pawn.CanReserveAndReach(stand, PathEndMode.OnCell, Danger.Some))
				{
					continue;
				}
				bestDistance = distance;
				best = stand;
			}
			return best.IsValid ? new LocalTargetInfo(best) : LocalTargetInfo.Invalid;
		}
	}
}
