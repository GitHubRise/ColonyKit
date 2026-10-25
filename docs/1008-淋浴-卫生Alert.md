# 1008 淋浴 - 卫生 Alert

> 返回：[1000 淋浴索引](1000-淋浴索引.md) · 总索引：[0000 索引](0000-索引.md)
> 状态：✅ 已实现（设施不可用 + 淋浴占用/排队） · 适用：ColonyKit 0.0.2 · 最后更新：2026-10-25

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

右侧 Alert 分两类：**设施不可用**（断水/堵塞/断电）和**淋浴长期占用/排队**。两类都由 `Alert` 叶子子类自动发现，不需要 AlertDef XML。

## 描述

### `Alert_HygieneFixture`（设施不可用）

- 扫描玩家主地图的所有 `Building_AssignableFixture`：淋浴器、浴缸、洗手池、马桶。
- 条件：`!fixture.HasWaterSupply || !fixture.Working().Accepted`。
  - 没接水网/阀门关闭 → `HasWaterSupply=false`；
  - 堵塞、电热水淋浴器断电、水箱余量不足 → `Working()` 失败。
- 简易淋浴器断电仍可洗冷水，所以不会触发；电热水淋浴器断电会触发。
- 有具体建筑时 `AlertReport.CulpritsAre` 允许点击跳转；说明逐条列出建筑与原因。

### `Alert_HygieneShowerBusy`（长期占用/排队）

- `Building_Shower.occupiedSinceTick` 在洗澡 `initAction` 记录、finish action 清零，并随存档保存。
- 占用 ≥ **2500 tick（约 1 游戏小时）** 触发长期占用提示。
- 排队：对每个卫生值 `< 0.2` 的殖民者，检查是否有未禁用、可到达、可预约的淋浴器；没有再看浴缸；都没有则 `waitingCount++`。
- 只有排队、没有具体长期占用建筑时返回 `AlertReport.Active`（可显示文字，但没有可跳转目标）。
- 这是**提示**，不是真正排队系统：`JobGiver_HaveWash` 找不到空位仍然返回 null/浴缸。

## 绘制调用逻辑图

```text
Alert_HygieneFixture
      └─ 玩家地图所有卫生洁具
            ├─ HasWaterSupply == false
            └─ Working().Accepted == false
                  → 加入 culprits

Alert_HygieneShowerBusy
      ├─ occupiedSinceTick >= 0 且 ≥2500 tick → culprits
      └─ 殖民者卫生 <0.2 且无可用淋浴/浴缸 → waitingCount
```

## 函数清单

| 类型 / 函数 | 作用 |
|---|---|
| `Alert_HygieneFixture` | 断水/堵塞/断电提示 |
| `Alert_HygieneShowerBusy` | 长期占用/排队提示 |
| `Building_AssignableFixture.HasWaterSupply` | 洁具统一供水判断 |
| `Building_AssignableFixture.Working` | 堵塞/电力/水量判断 |
| `Building_Shower.occupiedSinceTick` | 占用计时 |
| `JobDriver_TakeShower` | 记录/清理占用 |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| 淋浴器不可用 Alert | ✅ | 断水/堵塞/断电 |
| 浴缸/洗手池/马桶不可用 | ✅ | 统一扫描 |
| 长期占用 | ✅ | ≥2500 tick |
| 排队人数提示 | ✅ | 卫生 <0.2 + 无空位 |
| 实际排队 Job | ❌ | 只提示，不排队 |
| 占用提示的长时间实机验证 | ⚠️ | 逻辑/quicktest 通过，未做长时游玩 |

## 涉及文件

| 路径 | 作用 |
|---|---|
| `Source/Modules/Hygiene/Alert_HygieneFixture.cs` | 设施不可用 |
| `Source/Modules/Hygiene/Alert_HygieneShowerBusy.cs` | 占用/排队 |
| `Source/Modules/Hygiene/Building_AssignableFixture.cs` | `HasWaterSupply` |
| `Source/Modules/Hygiene/Building_Shower.cs` | 占用计时 |
| `Source/Modules/Hygiene/JobDriver_TakeShower.cs` | 占用写入/清理 |
| `Languages/*/Keyed/ColonyKit.xml` | Alert 文本 |

## 所含功能

- 设施不可用提示与跳转。
- 淋浴长期占用提示。
- 无空位等待人数提示。
- 自动发现，无额外 XML。

## 如何使用

1. 看右侧 Alert；点击带建筑目标的提示可跳转。
2. 断水/堵塞/断电：检查水网、电力、堵塞 Gizmo。
3. 长期占用/排队：加建淋浴器/浴缸，或检查是否有人卡在淋浴 Job。
4. 排队提示只说明有人需要洗澡且没空位，不会自动排 Job。

## 常见问题

| 现象 | 原因 | 处理 |
|---|---|---|
| 简易淋浴器断电但不报警 | 可洗冷水，设计如此 | 需要热水就接电/热水罐 |
| 浴缸/马桶不报警 | 没有水网/不在玩家地图 | 检查 `HasWaterSupply` 与水网 |
| 排队提示不消失 | 没有空位/卫生值仍低 | 加建洁具或降低需求速率 |
| 点击排队提示不跳转 | 只有等待人数、没有具体长期占用建筑 | 这是预期；看卫生低的小人 |

## 相关文档

- [1001-淋浴-卫生需求与洗澡.md](1001-淋浴-卫生需求与洗澡.md)
- [1002-淋浴-淋浴器与热水.md](1002-淋浴-淋浴器与热水.md)
- [1003-淋浴-念头与隐私.md](1003-淋浴-念头与隐私.md)
