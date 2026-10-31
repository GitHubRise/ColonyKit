using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 1 L 一瓶的瓶装水：矿泉水（未处理，可能闹肚子）和纯净水（过滤，不会中毒）。
	/// 两者都恢复 0.2 口渴，不需要容器。
	/// </summary>
	internal static class MineralWaterUtility
	{
		public const float LitersPerBottle = 1f;

		public const float ThirstPerBottle = 0.2f;

		private static ThingDef cachedMineral;

		private static ThingDef cachedPurified;

		public static ThingDef Def
		{
			get
			{
				if (cachedMineral == null)
				{
					cachedMineral = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MineralWater");
				}
				return cachedMineral;
			}
		}

		public static ThingDef PurifiedDef
		{
			get
			{
				if (cachedPurified == null)
				{
					cachedPurified = DefDatabase<ThingDef>.GetNamedSilentFail("CK_PurifiedWater");
				}
				return cachedPurified;
			}
		}

		public static bool IsMineralWater(Thing thing)
		{
			return thing != null && thing.def == Def;
		}

		public static bool IsPurifiedWater(Thing thing)
		{
			return thing != null && thing.def == PurifiedDef;
		}

		public static bool IsBottledWater(Thing thing)
		{
			return IsMineralWater(thing) || IsPurifiedWater(thing);
		}

		public static Thing Carried(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				if (IsBottledWater(thing) && thing.stackCount > 0)
				{
					return thing;
				}
			}
			return null;
		}

		public static Thing FindSpawned(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				return null;
			}
			return FindSpawnedDef(pawn, Def) ?? FindSpawnedDef(pawn, PurifiedDef);
		}

		private static Thing FindSpawnedDef(Pawn pawn, ThingDef def)
		{
			if (def == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
			{
				if (thing.stackCount <= 0
					|| thing.IsForbidden(pawn)
					|| thing.IsBurning()
					|| !pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
				{
					continue;
				}
				return thing;
			}
			return null;
		}

		public static Thing FindInCaravan(Caravan caravan)
		{
			if (caravan == null)
			{
				return null;
			}
			List<Thing> items = CaravanInventoryUtility.AllInventoryItems(caravan);
			for (int i = 0; i < items.Count; i++)
			{
				if (IsBottledWater(items[i]) && items[i].stackCount > 0)
				{
					return items[i];
				}
			}
			return null;
		}
	}

	/// <summary>喝瓶装水时给口渴加 0.2（1 L）；矿泉水可能带未处理水污染，纯净水不会。</summary>
	[HarmonyLib.HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
	internal static class Patch_Thing_Ingested_MineralWater
	{
		private static void Postfix(Thing __instance, Pawn ingester)
		{
			if (ingester == null)
			{
				return;
			}
			Need_Thirst thirst = ingester.needs?.TryGetNeed<Need_Thirst>();
			if (thirst == null)
			{
				return;
			}
			if (MineralWaterUtility.IsPurifiedWater(__instance))
			{
				thirst.Drink(MineralWaterUtility.ThirstPerBottle);
			}
			else if (MineralWaterUtility.IsMineralWater(__instance))
			{
				thirst.Drink(MineralWaterUtility.ThirstPerBottle);
				SanitationUtil.ContaminationCheckDrinking(ingester, ContaminationLevel.Untreated);
			}
		}
	}
}
