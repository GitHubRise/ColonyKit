using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>新开局在玩家起始地图赠送一台 Mk-V；只影响新游戏，不影响读档。</summary>
	[HarmonyPatch(typeof(Scenario), nameof(Scenario.GenerateIntoMap))]
	internal static class Patch_StartWithMkV
	{
		internal static bool grantedThisGame;

		private static void Postfix(Map map)
		{
			if (grantedThisGame || !NannyBotUtility.Settings.startWithMkV
				|| map == null || !map.IsPlayerHome)
			{
				return;
			}
			if (map.mapPawns.AllPawnsSpawned.Any(NannyBotUtility.IsMkV))
			{
				grantedThisGame = true;
				return;
			}
			Faction faction = Faction.OfPlayer;
			if (faction == null || NannyBotDefOf.CK_RobotMkVKind == null)
			{
				return;
			}

			Pawn pawn = PawnGenerator.GeneratePawn(NannyBotDefOf.CK_RobotMkVKind, faction);
			if (pawn == null)
			{
				ModLog.Warn("Robotics", "starting Mk-V generation returned null.");
				return;
			}

			IntVec3 cell = MapGenerator.PlayerStartSpotValid
				? MapGenerator.PlayerStartSpot
				: map.Center;
			if (!cell.IsValid || !cell.InBounds(map) || !cell.Standable(map))
			{
				cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12,
					c => c.Standable(map) && !c.Fogged(map));
			}
			if (!cell.IsValid || !cell.InBounds(map))
			{
				cell = map.Center;
			}

			pawn.Name = new NameSingle("CK.Robotics.MkVName".Translate(1).ToString(), true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}
			GenSpawn.Spawn(pawn, cell, map, Rot4.South);
			if (!pawn.Spawned)
			{
				ModLog.Warn("Robotics", "starting Mk-V could not be spawned.");
				return;
			}

			if (NannyBotDefOf.CK_ArcEmitter != null && pawn.equipment != null
				&& pawn.equipment.Primary == null)
			{
				ThingWithComps weapon = ThingMaker.MakeThing(NannyBotDefOf.CK_ArcEmitter) as ThingWithComps;
				if (weapon != null)
				{
					pawn.equipment.AddEquipment(weapon);
				}
			}

			grantedThisGame = true;
			ModLog.Info("Robotics", $"starting Mk-V granted at {cell}.");
		}
	}

	[HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
	internal static class Patch_ResetStartWithMkV
	{
		private static void Prefix()
		{
			Patch_StartWithMkV.grantedThisGame = false;
		}
	}
}
