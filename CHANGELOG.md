# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 的基本格式，版本号使用 `0.0.x` 预发布序列。

## [0.0.2] - 2026-10-25

0.0.1 的补丁版本：修复发布审查发现的 P0 问题，并把冒烟测试与 CI 纳入流水线。

### Fixed

- `ModLoc` 的格式化回退改用 `TranslatorFormattedStringExtensions` + `NamedArgument`，清洁重建不再有 `CS0618`。
- `NotificationService.ThrottledMessage` 在 `Current.Game == null` 时不再访问 `Find.TickManager`。
- `SettingsWidgets.Dropdown` 对 `null` / 空 options 做保护。
- `MainButtonService.ApplyOrder` 在反射字段缺失或类型不符时用 `ModLog.Once` 留下明确警告，而不是静默失效。

### Added

- `smoke-test.ps1`：verify → build + deploy → quicktest → 断言 Bootstrap/Migration/Selftest/Diag/备份/诊断文件 → 关闭游戏；使用独立 `-savedatafolder`。
- `.github/workflows/ci.yml`：Windows runner 上执行 build / verify / package；游戏冒烟测试留给本机或 self-hosted runner。

### Changed

- `build.ps1` 默认 `-t:Rebuild`，并把编译警告视为失败；日常增量构建可显式加 `-NoRebuild`。
- 没有工作区 `.tools` 时，restore 自动走 PATH 里的 dotnet + 默认 NuGet 缓存，便于 CI。

## [0.0.1] - 2026-10-25

首个可运行的架构模板版本，目标是「拉下来能编译、进游戏能看到、改功能有章可循」，不是完整玩法 Mod。

### Added

- 核心生命周期：`Mod` 入口、`Bootstrap`、`Def`/`DefOf`、`GameComponent` 持久化、Harmony Postfix 示例。
- 模块系统：`IModTemplateModule` + `ModuleRegistry`，内置 `core`、`pawn-menu`、`notifications`、`build-control`、`tech-tree`、`diagnostics` 六个模块，支持开关、初始化顺序、异常隔离和游戏加载重置。
- 设置框架：五页签（模块 / 常规 / 日志 / 通知 / 关于）、通用控件库、当前页检索、按内容高度自动滚动、下拉框 / ◀ ▶ 循环 / ✓ ✗ 打勾三种选项控件、设置窗口中英文切换。
- 设置与存档迁移：`schemaVersion`、启动 `.bak` 备份、迁移链、`Validate`、存档 `saveVersion`。
- 日志与诊断：结构化 `ModLog`（级别/分类/Once/Throttled/最近日志）、`Diagnostics` 报告、设置页诊断按钮。
- 交互能力：右键小人菜单、通知消息/信件/节流、建筑放置规则与蓝图、底部主菜单按钮、科技树窗口。
- 本地化：中英 Keyed 各 125 键、DefInjected、`ModLoc` 设置窗口语言覆盖。
- 工程流水线：`build.ps1` / `verify.ps1` / `package.ps1` / `new-mod.ps1`，以及 `-modtemplate-selftest`、`-modtemplate-settings`、`-modtemplate-techtree`、`-modtemplate-ui-test` 调试参数。

### Changed

- 设置页选项控件从单一列表改为三卡片预览；悬停使用灰色蒙版/边框/左侧强调条，选中与悬停能区分。
- 下拉框按钮统一显示 `▼`；`✓ / ✗` 行增加 8px 垂直间距。
- 设置窗口语言覆盖只影响本 Mod 的设置内容，不修改游戏全局语言。

### Validated

- `build.ps1`：0 error / 0 warning。
- `verify.ps1`：0 问题。
- `package.ps1`：`dist/ModTemplate-0.0.1.zip`，12 个文件，第一层 `About/Assemblies/Defs/Languages/Patches`。
- quicktest（RimWorld 1.6.4871）：`modules=6, schema=2, pawnMenu=2, notifications=True, mainButton=True, wallPlacement=False`。
- 设置窗口简体中文切换、普通配置行/卡片悬停、下拉 `▼` 已通过实机截图确认。

### Known limitations

- 所有模块仍在同一个 `ModTemplate.dll`，没有独立程序集/热卸载；禁用模块只是逻辑门控。
- `DefInjected`、原版 `Dialog_ModSettings` 的 Close 按钮仍跟随游戏语言。
- 设置检索只覆盖当前页，不支持跨页全局搜索。
- 科技树绘制、UI 自动化截图、建筑规则非空路径等仍建议在发布前人工复验。
