#nullable disable

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw
{
	/// <summary>
	/// 大被同眠：双人床按「群交人数上限」提供额外睡眠位。
	/// 多出来的位置铺在床尾那一排格子上，因此一张双人床最多可以睡 4 个人。
	/// </summary>
	[HarmonyPatch(typeof(Building_Bed), nameof(Building_Bed.SleepingSlotsCount), MethodType.Getter)]
	internal static class PATCH_BedSharing_SleepingSlotsCount
	{
		private static void Postfix(Building_Bed __instance, ref int __result)
		{
			int extra = BedSharing.ExtraSlots(__instance);
			if (extra > 0)
				__result += extra;
		}
	}

	/// <summary>让床的「可分配人数」跟着睡眠位一起增加。</summary>
	[HarmonyPatch(typeof(CompAssignableToPawn), nameof(CompAssignableToPawn.TotalSlots), MethodType.Getter)]
	internal static class PATCH_BedSharing_TotalSlots
	{
		private static void Postfix(CompAssignableToPawn __instance, ref int __result)
		{
			if (__instance?.parent is not Building_Bed bed) return;

			int slots = bed.SleepingSlotsCount;
			if (slots > __result)
				__result = slots;
		}
	}

	/// <summary>同上，部分分配逻辑走的是这个属性。</summary>
	[HarmonyPatch(typeof(CompAssignableToPawn), nameof(CompAssignableToPawn.MaxAssignedPawnsCount), MethodType.Getter)]
	internal static class PATCH_BedSharing_MaxAssignedPawnsCount
	{
		private static void Postfix(CompAssignableToPawn __instance, ref int __result)
		{
			if (__instance?.parent is not Building_Bed bed) return;

			int slots = bed.SleepingSlotsCount;
			if (slots > __result)
				__result = slots;
		}
	}

	/// <summary>
	/// 原版只支持 0..size.x-1 的床位索引，越界会直接报错并返回床边。
	/// 这里为多出来的索引提供床尾那一排的位置。
	/// </summary>
	[HarmonyPatch(typeof(BedUtility), nameof(BedUtility.GetSleepingSlotPos))]
	internal static class PATCH_BedSharing_GetSleepingSlotPos
	{
		private static bool Prefix(int __0, IntVec3 __1, Rot4 __2, IntVec2 __3, ref IntVec3 __result)
		{
			int index = __0;
			IntVec3 bedPos = __1;
			Rot4 rotation = __2;
			IntVec2 size = __3;

			if (size.x < 2 || size.z < 2) return true;

			int vanilla = size.x;
			int wanted = BedSharing.TotalSlots(size);
			if (index < vanilla || index >= wanted) return true;

			__result = BedSharing.ExtraSlotPos(index - vanilla, bedPos, rotation, size);
			return false;
		}
	}

	internal static class BedSharing
	{
		/// <summary>双人床固定提供的睡眠位数量（与性行为的群交人数上限无关）。</summary>
		public const int FixedSlots = 4;

		/// <summary>这张床实际提供的睡眠位：双人床 4 个，其余保持原版。</summary>
		public static int TotalSlots(IntVec2 size)
		{
			if (size.x < 2 || size.z < 2) return Mathf.Max(1, size.x);

			int vanilla = size.x;
			int wanted = Mathf.Min(size.x * size.z, FixedSlots);
			return Mathf.Max(vanilla, wanted);
		}

		/// <summary>这张床比原版多出几个睡眠位（单床/非双人床返回 0）。</summary>
		public static int ExtraSlots(Building_Bed bed)
		{
			if (bed?.def == null) return 0;

			IntVec2 size = bed.def.size;
			if (size.x < 2 || size.z < 2) return 0;

			int vanilla = size.x;
			int wanted = TotalSlots(size);
			return Mathf.Max(0, wanted - vanilla);
		}

		/// <summary>
		/// 床尾那一排的格子位置，方向与 <see cref="BedUtility.GetSleepingSlotPos"/> 的朝向规则一致。
		/// </summary>
		public static IntVec3 ExtraSlotPos(int extraIndex, IntVec3 bedPos, Rot4 rotation, IntVec2 size)
		{
			CellRect rect = GenAdj.OccupiedRect(bedPos, rotation, size);
			switch (rotation.AsInt)
			{
				case 0: return new IntVec3(rect.maxX - extraIndex, bedPos.y, rect.maxZ);
				case 1: return new IntVec3(rect.minX, bedPos.y, rect.maxZ - extraIndex);
				case 2: return new IntVec3(rect.minX + extraIndex, bedPos.y, rect.minZ);
				case 3: return new IntVec3(rect.maxX, bedPos.y, rect.minZ + extraIndex);
				default: return bedPos;
			}
		}
	}
}
