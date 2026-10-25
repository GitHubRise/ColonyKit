#nullable disable

﻿using HarmonyLib;
using RimWorld;
using rjw.RMB;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace rjw
{
	//添加用于开始性行为的新按钮
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	class Patch_AddGizmo
	{
		[HarmonyPriority(99), HarmonyPostfix]
		static IEnumerable<Gizmo> StartSexGizmo(IEnumerable<Gizmo> __result, Pawn __instance)
		{

			foreach (Gizmo entry in __result)
			{
				yield return entry;
			}

			if (RJWSettings.RMB_ability)
				if (RMB_Menu.CanControlPawn(__instance))
				{
					yield return new Command_Target
					{
						//defaultLabel = "Have sex",
						defaultDesc = "Select a pawn for action.",
						icon = ContentFinder<Texture2D>.Get("UI/PsyfocusIcons/PsyfocusSex"),
						targetingParams = new TargetingParameters
						{
							canTargetPawns = true,
							canTargetBuildings = false,
							canTargetSelf = false
						},
						action = target =>
						{
							Find.WindowStack.Add(new FloatMenu(RMB_Menu.GetOptionsForTarget(__instance, target.Thing).ToList()));
						}
					};
				}
		}
	}
}
