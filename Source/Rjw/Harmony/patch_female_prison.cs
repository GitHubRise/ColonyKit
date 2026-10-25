#nullable disable

using HarmonyLib;
using RimWorld;
using Verse;

namespace rjw
{
	/// <summary>
	/// 「女子监狱」房间类型：有囚犯床，且房间里的囚犯全部为女性时才成立。
	/// 分数高于原版「监狱」，因此会覆盖原版房间类型。
	/// </summary>
	public class RoomRoleWorker_FemalePrison : RoomRoleWorker
	{
		public override float GetScore(Room room)
		{
			if (room == null) return 0f;

			int prisonBeds = 0;
			foreach (Thing thing in room.ContainedAndAdjacentThings)
			{
				if (thing is Building_Bed bed
					&& bed.def?.building != null
					&& bed.def.building.bed_humanlike
					&& bed.ForPrisoners
					&& !bed.Medical)
				{
					prisonBeds++;
				}
			}

			if (prisonBeds == 0) return 0f;

			int femalePrisoners = 0;
			foreach (Thing thing in room.ContainedAndAdjacentThings)
			{
				if (thing is not Pawn pawn || !pawn.IsPrisonerOfColony) continue;

				// 只要有一名男性囚犯，就不是女子监狱。
				if (pawn.gender != Gender.Female) return 0f;

				femalePrisoners++;
			}

			return femalePrisoners > 0 ? 150f : 0f;
		}
	}

	/// <summary>自定义房间类型仍需被当作监狱格，否则原版的囚犯房间逻辑会失效。</summary>
	[HarmonyPatch(typeof(Room), nameof(Room.IsPrisonCell), MethodType.Getter)]
	internal static class PATCH_FemalePrison_IsPrisonCell
	{
		private static RoomRoleDef femalePrisonRole;

		private static void Postfix(Room __instance, ref bool __result)
		{
			if (__result || __instance == null) return;

			femalePrisonRole ??= DefDatabase<RoomRoleDef>.GetNamedSilentFail("RJW_FemalePrison");
			if (femalePrisonRole != null && __instance.Role == femalePrisonRole)
				__result = true;
		}
	}
}
