using System.Collections.Generic;
using ColonyKit.Modules.Hygiene.Dbh;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>处理「排空水箱」标记：把带 <see cref="CompWaterStorage"/> 的设施排空。</summary>
	public class WorkGiver_DrainWater : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

		private static DesignationDef DesDef =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_DrainTankDes");

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			DesignationDef def = DesDef;
			if (def == null)
			{
				yield break;
			}
			foreach (Designation designation in pawn.Map.designationManager
				.SpawnedDesignationsOfDef(def))
			{
				Thing thing = designation.target.Thing;
				if (thing != null)
				{
					yield return thing;
				}
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			DesignationDef def = DesDef;
			if (def == null || pawn.Map.designationManager.DesignationOn(t, def) == null)
			{
				return false;
			}
			CompWaterStorage storage = t.TryGetComp<CompWaterStorage>();
			if (storage == null || storage.WaterStorage <= 0.01f)
			{
				return false;
			}
			return pawn.CanReserveAndReach(
				t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_DrainWater");
			return def == null ? null : JobMaker.MakeJob(def, t);
		}
	}
}
