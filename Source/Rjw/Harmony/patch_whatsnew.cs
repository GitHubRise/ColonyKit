using HarmonyLib;
using rjw.RJWUI.WhatsNew;
using Verse;

namespace rjw;

[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
internal static class Patch_WhatsNew
{
	private static void Postfix()
	{
		try
		{
			WhatsNewManager.ShowIfNeeded();
		}
		catch (System.Exception ex)
		{
			ModLog.Warning("Failed to show What's New window: " + ex.Message);
		}
	}
}
