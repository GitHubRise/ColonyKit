#nullable disable

﻿using RimWorld;
using rjw.Modules.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace rjw
{
	public static class PawnExtensions
	{
		public static bool RaceHasFertility(this Pawn pawn)
		{
			// 默认为 true。
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
				return raceGroupDef.hasFertility;
			return true;
		}

		public static bool RaceHasPregnancy(this Pawn pawn)
		{
			// 默认为 true。
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
				return raceGroupDef.hasPregnancy;
			return true;
		}

		public static bool RaceHasOviPregnancy(this Pawn pawn)
		{
			// 默认为 false。
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
				return raceGroupDef.oviPregnancy;
			return false;
		}

		public static bool RaceImplantEggs(this Pawn pawn)
		{
			// 默认为 false。
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
				return raceGroupDef.ImplantEggs;
			return false;
		}

		public static bool RaceHasSexNeed(this Pawn pawn)
		{
			// 默认为 true。
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
				return raceGroupDef.hasSexNeed;
			return true;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryAddRacePart(this Pawn pawn, BodyPartRecord partBPR, Gender gender = Gender.None)
		{
			return RaceGroupDef_Helper.TryAddRacePart(pawn, partBPR, gender);
		}

		public static bool Has(this Pawn pawn, RaceTagDef tag)
		{
			if (RaceGroupDef_Helper.TryGetRaceGroupDef(pawn, out var raceGroupDef))
			{
				return raceGroupDef.tags != null && raceGroupDef.tags.Contains(tag);
			}
			else
			{
				return tag.DefaultWhenNoRaceGroupDef(pawn);
			}
		}

		/// <summary>
		/// 检查该小人是否对另一个小人存有特定的社交记忆。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="other">另一个小人。</param>
		/// <param name="thought">要检查的想法定义。</param>
		/// <returns>该小人是否存有该记忆。</returns>
		public static bool HasMemoryWith(this Pawn pawn, Pawn other, ThoughtDef thought)
		{
			if (pawn.needs.mood?.thoughts.memories.Memories is { } memories)
				foreach (var memory in memories)
					if (memory.otherPawn == other)
						if (memory.def == thought)
							return true;
			return false;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSexyRobot(this Pawn pawn)
		{
			return AndroidsCompatibility.IsAndroid(pawn);
		}

		// 理论上我认为这里应该涉及 RaceGroupDef。
		public static bool IsUnsexyRobot(this Pawn pawn)
		{
			return !IsSexyRobot(pawn)
				&& (xxx.is_mechanoid(pawn) || pawn.kindDef.race.defName.ToLower().Contains("droid"));
		}
		
		/// <summary>
		/// <para>检查该小人是否具有动物智力。</para>
		/// <para>我们通常将所有具有此智力的小人视为动物。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该小人是否具有动物智力。</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAnimal(this Pawn pawn) => xxx.is_animal(pawn);

		/// <summary>
		/// <para>检查该小人是否具有类人智力。</para>
		/// <para>对我们认为足够"人类"的所有小人而言，这通常为真。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该小人是否具有类人智力。</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsHumanLike(this Pawn pawn) => xxx.is_human(pawn);

		/// <summary>
		/// <para>检查该小人是否与另一个小人属于同一"物种"。</para>
		/// <para>这会考虑小人的基因。是的，我知道异种人
		/// 并不代表不同物种，但这里更关心的是我们能否对
		/// 两个小人之间的身体差异作出假设。</para>
		/// </summary>
		/// <param name="pawn">第一个小人。</param>
		/// <param name="other">第二个小人。</param>
		/// <returns>这两个小人是否属于同一物种。</returns>
		public static bool IsSameSpecies(this Pawn pawn, Pawn other)
		{
			// 不同种族不是同一物种。
			if (pawn.kindDef.race != other.kindDef.race) return false;
			// 仅在相关时才比较基因。
			if (!ModsConfig.BiotechActive) return true;

			var pGeneTracker = pawn.genes;
			var oGeneTracker = other.genes;

			// 两者都不是类人，因此为同一物种。
			if (pGeneTracker is null && oGeneTracker is null) return true;
			// 一个是类人，另一个不是，因此不是同一物种。
			if (pGeneTracker is null || oGeneTracker is null) return false;

			// 相同的异种人类型被视为同一物种。
			if (pGeneTracker.Xenotype == oGeneTracker.Xenotype) return true;

			// 以上就是我们能做的所有快速检查。现在我们得动真格，
			// 直接比较基因。
			var pGenes = pGeneTracker.GenesListForReading.Select(ToGeneDef).ToHashSet();
			return pGenes.SetEquals(oGeneTracker.GenesListForReading.Select(ToGeneDef));

			static GeneDef ToGeneDef(Gene gene) => gene.def;
		}

		/// <summary>
		/// 检查在观察者眼中该小人是否被视为毁容。
		/// </summary>
		/// <param name="observed">被观察的小人。</param>
		/// <param name="observer">进行观察的小人。</param>
		/// <returns>该小人是否被视为毁容。</returns>
		public static bool IsDisfiguredTo(this Pawn observed, Pawn observer)
		{
			var okWithBlindness = observer.Ideo?.IdeoApprovesOfBlindness() is true;
			return RelationsUtility.IsDisfigured(observed, observer, okWithBlindness);
		}

		/// <summary>
		/// 检查该小人是否会猎杀人类。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该小人是否会猎杀人类。</returns>
		public static bool IsManhunter(this Pawn pawn)
		{
			if (pawn?.InMentalState is not true) return false;
			if (pawn.MentalStateDef == MentalStateDefOf.Manhunter) return true;
			if (pawn.MentalStateDef == MentalStateDefOf.ManhunterPermanent) return true;
			return false;
		}

		public static bool IsPoly(this Pawn pawn)
		{
			if (!pawn.IsHumanLike())
			{
				return true;
			}
			if (pawn.IsIdeologicallyPoly())
			{
				return true;
			}
			if (pawn.story?.traits == null)
			{
				return false;
			}
			if (xxx.AnyPolyamoryModIsActive && pawn.story.traits.HasTrait(xxx.polyamorous))
			{
				return true;
			}
			if (xxx.AnyPolygamyModIsActive && pawn.story.traits.HasTrait(xxx.polygamous))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// <para>检查该小人是否为已驯服的动物。</para>
		/// <para>这并不专指玩家派系。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该小人是否为已驯服的动物。</returns>
		public static bool IsTameAnimal(this Pawn pawn) =>
			pawn.IsAnimal() && pawn.Faction?.def.humanlikeFaction is true;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsVisiblyPregnant(this Pawn pawn)
		{
			return pawn.IsPregnant(true);
		}

		public static bool IsPregnant(this Pawn pawn, bool mustBeVisible = false)
		{
			var set = pawn.health.hediffSet;
			return set.HasHediff(HediffDefOf.PregnantHuman, mustBeVisible) || 
				set.HasHediff(HediffDefOf.Pregnant, mustBeVisible) ||
				Hediff_BasePregnancy.KnownPregnancies().Any(x => set.HasHediff(HediffDef.Named(x), mustBeVisible));
		}

		/// <summary>
		/// 检查该小人是否与给定小人存在心灵恋爱联结。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="other">另一个小人。</param>
		/// <returns>他们是否会一起进行心灵恋爱。</returns>
		public static bool HasPsychicLoveWith(this Pawn pawn, Pawn other) {
			if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicLove) is HediffWithTarget hediff)
				return hediff.target == other;
			return false;
		}

		/// <summary>
		/// 检查该小人是否戴着"啤酒眼镜"，即是否喝醉了。
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <returns>该小人是否喝醉。</returns>
		public static bool HasBeerGoggles(this Pawn pawn) =>
			pawn.health.hediffSet.HasHediff(HediffDefOf.AlcoholHigh);

		/// <summary>
		/// <para>检查该小人是否戴着"啤酒眼镜"，即是否喝醉了。</para>
		/// <para>此变体还会获取严重程度。</para>
		/// </summary>
		/// <param name="pawn">要检查的小人。</param>
		/// <param name="severity">用于存储严重程度的变量。</param>
		/// <returns>该小人是否喝醉。</returns>
		public static bool HasBeerGoggles(this Pawn pawn, out float severity)
		{
			severity = 0f;
			var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
			if (hediff is null) return false;
			severity = hediff.Severity;
			return true;
		}

		public static List<Hediff> GetGenitalsList(this Pawn pawn)
		{
			List<Hediff> set;
			try//世界生成时会出错
			{
				set = pawn.GetRJWPawnData().genitals;
				if (set.NullOrEmpty())
				{
					var partBPR = Genital_Helper.get_genitalsBPR(pawn);
					set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
					pawn.GetRJWPawnData().genitals = set;
				}

			}
			catch
			{
				var partBPR = Genital_Helper.get_genitalsBPR(pawn);
				set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
			}
			return set;
		}

		public static List<Hediff> GetBreastList(this Pawn pawn)
		{
			List<Hediff> set;
			try//世界生成时会出错
			{
				set = pawn.GetRJWPawnData().breasts;
				if (set.NullOrEmpty())
				{
					var partBPR = Genital_Helper.get_breastsBPR(pawn);
					set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
					pawn.GetRJWPawnData().breasts = set;
				}

			}
			catch
			{
				var partBPR = Genital_Helper.get_breastsBPR(pawn);
				set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
			}
			return set;
		}
		public static List<Hediff> GetAnusList(this Pawn pawn)
		{
			List<Hediff> set;
			try//世界生成时会出错
			{
				set = pawn.GetRJWPawnData().anus;
				if (set.NullOrEmpty())
				{
					var partBPR = Genital_Helper.get_anusBPR(pawn);
					set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
					pawn.GetRJWPawnData().anus = set;
				}

			}
			catch
			{
				var partBPR = Genital_Helper.get_anusBPR(pawn);
				set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
			}
			return set;
		}

		public static List<Hediff> GetTorsoList(this Pawn pawn)
		{
			List<Hediff> set;
			try//世界生成时会出错
			{
				set = pawn.GetRJWPawnData().torso;
				if (set.NullOrEmpty())
				{
					var partBPR = Genital_Helper.get_torsoBPR(pawn);
					set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
					pawn.GetRJWPawnData().torso = set;
				}

			}
			catch
			{
				var partBPR = Genital_Helper.get_torsoBPR(pawn);
				set = Genital_Helper.get_PartsHediffList(pawn, partBPR);
			}
			return set;
		}

#nullable enable
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SexProps? GetRMBSexPropsCache(this Pawn pawn)
		{
			return pawn.GetRJWPawnData()?.SexProps;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PawnData? GetRJWPawnData(this Pawn pawn)
		{
			return pawn.GetCompRJW()?.PawnData;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasParts(this Pawn pawn)
		{
			return (LewdPartHelper.GetLewdParts(pawn)?.AllParts?.Count ?? 0) > 0;
		}

		public static AgeCategory GetAgeCategory(this Pawn pawn)
		{
			// 本不该出现这种情况，但模组作者总能别出心裁
			if (pawn.ageTracker is not Pawn_AgeTracker age)
			{
				return AgeCategory.Child;
			}

			if (pawn.GetRJWPawnData()?.RaceSupportDef is RaceGroupDef raceGroup)
			{
				if (raceGroup.adultAge != 0 && age.AgeBiologicalYears >= raceGroup.adultAge)
				{
					return AgeCategory.Adult;
				}
				if (raceGroup.teenAge != 0 && age.AgeBiologicalYears >= raceGroup.teenAge)
				{
					return RJWSettings.AllowYouthSex ? AgeCategory.Youth : AgeCategory.Child;
				}
			}

			// 原版机械族的 reproductive = false
			if (xxx.is_mechanoid(pawn))
			{
				return AgeCategory.Adult;
			}

			// age.Adult 没有用，因为它在以下情况下为 true：
			// 1. 对类人而言，当达到第一个 developmentalStage.Adult 的生命阶段时为 true。人类是 13 岁
			// 2. 如果 raceProps 的 lifeStageAges 为空，则始终为 true
			// 3. 其他所有情况下——如果小人处于最后一个生命阶段
			// 对人类而言它过早变为 true。对动物而言可能过晚

			if (age.Growth > 0.999f)
			{
				return AgeCategory.Adult;
			}

			var stages = pawn.def.race.lifeStageAges;
			int index = -1;
			for (int i = 0; i < stages.Count; i++)
			{

				if (stages[i].def.defName.IndexOf("adult", StringComparison.OrdinalIgnoreCase) >= 0)
				{
						index = i;
						break;
				}
			}

			if (index != -1 && age.CurLifeStageIndex >= index)
			{
				return AgeCategory.Adult;
			}

			if (age.CurLifeStage?.reproductive ?? true)
			{
				return RJWSettings.AllowYouthSex ? AgeCategory.Youth : AgeCategory.Child;
			}

			return AgeCategory.Child;
		}
	}
}
