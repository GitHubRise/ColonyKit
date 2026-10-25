#nullable enable

using System.Collections.Generic;
using Verse;

namespace rjw
{
	[StaticConstructorOnStartup]
	public class HediffDef_EnemyImplants : HediffDef
	{
		//单亲卵
		public string parentDef = "";
		//多亲卵
		public List<string> parentDefs = new();

		//用于植入卵
		public bool CanHaveParent(string defName)
		{
			// 预定义的亲本卵。
			if (parentDef == defName) return true;
			if (parentDefs.Contains(defName)) return true;
			// 动态卵。
			return RJWPregnancySettings.egg_pregnancy_implant_anyone;
		}
	}

	[StaticConstructorOnStartup]
	public class HediffDef_InsectEgg : HediffDef_EnemyImplants
	{
		//这由 xml 填充
		//1 天 = 60000 tick
		public float eggsize = 1;
		public bool selffertilized = false;

		public List<string> childrenDefs = new();
		public string? UnFertEggDef = null;
		public string? FertEggDef = null;
	}

	[StaticConstructorOnStartup]
	public class HediffDef_MechImplants : HediffDef_EnemyImplants
	{
		public List<string> randomHediffDefs = new();
		public int minEventInterval = 30000;
		public int maxEventInterval = 90000;

		public List<string> childrenDefs = new();
	}
}