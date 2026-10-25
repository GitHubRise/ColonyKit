using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>淋浴/卫生模块：需求、淋浴建筑与自动洗澡 Job。</summary>
	public sealed class HygieneModule : ColonyKitModule
	{
		public override string Id => "hygiene";

		public override string DisplayNameKey => "ColonyKit.Module.Hygiene.Name";

		public override string DescriptionKey => "ColonyKit.Module.Hygiene.Desc";

		public override int InitOrder => 100;

		public override void OnDefsLoaded()
		{
			ModLog.Info(
				"Hygiene",
				"module loaded (DBH shower migration); " +
				$"DBH active={ModsConfig.IsActive("dubwise.dubsbadhygiene")}");
		}

		public override void OnGameLoaded(Game game)
		{
			if (game == null)
			{
				return;
			}
			int humanlikes = 0;
			int withNeed = 0;
			foreach (Map map in game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
					{
						continue;
					}
					humanlikes++;
					if (pawn.needs?.TryGetNeed<Need_Hygiene>() != null)
					{
						withNeed++;
					}
				}
			}
			ModLog.Info("Hygiene", $"need injection check: humanlikes={humanlikes}, withHygiene={withNeed}");
		}
	}

	[HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
	internal static class Patch_NeedsTracker_Hygiene
	{
		private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> PawnField =
			AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");

		private static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
		{
			if (__result || nd == null || nd.defName != "CK_Hygiene")
			{
				return;
			}
			if (!ModuleRegistry.IsEnabled("hygiene")
				|| ModsConfig.IsActive("dubwise.dubsbadhygiene"))
			{
				return;
			}

			Pawn pawn = PawnField(__instance);
			if (pawn != null
				&& pawn.RaceProps.Humanlike
				&& !pawn.RaceProps.IsMechanoid)
			{
				__result = true;
			}
		}
	}
}
