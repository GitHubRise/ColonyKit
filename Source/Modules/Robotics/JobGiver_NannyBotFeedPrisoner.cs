using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>送餐/喂餐：给殖民地囚犯送饭（原版 Warden 喂食），也给卧床病人喂食（原版医生喂食）。</summary>
	public class JobGiver_NannyBotFeedPrisoner : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.FeedPrisoners;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}

			WorkGiver_Warden_Feed feed = new WorkGiver_Warden_Feed();
			foreach (Pawn prisoner in pawn.Map.mapPawns.PrisonersOfColonySpawned)
			{
				if (prisoner == null || prisoner.Dead || !prisoner.Spawned)
				{
					continue;
				}
				Job job = feed.JobOnThing(pawn, prisoner);
				if (job != null)
				{
					return job;
				}
			}

			// 卧床病人：原版「给病人喂食」工作器（DoctorFeedHumanlikes）。
			WorkGiver_FeedPatient patientFeed = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail("DoctorFeedHumanlikes")?.Worker as WorkGiver_FeedPatient;
			if (patientFeed == null || patientFeed.PotentialWorkThingsGlobal(pawn) == null)
			{
				return null;
			}
			foreach (Thing thing in patientFeed.PotentialWorkThingsGlobal(pawn))
			{
				if (thing == null
					|| !thing.Spawned
					|| !NannyBotUtility.IsWithinWorkArea(pawn, thing.Position)
					|| !patientFeed.HasJobOnThing(pawn, thing))
				{
					continue;
				}
				Job job = patientFeed.JobOnThing(pawn, thing);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}
}
