using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>使用屠宰台的屠宰账单处理尸体。</summary>
	public class JobGiver_NannyBotButcher : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.CanWork(pawn) || !NannyBotUtility.Settings.butcher)
			{
				return null;
			}
			if (ThingDefOf.TableButcher == null || pawn.Map == null)
			{
				return null;
			}

			WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail("DoBillsButcherFlesh");
			WorkGiver_DoBill worker = def?.Worker as WorkGiver_DoBill;
			if (worker == null)
			{
				return null;
			}

			foreach (Building table in pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.TableButcher))
			{
				if (!table.Spawned || !NannyBotUtility.IsWithinWorkArea(pawn, table.Position))
				{
					continue;
				}
				Job job = worker.JobOnThing(pawn, table);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}
	}
}
