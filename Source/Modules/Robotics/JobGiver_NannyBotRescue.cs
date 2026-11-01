using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>最高优先级：把殖民地倒地者抱回床上。</summary>
	public class JobGiver_NannyBotRescue : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Rescue;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn))
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
				// 失血/伤势危急又没近处床位：就地铺一张临时医疗睡眠点，直接抱过去。
				Job emergency;
				if (MkVMedicalAid.TryEmergencyRescue(pawn, patient, out emergency))
				{
					return emergency;
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
