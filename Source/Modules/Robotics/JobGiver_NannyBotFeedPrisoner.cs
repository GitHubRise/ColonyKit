using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>给殖民地囚犯喂食/送餐；复用原版 Warden 喂食逻辑。</summary>
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
			return null;
		}
	}
}
