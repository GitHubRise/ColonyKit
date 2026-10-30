using HarmonyLib;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>
	/// 级联冷室的每 <c>TickRare</c> 结算：重建空调链、写目标温度、拉低冷室。
	/// </summary>
	/// <remarks>
	/// 挂在 <c>Map.MapPostTick</c> 上而不是空调的 <c>TickRare</c>：
	/// 建筑 tick 全部跑完后才结算，避免同一 tick 里后面的空调又把热量推进冷室。
	/// </remarks>
	[HarmonyPatch(typeof(Map), "MapPostTick")]
	internal static class Patch_CascadeFreezeTick
	{
		private const int Interval = 250;

		[HarmonyPostfix]
		private static void Postfix(Map __instance)
		{
			if (__instance == null || !__instance.IsHashIntervalTick(Interval))
			{
				return;
			}

			CascadeFreeze.Tick(__instance);
			CascadeSelftest.DriveIfArmed(__instance);
		}
	}

	/// <summary>自检每帧水泵：把 quicktest 停住的强制暂停窗口关掉并推时间。</summary>
	[HarmonyPatch(typeof(TickManager), "TickManagerUpdate")]
	internal static class Patch_CascadeFreezePump
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			CascadeSelftest.Pump();
		}
	}

	/// <summary>级联冷腔的墙体漏热按理想保温处理。</summary>
	[HarmonyPatch(typeof(RoomTempTracker), "WallEqualizationTempChangePerInterval")]
	internal static class Patch_CascadeFreezeWall
	{
		[HarmonyPostfix]
		private static void Postfix(RoomTempTracker __instance, ref float __result)
		{
			if (__result != 0f && CascadeFreezePatchUtility.IsCascadeRoom(__instance))
			{
				__result = 0f;
			}
		}
	}

	/// <summary>级联冷腔的薄屋顶漏热按理想保温处理。</summary>
	[HarmonyPatch(typeof(RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
	internal static class Patch_CascadeFreezeRoof
	{
		[HarmonyPostfix]
		private static void Postfix(RoomTempTracker __instance, ref float __result)
		{
			if (__result != 0f && CascadeFreezePatchUtility.IsCascadeRoom(__instance))
			{
				__result = 0f;
			}
		}
	}

	/// <summary>级联冷腔的无顶漏热按理想保温处理。</summary>
	[HarmonyPatch(typeof(RoomTempTracker), "NoRoofEqualizationTempChangePerInterval")]
	internal static class Patch_CascadeFreezeNoRoof
	{
		[HarmonyPostfix]
		private static void Postfix(RoomTempTracker __instance, ref float __result)
		{
			if (__result != 0f && CascadeFreezePatchUtility.IsCascadeRoom(__instance))
			{
				__result = 0f;
			}
		}
	}

	internal static class CascadeFreezePatchUtility
	{
		public static bool IsCascadeRoom(RoomTempTracker tracker)
		{
			if (tracker == null)
			{
				return false;
			}

			Room room = Traverse.Create(tracker).Field("room").GetValue<Room>();
			return room != null && room.Map != null && CascadeFreeze.IsInsulated(room);
		}
	}
}

