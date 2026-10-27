using System.Reflection;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// RJW 的 RMB 性行为菜单还会检查双方好感与吸引力。
	/// 机器人没有社交历史，默认好感/吸引力为 0，于是菜单会被静默过滤。
	/// Mk-V 参与时跳过这两项社交门槛；can_fuck/can_be_fucked 仍由 RJW 自己检查。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_RjwRmbMkV
	{
		private static bool Prepare()
		{
			return RjwCompat.Loaded;
		}

		private static MethodBase TargetMethod()
		{
			return AccessTools.Method("rjw.RMB.RMB_Sex:DoChecks");
		}

		private static bool Prefix(Pawn pawn, Pawn target, ref AcceptanceReport __result)
		{
			if (NannyBotUtility.IsMkV(pawn) || NannyBotUtility.IsMkV(target))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
