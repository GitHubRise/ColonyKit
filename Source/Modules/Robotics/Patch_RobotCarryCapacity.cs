using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 机器人携带能力直接取设置页的值。原版 <c>CarryingCapacity</c> 带一个
	/// <c>StatPart_BodySize</c>，会把 Def 里的基数按体型再乘一次；这里在 <c>GetStatValue</c>
	/// 之后覆盖成最终值，所以设置里填 132，信息卡上就是 132，搬运/拾取件数也按它算。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_RobotCarryCapacity
	{
		[HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue),
			new[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })]
		[HarmonyPostfix]
		private static void OverrideCarryCapacity(Thing thing, StatDef stat, ref float __result)
		{
			if (stat != StatDefOf.CarryingCapacity
				|| !(thing is Pawn pawn)
				|| !NannyBotUtility.IsNannyBot(pawn))
			{
				return;
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			float value = NannyBotUtility.IsMkV(pawn)
				? settings.mkvCarryCapacity
				: settings.robotCarryCapacity;
			__result = Mathf.Clamp(value,
				RoboticsSettings.MinRobotCarryCapacity,
				RoboticsSettings.MaxRobotCarryCapacity);
		}
	}
}
