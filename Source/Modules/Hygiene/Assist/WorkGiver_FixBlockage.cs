using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 清洁工作：找到被 <see cref="CompBlockage"/> 标记为堵塞的洁具，
	/// 派小人去用皮搋子疏通（DBH WorkGiver_FixBlockage 的移植）。
	/// </summary>
	public class WorkGiver_FixBlockage : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(
				ThingRequestGroup.BuildingArtificial))
			{
				if (IsBlocked(thing))
				{
					yield return thing;
				}
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return !AnyBlocked(pawn.Map);
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building building) || !IsBlocked(building))
			{
				return false;
			}
			if (t.Faction != pawn.Faction)
			{
				return false;
			}
			if (t.IsForbidden(pawn))
			{
				return false;
			}
			if (!pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
			{
				return false;
			}
			return !t.IsBurning();
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_ClearBlockage");
			return def == null ? null : JobMaker.MakeJob(def, t);
		}

		private static bool IsBlocked(Thing thing)
		{
			CompBlockage blockage = thing?.TryGetComp<CompBlockage>();
			return blockage != null && blockage.blocked;
		}

		private static bool AnyBlocked(Map map)
		{
			foreach (Thing thing in map.listerThings.ThingsInGroup(
				ThingRequestGroup.BuildingArtificial))
			{
				if (IsBlocked(thing))
				{
					return true;
				}
			}
			return false;
		}
	}
}
