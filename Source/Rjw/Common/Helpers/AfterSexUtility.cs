#nullable disable

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Multiplayer.API;
using System.Collections.Generic;

namespace rjw
{
	public static class AfterSexUtility
	{
		//性行为后的想法
		public static readonly ThoughtDef got_raped = DefDatabase<ThoughtDef>.GetNamed("GotRaped");
		public static readonly ThoughtDef got_anal_raped = DefDatabase<ThoughtDef>.GetNamed("GotAnalRaped");
		public static readonly ThoughtDef got_anal_raped_byfemale = DefDatabase<ThoughtDef>.GetNamed("GotAnalRapedByFemale");
		public static readonly ThoughtDef got_raped_unconscious = DefDatabase<ThoughtDef>.GetNamed("GotRapedUnconscious");
		public static readonly ThoughtDef masochist_got_raped_unconscious = DefDatabase<ThoughtDef>.GetNamed("MasochistGotRapedUnconscious");

		public static readonly ThoughtDef got_bred = DefDatabase<ThoughtDef>.GetNamed("GotBredByAnimal");
		public static readonly ThoughtDef got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("GotAnalBredByAnimal");
		public static readonly ThoughtDef got_licked = DefDatabase<ThoughtDef>.GetNamed("GotLickedByAnimal");
		public static readonly ThoughtDef got_groped = DefDatabase<ThoughtDef>.GetNamed("GotGropedByAnimal");

		public static readonly ThoughtDef masochist_got_raped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotRaped");
		public static readonly ThoughtDef masochist_got_anal_raped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalRaped");
		public static readonly ThoughtDef masochist_got_anal_raped_byfemale = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalRapedByFemale");
		public static readonly ThoughtDef masochist_got_bred = DefDatabase<ThoughtDef>.GetNamed("MasochistGotBredByAnimal");
		public static readonly ThoughtDef masochist_got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("MasochistGotAnalBredByAnimal");
		public static readonly ThoughtDef masochist_got_licked = DefDatabase<ThoughtDef>.GetNamed("MasochistGotLickedByAnimal");
		public static readonly ThoughtDef masochist_got_groped = DefDatabase<ThoughtDef>.GetNamed("MasochistGotGropedByAnimal");
		public static readonly ThoughtDef allowed_animal_to_breed = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToBreed");
		public static readonly ThoughtDef allowed_animal_to_lick = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToLick");
		public static readonly ThoughtDef allowed_animal_to_grope = DefDatabase<ThoughtDef>.GetNamed("AllowedAnimalToGrope");
		public static readonly ThoughtDef zoophile_got_bred = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotBredByAnimal");
		public static readonly ThoughtDef zoophile_got_anal_bred = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotAnalBredByAnimal");
		public static readonly ThoughtDef zoophile_got_licked = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotLickedByAnimal");
		public static readonly ThoughtDef zoophile_got_groped = DefDatabase<ThoughtDef>.GetNamed("ZoophileGotGropedByAnimal");
		public static readonly ThoughtDef hate_my_rapist = DefDatabase<ThoughtDef>.GetNamed("HateMyRapist");
		public static readonly ThoughtDef kinda_like_my_rapist = DefDatabase<ThoughtDef>.GetNamed("KindaLikeMyRapist");
		public static readonly ThoughtDef allowed_me_to_get_raped = DefDatabase<ThoughtDef>.GetNamed("AllowedMeToGetRaped");
		public static readonly ThoughtDef stole_some_lovin = DefDatabase<ThoughtDef>.GetNamed("StoleSomeLovin");
		public static readonly ThoughtDef bloodlust_stole_some_lovin = DefDatabase<ThoughtDef>.GetNamed("BloodlustStoleSomeLovin");
		public static readonly ThoughtDef violated_corpse = DefDatabase<ThoughtDef>.GetNamed("ViolatedCorpse");
		public static readonly ThoughtDef gave_virginity = DefDatabase<ThoughtDef>.GetNamed("FortunateGaveVirginity");
		public static readonly ThoughtDef lost_virginity = DefDatabase<ThoughtDef>.GetNamed("UnfortunateLostVirginity");
		public static readonly ThoughtDef took_virginity = DefDatabase<ThoughtDef>.GetNamed("TookVirginity");

		//violent——小人强暴对方时标记为 true
		//注意：violent 并不可靠，因为任何一方都可能是强暴者。应改为检查 jobdriver，由于此方法在工作结束前调用，它们仍然处于活动状态。
		public static void think_about_sex(SexProps props, bool applyReceiverSide = true)
		{
			Pawn receiving = props.partner;//TODO：等交互支持角色反转后，找个时间把这套烂东西换掉
			think_about_sex(props.pawn, props.partner, receiving == props.pawn, props, props.isWhoring);
			if (applyReceiverSide)
				think_about_sex(props.partner, props.pawn, receiving == props.partner, props, false);
		}
		public static void think_about_sex(Pawn pawn, Pawn partner, bool isReceiving, SexProps props, bool whoring = false)
		{
			// 伴侣不应为 null，但以防别处改动了什么……
			if (partner == null)
			{
				ModLog.Error("xxx::think-after_sex( ERROR: " + xxx.get_pawnname(pawn) + " has no partner. This should not be called from solo acts. Sextype: " + props.sexType);
				return;
			}

			// 现在会分别检查两个小人，而不是直接给伴侣添加想法。
			//若当前检查的小人已死亡或无法产生想法，直接返回即可，这样能简化检查。
			if (pawn.Dead || !xxx.is_human(pawn))
				return;

			bool masochist = xxx.is_masochist(pawn);
			bool zoophile = xxx.is_zoophile(pawn);
			bool guilty = props.canBeGuilty;
			if (HasLoveEnhancer(pawn, partner) && RJWSettings.love_enhancer_override)
			{
				masochist = true;
				zoophile = true;
			}
			if (masochist)
			{
				if (RJWSettings.rape_beating)
					pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.HarmedMe, partner);
			}
			if (!masochist)
			{
				//TODO：以后要不要加粗暴性行为？
				//foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
				//{
				//	if (relation.otherPawn == partner)
				//		if (relation.def == PawnRelationDefOf.Lover ||
				//			relation.def == PawnRelationDefOf.Fiance ||
				//			relation.def == PawnRelationDefOf.Spouse)
				//		{
				//			masochist = true;
				//			break;
				//		}
				//}
				//if (pawn.IsPrisoner || masochist || is_slave(pawn) || pawn.IsDesignatedComfort())
				if (pawn.IsPrisoner || xxx.is_slave(pawn) || pawn.IsDesignatedComfort())
				{
					guilty = false;
				}
			}
			else
				guilty = false;

			ThoughtDef thoughtgain = null;
			//昏迷的小人不会产生想法
			//而如果他们发生了性行为，那很可能是强暴，因为他们别无选择
			if (!pawn.health.capacities.CanBeAwake)
			{
				thoughtgain = xxx.is_masochist(pawn) ? masochist_got_raped_unconscious : got_raped_unconscious;
				pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtgain);
				return;
			}

			// TODO：把所有这些重复的参数重构为一个 `ref struct`。
			// 不过，这会是一项破坏性改动。

			thoughtgain = think_about_sex_Bestiality(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);

			if (thoughtgain == null)
				thoughtgain = think_about_sex_Rapist(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);

			if (thoughtgain == null)
				thoughtgain = think_about_sex_Victim(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);

			if (thoughtgain == null)
				thoughtgain = think_about_sex_Consensual(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);

			if (thoughtgain != null)
			{
				var newMemory = FinalizeThought(thoughtgain, pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);
				pawn.needs.mood?.thoughts.memories.TryGainMemory(newMemory, partner);
			}
		}

		public static Thought_Memory FinalizeThought(
			ThoughtDef thoughtgain,
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
		  )
		{
			var newMemory = (Thought_Memory)ThoughtMaker.MakeThought(thoughtgain);

			// 应用来自爱情增强器的原版做爱心情加成。
			if (thoughtgain == ThoughtDefOf.GotSomeLovin && HasLoveEnhancer(pawn, partner))
				newMemory.moodPowerFactor = 1.5f;

			return newMemory;
		}

		public static ThoughtDef think_about_sex_Bestiality(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{

			//ModLog.Message("xxx::think-after_sex( ERROR: 3");
			// 动物对殖民者的想法。
			ThoughtDef thoughtDef = null;
			if (xxx.is_animal(partner) && isReceiving)
			{
				if (!zoophile && !props.isRape)
				{
					//ModLog.Message("xxx::think-after_sex( ERROR: 3.1");
					if (props.sexType == xxx.rjwSextype.Oral)
						thoughtDef = (allowed_animal_to_lick);
					else if (props.sexType == xxx.rjwSextype.Anal || props.sexType == xxx.rjwSextype.Vaginal)
						thoughtDef = (allowed_animal_to_breed);
					else //Other rarely seen sex types, such as fingering (by primates, monster girls, etc)
						thoughtDef = (allowed_animal_to_grope);
				}
				else
				{
					//其他少见的性行为类型，例如指交（由灵长类、怪物娘等执行）
					if (!zoophile)
					{
						if (props.sexType == xxx.rjwSextype.Oral)
							thoughtDef = (masochist ? masochist_got_licked : got_licked);
						else if (props.sexType == xxx.rjwSextype.Vaginal)
							thoughtDef = (masochist ? masochist_got_bred : got_bred);
						else if (props.sexType == xxx.rjwSextype.Anal)
							thoughtDef = (masochist ? masochist_got_anal_bred : got_anal_bred);
						else //其他类型
							thoughtDef = (masochist ? masochist_got_groped : got_groped);
					}
					else
					{
						if (props.sexType == xxx.rjwSextype.Oral)
							thoughtDef = (zoophile_got_licked);
						else if (props.sexType == xxx.rjwSextype.Vaginal)
							thoughtDef = (zoophile_got_bred);
						else if (props.sexType == xxx.rjwSextype.Anal)
							thoughtDef = (zoophile_got_anal_bred);
						else //其他类型
							thoughtDef = (zoophile_got_groped);
					}
				}
				think_about_sex_Bestiality_TameAttempt(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);
			}
			return thoughtDef;
		}

		public static void think_about_sex_Bestiality_TameAttempt(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{
			if (pawn.CurJob != null)
				if (!(pawn.Downed && partner.Downed))
					if (!partner.Dead && zoophile && (pawn.CurJob.def != xxx.gettin_raped) && partner.Faction == null && pawn.Faction == Faction.OfPlayer)
					{
						InteractionDef intDef = !partner.AnimalOrWildMan() ? InteractionDefOf.RecruitAttempt : InteractionDefOf.TameAttempt;
						pawn.interactions.TryInteractWith(partner, intDef);
					}
		}

		public static ThoughtDef think_about_sex_Rapist(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{
			ThoughtDef thoughtDef = null;
			if (partner.Dead || (partner.CurJob != null && partner.CurJob.def == xxx.gettin_raped) || pawn.jobs?.curDriver is JobDriver_Rape)
			{
				thoughtDef = (xxx.is_rapist(pawn) || xxx.is_bloodlust(pawn) || xxx.is_psychopath(pawn)) ? bloodlust_stole_some_lovin : stole_some_lovin;

				if ((xxx.is_necrophiliac(pawn) || xxx.is_psychopath(pawn)) && partner.Dead)
				{
					thoughtDef = (violated_corpse);
				}
			}
			return thoughtDef;
		}

		public static ThoughtDef think_about_sex_Victim(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{
			ThoughtDef thoughtDef = null;
			if (pawn.CurJob != null && pawn.CurJob.def == xxx.gettin_raped || partner.jobs?.curDriver is JobDriver_Rape)
			{
				if (xxx.is_human(partner))
				{
					if (props.sexType == xxx.rjwSextype.Anal)
					{
						if (xxx.is_female(partner))
						{
							thoughtDef = masochist ? masochist_got_anal_raped_byfemale : got_anal_raped_byfemale;
						}
						else
						{
							thoughtDef = masochist ? masochist_got_anal_raped : got_anal_raped;
						}
					}
					else
					{
						thoughtDef = masochist ? masochist_got_raped : got_raped;
					}

					ThoughtDef pawn_thought_about_rapist = masochist ? kinda_like_my_rapist : hate_my_rapist;
					pawn.needs.mood.thoughts.memories.TryGainMemory(pawn_thought_about_rapist, partner);
					if (guilty)
						partner.guilt.Notify_Guilty(GenDate.TicksPerDay);

				}

				think_about_sex_Victim_Blame(pawn, partner, isReceiving, props, whoring, masochist, zoophile, guilty);
			}
			return thoughtDef;
		}
		public static void think_about_sex_Victim_Blame(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{
			if (pawn.Faction != null && pawn.Map != null && !masochist && !(xxx.is_animal(partner) && zoophile))
			{
				foreach (Pawn bystander in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Where(x => !xxx.is_animal(x) && x != pawn && x != partner && !x.Downed && !x.Suspended))
				{
					// 不能穿墙视物，不能看到整张地图，只能看到周围 15 格
					if (pawn.CanSee(bystander) && pawn.Position.DistanceToSquared(bystander.Position) < 225)
					{
						pawn.needs.mood.thoughts.memories.TryGainMemory(allowed_me_to_get_raped, bystander);
					}
				}
			}
		}

		public static ThoughtDef think_about_sex_Consensual(
			Pawn pawn,
			Pawn partner,
			bool isReceiving,
			SexProps props,
			bool whoring,
			bool masochist,
			bool zoophile,
			bool guilty
			)
		{
			ThoughtDef thoughtDef = null;
			if (xxx.is_human(partner))
			{
				if (!props.isCoreLovin && !whoring)
				{
					// 人类伴侣且不属于强暴或恋尸行为。添加原版的 GotSomeLovin 想法
					thoughtDef = ThoughtDefOf.GotSomeLovin;
				}
			}
			return thoughtDef;
		}

		public static bool HasLoveEnhancer(Pawn pawn) =>
			pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer);

		public static bool HasLoveEnhancer(Pawn pawn, Pawn partner) =>
			HasLoveEnhancer(pawn) || HasLoveEnhancer(partner);

		public static void UpdateRecords(SexProps props, bool applyReceiverSide = true)
		{
			UpdateRecordsInitiator(props);
			if (applyReceiverSide && props.sexType != xxx.rjwSextype.Masturbation)
				UpdateRecordsPartner(props);
			if (applyReceiverSide)
				processAnimalParaphilia(props);
		}

		private static void UpdateRecordsInitiator(SexProps props)
		{

			Pawn pawn = props.pawn;
			Pawn partner = props.partner;

			if (props.sexType == xxx.rjwSextype.Masturbation)
			{
				pawn.records.Increment(xxx.CountOfFappin);
				return;
			}

			//bool isVirginSex = xxx.is_Virgin(pawn); //need copy value before count increase.
			//ThoughtDef currentThought = null;

			pawn.records.Increment(xxx.CountOfSex);
			//bool masochist = xxx.is_masochist(pawn);
			//bool zoophile = xxx.is_zoophile(pawn);
			//if (HasLoveEnhancer(pawn, partner))
			//{
			//	masochist = true;
			//	zoophile = true;
			//}

			if (props.partner.Dead)
			{
				pawn.records.Increment(xxx.CountOfSexWithCorpse);
			}
			if (!props.isRape)
			{
				if (xxx.is_human(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithHumanlikes);
					//currentThought = isLoveSex ? xxx.gave_virginity : null;
				}
				else if (xxx.is_insect(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithInsects);
				}
				else if (xxx.is_animal(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithAnimals);
					//currentThought = zoophile ? xxx.gave_virginity : null;
				}
				else
				{
					pawn.records.Increment(xxx.CountOfSexWithOthers);
				}
			}
			else
			{
				//if (!pawnIsRaper)
				//{
				//	currentThought = masochist ? xxx.gave_virginity : xxx.lost_virginity;
				//}

				if (xxx.is_human(partner))
				{
					pawn.records.Increment(xxx.CountOfRapedHumanlikes);
					//if (pawnIsRaper && (xxx.is_rapist(pawn) || xxx.is_bloodlust(pawn)))
					//	currentThought = xxx.gave_virginity;
				}
				else if (xxx.is_insect(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithInsects);
					pawn.records.Increment(xxx.CountOfRapedInsects);
				}
				else if (xxx.is_animal(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithAnimals);
					pawn.records.Increment(xxx.CountOfRapedAnimals);
					//if (zoophile) currentThought = xxx.gave_virginity;
				}
				else
				{
					pawn.records.Increment(xxx.CountOfSexWithOthers);
					pawn.records.Increment(xxx.CountOfRapedOthers);
				}
			}

			//if (isVirginSex) //&& (sextype == rjwSextype.Vaginal || sextype == rjwSextype.DoublePenetration))
			//{
			//	ModLog.Message(xxx.get_pawnname(pawn) + " | " + xxx.get_pawnname(partner) + " | " + currentThought);
			//	ModLog.Message("1");
			//	if (!is_animal(partner))//passive
			//	{
			//		if (currentThought != null)
			//			partner.needs.mood.thoughts.memories.TryGainMemory(currentThought);
			//	}
			//	ModLog.Message("2");
			//	if (!is_animal(pawn))//active
			//	{
			//		currentThought = took_virginity;
			//		pawn.needs.mood.thoughts.memories.TryGainMemory(currentThought);
			//	}
			//}
		}

		private static void UpdateRecordsPartner(SexProps props)
		{

			if (props.partner == null || props.partner.Dead) // 自慰或奸尸
				return;

			Pawn pawn = props.partner;
			Pawn partner = props.pawn;

			//bool isVirginSex = xxx.is_Virgin(pawn); //need copy value before count increase.
			//ThoughtDef currentThought = null;

			pawn.records.Increment(xxx.CountOfSex);
			//bool masochist = xxx.is_masochist(pawn);
			//bool zoophile = xxx.is_zoophile(pawn);
			//if (HasLoveEnhancer(pawn, partner))
			//{
			//	masochist = true;
			//	zoophile = true;
			//}

			if (!props.isRape)
			{
				if (xxx.is_human(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithHumanlikes);
					//currentThought = isLoveSex ? xxx.gave_virginity : null;
				}
				else if (xxx.is_insect(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithInsects);
				}
				else if (xxx.is_animal(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithAnimals);
					//currentThought = zoophile ? xxx.gave_virginity : null;
				}
				else
				{
					pawn.records.Increment(xxx.CountOfSexWithOthers);
				}
			}
			else
			{
				//if (!pawnIsRaper)
				//{
				//	currentThought = masochist ? xxx.gave_virginity : xxx.lost_virginity;
				//}

				if (xxx.is_human(partner))
				{
					pawn.records.Increment(xxx.CountOfBeenRapedByHumanlikes);
					//if (pawnIsRaper && (xxx.is_rapist(pawn) || xxx.is_bloodlust(pawn)))
					//	currentThought = xxx.gave_virginity;
				}
				else if (xxx.is_insect(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithInsects);
					pawn.records.Increment(xxx.CountOfBeenRapedByInsects);
				}
				else if (xxx.is_animal(partner))
				{
					pawn.records.Increment(xxx.CountOfSexWithAnimals);
					pawn.records.Increment(xxx.CountOfBeenRapedByAnimals);
					//if (zoophile) currentThought = xxx.gave_virginity;
				}
				else
				{
					pawn.records.Increment(xxx.CountOfSexWithOthers);
					pawn.records.Increment(xxx.CountOfBeenRapedByOthers);
				}
			}

			//if (isVirginSex) //&& (sextype == rjwSextype.Vaginal || sextype == rjwSextype.DoublePenetration))
			//{
			//	ModLog.Message(xxx.get_pawnname(pawn) + " | " + xxx.get_pawnname(partner) + " | " + currentThought);
			//	ModLog.Message("1");
			//	if (!is_animal(partner))//passive
			//	{
			//		if (currentThought != null)
			//			partner.needs.mood.thoughts.memories.TryGainMemory(currentThought);
			//	}
			//	ModLog.Message("2");
			//	if (!is_animal(pawn))//active
			//	{
			//		currentThought = took_virginity;
			//		pawn.needs.mood.thoughts.memories.TryGainMemory(currentThought);
			//	}
			//}
		}

		/// <summary> 检查小人是否拥有动物恋物状态</summary>
		/// <returns> 如果小人拥有动物恋物则返回 true，没有动物恋物则返回 false。 </returns>
		public static bool AnimalParaphilia(Pawn pawn)
		{
			if (pawn.health.hediffSet.HasHediff(xxx.humanlikeParaphilia))
				return true;
			return false;
		}

		/// <summary> 返回动物恋物状态的当前阶段</summary>
		/// <returns> 当前严重度阶段对应的 int 值。 </returns>
		public static int AnimalParaphiliaStage(Pawn pawn)
		{
			if (AnimalParaphilia(pawn))
			{
				return pawn.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).CurStageIndex;
			}
			return 0;
		}

		/// <summary> 检查小人是否处于动物恋物的 Fixated 阶段。</summary>
		/// <returns> 如果小人处于动物恋物的 Fixated 阶段则返回 true；如果没有动物恋物或不在 Fixated 阶段则返回 false。 </returns>
		public static bool AnimalParaphiliaFixated(Pawn pawn)
		{
			if (AnimalParaphilia(pawn))
			{
				if (pawn.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).CurStageIndex == 3)
					return true;
				return false;
			}
			return false;
		}

		/// <summary> 检查小人是否处于动物恋物的 Obsessed 阶段。</summary>
		/// <returns> 如果小人处于动物恋物的 Fixated 阶段则返回 true；如果没有动物恋物或不在 Obsessed 阶段则返回 false。 </returns>
		public static bool AnimalParaphiliaObsessed(Pawn pawn)
		{
			if (AnimalParaphilia(pawn))
			{
				if (pawn.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).CurStageIndex == 4)
					return true;
				return false;
			}
			return false;
		}
		public static void processAnimalParaphilia(SexProps props)

		{
			Pawn pawn = props.pawn;
			Pawn partner = props.partner;
			//ModLog.Message("Initiated AnimalParaphilia");

			if (pawn is null)
			{
				//ModLog.Message("Pawn is null");
				return;
			}

			if (xxx.is_animal(pawn) && !pawn.Dead && xxx.is_human(partner) && !props.isRape)
			{
				//ModLog.Message("Animal is pawn");
				float ParaphiliaSeverityGain = xxx.humanlikeParaphilia.initialSeverity;
				//ModLog.Message("AnimalParaphilia severity " + xxx.humanlikeParaphilia.initialSeverity);

				if (!AnimalParaphilia(pawn) && (pawn.records.GetValue(xxx.CountOfSexWithHumanlikes) > 0))
				{
					pawn.health.AddHediff(xxx.humanlikeParaphilia);
					//ModLog.Message("Adding AnimalParaphilia to pawn");
					return;
				}

				else if (AnimalParaphilia(partner))
				{
					int animalParaphiliaStage = AnimalParaphiliaStage(pawn);
					//ModLog.Message("AnimalParaphilia current stage " + animalParaphiliaStage);
				}

					if (xxx.HasBond(pawn))
				{
					ParaphiliaSeverityGain *= 2.0f;
				}

				{
					pawn.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).Severity += ParaphiliaSeverityGain;
					//ModLog.Message("Adding AnimalParaphilia stage to pawn");
				}
			}
			else if (xxx.is_animal(partner) && !pawn.Dead && xxx.is_human(pawn) && !props.isRape)
			{
				//ModLog.Message("Animal is partner");
				float ParaphiliaSeverityGain = xxx.humanlikeParaphilia.initialSeverity;
				//ModLog.Message("AnimalParaphilia severity " + xxx.humanlikeParaphilia.initialSeverity);

				if (!AnimalParaphilia(partner) && (partner.records.GetValue(xxx.CountOfSexWithHumanlikes) > 0))
				{
					partner.health.AddHediff(xxx.humanlikeParaphilia);
					//ModLog.Message("Adding AnimalParaphilia to partner");
					return;
				}

				else if (AnimalParaphilia(partner))
				{
					int AnimalParaphiliaStage = partner.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).CurStageIndex;
					//ModLog.Message("AnimalParaphilia current stage " + AnimalParaphiliaStage);
				}

				if (xxx.HasBond(partner))
				{
					ParaphiliaSeverityGain *= 2.0f;
				}

				{
					partner.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).Severity += ParaphiliaSeverityGain;
					//ModLog.Message("Adding AnimalParaphilia stage to partner");
				}
			}
			
			else if (xxx.is_animal(pawn) && !pawn.Dead && xxx.is_animal(partner) && !props.isRape && AnimalParaphilia(pawn))		
			{
				pawn.health.hediffSet.GetFirstHediffOfDef(xxx.humanlikeParaphilia).Severity -= xxx.humanlikeParaphilia.initialSeverity;
				//ModLog.Message("Removing AnimalParaphilia stage to pawn");
			}
		
			else return;
		}
		//============→=====处理身体崩坏系统的部分===============→============
		public static bool BodyIsBroken(Pawn pawn)
		{
			return pawn.health.hediffSet.HasHediff(xxx.feelingBroken);
		}

		[SyncMethod]
		public static bool BadlyBroken(Pawn pawn, out Trait addedTrait)
		{
			addedTrait = null;
			if (!BodyIsBroken(pawn))
			{
				return false;
			}

			int stage = pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).CurStageIndex;
			if (stage >= 3)
			{
				//身体崩坏时让角色成为受虐狂
				//TODO：移除/替换社交/需求方面的减益
				if (RJWSettings.AddTrait_Masocist && !pawn.story.traits.allTraits.Any(T => T.def == TraitDef.Named("Masochist")))
				{
					var chance = 0.05f;
					if (Rand.Chance(chance))
					{
						addedTrait = new Trait(xxx.masochist);

						Trait rapist = pawn.story.traits.GetTrait(xxx.rapist);
						if (rapist != null)
						{
							if (rapist.sourceGene != null)
							{
								addedTrait.suppressedByGene = rapist.sourceGene;
							}
							else
							{
								pawn.story.traits.allTraits.Remove(pawn.story.traits.GetTrait(xxx.rapist));
								//ModLog.Message(xxx.get_pawnname(pawn) + " BadlyBroken, switch rapist -> masochist");
							}
						}

						for (int i = 0; i < pawn.story.traits.allTraits.Count(); i++)
						{
							if (!pawn.story.traits.allTraits[i].Suppressed && pawn.story.traits.allTraits[i].def.ConflictsWith(xxx.masochist))
							{
								if (pawn.story.traits.allTraits[i].sourceGene != null)
								{
									addedTrait.suppressedByGene = pawn.story.traits.allTraits[i].sourceGene;
								}
								else
								{
									pawn.story.traits.allTraits.Remove(pawn.story.traits.allTraits[i]);
									i--;
								}
							}
						}

						pawn.story.traits.GainTrait(addedTrait);
						//ModLog.Message(xxx.get_pawnname(pawn) + " BadlyBroken, not masochist, adding masochist trait");

						if (xxx.is_masochist(pawn))
						{
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(got_raped);
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(got_licked);
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(hate_my_rapist);
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(allowed_me_to_get_raped);
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(got_anal_raped);
							pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(got_anal_raped_byfemale);
						}
					}
				}
				if (pawn.IsPrisonerOfColony)
				{
					pawn.guest.resistance = Mathf.Max(pawn.guest.resistance - 1f, 0f);
					//ModLog.Message(xxx.get_pawnname(pawn) + " BadlyBroken, reduce prisoner resistance");
				}
			}
			return stage > 1;
		}

		public static float CalcBrokenSeverityGain(Pawn pawn)
		{
			float brokenSeverityGain = xxx.feelingBroken.initialSeverity;

			if (xxx.RoMIsActive)
				if (pawn.story.traits.HasTrait(xxx.Succubus))
					brokenSeverityGain *= 0.25f;

			if (pawn.story.traits.HasTrait(VanillaTraitDefOf.Tough))
			{
				brokenSeverityGain *= 0.5f;
			}
			if (pawn.story.traits.HasTrait(TraitDefOf.Wimp))
			{
				brokenSeverityGain *= 2.0f;
			}
			if (pawn.story.traits.HasTrait(VanillaTraitDefOf.Nerves))
			{
				int currentNervesDegree = pawn.story.traits.DegreeOfTrait(VanillaTraitDefOf.Nerves);

				switch (currentNervesDegree)
				{
					case -2:
						brokenSeverityGain *= 2.0f;
						break;
					case -1:
						brokenSeverityGain *= 1.5f;
						break;
					case 1:
						brokenSeverityGain *= 0.5f;
						break;
					case 2:
						brokenSeverityGain *= 0.25f;
						break;
				}
			}

			return brokenSeverityGain;
		}

		private static void IncreaseBrokenSeverity(Pawn pawn, float  brokenSeverityGain)
		{
			pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).Severity += brokenSeverityGain;
		}

		//为卵添加变体？
		public static void processBrokenPawn(Pawn pawn, List<Trait> addedTraits)
		{
			// 在强暴/繁殖后调用
			if (pawn is null)
				return;

			if (xxx.is_human(pawn) && !pawn.Dead && pawn.records != null)
			{
				if (xxx.has_traits(pawn))
				{
					if (xxx.is_slime(pawn))
						return;

					if (!BodyIsBroken(pawn))
						pawn.health.AddHediff(xxx.feelingBroken);
					else
					{
						float brokenSeverityGain = CalcBrokenSeverityGain(pawn);
						int feelingBrokenStage = pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken).CurStageIndex;

						if (pawn.story.traits.HasTrait(VanillaTraitDefOf.Nerves))
						{
							int currentNervesDegree = pawn.story.traits.DegreeOfTrait(VanillaTraitDefOf.Nerves);

							if (RJWSettings.AddTrait_Nerves)
							{
								int newNervesDegree = 0;
								if (feelingBrokenStage >= 3 && currentNervesDegree > -2)
								{
									newNervesDegree = -2;
								}
								else if (feelingBrokenStage >= 2 && currentNervesDegree > -1)
								{
									newNervesDegree = -1;
								}

								if (newNervesDegree < 0)
								{
									pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(VanillaTraitDefOf.Nerves));
									Trait newNerves = new Trait(VanillaTraitDefOf.Nerves, newNervesDegree);
									pawn.story.traits.GainTrait(newNerves);
									addedTraits.Add(newNerves);
								}
							}
						}
						else if (RJWSettings.AddTrait_Nerves && feelingBrokenStage > 1)
						{
							pawn.story.traits.GainTrait(new Trait(VanillaTraitDefOf.Nerves, -1));
							Trait nervous = new Trait(VanillaTraitDefOf.Nerves, -1);
							pawn.story.traits.GainTrait(nervous);
							addedTraits.Add(nervous);

						}

						IncreaseBrokenSeverity(pawn, brokenSeverityGain);
					}

					BadlyBroken(pawn, out Trait newMasochistTrait);
					if (newMasochistTrait != null)
					{
						addedTraits.Add(newMasochistTrait);
					}
				}
			}
		}

		//============→=====处理身体崩坏系统的部分===============→============
	}
}
