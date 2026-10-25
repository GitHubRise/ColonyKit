#nullable disable

using RimWorld;
using rjw.Modules.Shared.Extensions;
using rjw.Modules.Shared.Logs;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobGiver_JoinInBed : ThinkNode_JobGiver
	{
		private readonly static ILog _log = LogManager.GetLogger<JobGiver_JoinInBed>();

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!RJWHookupSettings.HookupsEnabled)
			{
				if (RJWSettings.DebugLogJoinInBed) _log.Debug("Hookups disabled");
				return null;
			}

			if (pawn.Drafted)
				return null;

			if (!pawn.HasParts()) return null;

			if (!SexUtility.ReadyForHookup(pawn))
				return null;

			string name = pawn.GetName();

			if (RJWSettings.DebugLogJoinInBed) _log.Debug($"{name} was ready");

			// 如果小人是妓女，或最近做过爱，就跳过此工作，除非他们真的欲火难耐
			if (!xxx.is_frustrated(pawn) && (xxx.is_whore(pawn) || !SexUtility.ReadyForLovin(pawn)))
			{
				if (RJWSettings.DebugLogJoinInBed) _log.Debug($"{name} but wasn't ready");
				return null;
			}

			// 此检查试图避免正在离开地图的队伍（如访客或商人）折返来勾搭
			if (pawn.mindState?.duty?.def == DutyDefOf.TravelOrLeave)
			{
				// TODO：有些访客小人一直保持 TravelOrLeave 职责，我想是那些被派去看守驮运动物的。
				// 这大概没问题，虽然并非本意。
				if (RJWSettings.DebugLogJoinInBed) _log.Debug($"JoinInBed.TryGiveJob:({xxx.get_pawnname(pawn)}): has TravelOrLeave, no time for lovin!");
				return null;
			}

			if (pawn.CurJob == null || pawn.CurJob.def == JobDefOf.LayDown)
			{
				//--ModLog.Message("   checking pawn and abilities");
				if (CasualSex_Helper.CanHaveSex(pawn))
				{
					//--ModLog.Message("   finding partner");
					Pawn partner = CasualSex_Helper.FindPartner(pawn, true);

					//--ModLog.Message("   checking partner");
					if (partner == null)
					{
						if (RJWSettings.DebugLogJoinInBed) _log.Debug($"{name} didn't find a partner");
						return null;
					}

					// 永不为 null，因为查找时会检查床。
					Building_Bed bed = partner.CurrentBed();

					// 打断当前工作。
					if (pawn.CurJob != null && pawn.jobs.curDriver != null)
						pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);

					//--ModLog.Message("   returning job");
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.casual_sex, partner, bed);
				}
			}

			return null;
		}
	}
}
