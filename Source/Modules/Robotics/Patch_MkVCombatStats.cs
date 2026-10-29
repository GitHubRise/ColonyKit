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
			// ArcEmitter 使用独立的“连发间隔”设置，默认 1.4 秒；其他 Mk-V
			// 近战武器继续由攻击速度倍率控制。
			if (attacker.equipment?.Primary?.def == NannyBotDefOf.CK_ArcEmitter)
			{
				__result = Mathf.Clamp(NannyBotUtility.Settings.mkvBurstCooldown,
					RoboticsSettings.MinMkvBurstCooldown, RoboticsSettings.MaxMkvBurstCooldown);
				return;
			}
			float speed = Mathf.Clamp(NannyBotUtility.Settings.mkvAttackSpeed,
				RoboticsSettings.MinMkvAttackSpeed, RoboticsSettings.MaxMkvAttackSpeed);
			if (speed > 0.001f)
			{
				__result /= speed;
			}
		}

		[HarmonyPatch(typeof(Verb), nameof(Verb.BurstShotCount), MethodType.Getter)]
		[HarmonyPostfix]
		private static void AdjustBurstShotCount(Verb __instance, ref int __result)
		{
			if (__instance?.CasterPawn is Pawn pawn
				&& NannyBotUtility.IsMkV(pawn)
				&& __instance.EquipmentSource?.def == NannyBotDefOf.CK_ArcEmitter)
			{
				__result = Mathf.Clamp(NannyBotUtility.Settings.mkvBurstShotCount,
					RoboticsSettings.MinMkvBurstShotCount, RoboticsSettings.MaxMkvBurstShotCount);
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
