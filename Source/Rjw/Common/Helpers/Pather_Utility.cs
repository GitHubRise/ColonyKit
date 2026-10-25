#nullable disable

﻿using Verse;
using Verse.AI;

namespace rjw
{
	public static class Pather_Utility
	{
		/// <summary>
		/// 检查小人到目标的距离，使用随性性行为的
		/// 距离配置。此检查相对较快，会忽略墙体。
		/// </summary>
		/// <param name="pawn">作为起点位置的小人。</param>
		/// <param name="target">目标位置。</param>
		/// <returns>目标是否足够近。</returns>
		public static bool cells_to_target_casual(Pawn pawn, LocalTargetInfo target)
		{
			if (!target.IsValid) return false;
			return pawn.Position.DistanceToSquared(target.Cell) < RJWSettings.maxDistanceCellsCasual * RJWSettings.maxDistanceCellsCasual;
		}

		/// <summary>
		/// 检查小人到目标的距离，使用强暴的
		/// 距离配置。此检查相对较快，会忽略墙体。
		/// </summary>
		/// <param name="pawn">作为起点位置的小人。</param>
		/// <param name="target">目标位置。</param>
		/// <returns>目标是否足够近。</returns>
		public static bool cells_to_target_rape(Pawn pawn, LocalTargetInfo target)
		{
			if (!target.IsValid) return false;
			return pawn.Position.DistanceToSquared(target.Cell) < RJWSettings.maxDistanceCellsRape * RJWSettings.maxDistanceCellsRape;
		}

		/// <summary>
		/// 检查小人能否到达目标。此检查实际会计算寻路，因此
		/// 开销较大。在评估多个小人时，尽量把它放在最后
		/// 检查。
		/// </summary>
		/// <param name="pawn">作为起点位置的小人。</param>
		/// <param name="target">目标位置。</param>
		/// <param name="maxDanger">路径上允许的最大危险度。</param>
		/// <param name="peMode">寻路器使用的最终目标模式。</param>
		/// <returns>目标是否可达。</returns>
		public static bool can_path_to_target(
			Pawn pawn,
			LocalTargetInfo target,
			Danger maxDanger = Danger.Some,
			PathEndMode peMode = PathEndMode.Touch)
		{
			if (!target.IsValid) return false;

			var startPos = pawn.Position;
			var traversalParams = TraverseParms.For(pawn, maxDanger);

			// 如果无限制，直接让 RimWorld 用它通常的可达性
			// 检查来优化。
			if (RJWSettings.maxDistancePathCost <= 0)
				return pawn.Map.reachability.CanReach(startPos, target, peMode, traversalParams);

			// 否则，我们就需要真正计算整条路径的代价。
			using var pawnPath = pawn.Map.pathFinder.FindPathNow(startPos, target, traversalParams, peMode: peMode);
			return pawnPath.TotalCost <= RJWSettings.maxDistancePathCost;
		}
	}
}
