using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>扫地：清理家区域内的污渍。</summary>
	public class JobGiver_NannyBotSweep : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Sweep;

		protected override Job TryGiveJob(Pawn pawn)
		{
			return CanDoChore(pawn)
				? NannyBotCleanUtility.TryGiveJob(pawn, insideHome: true)
				: null;
		}
	}
}
