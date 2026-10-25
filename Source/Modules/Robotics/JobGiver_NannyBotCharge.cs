using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>电量低于 25% 时优先回充电桩。</summary>
	public class JobGiver_NannyBotCharge : ThinkNode_JobGiver
	{
		private const float ChargeThreshold = 0.25f;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.Downed
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return null;
			}

			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy == null || energy.CurLevel > ChargeThreshold || energy.IsCharging)
			{
				return null;
			}

			Building station = NannyBotUtility.FindStation(pawn, true);
			if (station == null)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCharge, station);
			job.expiryInterval = 0;
			return job;
		}
	}
}
