using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「搬运」工作：到水龙头/免费取水点灌一批 1 L 瓶装水，再送回原版仓库。
	/// 只在殖民地瓶装水少于「每人 30 L」的缓冲量时开工，避免无限刷水把仓库塞满。
	/// </summary>
	public class WorkGiver_BottleWater : WorkGiver_Scanner
	{
		private const int Batch = 10;

		/// <summary>取水全流程自检期间暂停本工作，避免和测试里手工排的取水账单抢设施。</summary>
		internal static bool SuspendedByTest;

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			Map map = pawn?.Map;
			if (map == null)
			{
				yield break;
			}
			foreach (Building_WaterPoint point in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_WaterPoint>())
			{
				if (point != null)
				{
					yield return point;
				}
			}
			foreach (Building_WaterSpot spot in map.listerBuildings
				.AllBuildingsColonistOfClass<Building_WaterSpot>())
			{
				if (spot != null)
				{
					yield return spot;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (SuspendedByTest
				|| !HygieneSettings.Current.ShouldRunHygiene
				|| pawn?.Map == null
				|| t == null
				|| t.Map != pawn.Map)
			{
				return false;
			}
			int target = StockTarget(pawn.Map);
			if (target <= 0 || CountBottledWater(pawn.Map) >= target)
			{
				return false;
			}
			if (t.IsForbidden(pawn)
				|| t.IsBurning()
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
			{
				return false;
			}
			if (t is Building_WaterPoint point)
			{
				if (!point.CanBottleMineralWater(pawn))
				{
					return false;
				}
			}
			else if (!(t is Building_WaterSpot spot)
				|| !spot.HasWaterSupply
				|| (spot.BillStack != null && spot.BillStack.AnyShouldDoNow))
			{
				// 取水点上还有玩家排的取水/装瓶账单时，先让账单自己的 WorkGiver 干活，
				// 不要用批量打水把设施占住。
				return false;
			}
			return FindStoreCell(pawn) != IntVec3.Invalid;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!HasJobOnThing(pawn, t, forced))
			{
				return null;
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_BottleWaterToStockpile");
			IntVec3 cell = FindStoreCell(pawn);
			if (def == null || cell == IntVec3.Invalid)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(def, t, cell);
			int remaining = StockTarget(pawn.Map) - CountBottledWater(pawn.Map);
			job.count = Mathf.Max(1, Mathf.Min(Batch, remaining));
			return job;
		}

		private static IntVec3 FindStoreCell(Pawn pawn)
		{
			ThingDef bottleDef = MineralWaterUtility.Def;
			if (bottleDef == null)
			{
				return IntVec3.Invalid;
			}
			Thing template = ThingMaker.MakeThing(bottleDef);
			if (!StoreUtility.TryFindBestBetterStorageFor(
				template,
				pawn,
				pawn.Map,
				StoragePriority.Unstored,
				pawn.Faction,
				out IntVec3 cell,
				out _))
			{
				return IntVec3.Invalid;
			}
			return cell;
		}

		/// <summary>
		/// 只按当前缺口生产：每个殖民者缺多少升就补多少瓶，另留 5 瓶应急。
		/// 这样开局不会为了「每人 30 L」一口气灌满几百瓶，水袋掉下来才开工。
		/// </summary>
		private static int StockTarget(Map map)
		{
			float demand = 0f;
			IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (!pawn.IsColonist && !pawn.IsSlave)
				{
					continue;
				}
				float water = WaterPouchUtility.GetWater(pawn);
				if (water < WaterPouchUtility.FillTarget)
				{
					demand += WaterPouchUtility.FillTarget - water;
				}
			}
			return Mathf.Max(5, Mathf.CeilToInt(demand));
		}

		private static int CountBottledWater(Map map)
		{
			int count = 0;
			count += CountDef(map, MineralWaterUtility.Def);
			count += CountDef(map, MineralWaterUtility.PurifiedDef);
			count += CountDef(map, MineralWaterUtility.BoiledDef);
			return count;
		}

		private static int CountDef(Map map, ThingDef def)
		{
			if (def == null)
			{
				return 0;
			}
			int count = 0;
			foreach (Thing thing in map.listerThings.ThingsOfDef(def))
			{
				count += thing.stackCount;
			}
			return count;
		}
	}
}
