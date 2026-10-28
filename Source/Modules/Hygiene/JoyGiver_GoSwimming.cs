using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>「去游泳」的 JoyGiver（DBH JoyGiver_GoSwimming 的移植）：水温够、有水、没下雨才去。</summary>
	public class JoyGiver_GoSwimming : JoyGiver_InteractBuilding
	{
		protected override bool CanDoDuringGathering => true;

		protected override Job TryGivePlayJob(Pawn pawn, Thing t)
		{
			if (t is Building_Pool pool
				&& pool.Working().Accepted
				&& !pool.IsForbidden(pawn)
				&& pawn.CanReserveAndReach(pool, PathEndMode.Touch, Danger.Some))
			{
				if (t.Map.weatherManager.curWeather.rainRate > 0.01f
					&& t.OccupiedRect().Cells.Any(c => !c.Roofed(t.Map)))
				{
					return null;
				}
				return JobMaker.MakeJob(def.jobDef, t);
			}
			return null;
		}
	}
}
