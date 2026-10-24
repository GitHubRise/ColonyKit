# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 的基本格式，版本号使用 `0.0.x` 预发布序列。

## [0.0.2] - 2026-10-25

Defines the bottom drawer as the default mod opening interaction, adds the vanilla Research demo tab, and folds in the P0 fixes plus smoke/CI.

### Added

- **Default opening interaction**：底部按钮打开贴底全宽 `ModDrawerWindow`，向上滑入、不暂停、不吸收面板外输入；点其他底部菜单自动收起，打开时收起其他主菜单页签（World 例外）。
- `Defs/Research/ModTemplate_ResearchExamples.xml`：原版 Research 新增 `MT_ResearchTab` 页签 + 15 个示例研究，覆盖原版前置、独立根、线性链、线性后分叉、条件分叉、生产研究解锁配方。
- 研究/页签/配方 DefInjected；`smoke-test.ps1`；`.github/workflows/ci.yml`；UI 自动化 `05-demo-drawer` 场景。

### Changed

- 玩家可见文案统一为「研究树 / Research tree」；代码类型和翻译键保留 `TechTree*`，避免破坏 API。
- 研究入口改为原版 Research 页签，不再注册研究树底部按钮；底部「模板演示」成为独立测试项。
- `build.ps1` 默认 `-t:Rebuild`，警告视为失败；新增 `-NoRebuild`；部署改为 `/MIR` 清理陈旧文件。

### Fixed

- `ModLoc` 格式化回退改用 `TranslatorFormattedStringExtensions` + `NamedArgument`，清洁重建不再有 `CS0618`。
- `NotificationService.ThrottledMessage` 在 `Current.Game == null` 时不再访问 `Find.TickManager`。
- `SettingsWidgets.Dropdown` 对 `null` / 空 options 做保护。
- `MainButtonService.ApplyOrder` 反射字段缺失/类型不符时用 `ModLog.Once` 警告。
- `build.ps1` 部署后 `LASTEXITCODE` 被 robocopy 成功码污染；成功路径显式归零。
- 配方 `products` 改为字典语法 `<ComponentIndustrial>1</ComponentIndustrial>`。
- 修复 `MainButtonWorker_TechTree` 陈旧 Def 与 `ComponentIndustrial1` 解析错误。

### Validated

- clean rebuild 0 error / 0 warning；`verify.ps1` 0 问题；`package.ps1` 19 个文件。
- `smoke-test.ps1` PASSED：`demoButton=True, demoResearch=15`，迁移/诊断/备份命中。
- UI 自动化 5 场景截图，含 `05-demo-drawer` 贴底抽屉。
- 中英 Keyed 各 129 键。

## [0.0.1] - 2026-10-25

首个可运行的架构模板版本，目标是「拉下来能编译、进游戏能看到、改功能有章可循」，不是完整玩法 Mod。

### Added

- 核心生命周期：`Mod` 入口、`Bootstrap`、`Def`/`DefOf`、`GameComponent` 持久化、Harmony Postfix 示例。
- 模块系统：`IModTemplateModule` + `ModuleRegistry`，六个内置模块、开关、初始化顺序、异常隔离。
- 设置框架：五页签、控件库、检索、自动滚动、下拉/循环/打勾三模式、设置窗口语言覆盖、迁移/备份/Validate。
- 日志与诊断、右键小人菜单、通知、建造控制、底部菜单、科技树窗口。
- 中英 125 Keyed、build / verify / package / new-mod、UI 自动化截图入口。

### Known limitations

- 模块仍在同一个 DLL，没有独立程序集/热卸载。
- `DefInjected`、原版 Close 按钮不参与 `ModLoc` 设置语言覆盖。
- 检索只覆盖当前页。
- 科技树独立窗口绘制、配方实际制作等需人工复验。
