using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 电量需求只属于保姆机器人。
	/// NeedDef 的 minIntelligence 是“最低智能”，Humanlike 会让所有人类（殖民者、囚犯、
	/// 访客）都获得 CK_RobotEnergy；所以这里用 Prefix 完全接管判断，只有种族为
	/// CK_RobotNanny 的机器人返回 true，其他 Pawn 一律返回 false。
	/// </summary>
	[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
	internal static class Patch_NeedsTracker_RobotEnergy
	{
		private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> PawnField =
			AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");

		private static bool Prefix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
		{
			if (nd == null || nd.defName != "CK_RobotEnergy")
			{
				return true;
			}

			Pawn pawn = PawnField(__instance);
			__result = ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId)
				&& NannyBotUtility.IsNannyBot(pawn);
			return false;
		}
	}
}
