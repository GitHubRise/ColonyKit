using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 原版 FloatMenuUtility 把 Mk-V 当成“非玩家控制”目标，
	/// 征召后远程/近战攻击选项都会被过滤。这里让玩家阵营的 Mk-V 通过该检查。
	/// </summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonistPlayerControlled), MethodType.Getter)]
	internal static class Patch_MkV_ColonistControlled
	{
		private static void Postfix(Pawn __instance, ref bool __result)
		{
			if (!__result
				&& NannyBotUtility.IsMkV(__instance)
				&& __instance.Spawned
				&& !__instance.Downed
				&& __instance.MentalStateDef == null
				&& __instance.Faction == Faction.OfPlayer)
			{
				__result = true;
			}
		}
	}
}
