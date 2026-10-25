#nullable disable

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using Multiplayer.API;
using System.Linq;

namespace rjw
{
	public static class Nymph_Generator
	{
		/// <summary>
		/// 如果给定的 pawnGenerationRequest 是针对花痴 pawnKind 的，则返回 true。
		/// </summary>
		public static bool IsNymph(PawnGenerationRequest pawnGenerationRequest)
		{
			return pawnGenerationRequest.KindDef != null && pawnGenerationRequest.KindDef.defName.Contains("Nymph") && pawnGenerationRequest.KindDef.RaceProps.Humanlike;
		}

		private static bool is_trait_conflicting_or_duplicate(Pawn pawn, Trait t)
		{
			foreach (var existing in pawn.story.traits.allTraits)
				if ((existing.def == t.def) || (t.def.ConflictsWith(existing)))
					return true;
			return false;
		}

		public static bool IsNymphBodyType(Pawn pawn)
		{
			return pawn.story.bodyType == BodyTypeDefOf.Female || pawn.story.bodyType == BodyTypeDefOf.Thin;
		}

		[SyncMethod]
		public static Gender RandomNymphGender()
		{
			//即使雄性设为 100% 也仍是 99%，因为我太懒了不想修
			//float rnd = Rand.Value;
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			float chance = RJWSettings.male_nymph_chance;

			Gender Pawngender = Rand.Chance(chance) ? Gender.Male: Gender.Female;
			//ModLog.Message(" setnymphsex: " + (rnd < chance) + " rnd:" + rnd + " chance:" + chance);
			//ModLog.Message(" setnymphsex: " + Pawngender);
			return Pawngender;
		}

		/// <summary>
		/// 替换小人随机生成的背景故事和特质，把它变成合格的花痴
		/// </summary>
		[SyncMethod]
		public static void set_story(Pawn pawn)
		{
			//C# rjw 背景故事
			nymph_backstories.generate(pawn);

			//More Trait Slots 模组会调整小人可获得的特质数量上限，因此，
			//在添加 nymph_story 特质之后，我需要收集小人的特质，并把 other_traits 重新赋回给小人。

			//备份生成的 + rjw 特质
			Stack<Trait> other_traits = new Stack<Trait>();
			int numberOfTotalTraits = 0;
			if (!pawn.story.traits.allTraits.NullOrEmpty())
			{
				foreach (Trait t in pawn.story.traits.allTraits)
				{
					other_traits.Push(t);
					++numberOfTotalTraits;
				}
			}
			pawn.story.traits.allTraits.Clear();

			//xml 花痴背景故事强制的特质
			var trait_count = 0;
			foreach (var t in pawn.story.Adulthood.forcedTraits)
			{
				pawn.story.traits.GainTrait(new Trait(t.def, t.degree));
				++trait_count;
			}

			//xml 背景故事 + rjw 备份 + 生成器生成的任意备份
			while (trait_count < numberOfTotalTraits)
			{
				Trait t = other_traits.Pop();
				if (!is_trait_conflicting_or_duplicate(pawn, t))
					pawn.story.traits.GainTrait(t);
				++trait_count;
			}

			// 给残缺花痴添加残缺身体状态
			if (pawn.story.Adulthood.defName.Contains("broken"))
			{
				pawn.health.AddHediff(xxx.feelingBroken);
				(pawn.health.hediffSet.GetFirstHediffOfDef(xxx.feelingBroken)).Severity = Rand.Range(0.4f, 1.0f);
			}

			//重置被禁用的技能／工作
			pawn.skills.Notify_SkillDisablesChanged();
			pawn.Notify_DisabledWorkTypesChanged();
		}

		[SyncMethod]
		private static int sum_previous_gains(SkillDef def, Pawn_StoryTracker sto, Pawn_AgeTracker age)
		{
			int total_gain = 0;
			SkillGain gain;

			// 来自背景故事的收益
			if((gain = sto.Childhood.skillGains.FirstOrDefault((SkillGain skillGain) => skillGain.skill == def )) != null)
				total_gain += gain.amount;
			if ((gain = sto.Adulthood.skillGains.FirstOrDefault((SkillGain skillGain) => skillGain.skill == def)) != null)
				total_gain += gain.amount;

			// 来自特质的收益
			foreach (var trait in sto.traits.allTraits)
				if ((gain = trait.CurrentData.skillGains.FirstOrDefault((SkillGain skillGain) => skillGain.skill == def)) != null)
					total_gain += gain.amount;

			// 来自年龄的收益
			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			var rgain = Rand.Value * (float)total_gain * 0.35f;
			var age_factor = Mathf.Clamp01((age.AgeBiologicalYearsFloat - 17.0f) / 10.0f); // 假设花痴的年龄是 17~27 岁
			total_gain += (int)(age_factor * rgain);

			return Mathf.Clamp(total_gain, 0, 20);
		}

		/// <summary>
		/// 根据背景故事、特质和年龄设置花痴的初始技能 &amp; 热情
		/// </summary>
		[SyncMethod]
		public static void set_skills(Pawn pawn)
		{
			foreach (var skill_def in DefDatabase<SkillDef>.AllDefsListForReading)
			{
				var rec = pawn.skills.GetSkill(skill_def);
				if (!rec.TotallyDisabled)
				{
					//Rand.PopState();
					//Rand.PushState(RJW_Multiplayer.PredictableSeed());
					rec.Level = sum_previous_gains(skill_def, pawn.story, pawn.ageTracker);
					rec.xpSinceLastLevel = rec.XpRequiredForLevelUp * Rand.Range(0.10f, 0.90f);

					var pas_cha = nymph_backstories.get_passion_chances(pawn.story.Childhood, pawn.story.Adulthood, skill_def);
					var rv = Rand.Value;
					if (rv < pas_cha.major) rec.passion = Passion.Major;
					else if (rv < pas_cha.minor) rec.passion = Passion.Minor;
					else rec.passion = Passion.None;
				}
				else
					rec.passion = Passion.None;
			}
		}

		[SyncMethod]
		public static PawnKindDef GetFixedNymphPawnKindDef()
		{
			var def = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.defName.Contains("Nymph")).RandomElement();
			//var def = PawnKindDef.Named("Nymph");
			// XML 里是 18，但某物把它覆盖成了 5。
			//def.minGenerationAge = 18;
			return def;
		}

		[SyncMethod]
		public static Pawn GenerateNymph(Faction faction = null)
		{
			// 花痴的大部分特殊属性都位于对 PawnGenerator 的 Harmony 补丁中。
			PawnGenerationRequest request = new PawnGenerationRequest(
				kind: GetFixedNymphPawnKindDef(),
				faction: faction,
				forceGenerateNewPawn: true,
				canGeneratePawnRelations: true,
				colonistRelationChanceFactor: 0.0f,
				relationWithExtraPawnChanceFactor: 0
				);

			//Rand.PopState();
			//Rand.PushState(RJW_Multiplayer.PredictableSeed());
			Pawn pawn = PawnGenerator.GeneratePawn(request);
			//ModLog.Message(""+ pawn.Faction);
			//IntVec3 spawn_loc = CellFinder.RandomClosewalkCellNear(around_loc, map, 6);//RandomSpawnCellForPawnNear could be an alternative
			//GenSpawn.Spawn(pawn, spawn_loc, map);
			return pawn;
		}
	}
}
