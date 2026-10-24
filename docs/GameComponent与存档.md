# GameComponent 与存档

> 状态：✅ 已实现　适用：Mod Template 0.0.2（RimWorld 1.6.4871）　最后更新：2026-10-25
> 本文件说明每存档组件怎么被实例化、哪些字段写进存档、tick 里该做什么，以及调试用启动参数。

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

`DemoGameComponent` 是「每存档一份」的状态容器：统计殖民者生成次数、按设置间隔写汇总日志，并在带调试参数启动时自动打开研究树窗口或跑一次服务自检。它演示 GameComponent 的自动实例化、`ExposeData` 持久化和 tick 成本控制。

## 描述

- 自动实例化：游戏用 `Activator.CreateInstance(type, game)` 创建所有 `GameComponent` 子类，所以**构造函数必须接收 `Game`**，不需要 XML 注册。
- 持久化：`ExposeData` 里 `Scribe_Values.Look` 保存 `spawnsTracked`、`nextLogTick`；key 是存档契约，改名会丢数据。
- tick：`GameComponentTick` 每 tick 调用，先做最便宜的设置/级别判断，再取 `Find.TickManager.TicksGame`；到点才写日志。
- 日志级别：`LoggingSettings.detail == Off` 直接返回；`Summary/Verbose` 都写汇总；明细由 `verboseSpawnLog` 控制。
- 调试开关：`-modtemplate-techtree` 自动打开 `TechTreeWindow`；`-modtemplate-selftest` 打印服务自检、级别日志与诊断报告；`-modtemplate-ui-test` 运行截图式 UI 自动化。
- 不负责：UI 绘制（设置页/研究树窗口各自负责），也不做重计算。

## 绘制调用逻辑图

```text
新游戏 / 读档
      │
      ▼
Game.FillComponents()
  └─ Activator.CreateInstance(typeof(DemoGameComponent), game)
        │
        ▼
ExposeData()  ← 读档时恢复 spawnsTracked / nextLogTick
        │
        ▼
FinalizeInit()
  ├─ ModLog.ResetRuntimeState() / NotificationService.ResetThrottle()
  ├─ 若命令行含 -modtemplate-techtree：打开 TechTreeWindow
  ├─ 若命令行含 -modtemplate-selftest：打印服务自检、级别日志、诊断报告并发送测试通知
  └─ 若命令行含 -modtemplate-ui-test：启动截图式 UI 自动化
        │
        ▼
每 tick: GameComponentTick()
  ├─ detail == Off → return
  ├─ now < nextLogTick → return
  └─ 到点：写汇总日志，nextLogTick = now + logIntervalTicks

Pawn.SpawnSetup Postfix
  └─ NotifyColonistSpawned(pawn) → spawnsTracked++
```

## 函数清单

| 类型 / 函数 | 签名 | 作用 |
|---|---|---|
| `DemoGameComponent` | `DemoGameComponent(Game game)` | 反射实例化入口（参数不能省） |
| `DemoGameComponent.SpawnsTracked` | `public int SpawnsTracked { get; }` | 只读计数 |
| `DemoGameComponent.ExposeData` | `override void ExposeData()` | 存档读写 |
| `DemoGameComponent.FinalizeInit` | `override void FinalizeInit()` | 调试参数开窗口/自检/UI 自动化 |
| `DemoGameComponent.GameComponentTick` | `override void GameComponentTick()` | 定时汇总日志 |
| `DemoGameComponent.NotifyColonistSpawned` | `public void NotifyColonistSpawned(Pawn)` | 计数 + 明细日志 |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| 自动实例化 | ✅ | quicktest 中 `GameComponentTick` 正常执行 |
| `Scribe` 持久化 | ✅ | `spawnsTracked` / `nextLogTick` 写入存档节点 |
| 定时汇总日志 | ✅ | quicktest 日志 `tick 301: tracked 3 colonist spawn(s)` |
| 明细日志开关 | ✅ | 受 `detail == Verbose && verboseSpawnLog` 控制 |
| 调试自动开研究树 | ✅ | `-modtemplate-techtree` 触发 |
| 服务自检 | ✅ | `-modtemplate-selftest` 打印 `pawnMenu/buildRules/notifications/mainButton` |
| 新游戏清空日志/通知节流 | ✅ | `FinalizeInit` 调用 `ResetRuntimeState` / `ResetThrottle` |
| UI 自动化入口 | ✅ | `-modtemplate-ui-test` 启动 `UiAutomationService` |
| 存读档往返验证 | 🧩 | 代码路径已实现，未做专门存读档回归 |

## 涉及文件

| 路径（相对 `sources/mod-template/`） | 作用 |
|---|---|
| `Source/DemoGameComponent.cs` | 组件本体 |
| `Source/Settings/Data/LoggingSettings.cs` | 日志级别/间隔/前缀 |
| `Source/TechTree/TechTreeWindow.cs` | 调试参数打开的目标窗口 |
| `Source/Patch_Pawn_SpawnSetup.cs` | 计数的来源 |

## 所含功能

- GameComponent 自动实例化（`(Game)` 构造签名）；
- 每存档计数与 `Scribe` 持久化；
- 每 tick 轻量判断与定时日志；
- 日志级别/明细开关；
- 调试启动参数自动打开研究树；
- 只读计数供 UI/调试读取。

## 相关文档

- [Harmony补丁.md](Harmony补丁.md)
- [研究树.md](研究树.md)
- [多页签设置框架.md](多页签设置框架.md)
- [ARCHITECTURE.md](ARCHITECTURE.md) §4.4
- 工作区：[开发Mod须知](../../../docs/开发Mod须知.md)
