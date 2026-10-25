using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace rjw
{
	public class JobGiver_ComfortPrisonerRape : ThinkNode_JobGiver
	{
		/// <summary>
		/// 强暴者对慰安囚犯的最低吸引力，达到此值才
		/// 会启用他们。囚犯对强暴者的吸引力
		/// 与我们无关。
		/// </summary>
		const float MinimumAttraction = 0.1f;

		/// <summary>
		/// 找出该小人所在地图上所有可能成为
		/// 其合适强暴对象的小人。
		/// </summary>
		/// <param name="pawn">正在寻找强暴对象的小人。</param>
		/// <returns>潜在受害者的枚举。</returns>
		public static IEnumerable<Pawn> PotentialVictimsFor(Pawn pawn)
		{
			foreach (var victim in DesignatorsData.rjwComfort)
			{
				// 基本的资格检查。
				if (victim == pawn) continue;
				if (victim.Map != pawn.Map) continue;
				if (victim.Suspended) continue;
				if (victim.Drafted) continue;
				if (victim.IsForbidden(pawn)) continue;
				if (!xxx.can_get_raped(victim)) continue;
				if (!Pather_Utility.cells_to_target_rape(pawn, victim.Position)) continue;

				// 动物只考虑它们能看见的目标，而不会主动去寻找。
				if (pawn.IsAnimal() && !pawn.CanSee(victim)) continue;

				yield return victim;
			}
		}

		public static Pawn? FindComfortPrisoner(Pawn pawn)
		{
			if (!DesignatorsData.rjwComfort.Any()) return null;

			string pawnName = xxx.get_pawnname(pawn);
			if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({pawnName})");

			var targets = PotentialVictimsFor(pawn);

			if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({pawnName}): found {targets.Count()}");

			foreach (var result in SexAppraiser.FindVictims(pawn, targets))
			{
				var victim = result.Target;
				var weight = result.ObserverAttraction;
				if (weight < MinimumAttraction)
				{
					if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({pawnName}): {xxx.get_pawnname(victim)} not attractive enough");
					continue;
				}

				if (!pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Some, xxx.max_rapists_per_prisoner, 0))
				{
					if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({pawnName}): cannot path to {xxx.get_pawnname(victim)}");
					continue;
				}

				if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({pawnName}): using {xxx.get_pawnname(victim)} with score {weight}");
				return victim;
			}

			return null;
		}

		protected override Job? TryGiveJob(Pawn pawn)
		{
			if (RJWSettings.DebugRape) ModLog.Message($"JobGiver_ComfortPrisonerRape::TryGiveJob({xxx.get_pawnname(pawn)}) called");

			if (!SexAppraiser.InMoodForRape(pawn))
			{
				if (RJWSettings.DebugRape) ModLog.Message($"JobGiver_ComfortPrisonerRape({xxx.get_pawnname(pawn)}): not in mood for rape.");
				return null;
			}

			if (!pawn.HasParts()) return null;

			if (!RJWSettings.WildMode)
			{
				// 不允许标记为慰安囚犯的小人强暴他人
				if (RJWSettings.DebugRape) ModLog.Message($"JobGiver_ComfortPrisonerRape::TryGiveJob({xxx.get_pawnname(pawn)}): is healthy = {xxx.is_healthy(pawn)}, is cp = {pawn.IsDesignatedComfort()}, is ready = {SexUtility.ReadyForLovin(pawn)}, is frustrated = {xxx.is_frustrated(pawn)}");
				if (!xxx.is_healthy(pawn) || pawn.IsDesignatedComfort() || (!SexUtility.ReadyForLovin(pawn) && !xxx.is_frustrated(pawn))) return null;
			}

			if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({xxx.get_pawnname(pawn)}): can rape = {xxx.can_rape(pawn)}, is drafted = {pawn.Drafted}");
			if (pawn.Drafted || !xxx.can_rape(pawn)) return null;

			// 没必要加入其他工作检查。小人似乎只会在工作间隙或躺下闲着时才找新工作。
			// 但还是检查一下开发者模式吧，以防有人用调试菜单里的 JobGiver 工具来排查为什么不好使
			// "I already have a job (Wait_Wander)" 并不能告诉你它们为什么找不到目标。
			if (pawn.jobs.curJob is { } curJob && curJob.def != JobDefOf.LayDown && !RJWSettings.DevMode)
			{
				if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({xxx.get_pawnname(pawn)}): I already have a job ({pawn.CurJobDef})");
				return null;
			}

			// 派系检查。这会阻止访客和来客使用
			// 慰安囚犯。这是预期行为吗？
			if (pawn.Faction?.IsPlayer is not true && !pawn.IsPrisonerOfColony)
			{
				if (RJWSettings.DebugRape) ModLog.Message($"FindComfortPrisoner({xxx.get_pawnname(pawn)}): not considered part of colony");
				return null;
			}

			var target = FindComfortPrisoner(pawn);
			if (target is null)
			{
				if (RJWSettings.DebugRape) ModLog.Message($"JobGiver_ComfortPrisonerRape::TryGiveJob({xxx.get_pawnname(pawn)}): no target found");
				return null;
			}

			var jobDef = xxx.is_animal(target) ? xxx.bestiality : xxx.RapeCP;
			if (RJWSettings.DebugRape) ModLog.Message($"JobGiver_ComfortPrisonerRape::TryGiveJob({xxx.get_pawnname(pawn)}): using target {xxx.get_pawnname(target)} and job {jobDef.defName}");

			SexUtility.IncreaseTicksToNextHookup(pawn);
			return JobMaker.MakeJob(jobDef, target);
		}
	}
}
