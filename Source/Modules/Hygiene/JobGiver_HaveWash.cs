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
			// 模块关闭或检测到 DBH 时，即使旧存档里还留着 CK_Hygiene，
			// 也不应该继续自动派洗澡工作。
			if (!ModuleRegistry.IsEnabled("hygiene")
				|| ModsConfig.IsActive("dubwise.dubsbadhygiene"))
			{
				return 0f;
			}
			Need_Hygiene need = pawn.needs?.TryGetNeed<Need_Hygiene>();
			if (need == null || FoodUtility.ShouldBeFedBySomeone(pawn))
			{
				return 0f;
			}

			float threshold = 0.2f;
			TraitDef hygienic = DefDatabase<TraitDef>.GetNamedSilentFail("Hygienic");
			if (hygienic != null)
			{
				switch (pawn.story?.traits?.DegreeOfTrait(hygienic) ?? 0)
				{
					case 2: threshold = 0.4f; break;
					case 1: threshold = 0.3f; break;
					case 0: threshold = 0.2f; break;
					case -1: threshold = 0.1f; break;
					case -2: threshold = 0f; break;
				}
			}

			if (pawn.mindState?.IsIdle == true)
			{
				threshold = 0.5f;
			}
			Need_Rest rest = pawn.needs?.TryGetNeed<Need_Rest>();
			if (rest != null && rest.CurLevel > 0.95f)
			{
				threshold = 0.6f;
			}
			return need.CurLevel < threshold ? 9.25f : 0f;
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
				Building_Bath bestBath = null;
				float bestBathDistance = float.MaxValue;
				foreach (Building_Bath bath in pawn.Map.listerBuildings
					.AllBuildingsColonistOfClass<Building_Bath>())
				{
					if (!bath.CanUse(pawn) || bath.IsForbidden(pawn))
					{
						continue;
					}
					float distance = pawn.Position.DistanceTo(bath.Position);
					if (distance < bestBathDistance)
					{
						bestBathDistance = distance;
						bestBath = bath;
					}
				}
				if (bestBath != null)
				{
					JobDef bathJob = DefDatabase<JobDef>.GetNamedSilentFail("CK_TakeBath");
					return bathJob == null ? null : JobMaker.MakeJob(bathJob, bestBath);
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
