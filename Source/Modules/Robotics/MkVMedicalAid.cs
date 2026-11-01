using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 紧急救援：附近有殖民者倒地、失血/伤情危急，而身边又没有马上能用的床时，
	/// Mk-V 就地铺一张「医疗睡眠点」（睡眠点 + 医疗标记，不耗材料、瞬间完成），直接把人抱过去，
	/// 之后照常走医生包扎流程。临时点会记账，空置两个游戏小时后自动拆掉，不在图上留垃圾。
	/// </summary>
	internal static class MkVMedicalAid
	{
		/// <summary>机器人离伤者多远之内才值得跑过去（格）。</summary>
		private const float MaxRobotDistance = 30f;

		/// <summary>现成床离伤者超过这个距离，就不如就地铺一张临时点（格）。</summary>
		private const float MaxBedDistance = 25f;

		/// <summary>在伤者身边多少格内找地方铺床。</summary>
		private const float SpotSearchRadius = 8f;

		/// <summary>临时点空置多久后拆除（约 2 游戏小时）。</summary>
		private const int SpotLifetime = 6000;

		private static readonly Dictionary<Building_Bed, int> TempSpots =
			new Dictionary<Building_Bed, int>();

		private static readonly HashSet<Pawn> TmpReservers = new HashSet<Pawn>();

		private static int lastCleanupTick = -1;

		/// <summary>失血或伤情到了「不马上处理就会死」的程度。</summary>
		public static bool IsCritical(Pawn patient)
		{
			if (patient == null || patient.Dead || !patient.Spawned || !patient.Downed)
			{
				return false;
			}
			if (patient.Faction != Faction.OfPlayer || !patient.RaceProps.Humanlike
				|| patient.IsPrisonerOfColony || patient.InBed())
			{
				return false;
			}
			float bleed = patient.health?.hediffSet?.BleedRateTotal ?? 0f;
			float health = patient.health?.summaryHealth?.SummaryHealthPercent ?? 1f;
			Hediff bloodLoss = patient.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
			float lost = bloodLoss?.Severity ?? 0f;
			return bleed >= 0.35f || lost >= 0.4f || health <= 0.4f;
		}

		/// <summary>机器人附近有没有重伤倒地的殖民者（供 AI 判断要不要放下家务）。</summary>
		public static bool HasCriticalPatientNearby(Pawn robot, float radius)
		{
			Map map = robot?.Map;
			if (map == null)
			{
				return false;
			}
			foreach (Pawn patient in map.mapPawns.SpawnedDownedPawns)
			{
				if (patient == null || !patient.Spawned || !patient.Downed)
				{
					continue;
				}
				if (!patient.Position.InHorDistOf(robot.Position, radius))
				{
					continue;
				}
				if (IsCritical(patient))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 需要时铺一张临时医疗点并给出「直接抱过去」的 Rescue Job。
		/// 附近已经有能用的床时返回 false，交给原版救援走现成的床。
		/// </summary>
		public static bool TryEmergencyRescue(Pawn robot, Pawn patient, out Job job)
		{
			job = null;
			if (robot?.Map == null || patient == null || patient.Map != robot.Map)
			{
				return false;
			}
			if (!IsCritical(patient))
			{
				return false;
			}
			if (robot.Position.DistanceTo(patient.Position) > MaxRobotDistance)
			{
				return false;
			}
			if (!HealthAIUtility.CanRescueNow(robot, patient))
			{
				return false;
			}

			Building_Bed existing = RestUtility.FindBedFor(patient, robot, false, false, null);
			if (existing != null && !existing.Destroyed && existing.Spawned
				&& existing.Position.DistanceTo(patient.Position) <= MaxBedDistance
				&& robot.CanReach(existing, PathEndMode.OnCell, Danger.Deadly))
			{
				return false;
			}

			Building_Bed spot = FindOrCreateSpot(robot, patient);
			if (spot == null)
			{
				return false;
			}
			job = JobMaker.MakeJob(JobDefOf.Rescue, patient, spot);
			job.count = 1;
			return true;
		}

		/// <summary>拆除已经用不上的临时点；每 tick 调一次，内部自己节流。</summary>
		public static void Tick()
		{
			if (TempSpots.Count == 0)
			{
				return;
			}
			int now = Find.TickManager.TicksGame;
			if (now == lastCleanupTick)
			{
				return;
			}
			lastCleanupTick = now;

			List<Building_Bed> stale = null;
			foreach (KeyValuePair<Building_Bed, int> pair in TempSpots)
			{
				Building_Bed bed = pair.Key;
				if (bed == null || bed.Destroyed || !bed.Spawned)
				{
					(stale ?? (stale = new List<Building_Bed>())).Add(bed);
					continue;
				}
				if (bed.AnyOccupants || now - pair.Value < SpotLifetime)
				{
					continue;
				}
				// 空置够久：确认没人预订（可能正有人把它当目标床位）再拆。
				TmpReservers.Clear();
				bed.Map.reservationManager.ReserversOf(bed, TmpReservers);
				if (TmpReservers.Count > 0)
				{
					continue;
				}
				(stale ?? (stale = new List<Building_Bed>())).Add(bed);
			}
			if (stale == null)
			{
				return;
			}
			for (int i = 0; i < stale.Count; i++)
			{
				Building_Bed bed = stale[i];
				TempSpots.Remove(bed);
				if (bed != null && !bed.Destroyed)
				{
					bed.Destroy(DestroyMode.Vanish);
				}
			}
		}

		private static Building_Bed FindOrCreateSpot(Pawn robot, Pawn patient)
		{
			Map map = patient.Map;
			ThingDef def = ThingDefOf.SleepingSpot;
			if (map == null || def == null)
			{
				return null;
			}

			// 之前铺过的临时点就在附近、还空着：直接用，别铺一地。
			foreach (KeyValuePair<Building_Bed, int> pair in TempSpots)
			{
				Building_Bed bed = pair.Key;
				if (bed == null || bed.Destroyed || !bed.Spawned || bed.Map != map
					|| bed.AnyOccupants || bed.IsBurning())
				{
					continue;
				}
				if (bed.Position.DistanceTo(patient.Position) > SpotSearchRadius + 2f)
				{
					continue;
				}
				if (!robot.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
				{
					continue;
				}
				return bed;
			}

			Rot4 rot = Rot4.South;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(
				patient.Position, SpotSearchRadius, true))
			{
				if (!CanPlaceSpot(robot, cell, rot, def))
				{
					continue;
				}
				Building_Bed bed = ThingMaker.MakeThing(def) as Building_Bed;
				if (bed == null)
				{
					continue;
				}
				GenSpawn.Spawn(bed, cell, map, rot);
				if (!bed.Spawned)
				{
					continue;
				}
				bed.Medical = true;
				TempSpots[bed] = Find.TickManager.TicksGame;
				ModLog.Info("Robotics", "Mk-V " + robot.LabelShort + " 为 " + patient.LabelShort
					+ " 就地铺了一张临时医疗睡眠点。");
				return bed;
			}
			return null;
		}

		private static bool CanPlaceSpot(Pawn robot, IntVec3 cell, Rot4 rot, ThingDef def)
		{
			Map map = robot.Map;
			CellRect rect = GenAdj.OccupiedRect(cell, rot, def.size);
			if (!rect.InBounds(map))
			{
				return false;
			}
			foreach (IntVec3 c in rect)
			{
				if (!c.Standable(map) || c.Fogged(map) || c.GetEdifice(map) != null)
				{
					return false;
				}
				if (c.GetFirstPawn(map) != null || c.IsForbidden(robot))
				{
					return false;
				}
				if (FireNear(c, map, 1.5f))
				{
					return false;
				}
			}
			return robot.CanReach(cell, PathEndMode.OnCell, Danger.Deadly);
		}

		private static bool FireNear(IntVec3 cell, Map map, float radius)
		{
			if (map?.listerThings == null)
			{
				return false;
			}
			foreach (Thing fire in map.listerThings.ThingsInGroup(ThingRequestGroup.Fire))
			{
				if (fire != null && fire.Spawned && !fire.Destroyed
					&& fire.Position.InHorDistOf(cell, radius))
				{
					return true;
				}
			}
			return false;
		}
	}
}
