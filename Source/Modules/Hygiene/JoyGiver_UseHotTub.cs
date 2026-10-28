using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>「去泡热水浴缸」的娱乐 JobGiver（DBH JoyGiver_UseHotTub 的移植）。</summary>
	public class JoyGiver_UseHotTub : JoyGiver_InteractBuilding
	{
		protected override bool CanDoDuringGathering => true;

		protected override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			if (t is Building_HotTub tub
				&& tub.Working().Accepted
				&& tub.PawnAllowed(pawn)
				&& !tub.IsForbidden(pawn)
				&& pawn.CanReserveAndReach(tub, PathEndMode.OnCell, Danger.Some, 2, 0))
			{
				return JobMaker.MakeJob(def.jobDef, t);
			}
			return null;
		}
	}
}
