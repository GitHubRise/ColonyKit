using ColonyKit.Modules.Hygiene;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>卫生维护：清空化粪池、疏通堵塞洁具、清理卫生间污渍。</summary>
	public class JobGiver_NannyBotHygiene : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Hygiene;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}

			Job job = TryUnblock(pawn);
			if (job != null)
			{
				return job;
			}
			job = TryEmptySepticTank(pawn);
			if (job != null)
			{
				return job;
			}
			return NannyBotCleanUtility.TryGiveJob(pawn, insideHome: true, bathroomOnly: true);
		}

		private static Job TryUnblock(Pawn pawn)
		{
			foreach (Building_AssignableFixture fixture in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_AssignableFixture>())
			{
				if (fixture == null || !fixture.Spawned || fixture.blockageComp == null
					|| !fixture.blockageComp.blocked)
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, fixture.Position))
				{
					continue;
				}
				if (!pawn.CanReserveAndReach(fixture, PathEndMode.Touch, Danger.Deadly))
				{
					continue;
				}
				return JobMaker.MakeJob(NannyBotDefOf.CK_NannyUnblock, fixture);
			}
			return null;
		}

		private static Job TryEmptySepticTank(Pawn pawn)
		{
			WorkGiver_EmptySepticTank empty = new WorkGiver_EmptySepticTank();
			foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
			{
				if (building == null || !building.Spawned)
				{
					continue;
				}
				CompSewageHandler sewage = building.TryGetComp<CompSewageHandler>();
				if (sewage == null || sewage.sewageBuffer <= 1f)
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, building.Position))
				{
					continue;
				}
				if (empty.HasJobOnThing(pawn, building))
				{
					return empty.JobOnThing(pawn, building);
				}
			}
			return null;
		}
	}
}
