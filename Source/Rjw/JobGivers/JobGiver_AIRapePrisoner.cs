#nullable disable

using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using Multiplayer.API;
using rjw.Modules.Attraction;

namespace rjw
{
	//强暴 QuestPrisonerWillingToJoin 的囚犯
	class JobGiver_AIRapePrisoner : ThinkNode_JobGiver
	{
		[SyncMethod]
		public static Pawn find_victim(Pawn pawn, Map m)
		{
			float min_fuckability = 0.10f;                          // 不要强暴可性度低于 10% 的小人
			float avg_fuckability = 0f;                             // 目标的平均可性度，选择高于该值的目标
			var valid_targets = new Dictionary<Pawn, float>();      // 有效小人及其可性度
			Pawn chosentarget = null;                               // 最终目标小人

			IEnumerable<Pawn> targets = m.mapPawns.AllPawns.Where(x
				=> x != pawn
				&& IsPrisonerOf(x, pawn.Faction)
				&& xxx.can_get_raped(x)
				&& pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Some, xxx.max_rapists_per_prisoner, 0)
				&& !x.Position.IsForbidden(pawn)
				);

			foreach (Pawn target in targets)
			{
				if (!Pather_Utility.cells_to_target_rape(pawn, target.Position))
					continue;// 太远

				float fuc = AttractionUtility.Evaluate(pawn, target, AttractionPurpose.ForRape);

				if (fuc > min_fuckability)
					if (Pather_Utility.can_path_to_target(pawn, target.Position))
						valid_targets.Add(target, fuc);
			}

			if (valid_targets.Any())
			{
				avg_fuckability = valid_targets.Average(x => x.Value);

				// 选择可性度高于平均的小人
				var valid_targetsFiltered = valid_targets.Where(x => x.Value >= avg_fuckability);

				if (valid_targetsFiltered.Any())
					chosentarget = valid_targetsFiltered.RandomElement().Key;
			}

			return chosentarget;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_AIRapePrisoner::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) called ");

			if (!pawn.HasParts()) return null;

			if (!xxx.can_rape(pawn)) return null;

			if (SexUtility.ReadyForLovin(pawn) || xxx.is_hornyorfrustrated(pawn))
			{
				// 不允许标记为慰安囚犯的小人强暴他人
				if (xxx.is_healthy(pawn))
				{
					Pawn prisoner = find_victim(pawn, pawn.Map);

					if (prisoner != null)
					{
						if (RJWSettings.DebugRape) ModLog.Message(" JobGiver_RandomRape::TryGiveJob( " + xxx.get_pawnname(pawn) + " ) - found victim " + xxx.get_pawnname(prisoner));
						return JobMaker.MakeJob(xxx.RapeRandom, prisoner);
					}
				}
			}

			return null;
		}

		protected static bool IsPrisonerOf(Pawn pawn,Faction faction)
		{
			if (pawn?.guest == null) return false;
			return pawn.guest.HostFaction == faction && pawn.guest.IsPrisoner;
		}
	}
}
