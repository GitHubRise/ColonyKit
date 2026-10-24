# Mod Template

一个**能直接跑起来**的 RimWorld 1.6 Mod 架构模板：把常见的几种开发方式放在同一个工程里，演示「加载 → 配置 → 补丁 → 存档 → 设置」整条链路，本身不改变游戏平衡。

- 演示内容：统计本局殖民者生成次数，按设定间隔写日志。
- 当前版本：**0.0.2**（2026-10-25），记录见 [CHANGELOG.md](CHANGELOG.md) 与 [docs/版本记录.md](docs/版本记录.md)。
- 目标版本：1.6.4871（`net472` + `Krafs.Rimworld.Ref`）。
- 依赖：Harmony（创意工坊 `2009463077`）。

## 框架契约

> **框架只提供契约，Mod 只通过契约接入；升级框架不改 Mod 代码，升级 Mod 不改框架文件。**

- 契约面（公开 API）：
  - 模块：`IModTemplateModule`、`ModTemplateModule`、`ModuleRegistry`；
  - 服务：`PawnMenuService`、`NotificationService`、`BuildControlService`、`MainButtonService`；
  - 设置：`SettingsPage`、`SettingsWidgets`、`SettingsSearch`；
  - 日志/诊断：`ModLog`、`Diagnostics`；
  - 本地化：`Languages/*/Keyed` 的键名。
- 升级框架：不破坏上述公开签名和 Keyed 键；破坏性变更必须在 [CHANGELOG.md](CHANGELOG.md) 按 minor/major 说明迁移方式。
- 升级 Mod：只新增/修改自己的模块、服务、资源与翻译，不修改框架的 `Bootstrap`、`SettingsController`、`About` 和核心 `Defs`。
- **0.0.2 边界**：模块仍编译在同一个 DLL，模块注册与设置页列表仍有核心侧硬编码；独立程序集、页面贡献、补丁归属尚未落地。因此上面的声明是**契约目标与代码审查基线**，不是 0.0.2 已完成的物理隔离保证。

## 快速开始

```powershell
# 在 sources\mod-template 下
.\build.ps1              # 编译 → Assemblies\ModTemplate.dll
.\build.ps1 -Clean       # 先清 obj / bin / Assemblies
.\build.ps1 -Deploy      # 编译并部署到 Mods\ModTemplate（需先退出游戏）

.\verify.ps1             # 静态检查：packageId、XML、补丁 xpath、程序集
.\smoke-test.ps1         # 本机 quicktest 冒烟（需先退出游戏）
.\package.ps1            # 校验 + 编译 + 打包 → dist\ModTemplate-<version>.zip
```

进游戏验证：

1. 启用 `Mod Template`（放在 Harmony 之后）；
2. 开发者模式打开，进一局；
3. `Player.log` 里应有 `[ModTemplate] loaded; greeting="Hello from ModTemplate"`；
4. 选项 → Mod 设置 → `Mod 架构模板`：默认五个页签（模块 / 常规 / 日志 / 通知 / 关于）。顶栏有检索框（过滤当前页控件）；常规页可切换三种选项控件（下拉框 / ◀ ▶ / ✓ ✗），也可把设置窗口切到 English / 简体中文（不改游戏语言）；模块页可关闭可选功能；日志页演示下拉框（统计明细 / 框架日志级别）、滑块（间隔）、输入框（前缀）、禁用态勾选，以及「输出诊断报告」；通知页有测试消息/信件/节流按钮。
5. 原版底部 **Research** → 「示例研究」页签：15 个示例研究覆盖原版前置、独立根、线性、分叉、条件分叉、生产解锁；设置「关于」仍可打开独立研究树窗口。
6. 右键一个殖民者：应看到「记录该殖民者」「在附近建造演示墙」两项（常规页可关）。
7. 底部主菜单：多出一个独立的「模板演示」测试按钮（常规页可隐藏）；点击先收起当前其他主菜单页签（World 例外），再从底栏向上展开全宽抽屉，再次点击/Close/X 关闭，点其他底部菜单会自动收起；不暂停、面板外仍可拖地图；它与 Research/研究树无关。
8. 生成/招募几个殖民者，开「详细日志」后看 `[ModTemplate] spawn:` 行；也可带 `-modtemplate-selftest` 看服务自检日志。
9. 带 `-modtemplate-ui-test` 启动会依次截取底部栏 / 通知页 / 右键菜单 / 研究树窗口 / 模板演示按钮，PNG 在 `Config/ModTemplateUiTests/`。

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
├─ Defs/Core/MainButton_Demo.xml  底部「模板演示」按钮 Def（独立测试项）
├─ Defs/Research/ModTemplate_ResearchExamples.xml  示例研究页签 + 15 个研究 + 生产配方
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
│  ├─ Localization/ModLoc.cs     设置窗口语言覆盖（中/英/自动）
│  ├─ UI/
│  │  ├─ AutoScrollView.cs       自适应滚动视图
│  │  ├─ PawnMenuService.cs      右键小人菜单注册表
│  │  ├─ ModTemplatePawnMenuProvider.cs  原版 Provider 入口
│  │  ├─ MainButtonService.cs    底部按钮显示/顺序
│  │  ├─ MainButtonWorker_Demo.cs  模板演示按钮 Worker
│  │  ├─ DemoDrawerWindow.cs    贴底演示抽屉
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
│     ├─ SettingsSearch.cs       当前页检索状态
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
├─ build.ps1 / verify.ps1 / smoke-test.ps1 / package.ps1 / new-mod.ps1
├─ .github/workflows/ci.yml      CI：build / verify / package
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
| 设置检索与滚动 | `Settings/SettingsSearch.cs` + `UI/AutoScrollView.cs` | 当前页过滤 + 内容超高自动滚动条 |
| 选项控件模式 | `SettingsWidgets` + `GeneralSettings.controlMode` | 下拉框 / ◀ ▶ 循环 / ✓ ✗ 打勾 |
| 设置语言切换 | `Localization/ModLoc.cs` + `general.language` | 仅设置窗口切中/英，不改游戏语言 |
| 研究树 | `Source/TechTree/*` | 前置分层绘制、节点/连线/状态、前置条件统一 API |
| 研究示例 | `Defs/Research/ModTemplate_ResearchExamples.xml` | 原版 Research 新页签 + 6 类最小测试项 |
| 右键小人菜单 | `Source/UI/PawnMenu*` | Provider 自动发现 + 注册表 + 设置开关 |
| 通知消息 | `Source/Notifications/*` | 消息 / 信件 / 按 key 节流 |
| 结构化日志与诊断 | `Source/ModLog.cs` / `Source/Diagnostics.cs` | 级别/分类/Once/Throttled + 诊断报告 |
| 建筑建造 | `Source/Build/*` | 放置校验、规则、程序化放蓝图 |
| 底部主菜单 | `Source/UI/MainButton*` + `DemoDrawerWindow` | 按钮显示/隐藏/排序 + 贴底抽屉 |
| UI 自动化绘制 | `Source/UI/UiAutomation*` | 场景数据 → 开窗/等待/截图/点击/关闭 |
| 启动钩子 | `Bootstrap.cs` | Def 就绪后做一次初始化 |

## 复用与清理

- 用 `new-mod.ps1` 生成新工程，**不要**直接改模板本身。
- 新工程里把演示内容删掉即可：`Defs/DemoConfig.xml`、`DemoConfigDef.cs`、`ModTemplateDefOf.cs`、`DemoGameComponent.cs`、`Patch_Pawn_SpawnSetup.cs`，以及 `Bootstrap` 里对应的两行。
- 保留 `ModTemplateMod` / `ModTemplateSettings` / `Settings/*` / `ModLog` 作为骨架。

## 约定

- 目录名、程序集名、命名空间三者一致；程序集名必须全局唯一（否则 `Assembly.LoadFrom` 会直接复用别人的程序集）。
- 所有日志走 `ModLog`，格式 `[前缀][分类]`，方便在 `Player.log` 里按模块筛。
- 存档字段只增不改名；要改就加 `[Obsolete]` 兼容或迁移。
- XML 与 `.cs` 用 TAB 缩进；XML 一律 UTF-8。
- 注释按 [docs/ARCHITECTURE.md §9](docs/ARCHITECTURE.md#9-注释约定)：文件头写职责/时机/约束，公开成员写 `///`，行内只解释「为什么」。

## 已知状态

**0.0.2 基线**（2026-10-25，RimWorld 1.6.4871，`-quicktest` + 独立 `-savedatafolder`，只启用 Core + Harmony + 本 Mod）：

- `build.ps1` / `verify.ps1` / `package.ps1` / `new-mod.ps1` 均通过（0 错误 0 警告；包 12 个文件）。
- `smoke-test.ps1` PASSED：clean rebuild、迁移/自检/诊断/备份断言全部命中；CI 配置在 `.github/workflows/ci.yml`。
- `[ModTemplate][Migration] settings migrated 0 -> 2` + `.bak` —— 设置 schema 迁移与备份生效。
- `[ModTemplate][Bootstrap] loaded`；`[ModTemplate][Selftest] modules=6, schema=2, pawnMenu=2, buildRules=0, notifications=True, demoButton=True, demoResearch=15, wallPlacement=False`。
- Debug/Info/Warn 三行与 `[ModTemplate][Diag] diagnostics report written` —— 日志级别过滤和诊断报告生效。
- 设置页五页签、三卡片选项控件、下拉 `▼`、普通配置行（统计殖民者生成）悬停蒙版已通过实机截图确认；`05-demo-drawer` 已截图验证贴底抽屉。
- 英文游戏下把设置窗口切到简体中文成功；`DefInjected` 和原版 Close 按钮仍跟随游戏语言。
- 尚未覆盖：设置检索的实际输入/无结果、自动滚动条长页面、研究树独立窗口绘制、示例研究页签/配方实际点击、UI 自动化截图（`-modtemplate-ui-test`）、建筑规则非空路径、旧存档 `saveVersion` 迁移、Biotech 可选补丁（本机未装，`note` 为空）。
- `Patches/OptionalCompatibility.xml` 用 Biotech 举例，装上 Biotech 后可在设置页看到 `Patch note`。

详细验证与限制见 [docs/版本记录.md](docs/版本记录.md)；逐项能力见 [docs/CAPABILITIES.md](docs/CAPABILITIES.md)。

## 相关文档

- 工作区 [docs/Index.md](../../docs/Index.md)：加载机制、语言支持、开发须知。
- 本工程 [docs/Index.md](docs/Index.md)：功能文档索引（模块 / 生命周期 / 迁移 / Def / 补丁 / 存档 / 设置 / 检索滚动 / 选项模式 / 语言 / 研究树 / 研究树示例 / 右键菜单 / 通知 / 日志诊断 / 建造 / 底部菜单 / UI 自动化 / 本地化 / 构建）。
- 本工程 [docs/版本记录.md](docs/版本记录.md)：0.0.2 范围、验证、限制与发版检查清单。
- 本工程 [CHANGELOG.md](CHANGELOG.md)：逐版本变更。
- 本工程 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)：分层与生命周期。
- 本工程 [docs/CAPABILITIES.md](docs/CAPABILITIES.md)：能力清单（✅ 已实现 / 🧩 示例 / 📄 仅文档 / ❌ 未提供）。
