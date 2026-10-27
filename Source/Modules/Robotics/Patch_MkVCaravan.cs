using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 远程能力：200 携带量、9.8 格/天，并且全 Mk-V 商队不夜间休息。</summary>
	[HarmonyPatch]
	internal static class Patch_MkVCaravan
	{
		private const float MkVCaravanTilesPerDay = 9.8f;
		private const float MkVCaravanCapacity = 200f;

		[HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity),
			new[] { typeof(Pawn), typeof(StringBuilder) })]
		[HarmonyPostfix]
		private static void OverrideCapacity(Pawn p, ref float __result)
		{
			if (NannyBotUtility.IsMkV(p))
			{
				__result = MkVCaravanCapacity;
			}
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
}
