using HarmonyLib;
using RimWorld;

namespace ColonyKit
{
	/// <summary>主菜单构造完成后按 <see cref="MainButtonService.OrderOf"/> 重排一次。</summary>
	[HarmonyPatch(typeof(MainButtonsRoot), MethodType.Constructor)]
	internal static class Patch_MainButtonsRoot_Ctor
	{
		[HarmonyPostfix]
		private static void Postfix(MainButtonsRoot __instance)
		{
			MainButtonService.ApplyOrder(__instance);
		}
	}
}
