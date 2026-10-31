using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 电弧刃的命中特效：命中敌对目标时按设置页的几率施加「电弧麻痹」（意识压到麻醉级，
	/// 目标当场倒地不醒、不会致死），并有几率把麻痹连锁给目标周围的其他敌人。只认电弧刃
	/// 本身，别人拿同一把刀也照样生效。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_MkVArcBladeEffects
	{
		/// <summary>游戏内 1 小时 = 2,500 tick。</summary>
		public const int TicksPerHour = 2500;

		// ApplyMeleeDamageToTarget 只在真正命中时调用，未命中/被闪避都不会走到这里。
		[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
		[HarmonyPostfix]
		private static void ApplyMeleeEffects(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
		{
			if (__instance?.EquipmentSource?.def != NannyBotDefOf.CK_ArcBlade)
			{
				return;
			}
			Pawn caster = __instance.CasterPawn;
			if (caster == null || !(target.Thing is Pawn victim) || victim.Dead || !victim.Spawned)
			{
				return;
			}
			// 只对敌对目标生效：社交斗殴、误伤队友都不会把人麻倒 10 小时。
			if (!victim.HostileTo(caster))
			{
				return;
			}

			RoboticsSettings settings = NannyBotUtility.Settings;
			int ticks = HoursToTicks(settings.mkvArcBladeParalysisHours);
			if (Rand.Chance(Mathf.Clamp01(settings.mkvArcBladeParalysisChance)))
			{
				ApplyParalysis(victim, ticks);
			}
			if (Rand.Chance(Mathf.Clamp01(settings.mkvArcBladeChainChance)))
			{
				ApplyChain(caster, victim, settings.mkvArcBladeChainRadius, ticks);
			}
		}

		public static int HoursToTicks(float hours)
		{
			return Mathf.Max(1, Mathf.RoundToInt(hours * TicksPerHour));
		}

		/// <summary>施加/刷新电弧麻痹；已经有这个 hediff 时只把倒计时拨回满值。</summary>
		public static void ApplyParalysis(Pawn victim, int ticks)
		{
			HediffDef def = NannyBotDefOf.CK_ArcBladeParalysis;
			if (victim == null || victim.Dead || def == null || ticks <= 0)
			{
				return;
			}
			Hediff existing = victim.health?.hediffSet?.GetFirstHediffOfDef(def);
			if (existing != null)
			{
				existing.TryGetComp<HediffComp_Disappears>()?.SetDuration(ticks);
				return;
			}
			Hediff hediff = HediffMaker.MakeHediff(def, victim);
			hediff.TryGetComp<HediffComp_Disappears>()?.SetDuration(ticks);
			// AddHediff 后原版会走一次 ShouldBeDowned 检查，但意识是被缓存的，
			// 这里先让能力值失效再重算一次，保证当 tick 就倒地而不是等下一个健康检查节拍。
			victim.health.AddHediff(hediff);
			victim.health.capacities?.Notify_CapacityLevelsDirty();
			victim.health.CheckForStateChange(null, hediff);
		}

		/// <summary>连锁：给目标周围 ring 格内的其他敌对目标也挂上麻痹。</summary>
		public static void ApplyChain(Pawn caster, Pawn victim, float radius, int ticks)
		{
			Map map = victim?.Map;
			if (caster == null || map == null || radius <= 0.1f)
			{
				return;
			}
			float radiusSquared = radius * radius;
			foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
			{
				if (other == null || other == victim || other.Dead || !other.Spawned)
				{
					continue;
				}
				if (!other.HostileTo(caster))
				{
					continue;
				}
				if (other.Position.DistanceToSquared(victim.Position) > radiusSquared)
				{
					continue;
				}
				ApplyParalysis(other, ticks);
			}
		}
	}
}
