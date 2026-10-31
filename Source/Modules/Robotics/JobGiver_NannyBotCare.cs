using ColonyKit.Modules.Hygiene;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 送水 / 喂水：病人、囚徒口渴又走不到水时，机器人把 1 L 瓶装水送到手上
	/// （凉白开优先，其次纯净水、矿泉水）；自己端不起来的当场喂掉。
	/// 没有瓶装水时退回「拿水盆/水桶去水源打水、床边喂」（<c>CK_WaterPatient</c>）。
	/// 卫生模块关闭时这些小人不口渴，本 JobGiver 自然找不到活。
	/// </summary>
	public class JobGiver_NannyBotWater : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Water;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}
			foreach (Pawn target in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (target == pawn
					|| target.Dead
					|| !target.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, target.Position)
					|| !BringWaterUtility.ShouldDeliverWater(target))
				{
					continue;
				}
				Job job = BringWaterUtility.TryMakeJob(pawn, target);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}

	/// <summary>
	/// 种植：先收成熟的作物（种植区/水培盆里的玉米、稻米等），再砍掉标记了「砍伐/收获」的树和植物，
	/// 最后给空着的种植区播种。只做工作范围内的格子。
	/// </summary>
	public class JobGiver_NannyBotPlants : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Plants;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}
			return TryGrowJob(pawn, "GrowerHarvest")
				?? TryCutJob(pawn)
				?? TryGrowJob(pawn, "GrowerSow");
		}

		/// <summary>按格子扫描的种植类工作（收获/播种都走 WorkGiver_Grower 系列）。</summary>
		private static Job TryGrowJob(Pawn pawn, string workGiverDefName)
		{
			WorkGiver_Scanner giver = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail(workGiverDefName)?.Worker as WorkGiver_Scanner;
			if (giver == null)
			{
				return null;
			}
			foreach (IntVec3 cell in giver.PotentialWorkCellsGlobal(pawn))
			{
				if (!cell.InBounds(pawn.Map)
					|| !NannyBotUtility.IsWithinWorkArea(pawn, cell)
					|| !giver.HasJobOnCell(pawn, cell))
				{
					continue;
				}
				Job job = giver.JobOnCell(pawn, cell);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}

		/// <summary>砍树 / 收野果：原版按「砍伐、收获」标记派活。</summary>
		private static Job TryCutJob(Pawn pawn)
		{
			WorkGiver_Scanner giver = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("PlantsCut")?.Worker as WorkGiver_Scanner;
			if (giver == null)
			{
				return null;
			}
			System.Collections.Generic.IEnumerable<Thing> things = giver.PotentialWorkThingsGlobal(pawn);
			if (things == null)
			{
				return null;
			}
			foreach (Thing thing in things)
			{
				if (thing == null
					|| !thing.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
				{
					continue;
				}
				if (!giver.HasJobOnThing(pawn, thing))
				{
					continue;
				}
				Job job = giver.JobOnThing(pawn, thing);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}
}
