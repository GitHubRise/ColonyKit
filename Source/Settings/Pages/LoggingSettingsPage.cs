using System;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 「日志」页：下拉框（级别）、滑块（间隔）、输入框（前缀）、
	/// 禁用态勾选（明细开关）都在这一页演示，分「级别 / 输出」两个 Section。
	/// </summary>
	public sealed class LoggingSettingsPage : SettingsPage
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

		public override string Id => "logging";

		public override string TitleKey => "ModTemplate.Settings.Tab.Logging";

		public override int Order => 20;

		public override void DoContents(Listing_Standard listing, ModTemplateSettings settings)
		{
			LoggingSettings data = settings.logging;

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Level".Translate());

			// 下拉框：选择日志级别。选项文本走翻译键，当前项带 ✓。
			SettingsWidgets.Dropdown(
				listing,
				"ModTemplate.Settings.Logging.Detail".Translate(),
				data.detail,
				Details,
				detail => ("ModTemplate.Settings.LogDetail." + detail).Translate(),
				detail => data.detail = detail);

			// 框架日志级别：控制 ModLog 输出多少（Debug/Info/Warn/Error）。
			SettingsWidgets.Dropdown(
				listing,
				"ModTemplate.Settings.Logging.FrameworkLevel".Translate(),
				data.logLevel,
				Levels,
				level => ("ModTemplate.LogLevel." + level).Translate(),
				level => data.logLevel = level);

			// 禁用态勾选：只有 Verbose 级别才能打开明细开关，
			// 其他级别灰显，并用 Tooltip 解释原因。
			bool canLogSpawns = data.detail == LogDetail.Verbose;
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.Settings.Logging.VerboseSpawn".Translate(),
				ref data.verboseSpawnLog,
				canLogSpawns
					? "ModTemplate.Settings.Logging.VerboseSpawn.Desc".Translate()
					: "ModTemplate.Settings.Logging.VerboseSpawn.Locked".Translate(),
				enabled: canLogSpawns);

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Output".Translate());

			// 滑块：600–60000 tick，按 600 tick 吸附；右侧显示 tick + 约几天。
			data.logIntervalTicks = (int)SettingsWidgets.Slider(
				listing,
				"ModTemplate.Settings.Logging.Interval".Translate(),
				data.logIntervalTicks,
				600f,
				60000f,
				600f,
				value => "ModTemplate.Settings.Logging.IntervalValue".Translate(
					(int)value,
					(value / GenDate.TicksPerDay).ToString("0.0")).ToString());

			// 输入框：自定义前缀，空串回退默认值；最多 24 字符。
			data.customPrefix = SettingsWidgets.TextField(
				listing,
				"ModTemplate.Settings.Logging.Prefix".Translate(),
				data.customPrefix,
				24,
				"ModTemplate.Settings.Logging.Prefix.Desc".Translate()).Trim();

			SettingsWidgets.Label(listing, "ModTemplate.Settings.Logging.EffectivePrefix".Translate(data.EffectivePrefix));

			if (data.detail != LogDetail.Off && SettingsWidgets.Button(listing, "ModTemplate.Settings.Logging.TestLog".Translate()))
			{
				ModLog.Info("Settings", "test log line from the settings page");
			}

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Diagnostics".Translate());
			SettingsWidgets.Label(listing, "ModTemplate.Settings.Diagnostics.Hint".Translate());

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Diagnostics.Dump".Translate()))
			{
				Diagnostics.Dump();
				NotificationService.Message("ModTemplate.Notification.Diagnostics".Translate());
			}

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Diagnostics.Reset".Translate()))
			{
				ModLog.ResetRuntimeState();
				NotificationService.ResetThrottle();
				ModLog.Info("Settings", "log/menu throttle state reset");
			}
		}

		public override void Reset(ModTemplateSettings settings)
		{
			settings.logging.Reset();
		}
	}
}
