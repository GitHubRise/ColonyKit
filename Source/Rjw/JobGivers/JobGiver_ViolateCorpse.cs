#nullable disable

using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using rjw.Modules.Attraction;

namespace rjw
{
	public class JobGiver_ViolateCorpse : ThinkNode_JobGiver
	{
		[SyncMethod]
		public static Corpse find_corpse(Pawn pawn, Map m)
		{
			float min_fuckability = 0.10f;                            // 不要强暴可性度低于 10% 的小人
			float avg_fuckability = 0f;                               // 计算目标的平均可性度，选择可性度高于该值的目标
			var valid_targets = new Dictionary<Corpse, float>();      // 有效的小人及其可性度
			Corpse chosentarget = null;                               // 最终目标小人

			IEnumerable<Thing> targets = m.spawnedThings.Where(x 
				=> x is Corpse 
				&& pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Some)
				&& !x.IsForbidden(pawn)
				);

			foreach (Corpse target in targets)
			{
				if (!Pather_Utility.cells_to_target_rape(pawn, target.Position))
					continue;// 距离过远

				// 若不是恋尸癖，过滤掉腐烂的尸体。
				if (!xxx.is_necrophiliac(pawn) && target.CurRotDrawMode != RotDrawMode.Fresh)
					continue;

				float fuc = AttractionUtility.Evaluate(pawn, target.InnerPawn, AttractionPurpose.ForRape);

				if (fuc > min_fuckability)
					if (Pather_Utility.can_path_to_target(pawn, target.Position))
						valid_targets.Add(target, fuc);
			}

			if (valid_targets.Any())
			{
				avg_fuckability = valid_targets.Average(x => x.Value);

				// 选择可性度高于平均值的小人进行性交
				var valid_targetsFilteredAnimals = valid_targets.Where(x => x.Value >= avg_fuckability);

				if (valid_targetsFilteredAnimals.Any())
					chosentarget = valid_targetsFilteredAnimals.RandomElement().Key;
			}

			return chosentarget;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			// 大部分检查在 ThinkNode_ConditionalNecro 中完成。

			// 为花痴过滤掉恋尸行为
			if (!RJWSettings.necrophilia_enabled) return null;

			if (pawn.Drafted) return null;

			if (!pawn.HasParts()) return null;

			//--ModLog.Message(" JobGiver_ViolateCorpse::TryGiveJob for ( " + xxx.get_pawnname(pawn) + " )");
			if (SexUtility.ReadyForLovin(pawn) || xxx.is_hornyorfrustrated(pawn))
			{
				//--ModLog.Message(" JobGiver_ViolateCorpse::TryGiveJob, can love ");
				if (!xxx.can_rape(pawn)) return null;

				var target = find_corpse(pawn, pawn.Map);
				//--ModLog.Message(" JobGiver_ViolateCorpse::TryGiveJob - target is " + (target == null ? "NULL" : "Found"));
				if (target != null)
				{
					SexUtility.IncreaseTicksToNextHookup(pawn);
					return JobMaker.MakeJob(xxx.RapeCorpse, target);
				}
				// 只有在性交成功后才会累加 tick 数。
			}

			return null;
		}
	}
}
