using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版开发工具「大逃杀」（<see cref="ArenaUtility.PerformBattleRoyale"/>）会按
	/// <c>EloUtility.CalculateRating(combatPower, 1500, 60)</c> 给每个品种算评分，也就是
	/// log10(combatPower / 60)：combatPower 为 0 时评分是 -∞，两个 0 战力品种对上
	/// 期望值就变成 NaN，最后 <c>GenMath.RoundRandom(NaN)</c> 得到负数，
	/// <c>Enumerable.Repeat(kind, 负数)</c> 抛 ArgumentOutOfRangeException: count，
	/// 整个工具直接中断（原版所有品种的 combatPower 都 &gt; 0，所以官方没踩到）。
	///
	/// ColonyKit 的 Mk-I～V 与通用保姆机器人故意把 combatPower 设成 0——不想让机器人
	/// 抬高殖民地的威胁点数（生物科技 DLC 下 ColonyMech 会按 combatPower 加点数）。
	/// 这里在开打前把 0 战力品种剔掉，既不改平衡，也不让开发工具崩。
	/// </summary>
	[HarmonyPatch(typeof(ArenaUtility), nameof(ArenaUtility.PerformBattleRoyale))]
	internal static class Patch_ArenaUtility_ZeroCombatPower
	{
		private static bool Prefix(ref IEnumerable<PawnKindDef> kindsEnumerable)
		{
			if (kindsEnumerable == null)
			{
				return true;
			}
			List<PawnKindDef> kinds = kindsEnumerable
				.Where(kind => kind != null && kind.combatPower > 0f)
				.ToList();
			if (kinds.Count < 2)
			{
				ModLog.Warn("Robotics", "大逃杀已跳过：combatPower 大于 0 的品种不足 2 个。");
				return false;
			}
			kindsEnumerable = kinds;
			return true;
		}
	}
}
