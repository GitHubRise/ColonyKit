using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>绘制入口最后一道校正：确保 Misc 渲染树拿到的 facing 与移动路径一致。</summary>
	[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.Draw))]
	internal static class Patch_MkV_DrawFacing
	{
		private static readonly FieldInfo DrawRequestsField =
			AccessTools.Field(typeof(PawnRenderTree), "drawRequests");

		private static void Prefix(PawnRenderTree __instance, ref PawnDrawParms parms)
		{
			Pawn pawn = __instance?.pawn;
			if (!NannyBotUtility.IsMkV(pawn))
			{
				return;
			}
			ForceBodyMaterial(__instance, pawn);
			if (pawn.pather == null || !pawn.pather.Moving)
			{
				return;
			}
			IntVec3 delta = pawn.pather.nextCell - pawn.Position;
			if (delta.x > 0)
			{
				parms.facing = Rot4.East;
			}
			else if (delta.x < 0)
			{
				parms.facing = Rot4.West;
			}
			else if (delta.z > 0)
			{
				parms.facing = Rot4.North;
			}
			else if (delta.z < 0)
			{
				parms.facing = Rot4.South;
			}
		}

		private static void ForceBodyMaterial(PawnRenderTree tree, Pawn pawn)
		{
			if (!(DrawRequestsField?.GetValue(tree) is List<PawnGraphicDrawRequest> requests))
			{
				return;
			}
			for (int i = 0; i < requests.Count; i++)
			{
				PawnGraphicDrawRequest request = requests[i];
				if (request.node?.Props?.tagDef != PawnRenderNodeTagDefOf.Body
					|| !(request.node.PrimaryGraphic is Graphic_Multi graphic))
				{
					continue;
				}
				Material material;
				switch (pawn.Rotation.AsInt)
				{
					case 0:
						material = graphic.MatNorth;
						break;
					case 1:
						material = graphic.MatEast;
						break;
					case 2:
						material = graphic.MatSouth;
						break;
					case 3:
						material = graphic.MatWest;
						break;
					default:
						material = null;
						break;
				}
				if (material != null)
				{
					request.material = material;
					requests[i] = request;
				}
			}
		}
	}
}
