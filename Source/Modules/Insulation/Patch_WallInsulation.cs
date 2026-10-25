using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>把多墙体保温系数乘到原版墙体漏热上。</summary>
	/// <remarks>
	/// 只改 <c>RoomTempTracker.WallEqualizationTempChangePerInterval</c> 的返回值：
	/// 屋顶（薄/厚/无）、门、通风口仍按原版；系数 1 时与原版完全一致。
	/// 目标方法是 private，Harmony 用方法名匹配；字段 <c>room</c> 用 Traverse 读取，
	/// 避免版本改名时静态构造直接抛异常。
	/// </remarks>
	[HarmonyPatch(typeof(RoomTempTracker), "WallEqualizationTempChangePerInterval")]
	internal static class Patch_WallInsulation
	{
		[HarmonyPostfix]
		private static void Postfix(RoomTempTracker __instance, ref float __result)
		{
			if (__instance == null || __result == 0f)
			{
				return;
			}

			if (!ModuleRegistry.IsEnabled(InsulationModule.ModuleId))
			{
				return;
			}

			InsulationSettings settings = InsulationSettingsUtility.Current;
			if (settings == null || !settings.enabled)
			{
				return;
			}

			Room room = Traverse.Create(__instance).Field("room").GetValue<Room>();
			if (room == null || room.Map == null)
			{
				return;
			}

			InsulationProfile profile = InsulationUtility.ProfileFor(
				room,
				room.Map,
				__instance.EqualizeCellsForReading);
			__result *= profile.wallFactor;
		}
	}
}
