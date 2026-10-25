#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Multiplayer.API;
using rjw.Modules.Interactions;

namespace rjw
{
	public class SexUtility
	{
		private const float base_sat_per_fuck = 0.40f;

		public static readonly InteractionDef AnimalSexChat = DefDatabase<InteractionDef>.GetNamed("AnimalSexChat");

		public static readonly List<InteractionDef> SexInteractions = DefDatabase<InteractionDef>.AllDefsListForReading.Where(x => x.HasModExtension<SexInteractionExtension>()).ToList();

		/// <summary>
		/// 由多个工作调用的警报检查器。检查小人关系，以及是否应发出警报。<br/>
		/// 左上角通知<br/>
		/// 强暴尝试
		/// </summary>
		public static void RapeTargetAlert(Pawn rapist, Pawn target)
		{
			MessageTypeDef messageType = GetAlertMessageType(rapist, target, RJWPreferenceSettings.rape_attempt_alert);

			if (messageType == null)
			{
				return;
			}

			string messageKey;

			if (xxx.is_mechanoid(rapist))
			{
				messageKey = "RJW_AlertAssault";
			}
			else if (xxx.is_animal(rapist) || xxx.is_animal(target))
			{
				messageKey = "RJW_AlertBreed";
			}
			else
			{
				messageKey = "RJW_AlertRape";
			}

			PawnRelationDef relation = rapist.GetMostImportantRelation(target);
			TaggedString message;

			if (relation == null)
			{
				message = messageKey.Translate(
					rapist.Named("RAPIST"),
					target.Named("TARGET")
				);
			}
			else
			{
				messageKey += "Related";

				message = messageKey.Translate(
					rapist.Named("RAPIST"),
					target.Named("TARGET"),
					relation.GetGenderSpecificLabel(target).Named("RELATION")
				);
			}

			Messages.Message(message, rapist, messageType);
		}

		/// <summary>
		/// 由多个工作调用的警报检查器。<br/>
		/// 左上角通知<br/>
		/// 强暴开始
		/// </summary>
		public static void BeeingRapedAlert(Pawn rapist, Pawn target)
		{
			MessageTypeDef messageType = GetAlertMessageType(rapist, target, RJWPreferenceSettings.rape_alert);

			if (messageType == null)
			{
				return;
			}

			Messages.Message("RJW_AlertBeeingRaped".Translate(target.Named("TARGET")), target, messageType);
		}

		/// <summary>
		/// <para>
		/// 获取在给定情形下应按用户偏好发出的消息类型。
		/// </para>
		/// <para>
		/// 可返回 null，表示应抑制该消息。
		/// </para>
		/// </summary>
		private static MessageTypeDef GetAlertMessageType(Pawn rapist, Pawn target, RJWPreferenceSettings.RapeAlert alert)
		{
			if (target.IsDesignatedComfort())
			{
				Type jobDriverType = rapist.jobs.curDriver.GetType();

				if (jobDriverType == typeof(JobDriver_RapeComfortPawn) && !RJWPreferenceSettings.ShowForCP)
				{
					return null;
				}

				if (jobDriverType == typeof(JobDriver_Breeding) &&
					!RJWPreferenceSettings.ShowForBreeding &&
					target.IsDesignatedBreeding())
				{
					return null;
				}
			}

			switch (alert)
			{
				case RJWPreferenceSettings.RapeAlert.Enabled:
					break;

				case RJWPreferenceSettings.RapeAlert.Humanlikes:
					if (!xxx.is_human(target))
						return null;
					break;

				case RJWPreferenceSettings.RapeAlert.Colonists:
					if (!target.IsColonist)
						return null;
					break;

				case RJWPreferenceSettings.RapeAlert.Silent:
					return MessageTypeDefOf.SilentInput;

				default:
					return null;
			}

			return MessageTypeDefOf.NegativeEvent;
		}

		/// <summary>
		/// 检查强暴者与受害者是否属于不同派系，若是则降低双方好感度。 
		/// 若受害者属于隐藏派系、已死亡或已对强暴者敌对，则提前返回。 
		/// </summary>
		public static void Notify_MemberRaped(Pawn rapist, Pawn victim)
		{
			Faction rapistFaction = rapist?.Faction;
			Faction victimFaction = victim?.Faction;

			if (rapistFaction is null || victimFaction is null || rapistFaction == victimFaction)
			{
				return;
			}

			if (victimFaction.Hidden || victim.Dead || victimFaction.RelationKindWith(rapistFaction) is FactionRelationKind.Hostile)
			{
				return;
			}

			int goodwillReduction = -40;

			rapistFaction.TryAffectGoodwillWith(
				other: victimFaction,
				goodwillChange: goodwillReduction,
				canSendMessage: true,
				canSendHostilityLetter: true,
				reason: RJWHistoryEventDefOf.RJW_MemberRaped,
				lookTarget: victim
			);
		}

		// 按名称返回身体部位的快捷方法。用于检查小人是否拥有特定身体部位等。
		public static BodyPartRecord GetPawnBodyPart(Pawn pawn, string bodyPart)
		{
			return pawn.RaceProps.body.AllParts.Find(x => x.def == DefDatabase<BodyPartDef>.GetNamed(bodyPart));
		}

		public static float GetPartFilthAmount(HediffComp_SexPart part)
		{
			return ((part.GetBodySize() / 3f) + (2f / 3f)) * part.GetFluidMultiplier();
		}

		public static float GetFilthAmount(HediffComp_SexPart part, SexProps props)
		{
			return GetPartFilthAmount(part);
		}
		public static void CumFilthGenerator(Pawn pawn, SexProps props)
		{
			if (pawn == null) return;
			if (pawn.Dead) return;
			if (xxx.is_slime(pawn)) return;
			if (!RJWSettings.cum_filth) return;

			var partsProducingFluid = pawn.GetGenitalsList().OfType<ISexPartHediff>().Where(part => part.Def.produceFluidOnOrgasm);

			foreach (var part in partsProducingFluid)
			{
				HediffDef_SexPart def = part.Def;
				HediffComp_SexPart comp = part.GetPartComp();
				SexFluidDef fluid = comp.Fluid;
				if (fluid?.filth == null)
				{
					continue;
				}

				int filthCount = Math.Min(45, (int)Math.Round(RJWSettings.cum_filth_multiplier * GetFilthAmount(comp, props)));
				if (fluid.alwaysProduceFilth && filthCount < 1)
				{
					filthCount = 1;
				}

				//ModLog.Message("CumFilthGenerator: " + xxx.get_pawnname(pawn) + ", " + RJWSettings.cum_filth_multiplier + ", " + filthCount);

				FilthMaker.TryMakeFilth(pawn.PositionHeld, pawn.MapHeld, fluid.filth, pawn.LabelIndefinite(), filthCount, FilthSourceFlags.Pawn);
			}
		}

		// 小人自慰后可能会也可能不会清理狼藉。
		[SyncMethod]
		public static bool ConsiderCleaning(Pawn fapper)
		{
			if (!RJWSettings.cum_filth) return false;
			if (!xxx.has_traits(fapper) || fapper.story == null) return false;
			if (fapper.WorkTagIsDisabled(WorkTags.Cleaning)) return false;

			float do_cleaning = 0.5f; // 50%

			if (!fapper.PositionHeld.Roofed(fapper.Map))
				do_cleaning -= 0.25f; // 在室外时更不容易清理。

			if (xxx.CTIsActive && fapper.story.traits.HasTrait(xxx.RCT_NeatFreak))
				do_cleaning += 1.00f;

			switch (fapper.needs?.rest?.CurCategory)
			{
				case RestCategory.Exhausted:
					do_cleaning -= 0.5f;
					break;
				case RestCategory.VeryTired:
					do_cleaning -= 0.3f;
					break;
				case RestCategory.Tired:
					do_cleaning -= 0.1f;
					break;
				case RestCategory.Rested:
					do_cleaning += 0.3f;
					break;
			}

			if (fapper.story.traits.DegreeOfTrait(VanillaTraitDefOf.NaturalMood) == -2) // 抑郁
				do_cleaning -= 0.3f;
			if (fapper.story.traits.DegreeOfTrait(TraitDefOf.Industriousness) == 2) // 勤劳
				do_cleaning += 1.0f;
			else if (fapper.story.traits.DegreeOfTrait(TraitDefOf.Industriousness) == 1) // 勤奋
				do_cleaning += 0.5f;
			else if (fapper.story.traits.DegreeOfTrait(TraitDefOf.Industriousness) == -1) // 懒惰
				do_cleaning -= 0.5f;
			else if (fapper.story.traits.DegreeOfTrait(TraitDefOf.Industriousness) == -2) // 懒散
				do_cleaning -= 1.0f;

			if (xxx.is_ascetic(fapper))
				do_cleaning += 0.2f;

			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			return Rand.Chance(do_cleaning);
		}

		/// <summary>处理做爱后特质与想法的获取，以及液体的生成。行为的发起者（妓女、强暴者、女性兽交者等）应放在第一位。</summary>
		[SyncMethod]
		public static void Aftersex(SexProps props, bool applyReceiverSide = true)
		{
			if (props.sexType == xxx.rjwSextype.Masturbation)
			{
				AfterMasturbation(props);
				return;
			}

			bool bothInMap = false;

			if (!props.partner.Dead)
				bothInMap = props.pawn.Map != null && props.partner.Map != null; //由 Hoge 添加。对已消失的小人调用此函数时为 false：用于被绑架者之类的背景强暴

			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			if (bothInMap)
			{
				//兜底计时器增加，确保小人不会卡在重复工作中。
				if (!props.isCoreLovin)
				{
					props.pawn.rotationTracker.Face(props.partner.DrawPos);
					props.pawn.rotationTracker.FaceCell(props.partner.Position);
				}
				if (!props.partner.Dead)
				{
					if (!props.isCoreLovin)
					{
						props.partner.rotationTracker.Face(props.pawn.DrawPos);
						props.partner.rotationTracker.FaceCell(props.pawn.Position);
					}
					if (RJWSettings.sounds_enabled)
					{
						if (props.isRape)
						{
							if (Rand.Value > 0.30f)
								LifeStageUtility.PlayNearestLifestageSound(props.partner, (ls) => ls.soundAngry, null, null, 1.2f);
							else
								LifeStageUtility.PlayNearestLifestageSound(props.partner, (ls) => ls.soundCall, g => g.soundCall, null, 1.2f);

							props.pawn.Drawer.Notify_MeleeAttackOn(props.partner);
							props.partner.stances.stagger.StaggerFor(Rand.Range(10, 300));
						}
						else
							LifeStageUtility.PlayNearestLifestageSound(props.partner, (ls) => ls.soundCall, g => g.soundCall,null);
					}
					if (props.sexType == xxx.rjwSextype.Vaginal || props.sexType == xxx.rjwSextype.DoublePenetration)
						if (xxx.is_Virgin(props.partner))
						{
							//TODO：将童贞绑定到小人的各部位
							/*
							string thingdef_penis_name = Genital_Helper.get_penis_all(pawn)?.def.defName ?? "";
							ThingDef thingdef_penis = null;

							ModLog.Message("SexUtility::thingdef_penis_name " + thingdef_penis_name);
							ModLog.Message("SexUtility::thingdef_penis 1 " + thingdef_penis);

							if (thingdef_penis_name != "")
								thingdef_penis = (from x in DefDatabase<ThingDef>.AllDefs where x.defName == thingdef_penis_name select x).RandomElement();
							ModLog.Message("SexUtility::thingdef_penis 2 " + thingdef_penis);

							partner.TakeDamage(new DamageInfo(DamageDefOf.Stab, 1, 999, -1.0f, null, xxx.genitals, thingdef_penis));
							*/
						}
				}

				if (RJWSettings.sounds_enabled && props.isCoreLovin)
					SoundDef.Named("Cum").PlayOneShot(!props.partner.Dead
						? new TargetInfo(props.partner.Position, props.pawn.Map)
						: new TargetInfo(props.pawn.Position, props.pawn.Map));

				if (props.isRape)
				{
					if (Rand.Value > 0.30f)
						LifeStageUtility.PlayNearestLifestageSound(props.pawn, (ls) => ls.soundAngry, null,null, 1.2f);
					else
						LifeStageUtility.PlayNearestLifestageSound(props.pawn, (ls) => ls.soundCall, g => g.soundCall,null, 1.2f);
				}
				else
					LifeStageUtility.PlayNearestLifestageSound(props.pawn, (ls) => ls.soundCall, g => g.soundCall, null);
			}

			if (props.usedCondom)
			{
				if (CondomUtility.UsedCondom != null)
					GenSpawn.Spawn(CondomUtility.UsedCondom, props.pawn.Position, props.pawn.Map);
				CondomUtility.useCondom(props.pawn);
				CondomUtility.useCondom(props.partner);
			}
			else
			{
				if (props.isCoreLovin) // 非核心部分由工作驱动处理
				{
					//将精液洒到地面：
					CumFilthGenerator(props.pawn, props);
					if (applyReceiverSide)
					{
						CumFilthGenerator(props.partner, props);

						PregnancyHelper.impregnate(props);
						TransferNutritionCore(props);
					}
				}
			}

			List<Trait> newInitiatorTraits = new(), newReceiverTraits = new();

			if (applyReceiverSide && props.isRape && !props.partner.Dead)
				AfterSexUtility.processBrokenPawn(props.partner, newReceiverTraits);

			//Satisfy(pawn, partner, sextype, rape);

			//TODO：下面这段一团糟，以后收拾一下
			AfterSexUtility.UpdateRecords(props, applyReceiverSide);
			GiveTraits(props, newInitiatorTraits);
			if (applyReceiverSide)
				GiveTraits(props.GetForPartner(), newReceiverTraits);

			if (RJWSettings.sendTraitGainLetters)
			{
				SendTraitGainLetter(props.pawn, newInitiatorTraits);
				if (applyReceiverSide)
					SendTraitGainLetter(props.partner, newReceiverTraits);
			}
		}

		// <summary>单人行为。</summary>
		public static void AfterMasturbation(SexProps props)
		{
			IncreaseTicksToNextLovin(props.pawn);

			//将精液洒到地面：
			CumFilthGenerator(props.pawn, props);

			AfterSexUtility.UpdateRecords(props);

			// 单人行为不获得特质。若以后添加了特质再启用。（窥视癖？）
			//check_trait_gain(pawn);
		}

		// 将外星人寿命换算为人类年龄。 
		// 有些外星人的寿命配置有问题，可在此手动修正。
		public static int ScaleToHumanAge(Pawn pawn, int humanLifespan = 80)
		{
			float pawnAge = pawn.ageTracker.AgeBiologicalYearsFloat;
			float pawnLifespan = pawn.RaceProps.lifeExpectancy;

			if (pawn.def.defName == "Human") return (int)pawnAge; // 人类，无需换算。

			// 外星种族，全都有问题，需要修正。
			if (pawn.def.defName.ContainsAny("Alien_Sergal", "Alien_SergalNME", "Alien_Xenn", "Alien_Racc", "Alien_Ferrex", "Alien_Wolvx", "Alien_Frijjid", "Alien_Fennex") && pawnLifespan >= 2000f)
			{
				pawnAge = Math.Min(pawnAge, 80f); // 限制为 80。
				pawnLifespan = 80f;
			}
			if (pawn.def.defName.ContainsAny("Alien_Gnoll", "Alien_StripedGnoll") && pawnLifespan >= 2000f)
			{
				pawnAge = Math.Min(pawnAge, 60f); // 限制为 60。
				pawnLifespan = 60f; // 比人类成熟得更快。
			}

			// 永生种族，成熟速度与人类相近。
			if (pawn.def.defName.ContainsAny("LF_Dragonia", "LotRE_ElfStandardRace", "Alien_Crystalloid", "Alien_CrystalValkyrie"))
			{
				pawnAge = Math.Min(pawnAge, 40f); // 限制为 40 - 永远不会“变老”。
				pawnLifespan = 80f;
			}

			float age_scaling = humanLifespan / pawnLifespan;
			float scaled_age = pawnAge * age_scaling;

			if (scaled_age < 1)
				scaled_age = 1;

			return (int)scaled_age;
		}

		// 用于复杂的受孕计算。部位相似的小人/动物兼容性更好。
		public static float BodySimilarity(Pawn pawn, Pawn partner)
		{
			float size_adjustment = Mathf.Lerp(0.3f, 1.05f, 1.0f - Math.Abs(pawn.BodySize - partner.BodySize));

			//ModLog.Message(" Size adjustment: " + size_adjustment);

			List<BodyPartDef> pawn_partlist = new List<BodyPartDef> { };
			List<BodyPartDef> pawn_mismatched = new List<BodyPartDef> { };
			List<BodyPartDef> partner_mismatched = new List<BodyPartDef> { };

			//ModLog.Message("Checking compatibility for " + xxx.get_pawnname(pawn) + " and " + xxx.get_pawnname(partner));
			bool pawnHasHands = pawn.health.hediffSet.GetNotMissingParts().Any(part => part.IsInGroup(BodyPartGroupDefOf.RightHand) || part.IsInGroup(BodyPartGroupDefOf.LeftHand));

			foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
			{
				pawn_partlist.Add(part.def);
			}
			float pawn_count = pawn_partlist.Count();

			foreach (BodyPartRecord part in partner.RaceProps.body.AllParts)
			{
				partner_mismatched.Add(part.def);
			}
			float partner_count = partner_mismatched.Count();

			foreach (BodyPartDef part in pawn_partlist)
			{
				if (partner_mismatched.Contains(part))
				{
					pawn_mismatched.Add(part);
					partner_mismatched.Remove(part);
				}
			}

			float pawn_mismatch = pawn_mismatched.Count() / pawn_count;
			float partner_mismatch = partner_mismatched.Count() / partner_count;

			//ModLog.Message("Body type similarity for " + xxx.get_pawnname(pawn) + " and " + xxx.get_pawnname(partner) + ": " + Math.Round(((pawn_mismatch + partner_mismatch) * 50) * size_adjustment, 1) + "%");

			return ((pawn_mismatch + partner_mismatch) / 2) * size_adjustment;
		}

		/// <summary>
		/// 计算 props.pawn 从高潮中获得的最终满足度。
		/// 根据计算出的满足度，应用相关想法并补偿性需求与娱乐需求。
		/// </summary>
		/// <param name="props"></param>
		/// <param name="satisfaction">多个子模组会修改此值，以应用各自的满足度条件。</param>
		public static void SatisfyPersonal(SexProps props, float satisfaction = 0.4f)
		{
			Pawn pawn = props.pawn;
			Pawn partner = props.partner;
			//--ModLog.Message("xxx::satisfy( " + pawn_name + ", " + partner_name + ", " + violent + "," + isCoreLovin + " ) - modifying partner satisfaction");
			var sex_need = pawn?.needs?.TryGetNeed<Need_Sex>();
			if (sex_need == null) return;

			float Trait_Satisfaction = 1f;
			float Stat_Satisfaction = 0f;
			float Circumstances_Satisfaction = 0f; // 暴力/崩溃

			// 来自特质的额外满足度
			if (pawn != null && partner != null)
			{
				if (xxx.is_animal(partner) && xxx.is_zoophile(pawn))
				{
					Trait_Satisfaction += 0.5f;
				}
				if (partner.Dead && xxx.is_necrophiliac(pawn))
				{
					Trait_Satisfaction += 0.5f;
				}
			}

			// 将性满足度属性（最小 0.1，默认 1）作为总满足度的修正系数
			Stat_Satisfaction += Math.Max(xxx.get_sex_satisfaction(pawn), 0.1f);

			// 为特殊/暴力情形应用额外倍率
			Circumstances_Satisfaction += get_satisfaction_circumstance_multiplier(props);

			satisfaction *= Trait_Satisfaction * Stat_Satisfaction;

			float joysatisfaction = satisfaction;
			satisfaction *= Circumstances_Satisfaction;
			if (!RJWSettings.Disable_RecreationDrain)
				joysatisfaction *= (Circumstances_Satisfaction - 1);
			else
				joysatisfaction *= Circumstances_Satisfaction;

			//ModLog.Message("SatisfyPersonal( " + pawn + ", " + satisfaction + " ) - setting pawn sexneed");

			sex_need.CurLevel += satisfaction;

			var joy_need = pawn.needs.TryGetNeed<Need_Joy>();
			if (joy_need == null) return;

			joysatisfaction *= joysatisfaction > 0 ? 0.5f : 1f;              // 将一半的正向满足度转换为娱乐
																			 //ModLog.Message("SatisfyPersonal( " + pawn + ", " + joysatisfaction + " ) - setting pawn joyneed");
			pawn.needs.joy.CurLevel += joysatisfaction;

			//添加阿黑颜
			//性&娱乐 > 95%
			//性&崩溃

			var lev = sex_need.CurLevel;
			if (lev >= sex_need.thresh_ahegao() &&
				(joy_need.CurLevelPercentage > sex_need.thresh_ahegao()) ||
				(AfterSexUtility.BodyIsBroken(pawn) && pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken)?.CurStageIndex >= 3))
			{
				var thoughtDef = DefDatabase<ThoughtDef>.GetNamed("RJW_Ahegao");
				pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
			}
		}

		[SyncMethod]
		public static void GiveTraits(SexProps props, List<Trait> newTraits)
		{
			var pawn = props.pawn;
			var partner = props.partner;

			if (!xxx.has_traits(pawn) || pawn.records.GetValue(xxx.CountOfSex) <= 10)
			{
				return;
			}

			if (props.IsInitiator())
			{
				if (RJWSettings.AddTrait_Rapist && !xxx.is_rapist(pawn) && !xxx.is_masochist(pawn) && props.isRape && pawn.records.GetValue(xxx.CountOfRapedHumanlikes) > 0.12 * pawn.records.GetValue(xxx.CountOfSex))
				{
					var chance = 0.5f;
					if (xxx.is_kind(pawn)) chance -= 0.25f;
					if (xxx.is_prude(pawn)) chance -= 0.25f;
					if (xxx.is_zoophile(pawn)) chance -= 0.25f; // 对强暴类人生物兴趣较低。
					if (xxx.is_ascetic(pawn)) chance -= 0.2f;
					if (xxx.is_bloodlust(pawn)) chance += 0.2f;
					if (xxx.is_psychopath(pawn)) chance += 0.25f;

					if (Rand.Chance(chance))
					{
						Trait rapist = new Trait(xxx.rapist);
						pawn.story.traits.GainTrait(rapist);
						newTraits.Add(rapist);
						//--ModLog.Message(xxx.get_pawnname(pawn) + " aftersex, not rapist, adding rapist trait");
					}
				}

				if (RJWSettings.AddTrait_Necrophiliac && !xxx.is_necrophiliac(pawn) && partner.Dead && pawn.records.GetValue(xxx.CountOfSexWithCorpse) > 0.5 * pawn.records.GetValue(xxx.CountOfSex))
				{
					Trait necropphiliac = new Trait(xxx.necrophiliac);
					pawn.story.traits.GainTrait(necropphiliac);
					newTraits.Add(necropphiliac);
					//ModLog.Message(xxx.get_pawnname(necro) + " aftersex, not necro, adding necro trait");					
				}
			}

			if (RJWSettings.AddTrait_Zoophiliac && !xxx.is_zoophile(pawn) && xxx.is_animal(partner)
				&& (pawn.records.GetValue(xxx.CountOfSexWithAnimals) + pawn.records.GetValue(xxx.CountOfSexWithInsects) > 0.5 * pawn.records.GetValue(xxx.CountOfSex)))
			{
				Trait zoophile = new Trait(xxx.zoophile);
				pawn.story.traits.GainTrait(zoophile);
				newTraits.Add(zoophile);

				MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
				foreach (ThoughtDef memory in new[] {xxx.got_bred, xxx.got_anal_bred, xxx.got_groped, xxx.got_licked})
				{
					memories.RemoveMemoriesOfDef(memory);
				}
				//--ModLog.Message(xxx.get_pawnname(pawn) + " aftersex, not zoo, adding zoo trait");
			}

			if (RJWSettings.AddTrait_Nymphomaniac && !xxx.is_nympho(pawn))
			{
				if (pawn.health.hediffSet.HasHediff(RJWHediffDefOf.HumpShroomAddiction) && pawn.health.hediffSet.HasHediff(RJWHediffDefOf.HumpShroomEffect))
				{
					Trait nymphomaniac = new Trait(xxx.nymphomaniac);
					pawn.story.traits.GainTrait(nymphomaniac);
					newTraits.Add(nymphomaniac);
					//ModLog.Message(xxx.get_pawnname(pawn) + " is HumpShroomAddicted, not nymphomaniac, adding nymphomaniac trait");
				}
			}
		}

		private static void SendTraitGainLetter(Pawn pawn, List<Trait> newTraits)
		{
			if (newTraits.Count == 0 || !PawnUtility.ShouldSendNotificationAbout(pawn))
			{
				return;
			}

			TaggedString letterLabel;
			TaggedString letterDescription;
			if (newTraits.Count == 1)
			{
				string traitLabel = newTraits[0].Label;
				
				string letterLabelKey = TranslationKeyFor("LetterLabelGainedTraitFromSex", newTraits[0]);
				letterLabel = letterLabelKey.Translate(traitLabel.Named("TRAIT"), pawn.Named("PAWN")).CapitalizeFirst();
				
				string letterDescriptionKey = TranslationKeyFor("GainedTraitFromSex", newTraits[0]);
				letterDescription = letterDescriptionKey.Translate(traitLabel.Named("TRAIT"), pawn.Named("PAWN")).CapitalizeFirst();
			}
			else
			{
				letterLabel = "LetterLabelGainedMultipleTraitsFromSex".Translate(pawn.Named("PAWN"));
				letterDescription = "GainedMultipleTraitsFromSex".Translate(pawn.Named("PAWN"));

				letterDescription += "\n";
				foreach (var trait in newTraits)
				{
					letterDescription += "\n" + "GainedMultipleTraitsFromSexListItem".Translate(trait.LabelCap);
				}
			}

			Find.LetterStack.ReceiveLetter(letterLabel, letterDescription, LetterDefOf.NeutralEvent, pawn);

			string TranslationKeyFor(string stem, Trait trait)
			{
				string traitKey = stem + trait.def.defName;
				string traitKeyWithDegree = traitKey + trait.Degree;
				if (traitKeyWithDegree.CanTranslate())
				{
					return traitKeyWithDegree;
				}
				if (traitKey.CanTranslate())
				{
					return traitKey;
				}
				return stem;
			}
		}

		// 检查距上次做爱是否已过去足够时间。
		public static bool ReadyForLovin(Pawn pawn)
		{
			return Find.TickManager.TicksGame > pawn.mindState.canLovinTick;
		}

		// 检查距上次寻找约炮对象是否已过去足够时间。
		// 检查工作时间是否允许约炮，花痴除外
		public static bool ReadyForHookup(Pawn pawn)
		{
			if (!xxx.is_nympho(pawn) && RJWHookupSettings.NoHookupsDuringWorkHours && ((pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything) == TimeAssignmentDefOf.Work) return false;
			return Find.TickManager.TicksGame > pawn.GetCompRJW().NextHookupTick;
		}

		private static void IncreaseTicksToNextLovin(Pawn pawn)
		{
			if (pawn == null || pawn.Dead) return;
			int currentTime = Find.TickManager.TicksGame;
			if (pawn.mindState.canLovinTick <= currentTime)
				pawn.mindState.canLovinTick = currentTime + GenerateMinTicksToNextLovin(pawn);
		}

		[SyncMethod]
		public static int GenerateMinTicksToNextLovin(Pawn pawn)
		{
			if (DebugSettings.alwaysDoLovin) return 100;
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());

			float tick = 1.0f;

			// 花痴会自动获得特质对性欲影响的计时增加。
			if (xxx.is_animal(pawn))
			{
				//var mateMtbHours = pawn.RaceProps.mateMtbHours / 24 * GenDate.TicksPerDay;
				//if (mateMtbHours > 0)
				//	interval = mateMtbHours
				if (RJWSettings.Animal_mating_cooldown == 0)
					tick = 0.75f;
				else
					return RJWSettings.Animal_mating_cooldown * 2500;
			}
			else if (xxx.is_prude(pawn))
				tick = 1.5f;

			float sex_drive = xxx.get_sex_drive(pawn);
			if (sex_drive <= 0.05f)
				sex_drive = 0.05f;

			float interval = AgeConfigDef.Instance.lovinIntervalHoursByAge.Evaluate(ScaleToHumanAge(pawn));
			float rinterval = Math.Max(0.5f, Rand.Gaussian(interval, 0.3f));
			return (int)(tick * rinterval * (2500.0f / sex_drive));
		}

		public static void IncreaseTicksToNextHookup(Pawn pawn)
		{
			if (pawn == null || pawn.Dead)
				return;

			// RimWorld 中每小时有 2500 tick。每次检查之间睡一小时似乎是个不错的起点。
			// 我们本可以做得更花哨，按性欲等因素加权，但人们真的会注意到吗？
			const int TicksBetweenHookups = 2500;

			int currentTime = Find.TickManager.TicksGame;
			pawn.GetCompRJW().NextHookupTick = currentTime + TicksBetweenHookups;
		}

		/// <summary>
		/// <para>确定性行为类型并处理日志输出。</para>
		/// <para>`props.pawn` 应为行为的发起者（强暴者、妓女等）。</para>
		/// <para>`props.partner` 应为目标。</para>
		/// </summary>
		public static void ProcessSex(SexProps props)
		{
			//ModLog.Message("usedCondom=" + usedCondom);
			if (props.pawn == null || props.partner == null)
			{
				if (props.pawn == null)
					ModLog.Error("[SexUtility] ERROR: pawn is null.");
				if (props.partner == null)
					ModLog.Error("[SexUtility] ERROR: partner is null.");
				return;
			}

			// 群交结算方式：Once 时只有第一个发起者结算接受者一侧，
			// 避免接受者的需求/心情被 N 倍叠加。
			bool applyReceiverSide = ShouldApplyReceiverSide(props);

			IncreaseTicksToNextLovin(props.pawn);
			if (applyReceiverSide)
				IncreaseTicksToNextLovin(props.partner);

			Aftersex(props, applyReceiverSide);

			AfterSexUtility.think_about_sex(props, applyReceiverSide);
		}

		/// <summary>
		/// 判断本次结算是否需要应用到「接受者一侧」。
		/// 群交设置设为 <see cref="GroupSexSettlement.Once"/> 时，
		/// 只有接收者记录的主发起者会完整结算，其余发起者只结算自己。
		/// </summary>
		private static bool ShouldApplyReceiverSide(SexProps props)
		{
			if (RJWPreferenceSettings.group_sex_settlement == GroupSexSettlement.PerPartner)
				return true;

			if (props.pawn == null || props.partner == null)
				return true;

			if (props.partner.jobs?.curDriver is not JobDriver_SexBaseReciever receiver)
				return true;

			return receiver.Sexprops == null || receiver.Sexprops.pawn == props.pawn;
		}

		[SyncMethod]
		public static SexProps SelectSextype(Pawn pawn, Pawn partner, bool rape, bool whoring, bool isMechImplant = false)
		{
			var SP = new SexProps(pawn, partner);
			SP.isMechImplant = isMechImplant;
			SP.isRape = rape;
			SP.isWhoring = whoring;

			SexInteractionHelper.ChooseInteraction(SP);

			return SP;
		}

		[SyncMethod]
		public static void Sex_Beatings(SexProps props)
		{
			Pawn pawn = props.pawn;
			Pawn partner = props.partner;
			if ((xxx.is_animal(pawn) && xxx.is_animal(partner)))
				return;

			//不记得它是干什么的，大概是猎杀人类相关？先禁用，等反馈
			//if (!xxx.is_human(pawn))
			//	return;

			//如果小人无法使用暴力/近战能力低，他们很可能不会殴打伴侣
			if (pawn.skills?.GetSkill(SkillDefOf.Melee).Level < 1)
				return;

			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			float rand_value = Rand.Value;
			//float rand_value = RJW_Multiplayer.RJW_MP_RAND();
			float victim_pain = partner.health.hediffSet.PainTotal;
			// 嗜血使施暴者更可能殴打囚犯
			float beating_chance = xxx.config.base_chance_to_hit_prisoner * (xxx.is_bloodlust(pawn) ? 1.5f : 1.0f);
			// 精神病态使施暴者更可能在超过 significant_pain_threshold 后殴打囚犯
			float beating_threshold = xxx.is_psychopath(pawn) ? xxx.config.extreme_pain_threshold : pawn.HostileTo(partner) ? xxx.config.significant_pain_threshold : xxx.config.minor_pain_threshold;

			//--ModLog.Message("roll_to_hit:  rand = " + rand_value + ", beating_chance = " + beating_chance + ", victim_pain = " + victim_pain + ", beating_threshold = " + beating_threshold);
			if ((victim_pain < beating_threshold && rand_value < beating_chance) || (rand_value < (beating_chance / 2) && xxx.is_bloodlust(pawn)))
			{
				Sex_Beatings_Dohit(pawn, partner, props.isRapist);
			}
		}

		[SyncMethod]
		public static void Sex_Beatings_Dohit(Pawn pawn, Pawn partner, bool isRape = false)
		{
			//--ModLog.Message("   done told her twice already...");
			if (SocialInteractionUtility.TryGetRandomVerbForSocialFight(pawn, out Verb v))
			{
				//ModLog.Message("   v. : " + v);
				//ModLog.Message("   v.GetDamageDef : " + v.GetDamageDef());
				//ModLog.Message("   v.v.tool - " + v.tool.label);
				//ModLog.Message("   v.v.tool.power base - " + v.tool.power);
				var orgpower = v.tool.power;
				//以防出错
				try
				{
					//ModLog.Message("   v.v.tool.power base - " + v.tool.power);
					if (RJWSettings.gentle_rape_beating || !isRape)
					{
						v.tool.power = 0;
						//partner.stances.stunner.StunFor(600, pawn);
					}
					//ModLog.Message("   v.v.tool.power mod - " + v.tool.power);
					var guilty = true;
					if (!pawn.guilt.IsGuilty)
					{
						guilty = false;
					}
					pawn.meleeVerbs.TryMeleeAttack(partner, v);

					if (pawn.guilt.IsGuilty && !guilty)
						pawn.guilt.Notify_Guilty(0);
				}
				catch
				{ }
				v.tool.power = orgpower;
				//ModLog.Message("   v.v.tool.power reset - " + v.tool.power);
			}
		}


		// 覆盖当前着装。默认为裸体，可选择保留头部装备。
		public static void DrawNude(Pawn pawn, bool keep_hat_on = false)
		{
			
			if (!xxx.is_human(pawn)) return;
			if (pawn.Map != Find.CurrentMap) return;
			if (RJWPreferenceSettings.sex_wear == RJWPreferenceSettings.Clothing.Clothed) return;
			var comp = pawn.GetCompRJW();
			if(comp == null) return;
			if (comp.drawNude != true)
				{
				comp.drawNude = true;
				var renderer = pawn.Drawer?.renderer;
				if(renderer != null)
					renderer.SetAllGraphicsDirty();
			}
			
			comp.keep_hat_on = keep_hat_on;
		}

		public static void reduce_rest(Pawn pawn, float x = 1f)
		{
			if (pawn ==  null || x <= 0) return;

			Need_Rest need_rest = pawn.needs.TryGetNeed<Need_Rest>();
			if (need_rest == null)
				return;

			need_rest.CurLevel -= need_rest.RestFallPerTick * x;
		}
		public static void OffsetPsyfocus(Pawn pawn, float x = 0)//0-1
		{
			if (ModsConfig.RoyaltyActive)
			{
				//pawn.psychicEntropy.Notify_Meditated();
				if (pawn.HasPsylink)
				{
					pawn.psychicEntropy.OffsetPsyfocusDirectly(x);
				}
			}
		}

#nullable enable

		public static void TransferFluids(SexProps props)
		{
			Pawn pawn = props.pawn, partner = props.partner;
			if (partner == null || props.resolved == null)
			{
				return;
			}
			
			float nutritionLost = 0f;
			foreach ((var penetrator, var orifice) in props.resolved.Penetrations)
			{
				if (penetrator.Owner != pawn)
					continue;

				RJWLewdablePart? rjwPenetrator = penetrator as RJWLewdablePart;
				RJWLewdablePart? rjwOrifice = orifice as RJWLewdablePart;
				if (rjwPenetrator == null)
				{
					continue;
				}

				//对于非阴道和肛门插入，该部位始终为 null，所以这里之后只检查嘴/喙部位。
				if (rjwPenetrator.SexPart.Def.fluid.ingestThroughAnyOrifice 
					|| (rjwOrifice == null && (orifice.Family == LewdablePartFamily.Mouth || orifice.Family == LewdablePartFamily.Beak)))
				{
					IngestFluids(orifice.Owner, penetrator.Owner, rjwPenetrator.SexPart, rjwOrifice?.SexPart, ref nutritionLost);
					if (RJWSettings.DebugInteraction) ModLog.Message($"TransferFluids: {rjwPenetrator.Label} ({penetrator.Owner}) -> {orifice.Label} ({orifice.Owner}). {nutritionLost} nutrition.");
				}
			}

			if (pawn != partner && pawn.needs != null && partner.needs != null)
			{
				TransferNutritionSucc(props);
			}
		}

		public static void IngestFluids(Pawn receiver, Pawn giver, ISexPartHediff fromPart, ISexPartHediff? toPart, ref float totalNutritionLost)
		{
			const float maxNutritionLoss = 0.15f;

			Need_Food? receiverFood = receiver.needs?.food, giverFood = giver.needs?.food;

			var fluid = fromPart.GetPartComp().Fluid;
			
			float baseAmount = fromPart.GetPartComp().FluidAmount;
			float amountIngested;

			// 从给予方扣除营养
			if (giverFood != null && fluid.baseNutritionCost != 0)
			{
				float nutritionCost = Math.Min(baseAmount * fluid.baseNutritionCost, giverFood.CurLevel);
				amountIngested = nutritionCost / fluid.baseNutritionCost;
				if (totalNutritionLost < giverFood.MaxLevel * maxNutritionLoss)
				{
					totalNutritionLost += nutritionCost;
					giverFood.CurLevel -= Math.Min(giverFood.MaxLevel * maxNutritionLoss, nutritionCost);
				}
			}
			else
			{
				amountIngested = baseAmount;
			}

			if (fluid.quenchesThirst)
			{
				TransferThirst(giver, receiver);
			}

			if (fluid.consumable != null)
			{
				var thing = ThingMaker.MakeThing(fluid.consumable);

				thing.stackCount = (int) Math.Max(amountIngested * fluid.consumableFluidRatio, 1f);

				float ingestedNutrition = thing.Ingested(receiver, 1000f);
				if (receiverFood != null)
				{
					receiverFood.CurLevel += ingestedNutrition;
				}
			}
			else
			{
				if (receiverFood != null)
				{
					receiverFood.CurLevel += amountIngested * fluid.baseNutrition;
				}
			}

			if (fluid.ingestionDoers != null)
			{
				foreach (var doer in fluid.ingestionDoers)
				{
					doer.Ingested(receiver, fluid, amountIngested, fromPart, toPart);
				}
			}
		}

#nullable disable

		// 从（大概）插入的一方取走营养，并注入到承受的一方
		// 与本模组中的一切类似，这里可以大幅扩展，但当前重点是防止
		// 那些被困在一大群强暴者中的人（某些模组下可能发生）挨饿。目前仅核心做爱使用。

		// TODO (A)：弄清这段为何存在
		public static void TransferNutritionCore(SexProps props)
		{
			//ModLog.Message("xxx::TransferNutrition:: " + xxx.get_pawnname(pawn) + " => " + xxx.get_pawnname(partner)); 
			if (props.partner?.needs == null)
			{
				//ModLog.Message("xxx::TransferNutrition() failed due to lack of transfer equipment or pawn ");
				return;
			}
			if (props.pawn?.needs == null)
			{
				//ModLog.Message("xxx::TransferNutrition() failed due to lack of transfer equipment or pawn ");
				return;
			}

			TransferNutritionSucc(props);

			//将营养从（大概的）“给予方”转移给伴侣
			if (props.sexType == xxx.rjwSextype.Sixtynine ||
				props.sexType == xxx.rjwSextype.Oral ||
				props.sexType == xxx.rjwSextype.Cunnilingus ||
				props.sexType == xxx.rjwSextype.Fellatio)
			{
				Pawn pawn = props.pawn;
				Pawn partner = props.partner;
				Pawn giver, receiver;
				List<Hediff> pawnparts = pawn.GetGenitalsList();
				List<Hediff> partnerparts = partner.GetGenitalsList();
				bool didTransfer = false;

				if (props.sexType == xxx.rjwSextype.Sixtynine || Genital_Helper.has_penis_fertile(pawn, pawnparts) || Genital_Helper.has_ovipositorF(pawn, pawnparts))
				{
					giver = pawn;
					receiver = partner;

					Need_Food need = giver.needs.TryGetNeed<Need_Food>();
					if (need == null)
					{
						//ModLog.Message("TransferNutrition() " + xxx.get_pawnname(giver) + " doesn't track nutrition in itself, probably shouldn't feed the others");
						return;
					}
					float nutrition_amount = Math.Min(need.MaxLevel / 15f, need.CurLevel); //体型已由 need.MaxLevel 隐式考虑
					giver.needs.food.CurLevel = need.CurLevel - nutrition_amount;
					//ModLog.Message("TransferNutrition() " + xxx.get_pawnname(giver) + " sent " + nutrition_amount + " of nutrition");

					if (receiver.needs?.TryGetNeed<Need_Food>() != null)
					{
						//ModLog.Message("xxx::TransferNutrition() " +  xxx.get_pawnname(receiver) + " can receive");
						receiver.needs.food.CurLevel += nutrition_amount;
					}
					TransferThirst(giver, receiver);
					didTransfer = true;
				}

				if (props.sexType == xxx.rjwSextype.Sixtynine || (!didTransfer && (Genital_Helper.has_penis_fertile(partner, partnerparts) || Genital_Helper.has_ovipositorF(partner, partnerparts))))
				{
					giver = partner;
					receiver = pawn;

					Need_Food need = giver.needs?.TryGetNeed<Need_Food>();
					if (need == null)
					{
						//ModLog.Message("TransferNutrition() " + xxx.get_pawnname(giver) + " doesn't track nutrition in itself, probably shouldn't feed the others");
						return;
					}
					float nutrition_amount = Math.Min(need.MaxLevel / 15f, need.CurLevel); //体型已由 need.MaxLevel 隐式考虑
					giver.needs.food.CurLevel = need.CurLevel - nutrition_amount;
					//ModLog.Message("TransferNutrition() " + xxx.get_pawnname(giver) + " sent " + nutrition_amount + " of nutrition");

					if (receiver.needs?.TryGetNeed<Need_Food>() != null)
					{
						//ModLog.Message("TransferNutrition() " +  xxx.get_pawnname(receiver) + " can receive");
						receiver.needs.food.CurLevel += nutrition_amount;
					}
					TransferThirst(giver, receiver);
				}
			}
		}

		public static void TransferThirst(Pawn pawn, Pawn receiver)
		{
			if (xxx.DubsBadHygieneIsActive)
			{
				Need DBHThirst = receiver.needs?.AllNeeds.Find(x => x.def == xxx.DBHThirst);
				if (DBHThirst != null)
				{
					//ModLog.Message("TransferThirst() " + xxx.get_pawnname(receiver) + " decreasing thirst");
					receiver.needs.TryGetNeed(DBHThirst.def).CurLevel += 0.1f;
				}
			}
		}

		public static void TransferNutritionSucc(SexProps props)
		{
			//魅魔法力与休息恢复相关内容
			if (props.sexType == xxx.rjwSextype.Oral ||
				props.sexType == xxx.rjwSextype.Cunnilingus ||
				props.sexType == xxx.rjwSextype.Fellatio ||
				props.sexType == xxx.rjwSextype.Vaginal ||
				props.sexType == xxx.rjwSextype.Anal ||
				props.sexType == xxx.rjwSextype.DoublePenetration)
			{
				if (xxx.has_traits(props.partner))
				{
					bool gainrest = false;
					if (xxx.RoMIsActive && (props.partner.story.traits.HasTrait(xxx.Succubus) || props.partner.story.traits.HasTrait(xxx.Warlock)))
					{
						Need TM_Mana = props.partner?.needs?.AllNeeds.Find(x => x.def == xxx.TM_Mana);
						if (TM_Mana != null)
						{
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(partner) + " increase mana");
							props.partner.needs.TryGetNeed(TM_Mana.def).CurLevel += 0.1f;
						}
						gainrest = true;
					}

					if (xxx.NightmareIncarnationIsActive)
					{
						foreach (var x in props.partner.AllComps?.Where(x => x.props?.ToString() == "NightmareIncarnation.CompProperties_SuccubusRace"))
						{
							Need NI_Need_Mana = props.partner?.needs?.AllNeeds.Find(x => x.def == xxx.NI_Need_Mana);
							if (NI_Need_Mana != null)
							{
								//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(partner) + " increase mana");
								props.partner.needs.TryGetNeed(NI_Need_Mana.def).CurLevel += 0.1f;
							}
							gainrest = true;
							break;
						}
					}

					if (gainrest)
					{
						Need_Rest need1 = props.pawn.needs?.TryGetNeed<Need_Rest>();
						Need_Rest need2 = props.partner.needs?.TryGetNeed<Need_Rest>();
						if (need1 != null && need2 != null)
						{
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(partner) + " increase rest");
							props.partner.needs.TryGetNeed(need2.def).CurLevel += 0.25f;
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(pawn) + " decrease rest");
							props.pawn.needs.TryGetNeed(need1.def).CurLevel -= 0.25f;
						}
					}
				}

				if (xxx.has_traits(props.pawn))
				{
					bool gainrest = false;
					if (xxx.RoMIsActive && (props.pawn.story.traits.HasTrait(xxx.Succubus) || props.pawn.story.traits.HasTrait(xxx.Warlock)))
					{
						Need TM_Mana = props.pawn?.needs?.AllNeeds.Find(x => x.def == xxx.TM_Mana);
						if (TM_Mana != null)
						{
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(pawn) + " increase mana");
							props.pawn.needs.TryGetNeed(TM_Mana.def).CurLevel += 0.1f;
						}
					}

					if (xxx.NightmareIncarnationIsActive)
					{
						foreach (var x in props.pawn.AllComps?.Where(x => x.props?.ToString() == "NightmareIncarnation.CompProperties_SuccubusRace"))
						{
							Need NI_Need_Mana = props.pawn?.needs?.AllNeeds.Find(x => x.def == xxx.NI_Need_Mana);
							if (NI_Need_Mana != null)
							{
								//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(partner) + " increase mana");
								props.pawn.needs.TryGetNeed(NI_Need_Mana.def).CurLevel += 0.1f;
							}
							gainrest = true;
							break;
						}
					}

					if (gainrest)
					{
						Need_Rest need1 = props.partner.needs.TryGetNeed<Need_Rest>();
						Need_Rest need2 = props.pawn.needs.TryGetNeed<Need_Rest>();
						if (need1 != null && need2 != null)
						{
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(pawn) + " increase rest");
							props.pawn.needs.TryGetNeed(need2.def).CurLevel += 0.25f;
							//ModLog.Message("TransferNutritionSucc() " + xxx.get_pawnname(partner) + " decrease rest");
							props.partner.needs.TryGetNeed(need1.def).CurLevel -= 0.25f;
						}
					}
				}
			}
		}

		public static float get_broken_consciousness_debuff(Pawn pawn)
		{
			if (pawn == null)
			{
				return 1.0f;
			}

			try
			{
				Hediff broken = pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken);
				foreach (PawnCapacityModifier capMod in broken.CapMods)
				{
					if (capMod.capacity == PawnCapacityDefOf.Consciousness)
					{
						if (RJWSettings.DevMode) ModLog.Message("Broken Pawn Consciousness factor: " + capMod.postFactor);
						return capMod.postFactor;
					}
				}

				// 回退
				return 1.0f;
			}
			catch (NullReferenceException)
			{
				//ModLog.Warning(e.ToString());
				return 1f;
			}
		}

		public static float get_satisfaction_circumstance_multiplier(SexProps props)
		{
			// 获取应叠加在性满足度属性之上的满足度倍率
			// 这主要针对只在特定情形下影响满足度的特质

			Pawn pawn = props.pawn;
			Boolean isViolentSex = props.isRape;

			float multiplier = 1.0f;
			if (xxx.is_human(pawn))
			{
				// 抵消崩溃小人的意识减益（抵消低意识对性满足度评分的影响）
				multiplier = (1.0f / get_broken_consciousness_debuff(pawn));

				// 为暴力特质与暴力性行为提供倍率加成
				if (!props.isReceiver)
				{
					// 强暴者/嗜血者从暴力遭遇中获得 50% 更多满足度，从非暴力中获得较少
					if (isViolentSex)
					{
						if (xxx.is_rapist(pawn) || xxx.is_bloodlust(pawn) || xxx.is_psychopath(pawn))
							multiplier += 0.5f;
						else
							multiplier -= 0.2f;
					}
					else
					{
						if (xxx.is_rapist(pawn) || xxx.is_bloodlust(pawn))
							multiplier -= 0.2f;
						else
							multiplier += 0.2f;
					}
				}
				else if (props.isReceiver)
				{
					// 受虐狂从承受暴力性行为中获得 50% 更多满足度，从正常性行为中减少 20%
					if (isViolentSex)
					{
						if (xxx.is_masochist(pawn))
							multiplier += 0.5f;
						else
							multiplier -= 0.2f;
					}
					else
					{
						if (xxx.is_masochist(pawn))
							multiplier -= 0.2f;
						else
							multiplier += 0.2f;
					}

					// 崩溃小人的倍率
					if (isViolentSex && AfterSexUtility.BodyIsBroken(pawn))
					{
						// 根据阶段添加额外满足度
						switch (pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).CurStageIndex)
						{
							case 0:
								break;

							case 1:
								// 2 级时 50% 加成
								//multiplier += 0.5f;
								break;

							case 2:
								// 2 级时 100% 加成
								multiplier += 1.0f;
								break;
						}
					}
				}
				//ModLog.Message("Sex satisfaction multiplier: " + multiplier);
			}
			return multiplier;
		}
	}
}
