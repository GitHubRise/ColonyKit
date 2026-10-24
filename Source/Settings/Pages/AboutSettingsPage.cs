using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「关于」页：只读信息，给玩家看版本与框架能力。
	/// </summary>
	public sealed class AboutSettingsPage : SettingsPage
	{
		public override string Id => "about";

		public override string TitleKey => "ColonyKit.Settings.Tab.About";

		public override int Order => 90;

		// 只读页，不提供“重置本页”；控制器会隐藏对应按钮。
		public override bool CanReset => false;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			ModContentPack content = ColonyKitMod.Instance?.Content;

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Metadata".Loc());

			SettingsWidgets.Label(listing, "ColonyKit.Settings.About.Name".Loc(content?.Name ?? "?"));
			SettingsWidgets.Label(listing, "ColonyKit.Settings.About.PackageId".Loc(
				content?.PackageIdPlayerFacing ?? "?"));
			SettingsWidgets.Label(listing, "ColonyKit.Settings.About.Version".Loc(
				content?.ModMetaData?.ModVersion ?? "?"));

			listing.Gap();
			SettingsWidgets.Label(listing, "ColonyKit.Settings.About.Hint".Loc());
		}
	}
}
