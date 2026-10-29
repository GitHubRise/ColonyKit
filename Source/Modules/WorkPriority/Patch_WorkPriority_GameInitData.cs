using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.WorkPriority
{
	/// <summary>
	/// 新开局的起始殖民者：原版 <see cref="GameInitData.PrepForMapGen"/> 会先
	/// <c>DisableAll()</c>，再只给 alwaysStartActive 和「有人技能 ≥ 6」的工作设 3，
	/// 绕过了 <see cref="Pawn_WorkSettings.EnableAndInitialize"/>，所以默认值要在这里补一次。
	/// </summary>
	[HarmonyPatch(typeof(GameInitData), nameof(GameInitData.PrepForMapGen))]
	internal static class Patch_WorkPriority_PrepForMapGen
	{
		[HarmonyPostfix]
		private static void Postfix(GameInitData __instance)
		{
			if (!WorkPriorityModule.IsActive)
			{
				return;
			}

			List<Pawn> pawns = __instance.startingAndOptionalPawns;
			if (pawns == null)
			{
				return;
			}

			foreach (Pawn pawn in pawns)
			{
				if (pawn?.workSettings == null)
				{
					continue;
				}

				WorkPriorityDefaults.Apply(pawn, pawn.workSettings);
			}
		}
	}
}
