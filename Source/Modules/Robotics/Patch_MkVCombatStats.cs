using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 的攻速与命中率由设置页实时缩放；只影响 Mk-V，不改动其他机器人。</summary>
	[HarmonyPatch]
	internal static class Patch_MkVCombatStats
	{
		[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown),
			new[] { typeof(Verb), typeof(Pawn) })]
		[HarmonyPostfix]
		private static void AdjustCooldown(Pawn attacker, ref float __result)
		{
			if (!NannyBotUtility.IsMkV(attacker) || __result <= 0f)
			{
				return;
			}
			float speed = Mathf.Clamp(NannyBotUtility.Settings.mkvAttackSpeed,
				RoboticsSettings.MinMkvAttackSpeed, RoboticsSettings.MaxMkvAttackSpeed);
			if (speed > 0.001f)
			{
				__result /= speed;
			}
		}

		[HarmonyPatch(typeof(ShotReport), nameof(ShotReport.HitFactorFromShooter),
			new[] { typeof(Thing), typeof(float), typeof(float?) })]
		[HarmonyPostfix]
		private static void AdjustRangedHit(Thing caster, ref float __result)
		{
			if (caster is Pawn pawn && NannyBotUtility.IsMkV(pawn))
			{
				__result *= Mathf.Clamp(NannyBotUtility.Settings.mkvHitChance,
					RoboticsSettings.MinMkvHitChance, RoboticsSettings.MaxMkvHitChance);
			}
		}

		[HarmonyPatch(typeof(Verb_MeleeAttack), "GetNonMissChance",
			new[] { typeof(LocalTargetInfo) })]
		[HarmonyPostfix]
		private static void AdjustMeleeHit(Verb_MeleeAttack __instance, ref float __result)
		{
			if (__instance?.CasterPawn is Pawn pawn && NannyBotUtility.IsMkV(pawn))
			{
				__result *= Mathf.Clamp(NannyBotUtility.Settings.mkvHitChance,
					RoboticsSettings.MinMkvHitChance, RoboticsSettings.MaxMkvHitChance);
			}
		}
	}
}
