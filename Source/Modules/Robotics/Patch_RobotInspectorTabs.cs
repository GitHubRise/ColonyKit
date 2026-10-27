using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// BasePawn 自带一整套页签，单靠 XML 列表容易残留「角色/日志/需求/健康」重复项。
	/// 这里在 Def 解析后强制给机器人换成干净的列表，并加入自定义「角色」页。
	/// </summary>
	[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ResolveReferences))]
	internal static class Patch_RobotInspectorTabs
	{
		private static readonly Type[] Tabs =
		{
			typeof(ITab_Pawn_Character),
			typeof(ITab_Pawn_Gear),
			typeof(ITab_Pawn_Health),
			typeof(ITab_Pawn_Needs),
			typeof(ITab_Pawn_Log)
		};

		private static void Postfix(ThingDef __instance)
		{
			if (__instance == null
				|| (__instance.defName != "CK_RobotNanny" && __instance.defName != "CK_RobotMkV"))
			{
				return;
			}

			List<Type> tabs = new List<Type>(Tabs);
			__instance.inspectorTabs = tabs;
			__instance.inspectorTabsResolved = new List<InspectTabBase>();
			for (int i = 0; i < tabs.Count; i++)
			{
				try
				{
					__instance.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(tabs[i]));
				}
				catch (Exception exception)
				{
					Log.Error($"Could not instantiate robot inspector tab {tabs[i]}: {exception}");
				}
			}
		}
	}
}
