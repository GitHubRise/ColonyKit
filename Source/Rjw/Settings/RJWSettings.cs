using System;
using System.IO;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;
using rjw.Settings;
using rjw.RJWUI;
using rjw.RJWUI.RJWidgets;

namespace rjw
{
	[StaticConstructorOnStartup]
	public class RJWSettings : ModSettings
	{
		public static bool animal_on_animal_enabled = false;
		public static bool wild_mating_enabled = false;
		public static bool bestiality_enabled = false;
		public static bool necrophilia_enabled = false;
		public static bool mechanophilia_enabled = false;
		private static bool overdrive = false;

		public static bool rape_enabled = false;
		public static bool designated_freewill = true;
		public static bool legacy_PartsAdder = false;
		public static bool love_enhancer_override = true;
		public static bool animal_CP_rape = false;
		public static bool visitor_CP_rape = false;
		public static bool colonist_CP_rape = false;
		public static bool rape_beating = false;
		public static bool gentle_rape_beating = false;
		public static bool rape_stripping = false;

		public static bool cum_filth => cum_filth_multiplier > 0.0f;
		public static float cum_filth_multiplier = 1.0f;
		public static bool sounds_enabled = true;
		public static float sounds_sex_volume = 1.0f;
		public static float sounds_cum_volume = 1.0f;
		public static float sounds_voice_volume = 1.0f;
		public static float sounds_orgasm_volume = 1.0f;
		public static float sounds_animal_on_animal_volume = 0.5f;

		public static bool NymphTamed = false;
		public static bool NymphWild = true;
		public static bool NymphRaidEasy = false;
		public static bool NymphRaidHard = false;
		public static bool NymphRaidRP = false;
		public static bool NymphPermanentManhunter = false;
		public static bool NymphSappers = true;
		public static bool NymphFrustratedRape = true;
		public static bool FemaleFuta = false;
		public static bool MaleTrap = false;
		public static float male_nymph_chance = 0.0f;
		public static float futa_nymph_chance = 0.0f;
		public static float futa_natives_chance = 0.0f;
		public static float futa_spacers_chance = 0.5f;

		public static bool sexneed_fix = true;
		public static float sexneed_decay_rate = 1.0f;
		public static int Animal_mating_cooldown = 0;

		public static float nonFutaWomenRaping_MaxVulnerability = 0.8f;
		public static float rapee_MinVulnerability_human = 1.2f;

		public static bool RPG_hero_control = false;
		public static bool RPG_hero_control_HC = false;
		public static bool RPG_hero_control_Ironman = false;
		public static bool RPG_hero_control_AI_sex = true;

		public static bool RMB_enabled = true;
		public static bool RMB_ability = false;
		public static bool submit_button_enabled = true;
		public static bool show_RJW_designation_box = false;
		public static ShowParts ShowRjwParts = ShowParts.Show;
		public static float maxDistanceCellsCasual = 100;
		public static float maxDistanceCellsRape = 100;
		public static float maxDistancePathCost = 1000;

		public static bool WildMode = false;
		public static bool HippieMode = false;
		public static bool override_RJW_designation_checks = false;
		public static bool override_control = false;
		public static bool override_lovin = true;
		public static bool override_matin = true;
		public static bool matin_crossbreed = false;
		public static bool GenderlessAsFuta = false;
		public static bool DevMode = false;
		public static bool DebugLogJoinInBed = false;
		public static bool DebugRape = false;
		public static bool DebugInteraction = false;
		public static bool DebugNymph = false;
		public static bool DebugAttraction = false;
		public static bool AddTrait_Rapist = true;
		public static bool AddTrait_Masocist = true;
		public static bool AddTrait_Nymphomaniac = true;
		public static bool AddTrait_Necrophiliac = true;
		public static bool AddTrait_Nerves = true;
		public static bool AddTrait_Zoophiliac = true;
		public static bool Allow_RMB_DeepTalk = false;
		public static bool Disable_bestiality_pregnancy_relations = false;
		public static bool Disable_egg_pregnancy_relations = false;
		public static bool Disable_MeditationFocusDrain = false;
		public static bool Disable_RecreationDrain = false;
		public static bool UseAdvancedAgeScaling = true;
		public static bool AllowYouthSex = true;
		public static bool sendTraitGainLetters = true;

		// 仅用于设置预览的临时选中项。从不写入存档，也不参与迁移。
		private static HediffComp_SexPart.PartsPreviewKind partsPreviewKind = HediffComp_SexPart.PartsPreviewKind.Penis;
		private static Texture2D partsPreviewTooltipBG = null!;

		public static string lastSeenVersion = string.Empty;
		public static bool suppressWhatsNew;

		private static Vector2 scrollPosition;
		private static float height_modifier = 0f;
		private static float heroSectionHeight = Text.LineHeight * 4f; // 略微放大的估算值，在释放时按实测内容高度校正

		public enum ShowParts
		{
			Extended,
			Show,
			Known,
			Hide
		};

		private static readonly string[] ShowPartsSearchKeys =
		[
			"RJW_ShowParts_Extended", "RJW_ShowParts_Show", "RJW_ShowParts_Hide",
			"RJW_ShowParts_Extended_desc", "RJW_ShowParts_Show_desc", "RJW_ShowParts_Hide_desc",
			"RJW_ShowParts_Preview_Part",
			"RJW_ShowParts_Preview_Hidden",
			"RJW_PartInfo_T_Penis", "RJW_PartInfo_T_Breast", "RJW_PartInfo_T_Vagina", "RJW_PartInfo_T_Anus",
		];

		/// <summary>最近一次 <see cref="DoWindowContents"/> 绘制的活动根作用域；尚未绘制时为 null。</summary>
		private static SearchSection? liveScope;

		/// <summary>
		/// 最近一次实际绘制是否匹配到任何内容；若该标签页尚未绘制则为 null。
		/// 让 <see cref="RJWSettingsController"/> 无需执行索引即可刷新所选标签页的标记。
		/// </summary>
		internal static bool? LiveAnyMatch => liveScope?.AnyMatched;

		public static void DoWindowContents(Rect inRect)
		{
			sexneed_decay_rate = Mathf.Clamp(sexneed_decay_rate, 0.0f, 10000.0f);
			nonFutaWomenRaping_MaxVulnerability = Mathf.Clamp(nonFutaWomenRaping_MaxVulnerability, 0.0f, 3.0f);
			rapee_MinVulnerability_human = Mathf.Clamp(rapee_MinVulnerability_human, 0.0f, 3.0f);

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
			using (var general = scope.Group("RJWSettingsMainTitle_General"))
			{
				using (var animal = general.Toggle("animal_on_animal_enabled", "animal_on_animal_enabled_desc", animal_on_animal_enabled, v => animal_on_animal_enabled = v, Text.LineHeight))
				{
					animal.Checkbox("wild_mating_enabled", "wild_mating_enabled_desc", ref wild_mating_enabled);
				}
				if (!filtering && !animal_on_animal_enabled)
				{
					wild_mating_enabled = false;
				}

				general.Checkbox("bestiality_enabled", "bestiality_enabled_desc", ref bestiality_enabled);
				general.Checkbox("necrophilia_enabled", "necrophilia_enabled_desc", ref necrophilia_enabled);
				general.Checkbox("mechanophilia_enabled", "mechanophilia_enabled_desc", ref mechanophilia_enabled);
				using (var rape = general.Toggle("rape_enabled", "rape_enabled_desc", rape_enabled, v => rape_enabled = v, Text.LineHeight * 10))
				{
					rape.Checkbox("rape_stripping", "rape_stripping_desc", ref rape_stripping);
					rape.Checkbox("ColonistCanCP", "ColonistCanCP_desc", ref colonist_CP_rape);
					rape.Checkbox("VisitorsCanCP", "VisitorsCanCP_desc", ref visitor_CP_rape);
					//if (!bestiality_enabled)
					//{
					//	GUI.contentColor = Color.grey;
					//	animal_CP_rape = false;
					//}
					//rape.Listing.CheckboxLabeled("AnimalsCanCP".Translate(), ref animal_CP_rape, "AnimalsCanCP_desc".Translate());
					//if (!bestiality_enabled)
					//	GUI.contentColor = Color.white;
					using (var beating = rape.Toggle("PrisonersBeating", "PrisonersBeating_desc", rape_beating, v => rape_beating = v, Text.LineHeight, 0f))
					{
						beating.Checkbox("GentlePrisonersBeating", "GentlePrisonersBeating_desc", ref gentle_rape_beating);
					}
					if (!filtering && rape_beating)
						scope.Listing.Gap(4f);

					rape.Slider("NonFutaWomenRaping_MaxVulnerability", "NonFutaWomenRaping_MaxVulnerability_desc", ref nonFutaWomenRaping_MaxVulnerability, 0.0f, 3.0f, v => ((int)(v * 100)).ToString());
					rape.Slider("Rapee_MinVulnerability_human", "Rapee_MinVulnerability_human_desc", ref rapee_MinVulnerability_human, 0.0f, 3.0f, v => ((int)(v * 100)).ToString());
				}
				if (!filtering && !rape_enabled)
				{
					colonist_CP_rape = false;
					visitor_CP_rape = false;
					animal_CP_rape = false;
					rape_beating = false;
				}

				if (general.Row("RJW_ShowParts", "RJW_ShowParts_desc", ShowPartsSearchKeys))
				{
					general.Listing.Gap(12f);
				}

				general.DropdownLabeledWithOptionTooltips("RJW_ShowParts", "RJW_ShowParts_desc",
					() => "RJW_ShowParts".Translate(), () => ("RJW_ShowParts_" + ShowRjwParts.ToString()).Translate(),
					() => ("RJW_ShowParts_" + ShowRjwParts.ToString() + "_desc").Translate(),
					[
						new DropdownOption("RJW_ShowParts_Extended", () => ShowRjwParts = ShowParts.Extended, "RJW_ShowParts_Extended_desc"),
						new DropdownOption("RJW_ShowParts_Show", () => ShowRjwParts = ShowParts.Show, "RJW_ShowParts_Show_desc"),
						new DropdownOption("RJW_ShowParts_Hide", () => ShowRjwParts = ShowParts.Hide, "RJW_ShowParts_Hide_desc")
					],
					DropdownStyle.Custom, false,
					ShowPartsSearchKeys
				);
				if (general.Row("RJW_ShowParts", "RJW_ShowParts_desc", ShowPartsSearchKeys))
				{
					if (ShowRjwParts == ShowParts.Hide)
					{
						using (new UiScope(color: Color.grey, anchor: TextAnchor.MiddleCenter))
						{
							general.Listing.Label("RJW_ShowParts_Preview_Hidden".Translate(), -1);
						}
					}
					else
					{
						general.DropdownLabeledWithOptionTooltips("RJW_ShowParts", "RJW_ShowParts_desc",
							() => "RJW_ShowParts_Preview_Part".Translate(), GetPartsPreviewKindLabel, () => null,
							[
								new DropdownOption("RJW_PartInfo_T_Penis", () => partsPreviewKind = HediffComp_SexPart.PartsPreviewKind.Penis),
								new DropdownOption("RJW_PartInfo_T_Breast", () => partsPreviewKind = HediffComp_SexPart.PartsPreviewKind.Breasts),
								new DropdownOption("RJW_PartInfo_T_Vagina", () => partsPreviewKind = HediffComp_SexPart.PartsPreviewKind.Vagina),
								new DropdownOption("RJW_PartInfo_T_Anus", () => partsPreviewKind = HediffComp_SexPart.PartsPreviewKind.Anus)
							],
							DropdownStyle.Custom, false,
							ShowPartsSearchKeys
						);
						DrawPartsPreviewBox(general.Listing);
					}
				}
				if (general.Row("RJW_ShowParts", "RJW_ShowParts_desc", ShowPartsSearchKeys))
				{
					general.Listing.Gap(12f);
				}

				general.SliderPercent("cum_filth", "cum_filth_desc", ref cum_filth_multiplier, 0f, 5f);

				general.Checkbox("UseAdvancedAgeScaling", "UseAdvancedAgeScaling_desc", ref UseAdvancedAgeScaling);
				general.Checkbox("AllowYouthSex", "AllowYouthSex_desc", ref AllowYouthSex);
			}

			using (var audio = scope.Group("RJWSettingsMainTitle_Audio"))
			{
				using (var sounds = audio.Toggle("sounds_enabled", "sounds_enabled_desc", sounds_enabled, v => sounds_enabled = v, Text.LineHeight * 11))
				{
					sounds.SliderPercent("sounds_sex_volume", "sounds_sex_volume_desc", ref sounds_sex_volume, 0f, 2f);
					sounds.SliderPercent("sounds_cum_volume", "sounds_cum_volume_desc", ref sounds_cum_volume, 0f, 2f);
					sounds.SliderPercent("sounds_voice_volume", "sounds_voice_volume_desc", ref sounds_voice_volume, 0f, 2f);
					sounds.SliderPercent("sounds_orgasm_volume", "sounds_orgasm_volume_desc", ref sounds_orgasm_volume, 0f, 2f);
					sounds.SliderPercent("sounds_animal_on_animal_volume", "sounds_animal_on_animal_volume_desc", ref sounds_animal_on_animal_volume, 0f, 2f);
				}
			}

			if (!filtering)
			{
				scope.Listing.NewColumn();
			}

			using (var sexneed = scope.Group("RJWSettingsMainTitle_SexNeed"))
			{
				if (sexneed.Row("sexneed_decay_rate_name", "sexneed_decay_rate_desc"))
				{
					if (sexneed_decay_rate < 2.5f)
					{
						overdrive = false;
						sexneed.Listing.Label("sexneed_decay_rate_name".Translate() + ": " + Math.Round(sexneed_decay_rate * 100f, 0) + "%", -1f, new TipSignal("sexneed_decay_rate_desc".Translate()));
						sexneed_decay_rate = sexneed.Listing.Slider(sexneed_decay_rate, 0.0f, 5.0f);
					}
					else if (sexneed_decay_rate <= 5.0f && !overdrive)
					{
						using (new UiScope(contentColor: Color.yellow))
						{
							sexneed.Listing.Label("sexneed_decay_rate_name".Translate() + ": " + Math.Round(sexneed_decay_rate * 100f, 0) + "% " + "sexneed_decay_not_recommended".Translate(), -1f, new TipSignal("sexneed_decay_rate_desc".Translate()));
						}
						sexneed_decay_rate = sexneed.Listing.Slider(sexneed_decay_rate, 0.0f, 5.0f);
						if (sexneed_decay_rate == 5.0f)
						{
							using (new UiScope(color: Color.red))
							{
								if (sexneed.Listing.ButtonText("SexNeedOverdrive".Translate()))
									overdrive = true;
							}
						}
					}
					else
					{
						using (new UiScope(contentColor: Color.red))
						{
							sexneed.Listing.Label("sexneed_decay_rate_name".Translate() + ": " + Math.Round(sexneed_decay_rate * 100f, 0) + "% " + "sexneed_decay_warning_unsafe".Translate(), -1f, new TipSignal("sexneed_decay_rate_desc".Translate()));
						}
						sexneed_decay_rate = sexneed.Listing.Slider(sexneed_decay_rate, 0.0f, 10000.0f);
						if (sexneed.Listing.ButtonText("Reset".Translate()))
						{
							overdrive = false;
							sexneed_decay_rate = 1.0f;
						}
					}
				}

				sexneed.Checkbox("sexneed_fix_name", "sexneed_fix_desc", ref sexneed_fix);
				sexneed.Slider("Animal_mating_cooldown", "Animal_mating_cooldown_desc", ref Animal_mating_cooldown, 0, 100, v => v + "h");
			}

			using (var nymph = scope.Group("RJWSettingsMainTitle_Nymphomaniacs"))
			{
				nymph.Checkbox("NymphTamed", "NymphTamed_desc", ref NymphTamed);
				nymph.Checkbox("NymphWild", "NymphWild_desc", ref NymphWild);
				nymph.Checkbox("NymphRaidEasy", "NymphRaidEasy_desc", ref NymphRaidEasy);
				nymph.Checkbox("NymphRaidHard", "NymphRaidHard_desc", ref NymphRaidHard);
				nymph.Checkbox("NymphRaidRP", "NymphRaidRP_desc", ref NymphRaidRP);
				nymph.Checkbox("NymphPermanentManhunter", "NymphPermanentManhunter_desc", ref NymphPermanentManhunter);
				nymph.Checkbox("NymphSappers", "NymphSappers_desc", ref NymphSappers);
				nymph.Checkbox("NymphFrustratedRape", "NymphFrustratedRape_desc", ref NymphFrustratedRape);

				// 1.9.7 的存档兼容性检查
				// 以后大概可以安全删除，我怀疑没多少玩家会长期使用旧存档。
				if (male_nymph_chance > 1.0f || futa_nymph_chance > 1.0f || futa_natives_chance > 1.0f || futa_spacers_chance > 1.0f)
				{
					male_nymph_chance = 0.0f;
					futa_nymph_chance = 0.0f;
					futa_natives_chance = 0.0f;
					futa_spacers_chance = 0.0f;
				}

				nymph.SliderPercent("male_nymph_chance", "male_nymph_chance_desc", ref male_nymph_chance, 0.0f, 1.0f);

				if (nymph.Row("FemaleFuta", "FemaleFuta_desc", "MaleTrap", "MaleTrap_desc", "futa_nymph_chance", "futa_nymph_chance_desc", "futa_natives_chance", "futa_natives_chance_desc", "futa_spacers_chance", "futa_spacers_chance_desc"))
				{
					Rect futa_trap_rect = nymph.Listing.GetRect(30f);
					futa_trap_rect.SplitVertically(futa_trap_rect.width / 2, out Rect leftCell, out Rect rightCell);
					Widgets.CheckboxLabeled(leftCell, "FemaleFuta".Translate(), ref FemaleFuta, false, null, null, false);
					if (Mouse.IsOver(leftCell))
					{
						Widgets.DrawHighlight(leftCell);
					}
					TooltipHandler.TipRegion(leftCell, "FemaleFuta_desc".Translate());
					Widgets.CheckboxLabeled(rightCell, "MaleTrap".Translate(), ref MaleTrap, false, null, null, false);
					if (Mouse.IsOver(rightCell))
					{
						Widgets.DrawHighlight(rightCell);
					}
					TooltipHandler.TipRegion(rightCell, "MaleTrap_desc".Translate());
				}

				using (var futaKids = nymph.Nested(FemaleFuta || MaleTrap, Text.LineHeight * 7))
				{
					futaKids.SliderPercent("futa_nymph_chance", "futa_nymph_chance_desc", ref futa_nymph_chance, 0.0f, 1.0f);
					futaKids.SliderPercent("futa_natives_chance", "futa_natives_chance_desc", ref futa_natives_chance, 0.0f, 1.0f);
					futaKids.SliderPercent("futa_spacers_chance", "futa_spacers_chance_desc", ref futa_spacers_chance, 0.0f, 1.0f);
				}

				nymph.Checkbox("love_enhancer_override", "love_enhancer_override_desc", ref love_enhancer_override);
				nymph.Checkbox("SendTraitGainLetters", "SendTraitGainLetters_desc", ref RJWSettings.sendTraitGainLetters);
			}

			using (var pawn = scope.Group("RJWSettingsMainTitle_PawnControl"))
			{
				using (var hero = pawn.Toggle("RPG_hero_control_name", "RPG_hero_control_desc", RPG_hero_control, v => RPG_hero_control = v, heroSectionHeight, 4f, h => heroSectionHeight = h))
				{
					hero.Checkbox("RPG_hero_control_HC_name", "RPG_hero_control_HC_desc", ref RPG_hero_control_HC);
					hero.Checkbox("RPG_hero_control_Ironman_name", "RPG_hero_control_Ironman_desc", ref RPG_hero_control_Ironman);
					hero.Checkbox("RPG_hero_control_AI_sex_name", "RPG_hero_control_AI_sex_desc", ref RPG_hero_control_AI_sex);
				}
				if (!filtering && !RPG_hero_control)
				{
					RPG_hero_control_HC = false;
					RPG_hero_control_Ironman = false;
					RPG_hero_control_AI_sex = true;
				}
			}
		}

		internal static void ResetScroll() { scrollPosition = Vector2.zero; }

		private static string GetPartsPreviewKindLabel()
		{
			return partsPreviewKind switch
			{
				HediffComp_SexPart.PartsPreviewKind.Penis => "RJW_PartInfo_T_Penis".Translate(),
				HediffComp_SexPart.PartsPreviewKind.Breasts => "RJW_PartInfo_T_Breast".Translate(),
				HediffComp_SexPart.PartsPreviewKind.Vagina => "RJW_PartInfo_T_Vagina".Translate(),
				HediffComp_SexPart.PartsPreviewKind.Anus => "RJW_PartInfo_T_Anus".Translate(),
				_ => "RJW_PartInfo_T_Penis".Translate(),
			};
		}

		// 静态提示信息的仿制实现：与 ActiveTip.DrawInner 使用相同的图集、内边距和字体，但内联绘制，不会悬浮。
		private static void DrawPartsPreviewBox(Listing_Standard listing)
		{
			string previewTitle = HediffComp_SexPart.GetSinglePartPreviewTitle(partsPreviewKind);
			string previewBody = HediffComp_SexPart.GetSinglePartPreviewBody(ShowRjwParts, partsPreviewKind);
			float boxWidth = listing.ColumnWidth;
			using (new UiScope(font: GameFont.Small))
			{
				float titleHeight = Text.CalcHeight(previewTitle, boxWidth - 8f);
				float bodyHeight = string.IsNullOrEmpty(previewBody) ? 0f : Text.CalcHeight(previewBody, boxWidth - 8f);
				Rect boxRect = listing.GetRect(titleHeight + bodyHeight + 8f);
				partsPreviewTooltipBG ??= ContentFinder<Texture2D>.Get("UI/Widgets/TooltipBG");
				if (partsPreviewTooltipBG != null)
				{
					Widgets.DrawAtlas(boxRect, partsPreviewTooltipBG);
				}
				else
				{
					Widgets.DrawWindowBackground(boxRect);
				}
				Rect innerRect = boxRect.ContractedBy(4f);
				Rect titleRect = new(innerRect.x, innerRect.y, innerRect.width, titleHeight);
				Rect bodyRect = new(innerRect.x, innerRect.y + titleHeight, innerRect.width, bodyHeight);
				using (new UiScope(color: Color.yellow))
				{
					Widgets.Label(titleRect, previewTitle);
				}
				if (!string.IsNullOrEmpty(previewBody))
				{
					Widgets.Label(bodyRect, previewBody);
				}
			}
			listing.Gap(listing.verticalSpacing);
		}

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

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref animal_on_animal_enabled, nameof(animal_on_animal_enabled), animal_on_animal_enabled, true);
			Scribe_Values.Look(ref wild_mating_enabled, nameof(wild_mating_enabled), wild_mating_enabled, true);
			Scribe_Values.Look(ref bestiality_enabled, nameof(bestiality_enabled), bestiality_enabled, true);
			Scribe_Values.Look(ref necrophilia_enabled, nameof(necrophilia_enabled), necrophilia_enabled, true);
			Scribe_Values.Look(ref mechanophilia_enabled, nameof(mechanophilia_enabled), mechanophilia_enabled, true);
			Scribe_Values.Look(ref designated_freewill, nameof(designated_freewill), designated_freewill, true);
			Scribe_Values.Look(ref rape_enabled, nameof(rape_enabled), rape_enabled, true);
			Scribe_Values.Look(ref colonist_CP_rape, nameof(colonist_CP_rape), colonist_CP_rape, true);
			Scribe_Values.Look(ref visitor_CP_rape, nameof(visitor_CP_rape), visitor_CP_rape, true);
			Scribe_Values.Look(ref animal_CP_rape, nameof(animal_CP_rape), animal_CP_rape, true);
			Scribe_Values.Look(ref rape_beating, nameof(rape_beating), rape_beating, true);
			Scribe_Values.Look(ref gentle_rape_beating, nameof(gentle_rape_beating), gentle_rape_beating, true);
			Scribe_Values.Look(ref rape_stripping, nameof(rape_stripping), rape_stripping, true);
			Scribe_Values.Look(ref NymphTamed, nameof(NymphTamed), NymphTamed, true);
			Scribe_Values.Look(ref NymphWild, nameof(NymphWild), NymphWild, true);
			Scribe_Values.Look(ref NymphRaidEasy, nameof(NymphRaidEasy), NymphRaidEasy, true);
			Scribe_Values.Look(ref NymphRaidHard, nameof(NymphRaidHard), NymphRaidHard, true);
			Scribe_Values.Look(ref NymphRaidRP, nameof(NymphRaidRP), NymphRaidRP, true);
			Scribe_Values.Look(ref NymphPermanentManhunter, nameof(NymphPermanentManhunter), NymphPermanentManhunter, true);
			Scribe_Values.Look(ref NymphSappers, nameof(NymphSappers), NymphSappers, true);
			Scribe_Values.Look(ref NymphFrustratedRape, nameof(NymphFrustratedRape), NymphFrustratedRape, true);
			Scribe_Values.Look(ref FemaleFuta, nameof(FemaleFuta), FemaleFuta, true);
			Scribe_Values.Look(ref MaleTrap, nameof(MaleTrap), MaleTrap, true);
			Scribe_Values.Look(ref sounds_enabled, nameof(sounds_enabled), sounds_enabled, true);
			Scribe_Values.Look(ref sounds_sex_volume, "sounds_sexvolume", sounds_sex_volume, true);
			Scribe_Values.Look(ref sounds_cum_volume, "sounds_cumvolume", sounds_cum_volume, true);
			Scribe_Values.Look(ref sounds_voice_volume, "sounds_voicevolume", sounds_voice_volume, true);
			Scribe_Values.Look(ref sounds_orgasm_volume, "sounds_orgasmvolume", sounds_orgasm_volume, true);
			Scribe_Values.Look(ref sounds_animal_on_animal_volume, "sounds_animal_on_animalvolume", sounds_animal_on_animal_volume, true);
			Scribe_Values.Look(ref cum_filth_multiplier, nameof(cum_filth_multiplier), cum_filth_multiplier, true);
			Scribe_Values.Look(ref sexneed_fix, nameof(sexneed_fix), sexneed_fix, true);
			Scribe_Values.Look(ref sexneed_decay_rate, nameof(sexneed_decay_rate), sexneed_decay_rate, true);
			Scribe_Values.Look(ref Animal_mating_cooldown, nameof(Animal_mating_cooldown), Animal_mating_cooldown, true);
			Scribe_Values.Look(ref nonFutaWomenRaping_MaxVulnerability, nameof(nonFutaWomenRaping_MaxVulnerability), nonFutaWomenRaping_MaxVulnerability, true);
			Scribe_Values.Look(ref rapee_MinVulnerability_human, nameof(rapee_MinVulnerability_human), rapee_MinVulnerability_human, true);
			Scribe_Values.Look(ref male_nymph_chance, nameof(male_nymph_chance), male_nymph_chance, true);
			Scribe_Values.Look(ref futa_nymph_chance, nameof(futa_nymph_chance), futa_nymph_chance, true);
			Scribe_Values.Look(ref futa_natives_chance, nameof(futa_natives_chance), futa_natives_chance, true);
			Scribe_Values.Look(ref futa_spacers_chance, nameof(futa_spacers_chance), futa_spacers_chance, true);
			Scribe_Values.Look(ref RPG_hero_control, nameof(RPG_hero_control), RPG_hero_control, true);
			Scribe_Values.Look(ref RPG_hero_control_HC, nameof(RPG_hero_control_HC), RPG_hero_control_HC, true);
			Scribe_Values.Look(ref RPG_hero_control_Ironman, nameof(RPG_hero_control_Ironman), RPG_hero_control_Ironman, true);
			Scribe_Values.Look(ref RPG_hero_control_AI_sex, nameof(RPG_hero_control_AI_sex), RPG_hero_control_AI_sex, true);
			Scribe_Values.Look(ref UseAdvancedAgeScaling, nameof(UseAdvancedAgeScaling), UseAdvancedAgeScaling, true);
			Scribe_Values.Look(ref AllowYouthSex, "AllowTeenSex", AllowYouthSex, true);
			Scribe_Values.Look(ref sendTraitGainLetters, nameof(sendTraitGainLetters), sendTraitGainLetters, true);
			Scribe_Values.Look(ref love_enhancer_override, nameof(love_enhancer_override), love_enhancer_override, true);
			Scribe_Values.Look(ref lastSeenVersion, nameof(lastSeenVersion), string.Empty);
			Scribe_Values.Look(ref suppressWhatsNew, nameof(suppressWhatsNew));

			RJWDebugSettings.ExposeData();
			RJWPregnancySettings.ExposeData();
			RJWPreferenceSettings.ExposeData();
			RJWHookupSettings.ExposeData();
		}

		#region Migration from old config files
		public static bool MigrateOldSettings(string folderName)
		{
			string configPath = GenFilePaths.ConfigFolderPath;
			string sanitizedFolder = GenText.SanitizeFilename(folderName);

			var migrations = new (string filename, Action<XmlDocument> migrator)[]
			{
				($"Mod_{sanitizedFolder}_RJWDebugController.xml", MigrateDebugSettings),
				($"Mod_{sanitizedFolder}_RJWPregnancySettingsController.xml", MigratePregnancySettings),
				($"Mod_{sanitizedFolder}_RJWPreferenceSettingsController.xml", MigratePreferenceSettings),
				($"Mod_{sanitizedFolder}_RJWHookupSettingsController.xml", MigrateHookupSettings)
			};

			bool anyMigrated = false;

			foreach (var (filename, migrator) in migrations)
			{
				string filePath = Path.Combine(configPath, filename);
				if (RJWSettings.DevMode) ModLog.Message($"Checking for old settings file {filePath}...");
				if (File.Exists(filePath))
				{
					ModLog.Message($"Old settings file {filename} found. Trying to migrate.");
					try
					{
						XmlDocument doc = new();
						doc.Load(filePath);
						migrator(doc);
						File.Delete(filePath);
						ModLog.Message($"Migrated settings from {filename}");
						anyMigrated = true;
					}
					catch (Exception ex)
					{
						ModLog.Warning($"Failed to migrate {filename}: {ex.Message}");
					}
				}
			}

			return anyMigrated;
		}

		private static void MigrateDebugSettings(XmlDocument doc)
		{
			SetBool(doc, "RMB_enabled", ref RMB_enabled);
			SetBool(doc, "RMB_ability", ref RMB_ability);
			SetBool(doc, "submit_button_enabled", ref submit_button_enabled);
			SetBool(doc, "show_RJW_designation_box", ref show_RJW_designation_box);
			SetEnum(doc, "ShowRjwParts", ref ShowRjwParts);
			SetFloat(doc, "maxDistanceCellsCasual", ref maxDistanceCellsCasual);
			SetFloat(doc, "maxDistanceCellsRape", ref maxDistanceCellsRape);
			SetFloat(doc, "maxDistancePathCost", ref maxDistancePathCost);
			SetBool(doc, "AddTrait_Rapist", ref AddTrait_Rapist);
			SetBool(doc, "AddTrait_Masocist", ref AddTrait_Masocist);
			SetBool(doc, "AddTrait_Nymphomaniac", ref AddTrait_Nymphomaniac);
			SetBool(doc, "AddTrait_Necrophiliac", ref AddTrait_Necrophiliac);
			SetBool(doc, "AddTrait_Nerves", ref AddTrait_Nerves);
			SetBool(doc, "AddTrait_Zoophiliac", ref AddTrait_Zoophiliac);
			SetBool(doc, "Allow_RMB_DeepTalk", ref Allow_RMB_DeepTalk);
			SetBool(doc, "Disable_bestiality_pregnancy_relations", ref Disable_bestiality_pregnancy_relations);
			SetBool(doc, "Disable_egg_pregnancy_relations", ref Disable_egg_pregnancy_relations);
			SetBool(doc, "Disable_MeditationFocusDrain", ref Disable_MeditationFocusDrain);
			SetBool(doc, "Disable_RecreationDrain", ref Disable_RecreationDrain);
			SetBool(doc, "legacy_PartsAdder", ref legacy_PartsAdder);
			SetBool(doc, "GenderlessAsFuta", ref GenderlessAsFuta);
			SetBool(doc, "override_lovin", ref override_lovin);
			SetBool(doc, "override_matin", ref override_matin);
			SetBool(doc, "matin_crossbreed", ref matin_crossbreed);
			SetBool(doc, "Wildmode", ref WildMode);
			SetBool(doc, "Hippiemode", ref HippieMode);
			SetBool(doc, "override_RJW_designation_checks", ref override_RJW_designation_checks);
			SetBool(doc, "override_control", ref override_control);
			SetBool(doc, "DevMode", ref DevMode);
			SetBool(doc, "DebugLogJoinInBed", ref DebugLogJoinInBed);
			SetBool(doc, "DebugRape", ref DebugRape);
			SetBool(doc, "DebugInteraction", ref DebugInteraction);
			SetBool(doc, "DebugNymph", ref DebugNymph);
			SetBool(doc, "DebugAttraction", ref DebugAttraction);
		}

		private static void MigratePregnancySettings(XmlDocument doc)
		{
			SetBool(doc, "humanlike_pregnancy_enabled", ref RJWPregnancySettings.humanlike_pregnancy_enabled);
			SetBool(doc, "useVanillaPregnancy", ref RJWPregnancySettings.UseVanillaPregnancyWithoutBiotechDLCCheck);
			SetBool(doc, "animal_enabled", ref RJWPregnancySettings.animal_pregnancy_enabled);
			SetBool(doc, "animal_notifications_enabled", ref RJWPregnancySettings.animal_pregnancy_notifications_enabled);
			SetEnum(doc, "bestial_pregnancy", ref RJWPregnancySettings.bestial_pregnancy);
			SetBool(doc, "insect_pregnancy_enabled", ref RJWPregnancySettings.insect_pregnancy_enabled);
			SetBool(doc, "insect_anal_pregnancy_enabled", ref RJWPregnancySettings.insect_anal_pregnancy_enabled);
			SetBool(doc, "insect_oral_pregnancy_enabled", ref RJWPregnancySettings.insect_oral_pregnancy_enabled);
			SetBool(doc, "egg_pregnancy_implant_anyone", ref RJWPregnancySettings.egg_pregnancy_implant_anyone);
			SetBool(doc, "egg_pregnancy_fertilize_anyone", ref RJWPregnancySettings.egg_pregnancy_fertilize_anyone);
			SetBool(doc, "egg_pregnancy_genes", ref RJWPregnancySettings.egg_pregnancy_genes);
			SetBool(doc, "egg_pregnancy_fertOrificeCheck_enabled", ref RJWPregnancySettings.egg_pregnancy_fertOrificeCheck_enabled);
			SetFloat(doc, "egg_pregnancy_eggs_size", ref RJWPregnancySettings.egg_pregnancy_eggs_size);
			SetFloat(doc, "egg_pregnancy_ovipositor_capacity_factor", ref RJWPregnancySettings.egg_pregnancy_ovipositor_capacity_factor);
			SetBool(doc, "egg_pregnancy_unlimited_capacity", ref RJWPregnancySettings.egg_pregnancy_unlimited_capacity);
			SetBool(doc, "mechanoid_enabled", ref RJWPregnancySettings.mechanoid_pregnancy_enabled);
			SetBool(doc, "safer_mechanoid_pregnancy", ref RJWPregnancySettings.safer_mechanoid_pregnancy);
			SetBool(doc, "use_parent_method", ref RJWPregnancySettings.use_parent_method);
			SetFloat(doc, "humanlike_DNA_from_mother", ref RJWPregnancySettings.humanlike_DNA_from_mother);
			SetFloat(doc, "bestial_DNA_from_mother", ref RJWPregnancySettings.bestial_DNA_from_mother);
			SetFloat(doc, "bestiality_DNA_inheritance", ref RJWPregnancySettings.bestiality_DNA_inheritance);
			SetInt(doc, "humanlike_impregnation_chance", ref RJWPregnancySettings.humanlike_impregnation_chance);
			SetInt(doc, "animal_impregnation_chance", ref RJWPregnancySettings.animal_impregnation_chance);
			SetFloat(doc, "interspecies_impregnation_chance", ref RJWPregnancySettings.interspecies_impregnation_modifier);
			SetBool(doc, "complex_interspecies", ref RJWPregnancySettings.complex_interspecies);
			SetFloat(doc, "RJW_fertility_endAge_male", ref RJWPregnancySettings.fertility_endage_male);
			SetFloat(doc, "fertility_endage_female_humanlike", ref RJWPregnancySettings.fertility_endage_female_humanlike);
			SetFloat(doc, "fertility_endage_female_animal", ref RJWPregnancySettings.fertility_endage_female_animal);
			SetBool(doc, "phantasy_pregnancy", ref RJWPregnancySettings.phantasy_pregnancy);
			SetFloat(doc, "normal_pregnancy_duration", ref RJWPregnancySettings.normal_pregnancy_duration);
			SetFloat(doc, "egg_pregnancy_duration", ref RJWPregnancySettings.egg_pregnancy_duration);
			SetFloat(doc, "max_num_momtraits_inherited", ref RJWPregnancySettings.max_num_momtraits_inherited);
			SetFloat(doc, "max_num_poptraits_inherited", ref RJWPregnancySettings.max_num_poptraits_inherited);
		}

		private static void MigratePreferenceSettings(XmlDocument doc)
		{
			SetFloat(doc, "vaginal_frequency", ref RJWPreferenceSettings.vaginal);
			SetFloat(doc, "anal_frequency", ref RJWPreferenceSettings.anal);
			SetFloat(doc, "fellatio_frequency", ref RJWPreferenceSettings.fellatio);
			SetFloat(doc, "cunnilingus_frequency", ref RJWPreferenceSettings.cunnilingus);
			SetFloat(doc, "rimming_frequency", ref RJWPreferenceSettings.rimming);
			SetFloat(doc, "double_penetration_frequency", ref RJWPreferenceSettings.double_penetration);
			SetFloat(doc, "sixtynine_frequency", ref RJWPreferenceSettings.sixtynine);
			SetFloat(doc, "breastjob_frequency", ref RJWPreferenceSettings.breastjob);
			SetFloat(doc, "handjob_frequency", ref RJWPreferenceSettings.handjob);
			SetFloat(doc, "footjob_frequency", ref RJWPreferenceSettings.footjob);
			SetFloat(doc, "fingering_frequency", ref RJWPreferenceSettings.fingering);
			SetFloat(doc, "fisting_frequency", ref RJWPreferenceSettings.fisting);
			SetFloat(doc, "mutual_masturbation_frequency", ref RJWPreferenceSettings.mutual_masturbation);
			SetFloat(doc, "scissoring_frequency", ref RJWPreferenceSettings.scissoring);
			SetFloat(doc, "asexual_ratio", ref RJWPreferenceSettings.asexual_ratio);
			SetFloat(doc, "pansexual_ratio", ref RJWPreferenceSettings.pansexual_ratio);
			SetFloat(doc, "heterosexual_ratio", ref RJWPreferenceSettings.heterosexual_ratio);
			SetFloat(doc, "bisexual_ratio", ref RJWPreferenceSettings.bisexual_ratio);
			SetFloat(doc, "homosexual_ratio", ref RJWPreferenceSettings.homosexual_ratio);
			SetBool(doc, "FapEverywhere", ref RJWPreferenceSettings.FapEverywhere);
			SetBool(doc, "FapInBed", ref RJWPreferenceSettings.FapInBed);
			SetBool(doc, "FapInBelts", ref RJWPreferenceSettings.FapInBelts);
			SetBool(doc, "FapInArmbinders", ref RJWPreferenceSettings.FapInArmbinders);
			SetEnum(doc, "sex_wear", ref RJWPreferenceSettings.sex_wear);
			SetEnum(doc, "rape_attempt_alert", ref RJWPreferenceSettings.rape_attempt_alert);
			SetEnum(doc, "rape_alert", ref RJWPreferenceSettings.rape_alert);
			SetBool(doc, "ShowForCP", ref RJWPreferenceSettings.ShowForCP);
			SetBool(doc, "ShowForBreeding", ref RJWPreferenceSettings.ShowForBreeding);
			SetEnum(doc, "sexuality_distribution", ref RJWPreferenceSettings.sexuality_distribution);
			SetEnum(doc, "Malesex", ref RJWPreferenceSettings.Malesex);
			SetEnum(doc, "FeMalesex", ref RJWPreferenceSettings.FeMalesex);
			SetInt(doc, "max_sex_partners", ref RJWPreferenceSettings.max_sex_partners);
			SetEnum(doc, "group_sex_settlement", ref RJWPreferenceSettings.group_sex_settlement);
			SetInt(doc, "invite_range_cells", ref RJWPreferenceSettings.invite_range_cells);
		}

		private static void MigrateHookupSettings(XmlDocument doc)
		{
			SetBool(doc, "SettingHookupsEnabled", ref RJWHookupSettings.HookupsEnabled);
			SetBool(doc, "SettingQuickHookupsEnabled", ref RJWHookupSettings.QuickHookupsEnabled);
			SetBool(doc, "SettingFreeLovinMeansPoly", ref RJWHookupSettings.FreeLovinMeansPoly);
			SetBool(doc, "SettingSharedBedMeansPoly", ref RJWHookupSettings.SharedBedMeansPoly);
			SetBool(doc, "SettingCheatIfSafe", ref RJWHookupSettings.CheatIfSafe);
			SetBool(doc, "NoHookupsDuringWorkHours", ref RJWHookupSettings.NoHookupsDuringWorkHours);
			SetBool(doc, "SettingColonistsCanHookup", ref RJWHookupSettings.ColonistsCanHookup);
			SetBool(doc, "SettingColonistsCanHookupWithVisitor", ref RJWHookupSettings.ColonistsCanHookupWithVisitor);
			SetBool(doc, "SettingVisitorsCanHookupWithColonists", ref RJWHookupSettings.VisitorsCanHookupWithColonists);
			SetBool(doc, "SettingVisitorsCanHookupWithVisitors", ref RJWHookupSettings.VisitorsCanHookupWithVisitors);
			SetBool(doc, "SettingCanHookupWithPrisoner", ref RJWHookupSettings.CanHookupWithPrisoner);
			SetBool(doc, "SettingPrisonersCanHookupWithNonPrisoner", ref RJWHookupSettings.PrisonersCanHookupWithNonPrisoner);
			SetBool(doc, "SettingPrisonersCanHookupWithPrisoner", ref RJWHookupSettings.PrisonersCanHookupWithPrisoner);
			SetBool(doc, "SettingNymphosCanCheat", ref RJWHookupSettings.NymphosCanCheat);
			SetBool(doc, "SettingNymphosCanHomewreck", ref RJWHookupSettings.NymphosCanHomewreck);
			SetBool(doc, "SettingNymphosCanHomewreckReverse", ref RJWHookupSettings.NymphosCanHomewreckReverse);
			SetFloat(doc, "SettingHookupChanceForNonNymphos", ref RJWHookupSettings.HookupChanceForNonNymphos);
			SetFloat(doc, "SettingMinimumFuckabilityToHookup", ref RJWHookupSettings.MinimumFuckabilityToHookup);
			SetFloat(doc, "SettingMinimumAttractivenessToHookup", ref RJWHookupSettings.MinimumAttractivenessToHookup);
			SetInt(doc, "SettingMinimumRelationshipToHookup", ref RJWHookupSettings.MinimumRelationshipToHookup);
		}

		private static void SetBool(XmlDocument doc, string key, ref bool field)
		{
			var node = doc.SelectSingleNode($"//{key}");
			if (node != null)
			{
				if (bool.TryParse(node.InnerText, out bool val))
					field = val;
				else
					ModLog.Warning($"[RJW] Failed to parse bool value '{node.InnerText}' for key '{key}'");
			}
		}

		private static void SetFloat(XmlDocument doc, string key, ref float field)
		{
			var node = doc.SelectSingleNode($"//{key}");
			if (node != null)
			{
				if (float.TryParse(node.InnerText, out float val))
					field = val;
				else
					ModLog.Warning($"[RJW] Failed to parse float value '{node.InnerText}' for key '{key}'");
			}
		}

		private static void SetInt(XmlDocument doc, string key, ref int field)
		{
			var node = doc.SelectSingleNode($"//{key}");
			if (node != null)
			{
				if (int.TryParse(node.InnerText, out int val))
					field = val;
				else
					ModLog.Warning($"[RJW] Failed to parse int value '{node.InnerText}' for key '{key}'");
			}
		}

		private static void SetEnum<T>(XmlDocument doc, string key, ref T field) where T : struct
		{
			var node = doc.SelectSingleNode($"//{key}");
			if (node != null)
			{
				if (Enum.TryParse<T>(node.InnerText, out T val))
					field = val;
				else
					ModLog.Warning($"[RJW] Failed to parse enum value '{node.InnerText}' for key '{key}'");
			}
		}
		#endregion
	}
}
