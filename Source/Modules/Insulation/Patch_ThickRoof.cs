using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 墙体足够厚时，把薄屋顶按同等层数“加厚”结算。
	/// </summary>
	/// <remarks>
	/// 原版屋顶只有三档：无顶、薄顶（5e-5）、厚岩顶（冷房间不漏）。
	/// 玩家不能自己铺厚岩顶，所以这里不动 RoofGrid，只在薄顶漏热项上乘保温系数：
	/// 当房间所有外墙中最薄的一侧达到 <see cref="InsulationSettings.roofMinWallLayers"/> 层时，
	/// 屋顶按同样层数套用 f(d) = (1 + r) / (1 + r·d)。
	/// 无顶项（NoRoof）和厚岩顶项（Deep）不缩放。
	/// </remarks>
	[HarmonyPatch(typeof(RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
	internal static class Patch_ThickRoof
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
			if (settings == null || !settings.enabled || !settings.thickenRoof)
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

			if (profile.wallEdges == 0 || profile.minWallLayers < settings.roofMinWallLayers)
			{
				return;
			}

			__result *= InsulationUtility.Factor(profile.minWallLayers, settings.wallResistanceRatio);
		}
	}
}
