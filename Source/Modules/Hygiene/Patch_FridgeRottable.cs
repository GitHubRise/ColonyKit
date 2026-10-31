using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 冰箱保鲜：食物在通电的冰箱格里跳过 CompRottable 的腐烂结算。
	/// 只跳过两个 tick 入口（Normal / Rare），不改变其他 CompRottable 行为（合并、拆分、读取）。
	/// </summary>
	[HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickInterval))]
	internal static class Patch_CompRottable_FridgeInterval
	{
		private static bool Prefix(CompRottable __instance)
		{
			return !FridgeUtility.IsInPoweredFridge(__instance.parent);
		}
	}

	[HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickRare))]
	internal static class Patch_CompRottable_FridgeRare
	{
		private static bool Prefix(CompRottable __instance)
		{
			return !FridgeUtility.IsInPoweredFridge(__instance.parent);
		}
	}

	internal static class FridgeUtility
	{
		public static bool IsInPoweredFridge(Thing thing)
		{
			Map map = thing?.Map;
			if (map == null || !thing.Spawned)
			{
				return false;
			}
			// 1×1 冰箱优先走 edifice；兼容以后做成多格时再遍历格内建筑。
			Building edifice = thing.Position.GetEdifice(map);
			CompFridge fridge = edifice?.TryGetComp<CompFridge>();
			if (fridge != null && fridge.IsRefrigerated)
			{
				return true;
			}
			foreach (Thing candidate in thing.Position.GetThingList(map))
			{
				fridge = candidate.TryGetComp<CompFridge>();
				if (fridge != null && fridge.IsRefrigerated)
				{
					return true;
				}
			}
			return false;
		}
	}
}
