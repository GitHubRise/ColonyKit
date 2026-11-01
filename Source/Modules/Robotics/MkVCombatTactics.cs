using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 交战时的小战术：
	/// 1) 边打边撤——敌人贴上来就退到射程边缘继续打，不当固定靶；
	/// 2) 找掩体——优先站到周围掩体分高的格子，降低敌人的命中率；
	/// 3) 躲火——脚下或身边起火时先挪开，哪怕正在交火。
	/// 只在 Mk-V 无掩体/被贴脸/身边起火时才会挪窝，且新位置要明显更好（避免原地来回抽动）。
	/// </summary>
	internal static class MkVCombatTactics
	{
		/// <summary>最近的敌人近于这个距离（格）就往后撤。</summary>
		public const float MinKeepDistance = 13f;

		/// <summary>退到射程的这个比例附近：够得着敌人，又不至于贴脸。</summary>
		public const float PreferredRangeFactor = 0.75f;

		/// <summary>新位置的评分至少要比原地高这么多才值得跑一趟。</summary>
		private const float MinImprovement = 5f;

		/// <summary>找位置时只搜索身边这么多格，避免满地图算路径。</summary>
		private const float SearchRadius = 12f;

		public static bool InDanger(Pawn pawn)
		{
			Map map = pawn?.Map;
			if (map == null || pawn.Dead || !pawn.Spawned)
			{
				return false;
			}
			if (pawn.IsBurning())
			{
				return true;
			}
			return FireNear(pawn.Position, map, 2.5f);
		}

		/// <summary>该不该挪窝：着火、被贴脸，或者完全没有掩体又被敌人够得着。</summary>
		public static bool NeedsReposition(Pawn pawn, LocalTargetInfo target, Verb verb)
		{
			Map map = pawn?.Map;
			if (map == null || !target.IsValid)
			{
				return false;
			}
			if (InDanger(pawn))
			{
				return true;
			}
			float nearest = NearestHostileDistance(pawn);
			if (nearest < MinKeepDistance)
			{
				return true;
			}
			float cover = CoverUtility.TotalSurroundingCoverScore(pawn.Position, map);
			float reach = Mathf.Max(8f, (verb?.EffectiveRange ?? 20f) * 0.75f);
			return cover < 1f && nearest <= reach;
		}

		/// <summary>
		/// 找一个能打到目标、离敌人更远、又有掩体的开火位置；找不到（或原地已经足够好）返回 Invalid。
		/// </summary>
		public static IntVec3 FindFiringPosition(Pawn pawn, LocalTargetInfo target, Verb verb)
		{
			Map map = pawn?.Map;
			if (map == null || pawn.Dead || !pawn.Spawned || !target.IsValid || verb == null)
			{
				return IntVec3.Invalid;
			}
			List<Pawn> hostiles = Hostiles(pawn);
			float bestScore = ScoreCell(pawn, pawn.Position, target, verb, hostiles) + MinImprovement;
			IntVec3 best = IntVec3.Invalid;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, SearchRadius, true))
			{
				if (!CanFireFrom(pawn, cell, target, verb))
				{
					continue;
				}
				float score = ScoreCell(pawn, cell, target, verb, hostiles);
				if (score > bestScore)
				{
					bestScore = score;
					best = cell;
				}
			}
			if (best.IsValid && !pawn.CanReach(best, PathEndMode.OnCell, Danger.Deadly))
			{
				return IntVec3.Invalid;
			}
			return best;
		}

		/// <summary>离最近的敌人有多远；地图上没有敌人时返回一个大数。</summary>
		public static float NearestHostileDistance(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return 999f;
			}
			float nearest = float.MaxValue;
			foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (other == null || other == pawn || other.Dead || other.Downed || !other.Spawned
					|| !other.HostileTo(Faction.OfPlayer))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(other.Position);
				if (distance < nearest)
				{
					nearest = distance;
				}
			}
			return nearest == float.MaxValue ? 999f : nearest;
		}

		private static List<Pawn> Hostiles(Pawn pawn)
		{
			List<Pawn> list = new List<Pawn>();
			Map map = pawn?.Map;
			if (map == null)
			{
				return list;
			}
			foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
			{
				if (other != null && other != pawn && !other.Dead && !other.Downed
					&& other.Spawned && other.HostileTo(Faction.OfPlayer))
				{
					list.Add(other);
				}
			}
			return list;
		}

		private static bool CanFireFrom(Pawn pawn, IntVec3 cell, LocalTargetInfo target, Verb verb)
		{
			Map map = pawn.Map;
			if (!cell.InBounds(map) || !cell.Standable(map) || cell.Fogged(map))
			{
				return false;
			}
			if (cell.GetFirstPawn(map) != null || cell.IsForbidden(pawn))
			{
				return false;
			}
			float distance = cell.DistanceTo(target.Cell);
			float effective = Mathf.Max(4f, verb.EffectiveRange);
			if (distance < 4f || distance > effective * 0.95f)
			{
				return false;
			}
			if (!verb.CanHitTargetFrom(cell, target))
			{
				return false;
			}
			return !FireNear(cell, map, 1.5f);
		}

		private static float ScoreCell(
			Pawn pawn, IntVec3 cell, LocalTargetInfo target, Verb verb, List<Pawn> hostiles)
		{
			Map map = pawn.Map;
			float effective = Mathf.Max(4f, verb.EffectiveRange);
			float ideal = effective * PreferredRangeFactor;
			float distanceToTarget = cell.DistanceTo(target.Cell);

			float nearest = 999f;
			for (int i = 0; i < hostiles.Count; i++)
			{
				Pawn hostile = hostiles[i];
				if (hostile == null || hostile.Dead || hostile.Downed || !hostile.Spawned)
				{
					continue;
				}
				float distance = cell.DistanceTo(hostile.Position);
				if (distance < nearest)
				{
					nearest = distance;
				}
			}

			float score = 0f;
			// 掩体最重要：站到墙/沙袋后面，敌人命中率会明显下降。
			score += CoverUtility.TotalSurroundingCoverScore(cell, map) * 10f;
			// 离敌人越远越好（边打边撤的核心）。
			score += Mathf.Min(nearest, 25f) * 0.9f;
			// 别跑到射程边缘外，也别贴脸。
			score -= Mathf.Abs(distanceToTarget - ideal) * 0.7f;
			// 少跑路，避免为了 1 分掩体横穿半个基地。
			score -= pawn.Position.DistanceTo(cell) * 0.6f;
			if (FireNear(cell, map, 2f))
			{
				score -= 50f;
			}
			return score;
		}

		private static bool FireNear(IntVec3 cell, Map map, float radius)
		{
			if (map?.listerThings == null)
			{
				return false;
			}
			foreach (Thing fire in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
			{
				if (fire != null && fire.Spawned && !fire.Destroyed
					&& fire.Position.InHorDistOf(cell, radius))
				{
					return true;
				}
			}
			return false;
		}
	}
}
