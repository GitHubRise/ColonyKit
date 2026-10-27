using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>坑满之前主动清空旱厕（和清化粪池一样挂在 Cleaning 工作类型下）。</summary>
	public class WorkGiver_EmptyLatrine : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest =>
			ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamedSilentFail("CK_PitLatrine"));

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_Latrine latrine))
			{
				return false;
			}
			// 非强制时留出余量（80%），避免刚用过一次就派人；强制时只要坑里还有东西就干。
			float threshold = forced ? Building_Latrine.SewagePerUse : Building_Latrine.SewageLimit * 0.8f;
			if (latrine.sewage < threshold)
			{
				return false;
			}
			if (t.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
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
			return JobMaker.MakeJob(
				DefDatabase<JobDef>.GetNamedSilentFail("CK_EmptyLatrine"), t);
		}
	}
}
