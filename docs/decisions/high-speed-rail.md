# 高铁：世界地图铁路网 vs 车站直送 vs 列车实体

- 状态：提议（G3 构想，未排期）
- 日期：2026-10-25
- 关联任务：9000（G3 候选；若确认，建议排在 M8/1.0 后评估）
- 影响范围：模块 / 世界地图 / 存档 / 兼容 / 性能 / 经济

## 0. 结论先行

**首版不要做「列车实体」，也不要用 Vehicle Framework。** 用原版已经存在的**世界地图道路层**做铁路：

```text
RoadDef「CK_HighSpeedRail」（movementCostMultiplier ≈ 0.15）
  + 车站/铁路端点（Building / WorldObject）
  + 用 WorldGrid.OverlayRoad 把线路铺到 SurfaceTile.potentialRoads
  → 原版寻路/移动/渲染/存档自动生效
  → 商队就是列车：沿铁路移动时加速、夜间不休、外观换成列车
```

这样首版只需要写「车站、线路、扣料、选点、UI」，**不需要重写商队、寻路、世界地图渲染或载具**；风险最低，也最符合 ColonyKit 的 Core-only、原版优先原则。

**速度口径**：铁路 `movementCostMultiplier = 0.15`，约等于**越野速度的 6.7 倍、原版道路的 3.3 倍**；默认人类商队理论约 90 格/天，30 格约 8 小时（默认夜车；关掉夜车约 16 小时），越野 30 格约 4.3 天。

**分阶段**：R0 先做铺轨/渲染 spike；R1 车站 + 线路 MVP；R2 列车化（外观/夜间/载重）；R3 运营（拆除/维护/事件/自动化）。

## 1. 背景

### 1.1 玩家痛点

原版出远门（贸易、任务、探图、搬人）有三层成本：

1. **时间**：默认人类商队约 13.6 格/天（理论值），夜间还要休息，实际约 7 格/天；30 格就是 4 天以上。
2. **补给与载重**：食物、药物、驮运动物、载重上限，队伍越大越慢。
3. **操作**：选路、装货、途中事件、到点手动操作；高频远征很累。

玩家要的「高铁科技」不是「再快一点的鞋子」，而是**一条可重复使用、可见、需要建设的高运力通道**。

### 1.2 目标与非目标

**目标（首版）**

1. 新研究「铁路运输 / 高速铁路」，解锁车站与铺轨。
2. 玩家可以在世界地图上从车站向目标铺一条铁路；有距离成本、建造时间、最大长度。
3. 商队沿铁路移动时显著加速（≥ 5 倍越野），寻路会主动选择铁路。
4. 线路可见、可存档、可跨读档；不依赖 DLC。
5. 车站是端点：没有车站/铁路端点就不能凭空铺轨。
6. 与现有模块、修路类 Mod、商队类 Mod 尽量兼容。

**非目标（首版）**

- 同地图内跑火车、车厢编组、站台调度、时刻表、自动货运。
- 独立「列车」WorldObject / Pawn / Vehicle Framework 载具。
- 铁路拆除/维护/破坏/修复（放 R3）。
- 与 Odyssey gravship、SRTS、CE 的深度联动。
- 真实 3D 轨道、桥梁、隧道、信号系统。

### 1.3 约束

| 约束 | 现状 |
|---|---|
| 游戏版本 | **RimWorld 1.6.4871 rev590**（`About.xml` 只写 `1.6`） |
| DLC | 本机只装 Core；不依赖 Odyssey / Royalty / Ideology / Biotech / Anomaly |
| 硬依赖 | 只有 Harmony；模块必须可独立交付 |
| 工程原则 | 原版优先、不重写底层系统、模块之间不直接引用、首版占位美术 |
| 当前进度 | 0.0.2 框架；卫生/立体库/蓝图/机器人已有首版；铁路是 G3 候选 |

## 2. 现有 Mod 调研

数据来自本机 `docs/steam-mods` 快照、工坊检索与本地目录；订阅量只作量级参考，未逐项核实。

| Mod | 版本 | 做法 | 参考价值 | 备注 |
|---|---|---|---|---|
| **Roads of the Rim (Continued)** | 1.0–1.6 | 在世界地图上修路，带修路事件 | **直接先例**：证明「玩家铺世界地图道路」可行；可参考它的选点/扣料/事件 | 具体实现未核对源码；本机未装 |
| **Trains of the Rim** | 1.6 | 铁路/列车主题 Mod | **体验参考**：玩家对「车站、列车、批量运输」的预期 | 具体实现/依赖未核实；本机未装 |
| **CaravanOptions** | 1.0+ | Harmony 调商队速度：全局倍率、按道路类型减速、强制夜间旅行 | **参数参考**：证明速度/夜间/道路倍率是玩家真实需求；也说明「只改速度」会变成数值滑块 | 与铁路可叠加，需在设置里提示 |
| **Vehicle Framework** | 1.4–1.6 | 地面载具框架 | 若走「列车 = 载具」的底座 | 783k 订阅，但重依赖、非 Core-only；首版不碰 |
| **SRTS Expanded** | 1.0–1.3（tag） | 可重复使用的飞行运输船 | 「点对点快速运输」的体验黄金标准 | 不是铁路；可作为「车站直送」的对照 |
| **其他修路 Mod** | 各异 | 世界地图道路 | 道路优先级/兼容参考 | 与铁路共用 `RoadDef` 层，需测优先级 |

**调研结论**

- 「世界地图修路」已有成熟先例，说明**道路层就是原版给基础设施留的扩展点**；铁路不需要另起一套地图数据。
- 「列车实体」在工坊上通常要么走载具框架，要么是点对点运输；没有 Core-only、模块化的「铁路网 + 车站」产品，这是空位。
- 只改商队速度的 Mod 很多，但那只是滑块，不是「高铁科技」；玩家要的是建设、线路、车站、运力。

## 3. 原版 1.6 的技术事实

以下类型/方法都在本机 **Core 程序集**里，且已用元数据核对可见性；不依赖 DLC。

### 3.1 商队速度

| 类型 / 方法 | 事实 | 对方案的意义 |
|---|---|---|
| `CaravanTicksPerMoveUtility.GetTicksPerMove` | 基础值 ≈ `BaseHumanlikeTicksPerCell × 340`；默认人类约 **4420 tick/格** | 铁路倍率乘在这个成本上 |
| `Caravan_PathFollower.CostToMove` | `clamp((int)(TicksPerMove × 地形难度 × 道路倍率), 1, 30000)` | **道路倍率直接进移动成本**；不需要改商队 tick |
| `Caravan_PathFollower.CostToPayThisTick` | 每 tick 支付 `nextTileCostTotal / 30000`，一格走完需要 `nextTileCostTotal` tick | 一格耗时 = 成本，可直接换算格/天 |
| `WorldPathing.FindPath` | 公共方法；A* 时对每条边调用 `GetRoadMovementDifficultyMultiplier` | **寻路也会主动选铁路**，不只是移动加速 |
| `Caravan.TicksPerMove` | 只读属性，内部走 `GetTicksPerMove` | 想全局改速可 patch，但本方案不需要 |

**换算**：

```text
理论格/天 = 60000 / CostToMove
CostToMove(越野 1.0)   = 4420  → 13.6 格/天
CostToMove(道路 0.5)   = 2210  → 27.1 格/天
CostToMove(铁路 0.15)  =  663  → 90.5 格/天
```

实际还有夜间休息，约再减半；**铁路默认夜车**，所以按 90 格/天算，30 格约 8 小时；关掉夜车则约 45 格/天、16 小时。越野 30 格约 4.3 天。

### 3.2 世界地图道路层

| 类型 / 方法 | 事实 | 对方案的意义 |
|---|---|---|
| `RoadDef.movementCostMultiplier` | 公共字段；原版道路统一 0.5 | 铁路设 0.15，即 6.7 倍越野 |
| `RoadDef.priority` | 公共字段；原版道路 10–30 | 铁路设 1000，覆盖同边的低级道路 |
| `RoadDef.worldRenderSteps` | `layer`（`RoadWorldLayerDef`）+ `width` | 铁路线由原版道路渲染器画，白嫖 |
| `RoadWorldLayerDef` | 只有 `order`、`color` | 新增 `CK_Rail` 图层（颜色/顺序） |
| `SurfaceTile.potentialRoads` | **公共字段**，`List<RoadLink>{ neighbor, road }` | 可以直接读写；不用反射 |
| `WorldGrid.OverlayRoad(from, to, road)` | **公共方法**，双向加 `RoadLink`，按 `priority` 覆盖低级道路 | 铺轨只需对每对相邻格调用一次 |
| `WorldGrid.GetRoadMovementDifficultyMultiplier` | 从 `fromTile` 的 `potentialRoads` 找 `toTile` 的 `RoadLink`，返回倍率 | 加完立即生效，无需重算地形缓存 |
| `SurfaceLayer.SerializeRoads` | 序列化 `potentialRoads` 的 `neighbor/road.shortHash` | 铁路随原版存档保存；缺 Def 时跳过 |

**结论**：`RoadDef + OverlayRoad` 是原版公开 API，不是私有 hack；`WorldGrid.OverlayRoad` 和 `SurfaceTile.potentialRoads` 都是 **public**，ColonyKit 不需要引入 Publicizer。

### 3.3 渲染与刷新

| 类型 / 方法 | 事实 | 对方案的意义 |
|---|---|---|
| `WorldRenderer.SetAllLayersDirty` | 公共方法 | 铺完一段后强制重建世界地图网格 |
| `WorldDrawLayerBase` / `RoadWorldLayerDef` | 道路层按 `RoadDef.worldRenderSteps` 生成 | 铁路线颜色/宽度由 Def 控制，无需自写 mesh |
| `WorldPath.DrawPath` | 公共方法 | 选点时可选地画预览线（R2） |

**风险点**：`OverlayRoad` 的 IL 里没有看到 `SetDirty`；运行时铺轨后必须手动 `SetAllLayersDirty()`（或只 dirty 道路层），否则线路可能不显示。R0 spike 必须验证这一点。

### 3.4 世界组件 / 世界对象

| 类型 / 方法 | 事实 | 对方案的意义 |
|---|---|---|
| `World.FillComponents` | 反射实例化所有非抽象 `WorldComponent` 子类，构造函数参数是 `World` | **定义 `WorldComponent_RailNetwork(World world)` 即自动挂载**，不用注册 |
| `World.GetComponent<T>` | 公共方法 | 车站/补丁里直接取铁路网 |
| `WorldObjectDef` / `WorldObject` | 支持 `worldObjectClass` 与 `comps` | 铁路端点可以是一个可选中的世界对象 |
| `WorldObjectCompProperties_FormCaravan` | 原版 `Camp`/`Settlement` 在用 | 端点可以「就地组商队」，形成「铺到哪、从哪继续走」的循环 |
| `WorldObjectMaker.MakeWorldObject` | 公共静态方法 | 线路完成时在终点生成铁路端点 |

### 3.5 商队外观 / 夜间

| 类型 / 方法 | 事实 | 对方案的意义 |
|---|---|---|
| `Caravan.Material` | **public virtual** getter | Harmony postfix 换成列车贴图；不用改 Def |
| `Caravan.NightResting` | public getter | 在铁路上的玩家商队可以不休眠，实现「夜车」 |

## 4. 候选方案

| 方案 | 做法 | 优点 | 代价 | 风险 |
|---|---|---|---|---|
| **A. 世界地图铁路网**（推荐） | `RoadDef` + `OverlayRoad` + 车站/端点；商队沿铁路加速 | 复用寻路/移动/渲染/存档；Core-only；代码量小 | 不是独立列车实体；线路是「道路」语义；拆除麻烦 | 运行时渲染刷新；与修路 Mod 优先级 |
| **B. 车站直送** | 车站之间点对点，计时后把人/货送到 | 代码最少；不受地形/寻路影响；容易做「班次」 | 没有可见线路；像传送/穿梭机；要重做装载/到达 | 与原版商队生命周期脱节；事件/存档另做 |
| **C. 列车实体** | 自定义 `WorldObject_Train` 或 `Caravan` 子类，沿线路移动 | 最像「列车」；可做车厢/时刻表/载客 | 要处理 Pawn 离图、物品、事件、存档、AI；工作量大 | 高；容易和商队/载具 Mod 冲突 |
| **D. 只改速度** | `CaravanOptions` 式全局倍率 | 一天做完 | 不是科技/建设；没有铁路体验 | 数值失衡；玩家不会觉得是「高铁」 |

**结论**：选 **A 作为首版**；B 作为 A 的渲染/兼容失败时的回退；C 放 R2/R3，等 A 稳定后再评估；D 只作为调试/设置项，不作为产品。

## 5. 推荐方案 A：铁路网 + 车站/端点 + 商队列车化

### 5.1 玩家流程

```text
研究「铁路运输」→「高速铁路」
  → 在殖民地建造「铁路车站」（3×3，耗电）
  → 选中车站 →「铺设铁路」→ 在世界地图选目标格
      → 显示路径长度 / 造价 / 工期 / 是否可建
      → 确认：从殖民地库存扣钢/零部件，开始分段施工
  → 线路逐段出现；终点自动生成「铁路端点」
  → 在车站组商队，选远方目的地
      → 寻路自动沿铁路走；移动速度 ×6.7；夜间不休
      → 商队图标变成列车；到达后正常进定居点/事件
  → 从铁路端点继续铺下一段，形成网络
```

### 5.2 系统分层

```text
Defs
  RoadDef CK_HighSpeedRail（倍率/优先级/渲染）
  RoadWorldLayerDef CK_Rail（颜色/顺序）
  ThingDef CK_RailStation（3×3，500W）
  WorldObjectDef CK_Railhead（可选，端点）
  ResearchProjectDef CK_RailTransport / CK_HighSpeedRail
  DesignationCategoryDef CK_Rail（建造菜单）

运行时
  WorldComponent_RailNetwork
    ├─ stations: List<RailStation>（建筑 / 端点）
    ├─ lines: List<RailLine>（起点/终点/逐格 tile/进度）
    ├─ railTiles: HashSet<int>（O(1) 判断是否在铁路上）
    └─ WorldComponentTick：每 60 tick 推进施工
  CompRailStation（建筑侧）
    ├─ PostSpawnSetup → RegisterStation
    ├─ PostDestroy → UnregisterStation
    └─ Gizmo「铺设铁路 / 铁路网络」
  WorldObject_CKRailhead（端点侧）
    ├─ SpawnSetup → RegisterStation
    ├─ GetGizmos → 继续铺轨 / 组商队
    └─ GetInspectString → 线路数/长度
  RailUtility
    ├─ FindPath（WorldPathing）
    ├─ BuildCost / TryPay（扣钢/零部件）
    ├─ OverlaySegment（OverlayRoad）
    └─ CreateRailhead
  Harmony
    ├─ Caravan.Material postfix（列车贴图）
    └─ Caravan.NightResting postfix（夜车）
```

### 5.3 数据模型

```csharp
public class RailStation
{
    public string id;          // 建筑 ThingID / 端点 WorldObject.ID
    public PlanetTile tile;
    public string label;
    public RailStationKind kind; // Building / Railhead / Settlement
}

public class RailLine
{
    public int id;
    public PlanetTile from;
    public PlanetTile to;
    public List<int> tiles;    // 逐格 tileId
    public int nextIndex;      // 下一段
    public int nextBuildTick;
    public bool complete;
}
```

- `railTiles` 在 `ExposeData` 后从 `lines` 重建；`IsOnRail` 只查 `HashSet`。
- `RailLine` 只存 tile 列表，不存 `RoadLink`；读档后若线路完整，`OverlayRoad` 是幂等的（相同 `RoadDef` 直接返回），可安全重放。
- 车站在 `PostSpawnSetup` 注册；读档时建筑重新 spawn，会重新注册。端点 `WorldObject` 同理。
- `WorldComponent_RailNetwork` 的构造函数必须是 **public**（`World.FillComponents` 用 `Activator.CreateInstance(type, world)`，只找公共构造），否则组件不会被创建。

### 5.4 铺轨

```csharp
WorldPath path = Find.WorldGrid.Surface.Pather.FindPath(fromTile, toTile, null, null);
if (!path.Found) { /* 提示不可达 */ return; }
List<PlanetTile> tiles = new List<PlanetTile>();
while (path.NodesLeftCount > 0) tiles.Add(path.ConsumeNextNode());
path.ReleaseToPool();
```

- 路径会绕山/水，长度按实际格数算；**只对还不是铁路的格收费**。
- 只允许 `PrimaryBiome.allowRoads` 的格；水域/冰盖/无路地貌直接拒绝（原版 `get_Roads` 会返回 null，铺了也不生效）。
- 每段施工：`OverlayRoad(tiles[i], tiles[i+1], RailDefOf.CK_HighSpeedRail)`；完成一段后 `SetAllLayersDirty()`（或只 dirty 道路层）。
- 最大长度默认 80 格；超过则提示「分段铺设」。
- 目标格若已有非定居点世界对象，拒绝；若是定居点，线路停在定居点，不生成端点（R2 再考虑外交/贸易）。

### 5.5 扣料

首版从**殖民地库存**扣（`Map.resourceCounter` 检查 + `listerThings` 移除），不要求把钢搬到车站；这样不增加玩家最讨厌的搬运微操。R2 再考虑「车站料仓」作为可选硬核模式。

```text
每格：12 钢 + 0.2 零部件（30 格 = 360 钢 + 6 零部件）
端点：+100 钢 + 2 零部件（一次性）
车站：250 钢 + 8 零部件 + 500W
```

扣料失败时在确认框里红字列出缺口，不开始施工。

### 5.6 列车化（R2）

- **外观**：`Caravan.Material` postfix：玩家商队 `IsOnRail(Tile)` 时返回缓存的列车 `Material`；离开铁路恢复原图标。
- **夜车**：`Caravan.NightResting` postfix：玩家商队**正在移动**、在铁路上且设置允许时返回 `false`；停在车站时仍正常休息，避免到站不回复疲劳。
- **载重**：`Caravan.MassCapacity` postfix，铁路上 +500 kg（R2，需同步 `MassCapacityExplanation`）。
- **信件**：线路通车、首班列车发车/到达；R2。
- **端点组队**：`WorldObjectCompProperties_FormCaravan` 让玩家在端点直接组商队；验证 `canHaveMap=false` 的自定义对象是否可用（R0/R1 spike）。

### 5.7 UI

- **选点**：`WorldTargeter.BeginTargeting`，参考 `RoyalTitlePermitWorker_CallShuttle` 的选点流程；选中后弹 `Dialog_RailConfirm`，显示路径长度/造价/工期/缺口。
- **车站 Gizmo**：「铺设铁路」「铁路网络」；断电阻止新线路。
- **网络窗口**（R2）：列出线路、长度、状态、是否通车；拆除/维护放 R3。
- **研究页**：`CK_RailTab`，两个项目 + 一条链。

## 6. 数值草案

| 项目 | 默认值 | 说明 |
|---|---:|---|
| 研究「铁路运输」 | 1,200 | 前置 `Electricity` + `Machining`；`Industrial` |
| 研究「高速铁路」 | 2,400 | 前置「铁路运输」 |
| 车站 | 250 钢 / 8 零部件 / 500W / 3×3 | 需要电力；断电不能铺新线 |
| 铁路 | 12 钢 + 0.2 零部件/格 | 30 格 ≈ 360 钢 + 6 零部件 |
| 端点 | 100 钢 + 2 零部件 | 线路终点一次性 |
| 施工 | 1,500 tick/格 | 30 格 ≈ 0.75 天 |
| 单线上限 | 80 格 | 超过提示分段 |
| 速度倍率 | 0.15 | 越野 6.7×；道路 3.3× |
| 夜车 | 开 | 设置可关 |
| 载重加成 | +500 kg | R2 |
| 拆除/维护 | 无 | R3 |

**平衡口径**：一条 30 格线 ≈ 710 钢 + 16 零部件 + 3,600 研究点（车站 250/8 + 铁路 360/6 + 端点 100/2）；相当于一次中期远征的投入，但之后每次往返都省 4 天。铁路应该比「每次造运输舱/叫穿梭机」便宜，但比「修一条普通路」贵得多。

## 7. 最小 Def 草案

> 以下是 R0 spike 需要的最小集合；数值以 Def 为准，施工/扣料逻辑在 C#。

```xml
<!-- Defs/Rail/RoadDefs.xml -->
<Defs>
	<RoadDef>
		<defName>CK_HighSpeedRail</defName>
		<label>high-speed rail</label>
		<priority>1000</priority>
		<movementCostMultiplier>0.15</movementCostMultiplier>
		<worldRenderSteps>
			<li>
				<layer>Outline</layer>
				<width>0.7</width>
			</li>
			<li>
				<layer>CK_Rail</layer>
				<width>0.45</width>
			</li>
		</worldRenderSteps>
	</RoadDef>
</Defs>
```

```xml
<!-- Defs/Rail/RoadWorldLayerDefs.xml -->
<Defs>
	<RoadWorldLayerDef>
		<defName>CK_Rail</defName>
		<order>500</order>
		<color>(0.55, 0.65, 0.75)</color>
	</RoadWorldLayerDef>
</Defs>
```

```xml
<!-- Defs/Rail/ThingDefs_RailStation.xml（骨架） -->
<Defs>
	<ThingDef ParentName="BuildingBase">
		<defName>CK_RailStation</defName>
		<label>rail station</label>
		<description>A station where rail lines begin and caravans board the train.</description>
		<thingClass>ColonyKit.Modules.Rail.Building_CKRailStation</thingClass>
		<size>(3,3)</size>
		<graphicData>
			<texPath>Things/Building/CK_RailStation</texPath>
			<graphicClass>Graphic_Single</graphicClass>
		</graphicData>
		<costList>
			<Steel>250</Steel>
			<ComponentIndustrial>8</ComponentIndustrial>
		</costList>
		<comps>
			<li Class="CompProperties_Power">
				<compClass>CompPowerTrader</compClass>
				<basePowerConsumption>500</basePowerConsumption>
			</li>
		</comps>
		<researchPrerequisites>
			<li>CK_HighSpeedRail</li>
		</researchPrerequisites>
		<designationCategory>CK_Rail</designationCategory>
		<terrainAffordanceNeeded>Light</terrainAffordanceNeeded>
	</ThingDef>
</Defs>
```

```xml
<!-- Defs/Rail/WorldObjectDefs_Railhead.xml（骨架） -->
<Defs>
	<WorldObjectDef ParentName="StaticWorldObjectBase">
		<defName>CK_Railhead</defName>
		<label>railhead</label>
		<worldObjectClass>ColonyKit.Modules.Rail.WorldObject_CKRailhead</worldObjectClass>
		<texture>World/WorldObjects/CK_Railhead</texture>
		<expandingIcon>true</expandingIcon>
		<expandingIconTexture>World/WorldObjects/Expanding/CK_Railhead</expandingIconTexture>
		<expandingIconPriority>60</expandingIconPriority>
		<comps>
			<li Class="WorldObjectCompProperties_FormCaravan" />
		</comps>
	</WorldObjectDef>
</Defs>
```

```xml
<!-- Defs/Rail/Research.xml（骨架） -->
<Defs>
	<ResearchTabDef>
		<defName>CK_RailTab</defName>
		<label>Rail</label>
		<generalTitle>Rail transport</generalTitle>
		<generalDescription>Build stations and lay high-speed rail across the world map.</generalDescription>
	</ResearchTabDef>

	<ResearchProjectDef>
		<defName>CK_RailTransport</defName>
		<label>rail transport</label>
		<description>Unlock rail stations and basic right-of-way construction.</description>
		<baseCost>1200</baseCost>
		<techLevel>Industrial</techLevel>
		<prerequisites>
			<li>Electricity</li>
			<li>Machining</li>
		</prerequisites>
		<tab>CK_RailTab</tab>
		<researchViewX>0</researchViewX>
		<researchViewY>0</researchViewY>
	</ResearchProjectDef>

	<ResearchProjectDef>
		<defName>CK_HighSpeedRail</defName>
		<label>high-speed rail</label>
		<description>Lay high-speed rail. Caravans on rail move far faster and can travel through the night.</description>
		<baseCost>2400</baseCost>
		<techLevel>Industrial</techLevel>
		<prerequisites>
			<li>CK_RailTransport</li>
		</prerequisites>
		<tab>CK_RailTab</tab>
		<researchViewX>1</researchViewX>
		<researchViewY>0</researchViewY>
	</ResearchProjectDef>
</Defs>
```

> `RoadDef` 的 `roadGenSteps`、`tilesPerSegment`、`pathingMode` 是给世界生成的字段；运行时铁路不参与生成，可留空。若 XML 校验要求，补 `<tilesPerSegment>1</tilesPerSegment>` 与 `<pathingMode>Avoid</pathingMode>`。

## 8. 实现落点

```text
Source/Modules/Rail/
  RailModule.cs                 // IColonyKitModule，Id = "rail"
  RailSettings.cs               // IColonyKitModuleSettings
  RailSettingsPage.cs           // 设置页「铁路」
  RailDefOf.cs                  // RoadDef / ThingDef / WorldObjectDef / LetterDef
  RailStation.cs                // 车站记录
  RailLine.cs                   // 线路记录 + Scribe
  WorldComponent_RailNetwork.cs // 自动挂载；施工推进；IsOnRail
  RailUtility.cs                // FindPath / 造价 / 扣料 / OverlayRoad / 端点
  Building_CKRailStation.cs     // 3×3 车站
  CompRailStation.cs            // 注册 + Gizmo
  WorldObject_CKRailhead.cs     // 端点 + Gizmo + FormCaravan
  Dialog_RailConfirm.cs         // 选点后的确认框
  Dialog_RailNetwork.cs         // 线路列表（R2）
  Patch_CaravanTrainVisual.cs   // Caravan.Material
  Patch_CaravanNightTravel.cs   // Caravan.NightResting
  DebugActions_Rail.cs          // 铺 10 格 / 打印速度 / 存档测试

Defs/Rail/                      // RoadDef / RoadWorldLayerDef / ThingDef / WorldObjectDef / Research / DesignationCategory
Languages/ChineseSimplified/    // Keyed + DefInjected
Languages/English/
Textures/Things/Building/CK_RailStation.png
Textures/World/WorldObjects/CK_Railhead.png
Textures/World/WorldObjects/CK_Train.png
```

**设置项**（`RailSettings`）：

| 字段 | 范围 | 默认 | 说明 |
|---|---|---|---|
| `enabled` | bool | true | 模块开关 |
| `speedMultiplier` | 0.05–0.5 | 0.15 | 写入 `RoadDef.movementCostMultiplier` |
| `costMultiplier` | 0.5–2 | 1 | 只影响新线路 |
| `ticksPerTile` | 300–3000 | 1500 | 只影响新线路 |
| `allowNightTravel` | bool | true | 夜车 |
| `trainVisual` | bool | true | 列车贴图 |
| `maxLineLength` | 20–120 | 80 | 单线上限 |

> 改 `speedMultiplier` 时直接改 `RailDefOf.CK_HighSpeedRail.movementCostMultiplier`；原版寻路/移动每次现算，不需要重算缓存。

## 9. 分阶段与验收

### R0：技术 spike（1–2 天，必须先做）

1. 加 `RoadDef` + `RoadWorldLayerDef`。
2. Debug 动作：从殖民地 tile 向东方铺 10 格 `OverlayRoad`；`SetAllLayersDirty()`。
3. 生成商队，分别记录越野/铁路的 `TicksPerMove`、`CostToMove`、实际走完时间。
4. 存读档；退出重进；删除 Mod 后再读档（确认未知 `RoadDef` 不崩）。
5. 截图：线路在世界地图上可见，颜色/宽度正确。

**Go/No-Go**：铁路可见 + 实际移动 ≥ 5× + 寻路会选铁路 + 存读档正常。任何一条失败，先修 spike，不要进 R1；若渲染无法刷新，回退 B 或自写 `WorldDrawLayer`。

### R1：MVP（5–7 天）

- 研究页 + 车站建筑 + 车站注册 + 选点 + 造价/工期 + 分段施工 + 端点 + 中文/英文 + 设置页 + Debug 动作。
- 验收：从零开始能研究→建站→铺 20 格→商队走完；造价/缺口/工期与 UI 一致；存读档后线路和进度保留。

### R2：列车化（3–5 天）

- 列车贴图、夜车、载重 +500kg、通车/发车/到达信件、网络窗口、端点组商队。
- 验收：商队图标在铁路上变列车；夜间不停；载重 tooltip 正确；端点可组队继续走。

### R3：运营（5–8 天，可选）

- 拆除/修复（直接操作 `potentialRoads`，`OverlayRoad` 不支持移除）、维护费/电力、线路被破坏事件、AI 是否可用、时刻表/自动货运、主按钮。
- 验收：拆线退款/恢复道路；破坏后可修；AI 行为符合设置。

### 全局验收

| 维度 | 标准 |
|---|---|
| 功能 | 研究→建站→铺轨→商队加速→到达；全链路可重复 |
| 性能 | `IsOnRail` O(1)；`WorldComponentTick` 60 tick 一次；3× 速度 10 分钟无 tick 尖峰 |
| 存档 | 线路/进度/站点/端点/列车状态读档一致；删除 Mod 不崩 |
| 兼容 | 与 Roads of the Rim、CaravanOptions 同开可加载；优先级冲突有提示 |
| 本地化 | 无 `Keyed.` 泄漏；中英完整 |
| 平衡 | 30 格线 ≈ 710 钢 + 16 零部件；比普通道路贵、比穿梭机便宜 |

## 10. 风险与对策

| 风险 | 影响 | 对策 |
|---|---|---|
| `OverlayRoad` 运行时铺轨后地图不刷新 | 线路不可见，功能不可用 | R0 先验证 `SetAllLayersDirty`；失败则自写 `WorldDrawLayer`（仍用道路数据做寻路/速度） |
| 与修路 Mod 的 `RoadDef` 优先级冲突 | 铁路铺不上/被覆盖 | `priority=1000`；`OverlayRoad` 拒绝时提示；文档写明兼容策略 |
| AI 商队也吃铁路加速 | 袭击者沿铁路快速接近 | 首版接受（基础设施公共）；R3 若要玩家专属，需对 `WorldPathing` 做 transpiler 或自定义代价，成本较高 |
| 线路无法拆除 | 铺错永久占格 | R3 直接操作 `potentialRoads` 移除；首版在确认框写「不可拆除」 |
| 速度过快，探索/贸易失去意义 | 平衡崩 | 0.15 默认 + 设置 0.05–0.5；成本/工期/单线上限；先测 30/60/80 格 |
| 目标格不可达/被占 | 铺轨失败 | 选点后先 `FindPath`，失败红字；占用格拒绝 |
| 端点 `FormCaravan` 不生效 | 不能从端点继续走 | R1 spike 验证；失败则给端点加自定义「组商队」Gizmo |
| 夜车导致长线缺觉 | 小人疲劳/精神崩溃 | 夜车只在移动中生效；长线用卧铺/休息车厢或设置项抵消，R2 调平衡 |
| 删除 Mod 后未知 `WorldComponent` | 读档报错 | 文档标注「移除前先拆线/清空」；R0 测删除 Mod 读档 |
| `SetAllLayersDirty` 造成卡顿 | 铺轨时掉帧 | 只在每段/完成时调用；R0 测 100 格耗时，必要时只 dirty 道路层 |
| 占位美术像「一条灰线」 | 观感差 | R1 用双层线（深色描边 + 钢蓝内线）+ 车站/端点图标；R2 列车贴图 |

## 11. 待确认

1. **铁路是否只给玩家用？** 首版公共（实现简单）；若要玩家专属，`WorldPathing` 里的道路倍率是按边读取的，直接按 `caravan.Faction` 过滤需要 transpiler 或自定义代价，侵入性更高。
2. **扣料口径**：首版从殖民地库存扣；是否要「车站料仓 + 搬运」的硬核模式？
3. **目标是否包含 AI 定居点**：首版允许线路停在定居点但不生成端点；是否要贸易/外交接口？
4. **列车实体**：R2 只做商队列车化，还是直接上 `WorldObject_Train`？建议先 A 跑通，再看玩家反馈。
5. **拆除/维护/破坏**：R3 的范围；是否要铁路被袭击/中断的玩法？
6. **Odyssey gravship 平衡**：本机没装 DLC；如果以后装，铁路应比 gravship 便宜但固定路线。
7. **美术**：车站 3×3 占位几何、列车图标、线路颜色；是否要铁轨枕木的专用 `WorldDrawLayer`（R3）。

## 12. 相关文档

- [README](../../README.md) §5：模块范围与构想池
- [0000-索引](../0000-索引.md)：模块编号与文档地图
- [decisions/Index.md](Index.md)：ADR 模板与待定项
- [air-power.md](air-power.md)：同类的「先调研、再选型、spike 验收」写法
- [basement.md](basement.md)：口袋地图/世界对象/存档风险的参考
