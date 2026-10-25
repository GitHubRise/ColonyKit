using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>
	/// 扫地 / 清洁：先清理家区域内的污渍（扫地），再清理家区域外的污渍（清洁）。
	/// 两项都关时不会扫描污渍。
	/// </summary>
	public class JobGiver_NannyBotClean : ThinkNode_JobGiver
	{
		private const int MinTicksSinceThickened = 600;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!NannyBotUtility.CanWork(pawn))
			{
				return null;
			}

			CompNannyBot comp = NannyBotUtility.BotComp(pawn);
			if (comp == null || (!comp.sweep && !comp.scrub))
			{
				return null;
			}

			Filth filth = null;
			if (comp.sweep)
			{
				filth = FindFilth(pawn, insideHome: true);
			}
			if (filth == null && comp.scrub)
			{
				filth = FindFilth(pawn, insideHome: false);
			}
			if (filth == null)
			{
				return null;
			}

			Job job = JobMaker.MakeJob(NannyBotDefOf.CK_NannyCleanFilth, filth);
			job.expiryInterval = 5000;
			job.locomotionUrgency = LocomotionUrgency.Walk;
			return job;
		}

		private static Filth FindFilth(Pawn pawn, bool insideHome)
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return null;
			}

			Thing found = GenClosest.ClosestThing_Global_Reachable(
				pawn.Position,
				map,
				map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Filth)),
				PathEndMode.Touch,
				TraverseParms.For(pawn, Danger.Deadly),
				9999f,
				(Thing thing) => ValidFilth(pawn, thing, insideHome));

			return found as Filth;
		}

		private static bool ValidFilth(Pawn pawn, Thing thing, bool insideHome)
		{
			if (!(thing is Filth filth) || !filth.Spawned || filth.Destroyed)
			{
				return false;
			}
			if (filth.IsForbidden(pawn) || filth.Fogged())
			{
				return false;
			}
			if (filth.TicksSinceThickened < MinTicksSinceThickened)
			{
				return false;
			}
			if (filth.Position.Fogged(pawn.Map))
			{
				return false;
			}

			bool inHome = pawn.Map.areaManager.Home[filth.Position];
			if (inHome != insideHome)
			{
				return false;
			}
			if (!pawn.CanReserve(filth))
			{
				return false;
			}
			return pawn.CanReach(filth, PathEndMode.Touch, Danger.Deadly);
		}
	}
}
