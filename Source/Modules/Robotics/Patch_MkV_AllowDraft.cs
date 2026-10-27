using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 是玩家机械族，但原版只在 Biotech 的殖民地机械体上创建征召控制器；这里补上。</summary>
	[HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents))]
	internal static class Patch_MkV_AllowDraft
	{
		private static void Postfix(Pawn pawn, bool actAsIfSpawned)
		{
			if (pawn == null || !NannyBotUtility.IsMkV(pawn)
				|| (!pawn.Spawned && !actAsIfSpawned)
				|| pawn.Faction == null || !pawn.Faction.IsPlayer)
			{
				return;
			}
			if (pawn.drafter == null)
			{
				pawn.drafter = new Pawn_DraftController(pawn);
			}
		}
	}
}
