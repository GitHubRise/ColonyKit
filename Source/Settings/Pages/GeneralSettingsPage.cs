using ColonyKit.Modules.Localization;
using RimWorld;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// 「常规设置」页：设置界面语言（默认简体中文）+ 翻译方案 + 日志/诊断。
	/// 旧「常规 / 日志 / 通知」三个模板页签与「汉化」页已移除，页面数量减少到 4 个。
	/// </summary>
	public sealed class GeneralSettingsPage : SettingsPage
	{
		private static readonly LogDetail[] Details =
		{
			LogDetail.Off,
			LogDetail.Summary,
			LogDetail.Verbose
		};

		private static readonly ModLogLevel[] Levels =
		{
			ModLogLevel.Debug,
			ModLogLevel.Info,
			ModLogLevel.Warn,
			ModLogLevel.Error
		};

		public override string Id => "general-settings";

		public override string TitleKey => "ColonyKit.Settings.Tab.GeneralSettings";

		public override int Order => 80;

		public override void DoContents(Listing_Standard listing, ColonyKitSettings settings)
		{
			GeneralSettings general = settings.general;
			LocalizationSettings localization = settings.GetModuleSettings<LocalizationSettings>();
			LoggingSettings logging = settings.logging;

			SettingsWidgets.FrameScope frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Section.DefaultLanguage".Loc());
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Language".Loc(),
				general.language,
				new[] { ModLanguage.Auto, ModLanguage.English, ModLanguage.ChineseSimplified },
				language => ("ColonyKit.Settings.Language." + language).Loc(),
				language => general.language = language);
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Language.Desc".Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Section.TranslationScheme".Loc());
			bool translationEnabled = localization.enabled;
			localization.enabled = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Localization.Enabled".Loc(),
				ref translationEnabled,
				"ColonyKit.Settings.Localization.Enabled.Desc".Loc());
			SettingsWidgets.Label(
				listing,
				"ColonyKit.Settings.Localization.CurrentScheme".Loc(localization.Current.name));
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Localization.EditSchemes".Loc()))
			{
				Find.WindowStack.Add(new Dialog_LocalizationSchemes(localization));
			}
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Localization.Hint".Loc());
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Section.LogOutput".Loc());
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Logging.Detail".Loc(),
				logging.detail,
				Details,
				detail => ("ColonyKit.Settings.LogDetail." + detail).Loc(),
				detail => logging.detail = detail);
			SettingsWidgets.Dropdown(
				listing,
				"ColonyKit.Settings.Logging.FrameworkLevel".Loc(),
				logging.logLevel,
				Levels,
				level => ("ColonyKit.LogLevel." + level).Loc(),
				level => logging.logLevel = level);

			bool canLogSpawns = logging.detail == LogDetail.Verbose;
			logging.verboseSpawnLog = SettingsWidgets.ToggleRow(
				listing,
				"ColonyKit.Settings.Logging.VerboseSpawn".Loc(),
				ref logging.verboseSpawnLog,
				canLogSpawns
					? "ColonyKit.Settings.Logging.VerboseSpawn.Desc".Loc()
					: "ColonyKit.Settings.Logging.VerboseSpawn.Locked".Loc(),
				enabled: canLogSpawns);

			logging.logIntervalTicks = (int)SettingsWidgets.Slider(
				listing,
				"ColonyKit.Settings.Logging.Interval".Loc(),
				logging.logIntervalTicks,
				600f,
				60000f,
				600f,
				value => "ColonyKit.Settings.Logging.IntervalValue".Loc(
					(int)value,
					(value / GenDate.TicksPerDay).ToString("0.0")).ToString());

			logging.customPrefix = SettingsWidgets.TextField(
				listing,
				"ColonyKit.Settings.Logging.Prefix".Loc(),
				logging.customPrefix,
				24,
				"ColonyKit.Settings.Logging.Prefix.Desc".Loc()).Trim();

			SettingsWidgets.Label(
				listing,
				"ColonyKit.Settings.Logging.EffectivePrefix".Loc(logging.EffectivePrefix));

			if (logging.detail != LogDetail.Off
				&& SettingsWidgets.Button(listing, "ColonyKit.Settings.Logging.TestLog".Loc()))
			{
				ModLog.Info("Settings", "test log line from the settings page");
			}
			SettingsWidgets.FrameEnd(listing, frame);

			frame = SettingsWidgets.FrameBegin(
				listing, "ColonyKit.Settings.Section.Diagnostics".Loc());
			SettingsWidgets.Description(listing, "ColonyKit.Settings.Diagnostics.Hint".Loc());
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Diagnostics.Dump".Loc()))
			{
				Diagnostics.Dump();
				NotificationService.Message("ColonyKit.Notification.Diagnostics".Loc());
			}
			if (SettingsWidgets.Button(listing, "ColonyKit.Settings.Diagnostics.Reset".Loc()))
			{
				ModLog.ResetRuntimeState();
				NotificationService.ResetThrottle();
				ModLog.Info("Settings", "log/menu throttle state reset");
			}
			SettingsWidgets.FrameEnd(listing, frame);
		}

		public override void Reset(ColonyKitSettings settings)
		{
			settings.general.Reset();
			settings.logging.Reset();
		}
	}
}
