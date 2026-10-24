# UI 自动化绘制

> 状态：🧩 代码完成，实机截图待跑　适用：Mod Template 0.0.2（RimWorld 1.6.4871）　最后更新：2026-10-25
> 本文件说明截图式 UI 自动化：框架负责开窗/等待/截图/点击/关闭，使用者只提供场景数据。

## TOC快速预览

- [简介](#简介)
- [描述](#描述)
- [绘制调用逻辑图](#绘制调用逻辑图)
- [函数清单](#函数清单)
- [函数或功能完成情况](#函数或功能完成情况)
- [涉及文件](#涉及文件)
- [所含功能](#所含功能)
- [相关文档](#相关文档)

## 简介

`UiAutomationService` 把「打开界面 → 等几帧绘制 → `ScreenCapture` 截图 → 触发一次动作 → 关闭」做成通用循环。右键菜单、设置页、研究树、底部主菜单都能用同一条流水线验证绘制，不需要人工点。

带 `-modtemplate-ui-test` 启动时运行内置的 5 个场景，截图写到 `Config/ModTemplateUiTests/`。

## 描述

- 场景 = 数据：`new UiScenario(id, createWindow, onCaptured)`。
  - `createWindow` 返回要打开的 `Window`；返回 null 表示只截当前界面（例如底部主菜单）。
  - `onCaptured` 在截图后执行，用来模拟点击/触发；可为 null。
- 内置场景：
  1. `01-bottom-bar`：不建窗口，截完整游戏界面（验证底部按钮条）；
  2. `02-settings-notifications`：打开 `Dialog_ModSettings` 并预选「通知」页；
  3. `03-pawn-menu`：用 `PawnMenuService.GetOptions` 构造 `FloatMenu`，截图后执行第一项 action；
  4. `04-tech-tree`：打开 `TechTreeWindow`；
  5. `05-bottom-button`：截当前界面后调用「模板演示」底部按钮的 `InterfaceTryActivate()`，模拟点击。
- 调度：每帧由 `Root.Update` 的 Postfix 驱动（设置窗口 `forcePause` 会停 `GameComponentUpdate`，所以不能放那里）。
- 等待：打开后等 20 帧再截图，截图后等 20 帧再关闭，避免截到半帧或文件未落盘。
- 输出：`Config/ModTemplateUiTests/<id>.png`；日志打印每个路径和最终 `ui-test: done, captured=N, skipped=N`。
- 扩展：`UiAutomationService.Register(new UiScenario("my-window", () => new MyWindow()))`；不需要改调度代码。

## 绘制调用逻辑图

```text
-modtemplate-ui-test
      │
      ▼
DemoGameComponent.FinalizeInit()
  └─ UiAutomationService.RunAll()
        ├─ 创建 Config/ModTemplateUiTests
        └─ phase = Opening

Root.Update() Postfix（每帧）
  └─ UiAutomationService.Update()
        ├─ Opening：createWindow() → Find.WindowStack.Add
        ├─ 等 20 帧
        ├─ Capturing：ScreenCapture.CaptureScreenshot(<id>.png)
        │              → onCaptured()（可选点击）
        ├─ 等 20 帧
        ├─ Closing：window.Close(false) → 下一个
        └─ 全部完成：ui-test: done, captured=N
```

## 函数清单

| 类型 / 函数 | 签名 | 作用 |
|---|---|---|
| `UiScenario` | `UiScenario(string id, Func<Window> createWindow = null, Action onCaptured = null)` | 一个场景的数据 |
| `UiAutomationService.Register` | `static void Register(UiScenario scenario)` | 注册场景 |
| `UiAutomationService.RunAll` | `static void RunAll()` | 启动队列 |
| `UiAutomationService.Update` | `static void Update()` | 每帧推进状态机 |
| `Patch_Root_Update_UiAutomation` | `[HarmonyPostfix] Root.Update` | 每帧驱动 |
| `ModTemplateMod.SelectSettingsPage` | `bool SelectSettingsPage(string pageId)` | 深链接到设置页 |
| `SettingsController.SelectPage` | `bool SelectPage(string pageId)` | 按 Id 选中页 |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| 场景注册/调度/截图/关闭 | ✅ | 编译通过 |
| 5 个内置场景 | ✅ | 底部条/通知页/右键菜单/研究树/底部按钮 |
| 右键菜单 action 触发 | ✅ | 截图后执行第一项 |
| 底部按钮点击 | ✅ | `InterfaceTryActivate()` |
| 设置页深链接 | ✅ | `SelectSettingsPage("notifications")` |
| 实机截图 | 🧩 | 测试时游戏正在运行，DLL 无法覆盖；退出后带参数跑一次即可 |
| 截图自动比对/像素断言 | 📄 | 未提供；当前是人工看 PNG |

## 涉及文件

| 路径（相对 `sources/mod-template/`） | 作用 |
|---|---|
| `Source/UI/UiAutomationService.cs` | 场景/状态机/截图 |
| `Source/UI/Patch_Root_Update_UiAutomation.cs` | 每帧驱动 |
| `Source/ModTemplateMod.cs` | `SelectSettingsPage` |
| `Source/Settings/SettingsController.cs` | `SelectPage` |
| `Source/DemoGameComponent.cs` | `-modtemplate-ui-test` 入口 |
| `Config/ModTemplateUiTests/` | 运行时截图输出（不提交） |

## 所含功能

- 声明式 UI 场景（只传窗口工厂/动作）；
- 打开、等待、截图、触发、关闭的完整循环；
- 设置页深链接；
- 右键菜单/研究树/底部按钮/通知页的场景覆盖；
- 独立于 `forcePause` 的帧驱动；
- 日志化截图路径与结果。

## 相关文档

- [右键菜单控制.md](右键菜单控制.md)
- [底部菜单控制.md](底部菜单控制.md)
- [通知消息控制.md](通知消息控制.md)
- [研究树.md](研究树.md)
- [GameComponent与存档.md](GameComponent与存档.md)
