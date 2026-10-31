using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 给卧床病人/婴儿洗澡（DBH JobDriver_washPatient 的精简版）：
	/// 先到水源（TargetB，可以是取水点/水井/水箱，也可以是岸边水格）打水，
	/// 再到病人身边擦洗 800 tick，每 tick 提升 0.002 卫生值，洗完后清掉身上的污染。
	/// </summary>
	public class JobDriver_WashPatient : JobDriver
	{
		private const TargetIndex PatientInd = TargetIndex.A;

		private const TargetIndex SourceInd = TargetIndex.B;

		private const int WashDuration = 800;

		private const float CleanPerTick = 0.002f;

		private Pawn Patient => job.GetTarget(PatientInd).Thing as Pawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.Reserve(job.GetTarget(PatientInd), job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			LocalTargetInfo source = job.GetTarget(SourceInd);
			if (!source.IsValid)
			{
				return true;
			}
			// 水源可能是建筑，也可能只是岸边可站格（地表水），两种都交给 Reserve 处理。
			return pawn.Reserve(source, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(PatientInd);
			this.FailOn(() => !WorkGiver_WashPatient.ShouldBeWashedBySomeone(Patient));

			LocalTargetInfo source = job.GetTarget(SourceInd);
			if (source.IsValid)
			{
				Toil gotoSource = source.HasThing
					? Toils_Goto.GotoThing(SourceInd, PathEndMode.Touch)
					: Toils_Goto.GotoCell(SourceInd, PathEndMode.OnCell);
				gotoSource.FailOnForbidden(SourceInd);
				yield return gotoSource;
			}

			yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);

			Toil wash = ToilMaker.MakeToil("CK_WashPatient");
			wash.defaultDuration = WashDuration;
			wash.defaultCompleteMode = ToilCompleteMode.Delay;
			wash.socialMode = RandomSocialMode.Quiet;
			wash.FailOnDestroyedNullOrForbidden(PatientInd);
			wash.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
			wash.WithProgressBar(PatientInd, () =>
			{
				Need_Hygiene need = Patient?.needs?.TryGetNeed<Need_Hygiene>();
				return need?.CurLevel ?? 1f;
			});
			EffecterDef washing = DefDatabase<EffecterDef>.GetNamedSilentFail("CK_WashingEffect");
			if (washing != null)
			{
				wash.WithEffect(washing, PatientInd);
			}
			wash.AddEndCondition(() =>
			{
				Need_Hygiene need = Patient?.needs?.TryGetNeed<Need_Hygiene>();
				if (need == null)
				{
					return JobCondition.Incompletable;
				}
				return need.CurLevel < 1f ? JobCondition.Ongoing : JobCondition.Succeeded;
			});
			wash.tickAction = () =>
			{
				Patient?.needs?.TryGetNeed<Need_Hygiene>()?.clean(CleanPerTick);
			};
			wash.AddFinishAction(() =>
			{
				Patient?.needs?.TryGetNeed<Need_Hygiene>()?.CleanContamination();
			});
			yield return wash;
		}
	}
}
