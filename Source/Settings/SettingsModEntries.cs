using UnityEngine;
using Verse;

namespace ColonyKit
{
	// 薄壳设置入口。
	//
	// 原版 LoadedModManager.CreateModClasses() 会把程序集里**每一个**可实例化的 Mod 子类都建一遍，
	// 而「选项 → Mod 设置」列出的是所有 SettingsCategory() 非空的 Mod。所以同一个 Mod 可以有多条
	// 并列的设置菜单：下面几个类共用 ColonyKitMod 的那一份 ColonyKitSettings，只把窗口画成对应
	// 分组，关闭时把写盘交给主 Mod，设置文件仍然只有一份。

	/// <summary>「卫生与保温」设置菜单：卫生 / 保温两页。</summary>
	public class ColonyKitHygieneSettingsMod : Mod
	{
		public ColonyKitHygieneSettingsMod(ModContentPack content) : base(content)
		{
		}

		public override string SettingsCategory()
		{
			return "ColonyKit.SettingsCategory.Hygiene".Loc();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			ColonyKitMod.Instance?.DrawHygieneSettings(inRect);
		}

		public override void WriteSettings()
		{
			ColonyKitMod.Instance?.WriteSettings();
		}
	}

	/// <summary>「机器人」设置菜单：机器人一页。</summary>
	public class ColonyKitRoboticsSettingsMod : Mod
	{
		public ColonyKitRoboticsSettingsMod(ModContentPack content) : base(content)
		{
		}

		public override string SettingsCategory()
		{
			return "ColonyKit.SettingsCategory.Robotics".Loc();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			ColonyKitMod.Instance?.DrawRoboticsSettings(inRect);
		}

		public override void WriteSettings()
		{
			ColonyKitMod.Instance?.WriteSettings();
		}
	}

	/// <summary>「仓储与建造」设置菜单：立体库 / 蓝图两页。</summary>
	public class ColonyKitStorageSettingsMod : Mod
	{
		public ColonyKitStorageSettingsMod(ModContentPack content) : base(content)
		{
		}

		public override string SettingsCategory()
		{
			return "ColonyKit.SettingsCategory.Storage".Loc();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			ColonyKitMod.Instance?.DrawStorageSettings(inRect);
		}

		public override void WriteSettings()
		{
			ColonyKitMod.Instance?.WriteSettings();
		}
	}
}
