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

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Behavior".Loc());
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.Settings.Notifications.Enabled".Loc(),
				ref data.enabled,
				"ModTemplate.Settings.Notifications.Enabled.Desc".Loc());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Throttle".Loc());
			data.cooldownTicks = (int)SettingsWidgets.Slider(
				listing,
				"ModTemplate.Settings.Notifications.Cooldown".Loc(),
				data.cooldownTicks,
				60f,
				6000f,
				60f,
				value => "ModTemplate.Settings.Notifications.CooldownValue".Loc((int)value).ToString());

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Test".Loc());
			SettingsWidgets.Label(listing, "ModTemplate.Settings.Notifications.TestHint".Loc());

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Notifications.TestMessage".Loc()))
			{
				NotificationService.Message("ModTemplate.Notification.TestMessage".Loc());
			}

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Notifications.TestLetter".Loc()))
			{
				NotificationService.Letter(
					"ModTemplate.Notification.TestLetterLabel".Loc(),
					"ModTemplate.Notification.TestLetterText".Loc());
			}

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Notifications.TestThrottled".Loc()))
			{
				NotificationService.ThrottledMessage(
					"settings.test",
					"ModTemplate.Notification.TestThrottled".Loc());
			}
		}

		public override void Reset(ModTemplateSettings settings)
		{
			settings.notifications.Reset();
		}
	}
}
