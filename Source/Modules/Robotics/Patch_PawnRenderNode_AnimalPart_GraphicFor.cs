using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>给保姆机器人应用玩家选择的涂装颜色；只影响渲染，不改 Def 或贴图文件。</summary>
	[HarmonyPatch(typeof(PawnRenderNode_AnimalPart), "GraphicFor")]
	internal static class Patch_PawnRenderNode_AnimalPart_GraphicFor
	{
		private static void Postfix(Pawn pawn, ref Graphic __result)
		{
			if (__result == null || !NannyBotUtility.IsNannyBot(pawn))
			{
				return;
			}
			CompNannyBot comp = pawn.TryGetComp<CompNannyBot>();
			if (comp != null && comp.HasPaint)
			{
				__result = comp.GetPaintedGraphic(__result);
			}
		}
	}
}
