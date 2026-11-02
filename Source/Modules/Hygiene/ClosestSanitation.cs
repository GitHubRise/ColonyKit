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
		public static LocalTargetInfo FindBestDrink(Pawn pawn, bool urgent, float range = 30f, bool allowColonyFixtures = true)
		{
			if (pawn?.Map == null)
			{
				return LocalTargetInfo.Invalid;
			}
			if (!allowColonyFixtures)
			{
				return TryFindWaterCell(pawn, urgent ? 9999f : range);
			}
			// 野生动物只在自然水域喝水，不去殖民地的水槽/水盆排队，
			// 免得一大群动物因为口渴全聚到基地里；家畜/宠物照常用水槽。
			if (pawn.RaceProps.Animal
				&& pawn.Faction != Faction.OfPlayer
				&& pawn.HostFaction != Faction.OfPlayer)
			{
				return TryFindWaterCell(pawn, urgent ? 9999f : range);
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
				if (!pawn.CanReserveAndReach(
					thing,
					thing is Building_AssignableFixture fixture ? fixture.InteractionPathMode : PathEndMode.InteractionCell,
					Danger.Some))
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

		/// <summary>
		/// 找最近的可达地表水格子（找到就是站到水边喝）。
		/// 动物在「最近一圈」的候选格里随机挑一个，避免整群动物挤到同一个水边格子。
		/// </summary>
		public static LocalTargetInfo TryFindWaterCell(Pawn pawn, float range)
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return LocalTargetInfo.Invalid;
			}
			// 半径不能超过径向表上限，否则每次找水都会刷红字；见 WaterSourceUtility.MaxWaterSearchRadius。
			float radius = Mathf.Min(range, WaterSourceUtility.MaxWaterSearchRadius);
			if (radius <= 0.01f)
			{
				return LocalTargetInfo.Invalid;
			}
			bool spread = pawn.RaceProps.Animal;
			IntVec3 best = IntVec3.Invalid;
			float bestDistance = float.MaxValue;
			List<IntVec3> candidates = spread ? new List<IntVec3>() : null;
			List<float> candidateDistances = spread ? new List<float>() : null;
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
				if (distance >= bestDistance && !spread)
				{
					continue;
				}
				if (!pawn.CanReserveAndReach(stand, PathEndMode.OnCell, Danger.Some))
				{
					continue;
				}
				if (!spread)
				{
					bestDistance = distance;
					best = stand;
					continue;
				}
				if (candidates.Count < 64)
				{
					candidates.Add(stand);
					candidateDistances.Add(distance);
				}
				if (distance < bestDistance)
				{
					bestDistance = distance;
				}
			}
			if (!spread)
			{
				return best.IsValid ? new LocalTargetInfo(best) : LocalTargetInfo.Invalid;
			}
			// 只在「离最近水边不远」的格子里随机，既能喝到水，又不会分散到很远。
			List<IntVec3> pool = new List<IntVec3>();
			float limit = bestDistance * 1.3f + 3f;
			for (int i = 0; i < candidates.Count; i++)
			{
				if (candidateDistances[i] <= limit)
				{
					pool.Add(candidates[i]);
				}
			}
			if (pool.Count == 0)
			{
				return LocalTargetInfo.Invalid;
			}
			return new LocalTargetInfo(pool[Rand.Range(0, pool.Count)]);
		}
	}
}
