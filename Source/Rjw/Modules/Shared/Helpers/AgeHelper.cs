#nullable disable

﻿using System;
using UnityEngine;
using Verse;

namespace rjw.Modules.Shared.Helpers
{
	public static  class AgeHelper
	{
		public const int ImmortalRaceAgeClamp = 25;
		public const int NonHumanRaceAgeClamp = 25;

		public static int ScaleToHumanAge(Pawn pawn, int humanLifespan = 80)
		{
			float pawnAge = pawn.ageTracker.AgeBiologicalYearsFloat;

			if (pawn.def.defName == "Human") return (int)pawnAge; // 人类，无需缩放任何东西。

			float lifeExpectancy = pawn.RaceProps.lifeExpectancy;

			if (RJWSettings.UseAdvancedAgeScaling == true)
			{
				//伪不死与不死种族
				if (lifeExpectancy >= 500)
				{
					return CalculateForImmortals(pawn, humanLifespan);
				}

				if (lifeExpectancy != humanLifespan)
				{
					//其他种族
					return CalculateForNonHuman(pawn, humanLifespan);
				}
			}

			float ageScaling = humanLifespan / lifeExpectancy;
			float scaledAge = pawnAge * ageScaling;

			return (int)Mathf.Max(scaledAge, 1);
		}

		private static int CalculateForImmortals(Pawn pawn, int humanLifespan)
		{
			float age = pawn.ageTracker.AgeBiologicalYearsFloat;
			float lifeExpectancy = pawn.RaceProps.lifeExpectancy;
			float growth = pawn.ageTracker.Growth;

			//成长与 hack
			{
				growth = ImmortalGrowthHacks(pawn, age, growth);

				if (growth < 1)
				{
					return (int)Mathf.Lerp(0, ImmortalRaceAgeClamp, growth);
				}
			}

			//曲线
			{
				float life = age / lifeExpectancy;

				//假设：寿命极长的种族在寿命结束前看起来都“年轻”
				if (life < 0.9f)
				{
					return ImmortalRaceAgeClamp;
				}

				return (int)Mathf.LerpUnclamped(ImmortalRaceAgeClamp, humanLifespan, Mathf.InverseLerp(0.9f, 1, life));
			}
		}

		private static float ImmortalGrowthHacks(Pawn pawn, float age, float originalGrowth)
		{
			if (pawn.ageTracker.CurLifeStage.reproductive == false)
			{
				//希望处于生育生命阶段就意味着我们面对的是成年个体
				return Math.Min(1, age / ImmortalRaceAgeClamp);
			}

			return 1;
		}

		private static int CalculateForNonHuman(Pawn pawn, int humanLifespan)
		{
			float age = pawn.ageTracker.AgeBiologicalYearsFloat;
			float lifeExpectancy = pawn.RaceProps.lifeExpectancy;
			float growth = pawn.ageTracker.Growth;

			//成长与 hack
			{
				growth = NonHumanGrowthHacks(pawn, age, growth);

				if (growth < 1)
				{
					return (int)Mathf.Lerp(0, NonHumanRaceAgeClamp, growth);
				}
			}

			//曲线
			{
				float life = age / lifeExpectancy;

				return (int)Mathf.LerpUnclamped(NonHumanRaceAgeClamp, humanLifespan, life);
			}
		}

		private static float NonHumanGrowthHacks(Pawn pawn, float age, float originalGrowth)
		{
			if (pawn.ageTracker.CurLifeStage.reproductive == false)
			{
				//希望处于生育生命阶段就意味着我们面对的是成年个体
				return Math.Min(1, age / NonHumanRaceAgeClamp);
			}

			return 1;
		}

	}
}
