using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 远程能力：9.8 格/天，并且全 Mk-V 商队不夜间休息。
	/// 负重上限统一取「携带能力」（设置页里的 Mk-V 携带能力，默认 255），
	/// 信息卡、Gear 页的 MassCarried、背包/装备质量判定和商队质量容量读同一个数。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_MkVCaravan
	{
		private const float MkVCaravanTilesPerDay = 9.8f;

		[HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity),
			new[] { typeof(Pawn), typeof(StringBuilder) })]
		[HarmonyPrefix]
		private static bool OverrideCapacity(Pawn p, StringBuilder explanation, ref float __result)
		{
			if (!NannyBotUtility.IsMkV(p))
			{
				return true;
			}
			// 原版这里按 BodySize × 35 算；Mk-V 改成和属性面板同一个「携带能力」，
			// 避免出现「信息卡 255、Gear 页 /200」两套数。
			__result = p.GetStatValue(StatDefOf.CarryingCapacity);
			if (explanation != null)
			{
				if (explanation.Length > 0)
				{
					explanation.AppendLine();
				}
				explanation.Append("  - " + p.LabelShortCap + ": " + __result.ToStringMassOffset());
			}
			return false;
		}

		[HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove),
			new[]
			{
				typeof(List<Pawn>), typeof(float), typeof(float), typeof(bool), typeof(StringBuilder)
			})]
		[HarmonyPostfix]
		private static void OverrideCaravanSpeed(List<Pawn> pawns, ref int __result)
		{
			if (pawns != null && pawns.Count > 0 && pawns.All(NannyBotUtility.IsMkV))
			{
				__result = Mathf.Max(1, Mathf.RoundToInt(60000f / MkVCaravanTilesPerDay));
			}
		}

		[HarmonyPatch(typeof(Caravan), nameof(Caravan.NightResting), MethodType.Getter)]
		[HarmonyPrefix]
		private static bool SkipNightRestForMkV(Caravan __instance, ref bool __result)
		{
			List<Pawn> pawns = __instance?.PawnsListForReading;
			if (pawns != null && pawns.Count > 0 && pawns.All(NannyBotUtility.IsMkV))
			{
				__result = false;
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(CaravanUtility), nameof(CaravanUtility.IsOwner))]
		[HarmonyPostfix]
		private static void AllowMkVCaravanOwner(Pawn pawn, Faction caravanFaction, ref bool __result)
		{
			if (!__result && NannyBotUtility.IsMkV(pawn) && caravanFaction == Faction.OfPlayer)
			{
				__result = true;
			}
		}

		[HarmonyPatch(typeof(DaysWorthOfFoodCalculator), nameof(DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood),
			new[] { typeof(Caravan) })]
		[HarmonyPostfix]
		private static void InfiniteFoodForMkV(Caravan caravan, ref float __result)
		{
			List<Pawn> pawns = caravan?.PawnsListForReading;
			if (pawns != null && pawns.Count > 0 && pawns.All(NannyBotUtility.IsMkV))
			{
				__result = DaysWorthOfFoodCalculator.InfiniteDaysWorthOfFood;
			}
		}
	}

	/// <summary>
	/// 组建远行队时让 Mk-V 也像殖民者一样打包物资：原版只给殖民地居民
	/// <c>PrepareCaravan_GatherItems</c>，其他成员一律赶到集合点等待。
	/// 不改的话，只有 Mk-V 的远行队谁也搬不动货，物资全留在原地、商队空手出发。
	/// </summary>
	[HarmonyPatch(typeof(LordToil_PrepareCaravan_GatherItems), nameof(LordToil_PrepareCaravan_GatherItems.UpdateAllDuties))]
	internal static class Patch_MkVCaravanGatherItems
	{
		[HarmonyPostfix]
		private static void GatherItemsToo(LordToil_PrepareCaravan_GatherItems __instance)
		{
			List<Pawn> pawns = __instance?.lord?.ownedPawns;
			if (pawns == null)
			{
				return;
			}
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (!NannyBotUtility.IsMkV(pawn))
				{
					continue;
				}
				// 只在原版给了「等其他人搬完」的等待职责时改写，别覆盖别的流程。
				if (pawn.mindState?.duty?.def == DutyDefOf.PrepareCaravan_Wait)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherItems);
				}
			}
		}
	}
}
