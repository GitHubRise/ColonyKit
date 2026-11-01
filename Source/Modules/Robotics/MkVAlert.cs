using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 敌袭预警：地图上还有敌人、同时最近收到过原版威胁信（袭击、虫灾、机械集群……）时算「预警中」。
	/// 预警期间 Mk-V 放下家务、主动向敌人大致方向推进，用远程武器先手开火。
	/// 只看原版 LetterDef（ThreatBig / ThreatSmall 等），不读玩家没看到的细节；推进目标是最接近的敌人位置。
	/// </summary>
	internal static class MkVAlert
	{
		/// <summary>威胁信在多少 tick 内算数（约 2 游戏小时）。</summary>
		public const int AlertTicks = 6000;

		/// <summary>预警时的索敌范围：足够追上从地图边缘赶来的袭击。</summary>
		public const float AlertEngageRange = 250f;

		/// <summary>同一 tick 内只扫一次：CanWork/思考树会高频询问。</summary>
		private static Map cachedMap;

		private static int cachedTick = -1;

		private static bool cachedActive;

		public static bool IsActive(Map map)
		{
			if (map == null || Current.Game == null)
			{
				return false;
			}
			int now = Find.TickManager?.TicksGame ?? 0;
			if (map == cachedMap && now == cachedTick)
			{
				return cachedActive;
			}
			cachedMap = map;
			cachedTick = now;
			cachedActive = ComputeActive(map);
			return cachedActive;
		}

		private static bool ComputeActive(Map map)
		{
			if (map.dangerWatcher != null && map.dangerWatcher.DangerRating == StoryDanger.High)
			{
				return true;
			}
			if (!HasHostile(map))
			{
				return false;
			}
			return HasRecentThreatLetter();
		}

		/// <summary>地图上还有活着的敌人（不要求可见）。</summary>
		public static bool HasHostile(Map map)
		{
			if (map?.mapPawns == null)
			{
				return false;
			}
			foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (pawn != null && !pawn.Dead && !pawn.Downed && pawn.Spawned
					&& pawn.HostileTo(Faction.OfPlayer))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>最近有没有收到过原版威胁信；已被玩家关掉的信也算（信里有 ID 的那份列表仍在）。</summary>
		public static bool HasRecentThreatLetter()
		{
			LetterStack stack = Find.LetterStack;
			if (stack == null)
			{
				return false;
			}
			int now = Find.TickManager.TicksGame;
			List<Letter> letters = stack.LettersListForReading;
			for (int i = 0; i < letters.Count; i++)
			{
				Letter letter = letters[i];
				string name = letter?.def?.defName;
				if (string.IsNullOrEmpty(name)
					|| name.IndexOf("Threat", StringComparison.Ordinal) != 0)
				{
					continue;
				}
				if (now - letter.arrivalTick <= AlertTicks)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>离指定位置最近的敌人位置；没有敌人时返回 Invalid。雾里的敌人也算（这是「预警情报」）。</summary>
		public static IntVec3 NearestHostileCell(Map map, IntVec3 from)
		{
			if (map?.mapPawns == null)
			{
				return IntVec3.Invalid;
			}
			IntVec3 best = IntVec3.Invalid;
			float bestDistance = float.MaxValue;
			foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned
					|| !pawn.HostileTo(Faction.OfPlayer))
				{
					continue;
				}
				float distance = pawn.Position.DistanceToSquared(from);
				if (distance < bestDistance)
				{
					best = pawn.Position;
					bestDistance = distance;
				}
			}
			return best;
		}
	}
}
