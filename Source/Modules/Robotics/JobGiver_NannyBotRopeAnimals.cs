using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>牵引动物：把殖民地动物牵回围栏/拴系点；没有目标时不会主动找动物。</summary>
	public class JobGiver_NannyBotRopeAnimals : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.RopeAnimals;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}

			WorkGiver_TakeToPen takeToPen = new WorkGiver_TakeToPen();
			WorkGiver_TakeRoamingAnimalsToPen roaming = new WorkGiver_TakeRoamingAnimalsToPen();
			foreach (Pawn animal in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
			{
				if (animal == null || animal == pawn || !animal.IsAnimal
					|| animal.Dead || animal.Downed || !animal.Spawned)
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, animal.Position))
				{
					continue;
				}

				Job job = takeToPen.JobOnThing(pawn, animal);
				if (job == null)
				{
					job = roaming.JobOnThing(pawn, animal);
				}
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}
}
