using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Biotech 把「玩家阵营的机械族」一律当殖民地机械体（<c>Pawn.IsColonyMech</c>）：
	/// <c>Pawn.GetDisabledWorkTypes</c> 会把 <c>RaceProps.mechEnabledWorkTypes</c> 以外的工作类型全部禁用，
	/// 结果自维护机器人（Mk-V / 保姆机器人）装上 Biotech 后，技能页除了射击/格斗全变成「-」，
	/// 工作类型也被判成不可用。我们的机器人有自己的能量、装配舱和工作管控，不走 Biotech 机械体那套；
	/// 这里让它们不算 colony mech，技能与工作类型恢复成没有 DLC 时的样子。
	/// </summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonyMech), MethodType.Getter)]
	internal static class Patch_Pawn_IsColonyMech
	{
		[HarmonyPrefix]
		private static bool Prefix(Pawn __instance, ref bool __result)
		{
			if (NannyBotUtility.IsNannyBot(__instance))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
