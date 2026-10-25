using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>卫生值低时自动寻找最近淋浴器。</summary>
	public class JobGiver_HaveWash : ThinkNode_JobGiver
	{
		public override float GetPriority(Pawn pawn)
		{
			Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || need.CurLevel >= 0.25f)
			{
				return 0f;
			}
			return 9.25f;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (GetPriority(pawn) <= 0f || pawn.Map == null)
			{
				return null;
			}

			Building_Shower best = null;
			float bestDistance = float.MaxValue;
			foreach (Building_Shower shower in pawn.Map.listerBuildings
				.AllBuildingsColonistOfClass<Building_Shower>())
			{
				if (!shower.CanUse(pawn) || shower.IsForbidden(pawn))
				{
					continue;
				}
				float distance = pawn.Position.DistanceTo(shower.Position);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = shower;
				}
			}

			if (best == null)
			{
				return null;
			}
			JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeShower");
			return jobDef == null ? null : JobMaker.MakeJob(jobDef, best);
		}
	}
}
