#nullable disable

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System;
using Verse;
using rjw.Modules.Interactions;

namespace rjw;

/// <summary>
/// 修补 HealthCardUtility，向开发者菜单添加 RJW 选项
/// </summary>
[HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.DoDebugOptions))]
static class Patch_HealthCardDevTool
{
	public static bool buildingDebugMenu = false;
	public static Pawn pawn = null;

	public static void Prefix(Pawn pawn)
	{
		Patch_HealthCardDevTool.pawn = pawn;
		Patch_HealthCardDevTool.buildingDebugMenu = true;
	}

	public static void Postfix()
	{
		Patch_HealthCardDevTool.pawn = null;
		Patch_HealthCardDevTool.buildingDebugMenu = false;
	}
}

[HarmonyPatch(typeof(FloatMenu), MethodType.Constructor, new Type[] { typeof(List<FloatMenuOption>) })]
static class Patch_HealthCardDevTool_FloatMenu
{
	public static void Prefix(ref List<FloatMenuOption> options)
	{
		if (Patch_HealthCardDevTool.buildingDebugMenu && Patch_HealthCardDevTool.pawn != null)
		{
			Patch_HealthCardDevTool.buildingDebugMenu = false;


			var pawn = Patch_HealthCardDevTool.pawn;
			var parts = pawn.GetLewdParts();

			var editableParts = new List<ISexPartHediff>();
			editableParts.AddRange(parts.Breasts.AsSexPartHediffs());
			editableParts.AddRange(parts.Vaginas.AsSexPartHediffs());
			editableParts.AddRange(parts.Penises.AsSexPartHediffs());
			editableParts.AddRange(parts.Anuses.AsSexPartHediffs());

			if (editableParts.Count > 0)
			{
				options.Add(new FloatMenuOption("SexcardEditDevEntry".Translate(), delegate
				{
					Find.WindowStack.Add(new Dialog_SexcardNG(pawn));
				}));
				options.Add(new FloatMenuOption("PartsEditDevEntry".Translate(), delegate
				{
					Find.WindowStack.Add(new Dialog_PartcardNG(pawn, editableParts, xxx.get_pawnname(pawn).CapitalizeFirst()));
				}));
			}
		}
	}
}
