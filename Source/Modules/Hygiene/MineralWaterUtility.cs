using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>1 L 一瓶的矿泉水：喝水恢复 0.2 口渴，不需要容器。</summary>
	internal static class MineralWaterUtility
	{
		public const float LitersPerBottle = 1f;

		public const float ThirstPerBottle = 0.2f;

		private static ThingDef cachedDef;

		public static ThingDef Def
		{
			get
			{
				if (cachedDef == null)
				{
					cachedDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_MineralWater");
				}
				return cachedDef;
			}
		}

		public static bool IsMineralWater(Thing thing)
		{
			return thing != null && thing.def == Def;
		}

		public static Thing Carried(Pawn pawn)
		{
			if (pawn?.inventory?.innerContainer == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.inventory.innerContainer)
			{
				if (IsMineralWater(thing) && thing.stackCount > 0)
				{
					return thing;
				}
			}
			return null;
		}

		public static Thing FindSpawned(Pawn pawn)
		{
			if (pawn?.Map == null || Def == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(Def))
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
			if (caravan == null || Def == null)
			{
				return null;
			}
			List<Thing> items = CaravanInventoryUtility.AllInventoryItems(caravan);
			for (int i = 0; i < items.Count; i++)
			{
				if (IsMineralWater(items[i]) && items[i].stackCount > 0)
				{
					return items[i];
				}
			}
			return null;
		}
	}

	/// <summary>喝矿泉水时给口渴加 0.2（1 L）；矿泉水本身没有营养。</summary>
	[HarmonyLib.HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
	internal static class Patch_Thing_Ingested_MineralWater
	{
		private static void Postfix(Thing __instance, Pawn ingester)
		{
			if (ingester != null && MineralWaterUtility.IsMineralWater(__instance))
			{
				ingester.needs?.TryGetNeed<Need_Thirst>()?.Drink(MineralWaterUtility.ThirstPerBottle);
			}
		}
	}
}
