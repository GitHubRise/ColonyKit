# Harmony 补丁

> 状态：✅ 已实现　适用：Mod Template 0.0.2（RimWorld 1.6.4871）　最后更新：2026-10-25
> 本文件说明模板里的最小 Postfix：打在哪、怎么注入、为什么要跳过读档、怎么把结果交给 GameComponent。

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

模板只挂一个 Harmony Postfix：`Pawn.SpawnSetup` 之后统计殖民者生成。它演示三件事：**怎么选稳定目标、怎么只做只读追加、怎么把设置和状态判断放在补丁里**。

## 描述

- 目标：`Verse.Pawn.SpawnSetup(Map, bool)`，小人进入地图的统一入口，签名多年稳定。
- 补丁类型：`[HarmonyPostfix]`，原方法照常执行，只在结束后追加统计；不用 Prefix/Transpiler。
- 参数注入：
  - `Pawn __instance`：Harmony 按名字注入；
  - `bool respawningAfterLoad`：读档恢复为 true，此时计数已在存档里，**直接跳过**，避免读一次档翻倍。
- 守卫顺序：读档 → 设置/软开关 → 空值/死亡/是否殖民者 → `Current.Game`/GameComponent 判空。
- 结果交给 `DemoGameComponent.NotifyColonistSpawned`，补丁本身不写存档、不碰 UI。
- `PatchAll` 只在 `Bootstrap` 里执行一次，Harmony Id = `local.modtemplate`。

## 绘制调用逻辑图

```text
Pawn.SpawnSetup(map, respawningAfterLoad)
        │ 原方法执行
        ▼
Patch_Pawn_SpawnSetup.Postfix(__instance, respawningAfterLoad)
        ├─ respawningAfterLoad == true → return（读档不重复计数）
        ├─ Settings?.general.trackSpawns != true → return
        ├─ !__instance.IsColonist / Dead → return
        └─ Current.Game?.GetComponent<DemoGameComponent>()?.NotifyColonistSpawned(__instance)
                ├─ spawnsTracked++
                └─ 若 detail == Verbose 且 verboseSpawnLog：写 spawn 日志
```

## 函数清单

| 类型 / 函数 | 签名 | 作用 |
|---|---|---|
| `Patch_Pawn_SpawnSetup` | `[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]` | 补丁类 |
| `Patch_Pawn_SpawnSetup.Postfix` | `static void Postfix(Pawn __instance, bool respawningAfterLoad)` | 只读追加统计 |
| `DemoGameComponent.NotifyColonistSpawned` | `public void NotifyColonistSpawned(Pawn pawn)` | 计数与明细日志 |
| `ModTemplateMod.Settings` | `static ModTemplateSettings Settings { get; }` | 读设置 |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| Postfix 安装 | ✅ | `Bootstrap` 里 `PatchAll` |
| 读档跳过 | ✅ | `respawningAfterLoad` 判断 |
| 只统计殖民者 | ✅ | `IsColonist` + `Dead` 判断 |
| 设置开关 | ✅ | `settings.general.trackSpawns` |
| 实机验证 | ✅ | quicktest 日志出现 3 条 `[ModTemplate] spawn:` |
| Prefix/Transpiler 示例 | 📄 | 未提供；需要时按同样判空模式加 |

## 涉及文件

| 路径（相对 `sources/mod-template/`） | 作用 |
|---|---|
| `Source/Patch_Pawn_SpawnSetup.cs` | 补丁本体 |
| `Source/Bootstrap.cs` | `PatchAll` |
| `Source/DemoGameComponent.cs` | 接收计数的组件 |
| `Source/Settings/Data/GeneralSettings.cs` | `trackSpawns` 开关 |
| `Source/Settings/Data/LoggingSettings.cs` | 明细日志开关 |

## 所含功能

- 稳定目标选择与 `nameof` 绑定；
- Postfix 只读追加；
- `__instance` / 原参数注入；
- 读档恢复跳过；
- 设置与状态守卫；
- 把结果交给 GameComponent，而不是在补丁里做业务。

## 相关文档

- [生命周期与入口.md](生命周期与入口.md)
- [GameComponent与存档.md](GameComponent与存档.md)
- [ARCHITECTURE.md](ARCHITECTURE.md) §4.3
- 工作区：[开发Mod须知](../../../docs/开发Mod须知.md)
