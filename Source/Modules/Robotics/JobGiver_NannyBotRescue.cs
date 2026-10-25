using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>最高优先级：把殖民地倒地者抱回床上。</summary>
	public class JobGiver_NannyBotRescue : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.CanWork(pawn) || !NannyBotUtility.Settings.rescue)
			{
				return null;
			}

			WorkGiver_RescueDowned rescue = new WorkGiver_RescueDowned();
			foreach (Pawn patient in pawn.Map.mapPawns.SpawnedDownedPawns)
			{
				if (patient == null || patient == pawn || patient.Dead || !patient.Downed)
				{
					continue;
				}
				if (patient.Faction != Faction.OfPlayer)
				{
					continue;
				}
				if (rescue.HasJobOnThing(pawn, patient))
				{
					Job job = rescue.JobOnThing(pawn, patient);
					if (job != null)
					{
						return job;
					}
				}
			}
			return null;
		}
	}
}
