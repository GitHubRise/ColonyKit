using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 「优先清理室内污渍」：只在设置打开时启用，把有顶且位于 home area 内的
	/// 污渍集中清理（DBH WorkGiver_CleanFilth 的移植，避免和原版清洁工作抢同一批污渍）。
	/// </summary>
	public class WorkGiver_CleanFilthIndoors : WorkGiver_Scanner
	{
		private const int MinTicksSinceThickened = 600;

		private const int MaxQueuedTargets = 15;

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Filth);

		public override int MaxRegionsToScanBeforeGlobalSearch => 4;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerFilthInHomeArea.FilthInHomeArea;
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			if (!HygieneSettings.Current.priorityIndoorCleaning)
			{
				return true;
			}
			return pawn.Map.listerFilthInHomeArea.FilthInHomeArea.Count == 0;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Filth filth))
			{
				return false;
			}
			if (!filth.Map.areaManager.Home[filth.Position] || !filth.Position.Roofed(t.Map))
			{
				return false;
			}
			if (!pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			return filth.TicksSinceThickened >= MinTicksSinceThickened;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Job job = JobMaker.MakeJob(JobDefOf.Clean);
			job.AddQueuedTarget(TargetIndex.A, t);
			Map map = t.Map;
			Room room = t.GetRoom();
			for (int i = 0; i < 100; i++)
			{
				IntVec3 cell = t.Position + GenRadial.RadialPattern[i];
				if (!cell.InBounds(map))
				{
					continue;
				}
				// 只扫同一个房间，避免把整个基地的污渍都塞进一个 Job。
				if (cell.GetRoom(map) != room)
				{
					continue;
				}
				List<Thing> things = cell.GetThingList(map);
				for (int j = 0; j < things.Count; j++)
				{
					Thing other = things[j];
					if (other != t && HasJobOnThing(pawn, other, forced))
					{
						job.AddQueuedTarget(TargetIndex.A, other);
					}
				}
				if (job.GetTargetQueue(TargetIndex.A).Count >= MaxQueuedTargets)
				{
					break;
				}
			}
			if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
			{
				job.targetQueueA.SortBy(targ => targ.Cell.DistanceToSquared(pawn.Position));
			}
			return job;
		}
	}
}
