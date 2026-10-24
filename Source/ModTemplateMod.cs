using UnityEngine;
using Verse;

namespace ModTemplate
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
	public class ModTemplateMod : Mod
	{
		/// <summary>
		/// 设置窗口的调度器。首次打开设置页时才创建，
		/// 确保此时 settings 已从磁盘读入、各页对象可用。
		/// </summary>
		private SettingsController controller;

		/// <summary>
		/// 全局单例。Harmony 补丁和 GameComponent 通过它拿设置。
		/// 在构造函数里赋值，晚于程序集加载、早于任何游戏逻辑。
		/// </summary>
		public static ModTemplateMod Instance { get; private set; }

		/// <summary>
		/// 设置对象。<see cref="Mod.GetSettings{T}"/> 内部有缓存，重复调用不会重复读盘；
		/// 但在 <see cref="Instance"/> 赋值之前访问会得到 null，调用方必须判空。
		/// </summary>
		public static ModTemplateSettings Settings =>
			Instance?.GetSettings<ModTemplateSettings>();

		public ModTemplateMod(ModContentPack content) : base(content)
		{
			Instance = this;

			// 先备份旧设置，再读取；迁移失败时至少还有 .bak 可回退。
			SettingsMigration.BackupSettings(content.FolderName, GetType().Name);

			// 提前触发一次读取：设置文件损坏 / 权限不足之类的错误会暴露在启动阶段，
			// 而不是等玩家第一次打开设置页才炸。
			GetSettings<ModTemplateSettings>();

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
		public override string SettingsCategory() => "ModTemplate.SettingsCategory".Translate();

		/// <summary>
		/// 设置页内容。多页签、滚动、重置都委托给 <see cref="SettingsController"/>，
		/// 本类只负责在第一次打开时把控制器建起来。
		/// 只改内存，不需要手动保存：关闭窗口时 <c>Dialog_ModSettings.PreClose()</c>
		/// 会调用 <see cref="Mod.WriteSettings"/>。
		/// </summary>
		public override void DoSettingsWindowContents(Rect inRect)
		{
			if (controller == null && Settings != null)
			{
				controller = new SettingsController(Settings);
			}

			controller?.Draw(inRect);
		}

		/// <summary>
		/// 打开设置前预选页面（深链接/自动化测试）。会按需创建控制器。
		/// </summary>
		public bool SelectSettingsPage(string pageId)
		{
			if (Settings == null)
			{
				return false;
			}

			if (controller == null)
			{
				controller = new SettingsController(Settings);
			}

			return controller.SelectPage(pageId);
		}
	}
}
