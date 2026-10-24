using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ColonyKit.Modules.Localization
{
	/// <summary>一套开发者菜单翻译方案：极简/常用/全量或玩家自定义。</summary>
	public class TranslationScheme : IExposable
	{
		public string name = "新方案";
		public bool minimal;
		public List<string> menus = new List<string>();

		public void ExposeData()
		{
			Scribe_Values.Look(ref name, "name", "新方案");
			Scribe_Values.Look(ref minimal, "minimal", false);
			Scribe_Collections.Look(ref menus, "menus", LookMode.Value);
			if (menus == null)
			{
				menus = new List<string>();
			}
		}
	}

	/// <summary>
	/// 开发者菜单汉化设置：总开关、翻译方案、表加载状态。作为 ColonyKitSettings 的嵌套对象持久化。
	/// </summary>
	public class LocalizationSettings : IExposable
	{
		public const int MaxSchemes = 10;

		public bool enabled = true;
		public List<TranslationScheme> schemes = new List<TranslationScheme>();
		public int active;
		public int version;

		public static readonly string[] MenuIds =
		{
			"general", "pawns", "spawning", "incidents", "quests", "map", "world",
			"faction", "ideoligion", "anomaly", "odyssey", "generation", "pathing",
			"lighting", "sound", "translation", "autotests", "mods", "mechanoid",
			"insect", "other", "debug", "settings", "output",
			"actions", "showmoreactions", "moredebugactions", "alienrace", "badhygiene",
			"moelotldebugtools", "mugirl", "rimworldanimations", "rjw", "yuran"
		};

		public static readonly string[] MenuLabels =
		{
			"常规", "小人", "生成", "事件", "任务", "地图", "世界",
			"派系", "意识形态", "异象", "奥德赛", "生成（世界生成）", "寻路",
			"光照", "声音", "翻译", "自动测试", "Mod", "机械族",
			"昆虫", "其他", "调试", "设置", "输出",
			"动作", "显示更多动作", "更多调试动作", "外星种族", "卫生",
			"MoeLotl 调试工具", "牛娘", "动画", "RJW", "尤兰"
		};

		public TranslationScheme Current
		{
			get
			{
				if (schemes == null || schemes.Count == 0)
				{
					schemes = DefaultSchemes();
				}

				active = Mathf.Clamp(active, 0, schemes.Count - 1);
				TranslationScheme scheme = schemes[active];
				if (scheme == null)
				{
					scheme = new TranslationScheme();
					schemes[active] = scheme;
				}
				if (scheme.menus == null)
				{
					scheme.menus = new List<string>();
				}
				return scheme;
			}
		}

		public static TranslationScheme DefaultMinimal()
		{
			return new TranslationScheme { name = "极简", minimal = true };
		}

		public static TranslationScheme DefaultCommon()
		{
			return new TranslationScheme
			{
				name = "常用",
				minimal = true,
				menus = new List<string>
				{
					"general", "pawns", "spawning", "incidents",
					"quests", "ideoligion", "anomaly", "map", "mods"
				}
			};
		}

		public static TranslationScheme DefaultAll()
		{
			return new TranslationScheme
			{
				name = "全量",
				minimal = true,
				menus = MenuIds.ToList()
			};
		}

		public static List<TranslationScheme> DefaultSchemes()
		{
			return new List<TranslationScheme> { DefaultAll(), DefaultCommon(), DefaultMinimal() };
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref enabled, "enabled", true);
			Scribe_Collections.Look(ref schemes, "schemes", LookMode.Deep);
			Scribe_Values.Look(ref active, "active", 0);
			Scribe_Values.Look(ref version, "version", 0);

			if (schemes == null || schemes.Count == 0)
			{
				schemes = DefaultSchemes();
			}

			if (schemes.Count > MaxSchemes)
			{
				schemes.RemoveRange(MaxSchemes, schemes.Count - MaxSchemes);
			}

			// 旧方案缺 "mods" 时补上；与来源 Mod 的默认行为保持一致。
			foreach (TranslationScheme scheme in schemes)
			{
				if (scheme == null)
				{
					continue;
				}
				if (scheme.menus == null)
				{
					scheme.menus = new List<string>();
				}
				if (scheme.name == "常用" && !scheme.menus.Contains("mods"))
				{
					scheme.menus.Add("mods");
				}
				if (scheme.name == "全量")
				{
					foreach (string menu in MenuIds)
					{
						if (!scheme.menus.Contains(menu))
						{
							scheme.menus.Add(menu);
						}
					}
				}
			}

			active = Mathf.Clamp(active, 0, schemes.Count - 1);

			if (version < 2)
			{
				version = 2;
				int allIndex = schemes.FindIndex(scheme => scheme != null && scheme.name == "全量");
				if (allIndex >= 0)
				{
					active = allIndex;
				}
			}
		}

		public void Reset()
		{
			enabled = true;
			schemes = DefaultSchemes();
			active = 0;
			version = 2;
		}

		public bool Validate()
		{
			bool changed = false;

			if (schemes == null || schemes.Count == 0)
			{
				schemes = DefaultSchemes();
				active = 0;
				changed = true;
			}

			if (schemes.Count > MaxSchemes)
			{
				schemes.RemoveRange(MaxSchemes, schemes.Count - MaxSchemes);
				changed = true;
			}

			for (int i = schemes.Count - 1; i >= 0; i--)
			{
				if (schemes[i] == null)
				{
					schemes.RemoveAt(i);
					changed = true;
				}
			}

			if (schemes.Count == 0)
			{
				schemes = DefaultSchemes();
				active = 0;
				changed = true;
			}

			int clamped = Mathf.Clamp(active, 0, schemes.Count - 1);
			if (clamped != active)
			{
				active = clamped;
				changed = true;
			}

			foreach (TranslationScheme scheme in schemes)
			{
				if (scheme.menus == null)
				{
					scheme.menus = new List<string>();
					changed = true;
				}
				if (string.IsNullOrEmpty(scheme.name))
				{
					scheme.name = "未命名";
					changed = true;
				}
			}

			return changed;
		}
	}
}
