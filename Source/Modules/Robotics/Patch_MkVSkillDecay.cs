using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 内置「遗忘速度 -50%」：原版 <see cref="SkillRecord.Interval"/> 在等级 ≥10 时
	/// 每 200 tick 按等级表扣经验（10 级 -0.1 … 20 级 -12）。
	/// 这里对 Mk-V 用同一张表 ×0.5 结算；如果它又抽到「超凡记忆」，两个 0.5 相乘叠加成 ×0.25。
	/// 「完美记忆」（Anomaly）本来就不掉技能，直接交回原版。
	/// </summary>
	[HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
	internal static class Patch_MkVSkillDecay
	{
		private static readonly FieldInfo PawnField = AccessTools.Field(typeof(SkillRecord), "pawn");

		private static bool Prefix(SkillRecord __instance)
		{
			Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
			if (!NannyBotUtility.IsMkV(pawn))
			{
				return true;
			}

			TraitSet traits = pawn.story?.traits;
			if (ModsConfig.AnomalyActive && traits != null
				&& traits.HasTrait(TraitDefOf.PerfectMemory))
			{
				return true;
			}

			float factor = 0.5f;
			if (traits != null && traits.HasTrait(TraitDefOf.GreatMemory))
			{
				// 超凡记忆再减半：0.5 × 0.5 = 0.25。
				factor *= 0.5f;
			}
			float xp = DecayPerInterval(__instance.levelInt) * factor;
			if (xp != 0f)
			{
				__instance.Learn(xp);
			}
			return false;
		}

		/// <summary>与 1.6 原版 <c>SkillRecord.Interval</c> 的每 200 tick 扣减表保持一致。</summary>
		private static float DecayPerInterval(int level)
		{
			switch (level)
			{
				case 10:
					return -0.1f;
				case 11:
					return -0.2f;
				case 12:
					return -0.4f;
				case 13:
					return -0.6f;
				case 14:
					return -1f;
				case 15:
					return -1.8f;
				case 16:
					return -2.8f;
				case 17:
					return -4f;
				case 18:
					return -6f;
				case 19:
					return -8f;
				case 20:
					return -12f;
				default:
					return 0f;
			}
		}
	}
}
