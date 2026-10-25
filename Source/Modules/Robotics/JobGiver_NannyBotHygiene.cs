using ColonyKit.Modules.Hygiene;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>卫生维护：清空化粪池、疏通堵塞洁具；卫生间污渍由普通清洁覆盖。</summary>
	public class JobGiver_NannyBotHygiene : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.CanWork(pawn) || !NannyBotUtility.Settings.hygiene
				|| pawn.Map == null)
			{
				return null;
			}

			Job job = TryUnblock(pawn);
			if (job != null)
			{
				return job;
			}
			return TryEmptySepticTank(pawn);
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
