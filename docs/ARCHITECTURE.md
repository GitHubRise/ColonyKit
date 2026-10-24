# 架构说明

本文说明 Mod Template 为什么这样分层、每个文件放在生命周期的哪个位置，以及改哪里最容易踩坑。规则与机制依据见工作区 [docs/](../../../docs/Index.md)。

## 1. 设计目标

1. **最小可运行**：不装 HugsLib、不带美术资源，克隆下来就能编译、部署、进游戏。
2. **每种扩展点只出现一次**：Def、Patch、Harmony、GameComponent、Settings 各一个示例，位置固定，删改都有参照。
3. **数据与代码分离**：能放 XML 的放 XML；代码只做 XML 表达不了的事。
4. **流水线完整**：构建、静态校验、打包、改名生成新工程，各一条命令。
5. **不改变玩法**：模板只做只读统计，避免复制后忘记删演示逻辑而影响平衡。

## 2. 分层

```
┌───────────────────────────────────────────────┐
│ 数据层（无代码也能改）                          │
│ About/About.xml                                │
│ Defs/*.xml            → DemoConfigDef          │
│ Patches/*.xml         → 条件补丁               │
│ Languages/*           → Keyed / DefInjected    │
└───────────────────────────────────────────────┘
                     ↓ 读取
┌───────────────────────────────────────────────┐
│ 代码层                                          │
│ Bootstrap.cs          启动 + PatchAll           │
│ ModTemplateMod.cs     Mod 入口 + 设置页         │
│ SettingsController    标签栏 / 滚动 / 重置调度   │
│ SettingsPage(s)       各页 UI（DoContents）      │
│ ModTemplateSettings   Scribe 持久化              │
│ DemoGameComponent.cs  每存档状态 + Tick         │
│ ModTemplateDefOf.cs   Def 强类型引用            │
│ Patch_*.cs            原版行为                  │
│ ModLog.cs             日志前缀                  │
└───────────────────────────────────────────────┘
```

依赖方向是单向的：代码读数据，数据不感知代码。改数值、加翻译、加兼容补丁都不需要重新编译。

## 3. 生命周期对照

游戏的实际加载顺序（`PlayDataLoader.DoPlayLoad` → `LoadedModManager.LoadAllActiveMods`）：

| 阶段 | 本工程落点 | 能做 / 不能做 |
|---|---|---|
| 程序集 `Assembly.LoadFrom` | `ModTemplate.dll` | 静态字段初始化不要碰游戏状态 |
| `Mod` 类实例化（**Def 之前**） | `ModTemplateMod` 构造函数 | **不能**用 `DefDatabase` / `DefOf`；只读设置 |
| Def XML 合并 + Patch | `Defs/` + `Patches/` | Patch 改的是 XML 节点，不是 Def 对象 |
| Def 反序列化 | `DemoConfigDef` | 字段必须是 public |
| `DefOf` 绑定 | `ModTemplateDefOf` | 字段名 = `defName` |
| `[StaticConstructorOnStartup]`（Def 就绪后） | `Bootstrap` | 可安全读 Def、安装 Harmony |
| 新游戏 / 读档 | `DemoGameComponent` 构造 + `ExposeData` | 每存档一份，随存档读写 |
| 每 tick | `GameComponentTick` | 只做轻量判断，别做重活 |
| 设置窗口 | `SettingsController` / `SettingsPage` | 关闭时由 `Dialog_ModSettings` 自动 `WriteSettings()`；页面只画内容 |

一句话记法：**构造函数最早、静态构造函数最晚、Patch 在中间**。

## 4. 扩展点

### 4.1 自定义 Def

- XML 根元素名必须是**类型全名**（`ModTemplate.DemoConfigDef`），因为游戏用 `GenTypes.GetTypeInAnyAssembly` 按名字解析。
- 字段 public、有默认值；XML 里没写的字段保持默认。
- `DefOf` 字段名必须和 `defName` 一致（`Default` ↔ `<defName>Default</defName>`），否则运行时为 null。
- `DefInjected` 的目录名是**类型全名**，文件名随意；键是 `<defName>.<field>`。

### 4.2 XML Patch

- `Patches/*.xml` 根元素 `<Patch>`，子节点 `<Operation Class="...">`。
- `PatchOperationFindMod` 匹配的是 **Mod 显示名**（`mod.Name`），不是 `packageId`；按 `packageId` 判断请用 `MayRequire` 属性或代码。
- `xpath` 作用在**所有 Mod 合并后的 `<Defs>`** 上，所以可以改原版、也可以改自己的 Def。
- 本模板的补丁只给演示 Def 追加 `note`，不碰其他 Mod，删掉不影响加载。

### 4.3 Harmony

- 只在 `Bootstrap` 里 `PatchAll`，id 用 `packageId`，保证唯一。
- 优先 `Postfix` + 只读；必须改返回值时用 `__result`，必须跳过原方法时才用 `Prefix`。
- 补丁里对 `__instance`、`Current.Game`、设置对象全部判空；原版方法可能在读档、生成世界等奇怪时机被调用。
- 改 C# 必须重启游戏，Dev Mode 的 Reload defs 不会重载程序集。

### 4.4 GameComponent

- 构造函数必须接收 `Game`：`public DemoGameComponent(Game game)`。游戏用 `Activator.CreateInstance(type, game)` 自动实例化，不需要 XML。
- 每个存档一份；`ExposeData` 里只增字段、给默认值，不要改名。
- `GameComponentTick` 每 tick 都跑，只做 `if` 判断；重活放 `GameComponentUpdate` 或自己的计时器。

### 4.5 多页签设置（统一调度）

设置默认就是多页签形态，参考 RJW 的「控制器 + 多个设置页」拆分（[RJWSettingsController.cs](../../../sources/rjw/1.6/Source/Settings/RJWSettingsController.cs)），但把页面抽象成了对象，避免 `switch` 越写越长：

| 角色 | 文件 | 职责 |
|---|---|---|
| 数据根 | `Settings/ModTemplateSettings.cs` | 持有各页数据，`Scribe_Deep` 按页嵌套持久化 |
| 页数据 | `Settings/Data/*.cs` | 每页一个 `IExposable`，只管字段和 `Reset` |
| 页抽象 | `Settings/SettingsPage.cs` | `Id` / `TitleKey` / `Order` / `Visible` / `CanReset` / `DoContents` / `Reset` |
| 调度器 | `Settings/SettingsController.cs` | 标签栏、当前页、滚动、重置本页 / 全部 |
| 页面 | `Settings/Pages/*.cs` | 只往 `Listing_Standard` 追加控件 |

**统一调度**的含义：

- 窗口只调 `controller.Draw(inRect)`，不关心有几页、当前是哪页；
- 标签栏按 `Order` 排序绘制，点击切换并播放 `SoundDefOf.Click`；
- 滚动位置和内容高度按 `SettingsPage.Id` 缓存，切页不丢位置；
- 「重置本页」只在 `CanReset` 为 true 时显示；「全部重置」走 `Dialog_MessageBox` 确认；
- 关闭窗口时 `Dialog_ModSettings.PreClose()` 调用 `Mod.WriteSettings()`，控制器不需要保存按钮。

**新增一页（三步）**：

1. `Settings/Data/` 加数据类（`IExposable`：`ExposeData` + `Reset`），并在 `ModTemplateSettings.ExposeData` 里加一行 `Scribe_Deep.Look`；
2. `Settings/Pages/` 加页面类，实现 `Id` / `TitleKey` / `Order` / `DoContents`，需要重置就覆写 `Reset`；
3. 在 `SettingsController` 的页面列表里 `new` 出来；中英文各补 `ModTemplate.Settings.Tab.*` 与页内文案键。

**约定**：

- 页面每次绘制都从 `settings.xxx` 取数据，**不要长期保存对象引用**：`Scribe_Deep` 读档会替换实例，旧引用会失效。
- 所有修改立即生效（Harmony 补丁和 GameComponent 每次读当前字段），落盘统一在关窗时。
- 保存位置：`Config\Mod_<文件夹名>_<Mod 类名>.xml`；改文件夹名或类名 = 玩家丢设置。
- 页签标题用 `TitleKey` 而不是写死字符串，否则中英文要改代码。

#### 通用控件库（`SettingsWidgets`）

页面不要直接摆 `Widgets.*`，统一走这层包装；换样式或加 Tooltip 只改一处：

| 控件 | 方法 | 说明 |
|---|---|---|
| 分区标题 | `Section(listing, title)` | 分隔线 + 小标题，一个页签内可分多段 |
| 勾选框 | `Checkbox(listing, label, ref value, tooltip, enabled)` | `enabled: false` 时灰显不可点，Tooltip 解释原因 |
| 滑块 | `Slider(listing, label, value, min, max, step, format)` | `step` 做吸附，`format` 自定义右侧值文本 |
| 下拉框 | `Dropdown(listing, label, current, options, optionLabel, onChanged)` | 点击弹原生 `FloatMenu`，当前项带 ✓ |
| 输入框 | `TextField(listing, label, value, maxLength, tooltip)` | 超长输入丢弃，返回旧值 |

```csharp
SettingsWidgets.Section(listing, "ModTemplate.Settings.Section.Level".Translate());

SettingsWidgets.Dropdown(listing, label, data.detail, Details,
	d => ("ModTemplate.Settings.LogDetail." + d).Translate(),
	d => data.detail = d);

SettingsWidgets.Checkbox(listing, label, ref data.verboseSpawnLog, tooltip,
	enabled: data.detail == LogDetail.Verbose);

data.logIntervalTicks = (int)SettingsWidgets.Slider(listing, label,
	data.logIntervalTicks, 600f, 60000f, 600f,
	v => "…IntervalValue".Translate((int)v, (v / GenDate.TicksPerDay).ToString("0.0")).ToString());

data.customPrefix = SettingsWidgets.TextField(listing, label, data.customPrefix, 24, tooltip);
```

注意：

- 下拉框用原生 `FloatMenu`，几十个选项没问题；上百项请换带搜索的窗口。
- 输入框每帧返回新值，直接写回字段即可；保存仍由关窗统一完成。
- 禁用态勾选只是 UI 层限制，读取方仍要再判一次业务条件——本模板的 `verboseSpawnLog` 同时检查 `detail == Verbose`。

### 4.6 日志与诊断

所有输出走 `ModLog`，格式 `[前缀][分类] 消息`，级别 `Debug/Info/Warn/Error`（设置页「框架日志级别」）。支持 `Once` 去重、`Throttled` 节流，并保留最近 50 条日志。

排查：`Player.log` 搜前缀；设置页「输出诊断报告」会把版本、设置、服务状态和最近日志写到 `Config/ModTemplate-diagnostics.txt`。新游戏/读档时清空去重/节流状态，避免跨局误压消息。

详见 [结构化日志与诊断](结构化日志与诊断.md)。

### 4.7 多版本

模板目前只支持 1.6，内容直接放在 Mod 根目录：没有 `LoadFolders.xml` 时，游戏会按「没有版本目录 → 用根目录」的规则加载。

要并行支持 1.6 / 1.7：

1. 把版本相关内容移进 `1.6/`、`1.7/`（`Assemblies`、`Defs`、`Patches`、`Languages`、`Textures`）；
2. Mod 根目录放 `LoadFolders.xml`；
3. 改 csproj 的 `OutputPath`，或在 `build.ps1` 里加 `-GameVersion` 参数切换。

```xml
<loadFolders>
	<v1.6>
		<li>/</li>
		<li>1.6</li>
	</v1.6>
	<v1.7>
		<li>/</li>
		<li>1.7</li>
	</v1.7>
</loadFolders>
```

- `<li>/</li>` 会被规范成根目录（`folderName = ""`），用来放共享内容；列表里越靠后的目录优先级越高。
- `<li>` 还支持 `IfModActive` / `IfModActiveAll` / `IfModNotActive` 属性，可以按激活的 Mod 选目录。
- **别忘了**把 `1.7` 加进 `About.xml` 的 `supportedVersions`，否则加载器会报 `ModLoadFolderDefinesUnsupportedGameVersion`。

### 4.8 科技树（TechTree）

科技树不替换原版研究界面，而是在其之上提供「构图 + 布局 + 绘制 + 前置管理」：

| 角色 | 文件 | 职责 |
|---|---|---|
| 节点 | `TechTree/TechNode.cs` | 包 `ResearchProjectDef`，带层 / 行 / 矩形 / 树内前置 |
| 树 | `TechTree/TechTree.cs` | 按 tab 过滤、建边、两种布局、计算画布尺寸 |
| 前置工具 | `TechTree/TechPrerequisites.cs` | 遍历 `prerequisites` + `hiddenPrerequisites`、环检测 |
| 统一接口 | `TechTree/TechTreeService.cs` | 查询 / 添加 / 删除 / 整体替换前置，状态查询，`Changed` 事件 |
| 绘制 | `TechTree/TechTreeView.cs` | 先连线后节点；状态配色、进度条、Tooltip、点击 |
| 窗口 | `TechTree/TechTreeWindow.cs` | tab 切换、布局切换、滚动 |

**数据来源**：`DefDatabase<ResearchProjectDef>` 中 `tab == 当前 ResearchTabDef` 且 `!IsHidden` 的项目；前置同时读取 `prerequisites` 与 `hiddenPrerequisites`。

**两种布局**（`TechTreeLayoutMode`）：

- `PrerequisiteTiers`（默认）：`tier = max(前置 tier) + 1`，同层按原版 `ResearchViewY` 排序，结构最清楚；
- `VanillaCoordinates`：直接用 `ResearchViewX * 190`、`ResearchViewY * 100`，和原版研究页位置一致。

遇到环时记录警告并把该节点按 tier 0 处理，窗口不会崩。

**统一接口**：

```csharp
// 查询（默认包含隐藏前置）
IReadOnlyList<ResearchProjectDef> prerequisites = TechTreeService.GetPrerequisites(project);

// 增删改；会拒绝自引用、重复和成环
TechTreeService.AddPrerequisite(project, prerequisite);
TechTreeService.AddPrerequisite(project, hiddenPrerequisite, hidden: true);
TechTreeService.RemovePrerequisite(project, prerequisite);
TechTreeService.SetPrerequisites(project, new[] { a, b });

// 状态 + 变更通知
TechAvailability state = TechTreeService.GetAvailability(project);
TechTreeService.Changed += () => { /* 重建 UI */ };
```

**绘制**：`TechTreeView.Draw(rect, tree, ref scroll, onClick)`。先画前置连线（前置已完成变绿），再画节点：已完成深绿、可研究土黄、锁定深灰；当前研究项目额外黄框，底部按 `ProgressPercent` 画进度条。点击节点调用 `Find.ResearchManager.SetCurrentProject`。

**入口**：设置 → 关于 → 「打开科技树」；开发时可用 `-modtemplate-techtree` 启动参数自动打开（`DemoGameComponent.FinalizeInit` 里的调试钩子）。

**注意**：

- API 直接改 `ResearchProjectDef.prerequisites`，**原版研究树也会同步变化**，不是只改本窗口；
- 修改不写入存档，只在当前进程生效；需要持久化就在启动时按配置重放；
- 环检测只阻止「新增后成环」，不会修复已有 Def 数据；已有环会在布局时报警告。

### 4.9 外围控制（右键菜单 / 通知 / 建造 / 底部菜单）

四类「原版交互点」都不替换原逻辑，只做注册或追加：

| 能力 | 实现 | 关键点 |
|---|---|---|
| 右键小人菜单 | `PawnMenuService` + `ModTemplatePawnMenuProvider` | 原版自动发现 `FloatMenuOptionProvider`，不需要 Harmony；注册表负责条件/排序/开关 |
| 通知消息 | `NotificationService` | 消息/信件统一出口，按 key 节流，受设置总开关控制 |
| 建筑建造 | `BuildControlService` + Postfix on `CanPlaceBlueprintAt_NewTemp` | 规则叠加到 `AcceptanceReport`；`PlaceBlueprint` 校验后调原版放蓝图 |
| 底部主菜单 | `MainButtonService` + 两个 Patch | `Visible` 叠加可见性；`MainButtonsRoot` 构造后按覆盖值重排 |

详见 [右键菜单控制](右键菜单控制.md)、[通知消息控制](通知消息控制.md)、[建筑建造控制](建筑建造控制.md)、[底部菜单控制](底部菜单控制.md)。

## 5. 存档兼容

- `Scribe_Values.Look(ref x, "name", default)` 的 key 是存档契约：**改名 = 丢数据**。
- 新增字段给默认值；删除字段保留 `[Obsolete]` 或迁移逻辑。
- 不要在 `ExposeData` 里做副作用（生成物体、发消息），读档阶段执行顺序不可控。
- 删掉带 `GameComponent` 的 Mod 再读档，游戏会留下未知节点并警告，但通常能继续；发布后不要频繁改名。

## 6. 流水线

| 脚本 | 检查 / 动作 |
|---|---|
| `build.ps1` | restore（离线包目录）→ build → 统计 0 error / 0 warning → 可选 deploy（robocopy + DLL 哈希校验） |
| `verify.ps1` | packageId 正则、supportedVersions、XML 可解析、`Defs`/`Patch` 根元素、补丁 xpath 能在本地 Defs 命中、`Assemblies/` 只有程序集 |
| `package.ps1` | 先 verify 再 build；只复制运行期目录；断言 `About/About.xml` 在第一层、没有 `Source/obj/bin`；手写 zip entry 名保证分隔符是 `/` |
| `new-mod.ps1` | 复制模板 → 替换 `ModTemplate` / `local.modtemplate` / `Mod Template` → 改目录与文件名 → 首次编译 |

## 7. 常见失败模式

| 现象 | 原因 |
|---|---|
| Mod 列表里能看到，但一个 Def 都没加载 | `About/` 不在第一层（多套了一层文件夹） |
| 红字 `could not load types` / 类型全丢 | 程序集缺依赖，`AssemblyIsUsable` 把整个 DLL 丢了 |
| 两个 Mod 同时启用后其中一个「代码没跑」 | 程序集重名，`Assembly.LoadFrom` 复用已加载的那份 |
| 设置页打不开 / 没有条目 | `SettingsCategory()` 返回空，或 Mod 未启用 |
| 设置页只有标签栏、内容空白 | 页面列表为空、`TitleKey` 缺失，或页面在 `DoContents` 里抛异常（看日志） |
| 切页后设置「回退」到旧值 | 页面长期缓存了数据对象；`Scribe_Deep` 读档会替换实例，改成每次从 `settings.xxx` 取 |
| 没有「重置本页」按钮 | 当前页 `CanReset => false`（关于页就是这样） |
| `ModTemplateDefOf.Default` 为 null | XML 根元素名或 `defName` 与字段名不匹配 |
| 补丁不生效 | `FindMod` 写了 packageId；或 xpath 在合并文档里没命中 |
| 改了 C# 没反应 | 没有重启游戏 |
| 打包后进游戏报缺文件 | 打包时把 `About/` 套进了子目录，或漏了 `Languages/` |

## 8. 与工作区文档的对应

| 主题 | 文档 |
|---|---|
| 加载机制、程序集与 XML 顺序 | [Mod加载机制.md](../../../docs/Mod加载机制.md) |
| 语言选择（C# / Go / Rust…） | [开发语言支持.md](../../../docs/开发语言支持.md) |
| 六条硬性规定、发布前自检 | [开发Mod须知.md](../../../docs/开发Mod须知.md) |

## 9. 注释约定

模板的注释是写给「下一次改代码的人」的，按三层写：

1. **文件头**（每个 `.cs`）：一句话职责 + 运行时机 + 约束。
   例：`ModTemplateMod` 标注「构造在 Def 解析之前，不能碰 DefDatabase」；`DemoGameComponent` 标注「必须保留 `(Game)` 构造签名」。
2. **公开成员**：`/// <summary>` 说明「做什么、什么时候调用」；参数只在有契约时写（例如 `respawningAfterLoad` 为什么影响计数）。
3. **行内 `//`**：只解释**为什么**，不解释语法。
   判断标准是「删掉它，读者会不会踩坑」：
   - 该写：Scribe key 为什么不能改名、读档恢复为什么要跳过、Def 根元素为什么是类型全名。
   - 不写：`i++`、`return null`、`// 设置标题` 这类复述。

其他约定：

- 中文注释 + 英文 API 名（`DefDatabase`、`SpawnSetup`），方便全文搜索。
- 注释和代码不一致按 bug 处理；改行为时同一次提交里改注释。
- 不写没有触发条件的 TODO；要写就写清「什么情况下做、做完删掉」。
- XML：`Defs` 里非自明字段加一行 `<!-- -->`；`Patches` 顶部写清匹配条件与影响范围；翻译文件只在文件头写键格式，不逐条注释。
- 密度经验值：教学型模板 / 框架代码 30%–45%，业务代码 10%–20%。低于这个数通常意味着「为什么」丢了，高于这个数通常是在复述代码。
