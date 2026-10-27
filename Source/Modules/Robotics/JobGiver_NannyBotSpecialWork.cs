using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>Mk-V 专用：处理炉灶上的烹饪账单。</summary>
	public class JobGiver_NannyBotCook : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Cook;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || !NannyBotUtility.IsMkV(pawn) || pawn.Map == null)
			{
				return null;
			}
			WorkGiver_DoBill worker = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("DoBillsCook")?.Worker as WorkGiver_DoBill;
			if (worker == null)
			{
				return null;
			}
			foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
			{
				if (!(building is IBillGiver) || !building.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, building.Position))
				{
					continue;
				}
				Job job = worker.JobOnThing(pawn, building);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}

	/// <summary>Mk-V 专用：给受伤的殖民者/囚犯做医疗。</summary>
	public class JobGiver_NannyBotDoctor : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Doctor;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || !NannyBotUtility.IsMkV(pawn) || pawn.Map == null)
			{
				return null;
			}
			WorkGiver_TendOther_Humanlike worker = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("DoctorTendToHumanlikes")?.Worker as WorkGiver_TendOther_Humanlike;
			if (worker == null)
			{
				return null;
			}
			System.Collections.Generic.IEnumerable<Thing> things = worker.PotentialWorkThingsGlobal(pawn);
			if (things == null)
			{
				return null;
			}
			foreach (Thing thing in things)
			{
				if (thing == null || !thing.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
				{
					continue;
				}
				if (!worker.HasJobOnThing(pawn, thing))
				{
					continue;
				}
				Job job = worker.JobOnThing(pawn, thing);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}

	/// <summary>Mk-V 专用：在研究台推进当前研究项目。</summary>
	public class JobGiver_NannyBotResearch : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Research;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || !NannyBotUtility.IsMkV(pawn) || pawn.Map == null)
			{
				return null;
			}
			WorkGiver_Researcher worker = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("Research")?.Worker as WorkGiver_Researcher;
			if (worker == null)
			{
				return null;
			}
			System.Collections.Generic.IEnumerable<Thing> things = worker.PotentialWorkThingsGlobal(pawn);
			if (things == null)
			{
				return null;
			}
			foreach (Thing thing in things)
			{
				if (thing == null || !thing.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
				{
					continue;
				}
				if (!worker.HasJobOnThing(pawn, thing))
				{
					continue;
				}
				Job job = worker.JobOnThing(pawn, thing);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}
}
