using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>处理「倒掉污水」标记：把刷了 <c>CK_KickOverDes</c> 的粪渣桶倒在地上。</summary>
	public class WorkGiver_KickOver : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

		private static DesignationDef DesDef =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_KickOverDes");

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
			return pawn.CanReserveAndReach(
				t, PathEndMode.ClosestTouch, pawn.NormalMaxDanger(), 1, -1, null, forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_KickOver");
			return def == null ? null : JobMaker.MakeJob(def, t);
		}
	}
}
