using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版 <see cref="VerbTracker.CreateVerbTargetCommand"/> 对机械族还有一道额外检查：
	///   CasterPawn.RaceProps.IsMechanoid &amp;&amp; !MechanitorUtility.EverControllable(...)
	/// 满足时直接把武器指令禁用为「无法命令不由你操控的角色」。
	///
	/// Mk-V 是机械族，但没有 OverseerSubject，<see cref="MechanitorUtility.EverControllable"/>
	/// 永远返回 false；仅放行 Pawn.IsPlayerControlled 不足以绕过这里。
	/// 这里只对玩家阵营、已生成、未倒地的 Mk-V 返回 true，让电弧发射器的
	/// 手动瞄准指令真正可用。
	/// </summary>
	[HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.EverControllable))]
	internal static class Patch_MkV_EverControllable
	{
		private static bool Prefix(Pawn mech, ref bool __result)
		{
			if (mech == null
				|| !NannyBotUtility.IsMkV(mech)
				|| !mech.Spawned
				|| mech.Downed
				|| mech.Dead
				|| mech.Faction != Faction.OfPlayer)
			{
				return true;
			}

			__result = true;
			return false;
		}
	}
}
