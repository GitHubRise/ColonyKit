#nullable disable

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace rjw
{
	/// <summary>
	/// 根据小人的年龄与生育力来源计算其生育力
	/// </summary>
	public class PawnCapacityWorker_Fertility : PawnCapacityWorker
	{
		public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
		{
			Pawn pawn = diffSet.pawn;
			var parts = pawn.GetGenitalsList();

			if (!Genital_Helper.has_penis_fertile(pawn, parts) && !Genital_Helper.has_vagina(pawn, parts))
				return 0;

			if (Genital_Helper.has_ovipositorF(pawn, parts) || Genital_Helper.has_ovipositorM(pawn, parts))
				return 0;

			//ModLog.Message("PawnCapacityWorker_Fertility::CalculateCapacityLevel is called for: " + xxx.get_pawnname(pawn));

			if (!pawn.RaceHasFertility())
			{
				//ModLog.Message(" Fertility_filter, no fertility for : " + pawn.kindDef.defName);
				return 0f;
			}

			//仿生人只有使用超凡科技部件时才具有生育力
			if (AndroidsCompatibility.IsAndroid(pawn) && !(AndroidsCompatibility.AndroidPenisFertility(pawn) || AndroidsCompatibility.AndroidVaginaFertility(pawn)))
			{
				//ModLog.Message(" Android has no archotech genitals set fertility to 0 for: " + pawn.kindDef.defName);
				return 0f;
			}

			float result = 1;

			if (RJWPregnancySettings.UseVanillaPregnancy && pawn.RaceProps.Humanlike)
			{
				float fertilityStat = pawn.GetStatValue(StatDefOf.Fertility);
				if (fertilityStat != 1)
				{
					result *= fertilityStat;
					impactors?.Add(new CapacityImpactorVanillaFertility());
				}
			}
			else
			{
				float ageFactor = CalculateAgeImpact(pawn);
				if (ageFactor != 1)
				{
					result *= ageFactor;
					impactors?.Add(new CapacityImpactorAge());
				}
			}
			
			result *= PawnCapacityUtility.CalculateTagEfficiency(diffSet, BodyPartTagDefOf.RJW_Fertility, 1f, FloatRange.ZeroToOne, impactors);

			//ModLog.Message("PawnCapacityWorker_Fertility::CalculateCapacityLevel result is: " + result);
			return result;

		}

		public override bool CanHaveCapacity(BodyDef body)
		{
			return body.HasPartWithTag(BodyPartTagDefOf.RJW_Fertility);
		}

		private float CalculateAgeImpact(Pawn pawn)
		{
			RaceProperties race = pawn.RaceProps;
			float startAge = 0f;	//提高生育力
			float startMaxAge = 0f;	//最大生育力
			float endAge = race.lifeExpectancy * (RJWPregnancySettings.fertility_endage_male * 0.7f); // 男性开始丧失生殖能力的年龄。
			float zeroFertility = race.lifeExpectancy * RJWPregnancySettings.fertility_endage_male;	// 生育力降至 0% 的年龄。

			if (xxx.is_female(pawn))
			{
				if (xxx.is_animal(pawn))
				{
					endAge = race.lifeExpectancy * (RJWPregnancySettings.fertility_endage_female_animal * 0.6f);
					zeroFertility = race.lifeExpectancy * RJWPregnancySettings.fertility_endage_female_animal;
				}
				else
				{
					endAge = race.lifeExpectancy * (RJWPregnancySettings.fertility_endage_female_humanlike * 0.6f); // 生育力开始下降的年龄。
					zeroFertility = race.lifeExpectancy * RJWPregnancySettings.fertility_endage_female_humanlike; // 生育力降至 0% 的年龄。
				}
			}
			//若小人是动物，第一个可繁殖的生命阶段（通常）是成年，因此把 startMaxAge 设为第一个可繁殖阶段，把 startAge 设为其前一个阶段。 
			int adult;
			if (xxx.is_animal(pawn) && (adult = race.lifeStageAges.FirstIndexOf((ls) => ls.def.reproductive)) > 0)
			{
				startAge = race.lifeStageAges[adult - 1].minAge;
				startMaxAge = race.lifeStageAges[adult].minAge;
			}
			else
			{
				foreach (LifeStageAge lifestage in race.lifeStageAges)
				{
					if (lifestage.def.reproductive)
						//推测为青少年阶段
						if (startAge == 0f && startMaxAge == 0f)
						{
							startAge = lifestage.minAge;
							startMaxAge = (Mathf.Max(startAge + (startAge + endAge) * 0.08f, startAge));
						}
						//推测为成年阶段
						else
						{
							if (startMaxAge > lifestage.minAge) // 确保生育力峰值保持在成年前或稍早的阶段
								startMaxAge = lifestage.minAge;
						}
				}
			}
			//ModLog.Message(" Fertility ages for " + pawn.Name + " are: " + startAge + ", " + startMaxAge + ", " + endAge + ", " + endMaxAge);

			return GenMath.FlatHill(startAge, startMaxAge, endAge, zeroFertility, pawn.ageTracker.AgeBiologicalYearsFloat);
		}

		// 提示信息被硬编码为只使用原版具体的 CapacityImpactor（?!），所以我们需要继承一个
		// 才能显示内容，而不必做多余的打补丁（也可能避免踩到任何
		// 恰好也改动这些提示信息的模组的脚）。CapacityImpactorPain 没有任何字段或专用方法，
		// 所以我们就用它。
		public class CapacityImpactorAge : PawnCapacityUtility.CapacityImpactorPain
		{
			public override string Readable(Pawn pawn)
			{
				return "AgeImpactor".Translate() + pawn.ageTracker.AgeBiologicalYearsFloat.ToStringApproxAge();
			}
		}

		public class CapacityImpactorVanillaFertility : PawnCapacityUtility.CapacityImpactorPain
		{
			public override string Readable(Pawn pawn)
			{
				return "VanillaFertilityImpactor".Translate(
					((int) (pawn.GetStatValue(StatDefOf.Fertility) * 100)).Named("VALUE")
				);
			}
		}
	}
}
 
