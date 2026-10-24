using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Verse;

namespace ModTemplate
{
	/// <summary>设置界面的语言覆盖：自动跟随游戏，或强制英文/简中。</summary>
	public enum ModLanguage
	{
		Auto,
		English,
		ChineseSimplified
	}

	/// <summary>
	/// 读取本 Mod 自己的 Keyed 文件，按设置选择中英文；不改变游戏全局语言。
	/// </summary>
	/// <remarks>
	/// 只覆盖设置界面使用的 <c>".Loc()"</c> 调用；菜单/通知/研究树等仍由游戏语言决定。
	/// 字典在第一次使用时从 <c>Languages/&lt;Lang&gt;/Keyed</c> 加载，缺失时回退游戏翻译。
	/// </remarks>
	public static class ModLoc
	{
		private static readonly Dictionary<string, string> english =
			new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> chinese =
			new Dictionary<string, string>(StringComparer.Ordinal);

		private static bool loaded;

		/// <summary>设置界面专用翻译入口；key 为空返回空串。</summary>
		public static string Translate(string key, params object[] args)
		{
			if (key.NullOrEmpty())
			{
				return "";
			}

			ModLanguage language = ModTemplateMod.Settings?.general?.language ?? ModLanguage.Auto;
			if (language == ModLanguage.Auto)
			{
				return GameTranslate(key, args);
			}

			EnsureLoaded();
			Dictionary<string, string> dictionary =
				language == ModLanguage.English ? english : chinese;

			if (dictionary.TryGetValue(key, out string text) && !text.NullOrEmpty())
			{
				return Format(text, args);
			}

			// 缺键时回退游戏语言，至少不会显示 raw key。
			return GameTranslate(key, args);
		}

		private static string GameTranslate(string key, object[] args)
		{
			if (args == null || args.Length == 0)
			{
				return key.Translate();
			}

			NamedArgument[] namedArgs = args
				.Select(arg => new NamedArgument(arg, null))
				.ToArray();
			return TranslatorFormattedStringExtensions.Translate(key, namedArgs);
		}

		private static string Format(string text, object[] args)
		{
			if (args == null || args.Length == 0)
			{
				return text;
			}

			try
			{
				return string.Format(CultureInfo.InvariantCulture, text, args);
			}
			catch (FormatException)
			{
				return text;
			}
		}

		private static void EnsureLoaded()
		{
			if (loaded)
			{
				return;
			}

			loaded = true;
			ModContentPack content = ModTemplateMod.Instance?.Content;
			if (content == null)
			{
				return;
			}

			Load(Path.Combine(content.RootDir, "Languages", "English", "Keyed", "ModTemplate.xml"), english);
			Load(Path.Combine(content.RootDir, "Languages", "ChineseSimplified", "Keyed", "ModTemplate.xml"), chinese);
		}

		private static void Load(string path, Dictionary<string, string> target)
		{
			try
			{
				if (!File.Exists(path))
				{
					return;
				}

				XmlDocument document = new XmlDocument();
				document.Load(path);
				XmlNode root = document.DocumentElement;
				if (root == null)
				{
					return;
				}

				foreach (XmlNode node in root.ChildNodes)
				{
					if (node.NodeType == XmlNodeType.Element)
					{
						target[node.Name] = node.InnerText;
					}
				}
			}
			catch (Exception exception)
			{
				Log.Warning("[ModTemplate][Loc] failed to load " + path + ": " + exception.Message);
			}
		}
	}

	/// <summary>设置界面的翻译扩展：<c>"Key".Loc()</c> 按设置语言取值。</summary>
	public static class ModLocExtensions
	{
		public static string Loc(this string key, params object[] args)
		{
			return ModLoc.Translate(key, args);
		}
	}
}
