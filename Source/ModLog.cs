using System;
using System.Collections.Generic;
using Verse;

namespace ModTemplate
{
	/// <summary>框架日志级别，数值越大越严重。</summary>
	public enum ModLogLevel
	{
		Debug = 0,
		Info = 1,
		Warn = 2,
		Error = 3
	}

	/// <summary>最近日志的快照条目，供诊断报告使用。</summary>
	public readonly struct ModLogEntry
	{
		public ModLogEntry(ModLogLevel level, string category, string message, int tick)
		{
			Level = level;
			Category = category;
			Message = message;
			Tick = tick;
		}

		public ModLogLevel Level { get; }
		public string Category { get; }
		public string Message { get; }
		public int Tick { get; }
	}

	/// <summary>
	/// 结构化日志：统一前缀 + 分类 + 级别 + 去重/节流 + 最近日志缓冲。
	/// </summary>
	/// <remarks>
	/// 输出格式：<c>[前缀][分类] 消息</c>；级别只决定是否输出、走哪个原版通道：
	/// Debug/Info → <c>Log.Message</c>，Warn → <c>Log.Warning</c>，Error → <c>Log.Error</c>。
	/// 「最近日志」环形缓冲供 <see cref="Diagnostics"/> 写入报告。
	/// </remarks>
	public static class ModLog
	{
		private const string DefaultPrefix = "[ModTemplate]";
		private const int RecentCapacity = 50;

		private static readonly List<ModLogEntry> recent = new List<ModLogEntry>();
		private static readonly HashSet<string> onceKeys = new HashSet<string>();
		private static readonly Dictionary<string, int> throttleUntil = new Dictionary<string, int>();
		private static readonly object gate = new object();

		/// <summary>当前级别；设置未就绪时按 Info。</summary>
		public static ModLogLevel Level =>
			ModTemplateMod.Settings?.logging?.logLevel ?? ModLogLevel.Info;

		/// <summary>实际生效前缀（设置页可改，空串回退默认）。</summary>
		public static string Prefix =>
			ModTemplateMod.Settings?.logging?.EffectivePrefix ?? DefaultPrefix;

		public static int OnceKeyCount
		{
			get { lock (gate) { return onceKeys.Count; } }
		}

		public static int ThrottleKeyCount
		{
			get { lock (gate) { return throttleUntil.Count; } }
		}

		/// <summary>最近日志的副本，最多 50 条。</summary>
		public static IReadOnlyList<ModLogEntry> Recent
		{
			get { lock (gate) { return new List<ModLogEntry>(recent); } }
		}

		public static void Debug(string category, string message) =>
			Write(ModLogLevel.Debug, category, message);

		public static void Info(string category, string message) =>
			Write(ModLogLevel.Info, category, message);

		public static void Warn(string category, string message) =>
			Write(ModLogLevel.Warn, category, message);

		public static void Error(string category, string message) =>
			Write(ModLogLevel.Error, category, message);

		public static void Error(string category, string message, Exception exception) =>
			Write(ModLogLevel.Error, category, message + "\n" + exception);

		/// <summary>兼容旧调用：按 Core / Info 处理。</summary>
		public static void Message(string message) => Info("Core", message);

		/// <summary>兼容旧调用：按 Core / Warn 处理。</summary>
		public static void Warning(string message) => Warn("Core", message);

		/// <summary>兼容旧调用：按 Core / Error 处理。</summary>
		public static void Error(string message) => Error("Core", message);

		/// <summary>同一个 key 只输出一次；受级别过滤。</summary>
		public static void Once(string key, ModLogLevel level, string category, string message)
		{
			if (level < Level)
			{
				return;
			}

			lock (gate)
			{
				if (!onceKeys.Add(key ?? category + ":" + message))
				{
					return;
				}
			}

			WriteCore(level, category, message);
		}

		/// <summary>同一个 key 在冷却时间内只输出一次；受级别过滤。</summary>
		public static void Throttled(
			string key,
			int cooldownTicks,
			ModLogLevel level,
			string category,
			string message)
		{
			if (level < Level)
			{
				return;
			}

			int now = Find.TickManager?.TicksGame ?? 0;
			string throttleKey = key ?? category + ":" + message;

			lock (gate)
			{
				if (throttleUntil.TryGetValue(throttleKey, out int next) && now < next)
				{
					return;
				}
				throttleUntil[throttleKey] = now + Math.Max(0, cooldownTicks);
			}

			WriteCore(level, category, message);
		}

		/// <summary>绕过级别过滤输出（诊断报告用）。</summary>
		public static void Force(string category, string message) =>
			WriteCore(ModLogLevel.Info, category, message);

		/// <summary>清空去重/节流/最近日志；读档或新游戏时调用。</summary>
		public static void ResetRuntimeState()
		{
			lock (gate)
			{
				onceKeys.Clear();
				throttleUntil.Clear();
				recent.Clear();
			}
		}

		private static void Write(ModLogLevel level, string category, string message)
		{
			if (level < Level)
			{
				return;
			}

			WriteCore(level, category, message);
		}

		private static void WriteCore(ModLogLevel level, string category, string message)
		{
			string safeCategory = category.NullOrEmpty() ? "Core" : category;
			string safeMessage = message ?? "";
			string line = Prefix + "[" + safeCategory + "] " + safeMessage;

			switch (level)
			{
				case ModLogLevel.Error:
					Log.Error(line);
					break;
				case ModLogLevel.Warn:
					Log.Warning(line);
					break;
				default:
					Log.Message(line);
					break;
			}

			int tick = Find.TickManager?.TicksGame ?? -1;
			lock (gate)
			{
				recent.Add(new ModLogEntry(level, safeCategory, safeMessage, tick));
				if (recent.Count > RecentCapacity)
				{
					recent.RemoveAt(0);
				}
			}
		}
	}
}
