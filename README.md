# Mod Template

一个**能直接跑起来**的 RimWorld 1.6 Mod 架构模板：把常见的几种开发方式放在同一个工程里，演示「加载 → 配置 → 补丁 → 存档 → 设置」整条链路，本身不改变游戏平衡。

- 演示内容：统计本局殖民者生成次数，按设定间隔写日志。
- 目标版本：1.6.4871（`net472` + `Krafs.Rimworld.Ref`）。
- 依赖：Harmony（创意工坊 `2009463077`）。

## 快速开始

```powershell
# 在 sources\mod-template 下
.\build.ps1              # 编译 → Assemblies\ModTemplate.dll
.\build.ps1 -Clean       # 先清 obj / bin / Assemblies
.\build.ps1 -Deploy      # 编译并部署到 Mods\ModTemplate（需先退出游戏）

.\verify.ps1             # 静态检查：packageId、XML、补丁 xpath、程序集
.\package.ps1            # 校验 + 编译 + 打包 → dist\ModTemplate-<version>.zip
```

进游戏验证：

1. 启用 `Mod Template`（放在 Harmony 之后）；
2. 开发者模式打开，进一局；
3. `Player.log` 里应有 `[ModTemplate] loaded; greeting="Hello from ModTemplate"`；
4. 选项 → Mod 设置 → `Mod 架构模板`：默认五个页签（模块 / 常规 / 日志 / 通知 / 关于）。模块页可关闭可选功能；日志页演示下拉框（统计明细 / 框架日志级别）、滑块（间隔）、输入框（前缀）、禁用态勾选，以及「输出诊断报告」；通知页有测试消息/信件/节流按钮。
5. 关于页 → 「打开科技树」：独立窗口，按前置分层绘制节点和连线，可切研究 tab / 切布局、点击节点设为当前研究。
6. 右键一个殖民者：应看到「记录该殖民者」「在附近建造演示墙」两项（常规页可关）。
7. 底部主菜单：应出现「科技树」按钮（常规页可隐藏）。
8. 生成/招募几个殖民者，开「详细日志」后看 `[ModTemplate] spawn:` 行；也可带 `-modtemplate-selftest` 看服务自检日志。
9. 带 `-modtemplate-ui-test` 启动会依次截取底部栏 / 通知页 / 右键菜单 / 科技树 / 底部按钮，PNG 在 `Config/ModTemplateUiTests/`。

## 生成新 Mod

```powershell
.\new-mod.ps1 -Name MyMod -PackageId local.mymod -DisplayName "My Mod"
```

它会把模板复制到 `sources\MyMod`，替换三类占位符，并编译一次：

| 占位符 | 替换为 |
|---|---|
| `ModTemplate` | 新程序集名 / 命名空间 / 文件名 |
| `local.modtemplate` | 新 `packageId` |
| `Mod Template` | 新显示名 |

## 目录结构

```
mod-template/
├─ About/About.xml               Mod 元数据（packageId / supportedVersions / Harmony 依赖）
├─ Defs/DemoConfig.xml           自定义 Def 的 XML
├─ Defs/Core/MainButton_TechTree.xml  底部「科技树」按钮 Def
├─ Patches/OptionalCompatibility.xml  条件补丁（PatchOperationFindMod）
├─ Languages/
│  ├─ English/{Keyed,DefInjected}
│  └─ ChineseSimplified/{Keyed,DefInjected}
├─ Source/
│  ├─ ModTemplate.csproj
│  ├─ ModTemplateMod.cs          Mod 入口 + 设置窗口
│  ├─ DemoConfigDef.cs           自定义 Def 子类
│  ├─ ModTemplateDefOf.cs        DefOf 强类型引用
│  ├─ DemoGameComponent.cs       每存档状态 + 定时日志 + 调试自检
│  ├─ Bootstrap.cs               [StaticConstructorOnStartup] + PatchAll
│  ├─ Patch_Pawn_SpawnSetup.cs   最小 Harmony Postfix
│  ├─ ModLog.cs                  结构化日志（级别/分类/去重/节流）
│  ├─ Diagnostics.cs             诊断报告（设置/服务/最近日志）
│  ├─ Core/Modules/              模块契约 / 注册表 / 六个内置模块
│  ├─ UI/
│  │  ├─ PawnMenuService.cs      右键小人菜单注册表
│  │  ├─ ModTemplatePawnMenuProvider.cs  原版 Provider 入口
│  │  ├─ MainButtonService.cs    底部按钮显示/顺序
│  │  ├─ MainButtonWorker_TechTree.cs  自定义按钮 Worker
│  │  ├─ Patch_MainButton*.cs    可见性/排序 Patch
│  │  ├─ UiAutomationService.cs  截图式 UI 自动化
│  │  └─ Patch_Root_Update_UiAutomation.cs  每帧驱动
│  ├─ Build/
│  │  ├─ BuildControlService.cs  放置校验 + 规则 + 放蓝图
│  │  └─ Patch_GenConstruct_CanPlaceBlueprintAt.cs
│  ├─ Notifications/NotificationService.cs  消息/信件/节流
│  └─ Settings/
│     ├─ ModTemplateSettings.cs  设置根：Scribe 各页数据 + schemaVersion
│     ├─ SettingsMigration.cs    schema 迁移 / .bak 备份
│     ├─ SettingsPage.cs         页抽象（标题 / 顺序 / 绘制 / 重置）
│     ├─ SettingsController.cs   统一调度：标签栏 + 滚动 + 重置
│     ├─ Widgets/SettingsWidgets.cs  通用控件：下拉 / 滑块 / 输入 / 禁用勾选 / 分区标题
│     ├─ Data/{General,Logging,Notification,Module}Settings.cs
│     └─ Pages/{Modules,General,Logging,Notification,About}SettingsPage.cs
├─ Source/TechTree/
│  ├─ TechNode.cs / TechTree.cs          节点、前置边、两种布局
│  ├─ TechPrerequisites.cs               前置遍历 + 环检测
│  ├─ TechTreeService.cs                 前置条件统一 API + Changed 事件
│  ├─ TechTreeView.cs                    节点 / 连线 / 状态 / 进度绘制
│  └─ TechTreeWindow.cs                  独立窗口（tab / 布局切换）
├─ build.ps1 / verify.ps1 / package.ps1 / new-mod.ps1
├─ docs/ARCHITECTURE.md          设计说明：分层、生命周期、扩展点、坑
└─ Assemblies/                   构建产物（.gitignore 忽略）
```

## 演示了哪些开发方式

| 方式 | 文件 | 适用场景 |
|---|---|---|
| XML Def | `Defs/DemoConfig.xml` + `DemoConfigDef.cs` | 纯数据内容，改数值不用重编 |
| `DefOf` | `ModTemplateDefOf.cs` | 代码里强类型引用 Def，避免字符串 |
| `DefInjected` | `Languages/*/DefInjected/` | 翻译自定义 Def 的 `label` / `description` |
| XML Patch | `Patches/OptionalCompatibility.xml` | 条件兼容、给别的 Mod 打补丁 |
| Harmony | `Patch_Pawn_SpawnSetup.cs` | 改原版行为（本模板只做只读统计） |
| `GameComponent` | `DemoGameComponent.cs` | 每存档状态、随存档持久化 |
| 多页签设置 | `Settings/SettingsController.cs` + `Settings/Pages/*` | 页签调度、滚动、重置、按页持久化 |
| 模块系统 | `Source/Core/Modules/*` + `ModuleSettings` | 契约 / 注册表 / 生命周期 / 每模块开关 |
| 设置与存档迁移 | `Source/Settings/SettingsMigration.cs` | schema 版本 / `.bak` / `Validate` / 存档版本 |
| 通用控件 | `Settings/Widgets/SettingsWidgets.cs` | 下拉框 / 滑块 / 输入框 / 禁用勾选 / 页内分区标题 |
| 科技树 | `Source/TechTree/*` | 前置分层绘制、节点/连线/状态、前置条件统一 API |
| 右键小人菜单 | `Source/UI/PawnMenu*` | Provider 自动发现 + 注册表 + 设置开关 |
| 通知消息 | `Source/Notifications/*` | 消息 / 信件 / 按 key 节流 |
| 结构化日志与诊断 | `Source/ModLog.cs` / `Source/Diagnostics.cs` | 级别/分类/Once/Throttled + 诊断报告 |
| 建筑建造 | `Source/Build/*` | 放置校验、规则、程序化放蓝图 |
| 底部主菜单 | `Source/UI/MainButton*` | 按钮显示/隐藏/排序 + 自定义按钮 |
| UI 自动化绘制 | `Source/UI/UiAutomation*` | 场景数据 → 开窗/等待/截图/点击/关闭 |
| 启动钩子 | `Bootstrap.cs` | Def 就绪后做一次初始化 |

## 复用与清理

- 用 `new-mod.ps1` 生成新工程，**不要**直接改模板本身。
- 新工程里把演示内容删掉即可：`Defs/DemoConfig.xml`、`DemoConfigDef.cs`、`ModTemplateDefOf.cs`、`DemoGameComponent.cs`、`Patch_Pawn_SpawnSetup.cs`，以及 `Bootstrap` 里对应的两行。
- 保留 `ModTemplateMod` / `ModTemplateSettings` / `Settings/*` / `ModLog` 作为骨架。

## 约定

- 目录名、程序集名、命名空间三者一致；程序集名必须全局唯一（否则 `Assembly.LoadFrom` 会直接复用别人的程序集）。
- 所有日志走 `ModLog`，统一 `[ModTemplate]` 前缀，方便在 `Player.log` 里筛。
- 存档字段只增不改名；要改就加 `[Obsolete]` 兼容或迁移。
- XML 与 `.cs` 用 TAB 缩进；XML 一律 UTF-8。
- 注释按 [docs/ARCHITECTURE.md §9](docs/ARCHITECTURE.md#9-注释约定)：文件头写职责/时机/约束，公开成员写 `///`，行内只解释「为什么」。

## 已知状态

- `build.ps1` / `verify.ps1` / `package.ps1` / `new-mod.ps1` 均已在本机跑通（0 错误 0 警告）。
- **实机验证通过（单页设置版本）**（2026-10-25，RimWorld 1.6.4871，`-quicktest` + 独立 `-savedatafolder`，只启用 Core + Harmony + 本 Mod）：
  - `[ModTemplate] loaded; greeting="Hello from ModTemplate", note=""` —— About 解析、程序集加载、Bootstrap、Harmony `PatchAll`、Def XML + DefOf 全部走通；
  - 3 条 `[ModTemplate] spawn: ...` —— Harmony Postfix 生效，且只统计殖民者；
  - `[ModTemplate] tick 301: tracked 3 colonist spawn(s)` —— GameComponent 自动实例化、`GameComponentTick` 与设置里的 300 tick 间隔生效；
  - 全程没有本 Mod 相关的错误或翻译缺失日志。
- 多页签设置重构（`Settings/SettingsController` + `Settings/Pages/*`）：编译 / 校验 / 打包通过；嵌套 settings 的运行时读写与页签 UI 尚未实机复测——测试时 RimWorld 正在运行，DLL 无法覆盖，等游戏退出后按上面的步骤跑一次即可。
- 科技树（`Source/TechTree/*`）：编译 / 校验 / 打包通过；窗口绘制尚未实机验证。游戏退出后可带 `-modtemplate-techtree` 跑 quicktest，进游戏会自动打开窗口。
- 外围控制（右键菜单 / 通知 / 建造 / 底部菜单）2026-10-25 quicktest 自检通过：
  `[ModTemplate] selftest: pawnMenu=2, buildRules=0, notifications=True, mainButton=True, wallPlacement=False`
  —— Provider 被发现、菜单注册表有 2 项、通知设置读入、按钮 Def 加载且可见、建造校验可调用且不崩（`wallPlacement=False` 是地图中心被占用的正常拒绝）。
- UI 自动化（`UiAutomationService`）：编译 / 校验 / 打包通过；`-modtemplate-ui-test` 会截取底部栏 / 通知页 / 右键菜单 / 科技树 / 底部按钮。截图场景尚未跑（测试时游戏运行中），退出后可直接补。
- 结构化日志与诊断（`ModLog` / `Diagnostics`）：编译 / 校验 / 打包通过；日志级别、Once/Throttled、诊断报告待 quicktest 复跑（游戏运行中，DLL 无法覆盖）。运行时可用日志页「输出诊断报告」或 `-modtemplate-selftest`。
- 模块系统 + 设置/存档迁移（`Core/Modules` / `SettingsMigration`）：编译 / 校验 / 打包通过；模块开关、`.bak` 备份、schema 迁移与 `Validate` 待 quicktest 复跑（游戏运行中）。
- 尚未覆盖（截图自动化已就位，待实机跑）：多页签/通知页/控件的实际绘制、右键菜单与底部按钮的实际点击、科技树绘制、诊断按钮、存读档后 `ExposeData` 的持久化、Biotech 可选补丁（本机未装 Biotech，`note` 为空）、DefInjected 译文在 UI 中的显示。
- `Patches/OptionalCompatibility.xml` 用 Biotech 举例，装上 Biotech 后可在设置页看到 `Patch note`。

## 相关文档

- 工作区 [docs/Index.md](../../docs/Index.md)：加载机制、语言支持、开发须知。
- 本工程 [docs/Index.md](docs/Index.md)：功能文档索引（模块 / 生命周期 / 迁移 / Def / 补丁 / 存档 / 设置 / 科技树 / 右键菜单 / 通知 / 日志诊断 / 建造 / 底部菜单 / UI 自动化 / 本地化 / 构建）。
- 本工程 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)：分层与生命周期。
- 本工程 [docs/CAPABILITIES.md](docs/CAPABILITIES.md)：能力清单（✅ 已实现 / 🧩 示例 / 📄 仅文档 / ❌ 未提供）。
