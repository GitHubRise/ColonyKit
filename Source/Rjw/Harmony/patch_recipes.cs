#nullable disable

﻿using System.Linq;
using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using rjw.Modules.Interactions;

namespace rjw
{

	/// <summary>
	/// 必须防止采集到的部位出现重复的属性条目，因为属性是由每个安装该部位的配方
	/// 逐条填充的。
	/// </summary>
	[HarmonyPatch(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.GetMedicalStatsFromRecipeDefs))]
	public static class FixDuplicatePartStats
	{
		class StatDrawEntryEqualityComparer : IEqualityComparer<StatDrawEntry>
		{
			public bool Equals(StatDrawEntry x, StatDrawEntry y) => x.LabelCap == y.LabelCap && x.ValueString == y.ValueString;

			public int GetHashCode(StatDrawEntry entry) => entry.LabelCap.GetHashCode() ^ entry.ValueString.GetHashCode();
		}

		static IEqualityComparer<StatDrawEntry> comparer = new StatDrawEntryEqualityComparer();

		static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> recipes) => recipes.Distinct(comparer);
	}

	/// <summary>
	/// 过滤掉针对 RJW 部件的通用 "Remove {0}" 操作，因为我们有自己的专用操作
	/// </summary>
	[HarmonyPatch(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.GetPartsToApplyOn))]
	public static class Patch_FilterVanillaRemovalForRJW
	{
		public static void Postfix(Pawn pawn, ref IEnumerable<BodyPartRecord> __result)
		{
			if (pawn == null || __result == null)
			{
				return;
			}

			LewdParts lp = pawn.GetLewdParts();
			__result = __result.Where(part =>
				!(lp.Penises.Any(x => x.BodyPart == part)
				|| lp.Vaginas.Any(x => x.BodyPart == part)
				|| lp.Breasts.Any(x => x.BodyPart == part)
				|| lp.Udders.Any(x => x.BodyPart == part)
				|| lp.Anuses.Any(x => x.BodyPart == part)
				|| lp.FemaleOvipositors.Any(x => x.BodyPart == part)
				|| lp.MaleOvipositors.Any(x => x.BodyPart == part)));
		}
	}

	/// <summary>
	/// 把所有种族都补进 RJW 部件配方 
	/// </summary>
	[StaticConstructorOnStartup]
	public static class MakeAllRacesUseSexPartRecipes
	{
		static MakeAllRacesUseSexPartRecipes()
		{
			//召唤地毯式轰炸

			//把种族注入 RJW 配方
			foreach (RecipeDef x in	DefDatabase<RecipeDef>.AllDefsListForReading.Where(x => x.IsSurgery && (x.targetsBodyPart || !x.appliedOnFixedBodyParts.NullOrEmpty())))
			{
				if (x.appliedOnFixedBodyParts.Contains(xxx.genitalsDef)
					|| x.appliedOnFixedBodyParts.Contains(xxx.breastsDef)
					|| x.appliedOnFixedBodyParts.Contains(xxx.anusDef)
					|| x.modContentPack?.PackageId == "rim.job.world" 
					|| x.modContentPack?.PackageId == "local.colonykit" 
					|| x.modContentPack?.PackageId == "Safe.Job.World" //*叹气*
					//|| x.modContentPack?.PackageId == "Abraxas.RJW.RaceSupport" // 是为了乳房？
					)

					foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(thingDef =>
							thingDef.race != null && (
							thingDef.race.Humanlike ||
							thingDef.race.Animal
							)))
					{
						//大概是过滤掉什么东西？
						//if (thingDef.race. == "Human")
						//	continue;

						if (!x.recipeUsers.Contains(thingDef))
						{
							x.recipeUsers.Add(item: thingDef);
							//ModLog.Message("recipe: " + x.defName + ", thing: " + thingDef.defName);

						}
					}
			}
		}
	}
}
