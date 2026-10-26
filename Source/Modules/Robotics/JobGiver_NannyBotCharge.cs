using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>电量低于设置阈值时优先回充电桩。</summary>
	public class JobGiver_NannyBotCharge : ThinkNode_JobGiver
	{
		public override float GetPriority(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.Downed
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return 0f;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp != null && (comp.paused || comp.repairing
				|| NannyBotUtility.IsBeingRepaired(pawn)))
			{
				return 0f;
			}
			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy == null || energy.IsCharging
				|| energy.CurLevelPercentage > NannyBotUtility.Settings.chargeThreshold)
			{
				return 0f;
			}
			// 充电永远高于普通家务（最高优先级 1 对应 8；充电为 9）。
			return 9f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.IsNannyBot(pawn) || pawn.Downed
				|| !ModuleRegistry.IsEnabled(NannyBotUtility.ModuleId))
			{
				return null;
			}
			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp != null && (comp.paused || comp.repairing
				|| NannyBotUtility.IsBeingRepaired(pawn)))
			{
				return null;
			}

			Need_RobotEnergy energy = NannyBotUtility.Energy(pawn);
			if (energy == null || energy.IsCharging)
			{
				return null;
			}
			if (energy.CurLevelPercentage > NannyBotUtility.Settings.chargeThreshold)
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
