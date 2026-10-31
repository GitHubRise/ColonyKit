using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ColonyKit
{
	/// <summary>
	/// Mod 入口。只有「需要设置页」或「需要全局单例」时才继承 <see cref="Mod"/>；
	/// 纯 XML Mod 不需要这个类。
	/// </summary>
	/// <remarks>
	/// 运行时机（见 docs/ARCHITECTURE.md §3）：
	///   程序集加载 → 本类构造（此时 Def 还没解析）→ Def 解析 → DefOf 绑定 → StaticConstructorOnStartup。
	/// 所以构造函数里只能读设置，不能碰 DefDatabase / DefOf；
	/// 任何依赖 Def 的初始化都必须放到 Bootstrap。
	/// </remarks>
	public class ColonyKitMod : Mod
	{
		/// <summary>
		/// 设置窗口的分组调度器。一个 Mod 可以有多条设置菜单（见 <see cref="ColonyKitHygieneSettingsMod"/>
		/// 等薄壳入口）：本类负责主入口和另外几组页面，每组一个控制器，首次打开时才创建。
		/// </summary>
		private readonly Dictionary<int, SettingsController> groupControllers =
			new Dictionary<int, SettingsController>();

		/// <summary>
		/// 设置分组：按模块把 8 个页签拆到不同菜单里，避免一个窗口塞满页签。
		/// 顺序必须和 <see cref="DrawSettingsGroup"/> 的下标一致。
		/// </summary>
		private static readonly string[][] SettingsGroups =
		{
			new[] { "modules", "general-settings", "about" },
			new[] { "hygiene", "insulation" },
			new[] { "robotics" },
			new[] { "storage", "blueprint" }
		};

		/// <summary>
		/// 全局单例。Harmony 补丁和 GameComponent 通过它拿设置。
		/// 在构造函数里赋值，晚于程序集加载、早于任何游戏逻辑。
		/// </summary>
		public static ColonyKitMod Instance { get; private set; }

		/// <summary>
		/// 设置对象。<see cref="Mod.GetSettings{T}"/> 内部有缓存，重复调用不会重复读盘；
		/// 但在 <see cref="Instance"/> 赋值之前访问会得到 null，调用方必须判空。
		/// </summary>
		public static ColonyKitSettings Settings =>
			Instance?.GetSettings<ColonyKitSettings>();

		public ColonyKitMod(ModContentPack content) : base(content)
		{
			Instance = this;

			// 先备份旧设置，再读取；迁移失败时至少还有 .bak 可回退。
			SettingsMigration.BackupSettings(content.FolderName, GetType().Name);

			// 提前触发一次读取：设置文件损坏 / 权限不足之类的错误会暴露在启动阶段，
			// 而不是等玩家第一次打开设置页才炸。
			GetSettings<ColonyKitSettings>();

			// 迁移或校验改过值就立即回写，避免下一局又重复迁移。
			if (SettingsMigration.ConsumeDirty())
			{
				WriteSettings();
			}
		}

		/// <summary>
		/// 写设置前再校验一次，保证手动改坏字段/旧版本升级后落盘的是合法值。
		/// </summary>
		public override void WriteSettings()
		{
			Settings?.Validate();
			base.WriteSettings();
		}

		/// <summary>
		/// 设置页左侧的分类名。返回空字符串会导致本 Mod 根本不出现在设置列表里。
		/// </summary>
		public override string SettingsCategory() => "ColonyKit.SettingsCategory".Loc();

		/// <summary>
		/// 设置页内容。多页签、滚动、重置都委托给 <see cref="SettingsController"/>，
		/// 本类只负责按分组把控制器建起来。
		/// 只改内存，不需要手动保存：关闭窗口时 <c>Dialog_ModSettings.PreClose()</c>
		/// 会调用 <see cref="Mod.WriteSettings"/>。
		/// </summary>
		public override void DoSettingsWindowContents(Rect inRect)
		{
			DrawSettingsGroup(inRect, 0);
		}

		/// <summary>卫生 / 保温分组；由 <see cref="ColonyKitHygieneSettingsMod"/> 调用。</summary>
		public void DrawHygieneSettings(Rect inRect)
		{
			DrawSettingsGroup(inRect, 1);
		}

		/// <summary>机器人分组；由 <see cref="ColonyKitRoboticsSettingsMod"/> 调用。</summary>
		public void DrawRoboticsSettings(Rect inRect)
		{
			DrawSettingsGroup(inRect, 2);
		}

		/// <summary>立体库 / 蓝图分组；由 <see cref="ColonyKitStorageSettingsMod"/> 调用。</summary>
		public void DrawStorageSettings(Rect inRect)
		{
			DrawSettingsGroup(inRect, 3);
		}

		private void DrawSettingsGroup(Rect inRect, int group)
		{
			GetGroupController(group)?.Draw(inRect);
		}

		private SettingsController GetGroupController(int group)
		{
			if (Settings == null || group < 0 || group >= SettingsGroups.Length)
			{
				return null;
			}

			if (!groupControllers.TryGetValue(group, out SettingsController controller) || controller == null)
			{
				controller = new SettingsController(Settings, SettingsGroups[group]);
				groupControllers[group] = controller;
			}

			return controller;
		}

		/// <summary>
		/// 打开设置前预选页面（深链接/自动化测试）。会在所有分组里找这个页面，
		/// 找到哪组就选中哪组——注意调用方要打开对应分组的窗口才会看见效果。
		/// </summary>
		public bool SelectSettingsPage(string pageId)
		{
			for (int group = 0; group < SettingsGroups.Length; group++)
			{
				SettingsController controller = GetGroupController(group);
				if (controller != null && controller.HasPage(pageId))
				{
					return controller.SelectPage(pageId);
				}
			}

			return false;
		}
	}
}
