using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 给保姆机器人注入自定义电量需求。
	/// NeedDef 的 minIntelligence 设为 Humanlike 只是为了避免其他 ToolUser 机械族误获需求，
	/// 真正是否拥有由本补丁按种族判断。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
	internal static class Patch_NeedsTracker_RobotEnergy
	{
		private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> PawnField =
			AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");

		private static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
		{
			if (__result
				|| nd == null
				|| nd.defName != "CK_RobotEnergy"
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return;
			}

			if (NannyBotUtility.IsNannyBot(PawnField(__instance)))
			{
				__result = true;
			}
		}
	}
}
