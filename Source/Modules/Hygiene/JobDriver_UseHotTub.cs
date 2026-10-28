using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>泡热水浴缸（DBH JobDriver_UseHotTub 的移植）：先注水，再躺进去泡 1800 tick。</summary>
	public class JobDriver_UseHotTub : JobDriver
	{
		private Building_HotTub Tub => job.GetTarget(TargetIndex.A).Thing as Building_HotTub;

		public static IntVec3 GetSeatFor(Pawn pawn, Building_HotTub tub)
		{
			for (int i = 0; i < tub.seatCount; i++)
			{
				if (tub.GetCurOccupant(i) == pawn)
				{
					return tub.GetSleepingSlotPos(i);
				}
			}
			for (int i = 0; i < tub.seatCount; i++)
			{
				if (tub.GetCurOccupant(i) == null)
				{
					return tub.GetSleepingSlotPos(i);
				}
			}
			return tub.GetSleepingSlotPos(0);
		}

		private Toil GotoSeat()
		{
			Toil toil = new Toil();
			toil.initAction = () =>
			{
				Building_HotTub tub = Tub;
				if (tub == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				pawn.pather.StartPath(GetSeatFor(pawn, tub), PathEndMode.OnCell);
			};
			toil.tickAction = () =>
			{
				Building_HotTub tub = Tub;
				if (tub != null && pawn.pather.Moving
					&& tub.GetCurOccupantAt(pawn.pather.Destination.Cell) is Pawn other && other != pawn)
				{
					pawn.pather.StartPath(GetSeatFor(pawn, tub), PathEndMode.OnCell);
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			toil.FailOn(() => Tub == null || !Tub.Working().Accepted);
			return toil;
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Reserve.Reserve(TargetIndex.A, 2, 0);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

			Toil getIn = GotoSeat();
			Toil fill = new Toil
			{
				defaultDuration = 4000,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			fill.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			fill.tickAction = () =>
			{
				Building_HotTub tub = Tub;
				if (tub == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				if (tub.IsFull)
				{
					ReadyForNextToil();
					return;
				}
				if (!tub.TryFillTub())
				{
					EndJobWith(JobCondition.Incompletable);
				}
			};
			fill.JumpIf(() => Tub != null && Tub.IsFull, getIn);
			yield return fill;
			yield return getIn;

			Toil soak = new Toil
			{
				defaultDuration = 1800,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			soak.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			soak.socialMode = RandomSocialMode.SuperActive;
			soak.initAction = () =>
			{
				Map.mapDrawer.MapMeshDirty(Tub.Position, MapMeshFlagDefOf.Buildings);
				pawn.pather.StopDead();
				pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
			};
			soak.tickAction = () =>
			{
				Building_HotTub tub = Tub;
				if (tub == null)
				{
					EndJobWith(JobCondition.Incompletable);
					return;
				}
				tub.occupants.Add(pawn);
				pawn.GainComfortFromCellIfPossible(1);
				pawn.needs?.rest?.TickResting(0.8f);
				SanitationUtil.Hydrotherapy(pawn, tub, healscars: true);
				pawn.needs?.TryGetNeed<Need_Hygiene>()?.clean(tub.cleanedPerTick);
				if (!pawn.IsPrisoner)
				{
					JoyUtility.JoyTickCheckEnd(pawn, 1, JoyTickFullJoyAction.None, 1f, tub);
				}
			};
			soak.AddFinishAction(() =>
			{
				JoyUtility.TryGainRecRoomThought(pawn);
				if (Tub != null)
				{
					Tub.occupants.Remove(pawn);
					SanitationUtil.CheckForBlockage(Tub);
				}
			});
			yield return soak;
		}
	}
}
