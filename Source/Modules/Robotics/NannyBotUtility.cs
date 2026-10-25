using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>保姆机器人共用查询：身份、能量、充电桩。</summary>
	internal static class NannyBotUtility
	{
		public const string ModuleId = "robotics";

		public static bool IsNannyBot(Pawn pawn)
		{
			return pawn != null
				&& pawn.def != null
				&& pawn.def == NannyBotDefOf.CK_RobotNanny;
		}

		public static CompNannyBot BotComp(Pawn pawn)
		{
			return IsNannyBot(pawn) ? pawn.TryGetComp<CompNannyBot>() : null;
		}

		public static Need_RobotEnergy Energy(Pawn pawn)
		{
			return pawn?.needs?.TryGetNeed<Need_RobotEnergy>();
		}

		public static bool CanWork(Pawn pawn)
		{
			if (!ModuleRegistry.IsEnabled(ModuleId) || !IsNannyBot(pawn))
			{
				return false;
			}
			if (pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.Map == null)
			{
				return false;
			}
			Need_RobotEnergy energy = Energy(pawn);
			return energy == null || energy.CurLevel > 0.001f;
		}

		/// <summary>找一台可用的充电桩：优先机器人自己的母桩，否则找最近的。</summary>
		public static Building FindStation(Pawn pawn, bool requirePower)
		{
			if (pawn?.Map == null || NannyBotDefOf.CK_NannyBotStation == null)
			{
				return null;
			}

			CompNannyBot comp = BotComp(pawn);
			if (comp != null && IsStationUsable(pawn, comp.homeStation, requirePower))
			{
				return comp.homeStation;
			}

			Building best = null;
			float bestDistance = float.MaxValue;
			List<Building> stations = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(
				NannyBotDefOf.CK_NannyBotStation);
			for (int i = 0; i < stations.Count; i++)
			{
				Building station = stations[i];
				if (!IsStationUsable(pawn, station, requirePower))
				{
					continue;
				}
				float distance = station.Position.DistanceToSquared(pawn.Position);
				if (distance < bestDistance)
				{
					best = station;
					bestDistance = distance;
				}
			}
			return best;
		}

		public static bool IsStationUsable(Pawn pawn, Building station, bool requirePower)
		{
			if (pawn?.Map == null || station == null || !station.Spawned || station.Map != pawn.Map)
			{
				return false;
			}
			if (station.IsForbidden(pawn) || !pawn.CanReserve(station))
			{
				return false;
			}
			if (requirePower)
			{
				CompPowerTrader power = station.TryGetComp<CompPowerTrader>();
				if (power == null || !power.PowerOn)
				{
					return false;
				}
			}
			return pawn.CanReach(station, PathEndMode.InteractionCell, Danger.Deadly);
		}
	}
}
