using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>绘制入口最后一道校正：确保 Misc 渲染树拿到的 facing 与移动路径一致。</summary>
	[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.Draw))]
	internal static class Patch_MkV_DrawFacing
	{
		private static void Prefix(PawnRenderTree __instance, ref PawnDrawParms parms)
		{
			Pawn pawn = __instance?.pawn;
			if (!NannyBotUtility.IsMkV(pawn) || pawn.pather == null || !pawn.pather.Moving)
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
	}
}
