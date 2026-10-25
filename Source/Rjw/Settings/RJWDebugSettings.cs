using System;
using RimWorld;
using UnityEngine;
using Verse;
using rjw.Settings;
using rjw.RJWUI;
using rjw.RJWUI.WhatsNew;
using rjw.RJWUI.RJWidgets;

namespace rjw
{
	public class RJWDebugSettings
	{
		private static Vector2 scrollPosition;
		private static float height_modifier = 0f;

		/// <summary>最近一次 <see cref="DoWindowContents"/> 绘制的活动根作用域；尚未绘制时为 null。</summary>
		private static SearchSection? liveScope;

		/// <summary>
		/// 最近一次实际绘制是否匹配到任何内容；若该标签页尚未绘制则为 null。
		/// 让 <see cref="RJWSettingsController"/> 无需执行索引即可刷新所选标签页的标记。
		/// </summary>
		internal static bool? LiveAnyMatch => liveScope?.AnyMatched;

		public static void DoWindowContents(Rect inRect)
		{
			var f = RJWSettingsController.ActiveFilter;
			bool filtering = RJWSettingsController.IsFiltering;
			Rect outRect = new(inRect.x, inRect.y + 30f, inRect.width, inRect.height - 30f);
			Rect viewRect = new(inRect.x, inRect.y, inRect.width - 16f, inRect.height + height_modifier);

			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			Listing_Standard listingStandard = new Listing_Standard
			{
				maxOneColumn = true,
				ColumnWidth = filtering ? viewRect.width - 10f : viewRect.width / 2.05f
			};
			listingStandard.Begin(viewRect);

			var scope = new SearchSection(listingStandard, f);
			liveScope = scope;
			DrawBody(scope, filtering);

			listingStandard.End();
			height_modifier = Math.Max(0, listingStandard.MaxColumnHeightSeen - inRect.height);
			Widgets.EndScrollView();
		}

		private static void DrawBody(SearchSection scope, bool filtering)
		{
			scope.Checkbox("RMB_enabled", "RMB_enabled_desc", ref RJWSettings.RMB_enabled);
			scope.Checkbox("RMB_ability", "RMB_ability_desc", ref RJWSettings.RMB_ability);
			scope.Checkbox("submit_button_enabled", "submit_button_enabled_desc", ref RJWSettings.submit_button_enabled);
			scope.Checkbox("RJW_designation_box", "RJW_designation_box_desc", ref RJWSettings.show_RJW_designation_box);

			if (scope.Row("RJWSettingsDebugTitle_OnlyForDevelopers"))
			{
				scope.Listing.Gap(Text.LineHeight);
				using (new UiScope(color: Color.yellow))
				{
					scope.Listing.Label("RJWSettingsDebugTitle_OnlyForDevelopers".Translate());
				}
			}
			scope.Checkbox("override_RJW_designation_checks_name", "override_RJW_designation_checks_desc", ref RJWSettings.override_RJW_designation_checks);
			scope.Checkbox("override_control", "override_control_desc", ref RJWSettings.override_control);
			scope.Checkbox("AddTrait_Rapist", "AddTrait_Rapist_desc", ref RJWSettings.AddTrait_Rapist);
			scope.Checkbox("AddTrait_Masocist", "AddTrait_Masocist_desc", ref RJWSettings.AddTrait_Masocist);
			scope.Checkbox("AddTrait_Nymphomaniac", "AddTrait_Nymphomaniac_desc", ref RJWSettings.AddTrait_Nymphomaniac);
			scope.Checkbox("AddTrait_Necrophiliac", "AddTrait_Necrophiliac_desc", ref RJWSettings.AddTrait_Necrophiliac);
			scope.Checkbox("AddTrait_Nerves", "AddTrait_Nerves_desc", ref RJWSettings.AddTrait_Nerves);
			scope.Checkbox("AddTrait_Zoophiliac", "AddTrait_Zoophiliac_desc", ref RJWSettings.AddTrait_Zoophiliac);
			scope.Checkbox("DebugAttraction", "DebugAttraction_desc", ref RJWSettings.DebugAttraction);
			scope.Checkbox("Allow_RMB_DeepTalk", "Allow_RMB_DeepTalk_desc", ref RJWSettings.Allow_RMB_DeepTalk);
			scope.Checkbox("Disable_bestiality_pregnancy_relations", "Disable_bestiality_pregnancy_relations_desc", ref RJWSettings.Disable_bestiality_pregnancy_relations);
			scope.Checkbox("Disable_egg_pregnancy_relations", "Disable_egg_pregnancy_relations_desc", ref RJWSettings.Disable_egg_pregnancy_relations);
			scope.Checkbox("Disable_MeditationFocusDrain", "Disable_MeditationFocusDrain_desc", ref RJWSettings.Disable_MeditationFocusDrain);
			scope.Checkbox("Disable_RecreationDrain", "Disable_RecreationDrain_desc", ref RJWSettings.Disable_RecreationDrain);
			if (scope.ButtonTextLabeled("DebugWhatsNew_name", "DebugWhatsNew_desc",
				() => "DebugWhatsNew_name".Translate(),
				() => "Reset".Translate()))
			{
				WhatsNewManager.ResetSeen();
			}

			if (scope.Row("Hint_RMB_Meaning"))
			{
				scope.Listing.Gap(Text.LineHeight);
				using (new UiScope(contentColor: Color.cyan))
				{
					scope.Listing.Label("Hint_RMB_Meaning".Translate());
				}
			}

			if (!filtering)
			{
				scope.Listing.NewColumn();
			}
			using (new UiScope(contentColor: Color.yellow))
			{
				scope.Checkbox("legacy_PartsAdder", "legacy_PartsAdder_desc", ref RJWSettings.legacy_PartsAdder);
				scope.Checkbox("designated_freewill", "designated_freewill_desc", ref RJWSettings.designated_freewill);
				scope.Checkbox("override_lovin", "override_lovin_desc", ref RJWSettings.override_lovin);
				scope.Checkbox("override_matin", "override_matin_desc", ref RJWSettings.override_matin);
				scope.Checkbox("matin_crossbreed", "matin_crossbreed_desc", ref RJWSettings.matin_crossbreed);
				using (var devmode = scope.Toggle("DevMode_name", "DevMode_desc", RJWSettings.DevMode, v => RJWSettings.DevMode = v, Text.LineHeight * 7))
				{
					devmode.Checkbox("WildMode_name", "WildMode_desc", ref RJWSettings.WildMode);
					devmode.Checkbox("HippieMode_name", "HippieMode_desc", ref RJWSettings.HippieMode);
					devmode.Checkbox("DebugLogJoinInBed", "DebugLogJoinInBed_desc", ref RJWSettings.DebugLogJoinInBed);
					devmode.Checkbox("DebugRape", "DebugRape_desc", ref RJWSettings.DebugRape);
					devmode.Checkbox("DebugInteraction", "DebugInteraction_desc", ref RJWSettings.DebugInteraction);
					devmode.Checkbox("DebugNymph", "DebugNymph_desc", ref RJWSettings.DebugNymph);
				}
				if (!RJWSettings.DevMode)
				{
					RJWSettings.DebugLogJoinInBed = false;
					RJWSettings.DebugRape = false;
				}
				scope.Checkbox("GenderlessAsFuta_name", "GenderlessAsFuta_desc", ref RJWSettings.GenderlessAsFuta);
			}

			float distCasual = RJWSettings.maxDistanceCellsCasual;
			scope.Slider("maxDistanceCellsCasual_name", "maxDistanceCellsCasual_desc", ref distCasual, 0f, 10000f, v => ((int)v).ToString());
			RJWSettings.maxDistanceCellsCasual = distCasual;
			float distRape = RJWSettings.maxDistanceCellsRape;
			scope.Slider("maxDistanceCellsRape_name", "maxDistanceCellsRape_desc", ref distRape, 0f, 10000f, v => ((int)v).ToString());
			RJWSettings.maxDistanceCellsRape = distRape;
			float distPath = RJWSettings.maxDistancePathCost;
			scope.Slider("maxDistancePathCost_name", "maxDistancePathCost_desc", ref distPath, 0f, 5000f, v => ((int)v).ToString());
			RJWSettings.maxDistancePathCost = distPath;

			using (var mods = scope.Group("DebugSupportedMods_title", "DebugSupportedMods_desc", Text.LineHeight, DrawModsTitle,
				"DebugSupportedModsDetected_name", "DebugSupportedModsNotDetected_name"))
			{
				DrawSupportedMods(mods, filtering);
			}
		}

		private static void DrawModsTitle(Listing_Standard listing)
		{
			using (UiScope.title)
			{
				Rect title = listing.Label("DebugSupportedMods_title".Translate());
				using (new UiScope(contentColor: Color.white))
				{
					if (Mouse.IsOver(title))
					{
						Widgets.DrawHighlight(title);
					}
					TooltipHandler.TipRegion(title, "DebugSupportedMods_desc".Translate());
				}
			}
	}

		private static readonly (string LabelKey, Func<bool> IsActive)[] SupportedMods =
		[
			("DebugSupportedMods_CombatExtended", () => xxx.CombatExtendedIsActive),
			("DebugSupportedMods_CharacterEditor", () => xxx.CharacterEditorActive),
			("DebugSupportedMods_DubsBadHygiene", () => xxx.DubsBadHygieneIsActive),
			("DebugSupportedMods_AlienFramework", () => xxx.AlienFrameworkIsActive),
			("DebugSupportedMods_RimPsycheSexuality", () => xxx.RimpsycheSexualityIsActive),
			("DebugSupportedMods_Psychology", () => xxx.PsychologyIsActive),
			("DebugSupportedMods_SYRIndividuality", () => xxx.IndividualityIsActive),
			("DebugSupportedMods_RJWEx", () => xxx.RjwExIsActive),
			("DebugSupportedMods_Polyamory", () => xxx.AnyPolyamoryModIsActive),
			("DebugSupportedMods_Polygamy", () => xxx.AnyPolygamyModIsActive),
			("DebugSupportedMods_RomanceDiversified", () => xxx.RomanceDiversifiedIsActive),
			("DebugSupportedMods_RimworldOfMagic", () => xxx.RoMIsActive),
			("DebugSupportedMods_NightmareIncarnation", () => xxx.NightmareIncarnationIsActive),
			("DebugSupportedMods_Consolidated", () => xxx.CTIsActive),
			("DebugSupportedMods_SimpleSlavery", () => xxx.SimpleSlaveryIsActive),
			("DebugSupportedMods_Immortals", () => xxx.ImmortalsIsActive),
		];

		private static void DrawSupportedMods(SearchSection mods, bool filtering)
		{
			int i = 0;
			foreach (var (labelKey, isActive) in SupportedMods)
			{
				if (filtering && !mods.Row(labelKey) && !mods.Row("DebugSupportedModsDetected_name", "DebugSupportedModsNotDetected_name"))
					continue;
				Rect rect = mods.Listing.GetRect(Text.LineHeight);
				if (i++ % 2 == 1)
					Widgets.DrawAltRect(rect);
				Widgets.DrawHighlightIfMouseover(rect);
				rect.SplitVertically(rect.width / 2, out Rect leftCell, out Rect rightCell);

				Widgets.Label(leftCell, labelKey.Translate());

				bool isDetected = isActive();
				using (isDetected ? UiScope.ok : UiScope.disabled)
				{
					Widgets.Label(rightCell, isDetected ? "DebugSupportedModsDetected_name".Translate() : "DebugSupportedModsNotDetected_name".Translate());
				}
			}
		}

		internal static void ResetScroll() { scrollPosition = Vector2.zero; }

		/// <summary>
		/// 通过 <see cref="DrawBody"/> 的一次静默索引，判断此标签页上是否有任何行匹配。
		/// </summary>
		internal static bool HasAnyMatch(QuickSearchFilter? f)
		{
			if (f is not { Active: true })
				return true;
			using (var index = SearchSection.NewIndexScope(f))
			{
				DrawBody(index, filtering: true);
				return index.AnyMatched;
			}
		}

		public static void ExposeData()
		{
			Scribe_Values.Look(ref RJWSettings.RMB_enabled, nameof(RJWSettings.RMB_enabled), RJWSettings.RMB_enabled, true);
			Scribe_Values.Look(ref RJWSettings.RMB_ability, nameof(RJWSettings.RMB_ability), RJWSettings.RMB_ability, true);
			Scribe_Values.Look(ref RJWSettings.submit_button_enabled, nameof(RJWSettings.submit_button_enabled), RJWSettings.submit_button_enabled, true);
			Scribe_Values.Look(ref RJWSettings.show_RJW_designation_box, nameof(RJWSettings.show_RJW_designation_box), RJWSettings.show_RJW_designation_box, true);
			Scribe_Values.Look(ref RJWSettings.ShowRjwParts, nameof(RJWSettings.ShowRjwParts), RJWSettings.ShowRjwParts, true);
			Scribe_Values.Look(ref RJWSettings.maxDistanceCellsCasual, nameof(RJWSettings.maxDistanceCellsCasual), RJWSettings.maxDistanceCellsCasual, true);
			Scribe_Values.Look(ref RJWSettings.maxDistanceCellsRape, nameof(RJWSettings.maxDistanceCellsRape), RJWSettings.maxDistanceCellsRape, true);
			Scribe_Values.Look(ref RJWSettings.maxDistancePathCost, nameof(RJWSettings.maxDistancePathCost), RJWSettings.maxDistancePathCost, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Rapist, nameof(RJWSettings.AddTrait_Rapist), RJWSettings.AddTrait_Rapist, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Masocist, nameof(RJWSettings.AddTrait_Masocist), RJWSettings.AddTrait_Masocist, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Nymphomaniac, nameof(RJWSettings.AddTrait_Nymphomaniac), RJWSettings.AddTrait_Nymphomaniac, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Necrophiliac, nameof(RJWSettings.AddTrait_Necrophiliac), RJWSettings.AddTrait_Necrophiliac, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Nerves, nameof(RJWSettings.AddTrait_Nerves), RJWSettings.AddTrait_Nerves, true);
			Scribe_Values.Look(ref RJWSettings.AddTrait_Zoophiliac, nameof(RJWSettings.AddTrait_Zoophiliac), RJWSettings.AddTrait_Zoophiliac, true);
			Scribe_Values.Look(ref RJWSettings.Allow_RMB_DeepTalk, nameof(RJWSettings.Allow_RMB_DeepTalk), RJWSettings.Allow_RMB_DeepTalk, true);
			Scribe_Values.Look(ref RJWSettings.Disable_bestiality_pregnancy_relations, nameof(RJWSettings.Disable_bestiality_pregnancy_relations), RJWSettings.Disable_bestiality_pregnancy_relations, true);
			Scribe_Values.Look(ref RJWSettings.Disable_egg_pregnancy_relations, nameof(RJWSettings.Disable_egg_pregnancy_relations), RJWSettings.Disable_egg_pregnancy_relations, true);
			Scribe_Values.Look(ref RJWSettings.Disable_MeditationFocusDrain, nameof(RJWSettings.Disable_MeditationFocusDrain), RJWSettings.Disable_MeditationFocusDrain, true);
			Scribe_Values.Look(ref RJWSettings.Disable_RecreationDrain, nameof(RJWSettings.Disable_RecreationDrain), RJWSettings.Disable_RecreationDrain, true);
			Scribe_Values.Look(ref RJWSettings.legacy_PartsAdder, nameof(RJWSettings.legacy_PartsAdder), RJWSettings.legacy_PartsAdder, true);
			Scribe_Values.Look(ref RJWSettings.GenderlessAsFuta, nameof(RJWSettings.GenderlessAsFuta), RJWSettings.GenderlessAsFuta, true);
			Scribe_Values.Look(ref RJWSettings.override_lovin, nameof(RJWSettings.override_lovin), RJWSettings.override_lovin, true);
			Scribe_Values.Look(ref RJWSettings.override_matin, nameof(RJWSettings.override_matin), RJWSettings.override_matin, true);
			Scribe_Values.Look(ref RJWSettings.matin_crossbreed, nameof(RJWSettings.matin_crossbreed), RJWSettings.matin_crossbreed, true);
			Scribe_Values.Look(ref RJWSettings.WildMode, "Wildmode", RJWSettings.WildMode, true);
			Scribe_Values.Look(ref RJWSettings.HippieMode, "Hippiemode", RJWSettings.HippieMode, true);
			Scribe_Values.Look(ref RJWSettings.override_RJW_designation_checks, nameof(RJWSettings.override_RJW_designation_checks), RJWSettings.override_RJW_designation_checks, true);
			Scribe_Values.Look(ref RJWSettings.override_control, nameof(RJWSettings.override_control), RJWSettings.override_control, true);
			Scribe_Values.Look(ref RJWSettings.DebugAttraction, nameof(RJWSettings.DebugAttraction), RJWSettings.DebugAttraction, true);
			Scribe_Values.Look(ref RJWSettings.DevMode, nameof(RJWSettings.DevMode), RJWSettings.DevMode, true);
			Scribe_Values.Look(ref RJWSettings.DebugLogJoinInBed, nameof(RJWSettings.DebugLogJoinInBed), RJWSettings.DebugLogJoinInBed, true);
			Scribe_Values.Look(ref RJWSettings.DebugRape, nameof(RJWSettings.DebugRape), RJWSettings.DebugRape, true);
			Scribe_Values.Look(ref RJWSettings.DebugInteraction, nameof(RJWSettings.DebugInteraction), RJWSettings.DebugInteraction, true);
			Scribe_Values.Look(ref RJWSettings.DebugNymph, nameof(RJWSettings.DebugNymph), RJWSettings.DebugNymph, true);
		}
	}
}
