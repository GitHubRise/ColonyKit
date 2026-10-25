using System;
using System.IO;
using System.Text;
using Multiplayer.API;
using RimWorld;
using UnityEngine;
using Verse;

namespace rjw.RJWUI.WhatsNew;

/// <summary>
/// “新内容”窗口，每个模组版本在启动时显示一次。
/// 用户上次看到的版本保存在 <see cref="RJWSettings.lastSeenVersion"/> 中。
/// 更新日志在运行时从模组文件夹中的 <c>changelog.txt</c> 读取（见
/// <see cref="ChangelogFile"/>）。
/// </summary>
[StaticConstructorOnStartup]
public class Dialog_WhatsNew : Window
{
	private readonly string version;
	private readonly string body;
	private Vector2 scrollPosition;

	private static readonly Texture2D Logo = ContentFinder<Texture2D>.Get("RJW_Tab");
	private const float LogoSize = 40f;

	public override Vector2 InitialSize => new(640f, 460f);

	public Dialog_WhatsNew(string version, string body)
	{
		this.version = version;
		this.body = body;
		forcePause = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = false;
		soundClose = SoundDefOf.InfoCard_Close;
	}

	public override void DoWindowContents(Rect inRect)
	{
		float headerHeight = Mathf.Max(LogoSize, 42f);
		if (Logo != null)
		{
			GUI.DrawTexture(new Rect(0f, inRect.y, LogoSize, LogoSize), Logo);
		}
		float titleX = Logo != null ? LogoSize + 8f : 0f;
		using (new UiScope(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft))
		{
			Widgets.Label(new Rect(titleX, inRect.y, inRect.width - titleX, headerHeight), "RJW_WhatsNew_Title".Translate(version));
		}
		float y = inRect.y + headerHeight + 10f;

		Text.Font = GameFont.Small;
		Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.height - (y - inRect.y) - 35f - 5f);
		float width = outRect.width - 20f;
		Rect viewRect = new Rect(0f, 0f, width, Text.CalcHeight(body, width));
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), body);
		Widgets.EndScrollView();

		float footerY = inRect.height - 35f;
		Widgets.CheckboxLabeled(new Rect(0f, footerY, inRect.width / 2f - 70f, 35f), "RJW_WhatsNew_NeverShow".Translate(), ref RJWSettings.suppressWhatsNew);
		if (Widgets.ButtonText(new Rect(inRect.width / 2f - 60f, footerY, 120f, 35f), "CloseButton".Translate()))
		{
			Close();
		}
	}

	public override void Close(bool doCloseSound = true)
	{
		// 在每条关闭路径（按钮、X、Enter、Esc）上都持久化，以便该窗口
		// 每个版本只显示一次。
		PersistSeen();
		base.Close(doCloseSound);
	}

	private static void PersistSeen()
	{
		try
		{
			WhatsNewManager.MarkNewVersionSeen();
			LoadedModManager.GetMod<Settings.RJWSettingsController>()?.WriteSettings();
		}
		catch (Exception ex)
		{
			ModLog.Warning("Failed to save What's New seen-version: " + ex.Message);
		}
	}
}

/// <summary>
/// 在运行时读取模组的 <c>changelog.txt</c>。
/// 该文件逐行流式读取，而非整体加载。
/// </summary>
public static class ChangelogFile
{
	private const string FileName = "changelog.txt";
	private const string PendingHeader = "Next";

	/// <summary>
	/// 读取 <paramref name="version"/> 对应的条目。
	/// </summary>
	public static (string label, string body) GetSection(string version)
	{
		if (string.IsNullOrEmpty(version))
			return (version, string.Empty);

		string? path = ResolvePath();
		if (path == null)
			return (version, string.Empty);

		string baseVersion = BaseVersion(version);
		StringBuilder versionSection = new();
		StringBuilder baseSection = new();
		StringBuilder pendingSection = new();
		Target target = Target.None;
		bool versionHeaderFound = false;
		bool baseHeaderFound = false;
		bool pendingHeaderFound = false;

		try
		{
			using StreamReader reader = File.OpenText(path);
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				if (WhatsNewChangelog.IsVersionHeader(line))
				{
					// 当前运行版本的段落到下一个标题为止；其后的内容都无关。
					if (target == Target.Version && HasContent(versionSection))
						break;
					target = Target.None;

					string header = line.Trim();
					if (!versionHeaderFound && header.Equals(version, StringComparison.OrdinalIgnoreCase))
					{
						versionHeaderFound = true;
						target = Target.Version;
					}
					else if (baseVersion.Length > 0 && !baseHeaderFound && header.Equals(baseVersion, StringComparison.OrdinalIgnoreCase))
					{
						baseHeaderFound = true;
						target = Target.Base;
					}
					else if (!pendingHeaderFound && !header.Equals(version, StringComparison.OrdinalIgnoreCase) && header.Equals(PendingHeader, StringComparison.OrdinalIgnoreCase))
					{
						pendingHeaderFound = true;
						target = Target.Pending;
					}
					continue;
				}

				switch (target)
				{
					case Target.Version: versionSection.AppendLine(line); break;
					case Target.Base: baseSection.AppendLine(line); break;
					case Target.Pending: pendingSection.AppendLine(line); break;
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"Failed to read {FileName}: {ex.Message}");
			return (version, string.Empty);
		}

		if (HasContent(versionSection))
			return (version, versionSection.ToString().Trim());
		if (!versionHeaderFound && HasContent(baseSection))
			return (version, baseSection.ToString().Trim());
		if (HasContent(pendingSection))
			return (PendingHeader, pendingSection.ToString().Trim());
		return (version, string.Empty);
	}

	private enum Target
	{
		None,
		Version,
		Base,
		Pending
	}

	/// <summary>模组 <c>changelog.txt</c> 的路径，不可用时为 <c>null</c>。</summary>
	private static string? ResolvePath()
	{
		ModMetaData mod = ModLister.GetModWithIdentifier("rim.job.world");
		if (mod == null)
			return null;

		// 不区分大小写地查找，以便在区分大小写的文件系统上仍能找到该文件。
		string path = GenFile.ResolveCaseInsensitiveFilePath(mod.RootDir.FullName, FileName);
		if (string.IsNullOrEmpty(path))
		{
			ModLog.Warning($"{FileName} not found in {mod.RootDir.FullName}, What's New window will show fallback text.");
			return null;
		}
		return path;
	}

	/// <summary>不含 <c>-Dev</c> 这类 CI 后缀的版本，若没有则为空。</summary>
	private static string BaseVersion(string version)
	{
		int dash = version.IndexOf('-');
		return dash > 0 ? version[..dash] : string.Empty;
	}

	private static bool HasContent(StringBuilder section)
	{
		for (int i = 0; i < section.Length; i++)
		{
			if (!char.IsWhiteSpace(section[i]))
				return true;
		}
		return false;
	}
}

public static class WhatsNewManager
{
	private static bool shownThisSession;

	/// <summary>当前运行的 RJW 版本，例如 "6.2.1"。模组元数据不可用时为空。</summary>
	public static string CurrentVersion => ModLister.GetModWithIdentifier("rim.job.world")?.ModVersion?.Trim() ?? string.Empty;

	/// <summary>当“新内容”窗口已针对当前版本显示过时为 true。</summary>
	public static bool IsNewVersionSeen()
	{
		string current = CurrentVersion;
		if (string.IsNullOrEmpty(current))
			return true;
		return !string.IsNullOrEmpty(RJWSettings.lastSeenVersion) && RJWSettings.lastSeenVersion == current;
	}

	public static void MarkNewVersionSeen()
	{
		string current = CurrentVersion;
		if (!string.IsNullOrEmpty(current))
			RJWSettings.lastSeenVersion = current;
	}

	public static void ShowIfNeeded()
	{
		if (shownThisSession)
			return;
		if (MP.IsInMultiplayer)
			return;
		if (RJWSettings.suppressWhatsNew)
			return;
		if (IsNewVersionSeen())
			return;
		Open();
	}

	/// <summary>
	/// 重置“新内容”状态，使窗口下次能再次显示
	/// </summary>
	public static void ResetSeen()
	{
		RJWSettings.suppressWhatsNew = false;
		RJWSettings.lastSeenVersion = string.Empty;
		shownThisSession = false;
		try
		{
			LoadedModManager.GetMod<Settings.RJWSettingsController>()?.WriteSettings();
		}
		catch (Exception ex)
		{
			ModLog.Warning("Failed to save reset What's New state: " + ex.Message);
		}
	}

	private static void Open()
	{
		if (Find.WindowStack == null)
			return;
		string version = CurrentVersion;
		if (string.IsNullOrEmpty(version))
			return;

		(string label, string body) = ChangelogFile.GetSection(version);
		if (string.IsNullOrWhiteSpace(body))
			body = "RJW_WhatsNew_Fallback".Translate();

		shownThisSession = true;
		Find.WindowStack.Add(new Dialog_WhatsNew(label, body));
	}
}

/// <summary>识别更新日志的版本标题行。</summary>
internal static class WhatsNewChangelog
{
	/// <summary>
	/// 当 <paramref name="line"/> 是更新日志的版本标题时返回 true。
	/// <para>兼容的标题：<c>Next</c>、<c>6.2</c>、<c>6.2.1</c>、<c>6.1.0.1</c>、<c>6.0.0-rc7</c>、<c>6.2.1-Dev</c>。</para>
	/// <para>先检查 <c>^\d+\.\d+</c>，再检查 24 字符的单 token（<c>letter/digit/./-/_</c>）限制。</para>
	/// </summary>
	internal static bool IsVersionHeader(string line)
	{
		string header = line.Trim();

		if (header.Equals("Next", StringComparison.OrdinalIgnoreCase))
			return true;

		int leadingDigits = 0;
		while (leadingDigits < header.Length && header[leadingDigits] >= '0' && header[leadingDigits] <= '9')
			leadingDigits++;

		if (leadingDigits == 0)
			return false;
		if (leadingDigits + 1 >= header.Length)
			return false;
		if (header[leadingDigits] != '.')
			return false;
		if (header[leadingDigits + 1] < '0' || header[leadingDigits + 1] > '9')
			return false;

		if (header.Length > 24)
			return false;

		foreach (char c in header)
		{
			if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
				continue;
			return false;
		}

		return true;
	}
}

