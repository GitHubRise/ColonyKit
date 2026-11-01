using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>在检视面板显示随身水袋水量，方便玩家知道谁该去补给了。</summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
	internal static class Patch_Pawn_InspectString_WaterPouch
	{
		private static void Postfix(Pawn __instance, ref string __result)
		{
			if (!HygieneSettings.Current.ShouldRunHygiene
				|| __instance?.needs == null
				|| !__instance.RaceProps.Humanlike
				|| __instance.needs.TryGetNeed<Need_Thirst>() == null)
			{
				return;
			}
			float water = WaterPouchUtility.GetWater(__instance);
			if (water <= 0.001f)
			{
				return;
			}
			string line = "CK.Hygiene.Pouch.Inspect".Translate(
				water.ToString("0.0"),
				WaterPouchUtility.MaxWater.ToString("0"));
			__result = __result.NullOrEmpty() ? line : __result + "\n" + line;
		}
	}
}
