using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>保姆机器人共用查询：身份、能量、充电桩、设置与工作范围。</summary>
	internal static class NannyBotUtility
	{
		public const string ModuleId = "robotics";

		private static readonly RoboticsSettings FallbackSettings = new RoboticsSettings();

		public static RoboticsSettings Settings =>
			ColonyKitMod.Settings?.GetModuleSettings<RoboticsSettings>() ?? FallbackSettings;

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
			CompNannyBot comp = BotComp(pawn);
			if (comp != null && comp.paused)
			{
				return false;
			}
			if (Settings.dangerAvoidance && HasNearbyThreat(pawn))
			{
				return false;
			}
			Need_RobotEnergy energy = Energy(pawn);
			return energy == null || energy.CurLevel > 0.001f;
		}

		/// <summary>工作范围检查：半径 + 家区域/自定义区域。</summary>
		public static bool IsWithinWorkArea(Pawn pawn, IntVec3 cell)
		{
			if (pawn?.Map == null || !cell.InBounds(pawn.Map))
			{
				return false;
			}

			RoboticsSettings settings = Settings;
			float radius = settings.workRadius;
			if (radius > 0.1f
				&& pawn.Position.DistanceToSquared(cell) > radius * radius)
			{
				return false;
			}

			Map map = pawn.Map;
			switch (settings.areaMode)
			{
				case RobotAreaMode.Home:
					return map.areaManager.Home[cell];
				case RobotAreaMode.Custom:
					Area area = map.areaManager.GetLabeled(settings.allowedAreaLabel);
					return area == null || area[cell];
				default:
					return true;
			}
		}

		/// <summary>附近有活动敌对目标，或全图进入高危险状态。</summary>
		public static bool HasNearbyThreat(Pawn pawn, float radius = 30f)
		{
			if (pawn?.Map == null)
			{
				return false;
			}
			if (pawn.Map.dangerWatcher.DangerRating == StoryDanger.High)
			{
				return true;
			}

			foreach (IAttackTarget target in pawn.Map.attackTargetsCache.TargetsHostileToColony)
			{
				Thing thing = target?.Thing;
				if (thing == null || !thing.Spawned)
				{
					continue;
				}
				if (!thing.Position.InHorDistOf(pawn.Position, radius))
				{
					continue;
				}
				if (GenHostility.IsActiveThreatToPlayer(target))
				{
					return true;
				}
			}
			return false;
		}

		public static IEnumerable<Pawn> AllNannyBots()
		{
			if (Current.Game == null)
			{
				yield break;
			}
			foreach (Map map in Current.Game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
				{
					if (IsNannyBot(pawn))
					{
						yield return pawn;
					}
				}
			}
		}

		/// <summary>设置页的扫地/清洁/搬运总开关同步到每台机器人，保留手动 Gizmo 覆盖。</summary>
		public static void SyncLegacyToggles()
		{
			RoboticsSettings settings = Settings;
			foreach (Pawn pawn in AllNannyBots())
			{
				CompNannyBot comp = BotComp(pawn);
				if (comp == null)
				{
					continue;
				}
				comp.sweep = settings.sweep;
				comp.scrub = settings.scrub;
				comp.haul = settings.haul;
			}
		}

		/// <summary>电池容量变化时按百分比换算现有电量，避免改设置后电量跳变。</summary>
		public static void ApplyBatteryCapacity(int oldCapacity, int newCapacity)
		{
			if (oldCapacity <= 0 || newCapacity <= 0 || oldCapacity == newCapacity)
			{
				return;
			}

			foreach (Pawn pawn in AllNannyBots())
			{
				Need_RobotEnergy energy = Energy(pawn);
				if (energy == null)
				{
					continue;
				}
				float percentage = energy.CurLevel / oldCapacity;
				energy.CurLevel = percentage * newCapacity;
			}
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
