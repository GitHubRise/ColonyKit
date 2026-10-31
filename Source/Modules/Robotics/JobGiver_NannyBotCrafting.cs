using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// Mk-V 专用：制作（手工点/制作台/熔炼炉/炼油厂/药品台）、锻造、裁缝、艺术
	/// 四类工作台上的账单。只复用原版 DoBill 工作器，不绕过账单的材料与技能检查。
	/// </summary>
	public class JobGiver_NannyBotCrafting : JobGiver_NannyBotChore
	{
		private static readonly string[] WorkTypeNames = { "Crafting", "Smithing", "Tailoring", "Art" };

		private static List<WorkGiver_DoBill> workers;
		private static int cachedWorkGiverCount = -1;

		protected override RobotChore Chore => RobotChore.Craft;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || !NannyBotUtility.IsMkV(pawn) || pawn.Map == null)
			{
				return null;
			}
			List<WorkGiver_DoBill> craftWorkers = Workers();
			for (int i = 0; i < craftWorkers.Count; i++)
			{
				WorkGiver_DoBill worker = craftWorkers[i];
				List<ThingDef> giverDefs = worker.def.fixedBillGiverDefs;
				if (giverDefs == null)
				{
					continue;
				}
				for (int j = 0; j < giverDefs.Count; j++)
				{
					List<Building> buildings = pawn.Map.listerBuildings.AllBuildingsColonistOfDef(giverDefs[j]);
					for (int k = 0; k < buildings.Count; k++)
					{
						Building building = buildings[k];
						if (!building.Spawned
							|| !NannyBotUtility.IsWithinWorkArea(pawn, building.Position))
						{
							continue;
						}
						Job job = worker.JobOnThing(pawn, building);
						if (job != null)
						{
							return job;
						}
					}
				}
			}
			return null;
		}

		private static bool IsCraftWorkType(WorkTypeDef workType)
		{
			for (int i = 0; i < WorkTypeNames.Length; i++)
			{
				if (workType.defName == WorkTypeNames[i])
				{
					return true;
				}
			}
			return false;
		}

		private static List<WorkGiver_DoBill> Workers()
		{
			List<WorkGiverDef> all = DefDatabase<WorkGiverDef>.AllDefsListForReading;
			if (workers != null && cachedWorkGiverCount == all.Count)
			{
				return workers;
			}
			cachedWorkGiverCount = all.Count;
			workers = new List<WorkGiver_DoBill>();
			for (int i = 0; i < all.Count; i++)
			{
				WorkGiverDef def = all[i];
				if (def.workType == null || !IsCraftWorkType(def.workType)
					|| def.fixedBillGiverDefs == null)
				{
					continue;
				}
				if (def.Worker is WorkGiver_DoBill worker)
				{
					workers.Add(worker);
				}
			}
			return workers;
		}
	}
}
