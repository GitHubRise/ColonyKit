using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// MakeRoomFor 有两个重载，这里精确瞄准带 out 参数的那个：它才是真正会丢武器的地方，
	/// 单参数重载只是转调它。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_MkVEquipment_MakeRoomFor
	{
		[HarmonyTargetMethod]
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Pawn_EquipmentTracker), "MakeRoomFor",
				new[] { typeof(ThingWithComps), typeof(ThingWithComps).MakeByRefType() });
		}

		[HarmonyPrefix]
		private static bool Prefix(
			Pawn_EquipmentTracker __instance, ThingWithComps eq, ref ThingWithComps dropped)
		{
			dropped = null;
			if (eq?.def == null || eq.def.equipmentType != EquipmentType.Primary)
			{
				return true;
			}
			Pawn pawn = __instance?.pawn;
			if (pawn == null || !NannyBotUtility.IsMkV(pawn) || !MkVLoadout.Enabled)
			{
				return true;
			}
			if (pawn.MapHeld == null || __instance.Primary == null)
			{
				return true;
			}
			// 交给 AddEquipment 前缀统一安排，这里不要丢掉主手。
			return false;
		}
	}

	/// <summary>
	/// 双持换装：原版会先把主手丢到地上再装新武器；Mk-V 改成把旧武器收进副手位，
	/// 两把都满时按「保留专属武器、优先换掉同类武器」的规则丢一把，再让新武器进主手。
	/// 掉在地上的武器和原版一样解除禁用。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
	internal static class Patch_MkVEquipment_AddEquipment
	{
		[HarmonyPrefix]
		private static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
		{
			Pawn pawn = __instance?.pawn;
			if (!MkVLoadout.TryPrepareEquip(pawn, newEq, out ThingWithComps keep, out ThingWithComps discard))
			{
				return true;
			}
			// 新武器还在别的容器里/还在地图上时让原版自己处理，避免把它从原位置抢走。
			if (newEq.holdingOwner != null || newEq.Spawned)
			{
				return true;
			}
			if (discard != null && !DropOrStash(pawn, __instance, discard))
			{
				// 实在放不下就先交给原版处理，免得武器凭空消失。
				ModLog.Warn("Robotics",
					$"MkV loadout could not replace {discard.def?.defName}; falling back.");
				return true;
			}

			if (keep != null)
			{
				__instance.Remove(keep);
			}

			ThingOwner owner = __instance.GetDirectlyHeldThings();
			if (owner == null)
			{
				if (keep != null)
				{
					MkVLoadout.AddAsOffhand(pawn, keep);
				}
				return true;
			}

			// 主手仍然是新武器还是旧武器，按「有远程优先远程、否则评分高的」决定，
			// 这样装上近战副手时不会把枪挤到副手位、导致 Mk-V 没法开火。
			ThingWithComps main = keep == null ? newEq : MkVLoadout.BetterMain(newEq, keep);
			ThingWithComps offhand = main == newEq ? keep : newEq;

			bool mainAdded = owner.TryAdd(main);
			if (!mainAdded && offhand != null)
			{
				// 主手放不进去时退而求其次：先保证手上还有一把武器。
				mainAdded = owner.TryAdd(offhand);
				offhand = null;
			}
			if (mainAdded && offhand != null && !owner.TryAdd(offhand))
			{
				ModLog.Warn("Robotics",
					$"MkV loadout could not keep {offhand.def?.defName} as offhand.");
			}
			if (!mainAdded)
			{
				ModLog.Warn("Robotics", $"MkV loadout could not equip {newEq.def?.defName}.");
			}

			// 已经自己装好了，跳过原版 AddEquipment（否则它会因为「已有主手」报错并拒绝）。
			return false;
		}

		private static bool DropOrStash(Pawn pawn, Pawn_EquipmentTracker tracker, ThingWithComps weapon)
		{
			if (weapon == null)
			{
				return true;
			}
			if (tracker.TryDropEquipment(weapon, out ThingWithComps dropped, pawn.Position))
			{
				dropped?.SetForbidden(false, false);
				return true;
			}
			// 地上放不下（例如位置异常）就塞进随身物品，至少不销毁武器。
			if (pawn.inventory?.innerContainer != null
				&& pawn.inventory.innerContainer.TryAddOrTransfer(weapon))
			{
				return true;
			}
			return false;
		}
	}
}
