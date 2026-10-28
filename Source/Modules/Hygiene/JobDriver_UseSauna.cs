using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>蒸桑拿（DBH JobDriver_UseSauna 的移植）：先到加热器点火，再坐到桑拿房里的座位上。</summary>
	public class JobDriver_UseSauna : JobDriver
	{
		private ThingWithComps Heater => job.GetTarget(TargetIndex.A).Thing as ThingWithComps;

		private Comp_SaunaHeater HeaterComp => Heater?.GetComp<Comp_SaunaHeater>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0, null, errorOnFailed)
				&& pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil stoke = new Toil
			{
				defaultDuration = 60,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			stoke.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			stoke.initAction = () => HeaterComp?.UsedNow();
			stoke.tickAction = () => HeaterComp?.UsedNow();
			yield return stoke;
			Toil sit = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			sit.AddPreTickAction(() => HeaterComp?.UsedNow());
			yield return sit;
			Toil relax = new Toil
			{
				defaultCompleteMode = ToilCompleteMode.Delay,
				defaultDuration = job.def.joyDuration,
				handlingFacing = true
			};
			relax.AddPreTickAction(SaunaTick);
			relax.AddFinishAction(() => JoyUtility.TryGainRecRoomThought(pawn));
			yield return relax;
		}

		private void SaunaTick()
		{
			ThingWithComps heater = Heater;
			if (heater == null)
			{
				EndJobWith(JobCondition.Incompletable);
				return;
			}
			CompRefuelable fuel = heater.TryGetComp<CompRefuelable>();
			CompPowerTrader power = heater.TryGetComp<CompPowerTrader>();
			if ((fuel != null && !fuel.HasFuel) || (power != null && !power.PowerOn))
			{
				EndJobWith(JobCondition.Incompletable);
				return;
			}
			HeaterComp?.UsedNow();
			pawn.needs?.rest?.TickResting(0.8f);
			SanitationUtil.Hydrotherapy(pawn, heater, healscars: true);
			pawn.rotationTracker.FaceCell(TargetA.Cell);
			pawn.GainComfortFromCellIfPossible(1);
			JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.EndJob, 1f, heater as Building);
		}
	}
}
