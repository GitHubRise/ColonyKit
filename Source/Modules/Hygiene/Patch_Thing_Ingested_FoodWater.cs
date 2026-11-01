using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 食物含水量：吃任何东西都按吃进去的营养补水（口渴 + 营养 × 设置系数）。
	/// DBH 的 <c>Thing.Ingested</c> 后置是固定 ×0.2；这里改成可调，默认 0.25。
	/// 草、树被动物啃食也走同一个入口，所以野生动物不会只饱不渴。
	/// 动物另有一档固定补水：草食动物吃植物类食物 +0.1、肉食动物吃肉或尸体 +0.1
	/// （营养 × 0.25 对一棵草只有 0.04 左右，撑不住野生动物找水的频率）。
	/// 瓶装水自己已经有「喝一瓶 × 每升解渴」的后置，这里排除，避免叠加。
	/// </summary>
	[HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
	internal static class Patch_Thing_Ingested_FoodWater
	{
		private const FoodTypeFlags PlantFoodFlags =
			FoodTypeFlags.Plant | FoodTypeFlags.Tree | FoodTypeFlags.VegetableOrFruit
			| FoodTypeFlags.Fungus | FoodTypeFlags.Seed;

		private const FoodTypeFlags MeatFoodFlags = FoodTypeFlags.Meat | FoodTypeFlags.Corpse;

		private static void Postfix(Thing __instance, Pawn ingester, float __result)
		{
			if (ingester?.needs == null || __result <= 0.0001f)
			{
				return;
			}
			HygieneSettings settings = HygieneSettings.Current;
			if (!settings.ShouldRunHygiene
				|| MineralWaterUtility.IsBottledWater(__instance))
			{
				return;
			}
			Need_Thirst thirst = ingester.needs.TryGetNeed<Need_Thirst>();
			if (thirst == null)
			{
				return;
			}
			if (ingester.RaceProps.Animal
				&& TryAnimalFoodGain(__instance, ingester, settings, out float animalGain))
			{
				if (animalGain > 0f)
				{
					thirst.Drink(animalGain);
				}
				return;
			}
			float factor = settings.foodWaterFactor;
			if (factor <= 0f)
			{
				return;
			}
			thirst.Drink(__result * factor);
		}

		/// <summary>草、树、蔬果、菌菇、种子——草食动物吃这些按固定值补水。</summary>
		internal static bool IsPlantFood(Thing food)
		{
			return (Flags(food) & PlantFoodFlags) != 0;
		}

		/// <summary>生肉、尸体——肉食动物吃这些按固定值补水。</summary>
		internal static bool IsMeatFood(Thing food)
		{
			return food is Corpse || (Flags(food) & MeatFoodFlags) != 0;
		}

		private static FoodTypeFlags Flags(Thing food)
		{
			return food?.def?.ingestible?.foodType ?? FoodTypeFlags.None;
		}

		/// <summary>动物吃对口的东西时给固定补水；不在食谱里就返回 false，走营养折算。</summary>
		private static bool TryAnimalFoodGain(
			Thing food, Pawn ingester, HygieneSettings settings, out float gain)
		{
			gain = 0f;
			if (ingester.RaceProps.herbivore && IsPlantFood(food))
			{
				gain = settings.herbivorePlantThirst;
				return true;
			}
			if (ingester.RaceProps.carnivore && IsMeatFood(food))
			{
				gain = settings.carnivoreMeatThirst;
				return true;
			}
			return false;
		}
	}
}
