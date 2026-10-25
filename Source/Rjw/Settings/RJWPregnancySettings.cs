using System;
using RimWorld;
using UnityEngine;
using Verse;
using rjw.Settings;
using rjw.RJWUI;
using rjw.RJWUI.RJWidgets;

namespace rjw
{
	public class RJWPregnancySettings
	{
		public static bool humanlike_pregnancy_enabled = true;
		public static bool animal_pregnancy_enabled = true;
		public static bool animal_pregnancy_notifications_enabled = true;
		public static BestialPregnancy bestial_pregnancy = BestialPregnancy.Unlimited;
		public static bool insect_pregnancy_enabled = true;
		public static bool insect_anal_pregnancy_enabled = false;
		public static bool insect_oral_pregnancy_enabled = false;
		public static bool egg_pregnancy_implant_anyone = true;
		public static bool egg_pregnancy_fertilize_anyone = false;
		public static bool egg_pregnancy_genes = true;
		public static bool egg_pregnancy_fertOrificeCheck_enabled = true;
		public static float egg_pregnancy_eggs_size = 1.0f;
		public static float egg_pregnancy_ovipositor_capacity_factor = 1f;
		public static bool egg_pregnancy_unlimited_capacity = false;
		public static bool safer_mechanoid_pregnancy = false;

		public static bool mechanoid_pregnancy_enabled = true;

		public static bool use_parent_method = true;
		public static float humanlike_DNA_from_mother = 0.5f;
		/// <summary>
		/// 人/兽滑块
		/// </summary>
		public static float bestiality_DNA_inheritance = 0.5f;
		/// <summary>
		/// 母/父滑块
		/// </summary>
		public static float bestial_DNA_from_mother = 1.0f;

		public static bool complex_interspecies = false;
		public static int animal_impregnation_chance = 25;
		public static int humanlike_impregnation_chance = 25;
		public static float interspecies_impregnation_modifier = 0.2f;
		public static float fertility_endage_male = 1.2f;
		public static float fertility_endage_female_humanlike = 0.58f;
		public static float fertility_endage_female_animal = 0.96f;

		public static bool phantasy_pregnancy = false;
		public static float normal_pregnancy_duration = 1.0f;
		public static float egg_pregnancy_duration = 1.0f;
		public static float max_num_momtraits_inherited = 3.0f;
		public static float max_num_poptraits_inherited = 3.0f;

		internal static bool UseVanillaPregnancyWithoutBiotechDLCCheck = true;
		public static bool UseVanillaPregnancy => UseVanillaPregnancyWithoutBiotechDLCCheck && ModsConfig.BiotechActive;

		private static Vector2 scrollPosition;
		private static float height_modifier = 0f;

		public enum BestialPregnancy
		{
			None,
			Limited,
			Unlimited
		};

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
			Rect viewRect = new(inRect.x, inRect.y + 30f, inRect.width - 16f, inRect.height + height_modifier);

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
		using (var general = scope.Group("RJWSettingsPregnancyTitle_General"))
		{
			general.Checkbox("RJWH_pregnancy", "RJWH_pregnancy_desc", ref humanlike_pregnancy_enabled);
			if (ModsConfig.BiotechActive)
			{
				general.Checkbox("UseVanillaPregnancy", "UseVanillaPregnancy_desc", ref UseVanillaPregnancyWithoutBiotechDLCCheck);
			}
			general.Checkbox("RJWA_pregnancy", "RJWA_pregnancy_desc", ref animal_pregnancy_enabled);
			using (var animalKids = general.Nested(animal_pregnancy_enabled, Widgets.CheckboxSize))
			{
				animalKids.Checkbox("RJWA_pregnancy_notifications", "RJWA_pregnancy_notifications_desc", ref animal_pregnancy_notifications_enabled);
			}

			general.DropdownLabeledWithOptionTooltips("RJW_B_Pregnancy_Setting", "RJW_B_Pregnancy_Setting_desc",
				() => "RJW_B_Pregnancy_Setting".Translate(), () => ("BestialPregnancy_" + bestial_pregnancy.ToString()).Translate(),
				() => ("BestialPregnancy_" + bestial_pregnancy.ToString() + "_desc").Translate(),
				[
					new DropdownOption("BestialPregnancy_None", () => bestial_pregnancy = BestialPregnancy.None, "BestialPregnancy_None_desc"),
					new DropdownOption("BestialPregnancy_Limited", () => bestial_pregnancy = BestialPregnancy.Limited, "BestialPregnancy_Limited_desc"),
					new DropdownOption("BestialPregnancy_Unlimited", () => bestial_pregnancy = BestialPregnancy.Unlimited, "BestialPregnancy_Unlimited_desc")
				],
				DropdownStyle.Custom, false);
			if (!scope.Filtering)
				scope.Listing.Gap(5f);
		}

		using (var eggs = scope.Group("RJWSettingsPregnancyTitle_Eggs"))
		{
			eggs.Checkbox("RJWI_pregnancy", "RJWI_pregnancy_desc", ref insect_pregnancy_enabled);
			eggs.Checkbox("egg_pregnancy_implant_anyone", "egg_pregnancy_implant_anyone_desc", ref egg_pregnancy_implant_anyone);
			eggs.Checkbox("egg_pregnancy_fertilize_anyone", "egg_pregnancy_fertilize_anyone_desc", ref egg_pregnancy_fertilize_anyone);
			eggs.Checkbox("egg_pregnancy_genes", "egg_pregnancy_genes_desc", ref egg_pregnancy_genes);
			eggs.Checkbox("RJWI_analPregnancy", "RJWI_analPregnancy_desc", ref insect_anal_pregnancy_enabled);
			eggs.Checkbox("RJWI_oralPregnancy", "RJWI_oralPregnancy_desc", ref insect_oral_pregnancy_enabled);
			eggs.Checkbox("RJWI_FertilizationCheck", "RJWI_FertilizationCheck_desc", ref egg_pregnancy_fertOrificeCheck_enabled);

			eggs.SliderPercent("egg_pregnancy_eggs_size", "egg_pregnancy_eggs_size_desc", ref egg_pregnancy_eggs_size, 0.0f, 1.0f);
			// 注意：若把取值范围再放宽，就必须改用其他输入方式，或者加大滑块宽度！
			eggs.SliderPercent("egg_pregnancy_ovipositor_capacity_factor", "egg_pregnancy_ovipositor_capacity_factor_desc", ref egg_pregnancy_ovipositor_capacity_factor, 0.1f, 5f);
			eggs.Checkbox("egg_pregnancy_unlimited_capacity", "egg_pregnancy_unlimited_capacity_desc", ref egg_pregnancy_unlimited_capacity);
		}

		scope.Listing.Gap(4f * 2);

		using (var inherit = scope.Group("RJWSettingsPregnancyTitle_Inheritance"))
		{
			inherit.Checkbox("UseParentMethod", "UseParentMethod_desc", ref use_parent_method);
			using (var dna = inherit.Nested(use_parent_method, Text.LineHeight * 7))
			{
				if (dna.Row("OffspringLookLikeTheirMother", "OffspringLookLikeTheirMother_desc"))
				{
					if (humanlike_DNA_from_mother == 0.0f)
					{
						dna.Listing.Label("OffspringLookLikeTheirMother".Translate() + ": " + "AlwaysFather".Translate(), -1f, "OffspringLookLikeTheirMother_desc".Translate());
						humanlike_DNA_from_mother = dna.Listing.Slider(humanlike_DNA_from_mother, 0.0f, 1.0f);
					}
					else if (humanlike_DNA_from_mother == 1.0f)
					{
						dna.Listing.Label("OffspringLookLikeTheirMother".Translate() + ": " + "AlwaysMother".Translate(), -1f, "OffspringLookLikeTheirMother_desc".Translate());
						humanlike_DNA_from_mother = dna.Listing.Slider(humanlike_DNA_from_mother, 0.0f, 1.0f);
					}
					else
					{
						int value = (int)(humanlike_DNA_from_mother * 100);
						dna.Listing.Label("OffspringLookLikeTheirMother".Translate() + ": " + value + "%", -1f, new TipSignal("OffspringLookLikeTheirMother_desc".Translate()));
						humanlike_DNA_from_mother = dna.Listing.Slider(humanlike_DNA_from_mother, 0.0f, 1.0f);
					}
				}

				if (dna.Row("OffspringIsHuman", "OffspringIsHuman_desc"))
				{
					if (bestial_DNA_from_mother == 0.0f)
					{
						dna.Listing.Label("OffspringIsHuman".Translate() + ": " + "AlwaysFather".Translate(), -1f, "OffspringIsHuman_desc".Translate());
						bestial_DNA_from_mother = dna.Listing.Slider(bestial_DNA_from_mother, 0.0f, 1.0f);
					}
					else if (bestial_DNA_from_mother == 1.0f)
					{
						dna.Listing.Label("OffspringIsHuman".Translate() + ": " + "AlwaysMother".Translate(), -1f, "OffspringIsHuman_desc".Translate());
						bestial_DNA_from_mother = dna.Listing.Slider(bestial_DNA_from_mother, 0.0f, 1.0f);
					}
					else
					{
						int value = (int)(bestial_DNA_from_mother * 100);
						dna.Listing.Label("OffspringIsHuman".Translate() + ": " + value + "%", -1f, new TipSignal("OffspringIsHuman_desc".Translate()));
						bestial_DNA_from_mother = dna.Listing.Slider(bestial_DNA_from_mother, 0.0f, 1.0f);
					}
				}

				if (dna.Row("OffspringIsHuman2", "OffspringIsHuman2_desc"))
				{
					if (bestiality_DNA_inheritance == 0.0f)
					{
						dna.Listing.Label("OffspringIsHuman2".Translate() + ": " + "AlwaysBeast".Translate(), -1f, "OffspringIsHuman2_desc".Translate());
						bestiality_DNA_inheritance = dna.Listing.Slider(bestiality_DNA_inheritance, 0.0f, 1.0f);
					}
					else if (bestiality_DNA_inheritance == 1.0f)
					{
						dna.Listing.Label("OffspringIsHuman2".Translate() + ": " + "AlwaysHumanlike".Translate(), -1f, "OffspringIsHuman2_desc".Translate());
						bestiality_DNA_inheritance = dna.Listing.Slider(bestiality_DNA_inheritance, 0.0f, 1.0f);
					}
					else
					{
						dna.Listing.Label("OffspringIsHuman2".Translate() + ": " + "UsesOffspringIsHuman".Translate(), -1f, "OffspringIsHuman2_desc".Translate());
						bestiality_DNA_inheritance = dna.Listing.Slider(bestiality_DNA_inheritance, 0.0f, 1.0f);
					}
				}
			}
			if (!filtering && !use_parent_method)
			{
				humanlike_DNA_from_mother = 0.5f;
				bestial_DNA_from_mother = 0.5f;
				bestiality_DNA_inheritance = 1.0f;
			}

			if (inherit.Row("max_num_momtraits_inherited"))
			{
				inherit.Listing.Label("max_num_momtraits_inherited".Translate() + ": " + (int)(max_num_momtraits_inherited));
				max_num_momtraits_inherited = inherit.Listing.Slider(max_num_momtraits_inherited, 0.0f, 9.0f);
			}
			if (inherit.Row("max_num_poptraits_inherited"))
			{
				inherit.Listing.Label("max_num_poptraits_inherited".Translate() + ": " + (int)(max_num_poptraits_inherited));
				max_num_poptraits_inherited = inherit.Listing.Slider(max_num_poptraits_inherited, 0.0f, 9.0f);
			}

			bool mechanophiliaEnabled = RJWSettings.mechanophilia_enabled;
			if (mechanophiliaEnabled)
			{
				inherit.Checkbox("MechanoidImplanting", "MechanoidImplanting_desc", ref mechanoid_pregnancy_enabled, "SaferMechanoidImplanting", "SaferMechanoidImplanting_desc");
				using (var mech = inherit.Nested(mechanoid_pregnancy_enabled, Widgets.CheckboxSize))
				{
					mech.Checkbox("SaferMechanoidImplanting", "SaferMechanoidImplanting_desc", ref safer_mechanoid_pregnancy);
				}
			}
			inherit.Checkbox("ComplexImpregnation", "ComplexImpregnation_desc", ref complex_interspecies);

			if (!filtering)
			{
				using (new UiScope(contentColor: Color.cyan))
				{
					string humanSame = "  " + "PregnancyChance_HumanlikeSameRace".Translate();
					string humanDiff = "  " + "PregnancyChance_HumanlikeDifferentRace".Translate();
					string animalSame = "  " + "PregnancyChance_AnimalSameRace".Translate();
					string animalDiff = "  " + "PregnancyChance_AnimalDifferentRace".Translate();
					string humanAnimal = "  " + "PregnancyChance_HumanlikeAnimal".Translate();
					string animalHuman = "  " + "PregnancyChance_AnimalHumanlike".Translate();

					scope.Listing.Label("BasePregnancyChances".Translate());
					scope.Listing.Gap(4f);
					if (humanlike_pregnancy_enabled)
						scope.Listing.Label("PregnancyChanceLine".Translate(humanSame, humanlike_impregnation_chance));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(humanSame));
					if (humanlike_pregnancy_enabled && !(humanlike_impregnation_chance * interspecies_impregnation_modifier <= 0.0f) && !complex_interspecies)
						scope.Listing.Label("PregnancyChanceLine".Translate(humanDiff, Math.Round(humanlike_impregnation_chance * interspecies_impregnation_modifier, 1)));
					else if (complex_interspecies)
						scope.Listing.Label("PregnancyChanceLineDependsOnSpecies".Translate(humanDiff));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(humanDiff));
					if (animal_pregnancy_enabled)
						scope.Listing.Label("PregnancyChanceLine".Translate(animalSame, animal_impregnation_chance));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(animalSame));
					if (animal_pregnancy_enabled && !(animal_impregnation_chance * interspecies_impregnation_modifier <= 0.0f) && !complex_interspecies)
						scope.Listing.Label("PregnancyChanceLine".Translate(animalDiff, Math.Round(animal_impregnation_chance * interspecies_impregnation_modifier, 1)));
					else if (complex_interspecies)
						scope.Listing.Label("PregnancyChanceLineDependsOnSpecies".Translate(animalDiff));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(animalDiff));
					if (RJWSettings.bestiality_enabled && bestial_pregnancy != BestialPregnancy.None && !(animal_impregnation_chance * interspecies_impregnation_modifier <= 0.0f) && !complex_interspecies)
						scope.Listing.Label("PregnancyChanceLine".Translate(humanAnimal, Math.Round(animal_impregnation_chance * interspecies_impregnation_modifier, 1)));
					else if (complex_interspecies)
						scope.Listing.Label("PregnancyChanceLineDependsOnSpecies".Translate(humanAnimal));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(humanAnimal));
					if (RJWSettings.bestiality_enabled && bestial_pregnancy != BestialPregnancy.None && !(humanlike_impregnation_chance * interspecies_impregnation_modifier <= 0.0f) && !complex_interspecies)
						scope.Listing.Label("PregnancyChanceLine".Translate(animalHuman, Math.Round(humanlike_impregnation_chance * interspecies_impregnation_modifier, 1)));
					else if (complex_interspecies)
						scope.Listing.Label("PregnancyChanceLineDependsOnSpecies".Translate(animalHuman));
					else
						scope.Listing.Label("PregnancyChanceLineDisabled".Translate(animalHuman));
				}
			}

			if (!filtering)
			{
				scope.Listing.NewColumn();
			}

		} // 继承

		using (var fert = scope.Group("RJWSettingsPregnancyTitle_Fertility"))
		{
			int humanChance = humanlike_impregnation_chance;
			fert.Slider("PregnantCoeffecientForHuman", "PregnantCoeffecientForHuman_desc", ref humanChance, 0, 100, v => v + "%");
			humanlike_impregnation_chance = humanChance;
			int animalChance = animal_impregnation_chance;
			fert.Slider("PregnantCoeffecientForAnimals", "PregnantCoeffecientForAnimals_desc", ref animalChance, 0, 100, v => v + "%");
			animal_impregnation_chance = animalChance;
			if (!filtering ? !complex_interspecies : fert.Row("InterspeciesImpregnantionModifier", "InterspeciesImpregnantionModifier_desc", "InterspeciesDisabled", "InterspeciesMaximum"))
			{
				switch (interspecies_impregnation_modifier)
				{
					case 0.0f:
						using (new UiScope(contentColor: Color.grey))
						{
							fert.Listing.Label("InterspeciesImpregnantionModifier".Translate() + ": " + "InterspeciesDisabled".Translate(), -1f, "InterspeciesImpregnantionModifier_desc".Translate());
						}
						break;
					case 1.0f:
						using (new UiScope(contentColor: Color.cyan))
						{
							fert.Listing.Label("InterspeciesImpregnantionModifier".Translate() + ": " + "InterspeciesMaximum".Translate(), -1f, "InterspeciesImpregnantionModifier_desc".Translate());
						}
						break;
					default:
						fert.Listing.Label("InterspeciesImpregnantionModifier".Translate() + ": " + Math.Round(interspecies_impregnation_modifier * 100, 1) + "%", -1f, new TipSignal("InterspeciesImpregnantionModifier_desc".Translate()));
						break;
				}
				interspecies_impregnation_modifier = fert.Listing.Slider(interspecies_impregnation_modifier, 0.0f, 1.0f);
			}
			fert.Slider("RJW_fertility_endAge_male", "RJW_fertility_endAge_male_desc", ref fertility_endage_male, 0.1f, 3.0f, v => (int)(v * 80) + "In_human_years".Translate(), "In_human_years");
			fert.Slider("RJW_fertility_endAge_female_humanlike", "RJW_fertility_endAge_female_humanlike_desc", ref fertility_endage_female_humanlike, 0.1f, 3.0f, v => (int)(v * 80) + "In_human_years".Translate(), "In_human_years");
			fert.Slider("RJW_fertility_endAge_female_animal", "RJW_fertility_endAge_female_animal_desc", ref fertility_endage_female_animal, 0.1f, 3.0f, v => (int)(v * 100) + "XofLifeExpectancy".Translate(), "XofLifeExpectancy");
		}
		scope.Listing.Gap(4f * 2);

		using (var duration = scope.Group("RJWSettingsPregnancyTitle_Duration"))
		{
			duration.Checkbox("phantasy_pregnancy", "phantasy_pregnancy_desc", ref phantasy_pregnancy);
			duration.SliderPercent("normal_pregnancy_duration", "normal_pregnancy_duration_desc", ref normal_pregnancy_duration, 0.05f, 2.0f);
			duration.SliderPercent("egg_pregnancy_duration", "egg_pregnancy_duration_desc", ref egg_pregnancy_duration, 0.05f, 2.0f);
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
			Scribe_Values.Look(ref humanlike_pregnancy_enabled, nameof(humanlike_pregnancy_enabled), humanlike_pregnancy_enabled, true);
			Scribe_Values.Look(ref UseVanillaPregnancyWithoutBiotechDLCCheck, nameof(UseVanillaPregnancyWithoutBiotechDLCCheck), UseVanillaPregnancyWithoutBiotechDLCCheck, true);
			Scribe_Values.Look(ref animal_pregnancy_enabled, nameof(animal_pregnancy_enabled), animal_pregnancy_enabled, true);
			Scribe_Values.Look(ref animal_pregnancy_notifications_enabled, nameof(animal_pregnancy_notifications_enabled), animal_pregnancy_notifications_enabled, true);
			Scribe_Values.Look(ref bestial_pregnancy, nameof(bestial_pregnancy), bestial_pregnancy, true);
			Scribe_Values.Look(ref insect_pregnancy_enabled, nameof(insect_pregnancy_enabled), insect_pregnancy_enabled, true);
			Scribe_Values.Look(ref insect_anal_pregnancy_enabled, nameof(insect_anal_pregnancy_enabled), insect_anal_pregnancy_enabled, true);
			Scribe_Values.Look(ref insect_oral_pregnancy_enabled, nameof(insect_oral_pregnancy_enabled), insect_oral_pregnancy_enabled, true);
			Scribe_Values.Look(ref egg_pregnancy_implant_anyone, nameof(egg_pregnancy_implant_anyone), egg_pregnancy_implant_anyone, true);
			Scribe_Values.Look(ref egg_pregnancy_fertilize_anyone, nameof(egg_pregnancy_fertilize_anyone), egg_pregnancy_fertilize_anyone, true);
			Scribe_Values.Look(ref egg_pregnancy_genes, nameof(egg_pregnancy_genes), egg_pregnancy_genes, true);
			Scribe_Values.Look(ref egg_pregnancy_fertOrificeCheck_enabled, nameof(egg_pregnancy_fertOrificeCheck_enabled), egg_pregnancy_fertOrificeCheck_enabled, true);
			Scribe_Values.Look(ref egg_pregnancy_eggs_size, nameof(egg_pregnancy_eggs_size), egg_pregnancy_eggs_size, true);
			Scribe_Values.Look(ref egg_pregnancy_ovipositor_capacity_factor, nameof(egg_pregnancy_ovipositor_capacity_factor), egg_pregnancy_ovipositor_capacity_factor, true);
			Scribe_Values.Look(ref egg_pregnancy_unlimited_capacity, nameof(egg_pregnancy_unlimited_capacity), egg_pregnancy_unlimited_capacity, true);
			Scribe_Values.Look(ref mechanoid_pregnancy_enabled, nameof(mechanoid_pregnancy_enabled), mechanoid_pregnancy_enabled, true);
			Scribe_Values.Look(ref safer_mechanoid_pregnancy, nameof(safer_mechanoid_pregnancy), safer_mechanoid_pregnancy, true);
			Scribe_Values.Look(ref use_parent_method, nameof(use_parent_method), use_parent_method, true);
			Scribe_Values.Look(ref humanlike_DNA_from_mother, nameof(humanlike_DNA_from_mother), humanlike_DNA_from_mother, true);
			Scribe_Values.Look(ref bestial_DNA_from_mother, nameof(bestial_DNA_from_mother), bestial_DNA_from_mother, true);
			Scribe_Values.Look(ref bestiality_DNA_inheritance, nameof(bestiality_DNA_inheritance), bestiality_DNA_inheritance, true);
			Scribe_Values.Look(ref humanlike_impregnation_chance, nameof(humanlike_impregnation_chance), humanlike_impregnation_chance, true);
			Scribe_Values.Look(ref animal_impregnation_chance, nameof(animal_impregnation_chance), animal_impregnation_chance, true);
			Scribe_Values.Look(ref interspecies_impregnation_modifier, nameof(interspecies_impregnation_modifier), interspecies_impregnation_modifier, true);
			Scribe_Values.Look(ref complex_interspecies, nameof(complex_interspecies), complex_interspecies, true);
			Scribe_Values.Look(ref fertility_endage_male, nameof(fertility_endage_male), fertility_endage_male, true);
			Scribe_Values.Look(ref fertility_endage_female_humanlike, nameof(fertility_endage_female_humanlike), fertility_endage_female_humanlike, true);
			Scribe_Values.Look(ref fertility_endage_female_animal, nameof(fertility_endage_female_animal), fertility_endage_female_animal, true);
			Scribe_Values.Look(ref phantasy_pregnancy, nameof(phantasy_pregnancy), phantasy_pregnancy, true);
			Scribe_Values.Look(ref normal_pregnancy_duration, nameof(normal_pregnancy_duration), normal_pregnancy_duration, true);
			Scribe_Values.Look(ref egg_pregnancy_duration, nameof(egg_pregnancy_duration), egg_pregnancy_duration, true);
			Scribe_Values.Look(ref max_num_momtraits_inherited, nameof(max_num_momtraits_inherited), max_num_momtraits_inherited, true);
			Scribe_Values.Look(ref max_num_poptraits_inherited, nameof(max_num_poptraits_inherited), max_num_poptraits_inherited, true);
		}
	}
}
