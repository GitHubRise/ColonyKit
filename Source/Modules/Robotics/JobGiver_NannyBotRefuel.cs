using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>给木柴/化合燃料发电机加燃料，低于设置阈值才动手。</summary>
	public class JobGiver_NannyBotRefuel : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.CanWork(pawn) || !NannyBotUtility.Settings.refuel
				|| pawn.Map == null)
			{
				return null;
			}

			Thing target = GenClosest.ClosestThing_Global_Reachable(
				pawn.Position,
				pawn.Map,
				pawn.Map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Refuelable)),
				PathEndMode.Touch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing thing) => CanRefuelGenerator(pawn, thing));

			if (target == null)
			{
				return null;
			}
			return RefuelWorkGiverUtility.RefuelJob(pawn, target, forced: true);
		}

		private static bool CanRefuelGenerator(Pawn pawn, Thing thing)
		{
			if (thing == null || !thing.Spawned || thing.Faction != pawn.Faction)
			{
				return false;
			}
			if (thing.TryGetComp<CompPowerPlant>() == null)
			{
				return false;
			}
			CompRefuelable fuel = thing.TryGetComp<CompRefuelable>();
			if (fuel == null || !fuel.allowAutoRefuel || fuel.IsFull)
			{
				return false;
			}
			if (fuel.FuelPercentOfMax > NannyBotUtility.Settings.refuelThreshold)
			{
				return false;
			}
			if (!NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
			{
				return false;
			}
			return RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced: true);
		}
	}
}
