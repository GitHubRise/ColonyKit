using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 的 Misc 渲染树在部分情况下没有把 pawn.Rotation 同步到绘制 facing。
	/// 绘制前直接按实际路径下一格校正 facing，保证身体四向贴图被使用。
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
	internal static class Patch_MkV_RenderFacing
	{
		private static void Postfix(PawnRenderTree __instance, ref PawnDrawParms parms)
		{
			Pawn pawn = __instance?.pawn;
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || pawn.pather == null || !pawn.pather.Moving)
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
