using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 最后的材质选择兜底：Misc 渲染树没有把 facing 正确传给 Graphic_Multi 时，
	/// 直接按 pawn.Rotation 从 Body 节点的 Graphic_Multi 上取对应方向的材质。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_MkV_BodyMaterial
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(PawnRenderNodeWorker), "GetMaterial",
				new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
		}

		private static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Material __result)
		{
			Pawn pawn = parms.pawn;
			if (!NannyBotUtility.IsMkV(pawn)
				|| node?.Props?.tagDef != PawnRenderNodeTagDefOf.Body
				|| !(node.PrimaryGraphic is Graphic_Multi graphic))
			{
				return;
			}
			switch (pawn.Rotation.AsInt)
			{
				case 0:
					__result = graphic.MatNorth;
					break;
				case 1:
					__result = graphic.MatEast;
					break;
				case 2:
					__result = graphic.MatSouth;
					break;
				case 3:
					__result = graphic.MatWest;
					break;
			}
		}
	}
}
