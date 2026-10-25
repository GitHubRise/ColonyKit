using System;
using RimWorld;
using UnityEngine;
using Verse;
using rjw.Settings;
using rjw.RJWUI;
using rjw.RJWUI.RJWidgets;

namespace rjw
{
	// TODO：增加在性交后记录怀孕几率的选项（仅开发者模式？）
	// TODO：增加一套更复杂的备选怀孕计算系统，依据身体部位、生殖器和体型（相似物种 -> 更高几率，不同 -> 更低几率）

	// TODO：未使用的旧设置——评估它们是否仍然需要，然后要么改为设置项，要么删除。
	/*
	public float significant_pain_threshold; // Updated
	public float extreme_pain_threshold; // Updated
	public float base_chance_to_hit_prisoner; // Updated
	public int min_ticks_between_hits; // Updated
	public int max_ticks_between_hits; // Updated
	public float max_nymph_fraction; // Updated
	public float comfort_prisoner_rape_mtbh_mul; // Updated
	public float whore_mtbh_mul; // Updated
	public static bool comfort_prisoners_enabled; // Updated //this one is in config.cs as well!
	public static bool ComfortColonist; // New
	public static bool ComfortAnimal; // New
	public static bool rape_me_sticky_enabled; // Updated
	public static bool bondage_gear_enabled; // Updated
	public static bool nymph_joiners_enabled; // Updated
	public static bool always_accept_whores; // Updated
	public static bool nymphs_always_JoinInBed; // Updated
	public static bool zoophis_always_rape; // Updated
	public static bool rapists_always_rape; // Updated
	public static bool pawns_always_do_fapping; // Updated
	public static bool whores_always_findjob; // Updated
	public bool show_regular_dick_and_vag; // Updated
	*/

	/// <summary>群交时，接受者一侧的结算方式。</summary>
	public enum GroupSexSettlement
	{
		/// <summary>每个参与者都完整结算，接受者会被结算 N 次（旧行为）。</summary>
		PerPartner,

		/// <summary>只有第一个发起者结算接受者一侧，其余人只结算自己。</summary>
		Once
	}

	public class RJWPreferenceSettings
	{
		public static float vaginal = 1.20f;
		public static float anal = 0.80f;
		public static float fellatio = 0.80f;
		public static float cunnilingus = 0.80f;
		public static float rimming = 0.40f;
		public static float double_penetration = 0.60f;
		public static float breastjob = 0.50f;
		public static float handjob = 0.80f;
		public static float mutual_masturbation = 0.70f;
		public static float fingering = 0.50f;
		public static float footjob = 0.30f;
		public static float scissoring = 0.50f;
		public static float fisting = 0.30f;
		public static float sixtynine = 0.69f;

		public static float asexual_ratio = 0.02f;
		public static float pansexual_ratio = 0.03f;
		public static float heterosexual_ratio = 0.7f;
		public static float bisexual_ratio = 0.15f;
		public static float homosexual_ratio = 0.1f;

		public static bool FapEverywhere = false;
		public static bool FapInBed = true;
		public static bool FapInBelts = false;
		public static bool FapInArmbinders = false;
		public static bool ShowForCP = true;
		public static bool ShowForBreeding = true;

		public static Clothing sex_wear = Clothing.Nude;
		public static RapeAlert rape_attempt_alert = RapeAlert.Humanlikes;
		public static RapeAlert rape_alert = RapeAlert.Humanlikes;
		public static Rjw_sexuality sexuality_distribution = Rjw_sexuality.Vanilla;

		/// <summary>同一场性行为中，一个接受者最多可同时接纳的发起者数量（1–8）。</summary>
		public static int max_sex_partners = 3;

		/// <summary>经过范围限制后的群交上限，供任务预约与判定使用。</summary>
		public static int MaxSexPartners => Mathf.Clamp(max_sex_partners, 1, 8);

		/// <summary>群交时接受者一侧的结算方式。</summary>
		public static GroupSexSettlement group_sex_settlement = GroupSexSettlement.PerPartner;

		/// <summary>邀请加入时搜索候选人的半径（格）；同房间的人不受此限制。</summary>
		public static int invite_range_cells = 45;

		/// <summary>经过范围限制后的邀请半径。</summary>
		public static int InviteRange => Mathf.Clamp(invite_range_cells, 1, 100);

		public static AllowedSex Malesex = AllowedSex.All;
		public static AllowedSex FeMalesex = AllowedSex.All;

		private static Vector2 scrollPosition;
		private static float height_modifier = 0f;

		private static readonly float minFrequency = 0.01f;
		private static readonly float maxFrequency = 3.0f;

		public enum AllowedSex
		{
			All,
			Homo,
			Nohomo
		};

		public enum Clothing
		{
			Nude,
			Headgear,
			Clothed
		};

		public enum RapeAlert
		{
			Enabled,
			Colonists,
			Humanlikes,
			Silent,
			Disabled
		};

		public enum Rjw_sexuality
		{
			Vanilla,
			RimJobWorld,
			Psychology,
			SYRIndividuality,
			RimPsyche
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
			Rect viewRect = new(inRect.x, inRect.y, inRect.width - 16f, inRect.height + height_modifier);

			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			Listing_Standard listingStandard = new Listing_Standard
			{
				maxOneColumn = true,
				ColumnWidth = filtering ? viewRect.width - 10f : viewRect.width / 3.15f
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
			using (var freq = scope.Group("SexTypeFrequency"))
			{
				FrequencyRow(freq, ref vaginal, "vaginal", "vaginal_desc");
				FrequencyRow(freq, ref anal, "anal", "anal_desc");
				FrequencyRow(freq, ref double_penetration, "double_penetration", "double_penetration_desc");
				FrequencyRow(freq, ref fellatio, "fellatio", "fellatio_desc");
				FrequencyRow(freq, ref cunnilingus, "cunnilingus", "cunnilingus_desc");
				FrequencyRow(freq, ref rimming, "rimming", "rimming_desc");
				FrequencyRow(freq, ref sixtynine, "sixtynine", "sixtynine_desc");

				freq.Checkbox("FapEverywhere", "FapEverywhere_desc", ref FapEverywhere);
				freq.Checkbox("FapInBed", "FapInBed_desc", ref FapInBed);
				freq.Checkbox("FapInBelts", "FapInBelts_desc", ref FapInBelts);
				freq.Checkbox("FapInArmbinders", "FapInArmbinders_desc", ref FapInArmbinders);

				if (!filtering)
				{
					freq.Listing.NewColumn();
					if (freq.Listing.ButtonText("Reset".Translate()))
					{
						Find.WindowStack.Add(new Dialog_MessageBox(
							"SexFrequencyResetConfirm".Translate(),
							"Reset".Translate(), ResetFrequencies,
							"GoBack".Translate(), null, null, true));
					}
					freq.Listing.Gap(22f);
				}

				FrequencyRow(freq, ref breastjob, "breastjob", "breastjob_desc");
				FrequencyRow(freq, ref handjob, "handjob", "handjob_desc");
				FrequencyRow(freq, ref fingering, "fingering", "fingering_desc");
				FrequencyRow(freq, ref fisting, "fisting", "fisting_desc");
				FrequencyRow(freq, ref mutual_masturbation, "mutual_masturbation", "mutual_masturbation_desc");
				FrequencyRow(freq, ref footjob, "footjob", "footjob_desc");
				FrequencyRow(freq, ref scissoring, "scissoring", "scissoring_desc");
			}

			scope.Dropdown("Malesex", null, () => "Malesex".Translate() + ("AllowedSex." + Malesex).Translate(), () => null,
				[
					new DropdownOption("AllowedSex.All", () => Malesex = AllowedSex.All),
					new DropdownOption("AllowedSex.Homo", () => Malesex = AllowedSex.Homo),
					new DropdownOption("AllowedSex.Nohomo", () => Malesex = AllowedSex.Nohomo)
				],
				DropdownStyle.Custom, false
			);
			scope.Dropdown("FeMalesex", null, () => "FeMalesex".Translate() + ("AllowedSex." + FeMalesex).Translate(), () => null,
				[
					new DropdownOption("AllowedSex.All", () => FeMalesex = AllowedSex.All),
					new DropdownOption("AllowedSex.Homo", () => FeMalesex = AllowedSex.Homo),
					new DropdownOption("AllowedSex.Nohomo", () => FeMalesex = AllowedSex.Nohomo)
				],
				DropdownStyle.Custom, false
			);

			// 群交上限：仅影响自愿性行为与强暴，动物/机械固定一对一。
			scope.Slider("MaxSexPartners", "MaxSexPartners_desc", ref max_sex_partners, 1, 8, v => v.ToString());
			scope.Dropdown("GroupSexSettlement", "GroupSexSettlement_desc",
				() => "GroupSexSettlement".Translate() + ("GroupSexSettlement_" + group_sex_settlement).Translate(), () => null,
				[
					new DropdownOption("GroupSexSettlement_PerPartner", () => group_sex_settlement = GroupSexSettlement.PerPartner),
					new DropdownOption("GroupSexSettlement_Once", () => group_sex_settlement = GroupSexSettlement.Once)
				],
				DropdownStyle.Custom, false
			);

			// 邀请加入的搜索范围：同房间始终可选，此外允许半径内的其他人。
			scope.Slider("InviteRange", "InviteRange_desc", ref invite_range_cells, 1, 100, v => v.ToString());

			if (!filtering)
			{
				scope.Listing.NewColumn();
			}
			scope.Dropdown("SexClothing", null, () => "SexClothing".Translate() + sex_wear.ToString(), () => null,
				[
					new DropdownOption("SexClothingNude", () => sex_wear = Clothing.Nude),
					new DropdownOption("SexClothingHeadwear", () => sex_wear = Clothing.Headgear),
					new DropdownOption("SexClothingFull", () => sex_wear = Clothing.Clothed)
				],
				DropdownStyle.Custom, false
			);
			if (!filtering)
				scope.Listing.Gap(4f);
			scope.Dropdown("RapeAttemptAlert", null, () => "RapeAttemptAlert".Translate() + rape_attempt_alert.ToString(), () => null,
				[
					new DropdownOption("RapeAttemptAlertAlways", () => rape_attempt_alert = RapeAlert.Enabled),
					new DropdownOption("RapeAttemptAlertHumanlike", () => rape_attempt_alert = RapeAlert.Humanlikes),
					new DropdownOption("RapeAttemptAlertColonist", () => rape_attempt_alert = RapeAlert.Colonists),
					new DropdownOption("RapeAttemptAlertSilent", () => rape_attempt_alert = RapeAlert.Silent),
					new DropdownOption("RapeAttemptAlertDisabled", () => rape_attempt_alert = RapeAlert.Disabled)
				],
				DropdownStyle.Custom, false
			);
			scope.Dropdown("RapeAlert", null, () => "RapeAlert".Translate() + rape_alert.ToString(), () => null,
				[
					new DropdownOption("RapeAlertAlways", () => rape_alert = RapeAlert.Enabled),
					new DropdownOption("RapeAlertHumanlike", () => rape_alert = RapeAlert.Humanlikes),
					new DropdownOption("RapeAlertColonist", () => rape_alert = RapeAlert.Colonists),
					new DropdownOption("RapeAlertSilent", () => rape_alert = RapeAlert.Silent),
					new DropdownOption("RapeAlertDisabled", () => rape_alert = RapeAlert.Disabled)
				],
				DropdownStyle.Custom, false
			);
			scope.Checkbox("RapeAlertCP", "RapeAlertCP_desc", ref ShowForCP);
			scope.Checkbox("RapeAlertBreeding", "RapeAlertBreeding_desc", ref ShowForBreeding);

			using (var distr = scope.Group("SexualitySpread1", "SexualitySpread2", 0f, DrawSexualityTitle))
			{
				DrawSexualitySpreadSettings(distr, xxx.RimpsycheSexualityIsActive);
			}
		}

		private static void ResetFrequencies()
		{
			vaginal = 1.20f;
			anal = 0.80f;
			fellatio = 0.80f;
			cunnilingus = 0.80f;
			rimming = 0.40f;
			double_penetration = 0.60f;
			breastjob = 0.50f;
			handjob = 0.80f;
			mutual_masturbation = 0.70f;
			fingering = 0.50f;
			footjob = 0.30f;
			scissoring = 0.50f;
			fisting = 0.30f;
			sixtynine = 0.69f;
		}

		private static void FrequencyRow(SearchSection sec, ref float value, string labelKey, string tooltipKey)
		{
			if (!sec.Row(labelKey, tooltipKey))
				return;
			CreateScoreLabel(sec.Listing, value, labelKey, tooltipKey);
			value = sec.Listing.Slider(value, minFrequency, maxFrequency);
		}

		private static void DrawSexualityTitle(Listing_Standard listing)
		{
			listing.Gap(12f);
			listing.GapLine();
			listing.Gap(12f);
			using (UiScope.title)
			{
				listing.Label("SexualitySpread1".Translate(), -1, "SexualitySpread2".Translate());
			}
		}

		private static readonly string[] SexualityRatioKeys =
		[
			"SexualityAsexual", "SexualityAsexual_desc",
			"SexualityPansexual", "SexualityPansexual_desc",
			"SexualityHeterosexual", "SexualityHeterosexual_desc",
			"SexualityBisexual", "SexualityBisexual_desc",
			"SexualityGay", "SexualityGay_desc",
		];

		internal static void DrawSexualitySpreadSettings(SearchSection sec, bool isEnforced)
		{
			sec.Dropdown("SexualitySpread1", "SexualitySpread2", () => sexuality_distribution.ToString(), () => null,
				[
					new DropdownOption("SexualitySpreadVanillaDropdown", () => sexuality_distribution = Rjw_sexuality.Vanilla),
					new DropdownOption("SexualitySpreadRimJobWorldDropdown", () => sexuality_distribution = Rjw_sexuality.RimJobWorld),
					new DropdownOption("SexualitySpreadIndividualityDropdown", () => sexuality_distribution = Rjw_sexuality.SYRIndividuality),
					new DropdownOption("SexualitySpreadPsychologyDropdown", () => sexuality_distribution = Rjw_sexuality.Psychology)
				],
				DropdownStyle.Custom, isEnforced, SexualityRatioKeys
			);

			using (new UiScope(color: isEnforced ? new Color(0.7f, 0.7f, 0.7f) : GUI.color))
			{
				if (sexuality_distribution == Rjw_sexuality.RimJobWorld)
				{
					float total = GetRatioTotal();
					if (sec.Row("SexualityAsexual", "SexualityAsexual_desc"))
					{
						sec.Listing.Label("SexualityAsexual".Translate() + Math.Round(asexual_ratio / total * 100f, 1) + "%", -1, new TipSignal("SexualityAsexual_desc".Translate()));
						asexual_ratio = sec.Listing.Slider(asexual_ratio, 0.0f, 1.0f);
					}
					if (sec.Row("SexualityPansexual", "SexualityPansexual_desc"))
					{
						sec.Listing.Label("SexualityPansexual".Translate() + Math.Round(pansexual_ratio / total * 100f, 1) + "%", -1, new TipSignal("SexualityPansexual_desc".Translate()));
						pansexual_ratio = sec.Listing.Slider(pansexual_ratio, 0.0f, 1.0f);
					}
					if (sec.Row("SexualityHeterosexual", "SexualityHeterosexual_desc"))
					{
						sec.Listing.Label("SexualityHeterosexual".Translate() + Math.Round(heterosexual_ratio / total * 100f, 1) + "%", -1, new TipSignal("SexualityHeterosexual_desc".Translate()));
						heterosexual_ratio = sec.Listing.Slider(heterosexual_ratio, 0.0f, 1.0f);
					}
					if (sec.Row("SexualityBisexual", "SexualityBisexual_desc"))
					{
						sec.Listing.Label("SexualityBisexual".Translate() + Math.Round(bisexual_ratio / total * 100f, 1) + "%", -1, new TipSignal("SexualityBisexual_desc".Translate()));
						bisexual_ratio = sec.Listing.Slider(bisexual_ratio, 0.0f, 1.0f);
					}
					if (sec.Row("SexualityGay", "SexualityGay_desc"))
					{
						sec.Listing.Label("SexualityGay".Translate() + Math.Round(homosexual_ratio / total * 100f, 1) + "%", -1, new TipSignal("SexualityGay_desc".Translate()));
						homosexual_ratio = sec.Listing.Slider(homosexual_ratio, 0.0f, 1.0f);
					}
				}
				else
				{
					if (!xxx.IndividualityIsActive && sexuality_distribution == Rjw_sexuality.SYRIndividuality)
						sexuality_distribution = Rjw_sexuality.Vanilla;
					else if (sexuality_distribution == Rjw_sexuality.SYRIndividuality)
					{
						if (sec.Row("SexualitySpread1", "SexualitySpread2", "SexualitySpreadIndividuality"))
							sec.Listing.Label("SexualitySpreadIndividuality".Translate());
					}

					else if (!xxx.PsychologyIsActive && sexuality_distribution == Rjw_sexuality.Psychology)
						sexuality_distribution = Rjw_sexuality.Vanilla;
					else if (sexuality_distribution == Rjw_sexuality.Psychology)
					{
						if (sec.Row("SexualitySpread1", "SexualitySpread2", "SexualitySpreadPsychology"))
							sec.Listing.Label("SexualitySpreadPsychology".Translate());
					}

					else if (!xxx.RimpsycheSexualityIsActive && sexuality_distribution == Rjw_sexuality.RimPsyche)
						sexuality_distribution = Rjw_sexuality.Vanilla;
					else if (sexuality_distribution == Rjw_sexuality.RimPsyche)
					{
						if (sec.Row("SexualitySpread1", "SexualitySpread2", "SexualitySpreadRimpsyche"))
							sec.Listing.Label("SexualitySpreadRimpsyche".Translate());
					}

					else if (sec.Row("SexualitySpread1", "SexualitySpread2", "SexualitySpreadVanilla"))
						sec.Listing.Label("SexualitySpreadVanilla".Translate());
				}
			}
		}

		public static void CreateScoreLabel(Listing_Standard listingStandard, float sexType, string sexTypeName, string sexTypeDescription)
		{
			Rect rect = listingStandard.GetRect(Text.LineHeight);
			rect.SplitVertically(rect.width / 1.5f, out Rect leftCell, out Rect rightCell);
			rightCell.SplitVertically(rightCell.width / 2, out Rect percentCell, out Rect scoreCell);

			using (new UiScope(contentColor: Color.cyan))
			{
				Widgets.Label(
					leftCell,
					$"  {sexTypeName.Translate()}"
				);
			}
			Widgets.Label(
				percentCell,
				$"{Math.Round(sexType / GetFrequencyTotal() * 100, 1)}%"
			);
			Widgets.Label(
				scoreCell,
				$"({Math.Round(sexType * 100, 0)})"
			);

			TooltipHandler.TipRegion(rect, sexTypeDescription.Translate());
		}

		public static float GetFrequencyTotal()
		{
			return vaginal + anal + fellatio + cunnilingus + rimming + double_penetration + breastjob + handjob + mutual_masturbation + fingering + footjob + scissoring + fisting + sixtynine;
		}

		public static float GetRatioTotal()
		{
			return asexual_ratio + pansexual_ratio + heterosexual_ratio + bisexual_ratio + homosexual_ratio;
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
			Scribe_Values.Look(ref vaginal, nameof(vaginal) + "_frequency", vaginal, true);
			Scribe_Values.Look(ref anal, nameof(anal) + "_frequency", anal, true);
			Scribe_Values.Look(ref fellatio, nameof(fellatio) + "_frequency", fellatio, true);
			Scribe_Values.Look(ref cunnilingus, nameof(cunnilingus) + "_frequency", cunnilingus, true);
			Scribe_Values.Look(ref rimming, nameof(rimming) + "_frequency", rimming, true);
			Scribe_Values.Look(ref double_penetration, nameof(double_penetration) + "_frequency", double_penetration, true);
			Scribe_Values.Look(ref sixtynine, nameof(sixtynine) + "_frequency", sixtynine, true);
			Scribe_Values.Look(ref breastjob, nameof(breastjob) + "_frequency", breastjob, true);
			Scribe_Values.Look(ref handjob, nameof(handjob) + "_frequency", handjob, true);
			Scribe_Values.Look(ref footjob, nameof(footjob) + "_frequency", footjob, true);
			Scribe_Values.Look(ref fingering, nameof(fingering) + "_frequency", fingering, true);
			Scribe_Values.Look(ref fisting, nameof(fisting) + "_frequency", fisting, true);
			Scribe_Values.Look(ref mutual_masturbation, nameof(mutual_masturbation) + "_frequency", mutual_masturbation, true);
			Scribe_Values.Look(ref scissoring, nameof(scissoring) + "_frequency", scissoring, true);
			Scribe_Values.Look(ref asexual_ratio, nameof(asexual_ratio), asexual_ratio, true);
			Scribe_Values.Look(ref pansexual_ratio, nameof(pansexual_ratio), pansexual_ratio, true);
			Scribe_Values.Look(ref heterosexual_ratio, nameof(heterosexual_ratio), heterosexual_ratio, true);
			Scribe_Values.Look(ref bisexual_ratio, nameof(bisexual_ratio), bisexual_ratio, true);
			Scribe_Values.Look(ref homosexual_ratio, nameof(homosexual_ratio), homosexual_ratio, true);
			Scribe_Values.Look(ref FapEverywhere, nameof(FapEverywhere), FapEverywhere, true);
			Scribe_Values.Look(ref FapInBed, nameof(FapInBed), FapInBed, true);
			Scribe_Values.Look(ref FapInBelts, nameof(FapInBelts), FapInBelts, true);
			Scribe_Values.Look(ref FapInArmbinders, nameof(FapInArmbinders), FapInArmbinders, true);
			Scribe_Values.Look(ref sex_wear, nameof(sex_wear), sex_wear, true);
			Scribe_Values.Look(ref rape_attempt_alert, nameof(rape_attempt_alert), rape_attempt_alert, true);
			Scribe_Values.Look(ref rape_alert, nameof(rape_alert), rape_alert, true);
			Scribe_Values.Look(ref ShowForCP, nameof(ShowForCP), ShowForCP, true);
			Scribe_Values.Look(ref ShowForBreeding, nameof(ShowForBreeding), ShowForBreeding, true);
			Scribe_Values.Look(ref sexuality_distribution, nameof(sexuality_distribution), sexuality_distribution, true);
			Scribe_Values.Look(ref Malesex, nameof(Malesex), Malesex, true);
			Scribe_Values.Look(ref FeMalesex, nameof(FeMalesex), FeMalesex, true);
			Scribe_Values.Look(ref max_sex_partners, nameof(max_sex_partners), 3, true);
			Scribe_Values.Look(ref group_sex_settlement, nameof(group_sex_settlement), GroupSexSettlement.PerPartner, true);
			Scribe_Values.Look(ref invite_range_cells, nameof(invite_range_cells), 45, true);
			invite_range_cells = Mathf.Clamp(invite_range_cells, 1, 100);
			max_sex_partners = Mathf.Clamp(max_sex_partners, 1, 8);
		}
	}
}
