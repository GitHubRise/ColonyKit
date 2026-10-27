using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 不是 Biotech 的 IsColonyMech，原版 CanTakeOrder 会把它排除在
	/// 征召移动/攻击和右键指令之外；这里只对玩家阵营的 Mk-V 放行。
	/// </summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.CanTakeOrder), MethodType.Getter)]
	internal static class Patch_MkV_CanTakeOrder
	{
		private static void Postfix(Pawn __instance, ref bool __result)
		{
			if (!__result
				&& NannyBotUtility.IsMkV(__instance)
				&& __instance.Spawned
				&& !__instance.Downed
				&& __instance.Faction == Faction.OfPlayer)
			{
				__result = true;
			}
		}
	}
}
