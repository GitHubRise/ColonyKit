using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版武器/攻击指令（例如 ArcEmitter 的 verb command）会检查
	/// Pawn.IsPlayerControlled；这个属性又分别走 IsColonistPlayerControlled、
	/// IsColonyMechPlayerControlled 和 IsColonySubhumanPlayerControlled。
	///
	/// Mk-V 既不是殖民者、也不是 Biotech 的机械主控单位，因此即使我们已经
	/// 让它 CanTakeOrder 并创建了 drafter，武器指令仍可能显示：
	///   「禁用：无法命令不由你操控的角色」
	///
	/// 这里只对玩家阵营、已生成、未倒地的 Mk-V 放行 IsPlayerControlled，
	/// 让征召后的手动攻击、武器 verb 指令真正可用；自动攻击逻辑不变。
	/// </summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.IsPlayerControlled), MethodType.Getter)]
	internal static class Patch_MkV_PlayerControlled
	{
		private static void Postfix(Pawn __instance, ref bool __result)
		{
			if (__result
				|| __instance == null
				|| !NannyBotUtility.IsMkV(__instance)
				|| !__instance.Spawned
				|| __instance.Downed
				|| __instance.MentalStateDef != null
				|| __instance.Faction != Faction.OfPlayer)
			{
				return;
			}
			__result = true;
		}
	}
}
