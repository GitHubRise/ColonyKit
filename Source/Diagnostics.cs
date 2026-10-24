using System;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ModTemplate
{
	/// <summary>
	/// 诊断报告：把版本、设置、各服务的状态和最近日志写成一份文本文件，
	/// 方便玩家在 `Player.log` 之外直接发给你。
	/// </summary>
	public static class Diagnostics
	{
		public const string FileName = "ModTemplate-diagnostics.txt";

		/// <summary>写报告到 Config 目录，并在日志里输出路径。</summary>
		public static void Dump()
		{
			string path = DumpToFile();
			if (path.NullOrEmpty())
			{
				return;
			}

			// 绕过级别过滤：即使玩家把日志调到 Error，也要看到报告位置。
			ModLog.Force("Diag", "diagnostics report written to " + path);
		}

		/// <summary>写报告；失败时记 Error 并返回 null。</summary>
		public static string DumpToFile()
		{
			try
			{
				string path = Path.Combine(GenFilePaths.ConfigFolderPath, FileName);
				File.WriteAllText(path, BuildReport(), Encoding.UTF8);
				return path;
			}
			catch (Exception exception)
			{
				ModLog.Error("Diag", "failed to write diagnostics report", exception);
				return null;
			}
		}

		/// <summary>生成报告文本（不落盘）。</summary>
		public static string BuildReport()
		{
			ModTemplateSettings settings = ModTemplateMod.Settings;
			ModContentPack content = ModTemplateMod.Instance?.Content;
			StringBuilder builder = new StringBuilder();

			builder.AppendLine("=== ModTemplate diagnostics ===");
			builder.AppendLine("time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			builder.AppendLine("tick=" + (Current.Game != null ? Find.TickManager?.TicksGame ?? -1 : -1));
			builder.AppendLine(
				"mod=" + (content?.Name ?? "?")
				+ " packageId=" + (content?.PackageIdPlayerFacing ?? "?")
				+ " version=" + (content?.ModMetaData?.ModVersion ?? "?"));
			builder.AppendLine("logLevel=" + ModLog.Level + " prefix=" + ModLog.Prefix);

			if (settings != null)
			{
				builder.AppendLine(
					"general: trackSpawns=" + settings.general.trackSpawns
					+ " enablePawnMenu=" + settings.general.enablePawnMenu
					+ " showTechTreeButton=" + settings.general.showTechTreeButton);
				builder.AppendLine(
					"logging: detail=" + settings.logging.detail
					+ " logLevel=" + settings.logging.logLevel
					+ " interval=" + settings.logging.logIntervalTicks
					+ " verboseSpawn=" + settings.logging.verboseSpawnLog);
				builder.AppendLine(
					"notifications: enabled=" + settings.notifications.enabled
					+ " cooldown=" + settings.notifications.cooldownTicks);
			}
			else
			{
				builder.AppendLine("settings=<null>");
			}

			builder.AppendLine("services:");
			builder.AppendLine("  pawnMenuEntries=" + PawnMenuService.Count);
			builder.AppendLine("  buildRules=" + BuildControlService.RuleCount);
			builder.AppendLine(
				"  notifications=" + NotificationService.Enabled
				+ " throttleKeys=" + NotificationService.ThrottleCount);
			builder.AppendLine(
				"  logOnceKeys=" + ModLog.OnceKeyCount
				+ " logThrottleKeys=" + ModLog.ThrottleKeyCount);

			MainButtonDef techButton = DefDatabase<MainButtonDef>
				.GetNamedSilentFail(MainButtonService.TechTreeButtonDefName);
			builder.AppendLine(
				"  techTreeButton=" + (techButton != null && MainButtonService.IsVisible(techButton)));

			ResearchTabDef tab = Current.Game != null
				? Find.ResearchManager?.GetProject()?.tab ?? ResearchTabDefOf.Main
				: null;
			int researchNodes = tab == null
				? 0
				: DefDatabase<ResearchProjectDef>.AllDefsListForReading
					.Count(project => project.tab == tab);
			builder.AppendLine(
				"  researchTab=" + (tab?.defName ?? "?")
				+ " researchProjects=" + researchNodes);

			DemoGameComponent component = Current.Game?.GetComponent<DemoGameComponent>();
			builder.AppendLine("  trackedSpawns=" + (component?.SpawnsTracked ?? -1));

			builder.AppendLine("recentLogs:");
			foreach (ModLogEntry entry in ModLog.Recent)
			{
				string message = entry.Message.Replace("\n", " ");
				if (message.Length > 200)
				{
					message = message.Substring(0, 200) + "…";
				}
				builder.AppendLine(
					"  [" + entry.Level + "][" + entry.Category + "] " + message);
			}

			return builder.ToString();
		}
	}
}
