# 架构说明（0.0.2）

> 状态：0.0.2 框架已生成（M0 完成） · 适用：ColonyKit 0.0.2 / RimWorld 1.6.4871 · 最后更新：2026-10-25
>
> 本文说明 ColonyKit 当前实际骨架、模块生命周期、与模板的关系，以及四个参考模块的落点。设计细节见 [`框架内核.md`](框架内核.md)、[`设置系统.md`](设置系统.md)、[`模块系统`](../../mod-template/docs/模块系统.md)（模板原文）。

## 1. 来源与约束

- 本工程由 [`sources/mod-template`](../../mod-template/) **0.0.1 复制**生成，后同步 **0.0.2** 的框架修复；**没有修改模板本身**。
- 复制时保留：`Source/Core/Modules`、`Source/Settings`、`Source/UI`、`Source/TechTree`、`Source/Build`、`Source/Notifications`、`Source/Localization`、`build/verify/package.ps1`。
- 复制后删除：`DemoConfig`、`ColonyKitDefOf`、`Patch_Pawn_SpawnSetup`、`Patches/OptionalCompatibility.xml`；`DemoGameComponent` 改为 `ColonyKitGameComponent`。
- 新增：四个参考模块占位 `Source/Modules/ReferenceModules.cs`、`docs/参考源码映射.md`、本文件。

## 2. 实际分层

```
About/            Mod 元数据（name=殖民地套件, packageId=local.colonykit, modVersion=0.0.2）
Defs/             目前只有 Core/MainButton_TechTree.xml（模板科技树按钮）
Languages/        English / ChineseSimplified：Keyed + DefInjected
Source/
├─ ColonyKitMod.cs             Mod 入口 + 设置窗口
├─ Bootstrap.cs                [StaticConstructorOnStartup] + PatchAll + 注册模块
├─ ColonyKitGameComponent.cs   新游戏/读档 → ModuleRegistry.NotifyGameLoaded
├─ Core/Modules/               IColonyKitModule / ModuleRegistry / 模板 6 模块
├─ Modules/ReferenceModules.cs hygiene / storage / localization / rjw 四个占位
├─ Settings/                   多页签设置、迁移、控件库
├─ Localization/ModLoc.cs      设置窗口语言覆盖
├─ UI/                         右键菜单、底部按钮、UI 自动化（模板能力）
├─ Build/                      建造控制（模板能力）
├─ Notifications/              通知节流（模板能力）
└─ TechTree/                   科技树窗口（模板能力）
```

> 目标形态（`Source/Core/Water`、`Modules/Hygiene` 等）见 [README §3.6](../README.md#36-目标目录)；当前是模板骨架，功能落地时再迁移。

## 3. 生命周期

```text
程序集加载
  └─ ColonyKitMod 构造（只读设置，不碰 DefDatabase）
Def XML 合并 + Patch
Def 反序列化 / DefOf 绑定
  └─ Bootstrap 静态构造
        ├─ Harmony.PatchAll("local.colonykit")
        ├─ ModuleRegistry.DiscoverFromAssembly(...)：自动发现 10 个模块
        └─ ModuleRegistry.InitAll()
              └─ each enabled module.OnDefsLoaded()（try/catch 隔离）

新游戏 / 读档
  └─ ColonyKitGameComponent.FinalizeInit()
        └─ ModuleRegistry.NotifyGameLoaded(Game)
              ├─ enabled module.OnGameLoaded(game)
              └─ 所有 module.ResetRuntimeState()

设置窗口
  └─ ColonyKitMod.DoSettingsWindowContents → SettingsController（页签/滚动/重置）
```

## 4. 模块契约与软停用

- 契约：`IColonyKitModule`（`Id` / `DisplayNameKey` / `DescriptionKey` / `InitOrder` / `Locked` / `EnabledByDefault` / `OnDefsLoaded` / `OnGameLoaded` / `ResetRuntimeState`）。
- 注册表：`ModuleRegistry.Register` 去重 + 按 `InitOrder` 排序；`IsEnabled` 读 `ModuleSettings.disabled`；锁定模块始终启用。
- **模块自动发现**：`Bootstrap` 只调 `ModuleRegistry.DiscoverFromAssembly(typeof(Bootstrap).Assembly)`；新增模块只要实现 `IColonyKitModule` + 无参构造，不改 `Bootstrap`。
- **设置页自动发现**：`SettingsController` 扫描程序集里的 `SettingsPage` 子类，按 `Order` 排序、按 `Id` 去重；新增页面不改 `SettingsController`。
- **模块设置自动挂载**：模块设置实现 `IColonyKitModuleSettings`，通过 `ColonyKitSettings.GetModuleSettings<T>()` 取/建；持久化在 `moduleSettings` 列表里，不改 `ColonyKitSettings` 的字段列表。
- 软停用：模块关闭后不调用 `OnDefsLoaded` / `OnGameLoaded`，但仍调用 `ResetRuntimeState`；**Def 仍会加载**，不会从存档/地图消失。详见 README §3.4。
- 设置：多页签现为 **模块 / 立体库 / 汉化 / 常规设置 / 关于** 5 页；「常规设置」含 Mod 设置语言（默认简中）与日志/诊断；旧模板「常规 / 日志 / 通知」测试页已移除；模块细分设置放 `IColonyKitModuleSettings`，不再往设置根加字段。

## 5. 0.0.2 的 10 个模块

| Id | 来源 | 0.0.2 行为 |
|---|---|---|
| `core` | 模板 | 锁定；生命周期基准 |
| `pawn-menu` | 模板 | 右键菜单服务（模板能力） |
| `notifications` | 模板 | 通知/信件/节流（模板能力） |
| `build-control` | 模板 | 建造规则/程序化蓝图（模板能力） |
| `tech-tree` | 模板 | 科技树窗口 + 底部按钮（模板能力） |
| `diagnostics` | 模板 | 日志缓冲/诊断报告；锁定 |
| `hygiene` | 参考 DBH | 占位：`OnDefsLoaded` 日志 + DBH 检测 |
| `storage` | 参考 StackXXL | 占位：`OnDefsLoaded` 日志 + StackXXL 检测 |
| `localization` | 参考 DevMenuChinese | **已移植**：加载 `Translations/DevMenuLabels.xml`，Postfix `DebugActionNode.LabelNow`，Debug 窗口 tooltip，翻译方案设置；检测原 Mod 冲突 |
| `rjw` | 参考 RJW | 占位：`OnDefsLoaded` 日志 + RJW 检测；只做兼容层 |

## 6. 模块落地时的目录约定

- 功能代码放 `Source/Modules/<Module>/`；Defs 放 `Defs/<Module>/`；补丁放 `Patches/<Module>/` 或 `Source/Compat/`。
- 命名空间统一 `ColonyKit.Modules.<Module>`；`defName` 统一 `CK_` 前缀。
- 来源 Mod 作为可选依赖时用 `ModsConfig.IsActive` 守卫；作为复制时不要写硬依赖。
- 移植顺序：`localization` → `storage` → `hygiene` → `rjw`（见 [`参考源码映射.md`](参考源码映射.md)）。

## 7. 构建与校验

```powershell
cd sources\ColonyKit
.\build.ps1 -Clean   # 0 error / 0 warning
.\verify.ps1         # 0 问题 / 0 警告
.\package.ps1        # dist\ColonyKit-0.0.2.zip
```

当前结果：构建 0/0、verify 0/0、zip 9 文件（含 `Translations`）；`-Deploy` + `-quicktest` 通过（`modules=10`，翻译表 882/882，自检 tooltip/品质返回中文）；未人工点 Debug 菜单/设置页。

## 8. 相关文档

- [README](../README.md)
- [0000-索引.md](0000-索引.md)
- [参考源码映射.md](参考源码映射.md)
- [框架内核.md](框架内核.md) · [设置系统.md](设置系统.md) · [构建与发布.md](构建与发布.md)
- 模板文档：[模块系统](../../mod-template/docs/模块系统.md) · [ARCHITECTURE](../../mod-template/docs/ARCHITECTURE.md)
