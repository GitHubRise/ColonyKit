using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Bubbles.Patches
{
	/// <summary>
	/// 地图 UI 的主绘制入口：在标签栏之前画气泡，保证气泡压在地面/小人之上、又不会盖住底栏。
	/// </summary>
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs))]
	internal static class Patch_MapInterface_BeforeMainTabs
	{
		private static void Postfix()
		{
			Bubbler.Draw();
		}
	}

	/// <summary>新游戏 / 读档 / 换图时清空气泡，避免挂着上一张地图的 Pawn。</summary>
	[HarmonyPatch(typeof(Verse.Profile.MemoryUtility), nameof(Verse.Profile.MemoryUtility.ClearAllMapsAndWorld))]
	internal static class Patch_MemoryUtility_ClearAllMapsAndWorld
	{
		private static void Prefix()
		{
			Bubbler.Clear();
		}
	}

	/// <summary>每条新日志都过一遍；只有社交互动才会变成气泡。</summary>
	[HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
	internal static class Patch_PlayLog_Add
	{
		private static void Postfix(LogEntry entry)
		{
			Bubbler.Add(entry);
		}
	}
}
