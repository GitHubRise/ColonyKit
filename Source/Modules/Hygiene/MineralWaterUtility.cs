using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 1 L 一瓶的瓶装水：矿泉水（未处理，可能闹肚子）、纯净水（过滤）和凉白开（烧开）。
	/// 三种都按「每升解渴」折算（默认 0.65），不需要容器；除矿泉水外都不会中毒。
	/// </summary>
	internal static class MineralWaterUtility
	{
		public const float LitersPerBottle = 1f;

		/// <summary>每升水的解渴量，设置页「水与污染 → 每升解渴」可调（默认 0.65）。</summary>
		public static float ThirstPerLiter => Settings.thirstPerLiter;

		private static ThingDef cachedMineral;

		private static ThingDef cachedPurified;

		private static ThingDef cachedBoiled;

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

		public static ThingDef BoiledDef
		{
			get
			{
				if (cachedBoiled == null)
				{
					cachedBoiled = DefDatabase<ThingDef>.GetNamedSilentFail("CK_BoiledWater");
				}
				return cachedBoiled;
			}
		}

		private static HygieneSettings Settings => HygieneSettings.Current;

		/// <summary>喝掉一件水（1 L）解多少渴；三种瓶装水现在共用「每升解渴」。</summary>
		public static float ThirstFor(Thing thing)
		{
			// 三种瓶装水都是 1 L，解渴只取决于「每升解渴」；参数保留给调用方的三种水区分逻辑。
			return LitersPerBottle * ThirstPerLiter;
		}

		public static float SatietyFor(Thing thing)
		{
			if (IsBoiledWater(thing))
			{
				return Settings.boiledWaterSatiety;
			}
			if (IsPurifiedWater(thing))
			{
				return Settings.purifiedWaterSatiety;
			}
			return Settings.mineralWaterSatiety;
		}

		public static int MoodFor(Thing thing)
		{
			if (IsBoiledWater(thing))
			{
				return Settings.boiledWaterMood;
			}
			if (IsPurifiedWater(thing))
			{
				return Settings.purifiedWaterMood;
			}
			return 0;
		}

		public static bool IsMineralWater(Thing thing)
		{
			return thing != null && thing.def == Def;
		}

		public static bool IsPurifiedWater(Thing thing)
		{
			return thing != null && thing.def == PurifiedDef;
		}

		public static bool IsBoiledWater(Thing thing)
		{
			return thing != null && thing.def == BoiledDef;
		}

		public static bool IsBottledWater(Thing thing)
		{
			return IsMineralWater(thing) || IsPurifiedWater(thing) || IsBoiledWater(thing);
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
			return FindSpawnedDef(pawn, Def) ?? FindSpawnedDef(pawn, PurifiedDef) ?? FindSpawnedDef(pawn, BoiledDef);
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

	/// <summary>喝瓶装水时按 1 L × 每升解渴加口渴（默认 0.65）；矿泉水可能带未处理水污染，纯净水不会。</summary>
	[HarmonyLib.HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
	internal static class Patch_Thing_Ingested_MineralWater
	{
		private static void Postfix(Thing __instance, Pawn ingester)
		{
			if (ingester == null || !MineralWaterUtility.IsBottledWater(__instance))
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
				thirst.Drink(MineralWaterUtility.ThirstFor(__instance));
				GainWaterMood(
					ingester,
					"CK_DrankPurifiedWater",
					"CK_DrankBoiledWater",
					MineralWaterUtility.MoodFor(__instance));
			}
			else if (MineralWaterUtility.IsBoiledWater(__instance))
			{
				thirst.Drink(MineralWaterUtility.ThirstFor(__instance));
				GainWaterMood(
					ingester,
					"CK_DrankBoiledWater",
					"CK_DrankPurifiedWater",
					MineralWaterUtility.MoodFor(__instance));
			}
			else if (MineralWaterUtility.IsMineralWater(__instance))
			{
				thirst.Drink(MineralWaterUtility.ThirstFor(__instance));
				if (HygieneSettings.Current.mineralWaterCanPoison)
				{
					SanitationUtil.ContaminationCheckDrinking(ingester, ContaminationLevel.Untreated);
				}
			}
			HygieneUtility.AddDrinkSatiety(ingester, MineralWaterUtility.SatietyFor(__instance));
		}

		private static void GainWaterMood(
			Pawn pawn, string gainDefName, string removeDefName, int mood)
		{
			MemoryThoughtHandler memories = pawn?.needs?.mood?.thoughts?.memories;
			if (memories == null)
			{
				return;
			}
			ThoughtDef remove = DefDatabase<ThoughtDef>.GetNamedSilentFail(removeDefName);
			if (remove != null)
			{
				memories.RemoveMemoriesOfDef(remove);
			}
			if (mood <= 0)
			{
				return;
			}
			ThoughtDef gain = DefDatabase<ThoughtDef>.GetNamedSilentFail(gainDefName);
			if (gain?.stages == null || gain.stages.Count == 0)
			{
				return;
			}
			gain.stages[0].baseMoodEffect = mood;
			memories.TryGainMemory(gain);
		}
	}
}
