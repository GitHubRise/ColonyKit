using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「通知」页：总开关、节流冷却、三种测试入口。数据在 <see cref="NotificationSettings"/>。
	/// </summary>
	public sealed class NotificationSettingsPage : SettingsPage
	{
		public override string Id => "notifications";

		public override string TitleKey => "ColonyKit.Settings.Tab.Notifications";

		public override int Order => 30;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			NotificationSettings data = settings.notifications;

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Behavior".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ColonyKit.Settings.Notifications.Enabled".Loc(),
				ref data.enabled,
				"ColonyKit.Settings.Notifications.Enabled.Desc".Loc());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Throttle".Loc());
			data.cooldownTicks = (int)SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Notifications.Cooldown".Loc(),
				data.cooldownTicks,
				60f,
				6000f,
				60f,
				value => "ColonyKit.Settings.Notifications.CooldownValue".Loc((int)value).ToString());

			SettingsWidgets.Section(listing, "ColonyKit.Settings.Section.Test".Loc());
			SettingsWidgets.Label(listing, "ColonyKit.Settings.Notifications.TestHint".Loc());

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Notifications.TestMessage".Loc()))
			{
				NotificationService.Message("ColonyKit.Notification.TestMessage".Loc());
			}

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Notifications.TestLetter".Loc()))
			{
				NotificationService.Letter(
					"ColonyKit.Notification.TestLetterLabel".Loc(),
					"ColonyKit.Notification.TestLetterText".Loc());
			}

			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Notifications.TestThrottled".Loc()))
			{
				NotificationService.ThrottledMessage(
					"settings.test",
					"ColonyKit.Notification.TestThrottled".Loc());
			}
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.notifications.Reset();
		}
	}
}
