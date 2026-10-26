using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>灭火：扑灭工作范围内的火。</summary>
	public class JobGiver_NannyBotFirefight : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Firefight;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn))
			{
				return null;
			}

			Thing fire = GenClosest.ClosestThingReachable(
				pawn.Position,
				pawn.Map,
				ThingRequest.ForDef(ThingDefOf.Fire),
				PathEndMode.Touch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing thing) => CanFightFire(pawn, thing));

			if (fire == null)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(JobDefOf.BeatFire, fire);
			job.expiryInterval = 5000;
			return job;
		}

		private static bool CanFightFire(Pawn pawn, Thing thing)
		{
			if (!(thing is Fire fire) || !fire.Spawned || fire.Destroyed)
			{
				return false;
			}
			if (!NannyBotUtility.IsWithinWorkArea(pawn, fire.Position))
			{
				return false;
			}
			if (fire.Position.Fogged(pawn.Map))
			{
				return false;
			}

			Pawn reserver = pawn.Map.reservationManager.FirstRespectedReserver(fire, pawn);
			if (reserver != null && reserver.Position.InHorDistOf(fire.Position, 5f))
			{
				return false;
			}

			return pawn.CanReserve(fire)
				&& pawn.CanReach(fire, PathEndMode.Touch, Danger.Deadly);
		}
	}
}
