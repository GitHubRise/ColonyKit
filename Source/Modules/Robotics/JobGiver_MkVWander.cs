using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>核战/征召时不允许 Mk-V 闲逛。</summary>
	public class JobGiver_MkVWander : JobGiver_WanderColony
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn == null || pawn.Drafted || !pawn.Spawned || pawn.Map == null)
			{
				return null;
			}
			return base.TryGiveJob(pawn);
		}
	}
}
