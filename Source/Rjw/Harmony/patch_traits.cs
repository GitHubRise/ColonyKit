using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace rjw
{
	[HarmonyPatch(typeof(TraitSet), nameof(TraitSet.GainTrait))]
	internal static class PATCH_TraitSet_GainTrait
	{
		// public void GainTrait(Trait trait, bool suppressConflicts = false);
		[HarmonyPostfix]
		public static void gain_trait(Trait trait)
		{
			// TODO：Psychology 和 Individuality 有各自的性向处理机制
			if (
				RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.Psychology ||
				RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.SYRIndividuality
				)
				return;

			var pawn = trait.pawn;
			if (pawn == null)
			{
				ModLog.Warning($"Could not update orientation upon trait gain {trait} as pawn is null");
				return;
			}

			// 在生成阶段，不要干预性向的状态，
			// 因为其他部分假定这一步先由 sexualizer 完成。
			if (PawnGenerator.IsBeingGenerated(pawn))
			{
				return;
			}

			CompRJW.UpdateOrientation(pawn);
		}
	}

	[HarmonyPatch(typeof(TraitSet), nameof(TraitSet.RemoveTrait))]
	internal static class PATCH_TraitSet_RemoveTrait
	{
		//public void RemoveTrait(Trait trait, bool unsuppressConflicts = false);
		[HarmonyPostfix]
		public static void remove_trait(Trait trait)
		{
			// TODO：Psychology 和 Individuality 有各自的性向处理机制
			if (
				RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.Psychology ||
				RJWPreferenceSettings.sexuality_distribution == RJWPreferenceSettings.Rjw_sexuality.SYRIndividuality
				)
				return;

			var pawn = trait.pawn;
			if (pawn == null)
			{
				ModLog.Warning($"Could not update orientation upon trait removal {trait} as pawn is null");
				return;
			}

			// 在生成阶段，不要干预性向的状态，
			// 因为其他部分假定这一步先由 sexualizer 完成。
			if (PawnGenerator.IsBeingGenerated(pawn))
			{
				return;
			}

			CompRJW.UpdateOrientation(pawn);
		}
	}

	[HarmonyPatch]
	internal static class PATCH_CharacterEditorTraits
	{
		public static bool Prepare()
		{
			// 检查模组 id，以便在补丁过期时提前警告
			bool ceLoaded = ModLister.GetActiveModWithIdentifier("void.charactereditor", ignorePostfix: true) != null;

			if (ceLoaded && !xxx.CharacterEditorActive) ModLog.Error("Mod Character Editor is loaded, but has no TraitTool class. Check if CE has been refactored");
			if (!ceLoaded && xxx.CharacterEditorActive) ModLog.Warning("Mod Character Editor is not loaded, but its TraitTool class is. Check if CE has been forked");

			return xxx.CharacterEditorActive;
		}

		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method("CharacterEditor.TraitTool:AddTrait");
			yield return AccessTools.Method("CharacterEditor.TraitTool:RemoveTrait");
		}

		public static void Postfix(Pawn? pawn)
		{
			if (RJWPreferenceSettings.sexuality_distribution != RJWPreferenceSettings.Rjw_sexuality.Vanilla &&
				RJWPreferenceSettings.sexuality_distribution != RJWPreferenceSettings.Rjw_sexuality.RimJobWorld)
			{
				// 其他来源与特质并不相关
				return;
			}

			if (!xxx.has_traits(pawn))
			{
				// 原方法会检查 null，我们也照做
				return;
			}

			//ModLog.Message($"Updating orientation for {xxx.get_pawnname(pawn)} because CharacterEditor changed traits");
			CompRJW.UpdateOrientation(pawn);
		}
	}
}