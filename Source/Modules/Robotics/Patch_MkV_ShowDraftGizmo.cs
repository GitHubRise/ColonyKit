using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版对 IsColonyMech 且没有机械主控组的机械体隐藏征召按钮。
	/// Mk-V 不走 Biotech 机械主控，因此必须单独放行。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo),
		MethodType.Getter)]
	internal static class Patch_MkV_ShowDraftGizmo
	{
		private static void Postfix(Pawn_DraftController __instance, ref bool __result)
		{
			if (!__result && NannyBotUtility.IsMkV(__instance?.pawn))
			{
				__result = true;
			}
		}
	}
}
