using System.Reflection;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 非人形 Mk-V 在部分状态下 RotationTracker 不更新，身体会一直保持东向。
	/// 在原有旋转更新后，按实际路径下一步强制校正四向朝向。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
	internal static class Patch_MkV_Rotation
	{
		private static readonly FieldInfo PawnField =
			AccessTools.Field(typeof(Pawn_RotationTracker), "pawn");

		private static void Postfix(Pawn_RotationTracker __instance)
		{
			Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || pawn.pather == null || !pawn.pather.Moving)
			{
				return;
			}
			IntVec3 delta = pawn.pather.nextCell - pawn.Position;
			if (delta.x > 0)
			{
				pawn.Rotation = Rot4.East;
			}
			else if (delta.x < 0)
			{
				pawn.Rotation = Rot4.West;
			}
			else if (delta.z > 0)
			{
				pawn.Rotation = Rot4.North;
			}
			else if (delta.z < 0)
			{
				pawn.Rotation = Rot4.South;
			}
		}
	}
}
