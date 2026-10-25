#nullable disable

using System.Collections.Generic;
using Verse;
using System;

namespace rjw
{
	/// <summary>
	/// 定义与特定种族或种族组相关的所有 RJW 配置。
	/// 核心种族应在 RJW 中提供 RaceGroupDef。
	/// 非核心种族应在独立的 RJWRaceSupport 模组中提供 RaceGroupDef。
	/// 严格来说任何模组都可以为自己的种族添加 RaceGroupDef，但这种情况应该很少。
	/// </summary>
	public class RaceGroupDef : Def
	{
		public List<string> raceNames = null;
		public List<string> pawnKindNames = null;

		public List<PartAdder> partAdders;
		public List<string> anuses = null;
		public List<float> chanceanuses = null;
		public List<string> femaleBreasts = null;
		public List<float> chancefemaleBreasts = null;
		public List<string> femaleGenitals = null;
		public List<float> chancefemaleGenitals = null;
		public List<string> maleBreasts = null;
		public List<float> chancemaleBreasts = null;
		public List<string> maleGenitals = null;
		public List<float> chancemaleGenitals = null;

		public List<string> hybridRaceParents = null;
		public List<string> hybridChildKindDef = null;
		public List<string> backstoryChildTribal = null;
		public List<string> backstoryChildCivil = null;

		public List<RaceTagDef> tags = null;
		public bool hasSingleGender = false;
		public bool hasSexNeed = true;
		public bool hasFertility = true;
		public bool hasPregnancy = true;
		public bool oviPregnancy = false;
		public bool ImplantEggs = false;
		public bool isDemon = false;
		public bool isSlime = false;
		public float raceSexDrive = 1.0f;

		public string eggFertilizedDef = "RJW_EggFertilized";
		public string eggUnfertilizedDef = "RJW_EggUnfertilized";
		public float eggProgressUnfertilizedMax = 1.0f;
		public float eggLayIntervalDays = 3.5f;

		/// <summary>
		/// 限制人类/动物可以进行性行为的最小年龄
		/// </summary>
		public int teenAge;
		public int adultAge;
		//public int pPregnancyGrowth = 12;

		//public float eggCountRange = 3.5f;
		//public float eggFertilizationCountMax = 3.5f;
		//public float eggLayFemaleOnly = 3.5f;

		public List<string> GetRacePartDefNames(Pawn pawn, BodyPartRecord bpr, Gender gender = Gender.None)
		{
			if (gender == Gender.Female)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return anuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					return femaleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					return femaleGenitals;
				}
			}
			else if (gender == Gender.Male)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return anuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					return maleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					return maleGenitals;
				}
			}
			else// (pawn.gender == Gender.None)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return anuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					//List<string> mergedList = femaleBreasts.Concat(maleBreasts).ToList();
					return femaleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					//List<string> mergedList = femaleGenitals.Concat(maleGenitals).ToList();
					return femaleGenitals;
				}
			}
			ModLog.Error("Unrecognized GetRacePartDefNames: {" + bpr + "}");
			return null;
		}

		public List<float> GetChances(Pawn pawn, BodyPartRecord bpr, Gender gender = Gender.None)
		{
			if (gender == Gender.Female)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return chanceanuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					return chancefemaleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					return chancefemaleGenitals;
				}
			}
			else if (gender == Gender.Male)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return chanceanuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					return chancemaleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					return chancemaleGenitals;
				}
			}
			else// (pawn.gender == Gender.None)
			{
				if (bpr.def == xxx.anusDef || bpr.def == xxx.mechanusDef)
				{
					return chanceanuses;
				}
				if (bpr.def == xxx.breastsDef || bpr.def == xxx.mechbreastsDef)
				{
					return chancefemaleBreasts;
				}
				if (bpr.def == xxx.genitalsDef || bpr.def == xxx.mechgenitalsDef)
				{
					return chancefemaleGenitals;
				}
			}
			ModLog.Error("Unrecognized GetChances: {" + bpr + "}");
			return null;
		}
	}
}
