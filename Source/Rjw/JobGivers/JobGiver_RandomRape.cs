using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace rjw
{
	public class JobGiver_RandomRape : ThinkNode_JobGiver
	{
		/// <summary>
		/// 找出该小人所在地图上所有可能成为
		/// 其合适强暴对象的小人。
		/// </summary>
		/// <param name="pawn">正在寻找强暴对象的小人。</param>
		/// <returns>潜在受害者的枚举。</returns>
		public static IEnumerable<Pawn> PotentialVictimsFor(Pawn pawn)
		{
			foreach (var victim in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				// 基本的资格检查。
				if (victim == pawn) continue;
				if (victim.Suspended) continue;
				if (victim.Drafted) continue;
				if (victim.IsForbidden(pawn)) continue;
				if (victim.HostileTo(pawn)) continue;
				if (!xxx.is_not_dying(victim)) continue;
				if (!xxx.can_get_raped(victim)) continue;
				if (!Pather_Utility.cells_to_target_rape(pawn, victim)) continue;
				if (!pawn.CanReserve(victim, xxx.max_rapists_per_prisoner, 0)) continue;

				yield return victim;
			}
		}

		public Pawn? FindVictim(Pawn pawn)
		{
			if (!pawn.HasParts()) return null;
			// 可能是囚犯、殖民者、非敌对的外来者，甚至动物。
			var targets = PotentialVictimsFor(pawn).ToList();

			// TODO：我们是否应该始终禁用性向检查？也可以
			// 仅在没有符合其偏好的目标时才禁用，
			// 这样他们就不会在没有
			// 充分理由的情况下突然违背自己已确立的角色设定。
			foreach (var result in SexAppraiser.FindVictims(pawn, targets, true, true))
				if (Pather_Utility.can_path_to_target(pawn, result.Target))
					return result.Target;

			return null;
		}

		protected override Job? TryGiveJob(Pawn pawn)
		{
			//ModLog.Message(" JobGiver_RandomRape::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called");
			if (!xxx.can_rape(pawn)) return null;

			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_RapeEnemyCD"))) return null;
			pawn.health.AddHediff(HediffDef.Named("Hediff_RapeEnemyCD"), null, null, null);

			if (FindVictim(pawn) is not { } victim) return null;

			//ModLog.Message(" JobGiver_RandomRape::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - found victim " + xxx.get_pawnname(victim));
			SexUtility.IncreaseTicksToNextHookup(pawn);
			return JobMaker.MakeJob(xxx.RapeRandom, victim);
		}
	}
}