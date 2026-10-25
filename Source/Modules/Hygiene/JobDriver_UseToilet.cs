using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ColonyKit.Modules.Hygiene
{
	public class JobDriver_UseToilet : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
			Toil use = new Toil
			{
				defaultDuration = 900,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			use.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			use.socialMode = RandomSocialMode.Quiet;
			use.initAction = () =>
			{
				if (job.targetA.Thing is Building_Toilet toilet)
				{
					toilet.seatUp = true;
					toilet.occupiedSinceTick = Find.TickManager.TicksGame;
				}
				PrivacyUtil.ToiletPrivacyLOS(pawn);
				SanitationUtil.ApplyBathroomThought(pawn, job.targetA.Thing as Building_AssignableFixture);
			};
			use.tickAction = () =>
			{
				pawn.needs?.TryGetNeed<Need_Bladder>()?.dump();
				pawn.GainComfortFromCellIfPossible(1);
			};
			use.AddFinishAction(() =>
			{
				Building_Toilet toilet = job.targetA.Thing as Building_Toilet;
				if (toilet != null)
				{
					toilet.occupiedSinceTick = -1;
				}
				toilet?.TryUseFlush();
				if (toilet != null && Rand.Chance(0.7f))
				{
					toilet.seatUp = false;
				}
				PrivacyUtil.ToiletPrivacyLOS(pawn);
				SanitationUtil.CheckForBlockage(job.targetA.Thing as Building);
				SanitationUtil.ContaminationFromCellForPawn(pawn, pawn.Position);
				Building_Basin basin = FindBasin(pawn);
				if (basin != null)
				{
					Job wash = JobMaker.MakeJob(
						DefDatabase<JobDef>.GetNamedSilentFail("CK_WashHands"), basin);
					if (wash.TryMakePreToilReservations(pawn, false))
					{
						pawn.jobs.jobQueue.EnqueueLast(wash);
					}
				}
			});
			yield return use;
		}

		private static Building_Basin FindBasin(Pawn pawn)
		{
			Room room = pawn.Position.GetRoom(pawn.Map);
			if (room == null)
			{
				return null;
			}
			foreach (Thing thing in room.ContainedAndAdjacentThings)
			{
				if (thing is Building_Basin basin && basin.CanUse(pawn))
				{
					return basin;
				}
			}
			return null;
		}
	}
}
