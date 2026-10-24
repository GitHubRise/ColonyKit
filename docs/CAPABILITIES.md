# 能力清单

> 口径：只列**当前代码里真实存在**的能力。
> ✅ = 已实现并可用　🧩 = 只有演示用的最小示例　📄 = 只有文档 / 约定，没有实现　❌ = 未提供

一句话：这是一个覆盖「加载 → 配置 → 补丁 → 存档 → 设置 → 打包」主干的 1.6 Mod 模板，**不是完整的 modding SDK**。内容、玩法、美术要按扩展点自己长。

## 1. 工程与发布

| 能力 | 状态 | 入口 |
|---|---|---|
| `net472` 工程 + `Krafs.Rimworld.Ref 1.6.4871` + `Lib.Harmony 2.4.3` | ✅ | `Source/ModTemplate.csproj` |
| 离线构建（工作区 `.tools`） | ✅ | `build.ps1` |
| 部署到 `Mods\ModTemplate` + DLL 哈希校验 | ✅ | `build.ps1 -Deploy` |
| 静态校验（packageId / XML / 补丁 xpath / 程序集） | ✅ | `verify.ps1` |
| 发布 zip（只含运行期目录、第一层布局校验、`/` 分隔符） | ✅ | `package.ps1` |
| 从模板生成新 Mod（改名 + 替换 packageId + 首次编译） | ✅ | `new-mod.ps1` |
| CI、Steam 自动上传、程序集分析器 / Publicizer | ❌ | 需要自己加 |

## 2. 加载与生命周期

| 能力 | 状态 | 入口 |
|---|---|---|
| `Mod` 子类入口 + 设置分类 | ✅ | `ModTemplateMod` |
| `[StaticConstructorOnStartup]` 初始化 + Harmony `PatchAll` | ✅ | `Bootstrap` |
| 自定义 Def 子类 + `DefOf` 强类型引用 | ✅ | `DemoConfigDef` / `ModTemplateDefOf` |
| `GameComponent`（每存档一份、自动实例化） | ✅ | `DemoGameComponent` |
| `GameComponentTick` 周期逻辑 | ✅ | `DemoGameComponent` |
| HugsLib / ModBase / 更新检查 | ❌ | 本模板刻意不引入 |
| Multiplayer API / Prepatcher | ❌ | 需要时再加引用 |

## 3. 数据与补丁

| 能力 | 状态 | 入口 |
|---|---|---|
| XML Def 数据驱动 | ✅ | `Defs/DemoConfig.xml` |
| `DefInjected` 翻译（中 / 英） | ✅ | `Languages/*/DefInjected/` |
| 自定义 Def 的字段 & 默认值 | ✅ | `DemoConfigDef` |
| 条件 XML 补丁（按 Mod 显示名） | ✅ | `Patches/OptionalCompatibility.xml` |
| `MayRequire`（按 packageId 条件加载） | 📄 | 架构文档有说明，未落地示例 |
| 多版本 `LoadFolders.xml` | 📄 | 架构文档 §4.7，当前单版本 flat |
| ThingDef / RecipeDef / HediffDef / ResearchDef 等真实内容 | ❌ | 复制 `DemoConfigDef` 的模式自己加 |
| `DefModExtension`、自定义 `PatchOperation` | ❌ | 需要自己写 |
| JobDriver / ThinkNode / WorkGiver / Comp / 精神崩溃 | ❌ | 需要自己写 |

## 4. 配置系统（多页签）

| 能力 | 状态 | 入口 |
|---|---|---|
| 默认多页签（常规 / 日志 / 通知 / 关于） | ✅ | `SettingsController` + `Settings/Pages/*` |
| 标签栏排序、选中态、切换音效 | ✅ | `SettingsController.DrawTabs` |
| 每页独立滚动位置 / 内容高度缓存 | ✅ | `SettingsController.DrawPage` |
| 重置本页（`CanReset` 控制显隐） | ✅ | `SettingsController.DrawToolbar` |
| 全部重置 + 二次确认 | ✅ | `Dialog_MessageBox.CreateConfirmation` |
| 按页拆分持久化（嵌套 `Scribe_Deep`） | ✅ | `ModTemplateSettings` + `Data/*` |
| 修改立即生效、关窗统一落盘 | ✅ | 字段直读 + `PreClose` |
| 中英文页签 / 文案 | ✅ | `Languages/*/Keyed/` |
| 通知页（开关 / 节流 / 测试按钮） | ✅ | `NotificationSettingsPage` |
| 通用控件：下拉框 / 滑块 / 输入框 / 禁用勾选 / 分区标题 | ✅ | `Settings/Widgets/SettingsWidgets.cs` |
| 下拉框演示（日志级别枚举） | 🧩 | `LoggingSettings.detail` + `LogDetail` |
| 设置搜索、预设、导入 / 导出、快捷键 | ❌ | 需要自己加 |
| 统一的 `Validate()` 钩子（非法值纠正） | ❌ | 目前只在控件层夹取范围 |

## 5. 运行期逻辑

| 能力 | 状态 | 入口 |
|---|---|---|
| Harmony Postfix（只读、全空值保护） | ✅ | `Patch_Pawn_SpawnSetup` |
| 读档恢复跳过（避免重复计数） | ✅ | `respawningAfterLoad` 判断 |
| 对局状态累加 + 定时日志 | ✅ | `DemoGameComponent` |
| 统一日志前缀（设置页可改） | ✅ | `ModLog` + `LoggingSettings.customPrefix` |
| 结构化日志（级别 / 分类 / Once / Throttled） | ✅ | `ModLog` + `LoggingSettings.logLevel` |
| 最近日志缓冲 + 诊断报告 | ✅ | `ModLog.Recent` + `Diagnostics` |
| 诊断按钮 / 新游戏清空状态 | 🧩 | 日志页按钮 + `FinalizeInit` |
| 右键小人菜单（Provider 自动发现 + 注册表） | ✅ | `PawnMenuService` + `ModTemplatePawnMenuProvider` |
| 通知消息 / 信件 / 按 key 节流 | ✅ | `NotificationService` |
| 建造放置校验 + 规则 + 程序化放蓝图 | ✅ | `BuildControlService` + `Patch_GenConstruct_CanPlaceBlueprintAt` |
| 底部主菜单显示 / 隐藏 / 排序 + 自定义按钮 | ✅ | `MainButtonService` + `MainButtonWorker_TechTree` |
| Prefix / Transpiler 示例 | ❌ | 需要时按同样模式加 |
| 暴露给其他 Mod 的 API / 服务接口 | ✅ | `PawnMenuService` / `NotificationService` / `BuildControlService` / `MainButtonService` |

## 6. 科技树

| 能力 | 状态 | 入口 |
|---|---|---|
| 按 `ResearchTabDef` 建树（过滤隐藏项） | ✅ | `TechTree.Build` |
| 两种布局：前置分层 / 原版坐标 | ✅ | `TechTreeLayoutMode` |
| 节点 / 前置连线 / 状态配色 / 进度条 / Tooltip / 点击 | ✅ | `TechTreeView` |
| 前置查询 / 添加 / 删除 / 整体替换 | ✅ | `TechTreeService` |
| 环检测（拒绝新增成环）、隐藏前置 | ✅ | `TechPrerequisites` + `TechTreeService` |
| 前置变化通知（`Changed` 事件） | ✅ | `TechTreeService` |
| 独立窗口：tab 切换 / 布局切换 / 滚动 | ✅ | `TechTreeWindow` |
| 开发用自动打开参数 | 🧩 | `-modtemplate-techtree` |
| 缩放、搜索、拖拽平移画布 | ❌ | 需要自己加 |
| 自定义研究内容 demo（ResearchProjectDef） | ❌ | 模板不新增研究，只展示原版树 |

## 7. 质量与文档

| 能力 | 状态 | 入口 |
|---|---|---|
| 编译门槛 0 error / 0 warning | ✅ | `build.ps1` |
| 包结构 / 补丁 xpath 静态校验 | ✅ | `verify.ps1` |
| zip 第一层 / 排除 `Source/obj/bin` 校验 | ✅ | `package.ps1` |
| quicktest 实机冒烟（手动流程） | 🧩 | README「进游戏验证」 |
| 截图式 UI 自动化（场景注册 / 开窗 / 截图 / 点击） | 🧩 | `UiAutomationService` |
| 截图像素比对 / 自动断言 | ❌ | 目前靠人看 PNG |
| 自动化 UI 测试 / 日志断言脚本 | ❌ | 目前靠人看 `Player.log` |
| 注释规范（文件头 / `///` / 行内 why） | ✅ | `docs/ARCHITECTURE.md` §9 |
| 架构文档 + 工作区交叉引用 | ✅ | `docs/ARCHITECTURE.md` |

## 8. 适合与不适合

**适合**

- 从零起一个 1.6 Mod，需要设置页、条件补丁、每存档状态；
- 想按「一个扩展点一个示例」的方式学习加载链路；
- 作为团队内部模板批量生成新工程。

**不适合（需要先加模块）**

- 大量美术内容（ThingDef / 贴图 / 音效 / AssetBundle）；
- 复杂 AI（JobDriver / ThinkTree / WorkGiver）；
- 联机同步、性能补丁（Prepatcher）、跨版本多目录发布。

## 9. 推荐加能力的顺序

1. **内容层**：ThingDef 或 HediffDef + RecipeDef，先把「Def → 游戏内可见」跑通；
2. **扩展点**：`DefModExtension` 或自定义 `PatchOperation`，让 XML 能表达更多逻辑；
3. **多版本**：加 `LoadFolders.xml` 与 `1.6/`、`1.7/` 目录；
4. **设置增强**：`Validate()`、搜索、预设导入导出；
5. **联机 / 性能**：Multiplayer API、Prepatcher。
