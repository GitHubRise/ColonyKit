using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 商队里原版只处理食物/休息/娱乐，不会走 Need_Thirst.NeedInterval，口渴实际上被冻住。
	/// 这里让口渴在商队里按正常速率下降；水优先喝随身水盆/水桶，没有就按地块
	/// 的河流/降雨找水（和 DBH 的商队取水一致），找不到才会渴死。
	/// </summary>
	[HarmonyPatch(typeof(Caravan_NeedsTracker), "NeedsTrackerTickInterval")]
	internal static class Patch_CaravanThirstTick
	{
		private static void Postfix(Caravan_NeedsTracker __instance, int delta)
		{
			Caravan caravan = __instance?.caravan;
			if (caravan?.PawnsListForReading == null)
			{
				return;
			}
			if (Find.TickManager.TicksGame % 150 != 0)
			{
				return;
			}
			foreach (Pawn pawn in caravan.PawnsListForReading)
			{
				if (pawn == null || pawn.Dead)
				{
					continue;
				}
				pawn.needs?.TryGetNeed<Need_Thirst>()?.NeedInterval();
			}
		}
	}

	[HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds")]
	internal static class Patch_CaravanThirstDrink
	{
		private const float LitersPerDrink = 3f;

		private const float ThirstPerDrink = 0.6f;

		private static void Prefix(Caravan_NeedsTracker __instance, Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
			{
				return;
			}
			if (Find.TickManager.TicksGame % 150 != 0)
			{
				return;
			}
			Need_Thirst thirst = pawn.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null || thirst.CurLevel > 0.55f)
			{
				return;
			}
			Thing bottle = MineralWaterUtility.FindInCaravan(__instance.caravan);
			if (bottle != null)
			{
				bool purified = MineralWaterUtility.IsPurifiedWater(bottle);
				bottle.stackCount--;
				if (bottle.stackCount <= 0)
				{
					bottle.Destroy();
				}
				thirst.Drink(MineralWaterUtility.ThirstPerBottle);
				if (!purified)
				{
					SanitationUtil.ContaminationCheckDrinking(pawn, ContaminationLevel.Untreated);
				}
				return;
			}
			CompBasinWater container = FindWaterContainer(__instance.caravan);
			if (container != null)
			{
				float taken = container.Drain(Mathf.Min(LitersPerDrink, container.Water));
				if (taken > 0.01f)
				{
					thirst.Drink(ThirstPerDrink * (taken / LitersPerDrink));
					SanitationUtil.ContaminationCheckDrinking(pawn, container.Contamination);
					return;
				}
			}
			TryForageWater(__instance.caravan, pawn, thirst);
		}

		/// <summary>
		/// 地块有河流就一定能取水；否则按降雨量给概率（0–2000 映射到 0–1）。
		/// 取到的水按未处理水做污染判定。
		/// </summary>
		internal static bool TryForageWater(Caravan caravan, Pawn pawn, Need_Thirst thirst)
		{
			if (caravan == null || !caravan.Tile.Valid)
			{
				return false;
			}
			Tile tile = Find.WorldGrid[caravan.Tile];
			bool river = tile.Mutators != null
				&& tile.Mutators.Any(m => m.categories != null && m.categories.Contains("River"));
			float chance = GenMath.LerpDoubleClamped(0f, 2000f, 0f, 1f, tile.rainfall);
			if (!river && !Rand.Chance(chance))
			{
				return false;
			}
			thirst.Drink(ThirstPerDrink);
			SanitationUtil.ContaminationCheckWater(pawn, ContaminationLevel.Untreated);
			return true;
		}

		private static CompBasinWater FindWaterContainer(Caravan caravan)
		{
			if (caravan == null)
			{
				return null;
			}
			List<Thing> items = CaravanInventoryUtility.AllInventoryItems(caravan);
			for (int i = 0; i < items.Count; i++)
			{
				CompBasinWater container = items[i].TryGetComp<CompBasinWater>();
				if (container != null && container.Water > 0.5f)
				{
					return container;
				}
			}
			return null;
		}
	}
}
