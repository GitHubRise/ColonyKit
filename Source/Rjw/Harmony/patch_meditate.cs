using HarmonyLib;
using RimWorld;
using Verse;

namespace rjw
{
	[StaticConstructorOnStartup]
	public static class InitNymphomaniacTrait
	{
		static InitNymphomaniacTrait()
		{
			// 禁用除性以外的所有 MeditationFocus 类型。写在这里是为了覆盖模组添加的类型
			if (xxx.nymphomaniac is null)
			{
				return;
			}

			foreach (TraitDegreeData traitDegree in xxx.nymphomaniac.degreeDatas)
			{
				traitDegree.disallowedMeditationFocusTypes = [];
				foreach (MeditationFocusDef focusDef in DefDatabase<MeditationFocusDef>.AllDefs)
				{
					if (traitDegree.allowedMeditationFocusTypes?.Contains(focusDef) != true)
					{
						traitDegree.disallowedMeditationFocusTypes.Add(focusDef);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(MeditationUtility), nameof(MeditationUtility.FindMeditationSpot))]
	internal static class PATCH_MeditationUtility_FindMeditationSpot
	{
		[HarmonyPostfix]
		public static void DisableNympho0GainSpot(Pawn pawn, ref MeditationSpotAndFocus __result)
		{
			if (!__result.IsValid)
			{
				return;
			}

			if (xxx.is_nympho(pawn))
			{
				// 特质把基础冥想增益设为 0，但拥有灵能链接的小人仍然会冥想
				if (!__result.focus.IsValid)
				{
					//ModLog.Message($"Removing spot for {pawn.Name} because focus is invalid");
					__result.spot = LocalTargetInfo.Invalid;
					return;
				}
				// 与其硬编码某个焦点类型，不如检查小人能否从冥想中获得收益
				float focusStr = __result.focus.Thing?.GetStatValueForPawn(StatDefOf.MeditationFocusStrength, pawn) ?? 0f;
				if (focusStr < 0.001f)
				{
					//ModLog.Message($"Removing spot for {pawn.Name} because MeditationFocusStrength is 0");
					__result.spot = LocalTargetInfo.Invalid;
				}
			}
		}
	}
}
