using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 搬运工作：处理「铲除污水」标记的格子（DBH WorkGiver_RemoveSewage 的移植）。
	/// 只扫标记格，不做全图污染扫描。
	/// </summary>
	public class WorkGiver_RemoveSewage : WorkGiver_Scanner
	{
		private const float MinPollution = 0.5f;

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		private static DesignationDef DesDef =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_RemoveSewageDes");

		public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
		{
			DesignationDef def = DesDef;
			if (def == null)
			{
				yield break;
			}
			foreach (Designation designation in pawn.Map.designationManager
				.SpawnedDesignationsOfDef(def))
			{
				yield return designation.target.Cell;
			}
		}

		public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
		{
			DesignationDef def = DesDef;
			if (def == null || pawn.Map.designationManager.DesignationAt(c, def) == null)
			{
				return false;
			}
			MapComponent_SewagePollution pollution =
				pawn.Map.GetComponent<MapComponent_SewagePollution>();
			if (pollution == null || pollution.PollutionAt(c) <= MinPollution)
			{
				return false;
			}
			return pawn.CanReserveAndReach(c, PathEndMode.Touch, pawn.NormalMaxDanger());
		}

		public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
		{
			JobDef def = DefDatabase<JobDef>.GetNamedSilentFail("CK_RemoveSewage");
			return def == null ? null : JobMaker.MakeJob(def, c);
		}
	}
}
