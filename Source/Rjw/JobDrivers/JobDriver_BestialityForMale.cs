#nullable disable

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Multiplayer.API;

namespace rjw
{
	public class JobDriver_BestialityForMale : JobDriver_Rape
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, 1, 0, null, errorOnFailed);
		}

		[SyncMethod]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			//--ModLog.Message(" JobDriver_BestialityForMale::MakeNewToils() called");

			setup_ticks();
			var PartnerJob = xxx.gettin_bred;

			//this.FailOn (() => (!Partner.health.capacities.CanBeAwake) || (!comfort_prisoners.is_designated (Partner)));
			// 若在小人到达前有其他人预定了该囚犯，或殖民者无法到达动物，则失败
			this.FailOn(() => !pawn.CanReserveAndReach(Partner, PathEndMode.Touch, Danger.Deadly));
			this.FailOn(() => Partner.HostileTo(pawn));
			this.FailOnDespawnedNullOrForbidden(iTarget);
			this.FailOn(() => pawn.Drafted);

			yield return Toils_Reserve.Reserve(iTarget, 1, 0);
			yield return Toils_Sex.RapeTargetAlert(pawn, Partner);
			//ModLog.Message(" JobDriver_BestialityForMale::MakeNewToils() - moving towards animal");
			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.Touch);
			yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
			yield return Toils_Interpersonal.GotoInteractablePosition(iTarget);

			if (xxx.is_kind(pawn)
				|| (xxx.CTIsActive && xxx.has_traits(pawn) && pawn.story.traits.HasTrait(xxx.RCT_AnimalLover)))
			{
				yield return TalkToAnimal(pawn, Partner);
				yield return TalkToAnimal(pawn, Partner);
			}

			if (Rand.Chance(0.6f))
				yield return TalkToAnimal(pawn, Partner);

			yield return Toils_Goto.GotoThing(iTarget, PathEndMode.OnCell);

			Toil StartPartnerJob = new Toil();
			StartPartnerJob.defaultCompleteMode = ToilCompleteMode.Instant;
			StartPartnerJob.socialMode = RandomSocialMode.Off;
			StartPartnerJob.initAction = delegate
			{
				//--ModLog.Message(" JobDriver_BestialityForMale::MakeNewToils() - Setting animal job driver");
				var dri = Partner.jobs.curDriver as JobDriver_SexBaseRecieverRaped;
				if (dri == null)
				{
					//野生动物可能逃跑或攻击
					if (pawn.Faction != Partner.Faction && Partner.GetStatValue(StatDefOf.Wildness) > Rand.Range(0.22f, 1.0f)
						&& !(pawn.TicksPerMoveCardinal < (Partner.TicksPerMoveCardinal / 2) && !Partner.Downed && xxx.is_not_dying(Partner)))
					{
						Partner.jobs.StopAll(); // 若动物正在睡觉，将其唤醒。

						float aggro = Partner.kindDef.RaceProps.manhunterOnTameFailChance;
						if (Partner.kindDef.RaceProps.predator)
							aggro += 0.2f;
						else
							aggro -= 0.1f;

						//野生动物可能攻击
						if (Rand.Chance(aggro) && Partner.CanSee(pawn))
						{
							Partner.rotationTracker.FaceTarget(pawn);
							LifeStageUtility.PlayNearestLifestageSound(Partner, (ls) => ls.soundAngry, null,null, 1.4f);
							ThrowMetaIconF(Partner.Position, Partner.Map, FleckDefOf.IncapIcon);
							ThrowMetaIcon(pawn.Position, pawn.Map, ThingDefOf.Mote_ColonistFleeing); //红色 '!'
							Partner.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
							if (Partner.kindDef.RaceProps.herdAnimal && Rand.Chance(0.2f))
							{ // 有 20% 的几率使整群动物转为敌对……
								List<Pawn> packmates = Partner.Map.mapPawns.AllPawnsSpawned.Where(x =>
									x != Partner && x.def == Partner.def && x.Faction == Partner.Faction &&
									x.Position.InHorDistOf(Partner.Position, 24f) && x.CanSee(Partner)).ToList();

								foreach (Pawn packmate in packmates)
								{
									packmate.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
								}
							}
							Messages.Message(pawn.Name.ToStringShort + " is being attacked by " + xxx.get_pawnname(Partner) + ".", pawn, MessageTypeDefOf.ThreatSmall);
						}
						//野生动物可能逃跑
						else
						{
							ThrowMetaIcon(Partner.Position, Partner.Map, ThingDefOf.Mote_ColonistFleeing);
							LifeStageUtility.PlayNearestLifestageSound(Partner, (ls) => ls.soundCall, g => g.soundCall, null);
							Partner.mindState.StartFleeingBecauseOfPawnAction(pawn);
							Partner.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
						}
						pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
					}
					else
					{
						Job gettin_bred = JobMaker.MakeJob(PartnerJob, pawn, Partner);
						Partner.jobs.StartJob(gettin_bred, JobCondition.InterruptForced, null, true);
					}
				}
			};
			yield return StartPartnerJob;

			Toil SexToil = new Toil();
			SexToil.defaultCompleteMode = ToilCompleteMode.Never;
			SexToil.defaultDuration = duration;
			SexToil.handlingFacing = true;
			SexToil.FailOn(() => Partner.CurJob?.def != PartnerJob);
			SexToil.initAction = delegate
			{
				Partner.pather.StopDead();
				Partner.jobs.curDriver.asleep = false;
				Start();
			};
			SexToil.tickAction = delegate
			{
				if (pawn.IsHashIntervalTick(ticks_between_hearts))
					if (xxx.is_zoophile(pawn))
						ThrowMetaIconF(pawn.Position, pawn.Map, FleckDefOf.Heart);
					else
						ThrowMetaIconF(pawn.Position, pawn.Map, xxx.mote_noheart);
				SexTick(pawn, Partner);
				//不要攻击野生动物，免得被其发狂（Manhunter）反杀
				/*
				if (pawn.IsHashIntervalTick (ticks_between_hits))
					roll_to_hit (pawn, Partner);
					*/
				SexUtility.reduce_rest(Partner, 1);
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
					//ModLog.Message(" JobDriver_BestialityForMale::MakeNewToils() - creating aftersex toil");
					SexUtility.ProcessSex(Sexprops);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		[SyncMethod]
		private Toil TalkToAnimal(Pawn pawn, Pawn animal)
		{
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				pawn.interactions.TryInteractWith(animal, SexUtility.AnimalSexChat);
			};
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = Rand.Range(120, 220);
			return toil;
		}
	}
}
