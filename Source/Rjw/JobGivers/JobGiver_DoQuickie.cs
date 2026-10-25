#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	public class JobGiver_DoQuickie : ThinkNode_JobGiver
	{
		/// <summary> 检查所有潜在伙伴看是否有符合条件者，返回最有吸引力且最方便的那一个。 </summary>
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!RJWHookupSettings.HookupsEnabled || !RJWHookupSettings.QuickHookupsEnabled)
				return null;

			if (pawn.Drafted)
				return null;

			if (!pawn.HasParts()) return null;

			if (!SexUtility.ReadyForHookup(pawn))
				return null;

			// 如果小人是妓女，或最近做过爱，就跳过此工作，除非他们真的欲火难耐
			if (!xxx.is_frustrated(pawn) && (xxx.is_whore(pawn) || !SexUtility.ReadyForLovin(pawn)))
				return null;

			// 此检查试图避免正在离开地图的队伍（如访客或商人）折返来勾搭
			if (pawn.mindState?.duty?.def == DutyDefOf.TravelOrLeave)
			{
				// TODO：有些访客小人一直保持 TravelOrLeave 职责，我想是那些被派去看守驮运动物的。
				// 这大概没问题，虽然并非本意。
				if (RJWSettings.DebugLogJoinInBed) ModLog.Message($" Quickie.TryGiveJob:({xxx.get_pawnname(pawn)}): has TravelOrLeave, no time for lovin!");
				return null;
			}

			if (pawn.CurJob == null)
			{
				//--ModLog.Message("   checking pawn and abilities");
				if (CasualSex_Helper.CanHaveSex(pawn))
				{
					//--ModLog.Message("   finding partner");
					Pawn partner = CasualSex_Helper.FindPartner(pawn, false);

					//--ModLog.Message("   checking partner");
					if (partner == null)
						return null;

					// 打断当前工作。
					if (pawn.CurJob != null && pawn.jobs.curDriver != null)
						pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);

					//--ModLog.Message("   returning job");
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.quick_sex, partner);
				}
			}

			return null;
		}
	}
}
