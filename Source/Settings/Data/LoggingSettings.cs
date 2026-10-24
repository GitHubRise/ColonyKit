using System;
using Verse;

namespace ColonyKit
{
	/// <summary>日志级别。下拉框的演示数据，同时决定 GameComponent / 补丁写多少日志。</summary>
	public enum LogDetail
	{
		/// <summary>不写任何日志（测试日志按钮除外）。</summary>
		Off,

		/// <summary>只写周期汇总。</summary>
		Summary,

		/// <summary>汇总 + 每次生成的明细。</summary>
		Verbose
	}

	/// <summary>
	/// 「日志」页的数据。只放字段和 Scribe，不画 UI（UI 在 <see cref="LoggingSettingsPage"/>）。
	/// </summary>
	public class LoggingSettings : IExposable
	{
		/// <summary>日志级别，设置页用下拉框选择。</summary>
		public LogDetail detail = LogDetail.Summary;

		/// <summary>框架日志级别（ModLog）：Debug/Info/Warn/Error。</summary>
		public ModLogLevel logLevel = ModLogLevel.Info;

		/// <summary>
		/// 是否记录每一次生成的明细。只在 <see cref="LogDetail.Verbose"/> 下可勾选；
		/// 其他级别下设置页会把它画成禁用态。
		/// </summary>
		public bool verboseSpawnLog;

		/// <summary>汇总日志间隔，单位 tick。60000 tick = 游戏内 1 天。</summary>
		public int logIntervalTicks = 60000;

		/// <summary>自定义日志前缀；空串表示用默认的 <c>[ColonyKit]</c>。</summary>
		public string customPrefix = "";

		/// <summary>实际生效的前缀，供 <see cref="ModLog"/> 使用。</summary>
		public string EffectivePrefix =>
			customPrefix.NullOrEmpty() ? "[ColonyKit]" : customPrefix.Trim();

		public void ExposeData()
		{
			// key 是配置文件契约，发布后不要改名。
			Scribe_Values.Look(ref detail, "detail", LogDetail.Summary);
			Scribe_Values.Look(ref logLevel, "logLevel", ModLogLevel.Info);
			Scribe_Values.Look(ref verboseSpawnLog, "verboseSpawnLog", false);
			Scribe_Values.Look(ref logIntervalTicks, "logIntervalTicks", 60000);
			Scribe_Values.Look(ref customPrefix, "customPrefix", "");
		}

		public void Reset()
		{
			detail = LogDetail.Summary;
			logLevel = ModLogLevel.Info;
			verboseSpawnLog = false;
			logIntervalTicks = 60000;
			customPrefix = "";
		}

		/// <summary>把间隔夹到 600–60000、前缀截到 24 字符，并校验枚举合法。</summary>
		public bool Validate()
		{
			bool changed = false;

			if (logIntervalTicks < 600)
			{
				logIntervalTicks = 600;
				changed = true;
			}
			else if (logIntervalTicks > 60000)
			{
				logIntervalTicks = 60000;
				changed = true;
			}

			if (customPrefix == null)
			{
				customPrefix = "";
				changed = true;
			}
			else
			{
				string trimmed = customPrefix.Trim();
				if (trimmed.Length > 24)
				{
					trimmed = trimmed.Substring(0, 24);
				}
				if (trimmed != customPrefix)
				{
					customPrefix = trimmed;
					changed = true;
				}
			}

			if (!Enum.IsDefined(typeof(LogDetail), detail))
			{
				detail = LogDetail.Summary;
				changed = true;
			}

			if (!Enum.IsDefined(typeof(ModLogLevel), logLevel))
			{
				logLevel = ModLogLevel.Info;
				changed = true;
			}

			return changed;
		}
	}
}
