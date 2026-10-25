# 1008 淋浴 - 卫生 Alert

> 返回：[1000 淋浴索引](1000-淋浴索引.md) · 总索引：[0000 索引](0000-索引.md)
> 状态：✅ 已实现（设施不可用 + 淋浴排队 Job + 洁具占用 Alert） · 适用：ColonyKit 0.0.2 · 最后更新：2026-10-25

## TOC快速预览

- [简介](#简介)
- [描述](#描述)
- [绘制调用逻辑图](#绘制调用逻辑图)
- [函数清单](#函数清单)
- [函数或功能完成情况](#函数或功能完成情况)
- [涉及文件](#涉及文件)
- [所含功能](#所含功能)
- [如何使用](#如何使用)
- [常见问题](#常见问题)
- [相关文档](#相关文档)

## 简介

右侧 Alert 分两类：**设施不可用**（断水/堵塞/断电）和**卫生洁具长期占用/排队**。淋浴器没空位时 Pawn 会去门口等待，占用者结束后自动接手；浴缸/马桶长期占用也会进入提示。

## 描述

### `Alert_HygieneFixture`（设施不可用）

- 扫描玩家主地图的所有 `Building_AssignableFixture`：淋浴器、浴缸、洗手池、马桶。
- 条件：`!fixture.HasWaterSupply || !fixture.Working().Accepted`。
  - 没接水网/阀门关闭 → `HasWaterSupply=false`；
  - 堵塞、电热水淋浴器断电、水箱余量不足 → `Working()` 失败。
- 简易淋浴器断电仍可洗冷水，所以不会触发；电热水淋浴器断电会触发。
- 有具体建筑时 `AlertReport.CulpritsAre` 允许点击跳转；说明逐条列出建筑与原因。

### `Alert_HygieneFixtureBusy`（长期占用/排队）

- 所有洁具共用基类 `Building_AssignableFixture.occupiedSinceTick`：
  - 淋浴、浴缸、马桶、洗手池在 Job 的 `initAction` 写入开始 tick，finish action 清零；
  - 随存档保存，旧档默认 -1。
- 占用 ≥ **2500 tick（约 1 游戏小时）** 的洁具进入 Alert。
- 排队人数：
  - 膀胱 `<0.15` 且没有可用马桶 → 计入等待；
  - 卫生 `<0.2` 且没有可用淋浴器/浴缸 → 计入等待；
  - 只统计殖民者，排除倒地/死亡/精神崩溃。
- 只有排队、没有具体长期占用建筑时返回 `AlertReport.Active`（可显示文字，但没有可跳转目标）。

### 真正的淋浴排队 Job

- `JobGiver_HaveWash` 找不到空淋浴器、也没有空浴缸时，如果有一台正在使用且供水正常的淋浴器，就派 `CK_WaitForShower`。
- `JobDriver_WaitForShower`：走到 InteractionCell，最多等 2500 tick；每 tick 检查 `Building_Shower.CanUse(pawn)`。
- 可用时把 `CK_TakeShower` 排到 Job 队列最前并结束等待；淋浴 Job 启动时正常预约。
- 没有做严格 FIFO 队列；多个等待者会竞争同一台淋浴器，先完成等待检查的人先接手。

## 绘制调用逻辑图

```text
Alert_HygieneFixture
      └─ 玩家地图所有卫生洁具
            ├─ HasWaterSupply == false
            └─ Working().Accepted == false
                  → 加入 culprits

Alert_HygieneFixtureBusy
      ├─ occupiedSinceTick >= 0 且 ≥2500 tick → culprits
      ├─ 膀胱 <0.15 且无空马桶 → waiting
      └─ 卫生 <0.2 且无空淋浴/浴缸 → waiting

JobGiver_HaveWash
      ├─ 有空淋浴 → CK_TakeShower
      ├─ 无淋浴有空浴缸 → CK_TakeBath
      ├─ 无空位但有占用淋浴 → CK_WaitForShower
      └─ 都没有 → null
```

## 函数清单

| 类型 / 函数 | 作用 |
|---|---|
| `Alert_HygieneFixture` | 断水/堵塞/断电提示 |
| `Alert_HygieneFixtureBusy` | 长期占用/排队提示 |
| `JobDriver_WaitForShower` | 门口等待，空闲后接手淋浴 |
| `Building_AssignableFixture.occupiedSinceTick` | 占用计时（所有洁具） |
| `Building_AssignableFixture.HasWaterSupply` | 洁具统一供水判断 |
| `Building_AssignableFixture.OccupancyCheck` | 可选“房间有人即占用”限制 |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| 淋浴器不可用 Alert | ✅ | 断水/堵塞/断电 |
| 浴缸/洗手池/马桶不可用 | ✅ | 统一扫描 |
| 淋浴等待 Job | ✅ | `CK_WaitForShower` |
| 浴缸/马桶/洗手池长期占用 | ✅ | 基类占用计时 |
| 排队人数提示 | ✅ | 卫生/膀胱两条需求 |
| 严格 FIFO 队列 | ❌ | 多个等待者竞争，非先来先得 |

## 涉及文件

| 路径 | 作用 |
|---|---|
| `Source/Modules/Hygiene/Alert_HygieneFixture.cs` | 设施不可用 |
| `Source/Modules/Hygiene/Alert_HygieneFixtureBusy.cs` | 占用/排队 |
| `Source/Modules/Hygiene/JobDriver_WaitForShower.cs` | 等待 Job |
| `Source/Modules/Hygiene/JobGiver_HaveWash.cs` | 空位/浴缸/等待选择 |
| `Source/Modules/Hygiene/Building_AssignableFixture.cs` | 占用计时与总开关 |
| `Source/Modules/Hygiene/Building_Shower.cs` 等 | 占用写入 |
| `Languages/*/Keyed/ColonyKit.xml` | Alert/占用/功率文本 |

## 所含功能

- 设施不可用提示与跳转。
- 淋浴器长期占用提示。
- 浴缸/马桶/洗手池长期占用提示。
- 无空位等待人数提示。
- 淋浴器真正的等待/接手 Job。

## 如何使用

1. 看右侧 Alert；点击带建筑目标的提示可跳转。
2. 断水/堵塞/断电：检查水网、电力、堵塞 Gizmo。
3. 淋浴排队：等待 Job 会自动在空闲后接手；想减少等待就加建淋浴器/浴缸。
4. 浴缸/马桶占用：增加洁具数量，或在洁具 Gizmo 开启「房间占用限制」避免多人挤同一房间。
5. 等待人数提示只说明没空位，不保证 FIFO。

## 常见问题

| 现象 | 原因 | 处理 |
|---|---|---|
| 简易淋浴器断电但不报警 | 可洗冷水，设计如此 | 需要热水就接电/热水罐 |
| 浴缸/马桶不报警 | 没有水网/不在玩家地图 | 检查 `HasWaterSupply` 与水网 |
| 排队提示不消失 | 没有空位/卫生值仍低 | 加建洁具或降低需求速率 |
| 点击排队提示不跳转 | 只有等待人数、没有具体长期占用建筑 | 这是预期；看需求低的小人 |
| 等待 Job 一直不开始 | 目标淋浴器仍被占用/断水/堵塞 | 看设施 Alert，必要时加建 |

## 相关文档

- [1001-淋浴-卫生需求与洗澡.md](1001-淋浴-卫生需求与洗澡.md)
- [1002-淋浴-淋浴器与热水.md](1002-淋浴-淋浴器与热水.md)
- [1003-淋浴-念头与隐私.md](1003-淋浴-念头与隐私.md)
- [1009-淋浴-膀胱与马桶.md](1009-淋浴-膀胱与马桶.md)
