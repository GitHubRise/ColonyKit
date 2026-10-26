using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>清洁：清理家区域外的污渍。</summary>
	public class JobGiver_NannyBotScrub : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Scrub;

		protected override Job TryGiveJob(Pawn pawn)
		{
			return CanDoChore(pawn)
				? NannyBotCleanUtility.TryGiveJob(pawn, insideHome: false)
				: null;
		}
	}
}
