using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 空闲时的兜底建造：缺研究台就先补研究台，缺做饭的地方就先搭篝火。
	/// 只负责下蓝图、标树；真正的搬运和盖房子交给「建造」家务。
	/// </summary>
	public class JobGiver_MkVBuildEssential : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsMkV(pawn) || pawn.Drafted || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			Job job;
			return MkVBuilder.TryBuildMissing(pawn, out job) ? job : null;
		}
	}
}
