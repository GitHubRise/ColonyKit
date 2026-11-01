using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 建造：把材料搬到蓝图/框架上，再把框架盖完。
	/// 走原版 WorkGiver，但传 forced=true 跳过殖民者的「工作类型分配」检查
	/// （机器人没有工作页，原版检查还会因为 workSettings 为空直接报错）。
	/// </summary>
	public class JobGiver_NannyBotConstruct : JobGiver_NannyBotChore
	{
		protected override RobotChore Chore => RobotChore.Construct;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!CanDoChore(pawn) || pawn.Map == null)
			{
				return null;
			}
			// 用 Def 里的 Worker（自带 def，原版内部会读 def.workType；new 出来的实例 def 是 null，会空引用）。
			Job job = DeliverJob(
				pawn, "ConstructDeliverResourcesToBlueprints", ThingRequestGroup.Blueprint);
			if (job != null)
			{
				return job;
			}
			job = DeliverJob(
				pawn, "ConstructDeliverResourcesToFrames", ThingRequestGroup.BuildingFrame);
			if (job != null)
			{
				return job;
			}
			return FinishJob(pawn);
		}

		private static Job DeliverJob(Pawn pawn, string giverDefName, ThingRequestGroup group)
		{
			WorkGiver_ConstructDeliverResources giver = DefDatabase<WorkGiverDef>
				.GetNamedSilentFail(giverDefName)?.Worker as WorkGiver_ConstructDeliverResources;
			if (giver == null)
			{
				return null;
			}
			foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(group))
			{
				if (thing == null || thing.Destroyed || !thing.Spawned)
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, thing.Position))
				{
					continue;
				}
				if (!giver.HasJobOnThing(pawn, thing, true))
				{
					continue;
				}
				Job job = giver.JobOnThing(pawn, thing, true);
				if (job != null)
				{
					return job;
				}
			}
			return null;
		}

		private static Job FinishJob(Pawn pawn)
		{
			foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
			{
				if (!(thing is Frame frame) || frame.Destroyed || !frame.Spawned)
				{
					continue;
				}
				if (frame.Faction != pawn.Faction || !frame.IsCompleted())
				{
					continue;
				}
				if (!NannyBotUtility.IsWithinWorkArea(pawn, frame.Position))
				{
					continue;
				}
				if (!GenConstruct.CanTouchTargetFromValidCell(frame, pawn))
				{
					continue;
				}
				if (GenConstruct.FirstBlockingThing(frame, pawn) != null)
				{
					continue;
				}
				if (!pawn.CanReach(frame, PathEndMode.Touch, Danger.Deadly)
					|| !pawn.CanReserve(frame))
				{
					continue;
				}
				return JobMaker.MakeJob(JobDefOf.FinishFrame, frame);
			}
			return null;
		}
	}
}
