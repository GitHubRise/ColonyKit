using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 铲除一格污水：走到格子边铲 120 tick，完工清掉 5 点污染并产出 1 个粪渣
	/// （DBH JobDriver_RemoveSewage 的简化版）。
	/// </summary>
	public class JobDriver_RemoveSewage : JobDriver
	{
		private const TargetIndex CellInd = TargetIndex.A;

		private const int WorkTicks = 120;

		private const float PollutionPerJob = 5f;

		private IntVec3 Cell => job.GetTarget(CellInd).Cell;

		private static DesignationDef Def =>
			DefDatabase<DesignationDef>.GetNamedSilentFail("CK_RemoveSewageDes");

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Cell, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() =>
			{
				DesignationDef def = Def;
				return def == null || pawn.Map.designationManager.DesignationAt(Cell, def) == null;
			});
			yield return Toils_Goto.GotoCell(CellInd, PathEndMode.Touch);

			Toil shovel = Toils_General.Wait(WorkTicks);
			shovel.WithProgressBarToilDelay(CellInd);
			shovel.FailOnCannotTouch(CellInd, PathEndMode.Touch);
			yield return shovel;

			yield return new Toil
			{
				initAction = Finish,
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		private void Finish()
		{
			Map map = pawn.Map;
			if (map == null)
			{
				return;
			}
			IntVec3 cell = Cell;
			DesignationDef def = Def;
			if (def != null)
			{
				map.designationManager.DesignationAt(cell, def)?.Delete();
			}
			map.GetComponent<MapComponent_SewagePollution>()?.ReducePollution(cell, PollutionPerJob);

			ThingDef sludgeDef = DefDatabase<ThingDef>.GetNamedSilentFail("CK_FecalSludge");
			if (sludgeDef == null)
			{
				return;
			}
			Thing sludge = ThingMaker.MakeThing(sludgeDef);
			sludge.stackCount = 1;
			GenPlace.TryPlaceThing(sludge, pawn.Position, map, ThingPlaceMode.Near);
		}
	}
}
