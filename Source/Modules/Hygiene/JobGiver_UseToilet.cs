using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobGiver_UseToilet : ThinkNode_JobGiver
	{
		public override float GetPriority(Pawn pawn)
		{
			if (!HygieneSettings.Current.ShouldRunHygiene)
			{
				return 0f;
			}
			Need_Bladder need = pawn.needs?.TryGetNeed<Need_Bladder>();
			if (need == null || FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}
			float threshold = pawn.mindState?.IsIdle == true ? 0.5f : 0.3f;
			Need_Rest rest = pawn.needs?.TryGetNeed<Need_Rest>();
			if (rest != null && rest.CurLevel > 0.95f)
			{
				threshold = 0.6f;
			}
			return need.CurLevel < threshold ? 9.6f : 0f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GetPriority(pawn) <= 0f || pawn.Map == null)
			{
				return null;
			}
			// 动物：优先用玩家阵营的猫砂盆，找不到就憋着（原版行为）。
			if (pawn.RaceProps.Animal)
			{
				if (pawn.Faction != Faction.OfPlayer || pawn.Map == null)
				{
					return null;
				}
				Building_Litterbox box = null;
				float boxDistance = 30f;
				foreach (Building_Litterbox candidate in pawn.Map.listerBuildings
					.AllBuildingsColonistOfClass<Building_Litterbox>())
				{
					if (candidate.IsForbidden(pawn) || !pawn.CanReserve(candidate))
					{
						continue;
					}
					float distance = pawn.Position.DistanceTo(candidate.Position);
					if (distance <= boxDistance)
					{
						boxDistance = distance;
						box = candidate;
					}
				}
				JobDef litterDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_UseToilet");
				return box != null && litterDef != null ? JobMaker.MakeJob(litterDef, box) : null;
			}
			Building_Toilet best = null;
			float bestDistance = float.MaxValue;
			foreach (Building_Toilet toilet in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Toilet>())
			{
				if (!toilet.CanUse(pawn) || toilet.IsForbidden(pawn))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(toilet.Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = toilet;
				}
			}
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_UseToilet");
			return best == null || def == null ? null : JobMaker.MakeJob(def, best);
		}
	}
}
