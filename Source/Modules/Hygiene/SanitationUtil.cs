using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	internal static class SanitationUtil
	{
		public static void GiveNastyGutBug(Pawn pawn, ContaminationLevel level)
		{
			if (pawn?.health == null
				|| level == ContaminationLevel.Treated
				|| HygieneSettings.Current.contaminationChanceMultiplier <= 0f
				|| HygieneDefOf.CK_GutBug == null
				|| pawn.health.hediffSet.HasHediff(HygieneDefOf.CK_GutBug))
			{
				return;
			}
			pawn.health.AddHediff(HediffMaker.MakeHediff(HygieneDefOf.CK_GutBug, pawn));
			if (PawnUtility.ShouldSendNotificationAbout(pawn))
			{
				Messages.Message(
					"CK.Hygiene.GutBugMessage".Translate(pawn.LabelShort),
					pawn,
					MessageTypeDefOf.NegativeEvent);
			}
		}

		public static void WaterTempCheck(Pawn pawn, bool cold, bool bath)
		{
			if (pawn?.needs?.mood?.thoughts?.memories == null)
			{
				return;
			}
			ThoughtDef thought = null;
			if (cold)
			{
				thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_ShowerCold");
			}
			else if (!bath)
			{
				thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_ShowerHot");
			}
			if (thought != null)
			{
				pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
			}
			if (!bath)
			{
				ThoughtDef showeredToday = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_ShoweredToday");
				if (showeredToday != null)
				{
					pawn.needs.mood.thoughts.memories.TryGainMemory(showeredToday);
				}
			}
		}

		public static void ContaminationCheckWater(Pawn pawn, ContaminationLevel level)
		{
			if (pawn?.health == null || level < ContaminationLevel.Untreated)
			{
				return;
			}
			float chance = HygieneSettings.Current.contaminationChanceMultiplier;
			if (chance <= 0f)
			{
				return;
			}
			if (level == ContaminationLevel.Untreated)
			{
				// 未处理水的基准概率沿用 DBH 的 0.2%，设置只做倍率。
				if (!Rand.Chance(0.002f * chance))
				{
					return;
				}
			}
			else if (chance < 1f && !Rand.Chance(chance))
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
			if (source == null
				|| !source.Spawned
				|| source.Map == null
				|| !source.Map.IsPlayerHome
				|| source.IsPrisoner
				|| source.RaceProps.Animal
				|| source.IsWildMan())
			{
				return false;
			}
			foreach (Thing thing in GenRadial.RadialDistinctThingsAround(
				source.Position, source.Map, radius, true))
			{
				if (thing is Pawn other && other != source && source.CaresIfSeenShittingBy(other))
				{
					ThoughtDef privacy = DefDatabase<ThoughtDef>.GetNamedSilentFail("CK_ToiletPrivacy");
					if (privacy != null)
					{
						source.needs?.mood?.thoughts?.memories?.TryGainMemory(privacy);
					}
					return true;
				}
			}
			return false;
		}

		private static bool CaresIfSeenShittingBy(this Pawn source, Pawn target)
		{
			if (target == null || target.RaceProps.Animal || target.RaceProps.IsMechanoid)
			{
				return false;
			}
			if (LovePartnerRelationUtility.LovePartnerRelationExists(source, target))
			{
				return false;
			}
			if (source.Map == null || target.Map != source.Map)
			{
				return false;
			}
			// 隔间门只挡隐私视线：门所在格当作遮挡，普通战斗视线不受影响。
			return GenSight.LineOfSight(
				source.Position, target.Position, source.Map, skipFirstCell: false,
				validator: c => !(c.GetEdifice(source.Map) is Building_StallDoor));
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
			if (!GenSight.LineOfSight(
				source.Position, target.Position, source.Map, skipFirstCell: false,
				validator: c => !(c.GetEdifice(source.Map) is Building_StallDoor)))
			{
				return false;
			}
			return source.gender != target.gender;
		}
	}

	internal static class ShowerCleanup
	{
		private const float CumCleanRadius = 2.5f;
		private const int MaxRoomFilth = 12;

		private static readonly string[] CumFilthDefNames =
		{
			"FilthCum",
			"FilthGirlCum",
			"FilthInsectCum",
			"ResinCum"
		};

		private static HashSet<ThingDef> cumFilthDefs;

		private static HashSet<ThingDef> CumFilthDefs
		{
			get
			{
				if (cumFilthDefs == null)
				{
					cumFilthDefs = new HashSet<ThingDef>();
					for (int i = 0; i < CumFilthDefNames.Length; i++)
					{
						ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(CumFilthDefNames[i]);
						if (def != null)
						{
							cumFilthDefs.Add(def);
						}
					}
				}
				return cumFilthDefs;
			}
		}

		/// <summary>清掉淋浴器周围 2.5 格内的 RJW 精液污渍；没装 RJW 时集合为空。</summary>
		public static void ClearCumAround(Pawn pawn, IntVec3 center)
		{
			if (!CanRun(pawn) || CumFilthDefs.Count == 0)
			{
				return;
			}
			Map map = pawn.Map;
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, CumCleanRadius, true))
			{
				if (!cell.InBounds(map))
				{
					continue;
				}
				List<Thing> things = cell.GetThingList(map);
				for (int i = things.Count - 1; i >= 0; i--)
				{
					if (things[i] is Filth filth && CumFilthDefs.Contains(filth.def))
					{
						filth.Destroy(DestroyMode.Vanish);
					}
				}
			}
		}

		/// <summary>把小人自己房间里的污渍排成一条原版清洁工作。</summary>
		public static void TryQueueRoomCleaning(Pawn pawn)
		{
			if (!CanRun(pawn)
				|| pawn.Downed
				|| pawn.Dead
				|| pawn.InMentalState
				|| pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning)
				|| !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				return;
			}
			Building_Bed bed = pawn.ownership?.OwnedBed;
			Room room = bed?.GetRoom();
			if (room == null)
			{
				return;
			}
			List<Filth> filths = new List<Filth>();
			foreach (IntVec3 cell in room.Cells)
			{
				List<Thing> things = cell.GetThingList(pawn.Map);
				for (int i = 0; i < things.Count; i++)
				{
					if (!(things[i] is Filth filth)
						|| filth.Destroyed
						|| !filth.Position.InAllowedArea(pawn)
						|| !pawn.CanReach(filth, PathEndMode.Touch, Danger.Some))
					{
						continue;
					}
					filths.Add(filth);
					if (filths.Count >= MaxRoomFilth)
					{
						break;
					}
				}
				if (filths.Count >= MaxRoomFilth)
				{
					break;
				}
			}
			if (filths.Count == 0)
			{
				return;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Clean);
			for (int i = 0; i < filths.Count; i++)
			{
				job.AddQueuedTarget(TargetIndex.A, filths[i]);
			}
			job.playerForced = true;
			pawn.jobs.jobQueue.EnqueueFirst(job);
		}

		private static bool CanRun(Pawn pawn)
		{
			return pawn != null && pawn.Spawned && pawn.Map != null;
		}
	}
}
