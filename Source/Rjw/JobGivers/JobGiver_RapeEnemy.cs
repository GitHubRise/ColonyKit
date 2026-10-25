#nullable disable

using Verse;
using Verse.AI;
using RimWorld;

namespace rjw
{
	/// <summary>
	/// 小人尝试寻找敌人来强暴。
	/// </summary>
	public class JobGiver_RapeEnemy : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called0");

			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) Drafted " + pawn.Drafted);
			if (pawn.Drafted) return null;

			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) Hediff_RapeEnemyCD " + pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_RapeEnemyCD")));
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) CanBeAwake " + pawn.health.capacities.CanBeAwake);
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) ReadyForLovin " + SexUtility.ReadyForLovin(pawn));
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) need_some_sex " + (xxx.need_some_sex(pawn) <= 1f));
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) either " + (SexUtility.ReadyForLovin(pawn) || xxx.need_some_sex(pawn) <= 1f));
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) canLovinTick ok " + (Find.TickManager.TicksGame > pawn.mindState.canLovinTick));
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) TicksGame " + Find.TickManager.TicksGame);
			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) canLovinTick " + pawn.mindState.canLovinTick);

			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_RapeEnemyCD")) ||
				!pawn.health.capacities.CanBeAwake ||
				!(SexUtility.ReadyForLovin(pawn) || xxx.need_some_sex(pawn) <= 1f))
			//if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_RapeEnemyCD")) || !pawn.health.capacities.CanBeAwake || (SexUtility.ReadyForLovin(pawn) || xxx.is_human(pawn) ? xxx.need_some_sex(pawn) <= 1f : false))
				return null;

			if (!pawn.HasParts()) return null;

			//ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) can_rape " + xxx.can_rape(pawn));
			if (!xxx.can_rape(pawn)) return null;
			if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) can rape");

			JobDef_RapeEnemy rapeEnemyJobDef = null;
			int? highestPriority = null;
			foreach (JobDef_RapeEnemy job in DefDatabase<JobDef_RapeEnemy>.AllDefs)
			{
				if (job.CanUseThisJobForPawn(pawn))
				{
					if (highestPriority == null)
					{
						rapeEnemyJobDef = job;
						highestPriority = job.priority;
					}
					else if (job.priority > highestPriority)
					{
						rapeEnemyJobDef = job;
						highestPriority = job.priority;
					}
				}
			}

			if (rapeEnemyJobDef == null)
			{
				if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) no valid rapeEnemyJobDef found");
				return null;
			}

			if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::ChoosedJobDef( " + xxx.get_pawnname(pawn) + " ) - " + rapeEnemyJobDef?.ToString() + " choosen");
			Pawn victim = rapeEnemyJobDef?.FindVictim(pawn, pawn.Map);

			if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::FoundVictim( " + xxx.get_pawnname(victim) + " )");

			//防止出现 10 层工作堆栈错误，不清楚 JobDriver_Rape 有什么问题
			//if (victim != null)
			pawn.health.AddHediff(HediffDef.Named("Hediff_RapeEnemyCD"), null, null, null);

			return victim != null ? JobMaker.MakeJob(rapeEnemyJobDef, victim) : null;
			/*
			else
			{
				if (RJWSettings.DebugRape) ModLog.Message("" + this.GetType().ToString() + "::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - unable to find victim");
				pawn.mindState.canLovinTick = Find.TickManager.TicksGame + Rand.Range(75, 150);
			}
			*/
			//else {  if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RapeEnemy::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - too fast to play next"); }
		}
	}
}
