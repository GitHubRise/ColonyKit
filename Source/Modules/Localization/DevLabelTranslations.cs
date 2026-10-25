using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Verse;

namespace ColonyKit.Modules.Localization
{
	/// <summary>
	/// 开发者菜单翻译表：读取 Mod 根目录 Translations/DevMenuLabels.xml，
	/// 按当前翻译方案筛选词条，供 DebugActionNode.LabelNow 的 Postfix 调用。
	/// </summary>
	public static class DevLabelTranslations
	{
		private const string PackageId = "local.colonykit";
		private const string FolderName = "Translations";
		private const string FileName = "DevMenuLabels.xml";

		private sealed class Entry
		{
			public string Zh;
			public string Menu;
			public bool Minimal;
		}

		private static readonly Dictionary<string, string> menus =
			new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, Entry> entries =
			new Dictionary<string, Entry>(StringComparer.Ordinal);

		private static readonly Dictionary<string, Def> allDefs =
			new Dictionary<string, Def>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> thingLabels =
			new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> constants =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "awful", "可怕" },
				{ "poor", "较差" },
				{ "normal", "一般" },
				{ "good", "良好" },
				{ "excellent", "优秀" },
				{ "masterwork", "大师" },
				{ "legendary", "传奇" }
			};

		private static readonly Dictionary<string, string> tooltips =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "Open the debug log.", "打开调试日志。" },
				{ "Open tweakvalues menu.\n\nThis lets you change internal values.", "打开数值调整菜单。\n\n可以修改游戏内部数值。" },
				{ "Open debug actions menu.\n\nThis lets you spawn items and force various events.", "打开调试动作菜单。\n\n可以生成物品、强制触发各种事件。" },
				{ "Open the view settings.\n\nThis lets you see special debug visuals.", "打开视图设置。\n\n可以查看各种调试可视化效果。" },
				{ "Open debug logging menu.", "打开调试日志菜单。" },
				{ "Open the inspector.\n\nThis lets you inspect what's happening in the game, down to individual variables.", "打开检查器。\n\n可以查看游戏内发生了什么，细到单个变量。" },
				{ "Toggle the dev palette.\n\nAllows you to setup a palette of debug actions for ease of use.", "切换开发者调色板。\n\n可以设置常用调试动作的快捷面板。" },
				{ "Toggle god mode.\n\nWhen god mode is on, you can build stuff instantly, for free, and sell things that aren't yours.", "切换上帝模式。\n\n开启后可以瞬间免费建造，还能出售不属于你的东西。" }
			};

		public static string SourcePath { get; private set; }

		public static int Count => entries.Count;

		public static int MenuCount => menus.Count;

		public static int EnabledCount
		{
			get
			{
				int count = 0;
				foreach (Entry entry in entries.Values)
				{
					if (IsEnabled(entry))
					{
						count++;
					}
				}
				return count;
			}
		}

		public static void Load()
		{
			menus.Clear();
			entries.Clear();
			thingLabels.Clear();
			SourcePath = null;

			string path = FindFile();
			if (path == null)
			{
				ModLog.Error("Localization", $"translation table not found: {FolderName}/{FileName}");
				return;
			}

			try
			{
				XDocument document = XDocument.Load(path);
				XElement root = document.Root;
				if (root == null)
				{
					ModLog.Error("Localization", "empty translation table: " + path);
					return;
				}

				XElement menuRoot = root.Element("menus");
				if (menuRoot != null)
				{
					foreach (XElement menu in menuRoot.Elements("menu"))
					{
						string key = NormKey((string)menu.Attribute("key"));
						string zh = (string)menu.Attribute("zh");
						if (key.Length > 0 && !string.IsNullOrEmpty(zh))
						{
							menus[key] = zh;
						}
					}
				}

				XElement entryRoot = root.Element("entries");
				if (entryRoot != null)
				{
					foreach (XElement element in entryRoot.Elements("entry"))
					{
						string zh = (string)element.Attribute("zh");
						if (string.IsNullOrEmpty(zh))
						{
							continue;
						}

						string key = NormKey((string)element.Attribute("key"));
						if (key.Length == 0)
						{
							key = NormKey((string)element.Attribute("en"));
						}
						if (key.Length == 0)
						{
							continue;
						}

						bool minimal;
						bool.TryParse((string)element.Attribute("minimal"), out minimal);

						entries[key] = new Entry
						{
							Zh = zh,
							Menu = NormKey((string)element.Attribute("menu")),
							Minimal = minimal
						};
					}
				}

				SourcePath = path;

				allDefs.Clear();
				foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
				{
					foreach (Def def in mod.AllDefs)
					{
						if (def != null && !string.IsNullOrEmpty(def.defName) && !allDefs.ContainsKey(def.defName))
						{
							allDefs[def.defName] = def;
						}
					}
				}

				ModLog.Info(
					"Localization",
					$"translation table loaded: {Count} entries, {MenuCount} menus, {EnabledCount} enabled; {path}");
			}
			catch (Exception exception)
			{
				ModLog.Error("Localization", "failed to load translation table: " + path, exception);
			}
		}

		private static string FindFile()
		{
			ModContentPack mod = LoadedModManager.RunningModsListForReading
				.FirstOrDefault(pack => string.Equals(pack.PackageId, PackageId, StringComparison.OrdinalIgnoreCase));
			if (mod != null)
			{
				string path = Path.Combine(mod.RootDir, FolderName, FileName);
				if (File.Exists(path))
				{
					return path;
				}
			}

			try
			{
				string assemblyDir = Path.GetDirectoryName(typeof(DevLabelTranslations).Assembly.Location);
				if (!string.IsNullOrEmpty(assemblyDir))
				{
					string path = Path.Combine(Path.GetFullPath(Path.Combine(assemblyDir, "..")), FolderName, FileName);
					if (File.Exists(path))
					{
						return path;
					}
				}
			}
			catch
			{
				// ignored
			}

			return null;
		}

		public static string Translate(string label)
		{
			if (string.IsNullOrEmpty(label) || !LocalizationModule.ShouldTranslate)
			{
				return label;
			}

			// 工具类动作用 "T: " 前缀标记，先剥掉再查表。
			if (label.StartsWith("T: ", StringComparison.Ordinal) || label.StartsWith("T:", StringComparison.Ordinal))
			{
				label = label.Substring(2).TrimStart();
			}

			string key = NormKey(label);
			if (key.Length == 0)
			{
				return label;
			}

			string menuZh;
			if (menus.TryGetValue(key, out menuZh))
			{
				return menuZh;
			}

			Entry entry;
			if (entries.TryGetValue(key, out entry) && IsEnabled(entry))
			{
				return entry.Zh;
			}

			string constant;
			if (constants.TryGetValue(key, out constant))
			{
				return constant;
			}

			// 子菜单里很多条目直接显示 defName，回落到 Def 的本地化名称；
			// 若名称本身仍是英文，再按英文标签查一次表，避免整列英文。
			string defLabel = TryDefLabel(label);
			if (!string.IsNullOrEmpty(defLabel) && !string.Equals(defLabel, label, StringComparison.Ordinal))
			{
				string defKey = NormKey(defLabel);
				if (defKey.Length > 0 && entries.TryGetValue(defKey, out entry) && IsEnabled(entry))
				{
					return entry.Zh;
				}
				return defLabel;
			}

			return label;
		}

		public static bool HasTooltip(string text)
		{
			return !string.IsNullOrEmpty(text) && tooltips.ContainsKey(text);
		}

		public static string TranslateTooltip(string text)
		{
			if (!LocalizationModule.ShouldTranslate)
			{
				return text;
			}

			string zh;
			return !string.IsNullOrEmpty(text) && tooltips.TryGetValue(text, out zh) ? zh : text;
		}

		private static bool IsEnabled(Entry entry)
		{
			LocalizationSettings settings = ColonyKitMod.Settings?.GetModuleSettings<LocalizationSettings>();
			if (settings == null || settings.enabled == false)
			{
				return false;
			}

			TranslationScheme scheme = settings.Current;
			if (scheme == null)
			{
				return entry.Minimal;
			}

			if (entry.Minimal && scheme.minimal)
			{
				return true;
			}

			return !string.IsNullOrEmpty(entry.Menu) && scheme.menus.Contains(entry.Menu);
		}

		private static string TryDefLabel(string name)
		{
			string cached;
			if (thingLabels.TryGetValue(name, out cached))
			{
				return cached;
			}

			string result = null;
			try
			{
				const string minified = " (minified)";
				if (name.EndsWith(minified, StringComparison.Ordinal))
				{
					string baseLabel = TryDefLabel(name.Substring(0, name.Length - minified.Length));
					if (!string.IsNullOrEmpty(baseLabel))
					{
						result = baseLabel + "（已打包）";
					}
				}
				else
				{
					Def def;
					if (allDefs.TryGetValue(name, out def))
					{
						// 没有译名就原样显示（英文 defName / 英文 label），
						// 不再逐词硬拼中文，避免出现「绘制河流Debug」这类半英半中的乱码。
						result = def.LabelCap;
					}
				}
			}
			catch
			{
				// ignored
			}

			thingLabels[name] = result;
			return result;
		}

		private static string NormKey(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(text.Length);
			foreach (char c in text)
			{
				if (char.IsLetterOrDigit(c))
				{
					builder.Append(char.ToLowerInvariant(c));
				}
			}
			return builder.ToString();
		}
	}
}
