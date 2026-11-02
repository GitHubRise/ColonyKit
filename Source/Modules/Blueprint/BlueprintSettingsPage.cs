using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Blueprint
{
	/// <summary>蓝图设置页：材料策略、捕获上限、模板/库限制。</summary>
	public sealed class BlueprintSettingsPage : SettingsPage
	{
		private static readonly BlueprintMaterialMode[] Modes =
		{
			BlueprintMaterialMode.PreferSource,
			BlueprintMaterialMode.Available,
			BlueprintMaterialMode.Cheapest
		};

		public override string Id => "blueprint";

		public override string TitleKey => "ColonyKit.Settings.Tab.Blueprint";

		public override int Order => 30;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			BlueprintSettings data = settings.GetModuleSettings<BlueprintSettings>();

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Blueprint.Hint".Loc());
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Tab.Blueprint".Loc());

			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Blueprint.MaterialMode".Loc(),
				data.materialMode,
				Modes,
				ModeLabel,
				mode => data.materialMode = mode);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Blueprint.MaterialMode.Desc".Loc());

			int side = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Blueprint.MaxSide".Loc(),
				data.maxSide,
				BlueprintSettings.MinMaxSide,
				BlueprintSettings.MaxMaxSide,
				1f,
				value => value.ToString("0")));
			data.maxSide = Mathf.Clamp(side, BlueprintSettings.MinMaxSide, BlueprintSettings.MaxMaxSide);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Blueprint.MaxSide.Desc".Loc());

			int entries = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Blueprint.MaxEntries".Loc(),
				data.maxEntries,
				BlueprintSettings.MinMaxEntries,
				BlueprintSettings.MaxMaxEntries,
				50f,
				value => value.ToString("0")));
			data.maxEntries = Mathf.Clamp(entries, BlueprintSettings.MinMaxEntries, BlueprintSettings.MaxMaxEntries);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Blueprint.MaxEntries.Desc".Loc());

			int records = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Blueprint.MaxRecords".Loc(),
				data.maxRecords,
				BlueprintSettings.MinMaxRecords,
				BlueprintSettings.MaxMaxRecords,
				8f,
				value => value.ToString("0")));
			data.maxRecords = Mathf.Clamp(records, BlueprintSettings.MinMaxRecords, BlueprintSettings.MaxMaxRecords);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Blueprint.MaxRecords.Desc".Loc());

			data.verboseLog = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Blueprint.Verbose".Loc(),
				ref data.verboseLog,
				"ColonyKit.Settings.Blueprint.Verbose.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.GetModuleSettings<BlueprintSettings>().Reset();
		}

		private static string ModeLabel(BlueprintMaterialMode mode)
		{
			switch (mode)
			{
				case BlueprintMaterialMode.Available:
					return "CK.Blueprint.Mode.Available".Loc();
				case BlueprintMaterialMode.Cheapest:
					return "CK.Blueprint.Mode.Cheapest".Loc();
				default:
					return "CK.Blueprint.Mode.PreferSource".Loc();
			}
		}
	}
}
