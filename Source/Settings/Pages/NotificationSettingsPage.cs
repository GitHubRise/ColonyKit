using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「通知」页：总开关、节流冷却、三种测试入口。数据在 <see cref="NotificationSettings"/>。
	/// </summary>
	public sealed class NotificationSettingsPage : SettingsPage
	{
		public override string Id => "notifications";

		public override string TitleKey => "ModTemplate.Settings.Tab.Notifications";

		public override int Order => 30;

		public override void DoContents(Listing_Standard listing, ModTemplateSettings settings)
		{
			NotificationSettings data = settings.notifications;

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Behavior".Translate());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.Settings.Notifications.Enabled".Translate(),
				ref data.enabled,
				"ModTemplate.Settings.Notifications.Enabled.Desc".Translate());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Throttle".Translate());
			data.cooldownTicks = (int)SettingsWidgets.Slider(
				listing,
				"ModTemplate.Settings.Notifications.Cooldown".Translate(),
				data.cooldownTicks,
				60f,
				6000f,
				60f,
				value => "ModTemplate.Settings.Notifications.CooldownValue".Translate((int)value).ToString());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Test".Translate());
			listing.Label("ModTemplate.Settings.Notifications.TestHint".Translate());

			if (listing.ButtonText("ModTemplate.Settings.Notifications.TestMessage".Translate()))
			{
				NotificationService.Message("ModTemplate.Notification.TestMessage".Translate());
			}

			if (listing.ButtonText("ModTemplate.Settings.Notifications.TestLetter".Translate()))
			{
				NotificationService.Letter(
					"ModTemplate.Notification.TestLetterLabel".Translate(),
					"ModTemplate.Notification.TestLetterText".Translate());
			}

			if (listing.ButtonText("ModTemplate.Settings.Notifications.TestThrottled".Translate()))
			{
				NotificationService.ThrottledMessage(
					"settings.test",
					"ModTemplate.Notification.TestThrottled".Translate());
			}
		}

		public override void Reset(ModTemplateSettings settings)
		{
			settings.notifications.Reset();
		}
	}
}
