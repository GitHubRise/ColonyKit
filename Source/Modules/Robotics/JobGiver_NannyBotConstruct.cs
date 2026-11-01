using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 建造：把材料搬到蓝图/框架上，再把框架盖完。
	/// 走原版 WorkGiver，但传 forced=false：机器人没有工作页，
	/// 由 Patch_GenConstruct_CanConstruct 替它们跳过「有没有分配工作类型」这一项，
	/// 材料/框架的预约检查则保留——否则两个机器人会抢同一摞木头，
	/// 作业一开始就 TryMakePreToilReservations 失败刷红字。
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
			job = FinishJob(pawn);
			if (job != null)
			{
				return job;
			}
			// 蓝图还差木料、地图上又没木头：自己去砍，别站在工地上干等。
			if (MkVBuilder.TryGatherWood(pawn, out job))
			{
				return job;
			}
			// 差钢铁：去矿里挖「压实的钢材」。
			return MkVBuilder.TryGatherSteel(pawn, out job) ? job : null;
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
				if (!giver.HasJobOnThing(pawn, thing, false))
				{
					continue;
				}
				Job job = giver.JobOnThing(pawn, thing, false);
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
