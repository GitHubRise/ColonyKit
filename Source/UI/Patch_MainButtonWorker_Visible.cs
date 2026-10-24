using HarmonyLib;
using RimWorld;

namespace ColonyKit
{
	/// <summary>把 <see cref="MainButtonService.IsVisible"/> 叠加到原版按钮可见性上。</summary>
	[HarmonyPatch(typeof(MainButtonWorker), nameof(MainButtonWorker.Visible), MethodType.Getter)]
	internal static class Patch_MainButtonWorker_Visible
	{
		[HarmonyPostfix]
		private static void Postfix(MainButtonWorker __instance, ref bool __result)
		{
			if (__result)
			{
				__result = MainButtonService.IsVisible(__instance.def);
			}
		}
	}
}
