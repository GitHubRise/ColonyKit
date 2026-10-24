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

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Level".Loc());

			// 下拉框：选择日志级别。选项文本走翻译键，当前项带 ✓。
			SettingsWidgets.Dropdown(
				listing,
				"ModTemplate.Settings.Logging.Detail".Loc(),
				data.detail,
				Details,
				detail => ("ModTemplate.Settings.LogDetail." + detail).Loc(),
				detail => data.detail = detail);

			// 框架日志级别：控制 ModLog 输出多少（Debug/Info/Warn/Error）。
			SettingsWidgets.Dropdown(
				listing,
				"ModTemplate.Settings.Logging.FrameworkLevel".Loc(),
				data.logLevel,
				Levels,
				level => ("ModTemplate.LogLevel." + level).Loc(),
				level => data.logLevel = level);

			// 禁用态勾选：只有 Verbose 级别才能打开明细开关，
			// 其他级别灰显，并用 Tooltip 解释原因。
			bool canLogSpawns = data.detail == LogDetail.Verbose;
			SettingsWidgets.Checkbox(
				listing,
				"ModTemplate.Settings.Logging.VerboseSpawn".Loc(),
				ref data.verboseSpawnLog,
				canLogSpawns
					? "ModTemplate.Settings.Logging.VerboseSpawn.Desc".Loc()
					: "ModTemplate.Settings.Logging.VerboseSpawn.Locked".Loc(),
				enabled: canLogSpawns);

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Output".Loc());

			// 滑块：600–60000 tick，按 600 tick 吸附；右侧显示 tick + 约几天。
			data.logIntervalTicks = (int)SettingsWidgets.Slider(
				listing,
				"ModTemplate.Settings.Logging.Interval".Loc(),
				data.logIntervalTicks,
				600f,
				60000f,
				600f,
				value => "ModTemplate.Settings.Logging.IntervalValue".Loc(
					(int)value,
					(value / GenDate.TicksPerDay).ToString("0.0")).ToString());

			// 输入框：自定义前缀，空串回退默认值；最多 24 字符。
			data.customPrefix = SettingsWidgets.TextField(
				listing,
				"ModTemplate.Settings.Logging.Prefix".Loc(),
				data.customPrefix,
				24,
				"ModTemplate.Settings.Logging.Prefix.Desc".Loc()).Trim();

			SettingsWidgets.Label(listing, "ModTemplate.Settings.Logging.EffectivePrefix".Loc(data.EffectivePrefix));

			if (data.detail != LogDetail.Off && SettingsWidgets.Button(listing, "ModTemplate.Settings.Logging.TestLog".Loc()))
			{
				ModLog.Info("Settings", "test log line from the settings page");
			}

			SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Diagnostics".Loc());
			SettingsWidgets.Label(listing, "ModTemplate.Settings.Diagnostics.Hint".Loc());

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Diagnostics.Dump".Loc()))
			{
				Diagnostics.Dump();
				NotificationService.Message("ModTemplate.Notification.Diagnostics".Loc());
			}

			if (SettingsWidgets.Button(listing, "ModTemplate.Settings.Diagnostics.Reset".Loc()))
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
