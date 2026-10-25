#nullable disable

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw;

[HarmonyPatch(typeof(TrainingCardUtility), nameof(TrainingCardUtility.DrawTrainingCard))]
public static class Patch_TrainingCard_AddMilestonesButton
{
	[HarmonyPostfix]
	public static void Postfix(Rect rect, Pawn pawn)
	{
		if (pawn == null || !pawn.IsAnimal()) return;
		float left = TrainingCardUtility.TrainabilityLeft;
		float top = TrainingCardUtility.TrainabilityTop;

		Rect btn = new(left - 24, top + 2, 24, 24);

		Texture2D tex = ContentFinder<Texture2D>.Get("RJW-LOGO", true);

		TooltipHandler.TipRegion(btn, "SexcardTooltip".Translate());

		if (Widgets.ButtonImage(btn, tex, Color.white, GenUI.MouseoverColor))
		{
			Find.WindowStack.Add(new Dialog_SexcardNG(pawn));
		}
	}

}
