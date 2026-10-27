using System.Reflection;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// RJW 的 xxx.can_do_loving 对所有 Mechanoid 直接返回 false。
	/// Mk-V 已有人工性器官，允许它通过这道门；其他机械族不受影响。
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_RjwMkVCompatibility
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method("rjw.xxx:can_do_loving");
		}

		private static bool Prefix(Pawn pawn, ref bool __result)
		{
			if (NannyBotUtility.IsMkV(pawn))
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
