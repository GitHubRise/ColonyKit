using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene.Agriculture
{
	/// <summary>施肥：每格消耗 10 个有机肥（DBH JobDriver_PlaceFertilizer 的移植）。</summary>
	public class JobDriver_PlaceFertilizer : JobDriver
	{
		private const int FertilizerPerCell = 10;

		private IntVec3 Cell => job.GetTarget(TargetIndex.A).Cell;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(Cell, job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			Toil getNextIngredient = Toils_General.Label();
			yield return getNextIngredient;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true)
				.FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return Toils_Jump.JumpIf(getNextIngredient, () => !job.GetTargetQueue(TargetIndex.B).NullOrEmpty());

			Toil getNextCell = Toils_General.Label();
			yield return getNextCell;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(60)
				.FailOnForbidden(TargetIndex.A)
				.WithEffect(EffecterDefOf.ConstructDirt, TargetIndex.A)
				.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
				.WithProgressBarToilDelay(TargetIndex.A);

			Toil apply = new Toil
			{
				initAction = () =>
				{
					List<Thing> things = new List<Thing>();
					if (!pawn.CurJob.placedThings.NullOrEmpty())
					{
						things.AddRange(pawn.CurJob.placedThings.Select(p => p.thing));
					}
					else if (pawn.CurJob.GetTarget(TargetIndex.B).Thing is Thing carried)
					{
						things.Add(carried);
					}
					Apply(pawn.CurJob.GetTarget(TargetIndex.A).Cell, things);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return apply;
			yield return Toils_Jump.JumpIf(getNextCell, () => !job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
		}

		private void Apply(IntVec3 cell, List<Thing> things)
		{
			MapComponent_Agriculture comp = MapComponent_Agriculture.Get(Map);
			if (comp == null)
			{
				return;
			}
			int remaining = FertilizerPerCell;
			for (int i = 0; i < things.Count && remaining > 0; i++)
			{
				Thing thing = things[i];
				if (thing == null || thing.Destroyed)
				{
					continue;
				}
				int take = Mathf.Min(remaining, thing.stackCount);
				thing.SplitOff(take).Destroy();
				remaining -= take;
			}
			comp.AddFertilizer(cell);
			// 施完把标记清掉，否则这块地会一直被当成待施肥。
			DesignationDef fert = DefDatabase<DesignationDef>.GetNamedSilentFail("CK_Fertilize");
			if (fert != null)
			{
				Map.designationManager.DesignationAt(cell, fert)?.Delete();
			}
		}
	}
}
