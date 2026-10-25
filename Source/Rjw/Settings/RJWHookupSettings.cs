using System;
using RimWorld;
using UnityEngine;
using Verse;
using rjw.Settings;
using rjw.RJWUI.RJWidgets;

namespace rjw
{
	public class RJWHookupSettings
	{
		public static bool HookupsEnabled = true;
		public static bool QuickHookupsEnabled = true;
		public static bool FreeLovinMeansPoly = true;
		public static bool SharedBedMeansPoly = true;
		public static bool CheatIfSafe = true;
		public static bool NoHookupsDuringWorkHours = true;
		public static bool ColonistsCanHookup = true;
		public static bool ColonistsCanHookupWithVisitor = false;
		public static bool CanHookupWithPrisoner = false;
		public static bool VisitorsCanHookupWithColonists = false;
		public static bool VisitorsCanHookupWithVisitors = true;
		public static bool PrisonersCanHookupWithNonPrisoner = false;
		public static bool PrisonersCanHookupWithPrisoner = true;
		public static float HookupChanceForNonNymphos = 0.3f;
		public static float MinimumFuckabilityToHookup = 0.1f;
		public static float MinimumAttractivenessToHookup = 0.5f;
		public static int MinimumRelationshipToHookup = 20;

		//public static bool NymphosCanPickAnyone = true;
		public static bool NymphosCanCheat = true;
		public static bool NymphosCanHomewreck = true;
		public static bool NymphosCanHomewreckReverse = true;

		private static Vector2 scrollPosition;
		private static float height_modifier = 0f;
		private static float sectionHeight = Text.LineHeight * 30f; // 初始时略微放大，在释放时按实测内容高度校正

		/// <summary>最近一次 <see cref="DoWindowContents"/> 绘制的活动根作用域；尚未绘制时为 null。</summary>
		private static SearchSection? liveScope;

		/// <summary>
		/// 最近一次实际绘制是否匹配到任何内容；若该标签页尚未绘制则为 null。
		/// 让 <see cref="RJWSettingsController"/> 无需执行索引即可刷新所选标签页的标记。
		/// </summary>
		internal static bool? LiveAnyMatch => liveScope?.AnyMatched;

		public static void DoWindowContents(Rect inRect)
		{
			MinimumFuckabilityToHookup = Mathf.Clamp(MinimumFuckabilityToHookup, 0.1f, 1f);
			MinimumAttractivenessToHookup = Mathf.Clamp(MinimumAttractivenessToHookup, 0.0f, 1f);
			MinimumRelationshipToHookup = Math.Clamp(MinimumRelationshipToHookup, -100, 100);

			var f = RJWSettingsController.ActiveFilter;
			Rect outRect = new(inRect.x, inRect.y + 30f, inRect.width, inRect.height - 30f);
			Rect viewRect = new(inRect.x, inRect.y, inRect.width - 16f, inRect.height + height_modifier);

			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.maxOneColumn = true;
			listingStandard.Begin(viewRect);

			var scope = new SearchSection(listingStandard, f);
			liveScope = scope;
			DrawBody(scope);

			listingStandard.End();
			height_modifier = Math.Max(0, listingStandard.MaxColumnHeightSeen - inRect.height);
			Widgets.EndScrollView();
		}

		private static void DrawBody(SearchSection scope)
		{
			// 随意性行为设置
			using (var hookups = scope.Toggle("SettingHookupsEnabled", "SettingHookupsEnabled_desc", HookupsEnabled, v => HookupsEnabled = v, sectionHeight, 4f, h => sectionHeight = h))
			{
				hookups.Checkbox("SettingQuickHookupsEnabled", "SettingQuickHookupsEnabled_desc", ref QuickHookupsEnabled);
				hookups.Checkbox("SettingFreeLovinMeansPoly", "SettingFreeLovinMeansPoly_desc", ref FreeLovinMeansPoly);
				hookups.Checkbox("SettingSharedBedMeansPoly", "SettingSharedBedMeansPoly_desc", ref SharedBedMeansPoly);
				hookups.Checkbox("SettingCheatIfSafe", "SettingCheatIfSafe_desc", ref CheatIfSafe);
				hookups.Checkbox("SettingNoHookupsDuringWorkHours", "SettingNoHookupsDuringWorkHours_desc", ref NoHookupsDuringWorkHours);

				hookups.Checkbox("SettingColonistsCanHookup", "SettingColonistsCanHookup_desc", ref ColonistsCanHookup);
				hookups.Checkbox("SettingColonistsCanHookupWithVisitor", "SettingColonistsCanHookupWithVisitor_desc", ref ColonistsCanHookupWithVisitor);
				hookups.Checkbox("SettingVisitorsCanHookupWithColonists", "SettingVisitorsCanHookupWithColonists_desc", ref VisitorsCanHookupWithColonists);
				hookups.Checkbox("SettingVisitorsCanHookupWithVisitors", "SettingVisitorsCanHookupWithVisitors_desc", ref VisitorsCanHookupWithVisitors);

				hookups.Checkbox("SettingPrisonersCanHookupWithNonPrisoner", "SettingPrisonersCanHookupWithNonPrisoner_desc", ref PrisonersCanHookupWithNonPrisoner);
				hookups.Checkbox("SettingPrisonersCanHookupWithPrisoner", "SettingPrisonersCanHookupWithPrisoner_desc", ref PrisonersCanHookupWithPrisoner);
				hookups.Checkbox("SettingCanHookupWithPrisoner", "SettingCanHookupWithPrisoner_desc", ref CanHookupWithPrisoner);

				//hookups.Checkbox("SettingNymphosCanPickAnyone", "SettingNymphosCanPickAnyone_desc", ref NymphosCanPickAnyone);
				hookups.Checkbox("SettingNymphosCanCheat", "SettingNymphosCanCheat_desc", ref NymphosCanCheat);
				hookups.Checkbox("SettingNymphosCanHomewreck", "SettingNymphosCanHomewreck_desc", ref NymphosCanHomewreck);
				hookups.Checkbox("SettingNymphosCanHomewreckReverse", "SettingNymphosCanHomewreckReverse_desc", ref NymphosCanHomewreckReverse);

				hookups.SliderPercent("SettingHookupChanceForNonNymphos", "SettingHookupChanceForNonNymphos_desc", ref HookupChanceForNonNymphos, 0.0f, 1.0f);
				// 最小值必须大于 0.0，以免破坏 SexAppraiser.would_fuck() 中返回 0f 的硬失败分支
				hookups.SliderPercent("SettingMinimumFuckabilityToHookup", "SettingMinimumFuckabilityToHookup_desc", ref MinimumFuckabilityToHookup, 0.1f, 1.0f);
				hookups.SliderPercent("SettingMinimumAttractivenessToHookup", "SettingMinimumAttractivenessToHookup_desc", ref MinimumAttractivenessToHookup, 0.0f, 1.0f);
				int relationship = MinimumRelationshipToHookup;
				hookups.Slider("SettingMinimumRelationshipToHookup", "SettingMinimumRelationshipToHookup_desc", ref relationship, -100, 100, v => v.ToString());
				MinimumRelationshipToHookup = relationship;
			}
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
				DrawBody(index);
				return index.AnyMatched;
			}
		}

		internal static void ResetScroll() { scrollPosition = Vector2.zero; }

		public static void ExposeData()
		{
			Scribe_Values.Look(ref HookupsEnabled, "Setting" + nameof(HookupsEnabled));
			Scribe_Values.Look(ref QuickHookupsEnabled, "Setting" + nameof(QuickHookupsEnabled));
			Scribe_Values.Look(ref FreeLovinMeansPoly, "Setting" + nameof(FreeLovinMeansPoly), true);
			Scribe_Values.Look(ref SharedBedMeansPoly, "Setting" + nameof(SharedBedMeansPoly), true);
			Scribe_Values.Look(ref CheatIfSafe, "Setting" + nameof(CheatIfSafe), true);
			Scribe_Values.Look(ref NoHookupsDuringWorkHours, nameof(NoHookupsDuringWorkHours));
			Scribe_Values.Look(ref ColonistsCanHookup, "Setting" + nameof(ColonistsCanHookup));
			Scribe_Values.Look(ref ColonistsCanHookupWithVisitor, "Setting" + nameof(ColonistsCanHookupWithVisitor));
			Scribe_Values.Look(ref VisitorsCanHookupWithColonists, "Setting" + nameof(VisitorsCanHookupWithColonists));
			Scribe_Values.Look(ref VisitorsCanHookupWithVisitors, "Setting" + nameof(VisitorsCanHookupWithVisitors));
			Scribe_Values.Look(ref CanHookupWithPrisoner, "Setting" + nameof(CanHookupWithPrisoner));
			Scribe_Values.Look(ref PrisonersCanHookupWithNonPrisoner, "Setting" + nameof(PrisonersCanHookupWithNonPrisoner));
			Scribe_Values.Look(ref PrisonersCanHookupWithPrisoner, "Setting" + nameof(PrisonersCanHookupWithPrisoner));
			Scribe_Values.Look(ref NymphosCanCheat, "Setting" + nameof(NymphosCanCheat));
			Scribe_Values.Look(ref NymphosCanHomewreck, "Setting" + nameof(NymphosCanHomewreck));
			Scribe_Values.Look(ref NymphosCanHomewreckReverse, "Setting" + nameof(NymphosCanHomewreckReverse));
			Scribe_Values.Look(ref HookupChanceForNonNymphos, "Setting" + nameof(HookupChanceForNonNymphos));
			Scribe_Values.Look(ref MinimumFuckabilityToHookup, "Setting" + nameof(MinimumFuckabilityToHookup));
			Scribe_Values.Look(ref MinimumAttractivenessToHookup, "Setting" + nameof(MinimumAttractivenessToHookup));
			Scribe_Values.Look(ref MinimumRelationshipToHookup, "Setting" + nameof(MinimumRelationshipToHookup));
		}
	}
}
