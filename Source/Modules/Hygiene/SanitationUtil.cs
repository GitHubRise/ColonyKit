using RimWorld;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	internal static class SanitationUtil
	{
		public static void GiveNastyGutBug(Pawn pawn, ContaminationLevel level)
		{
			// 肠道疾病/污水系统后续迁移；保留 DBH 调用点。
		}

		public static void WaterTempCheck(Pawn pawn, bool cold, bool bath)
		{
			if (!cold || pawn?.needs?.mood?.thoughts?.memories == null)
			{
				return;
			}
			ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_ShowerCold");
			if (thought != null)
			{
				pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
			}
		}

		public static void ContaminationCheckWater(Pawn pawn, ContaminationLevel level)
		{
			if (pawn?.health == null || level < ContaminationLevel.Contaminated)
			{
				return;
			}
			// 脏水洗完后要把污染标记重新打上；否则 CleanContamination 会把
			// “这次用脏水洗过”也一起洗掉。
			pawn.needs?.TryGetNeed<Need_Hygiene>()?.Contaminated();
			HediffDef badHygiene = HygieneDefOf.CK_BadHygiene;
			if (badHygiene != null)
			{
				HealthUtility.AdjustSeverity(pawn, badHygiene, 0.15f);
			}
		}

		public static void ApplyBathroomThought(Pawn pawn, Building_AssignableFixture fixture)
		{
			// finish action 可能在设施被拆除/搬运后执行，此时 Map 为 null，GetRoom 会抛 NRE。
			if (pawn == null || fixture == null || !fixture.Spawned || fixture.Map == null)
			{
				return;
			}
			Room room = fixture.Position.GetRoom(fixture.Map);
			bool privateRoom = room?.Role?.defName == "CK_PrivateBathroom";
			bool assigned = !fixture.AssignedPawns.NullOrEmpty() && fixture.AssignedPawns.Contains(pawn);
			if (!privateRoom && !assigned)
			{
				if (room?.Role?.defName == "CK_PublicBathroom")
				{
					ThoughtDef publicThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_PublicBathroom");
					if (publicThought != null)
					{
						int publicStage = room == null ? 1
							: RoomStatDefOf.Impressiveness.GetScoreStageIndex(
								room.GetStat(RoomStatDefOf.Impressiveness));
						publicStage = Mathf.Clamp(publicStage, 0, publicThought.stages.Count - 1);
						pawn.needs?.mood?.thoughts?.memories?.TryGainMemoryFast(publicThought, publicStage);
					}
				}
				return;
			}
			ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_PrivateBathroom");
			if (thought == null)
			{
				return;
			}
			int stage = 0;
			if (room != null)
			{
				stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(
					room.GetStat(RoomStatDefOf.Impressiveness));
			}
			stage = Mathf.Clamp(stage, 0, thought.stages.Count - 1);
			pawn.needs?.mood?.thoughts?.memories?.TryGainMemoryFast(thought, stage);
		}

		public static void ContaminationFromCellForPawn(Pawn pawn, IntVec3 cell)
		{
			if (pawn?.Map == null || !cell.InBounds(pawn.Map))
			{
				return;
			}
			bool dirty = false;
			foreach (Thing thing in cell.GetThingList(pawn.Map))
			{
				if (thing is Filth)
				{
					dirty = true;
					break;
				}
			}
			if (!dirty)
			{
				MapComponent_SewagePollution pollution =
					pawn.Map.GetComponent<MapComponent_SewagePollution>();
				dirty = pollution != null && pollution.PollutionAt(cell) > 0.5f;
			}
			if (dirty)
			{
				pawn.needs?.TryGetNeed<Need_Hygiene>()?.Contaminated();
			}
		}

		public static void CheckForBlockage(Building building)
		{
			building?.GetComp<CompBlockage>()?.CheckForBlockage();
		}
	}

	internal static class PrivacyUtil
	{
		public static bool BathingPrivacyLOS(Pawn source, float radius = 6f)
		{
			if (source == null || !source.Spawned || source.Map == null)
			{
				return false;
			}
			foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
				source.Position, source.Map, radius, true))
			{
				if (thing is Pawn other && other != source && source.CaresIfSeenBathingBy(other))
				{
					ThoughtDef privacy = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_WashPrivacy");
					if (privacy != null)
					{
						source.needs?.mood?.thoughts?.memories?.TryGainMemory(privacy);
					}
					return true;
				}
			}
			return false;
		}

		public static bool ToiletPrivacyLOS(Pawn source, float radius = 7f)
		{
			return false;
		}

		public static bool CaresIfSeenBathingBy(this Pawn source, Pawn target)
		{
			if (target == null || target.RaceProps.Animal || target.RaceProps.IsMechanoid)
			{
				return false;
			}
			if (LovePartnerRelationUtility.LovePartnerRelationExists(source, target))
			{
				return false;
			}
			if (source.Map == null || !source.Map.IsPlayerHome)
			{
				return false;
			}
			if (!GenSight.LineOfSight(source.Position, target.Position, source.Map))
			{
				return false;
			}
			return source.gender != target.gender;
		}
	}

	internal static class ShowerCleanup
	{
		public static void ClearCumAround(Pawn pawn, IntVec3 cell)
		{
			// RJW 污渍清理后续接。
		}

		public static void TryQueueRoomCleaning(Pawn pawn)
		{
		}
	}
}
