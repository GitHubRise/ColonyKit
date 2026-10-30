using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的攻速与命中率由设置页实时缩放，专属武器的射程加成也在这里；只影响 Mk-V，
	/// 不改动其他机器人和同款武器在别人手上的数值。
	/// </summary>
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

		[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedRange),
			new[] { typeof(Verb), typeof(Thing) })]
		[HarmonyPostfix]
		private static void AdjustExclusiveRange(Verb ownerVerb, Thing attacker, ref float __result)
		{
			if (__result <= 0f || !(attacker is Pawn pawn) || !NannyBotUtility.IsMkV(pawn))
			{
				return;
			}
			// 专属武器加成挂在 ThingDef 的 modExtension 上，其他武器不受影响。
			DefModExtension_MkvExclusiveWeapon exclusive =
				ownerVerb?.EquipmentSource?.def?.GetModExtension<DefModExtension_MkvExclusiveWeapon>();
			if (exclusive == null || exclusive.rangeBonus <= 0f)
			{
				return;
			}
			float range = __result + exclusive.rangeBonus;
			// 原版 AdjustedRange 已经把大雾/暴风雪之类的天气射程上限压进结果里，
			// 加成后要再压一次，否则专属武器能在恶劣天气里突破上限。
			Map map = pawn.MapHeld;
			if (map != null && map.weatherManager.CurWeatherMaxRangeCap >= 0f)
			{
				range = Mathf.Min(range, map.weatherManager.CurWeatherMaxRangeCap);
			}
			__result = range;
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
