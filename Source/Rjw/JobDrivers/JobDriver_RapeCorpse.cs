#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace rjw
{
	public class JobDriver_ViolateCorpse : JobDriver_Rape
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (RJWSettings.DebugRape) ModLog.Message(" JobDriver_ViolateCorpse::MakeNewToils() called");
			setup_ticks();

			this.FailOnDespawnedNullOrForbidden(iTarget);
			this.FailOn(() => !pawn.CanReserve(Target, 1, 0));  // 若在小人到达前有其他人预定了该囚犯，则失败
			this.FailOn(() => pawn.IsFighting());
			this.FailOn(() => pawn.Drafted);
			this.FailOn(Target.IsBurning);

			if (RJWSettings.DebugRape) ModLog.Message(" JobDriver_ViolateCorpse::MakeNewToils() - moving towards Target");
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			var alert = RJWPreferenceSettings.rape_attempt_alert == RJWPreferenceSettings.RapeAlert.Disabled ? 
				MessageTypeDefOf.SilentInput : MessageTypeDefOf.NeutralEvent;
			Messages.Message(xxx.get_pawnname(pawn) + " is trying to rape a corpse of " + xxx.get_pawnname(Partner), pawn, alert);

			setup_ticks();// 到达时重新设置 tick

			var SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.defaultDuration = duration;
			SexToil.handlingFacing = true;
			SexToil.initAction = delegate
			{
				if (RJWSettings.DebugRape) ModLog.Message(" JobDriver_ViolateCorpse::MakeNewToils() - init");
				if (RJWSettings.rape_stripping)
				{
					if (RJWSettings.DebugRape) ModLog.Message(" JobDriver_ViolateCorpse::MakeNewToils() - stripping Target");
					(Target as Corpse).Strip();
				}
				Start();
			};
			SexToil.tickAction = delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					if (xxx.is_necrophiliac(pawn))
						ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
					else
						ThrowMetaIconF(pawn.Position, pawn.Map, xxx.mote_noheart);
				//if (pawn.IsHashIntervalTick (ticks_between_hits))
				//	roll_to_hit (pawn, Target);
				SexTick(pawn, Target);
				SexUtility.reduce_rest(pawn, 2);
				if (ticks_left <= 0)
					ReadyForNextToil();
			};
			SexToil.AddFinishAction(delegate
			{
				End();
			});
			yield return SexToil;

			yield return new Toil
			{
				initAction = delegate
				{
					if (RJWSettings.DebugRape) ModLog.Message(" JobDriver_ViolateCorpse::MakeNewToils() - creating aftersex toil");
					SexUtility.ProcessSex(Sexprops);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
