using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 双持的战斗部分：
	/// 1) 双远程：主手开火时副手紧跟一轮自己的连发（各自用自己的精度、射程、伤害）。
	/// 2) 远程 + 近战：贴脸时优先用副手近战武器，而不是拿枪托砸。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_MkVDualWield
	{
		// Verb 内部的三个受保护字段：副手没有走暖机/瞄准流程，这里直接喂目标和取消站姿改写。
		private static readonly AccessTools.FieldRef<Verb, LocalTargetInfo> CurrentTargetRef =
			AccessTools.FieldRefAccess<Verb, LocalTargetInfo>("currentTarget");

		private static readonly AccessTools.FieldRef<Verb, LocalTargetInfo> CurrentDestinationRef =
			AccessTools.FieldRefAccess<Verb, LocalTargetInfo>("currentDestination");

		private static readonly AccessTools.FieldRef<Verb, bool> NonInterruptingRef =
			AccessTools.FieldRefAccess<Verb, bool>("nonInterruptingSelfCast");

		[HarmonyPatch(typeof(Verb_LaunchProjectile), nameof(Verb_LaunchProjectile.WarmupComplete))]
		[HarmonyPostfix]
		private static void OffhandFollowUp(Verb_LaunchProjectile __instance)
		{
			Pawn pawn = __instance?.CasterPawn;
			if (pawn == null || !NannyBotUtility.IsMkV(pawn) || !MkVLoadout.Enabled)
			{
				return;
			}
			ThingWithComps primary = pawn.equipment?.Primary;
			if (primary == null || __instance.EquipmentSource != primary)
			{
				return;
			}
			ThingWithComps offhand = MkVLoadout.Offhand(pawn);
			if (offhand == null || !offhand.def.IsRangedWeapon)
			{
				return;
			}
			if (!(offhand.TryGetComp<CompEquippable>()?.PrimaryVerb is Verb_LaunchProjectile verb))
			{
				return;
			}
			LocalTargetInfo target = __instance.CurrentTarget;
			// 副手射程比主手短时不要勉强开火（例如主手狙击枪、副手霰弹枪）。
			if (!target.IsValid || !verb.CanHitTarget(target))
			{
				return;
			}
			Fire(verb, target);
		}

		/// <summary>
		/// 让副手武器立刻打一轮：沿用武器自己的连发数和连发间隔，后续子弹由原版
		/// EquipmentTrackerTick → VerbTick 继续推进；因为不经过 Walkup/站姿流程，
		/// 不会覆盖主手的冷却站姿（否则慢速副手会把整台机器人的射速拖慢）。
		/// </summary>
		internal static void Fire(Verb verb, LocalTargetInfo target)
		{
			if (verb == null || !target.IsValid)
			{
				return;
			}
			if (verb.state != VerbState.Idle)
			{
				verb.Reset();
			}
			CurrentTargetRef(verb) = target;
			CurrentDestinationRef(verb) = LocalTargetInfo.Invalid;
			NonInterruptingRef(verb) = true;
			verb.WarmupComplete();
		}

		/// <summary>
		/// 副手武器不再单独出一条「开火」指令：原版 AttackStatic 只认主手动词，这条指令点了也
		/// 只会让 Mk-V 用主手打，留着反而误导；副手开火由上面的跟随逻辑负责。
		/// </summary>
		[HarmonyPatch(typeof(CompEquippable), nameof(CompEquippable.GetVerbsCommands))]
		[HarmonyPostfix]
		private static void HideOffhandCommands(CompEquippable __instance, ref IEnumerable<Command> __result)
		{
			ThingWithComps weapon = __instance?.parent;
			Pawn pawn = (weapon?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
			if (pawn == null || !NannyBotUtility.IsMkV(pawn) || !MkVLoadout.Enabled)
			{
				return;
			}
			if (weapon != MkVLoadout.Offhand(pawn))
			{
				return;
			}
			__result = Enumerable.Empty<Command>();
		}

		/// <summary>
		/// 远程 + 近战：主手是远程武器时，近战一律用副手的近战武器结算，
		/// 保留武器品质、材质、专属工具的伤害与护甲穿透。
		/// </summary>
		[HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack))]
		[HarmonyPrefix]
		private static void PreferOffhandMelee(Pawn_MeleeVerbs __instance, ref Verb verbToUse)
		{
			if (verbToUse != null)
			{
				return;
			}
			Pawn pawn = __instance?.Pawn;
			if (pawn == null || !NannyBotUtility.IsMkV(pawn) || !MkVLoadout.Enabled)
			{
				return;
			}
			ThingWithComps primary = pawn.equipment?.Primary;
			if (primary == null || !primary.def.IsRangedWeapon)
			{
				// 主手也是近战时按原版权重挑，不强行指定。
				return;
			}
			ThingWithComps offhand = MkVLoadout.Offhand(pawn);
			if (offhand == null || !offhand.def.IsMeleeWeapon)
			{
				return;
			}
			Verb melee = offhand.TryGetComp<CompEquippable>()?.AllVerbs?
				.FirstOrDefault(v => v != null && v.IsMeleeAttack);
			if (melee != null)
			{
				verbToUse = melee;
			}
		}
	}
}
