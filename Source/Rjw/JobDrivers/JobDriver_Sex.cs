#nullable disable

using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using Multiplayer.API;
using System.Linq;
using System;

namespace rjw
{
	public abstract class JobDriver_Sex : JobDriver
	{

		public readonly TargetIndex iTarget = TargetIndex.A;	//小人或尸体
		public readonly TargetIndex iBed = TargetIndex.B;		//床（将来也许是某种家具？）
		public readonly TargetIndex iCell = TargetIndex.C;		//做爱所在的格/位置（自慰）

		public float satisfaction = 1.0f;

		public bool shouldreserve = true;

		public int stackCount = 0;

		public int ticks_between_hearts = 60;
		public int ticks_between_hits = 60;
		public int ticks_between_thrusts = 60;
		public int ticks_left = 1000;	//toil 的 tick 数
		public int sex_ticks = 1000;	//高潮 tick 数
		public int orgasms = 0;

		// 性爱 tick 会递减直到达到高潮 tick，随后发生高潮，sex_ticks 可能会被重置
		// 性爱持续约 3 秒
		public int orgasmstick = 180; // ~3 秒
		public int orgasmStartTick = 5000;
		public int duration = 5000;

		public bool face2face = false;
		public bool isEndytophile = false; // 由子模组设置
		public bool isAnimalOnAnimal = false;
		public bool shouldGainFocus = false;
		public bool shouldGainFocusP = false;
		public bool isSuccubus = false;
		public bool isSuccubusP = false;

		//开关
		public bool beatings = false;// 开/关切换，也许 TODO？
		public bool beatonce = false;
		public bool neverendingsex = false;

		public Thing Target			// 用于预定
		{
			get
			{
				if (job == null)
				{
					return null;
				}

				if (job.GetTarget(TargetIndex.A).Pawn != null)
					return job.GetTarget(TargetIndex.A).Pawn;

				return job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public Pawn Partner
		{
			get
			{
				if (PartnerPawn != null)
					return PartnerPawn;
				else if (Target is Pawn)
					return job.GetTarget(TargetIndex.A).Pawn;
				else if (Target is Corpse)
					return (job.GetTarget(TargetIndex.A).Thing as Corpse).InnerPawn;
				else
					return null;
			}
		}

		public Building_Bed Bed
		{
			get
			{
				if (pBed != null)
					return pBed;
				else if (job.GetTarget(TargetIndex.B).Thing is Building_Bed)
					return job.GetTarget(TargetIndex.B).Thing as Building_Bed;
				else
					return null;
			}
		}
		//不是床；椅子，将来也许是其他东西
		public Building Building
		{
			get
			{
				if (job.GetTarget(TargetIndex.B).Thing is Building && !(job.GetTarget(TargetIndex.B).Thing is Building_Bed))
					return job.GetTarget(TargetIndex.B).Thing as Building;
				else
					return null;
			}
		}

		public Pawn PartnerPawn = null;
		public Building_Bed pBed = null;
		public SexProps Sexprops = null;

		[SyncMethod]
		public void setup_ticks()
		{
			SetupDurationTicks();
			SetupOrgasmTicks();

			ticks_between_hearts = Rand.RangeInclusive(70, 130);
			ticks_between_hits = Rand.Range(xxx.config.min_ticks_between_hits, xxx.config.max_ticks_between_hits);
			if (xxx.is_bloodlust(pawn))
				ticks_between_hits = (int)(ticks_between_hits * 0.75);
			if (xxx.is_brawler(pawn))
				ticks_between_hits = (int)(ticks_between_hits * 0.90);
			ticks_between_thrusts = 120;


			var EndConditions = this.ConditionsToAbortSex();
			foreach (var condition in EndConditions)
			{
				this.AddFailCondition(condition);
			}
		}

		public void Set_bed(Building_Bed newBed)
		{
			pBed = newBed;
		}

		public float OrgasmProgress {
			get {
				var orgasmDuration = orgasmStartTick - orgasmstick;
				var ticksUntilOrgasm = sex_ticks - orgasmstick;
				
				return 1f - (ticksUntilOrgasm / (float) orgasmDuration);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticks_left, "ticks_left", 0, false);
			Scribe_Values.Look(ref ticks_between_hearts, "ticks_between_hearts", 0, false);
			Scribe_Values.Look(ref ticks_between_hits, "ticks_between_hits", 0, false);
			Scribe_Values.Look(ref ticks_between_thrusts, "ticks_between_thrusts", 0, false);
			Scribe_Values.Look(ref duration, "duration", 0, false);
			Scribe_Values.Look(ref sex_ticks, "sex_ticks", 0, false);
			Scribe_Values.Look(ref orgasms, "orgasms", 0, false);
			Scribe_Values.Look(ref orgasmstick, "orgasmstick", 0, false);
			Scribe_Values.Look(ref orgasmStartTick, "orgasmStartTick", 0, false);

			Scribe_References.Look(ref pBed, "pBed");
			Scribe_References.Look(ref PartnerPawn, "PartnerPawn");
			Scribe_Deep.Look(ref Sexprops, "Sexprops", new SexProps[0]);
			Scribe_Values.Look(ref beatings, "beatings");
			Scribe_Values.Look(ref beatonce, "beatonce");
			Scribe_Values.Look(ref neverendingsex, "neverendingsex");
			Scribe_Values.Look(ref face2face, "face2face");
			Scribe_Values.Look(ref isEndytophile, "isEndytophile");
			Scribe_Values.Look(ref shouldGainFocus, "shouldGainFocus");
			Scribe_Values.Look(ref shouldGainFocusP, "shouldGainFocusP");
			Scribe_Values.Look(ref isSuccubus, "isSuccubus");
			Scribe_Values.Look(ref isSuccubusP, "isSuccubusP");
		}

		public void SexTick(Pawn pawn, Thing target, bool pawnnude = true, bool partnernude = true)
		{
			ticks_left--;
			sex_ticks--;
			Orgasm();

			var partner = target as Pawn;
			if (partner != null)
			{
				if (partner.jobs?.curDriver is JobDriver_SexBaseReciever receiver)//对伙伴执行 tick
				{
					// 群交时只有主发起者推进接受者的计时与高潮进度：
					// 否则 N 个发起者会让一场的时长缩短到 1/N，高潮条也会快 N 倍。
					bool primaryStillPresent = receiver.Sexprops?.pawn != null && receiver.parteners.Contains(receiver.Sexprops.pawn);
					bool advancesReceiver = receiver.Sexprops == null
						|| receiver.Sexprops.pawn == pawn
						|| !primaryStillPresent;

					if (advancesReceiver)
					{
						receiver.ticks_left--;
						receiver.sex_ticks--;
						receiver.Orgasm();
					}
				}

				// 双飞：主发起者同时推进额外接受者的计时与高潮进度。
				if (pawn.jobs?.curDriver is JobDriver_SexBaseInitiator initiator
					&& initiator.extraReceivers != null)
				{
					foreach (Pawn extra in initiator.extraReceivers)
					{
						if (extra?.jobs?.curDriver is JobDriver_SexBaseReciever extraReceiver)
						{
							extraReceiver.ticks_left--;
							extraReceiver.sex_ticks--;
							extraReceiver.Orgasm();
						}
					}

					initiator.GroupSexFlirtTick(partner);
				}

				if (pawn.jobs?.curDriver is JobDriver_SexBaseInitiator)
				{
					var hit = false;
					if (beatonce)
					{
						beatonce = false;
						SexUtility.Sex_Beatings_Dohit(pawn, Partner, Sexprops.isRapist);
					}
					else if (pawn.IsHashIntervalTick(ticks_between_hits))
					{
						Roll_to_hit();
					}
					if (hit)
						if (!isEndytophile)
						{
							SexUtility.DrawNude(pawn);

							if (partner != null)
								SexUtility.DrawNude(partner);
						}
				}
			}

			if (pawn.IsHashIntervalTick(ticks_between_thrusts))
			{
				ChangePsyfocus(pawn, partner);
				Animate(pawn, partner);
				PlaySexSound();
				if (!Sexprops.isRape)
				{
					pawn.GainComfortFromCellIfPossible(ticks_between_thrusts);
					partner?.GainComfortFromCellIfPossible(ticks_between_thrusts);
				}
			}
		}

		/// <summary>
		/// 简单的 rjw 抽送动画
		/// </summary>
		public void Animate(Pawn pawn, Thing target)
		{
			RotatePawns(pawn, Partner);
			//攻击/骑乘 1x2 格的飞机杯/假阳具？
			//if (Building != null)
			//	target = Building;
			if (target != null)
			{
				pawn.Drawer.Notify_MeleeAttackOn(target);

				var partner = target as Pawn;
				if (partner != null && !Sexprops.isRapist)
					partner.Drawer.Notify_MeleeAttackOn(pawn);


				//在攻击与 Notify_MeleeAttackOn 之后刷新 DrawNude
				// 恋衣癖偏好穿着衣服做爱，其他人则裸体。
				if (!isEndytophile)
				{
					SexUtility.DrawNude(pawn);

					if (partner != null)
						SexUtility.DrawNude(partner);
				}
			}
			else
			{
				//在攻击与 Notify_MeleeAttackOn 之后刷新 DrawNude
				// 恋衣癖偏好穿着衣服做爱，其他人则裸体。
				if (!isEndytophile)
				{
					SexUtility.DrawNude(pawn);
				}
			}
		}

		/// <summary>
		/// 通过做爱增加精神力
		/// </summary>
		public void ChangePsyfocus(Pawn pawn, Thing target)
		{
			if (ModsConfig.RoyaltyActive)
			{
				if (pawn.jobs?.curDriver is JobDriver_ViolateCorpse)
					if (xxx.is_necrophiliac(pawn) && MeditationFocusTypeAvailabilityCache.PawnCanUse(pawn, DefDatabase<MeditationFocusDef>.GetNamedSilentFail("Morbid")))
					{
						SexUtility.OffsetPsyfocus(pawn, 0.01f);
					}

				if (target != null)
				{
					var partner = target as Pawn;
					if (partner != null)
					{
						if (shouldGainFocus)
							SexUtility.OffsetPsyfocus(pawn, 0.01f);

						if (shouldGainFocusP)
							SexUtility.OffsetPsyfocus(partner, 0.01f);

						if (isSuccubus)
							SexUtility.OffsetPsyfocus(pawn, 0.01f);

						if (isSuccubusP)
							SexUtility.OffsetPsyfocus(partner, 0.01f);
					}
				}
			}
		}

		/// <summary>
		/// 旋转小人
		/// </summary>
		public void RotatePawns(Pawn pawn, Thing target)
		{
			if (Building != null)
			{
				if (face2face)
					pawn.Rotation = Building.Rotation.Opposite;
				else
					pawn.Rotation = Building.Rotation;

				return;
			}
			if (target == null) // 单人
			{
				//pawn.Rotation = Rot4.South;
				return;
			}

			if (target is not Pawn partner || partner.Dead) // 恋尸
			{
				pawn.rotationTracker.Face(target.DrawPos);
				return;
			}

			// 群交时只让「主发起者」控制接受者的朝向，其他参与者转向接受者，
			// 避免多人同时写 rotationTracker 造成抖动。
			if (partner.jobs?.curDriver is JobDriver_SexBaseReciever receiverDriver
				&& receiverDriver.parteners.Count > 1
				&& receiverDriver.Partner != pawn)
			{
				pawn.rotationTracker.Face(partner.DrawPos);
				return;
			}

			//也许可以为怪物娘加个手部检查，不过无所谓
			//bool partnerHasHands = Receiver.health.hediffSet.GetNotMissingParts().Any(part => part.IsInGroup(BodyPartGroupDefOf.RightHand) || part.IsInGroup(BodyPartGroupDefOf.LeftHand));

			// 大多数动物性交大概是后入式。
			if (isAnimalOnAnimal)
			{
				if (Sexprops.sexType == xxx.rjwSextype.Anal || Sexprops.sexType == xxx.rjwSextype.Vaginal || Sexprops.sexType == xxx.rjwSextype.DoublePenetration)
				{
					//>>
					//ModLog.Message("animal doggy");
					pawn.rotationTracker.Face(partner.DrawPos);
					partner.Rotation = pawn.Rotation;
				}
				else
				{
					//><
					//ModLog.Message("animal non doggy");
					pawn.rotationTracker.Face(target.DrawPos);
					partner.rotationTracker.Face(pawn.DrawPos);
				}
			}
			else
			{
				if (this is JobDriver_BestialityForFemale)
				{
					if (Sexprops.sexType == xxx.rjwSextype.Anal || Sexprops.sexType == xxx.rjwSextype.Vaginal || Sexprops.sexType == xxx.rjwSextype.DoublePenetration)
					{
						//<<
						//ModLog.Message("bestialityFF doggy");
						partner.rotationTracker.Face(pawn.DrawPos);
						pawn.Rotation = partner.Rotation;
					}
					else
					{
						//><
						//ModLog.Message("bestialityFF non doggy");
						pawn.rotationTracker.Face(target.DrawPos);
						partner.rotationTracker.Face(pawn.DrawPos);
					}
				}
				else if (partner.GetPosture() == PawnPosture.LayingInBed)
				{
					//x^
					//ModLog.Message("loving/casualsex in bed");
					// 如果能实现女上位/反向女上位以及谁插入谁，这里可以有更好的处理
					//在那之前……

					if (!face2face && Sexprops.sexType == xxx.rjwSextype.Anal ||
										Sexprops.sexType == xxx.rjwSextype.Vaginal ||
										Sexprops.sexType == xxx.rjwSextype.DoublePenetration ||
										Sexprops.sexType == xxx.rjwSextype.Fisting)
						//if (xxx.is_female(pawn) && xxx.is_female(partner))
					{
						// 床上做爱，面朝下
						pawn.Rotation = partner.CurrentBed().Rotation.Opposite;
					}
					//else if (!(xxx.is_male(pawn) && xxx.is_male(partner)))
					//{
					//	// 床上做爱，面朝下
					//	pawn.Rotation = partner.CurrentBed().Rotation.Opposite;
					//}
					else
					{
						// 床上做爱，面朝上
						pawn.Rotation = partner.CurrentBed().Rotation;
					}
				}
				// 无论如何都有 30% 的几率面对面，以增加变化。
				else if (!face2face && (Sexprops.sexType == xxx.rjwSextype.Anal ||
										Sexprops.sexType == xxx.rjwSextype.Vaginal ||
										Sexprops.sexType == xxx.rjwSextype.DoublePenetration ||
										Sexprops.sexType == xxx.rjwSextype.Fisting))
				{
					//>>
					//ModLog.Message("doggy");
					pawn.rotationTracker.Face(target.DrawPos);
					partner.Rotation = pawn.Rotation;
				}
				// 非后入式，或无论如何面对面
				else
				{
					//><
					//ModLog.Message("non doggy");
					pawn.rotationTracker.Face(target.DrawPos);
					partner.rotationTracker.Face(pawn.DrawPos);
				}
			}
		}

		[SyncMethod]
		public void Rollface2face(float chance = 0.3f)
		{
			Setface2face(Rand.Chance(chance));
		}
		public void Setface2face(bool chance)
		{
			face2face = chance;
		}

		public void Roll_to_hit()
		{
			if (beatings || (Sexprops.isRapist && RJWSettings.rape_beating))
				SexUtility.Sex_Beatings(Sexprops);
		}
		public void ThrowMetaIcon(IntVec3 pos, Map map, ThingDef icon)
		{
			MoteMaker.MakeStaticMote(pos, map, icon);
		}
		public void ThrowMetaIconF(IntVec3 pos, Map map, FleckDef icon)
		{
			FleckMaker.ThrowMetaIcon(pos, map, icon);
		}
		public void PlaySexSound()
		{
			if (RJWSettings.sounds_enabled)
			{
				SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
				sound.volumeFactor = RJWSettings.sounds_sex_volume;
				if(isAnimalOnAnimal)
					sound.volumeFactor *= RJWSettings.sounds_animal_on_animal_volume;
				SoundDef.Named("Sex").PlayOneShot(sound);
			}
		}
		public void PlayCumSound()
		{
			if (RJWSettings.sounds_enabled)
			{
				SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
				sound.volumeFactor = RJWSettings.sounds_cum_volume;
				if (isAnimalOnAnimal)
					sound.volumeFactor *= RJWSettings.sounds_animal_on_animal_volume;
				SoundDef.Named("Cum").PlayOneShot(sound);
			}
		}
		public void PlaySexVoice(){}
		public void PlayOrgasmVoice(){}

		public void Orgasm()
		{
			if (sex_ticks > orgasmstick) //速度 1 时约 3 秒
			{
				return;
			}
			orgasms++;
			// Sexprops 常被传给其他模组，以便之后评估想法、模因等。
			// 为保持稳定，保留了原有的高潮计数器
			Sexprops.orgasms ++;

			PlayCumSound();
			PlayOrgasmVoice();
			
			if (!Sexprops.usedCondom)
			{
				//把精液施加到地板上：
				SexUtility.CumFilthGenerator(pawn, Sexprops);
				if (Partner != null && !pawn.Dead && !Partner.Dead)
				{
					//TODO：哪天把这烂摊子修好
					PregnancyHelper.impregnate(Sexprops);
					SexUtility.TransferFluids(Sexprops);
				}
			}
			CalculateSatisfactionPerTick();

			SexUtility.SatisfyPersonal(Sexprops, satisfaction);

			SetupOrgasmTicks();

			if (neverendingsex)
				ticks_left = duration;
		}

		[SyncMethod]
		protected virtual void SetupDurationTicks()
		{
			ticks_left = (int)(2000.0f * Rand.Range(0.50f, 0.90f));
			duration = ticks_left;
		}

		[SyncMethod]
		protected virtual void SetupOrgasmTicks()
		{
			float need;
			if (xxx.is_human(pawn)) {
				need = 1.0f + xxx.need_some_sex(pawn); //1-4
			} else {
				need = 1.0f;
			}

			orgasmstick = 180;
			sex_ticks = (int)((duration / need) * Rand.Range(0.75f, 0.90f));
			orgasmStartTick = sex_ticks;
		}

		public void CalculateSatisfactionPerTick()
		{
			satisfaction = 0.4f;
		}

		public static bool IsInOrByBed(Building_Bed b, Pawn p)
		{
			for (int i = 0; i < b.SleepingSlotsCount; i++)
			{
				if (b.GetSleepingSlotPos(i).InHorDistOf(p.Position, 1f))
				{
					return true;
				}
			}
			return false;
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true; // 无需预定。
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			return null;
		}

		/// <summary>
		/// 提供一组用于"和平"中止性爱的谓词，
		/// 即它们不会再执行 SatisfyPersonal 或 TransferFluid 的后续阶段。
		/// 当前检查项：
		/// - 生殖器数量发生变化（例如某个生殖器被摧毁）
		/// - 小人原本未死，但现在死亡
		/// - 小人原本未倒地，但现在倒地
		/// - 伙伴的生殖器数量发生变化
		/// - 伙伴原本未死，但现在死亡
		/// - 伙伴原本未倒地，但现在倒地
		/// </summary>
		/// <returns></returns>
		public List<Func<bool>> ConditionsToAbortSex()
		{
			var EndConditions = new List<Func<bool>>();

			int pawnInitialGenitals = Genital_Helper.get_AllPartsHediffList(this.pawn).Count;

			Func<bool> EndIfPawnGenitalsWentMissing =
				() => {
					int foundPawnGenitals = Genital_Helper.get_AllPartsHediffList(this.pawn).Count;
					return foundPawnGenitals != pawnInitialGenitals;
				};
			EndConditions.Add(EndIfPawnGenitalsWentMissing);

			bool pawnInitiallyDead = this.pawn.Dead;
			Func<bool> EndIfPawnDied =
				() =>!pawnInitiallyDead && pawn.Dead;
			EndConditions.Add(EndIfPawnDied);

			bool pawnInitiallyDowned = this.pawn.Downed;
			Func<bool> EndIfPawnDowned =
				() => !pawnInitiallyDowned && pawn.Downed;
			EndConditions.Add(EndIfPawnDowned);

			if (Partner != null)
			{
				int partnerInitialGenitals = Genital_Helper.get_AllPartsHediffList(this.Partner).Count;

				Func<bool> EndIfPartnerGenitalsWentMissing =
					() => {
						int foundPartnerGenitals = Genital_Helper.get_AllPartsHediffList(this.Partner).Count;
						return foundPartnerGenitals != partnerInitialGenitals ;
					};
				EndConditions.Add(EndIfPartnerGenitalsWentMissing);

				bool partnerInitiallyDead = this.Partner.Dead;
				Func<bool> EndIfPartnerDied =
					() => !partnerInitiallyDead && pawn.Dead;
				EndConditions.Add(EndIfPawnDied);

				bool partnerInitiallyDowned = this.Partner.Downed;
				Func<bool> EndIfPartnerDowned =
					() => !partnerInitiallyDowned && Partner.Downed;
				EndConditions.Add(EndIfPartnerDowned);
			}

			return EndConditions;
		}
	}
}
