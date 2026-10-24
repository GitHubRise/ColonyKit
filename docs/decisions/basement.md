# 地下室：口袋地图 vs 同图逻辑楼层 vs 只做兼容

- 状态：提议（未排期）
- 日期：2026-10-25
- 关联任务：7000（候选模块，尚未占号）；建议排在 M2/M3 之后评估
- 影响范围：模块 / 存档 / 兼容 / 性能

## 背景

ColonyKit 现有六个模块都建立在**同一张地图的平面空间**上：淋浴、立体库、农业、蓝图、机器人都没有垂直方向的概念。如果引入「地下室」，先要区分玩家真正想要的是哪一件事：

1. **更多可用面积**：主地图被山、水、地图边缘卡住，想往下挖。
2. **恒温/无光环境**：地下农业、酒窖、仓库，不受寒潮、热浪、日晒影响。
3. **隐蔽/防突袭**：空投、炮击、突袭进不来，或至少多一层缓冲。
4. **垂直基地的观感**：楼梯、电梯、上下层同时运作，和《矮人要塞》/多楼层 Mod 一样。

这四种需求对应三种完全不同的实现，工作量差一个数量级。本文先记录调研结果与选型，不写代码。

### 约束

| 约束 | 现状 |
|---|---|
| 游戏版本 | **RimWorld 1.6.4871**（`About.xml` 只写 `1.6`） |
| 1.7 | 2026-10-12 进入 **unstable beta**，主分支仍是 1.6；官方说明 Waylith 是引擎/性能迁移，未提到原生 z 轴或多层 |
| DLC | 本机只装 Core，没有 Odyssey / Ideology / Biotech / Anomaly（`Data/` 下只有 `Core`） |
| 硬依赖 | 只有 Harmony；模块必须可独立交付 |
| 工程原则 | 原版优先、不重写底层系统、模块之间不直接引用（见 [README](../../README.md) §1、§3） |
| 当前进度 | 0.0.2 框架；六个模块均为占位/规划，M1 水网尚未开工 |

> 结论先行：**现在不适合自研「真地下室」**。如果只是要恒温地下房间/地下温室，同图内容就能做到 80%；如果必须要真分层，优先做兼容而不是自研。

## 现有 Mod 调研

数据截至 2026-10-25，订阅数来自 Steam `GetPublishedFileDetails`，只作量级参考。

| Mod | 版本 | 订阅 | 做法 | 参考价值 | 备注 |
|---|---|---:|---|---|---|
| [As above, So below 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3776015553) | 1.6 / 1.7 | 18,471 | **同一张地图叠「层带」**，用原版 region 系统连接；最多 3 层天空 + 地面 + 3 层地下 | 体验/兼容的黄金标准；证明「同图分层」可行 | 无 DLC、只需 Harmony；未见公开源码；AI 辅助开发；与其他分层 Mod 互斥 |
| [MultiFloors](https://steamcommunity.com/sharedfiles/filedetails/?id=3384660931) | 1.5 / 1.6 | 94,835 | **独立地图 + 跨层寻路/搬运/电力/热量同步**；自称旧 Z-Level 的精神续作 | 跨图 Job/寻路/管网的工程量参考；成熟度高 | 无 DLC（Odyssey 只影响多层 gravship）；未见公开源码；层间战斗不做 |
| [dd.Logical Floors](https://steamcommunity.com/sharedfiles/filedetails/?id=3813239016) | 1.6（冻结 1.0.56）/ 1.7 | 852 | **同一张地图、同坐标、每层独立空间数据**；原版 Job/移动 | 1.6 可用的同图分层；提供 API 文档（`API_INTEGRATION_EN.txt`、`BUILDING_SUPPORT.txt`、`Examples/OptionalFloors.cs`） | 1.6 版本已冻结；未见公开源码；适合做兼容/集成参考 |
| [RimLayers](https://steamcommunity.com/sharedfiles/filedetails/?id=3823557965) | 1.7 | 404 | 同一张地图的上下层、真实楼梯/电梯、跨层射击与 3D 爆炸 | 1.7 新引擎下的分层方向 | **1.7 only**，当前 1.6 不能用；与 CE/SOS2/Vehicle Framework 不兼容 |
| [Z-Levels: Living World](https://steamcommunity.com/sharedfiles/filedetails/?id=3664235563) | 1.6 / 1.7 | 361 | 多层地图 + 口袋维度；电力继电器、DBH 管道跨层、跨图工作 | 多地图 + 口袋维度的现代续作；DBH 管道/电力桥接参考 | 未见公开源码；源头是 RamRod 的 Z-Levels 概念 |
| [Z-Levels Beta](https://steamcommunity.com/sharedfiles/filedetails/?id=2127428910) | 1.1 / 1.2 | 18,771 | 多层地图、楼梯、地下洞穴、跨层 Job/战斗/电力 | **唯一公开源码的完整 Z-Level 实现**（[GitHub](https://github.com/RamRod6198/Z-Levels)）；作者已停止维护并声明代码可用 | 1.1/1.2 API 很旧，不能直接编译进 1.6；适合读架构 |
| [Private Basement](https://steamcommunity.com/sharedfiles/filedetails/?id=3727094520) | 1.6 / 1.7 | 440 | **口袋地图** + 地下室舱门/升降货梯/电力与管道桥/天窗/地下水培 | 功能最接近「ColonyKit 地下室」的现成产品 | **需要 Odyssey**（用口袋地图系统）；v0.8 WIP，存档兼容不保证 |
| [Basement](https://steamcommunity.com/sharedfiles/filedetails/?id=3547933601) | 1.6 | 367 | 可建造舱门生成 **25×25 口袋地图**；1000 钢 + 10 高级零部件；每图一个 | 最简单的口袋地图地下室实现 | **需要 Odyssey**；作者计划加尺寸/研究 |
| [Second Floor](https://steamcommunity.com/sharedfiles/filedetails/?id=3366432078) | 1.5 / 1.6 | 2,744 | **虚拟楼层**：楼梯按容量/升级模拟卧室，不产生真实地图格 | 只做「睡觉空间」的最低成本方案 | 不是真地下室；不能放仓库/工作台/农业 |
| [IsoRimZ](https://steamcommunity.com/sharedfiles/filedetails/?id=3824503834) | 1.7 | 1,758 | IsoRim 的上层楼层/楼梯/高山 | 1.7 等距渲染 + 分层的组合 | **1.7 only**，当前不可用 |

### 调研结论

- **1.6 上真正可用的「真分层」只有三类**：As above, So below 2（同图叠层）、dd.Logical Floors（同图独立空间数据）、MultiFloors / Z-Levels（独立地图 + 跨图补丁）。
- **口袋地图路线在工坊上被 Odyssey 事实绑定**：`Basement` 与 `Private Basement` 都声明 Required DLC = Odyssey。类本身未发现 DLC 门禁，但至少**贴图/示例内容在 Odyssey**：`Things/Building/Misc/CaveExit`、`UI/Commands/ViewCave|EnterCave|ExitCave` 在 Core 的 `Textures/` 下不存在，`CaveExit` ThingDef 和 `PlaceCaveExit` GenStep 却在 Core 的 Defs 里（见下节）。
- **同图分层的兼容性最好**，但代码量最大；独立地图的跨图补丁量也很大，且这些 Mod 之间**互斥**（As above 明确不兼容 MultiFloors / Z-Levels / 原 As above）。
- 没有任何一个现成 Mod 提供「ColonyKit 可以直接调用的地下室 API」；`dd.Logical Floors` 有集成 API，但只覆盖它自己的层模型。

## 原版 1.6 的口袋地图能力（技术事实）

这些类/Def 在 **Core 程序集与 Core Defs** 里，不在 DLC 程序集；但工坊实现普遍依赖 Odyssey 的资源。

| 类型 / Def | 作用 | 备注 |
|---|---|---|
| `RimWorld.MapPortal : Building, IThingHolder` | 可进入的传送门建筑；`GetOtherMap()` 首次进入时生成口袋地图；`GetDestinationLocation()` 指向出口；序列化 `pocketMap` / `exit` / `leftToLoad` | 类本身无 `MayRequireOdyssey`；Gizmo 图标硬编码 `UI/Commands/ViewCave` 等 |
| `Verse.PocketMapUtility.GeneratePocketMap(size, MapGeneratorDef, extraGenSteps, sourceMap)` | 创建 `PocketMapParent`，调用 `MapGenerator.GenerateMap(..., isPocketMap: true)`，加入 `Find.World.pocketMaps` | `WorldObjectDefOf.PocketMap` 在 Core 的 `WorldObjects.xml`，无 DLC 标记 |
| `RimWorld.PocketMapExit : MapPortal` | 口袋地图侧的返回出口；生成时通过 `PocketMapUtility.currentlyGeneratingPortal` 反向连回入口 | 需要 GenStep 在生成期间 spawn |
| `Verse.MapGeneratorDef` | `isUnderground`、`roofDef`、`pocketMapProperties`、`genSteps`、`forceCaves`、`customMapComponents` | `isUnderground=true` 时给全图铺 `RoofRockThick`（或 `roofDef`） |
| `Verse.PocketMapProperties` | `biome`、`temperature`、`destroyOnParentMapAbandoned`、`preventPrisonerEscape`、`canLaunchGravship`、`canBeCleaned` | 口袋地图必须给 `biome`，否则 `pocketTileInfo.PrimaryBiome` 为空 |
| `RimWorld.GenStep_PlaceCaveExit` | 找可站立格、清半径 4.5、spawn `ThingDefOf.CaveExit`、设 `PlayerStartSpot` | 硬编码 `CaveExit`，不是 `def.portal.exitDef` |
| Core Defs | `PocketMap`（WorldObjectDef）、`Underground`（BiomeDef / WeatherDef）、`CaveExit`、`PlaceCaveExit`、`EnterPortal` / `HaulToPortal` Job 与 WorkGiver | `BiomeDef Underground` 的 `baseWeatherCommonalities` 只有 `Underground` 天气 |

**因此，纯技术上存在一条「不依赖 Odyssey 的口袋地图地下室」路线**：自己定义 `ThingDef`（`thingClass=MapPortal` + `<portal>`）、`MapGeneratorDef`（`isUnderground` + `pocketMapProperties` + `Underground` biome）、出口 `ThingDef` 和 `GenStep`，并**自带** `CaveExit`/`ViewCave` 等缺失贴图。但这条路**没有现成 Mod 验证过**，需要先做 spike，不能直接排期。

## 三种实现路线

### A. 口袋地图（独立地图）

```text
地面：CK_BasementHatch (MapPortal)
        │ 首次进入
        ▼
PocketMapUtility.GeneratePocketMap
        ├─ PocketMapParent（不在世界地图上，进 Find.World.pocketMaps）
        ├─ MapGenerator.GenerateMap(isPocketMap: true)
        │    ├─ isUnderground → 全图厚岩顶
        │    ├─ pocketMapProperties.biome = Underground / temperature = 15
        │    └─ genSteps：挖出主室 + PlaceCaveExit
        ▼
地下：CK_BasementExit (PocketMapExit) → 返回地面
```

**优点**：原版 API 完整；地图尺寸/地形可控；地下恒温、无天气、无空投；代码量最小（一个模块几百行 + Defs）。
**代价**：
- 原版 WorkGiver/JobGiver 只扫当前地图：**没有自动跨图搬运、种植、洗澡、建造**；`JobDriver_EnterPortal` 只负责「人过去」。
- 电力/水网/财富/资源计数/温度/天气/贸易/商队都是 per-map；要跨图必须自己桥接，或像 MultiFloors/Z-Levels 那样打一整套补丁。
- 袭击、任务、空投、商队默认不认口袋地图；`MapParent` 不在世界地图上，需要自己处理生成/销毁/读档。
- 贴图/UI 资源缺失（Core 没有 `CaveExit` / `ViewCave` 等），需要自带。
- 与 Odyssey gravship 的口袋地图语义可能冲突（`canLaunchGravship`、`destroyOnParentMapAbandoned`）。

**适合**：独立「地窖/避难所/仓库扩展」，玩家能接受手动上下楼 + 显式货梯/桥；不适合作为淋浴/农业/立体库的自动工作区。

### B. 同图逻辑楼层

As above, So below 2 / dd.Logical Floors / RimLayers 的做法：楼层是同一张地图的「层带」或独立空间数据，pawn 的 Job、寻路、战斗、区域、存储仍然走原版（或大部分原版）逻辑。

**优点**：所有原版工作/搬运/战斗/账单都能跨层；没有「另一张地图」的 UI/事件问题；体验最好。
**代价**：要改地图尺寸、网格、region、寻路、渲染、Roof、温度、存档；`As above` 明确说两个层模型不能共存；兼容性问题会落在每一个直接读 `Map` 网格的 Mod 上。
**对 ColonyKit**：这是**再造一个大型框架模块**的工程，工作量与兼容风险都远超单个玩法模块，不建议自研。

### C. 同图「伪装」/虚拟层

- **同图地下房间**：在现有地图上用厚岩顶/建造屋顶 + 太阳灯 + 保温墙做「地下温室/酒窖」。没有新地图、没有新层，原版 Job/搬运/温度全部可用。
- **Second Floor 式虚拟层**：楼梯按容量模拟卧室，不产生真实空间；只适合「睡觉位」。
- **地形/屋顶伪装**：用 `RoofRockThick` + 地下地形做出「地下室」观感，实际仍在地面层。

**优点**：零兼容风险、零存档风险、工作量最小；对农业（地下温室）、Storage（酒窖/恒温库）、淋浴（室内浴室）都够用。
**代价**：不增加真实面积；不能实现「在山体/地图外挖新空间」；视觉上仍是一层。

## 对 ColonyKit 的建议

### 1. 现在不要开 7000 模块

M1 水网、M2 淋浴、M3 立体库都还没落地。地下室的跨图/分层成本会直接拖慢 0.1–0.3 的交付，而且它和六个模块的协同很弱（见下）。

### 2. 如果需求是「恒温/地下农业/酒窖/隐蔽仓库」：做同图内容，不做新地图

把「地下室」降级为 **Agriculture / Storage 的子功能**：

- `CK_Cellar` / `CK_UndergroundGreenhouse`：厚墙、保温、太阳灯/植物灯、防虫；用原版 `RoofRockThick` 或建造屋顶。
- 立体库的「恒温库」变体：降低腐烂/温度影响，不涉及跨图。
- 淋浴的「室内浴室」：只是房间类型，不需要新层。

这条路不需要 ADR 之外的新技术验证，也不会和任何分层 Mod 冲突。

### 3. 如果必须要真分层：优先做兼容，不自研

- 检测 [As above, So below 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3776015553)、[MultiFloors](https://steamcommunity.com/sharedfiles/filedetails/?id=3384660931)、[dd.Logical Floors](https://steamcommunity.com/sharedfiles/filedetails/?id=3813239016) 的 packageId。
- 给 ColonyKit 的建筑做跨层补丁：
  - 水网：`MapComponent_WaterNetwork` 是 per-map；在楼梯/电梯端点做「水网桥」，或在每层放泵/箱。
  - 电力：`CompPowerTrader`/`PowerNet` 是 per-map；参考 Z-Levels 的 `CompPowerZTransmitter` / `ConnectedPowerNets`。
  - 立体库/淋浴/农业：WorkGiver/JobGiver 需要跨层目标；如果对方 Mod 已提供跨层 Job 系统，只补「目标建筑可被扫描」。
- 在设置页明确「本模块的地下室内容需要上述任一分层 Mod」，不把对方代码复制进来。

这条路线符合 README 的「只检测/适配，不做硬依赖」，也是风险最低的「真分层」方案。

### 4. 如果坚持独立口袋地图：先做 2–3 天 spike

**目标**：证明「1.6 + 无 DLC + Harmony」能生成、进入、退出、存读档一个地下室口袋地图，且没有红字。

**最小原型**：

1. `CK_BasementHatch`：`ThingDef`，`thingClass=MapPortal`，`<portal>` 指向 `CK_BasementMap`，`pocketMapSize=50`；自带 `ViewCave`/`EnterCave` 贴图（或临时占位）。
2. `CK_BasementMap`：`MapGeneratorDef`，`isUnderground=true`，`roofDef=RoofRockThick`，`pocketMapProperties.biome=Underground`、`temperature=15`、`destroyOnParentMapAbandoned=true`；`genSteps`：`RocksFromGrid` → 自定义挖主室 → 自定义 spawn 出口。
3. `CK_BasementExit`：`ThingDef`，`thingClass=PocketMapExit`，自带贴图；GenStep 在生成时 spawn，确保 `currentlyGeneratingPortal` 链接。
4. 临时 DebugAction：新图生成舱门 → 小人进入 → 记录 `Map` 尺寸/温度/屋顶/出口位置 → 返回地面 → 存读档 → 再进入。

**Go/No-Go 标准**（全部满足才继续）：

- [ ] 进入后地图生成，出口可返回，`Player.log` 无 error（贴图缺失算 error，必须自备）。
- [ ] 存读档后 `pocketMap` / `exit` 链接仍在，地图不丢。
- [ ] 温度稳定在 `pocketMapProperties.temperature`，全图厚岩顶，无天气/空投。
- [ ] 小人能进入、返回、搬运 1 件物品、挖 1 格、造 1 个建筑。
- [ ] 电力/水网至少有一条可用的桥接原型（否则地下室对现有模块无价值）。
- [ ] 25×25 / 50×50 地图的 tick/渲染开销可接受（对比同规模地面地图）。
- [ ] 与 Odyssey（如果以后装）的 gravship 口袋地图不冲突；不装 DLC 时无红字。

**任一核心项失败且没有低成本修法 → 停止口袋地图路线**，转同图内容或兼容方案。

## 影响

| 方面 | 口袋地图（若走） | 同图内容 | 兼容现有分层 Mod |
|---|---|---|---|
| 新模块 | 7000，`Defs/` + `Source/Modules/Basement` + 贴图 | 并入 Agriculture/Storage，不新增模块 | 7000 或 Compat 目录，只加补丁 |
| 存档 | 新增 `Map` + `PocketMapParent` + `MapPortal` 引用；卸载风险高 | 无新存档结构 | 依赖对方存档结构；本 Mod 只存兼容标记 |
| 兼容 | 与所有分层 Mod 互斥；Odyssey 语义需验证 | 与分层 Mod 可共存（只要不抢同一格） | 依赖对方 API；需逐个版本适配 |
| 性能 | 多一张地图的 tick/渲染；小地图可控 | 无额外地图 | 取决于对方实现 |
| 文档 | 需要新的模块索引/功能页；明确「无自动跨图」 | 只需在农业/立体库页加一节 | 需要兼容矩阵与设置项 |

## 验证

- 先写 spike（见上），不要先写模块框架/UI。
- spike 通过后，再决定是否把 7000 写进 [0000-索引](../0000-索引.md) 的模块表；未通过就把本文状态改为「已废弃」，并记录失败点。
- 若走兼容路线，用 `ModLister.GetActiveModWithIdentifier` 做检测，缺 Mod 时给可读提示，不静默坏档。

## 相关文档

- [README](../../README.md) §1 目标与边界、§3.5 性能约束、§8 风险
- [框架内核](../框架内核.md)
- [水网服务](../水网服务.md)（per-map）
- [立体库模块](../立体库模块.md)（内部 `ThingOwner`，跨图不可见）
- [农业模块](../农业模块.md)（肥力网格 per-map）
- [决策记录索引](Index.md)
- [调查指南](../调查指南.md)

## 变更记录

| 日期 | 内容 |
|---|---|
| 2026-10-25 | 创建；记录 10 个现有 Mod、三条实现路线、口袋地图 API 事实与 spike 验收标准。 |
