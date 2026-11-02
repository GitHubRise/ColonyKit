using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Insulation
{
	/// <summary>「保温」设置页：开关、墙体热阻比、最大层数、自然岩层。</summary>
	public sealed class InsulationSettingsPage : SettingsPage
	{
		public override string Id => "insulation";

		public override string TitleKey => "ColonyKit.Settings.Tab.Insulation";

		public override int Order => 35;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			InsulationSettings data = settings.GetModuleSettings<InsulationSettings>();

			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.Hint".Loc());
			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Tab.Insulation".Loc());

			data.enabled = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Insulation.Enabled".Loc(),
				ref data.enabled,
				"ColonyKit.Settings.Insulation.Enabled.Desc".Loc());

			listing.Gap();

			data.wallResistanceRatio = SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Insulation.Ratio".Loc(),
				data.wallResistanceRatio,
				InsulationSettings.MinRatio,
				InsulationSettings.MaxRatio,
				0.1f,
				value => value.ToString("0.0"));
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.Ratio.Desc".Loc());

			int layers = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Insulation.MaxLayers".Loc(),
				data.maxLayers,
				InsulationSettings.MinLayers,
				InsulationSettings.MaxLayers,
				1f,
				value => value.ToString("0")));
			data.maxLayers = Mathf.Clamp(layers, InsulationSettings.MinLayers, InsulationSettings.MaxLayers);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.MaxLayers.Desc".Loc());

			data.includeNaturalRock = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Insulation.NaturalRock".Loc(),
				ref data.includeNaturalRock,
				"ColonyKit.Settings.Insulation.NaturalRock.Desc".Loc());

			listing.Gap();

			data.thickenRoof = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Insulation.ThickenRoof".Loc(),
				ref data.thickenRoof,
				"ColonyKit.Settings.Insulation.ThickenRoof.Desc".Loc());

			int roofLayers = Mathf.RoundToInt(SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Insulation.RoofMinLayers".Loc(),
				data.roofMinWallLayers,
				InsulationSettings.MinRoofWallLayers,
				InsulationSettings.MaxRoofWallLayers,
				1f,
				value => value.ToString("0")));
			data.roofMinWallLayers = Mathf.Clamp(
				roofLayers,
				InsulationSettings.MinRoofWallLayers,
				InsulationSettings.MaxRoofWallLayers);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.RoofMinLayers.Desc".Loc());

			listing.Gap();
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.Formula".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Insulation.RoofFormula".Loc());

			listing.Gap();
			data.cascadeFreeze = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Insulation.Cascade".Loc(),
				ref data.cascadeFreeze,
				"ColonyKit.Settings.Insulation.Cascade.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.GetModuleSettings<InsulationSettings>().Reset();
		}
	}
}
