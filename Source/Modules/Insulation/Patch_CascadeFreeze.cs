using HarmonyLib;
using UnityEngine;
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

	/// <summary>
	/// 级联冷腔的每次温度结算后把温度拉回级联目标。
	/// </summary>
	/// <remarks>
	/// 原版 <c>RoomTempTracker.EqualizeTemperature</c> 是所有房间温度变化的唯一出口：
	/// 墙体、屋顶、无顶、厚岩顶、门廊漏热都在这里结算。这里在结算之后把级联冷腔的温度
	/// 直接往目标拉 <see cref="CascadeFreeze.StepPerTick"/> 度，等于把漏热整项抵掉，
	/// 比逐个改漏热系数更不容易漏项；只对识别出的冷腔房间生效。
	/// </remarks>
	[HarmonyPatch(typeof(RoomTempTracker), "EqualizeTemperature")]
	internal static class Patch_CascadeFreezeEqualize
	{
		[HarmonyPostfix]
		private static void Postfix(RoomTempTracker __instance)
		{
			if (__instance == null)
			{
				return;
			}

			Room room = Traverse.Create(__instance).Field("room").GetValue<Room>();
			if (room != null && CascadeFreeze.TryGetTarget(room, out float target))
			{
				room.Temperature = Mathf.MoveTowards(room.Temperature, target, CascadeFreeze.StepPerTick);
			}
		}
	}
}
