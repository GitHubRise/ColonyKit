using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonyKit.Modules.Hygiene
{
	/// <summary>
	/// 食物含水量：吃任何东西都按吃进去的营养补水（口渴 + 营养 × 设置系数）。
	/// DBH 的 <c>Thing.Ingested</c> 后置是固定 ×0.2；这里改成可调，默认 0.25。
	/// 草、树被动物啃食也走同一个入口，所以野生动物不会只饱不渴。
	/// 瓶装水自己已经有「喝一瓶 +0.2 口渴」的后置，这里排除，避免叠加。
	/// </summary>
	[HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
	internal static class Patch_Thing_Ingested_FoodWater
	{
		private static void Postfix(Thing __instance, Pawn ingester, float __result)
		{
			if (ingester?.needs == null || __result <= 0.0001f)
			{
				return;
			}
			if (!HygieneSettings.Current.ShouldRunHygiene
				|| MineralWaterUtility.IsBottledWater(__instance))
			{
				return;
			}
			float factor = HygieneSettings.Current.foodWaterFactor;
			if (factor <= 0f)
			{
				return;
			}
			Need_Thirst thirst = ingester.needs.TryGetNeed<Need_Thirst>();
			if (thirst == null)
			{
				return;
			}
			thirst.Drink(__result * factor);
		}
	}
}
