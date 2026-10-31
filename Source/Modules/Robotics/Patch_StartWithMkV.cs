using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 新开局在玩家起始地图赠送若干台 Mk-V（数量由设置页「开局赠送 Mk-V 数量」决定，0–5 台）。
	/// 只影响新游戏，不影响读档；地图上已经有 Mk-V（剧本自带或别的模组放的）时不再追加。
	/// </summary>
	[HarmonyPatch(typeof(Scenario), nameof(Scenario.GenerateIntoMap))]
	internal static class Patch_StartWithMkV
	{
		internal static bool grantedThisGame;

		/// <summary>出生点被占用时，在这个半径内给后面的机器人找空位，避免几台叠在同一格。</summary>
		private const float SpawnSearchRadius = 8f;

		private static void Postfix(Map map)
		{
			if (grantedThisGame || map == null || !map.IsPlayerHome)
			{
				return;
			}

			int count = UnityEngine.Mathf.Clamp(
				NannyBotUtility.Settings.startWithMkVCount,
				RoboticsSettings.MinStartWithMkVCount,
				RoboticsSettings.MaxStartWithMkVCount);
			if (count <= 0)
			{
				grantedThisGame = true;
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

			IntVec3 origin = MapGenerator.PlayerStartSpotValid
				? MapGenerator.PlayerStartSpot
				: map.Center;
			if (!IsFreeSpawnCell(origin, map, null))
			{
				origin = map.Center;
			}

			List<IntVec3> used = new List<IntVec3>();
			int spawned = 0;
			for (int index = 1; index <= count; index++)
			{
				if (TrySpawnOne(map, faction, index, origin, used))
				{
					spawned++;
				}
			}

			grantedThisGame = true;
			ModLog.Info("Robotics", $"starting Mk-V granted: {spawned} of {count}.");
		}

		private static bool TrySpawnOne(Map map, Faction faction, int index, IntVec3 origin,
			List<IntVec3> used)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(NannyBotDefOf.CK_RobotMkVKind, faction);
			if (pawn == null)
			{
				ModLog.Warn("Robotics", $"starting Mk-V #{index} generation returned null.");
				return false;
			}

			IntVec3 cell = FindSpawnCell(map, origin, used);
			pawn.Name = new NameSingle("CK.Robotics.MkVName".Translate(index).ToString(), true);
			if (pawn.playerSettings == null)
			{
				pawn.playerSettings = new Pawn_PlayerSettings(pawn);
			}

			GenSpawn.Spawn(pawn, cell, map, Rot4.South);
			if (!pawn.Spawned)
			{
				ModLog.Warn("Robotics", $"starting Mk-V #{index} could not be spawned at {cell}.");
				return false;
			}

			// 主手电弧发射器 + 副手电弧刃；双持关闭时只发主手武器。
			MkVLoadout.GiveStartingWeapons(pawn);
			used.Add(cell);
			return true;
		}

		private static IntVec3 FindSpawnCell(Map map, IntVec3 origin, List<IntVec3> used)
		{
			if (IsFreeSpawnCell(origin, map, used))
			{
				return origin;
			}

			foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, SpawnSearchRadius, true))
			{
				if (IsFreeSpawnCell(cell, map, used))
				{
					return cell;
				}
			}

			IntVec3 fallback = CellFinder.RandomClosewalkCellNear(origin, map, 12,
				c => c.Standable(map) && !c.Fogged(map));
			return fallback.IsValid && fallback.InBounds(map) ? fallback : map.Center;
		}

		private static bool IsFreeSpawnCell(IntVec3 cell, Map map, List<IntVec3> used)
		{
			return cell.IsValid
				&& cell.InBounds(map)
				&& cell.Standable(map)
				&& !cell.Fogged(map)
				&& cell.GetFirstPawn(map) == null
				&& (used == null || !used.Contains(cell));
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
