using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Robotics
{
	/// <summary>殖民者维修保姆机器人：耐久不满时可修复。</summary>
	public class WorkGiver_RepairNannyBot : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			if (pawn?.Map == null)
			{
				yield break;
			}
			foreach (Pawn robot in pawn.Map.mapPawns.AllPawnsSpawned)
			{
				if (NannyBotUtility.IsNannyBot(robot))
				{
					yield return robot;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Pawn robot) || !NannyBotUtility.IsNannyBot(robot))
			{
				return false;
			}
			CompNannyBot comp = robot.TryGetComp<CompNannyBot>();
			if (comp == null || comp.Durability >= comp.MaxDurability - 0.5f)
			{
				return false;
			}
			if (robot.IsForbidden(pawn))
			{
				return false;
			}
			return pawn.CanReserveAndReach(robot, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(NannyBotDefOf.CK_RepairNannyBot, t);
		}
	}
}
