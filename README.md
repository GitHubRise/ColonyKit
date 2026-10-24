# 殖民地套件（Colony Kit）

> 一个**模块化**的殖民地生活设施框架 + 六套子系统：淋浴、立体库、农业、蓝图、智能机器人、汉化集合。
>
> 本文件是**规划稿 + 任务清单**，不是完工说明。0.0.2 框架已从 `mod-template` 0.0.2 同步；`localization`（开发者菜单汉化）已移植，其余三个参考模块仍是占位。

| 项目 | 值 |
|---|---|
| 目录 / 程序集 / 命名空间 | `ColonyKit`（三者一致） |
| 显示名 | 殖民地套件 / Colony Kit |
| `packageId` | `local.colonykit`（发布后不再改） |
| 目标游戏版本 | RimWorld 1.6.4871（`supportedVersions` 只写 `1.6`） |
| 目标框架 | `net472` + `Krafs.Rimworld.Ref` |
| 硬依赖 | Harmony（`brrainz.harmony`） |
| 可选兼容 | DBH、Stack XXL、RJW（只检测/适配，不做硬依赖） |
| 工程状态 | 0.0.2 框架已生成（M0 完成）；汉化模块已移植，其余三个参考模块占位 |

> 本文件 2026-10-25 曾按 `CustomEnhance`（自定义强化）立项，同日先调整为 `ColonyKit` 五系统，随后追加智能机器人模块；旧的强化设计未实施。

---

## 0. 0.0.2 实现状态

| 项 | 状态 | 说明 |
|---|---|---|
| 框架复制 | ✅ | 基线来自 `sources/mod-template` 0.0.1；已同步 0.0.2 的框架修复与构建改进 |
| 构建 | ✅ | `build.ps1 -Clean` 0 error / 0 warning |
| 静态校验 | ✅ | `verify.ps1` 0 问题 / 0 警告 |
| 汉化模块 | ✅ 已移植 | 开发者菜单 Debug 标签 + tooltip + 翻译方案设置；quicktest 自检通过，UI 未人工点击 |
| 淋浴 / 立体库 / RJW | 📄 占位 | `hygiene` / `storage` / `rjw`，`OnDefsLoaded` 只写日志 + 检测来源 Mod |
| 实机 | ✅ quicktest | `-Deploy` + `-quicktest` 通过；未人工点 Debug 菜单/设置页 |
| 来源映射 | ✅ | [docs/参考源码映射.md](docs/参考源码映射.md) |

> 0.0.2 不改变游戏平衡；它验证「框架复制 → 改名 → 模块注册 → 构建/校验」链路，并同步 0.0.2 的 P0 修复；为后续移植 DBH / StackXXL / DevMenuChinese / RJW 的代码做准备。

---

## 1. 目标与边界

一句话：**用一套共享内核（模块注册、设置、日志、水网、UI、本地化）承载六套可独立交付的子系统，先把淋浴跑通，再逐个加立体库、农业、蓝图、智能机器人和汉化。**

- **框架优先**：模块之间有明确边界，公共能力下沉到 `Core/`；新增第七套系统时只加一个模块目录，不改其他模块。
- **模块独立可玩**：每个模块有自己的 Defs、研究、建筑、设置和验收；做完一个发一个版本，不把六个半成品捆在一起发。
- **数据驱动**：建筑、配方、研究、文本尽量放 XML；代码只做 XML 表达不了的行为。
- **原版优先**：不重写健康、需求、存储、建造的底层系统；能用原版机制就用原版机制，必要时才 Harmony。
- **可卸载**：模块以目录为单位，删掉对应 `Source/Modules/<Name>` + `Defs/<Name>` 即可从工程里移除；玩家侧不提供运行时卸载（Def 加载后无法真正卸载，见 §3.4）。

**范围之外**：新种族/新派系/新地图、联机同步、Combat Extended 兼容、独立科技树大修、替换原版核心系统、内置美术资源（首版用占位色块 + 简单几何图形）。

---

## 2. 框架要求（强制）

**本工程必须基于 [`sources/mod-template`](../mod-template/README.md) 生成，不允许从零手写工程骨架，也不允许直接修改模板本身。**

这里的「自己的 Mod 框架」= **在 mod-template 之上建立的模块化内核**（`Core/` + `Modules/`），不是另起一套加载器。模板已经跑通「加载 → Def → Patch → Harmony → GameComponent → Settings → 构建/校验/打包」整条链路，并在 1.6.4871 实机验证过；本工程只做改名、加模块、替换演示内容。

### 2.1 生成方式

`new-mod.ps1` 会拒绝已存在的目标目录，而本 README 已经在 `sources\ColonyKit\` 里，所以先输出到临时目录再合入。**2026-10-25 已按此执行**，命令保留用于复现/重生成：

```powershell
$workspace = 'E:\RimWorld-WorkSpace'
# 放在工作区根下，new-mod.ps1 才能按 ..\..\ 找到 .tools\dotnet 并完成首次编译
$staging   = Join-Path $workspace '.scaffold\ColonyKit'
Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue

Push-Location "$workspace\sources\mod-template"
.\new-mod.ps1 -Name ColonyKit -PackageId local.colonykit `
	-DisplayName 'Colony Kit' -Destination $staging
Pop-Location

# 生成物覆盖到工程目录：
#   /XF README.md        保留本文件（模板 README 是模板说明，不适用）
#   /XD docs ...         保留本工程的编号文档 + 设计文档
#   /XD Source\obj ...   不把 new-mod.ps1 首次编译产生的中间文件带进来
robocopy $staging "$workspace\sources\ColonyKit" /E /XF README.md `
	/XD "$staging\docs" "$staging\Source\obj" "$staging\Source\bin" `
	"$staging\Assemblies" "$staging\dist" /NFL /NDL /NJH /NJS /NP
Remove-Item $staging -Recurse -Force
```

生成后必须核对：`Source\ColonyKit.csproj` 的 `AssemblyName` 是 `ColonyKit`、`About\About.xml` 的 `packageId` 是 `local.colonykit`、源码/脚本里的 `ModTemplate` 已替换干净：

```powershell
Get-ChildItem -Recurse -File -Include *.cs, *.csproj, *.xml, *.ps1 |
	Select-String -Pattern 'ModTemplate'
# 应无输出（本 README 不参与替换，也不在扫描范围）
```

### 2.2 必须保留

| 路径 | 用途 |
|---|---|
| `build.ps1` / `verify.ps1` / `package.ps1` | 编译、静态校验、打包；模板已验证可用，不改流程 |
| `Source/Core/ColonyKitMod.cs` | `Mod` 入口 + 设置页入口 |
| `Source/Core/ColonyKitSettings.cs` | `ModSettings` + `Scribe` 持久化根 |
| `Source/Core/SettingsUI.cs` | 设置页绘制（按模块分节） |
| `Source/Core/ModLog.cs` | 统一 `[ColonyKit][<Module>]` 日志前缀 |
| `Source/Core/Bootstrap.cs` | `[StaticConstructorOnStartup]` + `PatchAll`，保留并改写 |
| `About/About.xml` | Mod 元数据 |
| `Languages/{English,ChineseSimplified}/{Keyed,DefInjected}` | 双语骨架 |
| `docs/ARCHITECTURE.md` | 改写成**本工程**的模块契约与生命周期说明 |

### 2.3 必须删除的演示内容

| 文件 | 原因 |
|---|---|
| `Defs/DemoConfig.xml` | 演示 Def，换成真正的模块 Def |
| `Source/DemoConfigDef.cs` | 演示 Def 子类 |
| `Source/ColonyKitDefOf.cs`（生成的 `ModTemplateDefOf`） | 引用演示 Def；删除后新建 `Core/DefOf/CKDefOf.cs` 与各模块 DefOf |
| `Source/DemoGameComponent.cs` | 演示用每存档计数；改成本工程的 `ColonyKitGameComponent`（模块生命周期 + 蓝图库等） |
| `Source/Patch_Pawn_SpawnSetup.cs` | 演示用 Harmony Postfix；删除后按模块新增 |
| `Bootstrap.cs` 里的演示初始化两行 | 同上 |
| `Patches/OptionalCompatibility.xml` | 改为本工程的 `MayRequire` 兼容补丁 |

`docs/ARCHITECTURE.md` 里的示例名（`DemoConfigDef`、`DemoGameComponent` 等）必须同步替换，不能留模板口径。

### 2.4 硬性约束

1. **程序集名全局唯一**：`ColonyKit.dll`；类型全名都带 `ColonyKit` 命名空间。
2. **Harmony ID = `packageId`**（`local.colonykit`），只在 `Bootstrap` 里 `PatchAll` 一次。
3. **`Mod` 构造函数里不碰 `DefDatabase` / `DefOf`**：Def 还没解析；需要 Def 的逻辑放 `[StaticConstructorOnStartup]`。
4. **Def XML 根元素是类型全名**：自定义 Def 用 `<ColonyKit....>`；标准 Def 用 `ThingDef` / `HediffDef` / `NeedDef` 等。
5. **`defName` 统一 `CK_` 前缀**（`CK_Shower`、`CK_VerticalWarehouse`、`CK_RobotHauler`），研究/配方/念头同理；发布后不改名（存档契约）。
6. **`Scribe` key 只增不改名**：统一 `ck_<module>_<field>`；删除字段保留 `[Obsolete]` 或迁移。
7. **XML / `.cs` 用 TAB 缩进、UTF-8**；注释按模板 `docs/ARCHITECTURE.md` §9。
8. **日志只走 `ModLog`**，统一前缀，方便在 `Player.log` 里按模块筛。
9. **`Assemblies/` 只放运行期 DLL**；`Source/`、`obj/`、`bin/`、`dist/` 不进包（模板 `.gitignore` 已覆盖）。
10. **不引游戏安装目录里的 `Assembly-CSharp.dll`**：一律 `Krafs.Rimworld.Ref`。
11. **0 错误 0 警告才提交**；提交前跑 `verify.ps1` 与 `package.ps1`。

---

## 3. 框架架构

### 3.1 分层

```
┌─────────────────────────────────────────────────────────────┐
│ 内容层：Defs / Languages / Patches（按模块分目录）            │
│   Defs/{Core,Hygiene,Storage,Agriculture,Blueprint,Robotics}  │
│   Languages/{English,ChineseSimplified}/{Keyed,DefInjected}  │
└─────────────────────────────────────────────────────────────┘
                              ↓ 读取
┌─────────────────────────────────────────────────────────────┐
│ 模块层：Source/Modules/<Name>                                │
│   Hygiene     卫生需求、淋浴、热水                            │
│   Storage     立体库                                         │
│   Agriculture 肥力网格、灌溉、堆肥                            │
│   Blueprint   蓝图库、捕获、放置                              │
│   Robotics    智能机器人、能量、充电、休眠                    │
│   Localization 文本规范、校验、排版（代码很少）               │
└─────────────────────────────────────────────────────────────┘
                              ↓ 只依赖
┌─────────────────────────────────────────────────────────────┐
│ 内核层：Source/Core                                          │
│   模块注册 / 生命周期 / 设置 / 日志 / 共享 UI / 水网服务      │
└─────────────────────────────────────────────────────────────┘
```

依赖方向单向：`Modules → Core`，`Core` 不引用任何模块；模块之间不直接引用，需要协作时走 `Core` 的服务接口（目前只有水网）。

### 3.2 模块契约

```csharp
public interface IColonyKitModule
{
	string Id { get; }                  // Hygiene / Storage / ...
	string DisplayNameKey { get; }
	string DescriptionKey { get; }
	int InitOrder { get; }              // 小者先初始化
	bool Locked { get; }                // core / diagnostics
	void OnDefsLoaded();                // [StaticConstructorOnStartup] 之后，可安全读 Def
	void OnGameLoaded(Game game);       // 读档/新游戏后，注册地图组件、恢复状态
	void ResetRuntimeState();           // 清静态缓存（禁用模块也会调）
}
```

- `ModuleRegistry.DiscoverFromAssembly` 扫描程序集里所有 `IColonyKitModule` 实现并注册；`Bootstrap` 不再硬编码模块列表。单个模块抛异常只记 error，不拖垮其他模块。
- `SettingsController` 自动发现 `SettingsPage` 子类，按 `Order` 排序、`Id` 去重；新增设置页不改控制器。
- 模块设置实现 `IColonyKitModuleSettings`，通过 `ColonyKitSettings.GetModuleSettings<T>()` 取/建，持久化在 `moduleSettings` 列表；新增模块设置不改设置根字段。
- `ColonyKitGameComponent` 在游戏加载后统一调用 `OnGameLoaded()`，并持有蓝图库等全局状态。
- Harmony 补丁**按模块分文件**、按模块命名（`Patch_<Target>_<Module>`）；补丁内部第一行判断模块是否启用（见 §3.4）。
- 每个模块一个 `DefOf` 类（`HygieneDefOf`、`StorageDefOf`、`RoboticsDefOf`…），不要把所有字段堆进一个巨型类。

### 3.3 共享服务：水网

淋浴和农业都要用水，所以水网下沉到 `Core/Water/`，两个模块只实现「消费者」：

```
水泵（耗电） → 管道 → 水箱（容量） → 消费者（淋浴/喷灌）
```

- `MapComponent_WaterNetwork` + `WaterNet`：按网格连通的管网，缓存总容量/存量/产出/消耗。
- `CompWaterPipe` / `CompWaterPump` / `CompWaterTank` / `CompWaterConsumer`。
- 存档只存「每个网的存量 + 建筑状态」，读档后按建筑重建拓扑。
- 单位统一用升（`L`），内部 `float`，UI 取整；断水时消费者停机并给出可读原因。

> 备选简化方案：不做管网，每台设备自带水箱 + 运水补给 Job。M1-0 先做技术选型，默认按完整管网推进；若工期超预算再切换，并同步改 M2/M4 的依赖。

### 3.4 模块启用/停用

Def 一旦加载就无法在运行时真正卸载，所以**不向玩家承诺「关掉模块 = 内容消失」**。本工程的模块开关只做**软停用**：

- 关掉模块后：它的 Harmony 补丁不生效、Gizmo/设计器/自动 Job 不再出现、设置页对应分节折叠；
- 已存在于存档里的建筑/Hediff/物品仍然存在，但不报错、不继续产生效果；
- 要彻底移除模块：开发期删 `Source/Modules/<Name>` + `Defs/<Name>` 后重编，不保证旧存档干净。

这条限制写进设置页 tooltip 和 `docs/ARCHITECTURE.md`，避免玩家误以为能热插拔。

### 3.5 性能约束

- 水网：只在建筑增删/状态变化时重建；稳态按 250 tick 结算一次，不做每 tick 全图扫描。
- 肥力网格：只处理脏格（dirty queue），渲染用 `SectionLayer`，不每帧重算全图。
- 立体库：事件驱动（入库/出库/取货），不做每 tick 遍历库存。
- 蓝图：只在捕获/放置时计算，不常驻 Tick。
- 机器人：能量结算按 250 tick 批量处理；充电/休眠用 Job 与建筑事件驱动，只对低电量机器人唤醒 `JobGiver`，不每 tick 遍历全图机器人。
- 所有模块的 `GameComponentTick` 只做 `if` 判断，重活放 `GameComponentUpdate` 或自己的计时器。

### 3.6 目标目录

```
sources/ColonyKit/
├─ README.md                          ← 本文件
├─ About/About.xml
├─ Defs/
│  ├─ Core/                           ← 共享研究页、水网建筑
│  ├─ Hygiene/
│  ├─ Storage/
│  ├─ Agriculture/
│  ├─ Blueprint/
│  └─ Robotics/
├─ Patches/
│  ├─ Compatibility/                  ← MayRequire 补丁（DBH/StackXXL/RJW）
│  └─ Needs/                          ← 需求注入（ShouldHaveNeed Postfix，卫生/机器人能量）
├─ Languages/
│  ├─ English/{Keyed,DefInjected}
│  ├─ ChineseSimplified/{Keyed,DefInjected}
│  └─ ChineseTraditional/{Keyed,DefInjected}   ← M7-4 可选
├─ Translations/DevMenuLabels.xml       ← 开发者菜单翻译表（0.0.2 已用）
├─ Source/
│  ├─ ColonyKit.csproj
│  ├─ Core/
│  │  ├─ ColonyKitMod.cs / ColonyKitSettings.cs / SettingsUI.cs / ModLog.cs / Bootstrap.cs
│  │  ├─ ColonyKitGameComponent.cs
│  │  ├─ Modules/{IColonyModule.cs, ModuleRegistry.cs}
│  │  ├─ Water/{MapComponent_WaterNetwork.cs, WaterNet.cs, Comps/...}
│  │  ├─ DefOf/CKDefOf.cs
│  │  └─ UI/{Widgets.cs, Dialog_ModuleInfo.cs}
│  ├─ Modules/
│  │  ├─ Hygiene/{HygieneModule.cs, Need_Hygiene.cs, Building_Shower.cs, JobDriver_TakeShower.cs, ...}
│  │  ├─ Storage/{StorageModule.cs, Building_VerticalWarehouse.cs, CompWarehouse.cs, ...}
│  │  ├─ Agriculture/{AgricultureModule.cs, MapComponent_FertilityGrid.cs, CompSprinkler.cs, ...}
│  │  ├─ Blueprint/{BlueprintModule.cs, BlueprintRecord.cs, Designator_BlueprintCapture.cs, ...}
│  │  ├─ Robotics/{RoboticsModule.cs, Need_RobotEnergy.cs, Building_ChargingPod.cs, JobGiver_SelfCharge.cs, ...}
│  │  └─ Localization/                ← 代码很少，主要是 Keyed/DefInjected 与脚本
│  ├─ Compat/{DbhCompat.cs, StackXxlCompat.cs}
│  └─ Debug/{DebugActions_ColonyKit.cs}
├─ docs/
│  ├─ 0000-索引.md                    ← 文档总入口（模块索引 + 功能页）
│  ├─ 1000-淋浴索引.md / 1001–1004-淋浴-*.md
│  ├─ 2000-立体库索引.md / 2001–2005-立体库-*.md
│  ├─ 3000-农业索引.md / 3001–3004-农业-*.md
│  ├─ 4000-蓝图索引.md / 4001–4004-蓝图-*.md
│  ├─ 5000-汉化索引.md / 5001–5004-汉化-*.md
│  ├─ 6000-机器人索引.md / 6001–6006-机器人-*.md
│  ├─ （设计文档）框架内核.md / 设置系统.md / 水网服务.md / Index.md
│  ├─ （设计文档）淋浴模块.md / 立体库模块.md / 农业模块.md
│  ├─ （设计文档）蓝图模块.md / 智能机器人模块.md / 本地化.md
│  ├─ 构建与发布.md / 调查指南.md / 参考源码映射.md
│  ├─ ARCHITECTURE.md / balance.md（待建）/ l10n.md（待建）
│  └─ decisions/（Index.md + ADR）   ← 技术选型记录（水网、存储内核、机器人 Pawn、蓝图格式）
├─ build.ps1 / verify.ps1 / package.ps1
└─ dist/                              ← 打包产物，不提交
```

> **0.0.2 实际骨架**：上面的目录是目标形态；当前 `Source/` 仍是 mod-template 的 `Core/Modules`、`Settings/`、`UI/`、`TechTree/` 等，新增了 `Source/Modules/ReferenceModules.cs` 与 `Source/Modules/Localization/`。功能模块落地时再按目标目录迁移。

### 3.7 常用命令

```powershell
cd E:\RimWorld-WorkSpace\sources\ColonyKit

.\build.ps1              # Release → Assemblies\ColonyKit.dll
.\build.ps1 -Clean       # 清 obj / bin / Assemblies
.\build.ps1 -Deploy      # 部署到 Mods\ColonyKit（先退出游戏）
.\verify.ps1             # packageId / XML / 补丁 xpath / 程序集静态检查
.\package.ps1            # verify + build + dist\ColonyKit-<version>.zip
```

`build.ps1` 默认 `ModsPath` 是 `E:\SteamLibrary\steamapps\common\RimWorld\Mods`，与本机安装路径一致。

---

## 4. 子系统范围（v0.1 草案）

> 六个模块都是**独立可交付**的：先做 MVP，跑通再扩。每节列出「MVP / 后续 / 不做 / 关键难点」，任务清单见 §5。

### 4.1 淋浴（Hygiene）

**MVP**

- `Need_Hygiene`（0–1）：缓慢下降，低于阈值触发「肮脏」念头；洗澡恢复，洗净后清空小人携带污渍（`pawn.filth.carriedFilth.Clear()`，DBH 的 `JobDriver_takeShower` 已有先例）。
- 需求注入：`Pawn_NeedsTracker.ShouldHaveNeed` Postfix（参考 DBH `HarmonyPatches.cs`），只给能洗澡的人形加。
- `CK_Shower`（1×1，贴墙或独立均可）：需要水网供水 + 用电加热；`JobDriver_TakeShower` 预约、脱战、洗澡、消耗水、恢复需求。
- 热水：`CK_WaterHeater` 给管网加热水；没有热水/加热器关闭时洗冷水澡，给心情 debuff。
- 念头：洁净、肮脏、冷水澡、今天洗过澡。
- 研究「卫生设施」+ 建造材料 + 占位贴图。
- Alert：断水、断电、淋浴被占用。

**后续**：洗手台、浴缸、病人擦洗、公共浴室、水质/污水。

**不做**：厕所/排泄系统（与 DBH 重叠且工作量更大）。

**关键难点**：需求注入只加一次、不重复加；洗澡 Job 的中断/预约/多人排队；与 DBH 的 hygiene 需求共存策略（M2-7）。

### 4.2 立体库（Storage）

**MVP**

- `CK_VerticalWarehouse`（3×3，耗电）：内部 `ThingOwner` 容量默认 24 堆（研究后可升级），带过滤/优先级/禁用清单。
- 入库：`WorkGiver_HaulToWarehouse` + `JobDriver_StoreInWarehouse`，小人把货送到「入库口」，物品被吸入库内（不占地图格）。
- 出库：库面板选「取出」→ 生成 `JobDriver_WithdrawFromWarehouse` 把物品放到「出库口」或目标库存区；支持整堆/按数量。
- UI：`ITab` 显示库存、过滤、优先级、填充度；Gizmo 提供「清空 / 禁止入库 / 升级」。
- 容量分级：T1 24 堆 → T2 48 堆 → T3 96 堆（研究 + 材料升级）。
- 存档：`ThingOwner` + 建筑状态，读档保持内容与过滤。

**后续**：传送带/机械臂自动搬运、多库联动、温控/防虫、货架层数可视化。

**不做**：跨地图物流、轨道交易、自动合成。

**关键难点（最高风险）**：**存储内核选型**——内部 `ThingOwner` 的物品不在地图 `ListerThings` 里，原版账单/搬运/搜索可能看不见。M3-0 必须先做技术验证，三选一：

| 方案 | 做法 | 代价 |
|---|---|---|
| A | `ThingOwner` + 自定义入库/出库 Job + 自定义「取货请求」 | 原版账单看不见库存，需要出库接口 |
| B | 继承 `Building_Storage` + 自定义 `SlotGroup`，每格多堆 | 要改堆叠/搬运逻辑，和 Stack XXL 交互复杂 |
| C | A + 定向 Harmony（`ListerThings` / `GenClosest`）让原版能搜到 | 侵入性最强，但兼容性最好 |

M3-0 写 `docs/decisions/storage-kernel.md`，记录原型、性能、兼容性与最终选择。

### 4.3 农业（Agriculture）

**MVP**

- `MapComponent_FertilityGrid`：每格肥力（0–2），叠加在原地形 `fertility` 上；存档、分区渲染（`SectionLayer`）、脏格更新。
- 肥力生效：Postfix `FertilityGrid.CalculateFertilityAt`（DBH 的 `HarmonyPatches_Fertilizer.cs` 是现成参考），让原版种植/生长直接吃到网格值。
- 灌溉：`CK_Sprinkler` 消耗水网 + 电力，半径 5 格提升肥力到目标值；断水自动停。
- 堆肥：`CK_Composter` 消耗有机废料（食物残渣/尸体，可配置）产出 `CK_Fertilizer`；`Designator_ApplyFertilizer` + Job 把肥料施到地块，短期大幅提升肥力并随时间衰减。
- 研究「灌溉」「堆肥」+ 建造材料 + 占位贴图。
- UI：肥力 overlay（开关、图例）、喷灌机 Gizmo（半径/水量/状态）。

**后续**：轮作、温室玻璃、土壤改良剂、作物品种、牲畜粪肥。

**不做**：完整气象/季节模拟、新作物大礼包。

**关键难点**：`CalculateFertilityAt` 调用频繁，必须缓存/脏格；施肥网格的存档与渲染；与 DBH 灌溉/肥力系统重叠（M4-6）。

### 4.4 蓝图（Blueprint）

**MVP**

- `BlueprintRecord`：名称、尺寸、版本、条目列表（`defName` / `stuffDefName` / `rot` / 相对坐标 / 可选地形）。
- 捕获：`Designator_BlueprintCapture` 拉框，只收**玩家建造的建筑**（跳过蓝图/框架/物品），可选「包含地形」。
- 库：`BlueprintLibrary` 每存档一份（由 `ColonyKitGameComponent` 持有）；`Dialog_BlueprintLibrary` 列表、重命名、删除、材料统计、放置。
- 放置：`Designator_BlueprintPlace` 带幽灵预览，Q/E 旋转、镜像、逐格红绿校验（地形/占用/水域/研究/材料）。
- 施工：逐条 `GenConstruct.PlaceBlueprintForBuild`，保留材质与朝向；支持一次放置多个蓝图，取消可回滚。
- 材料清单：放置前显示总需求 vs 地图现有，缺料高亮。
- 存档：蓝图库随存档保存，schema 带版本号；缺 Mod/缺 Def 时跳过并给出警告清单。

**后续**：导入/导出 JSON、跨存档共享库、包含地形/物品、复制已有建筑、创意工坊分享。

**不做**：跨地图远程施工、自动重建/修复。

**关键难点**：多格建筑的原点/朝向；Modded Def 缺失；蓝图库的存档体积；与「计划」设计器（vanilla plan）的交互。

### 4.5 智能机器人（Robotics）

**MVP**

- 五种型号，全部是玩家阵营的机器人，靠研究 + 制造获得：

| 型号 | 默认工作 | 关键能力 | 备注 |
|---|---|---|---|
| 搬运机器人 | `Haul` | 高携带、移速略高 | 不做清洁/做饭 |
| 清洁机器人 | `Clean` | 清洁速度加成 | 不做搬运 |
| 做饭机器人 | `Cook` | 烹饪技能高 | 不做种植/屠宰（首版） |
| 医疗机器人 | `Doctor` | 医疗技能高，可照顾病人/手术（后续） | 不做搬运 |
| 战斗机器人 | 无工作，只能 draft | 射击/近战、可穿护甲 | 不自动去充电 |

- **能量**：`Need_RobotEnergy`（0–1）。满电可行动 **12 小时**；耗尽 → 停机（downed），不是死亡。电量条按 25% / 5% 分档。
- **充电**：`CK_ChargingPod`（1×2，耗电，一台机器人独占）。
	- 普通对接：**8 小时**充满，功耗 **2000W**；不视为睡眠，可被玩家中断。
	- 休眠快充：**4 小时**充满，功耗 **4000W**；视为睡眠，机器人完全无防备。
- **不需要睡觉**：移除 `Food` / `Rest` / `Joy` / `Mood` / `Beauty` / `Comfort` / `Social` 需求；没有床、没有睡眠念头、没有心情崩溃。
- **休眠即睡眠**：休眠快充时 `PawnUtility.IsSleeping` 返回 true，参与任何「是否在睡觉」的判断；但机器人本身没有 `Need_Rest`，不靠睡觉恢复。
- **自主充电**：电量 < 25% 自动找充电桩；< 5% 强制休眠快充；0% 原地停机，可被搬运到充电桩后恢复。战斗机器人被 draft 时不自动去充电，低电强制解除 draft。
- **断电/EMP**：断电即停止充电；EMP 眩晕并额外掉电（可选，M6-7）。
- **获得**：研究「机器人学」→ 机械加工台/精密装配台制造；高成本（塑钢/零部件/高级零部件/AI 核心可选）。
- **UI**：能量条、Gizmo「去充电 / 休眠快充 / 唤醒」、低电/断电/停机 Alert。

**默认数值口径（草案）**：满电 12 小时 = 30000 tick，活跃耗电 `1/30000` 每 tick；休眠 4 小时 = 10000 tick，充电 `1/10000` 每 tick；普通对接 8 小时 = 20000 tick，充电 `1/20000` 每 tick。角色差异（战斗耗电更快等）只放在 M6-8 调平衡，不改这组默认口径。

**后续**：机器人控制台/数量上限、维修 Job、自动防御/巡逻、升级模块、运输机器人、Biotech 机械师联动。

**不做**：机器人繁殖/社交/恋爱、完整 AI 人格、机器人派系、Biotech 深度集成（本机未装 DLC）。

**关键难点**：

- **机器人 Pawn 架构**：humanlike 能直接复用工作/技能/医疗系统，但需要摘掉一整套需求并处理社交/生育；mech-like 更干净，但工作系统和需求要自己接。M6-0 必须先做技术验证。
- **工作类型固定 vs 可调**：全能让其他殖民者失业；固定又限制玩家。默认固定，设置里允许解锁。
- **充电语义**：「普通对接」和「休眠快充」两种状态、排队/预约、0% 停机后的恢复。
- **「休眠 = 睡眠」**：只影响 `IsSleeping` 判断，不能真的给机器人加 `Need_Rest`。
- **电力冲击**：单桩 4000W，多机器人同时快充会拖垮电网；这是刻意设计的约束，需要 Alert 和平衡。

### 4.6 汉化集合（Localization）

**MVP**

- 全部模块的 `Keyed` + `DefInjected`：英文为基准，简中完整；术语表 + 文风指南（`docs/l10n.md`）。
- 键规范：`ColonyKit.<Module>.<Area>.<Name>`；DefInjected 按 Def 类型分目录，标准类型用 `ThingDef` / `ResearchProjectDef` 等。
- `tools/check-l10n.ps1`：扫描源码/XML 里用到的 Keyed、Defs 里的 `defName` 与各语言文件比对，输出缺失/多余；接入 `verify.ps1`，缺简中直接失败。
- 中文排版：验证长句换行、标点挤压、字体回退；只在实测有问题时加最小补丁，并记录到 `docs/l10n.md`。
- 设置页/研究页/提示/拒绝原因全部可翻译，不出现 `Keyed.xxx`。

**后续**：繁中（M7-4）、其他 Mod 的汉化补丁集（M7-7，需确认范围）、术语一致性自动检查、游戏内翻译缺失检查工具。

**不做**：机器翻译流水线、字体替换大修（原版中文字体够用）。

**关键难点**：DefInjected 的目录名/键格式；六套模块文本量大，容易漏；「汉化功能集合体」如果包含对其他 Mod 的汉化，范围会迅速膨胀（先确认再做）。

---

## 5. 任务清单

> 勾选状态是本文件的进度源。每完成一项就把 `[ ]` 改成 `[x]`，并补一句实际结果（偏差、踩坑、数值调整）。
> 依赖关系：`M0 → M1 → {M2, M4}`；`M3`、`M5`、`M6` 在 M0 后即可并行；`M7` 贯穿各模块；`M8` 最后。

### M0 框架与脚手架

- [x] **M0-1** 用 `new-mod.ps1` 生成骨架到工作区临时目录并合入本工程，保留本 README。
	- 完成：2026-10-25；`new-mod.ps1` 首次编译通过，模板未被修改。
- [x] **M0-2** 删除 §2.3 的演示文件；保留 `ColonyKitMod` / `Settings` / `SettingsUI` / `ModLog` / `Bootstrap` 骨架。
	- 完成：删除 `DemoConfig` / `ColonyKitDefOf` / `Patch_Pawn_SpawnSetup` / `OptionalCompatibility`；`DemoGameComponent` 改为 `ColonyKitGameComponent`。
- [x] **M0-3** 改写 `About/About.xml`：`name=殖民地套件`、`author`、`packageId=local.colonykit`、`supportedVersions=1.6`、`modVersion=0.0.1`、`description`、Harmony 依赖 + `loadAfter`。
	- 完成：0.0.1；描述改为框架 + 四个参考模块占位。
- [x] **M0-4** 模块内核：`IColonyKitModule`、`ModuleRegistry`、`ColonyKitGameComponent`；`ModuleRegistry.DiscoverFromAssembly` 自动发现 10 个模块，`OnDefsLoaded` / `OnGameLoaded` 有日志。
	- 完成：模板 6 个（core/pawn-menu/notifications/build-control/tech-tree/diagnostics）+ `hygiene` / `storage` / `localization` / `rjw` 四个；新增模块不再改 `Bootstrap`。
- [x] **M0-5** 设置壳：沿用模板的多页签设置框架（模块/常规/日志/通知/汉化/关于），`SettingsController` 自动发现页面；模块开关由 `ModuleSettings` 持久化，模块细分设置走 `IColonyKitModuleSettings` + `GetModuleSettings<T>()`。
	- 偏差：没有按 `ck_<module>_*` 新建嵌套设置类；0.0.1 用 `moduleSettings` 通用列表，设置页自动发现。
- [x] **M0-6** 文档：改写 `docs/ARCHITECTURE.md`（模块契约、生命周期、软停用限制、水网/存储/肥力/机器人约束）；维护 `docs/0000-索引.md` 与各模块索引/功能页；更新 [`sources/Index.md`](../Index.md) 增加本工程条目与构建命令。
	- 完成：新增 `docs/ARCHITECTURE.md`（0.0.1 实际骨架/生命周期）；`sources/Index.md` 已加 ColonyKit 段；`docs/0000-索引.md` 与 27 个功能页已维护。
- [x] **M0-7** `.\build.ps1` 0 错误 0 警告 + `.\verify.ps1` 0 问题 + `-Deploy` 后 `Player.log` 出现 `[ColonyKit] loaded; modules=10`，无红字。
	- 完成：2026-10-25；`-Deploy` + `-quicktest` 通过，日志 `[ColonyKit][Bootstrap] loaded; modules=10`；未人工点 Debug 菜单/设置页。

**M0 完成标准**：空框架能加载，10 个模块注册成功（模板 6 + 参考 4），设置页可见，不改玩法。

### M1 核心服务：水网

- [ ] **M1-0** 技术选型：完整管网 vs 每设备自带水箱；写 `docs/decisions/water-network.md`（连接规则、容量、断水行为、存档、性能），定稿后再写代码。
- [ ] **M1-1** `MapComponent_WaterNetwork` + `WaterNet`：网格连通、容量/存量/产出/消耗、地图加载后重建、随存档保存。
- [ ] **M1-2** Comps：`CompWaterPipe`、`CompWaterPump`（耗电产水）、`CompWaterTank`（容量）、`CompWaterConsumer`（需求/优先级/停机原因）。
- [ ] **M1-3** Defs：`CK_WaterPipe`、`CK_WaterPump`、`CK_WaterTank`（大/小）、研究「给排水」、建造材料与占位贴图。
- [ ] **M1-4** 分配与断水：按优先级分配、存量不足时停机、恢复后自动重启；所有拒绝原因可读。
- [ ] **M1-5** UI：选中管道/水箱显示网络统计；水网 overlay（可选）；断水 Alert。
- [ ] **M1-6** 调试动作：给网注水/抽干/生成测试台；`[ColonyKit][Water]` 日志。
- [ ] **M1-7** 测试：泵→箱→消费者；断开管道；存读档；10 个水箱/100 段管道下的性能与稳态开销。

**M1 完成标准**：一台测试消费者只有在水网有水时运行，UI 数字与存档一致。

### M2 淋浴（Hygiene）

- [ ] **M2-0** API 验证：`Pawn_NeedsTracker.ShouldHaveNeed` 注入、`pawn.filth.carriedFilth` 清理、洗澡 Job 的预约/中断；结论写进 `docs/decisions/hygiene.md`。
- [ ] **M2-1** `Need_Hygiene` + `NeedDef` + 衰减/恢复 + 存档；只给人形加，不重复加。
- [ ] **M2-2** `CK_Shower` + `JobDef` / `JobDriver_TakeShower`：预约、到场、洗澡、消耗水、恢复需求、清污渍。
- [ ] **M2-3** 热水：`CK_WaterHeater` + 管网温度/加热逻辑；冷水澡心情 debuff；断电降级。
- [ ] **M2-4** 念头/记忆：洁净、肮脏、冷水澡、今天洗过澡；阈值与持续时间进 `docs/balance.md`。
- [ ] **M2-5** Defs：建筑、研究、配方、材料、占位贴图；建造菜单归类。
- [ ] **M2-6** UI：需求条、Gizmo「去洗澡」、断水/断电/占用 Alert、拒绝原因。
- [ ] **M2-7** DBH 共存策略：检测 `dubwise.dubsbadhygiene`；三选一（共存/软停用本模块需求/声明不兼容），写进设置与文档。
- [ ] **M2-8** 测试：一天不洗澡→变脏→洗澡恢复；断水/断电/多人排队；存读档；与 DBH 同开冒烟。

**M2 完成标准**：正常玩家能在游戏内完成「变脏 → 去洗澡 → 恢复 → 心情变化」闭环。

### M3 立体库（Storage）

- [ ] **M3-0** 存储内核验证（最高优先）：按 §4.2 的 A/B/C 各做最小原型，验证原版账单/搬运/搜索能否看到库存、Stack XXL 下的堆叠、存读档与性能；写 `docs/decisions/storage-kernel.md` 定稿。
- [ ] **M3-1** `CK_VerticalWarehouse` + `CompWarehouse`：`ThingOwner`、容量、过滤/优先级、禁用清单、填充度、存读档。
- [ ] **M3-2** 入库：`WorkGiver_HaulToWarehouse` + `JobDriver_StoreInWarehouse`（入库口、堆叠合并、拒收原因）。
- [ ] **M3-3** 出库：`JobDriver_WithdrawFromWarehouse` + UI「取出」（整堆/数量、出库口、目标库存区）。
- [ ] **M3-4** 原版账单集成：验证账单是否能拿到库内物品；不能则设计「仓库链接/取货请求」最小方案，并记录限制。
- [ ] **M3-5** UI：`ITab`（库存列表、过滤、优先级、排序）、Gizmo（清空/禁止入库/升级）、填充度贴图。
- [ ] **M3-6** 电力与升级：待机/工作功耗，T1/T2/T3 容量与研究、升级材料。
- [ ] **M3-7** 兼容：Stack XXL（堆叠上限）、LWM Deep Storage 类 Mod（检测并提示）、DBH 仓库类建筑不冲突。
- [ ] **M3-8** 性能与存档：1 万件物品的入库/出库/读档；不做每 tick 遍历；物品腐烂/防火策略写进文档。
- [ ] **M3-9** 测试：入库→过滤→出库→再入库；断电；拆除建筑时物品掉落；存读档；升级。

**M3 完成标准**：不依赖原版库存区，玩家能用立体库完成「存 → 找 → 取」闭环。

### M4 农业（Agriculture）

- [ ] **M4-0** 肥力 API 验证：`FertilityGrid.CalculateFertilityAt` Postfix、`Plant` 生长速率、`SectionLayer` 渲染；写 `docs/decisions/fertility-grid.md`。
- [ ] **M4-1** `MapComponent_FertilityGrid`：网格、脏格更新、存档、分区渲染、地图加载重建。
- [ ] **M4-2** `CK_Sprinkler` + `CompSprinkler`：耗水/耗电、半径、目标肥力、断水停机、Gizmo。
- [ ] **M4-3** 堆肥与施肥：`CK_Composter`（有机废料→`CK_Fertilizer`）、`Designator_ApplyFertilizer` + Job、肥力短期提升与衰减。
- [ ] **M4-4** 生长联动：确认网格肥力真正影响生长/产量；必要时补最小 Patch；不吃掉原版地形肥力。
- [ ] **M4-5** UI/研究：肥力 overlay（开关/图例）、研究「灌溉」「堆肥」、建造材料、占位贴图。
- [ ] **M4-6** DBH 共存：DBH 已有灌溉/肥力网格；检测后默认停用本模块的肥力网格或喷灌，避免双重加成；写设置项。
- [ ] **M4-7** 平衡：每格耗水、肥力上限/衰减、堆肥转化率、喷灌覆盖与电力；写进 `docs/balance.md`。
- [ ] **M4-8** 测试：喷灌提升产量、断水停产、施肥衰减、存读档、100 台喷灌机的性能。

**M4 完成标准**：同一块地，灌溉/施肥前后产量有可测差异，且不改坏原版种植。

### M5 蓝图（Blueprint）

- [ ] **M5-0** 数据 schema：`BlueprintRecord` / `BlueprintEntry`，版本号、坐标系、`defName`/`stuff`/`rot`、可选地形；存档体积上限；写 `docs/decisions/blueprint-format.md`。
- [ ] **M5-1** 捕获：`Designator_BlueprintCapture` 拉框，只收玩家建造的建筑，跳过蓝图/框架/物品；可选包含地形。
- [ ] **M5-2** 库：`ColonyKitGameComponent` 持有 `BlueprintLibrary`；`Dialog_BlueprintLibrary` 列表、重命名、删除、材料统计、放置。
- [ ] **M5-3** 放置预览：`Designator_BlueprintPlace`，Q/E 旋转、镜像、逐格红绿校验（地形/占用/水域/研究/材料）。
- [ ] **M5-4** 施工：逐条 `GenConstruct.PlaceBlueprintForBuild`，保留材质/朝向；多蓝图排队；取消回滚。
- [ ] **M5-5** 材料清单：总需求 vs 地图现有，缺料高亮，可一键跳转到缺料提示。
- [ ] **M5-6** 边界：多格建筑原点、旋转/镜像、缺 Def（跳过+警告清单）、地形、版本迁移。
- [ ] **M5-7** 导入/导出（可选，排到 M5 之后）：`Config/ColonyKit/Blueprints/*.json`，带 schema 版本与校验。
- [ ] **M5-8** 测试：捕获 10×10 基地→存读档→放置重建；缺 Mod 场景；旋转/镜像；拆掉原建筑后重建。

**M5 完成标准**：能保存一套基地布局，在新地图/新存档按材料重建，缺料/缺 Mod 有可读反馈。

### M6 智能机器人（Robotics）

- [ ] **M6-0** Pawn 架构验证（最高优先）：对比「humanlike 机器人」与「mech-like 机器人」两条路线，验证工作系统、需求注入、技能、生成（无背景故事/特质）、社交/生育关闭、存档；写 `docs/decisions/robot-pawn.md` 定稿。
- [ ] **M6-1** `Need_RobotEnergy`：满电 12h（`1/30000` 每 tick）→ 休眠 4h（`1/10000`）→ 普通对接 8h（`1/20000`）；0% 停机、充电恢复、存档；能量条与分档。
- [ ] **M6-2** 机器人种族 + 5 个 `PawnKindDef`：体型/血量/护甲/技能/携带；移除 Food/Rest/Joy/Mood/Beauty/Comfort/Social 需求；不睡眠、不社交、不生育；生成无背景故事/特质。
- [ ] **M6-3** 工作类型：搬运/清洁/做饭/医疗/战斗的默认优先级与 WorkGiver 兼容；医疗机器人可照顾病人（手术是否开放由 M6-0 验证后定）；战斗机器人只能 draft；工作锁默认开、设置可解锁。
- [ ] **M6-4** `CK_ChargingPod`：1×2、独占、普通 8h/2000W、休眠 4h/4000W；断电停充、排队/预约、研究/材料/贴图。
- [ ] **M6-5** 自主充电：`JobGiver_SelfCharge` + `JobDriver_Charge` / `JobDriver_DormantCharge`；阈值 25%/5%/0%；draft 不自动充、低电强制解除；中断/唤醒/重新排队。
- [ ] **M6-6** 休眠语义：`PawnUtility.IsSleeping` Patch、`Need_Rest` 不注入、充电桩上的 laydown/wake、休眠中被中断与存档恢复。
- [ ] **M6-7** 战斗与故障：draft/装备/射击、EMP 眩晕/掉电（可选）、断电与 0% 停机、停机后可被搬运到充电桩。
- [ ] **M6-8** 获得与平衡：研究「机器人学」、制造配方/成本（塑钢/零部件/高级零部件/AI 核心）、数量上限/控制台（待确认）、维修/残骸、角色耗电差异；写 `docs/balance.md`。
- [ ] **M6-9** UI/提示：能量条、Gizmo（去充电/休眠快充/唤醒）、低电/断电/停机 Alert、机器人型号与当前状态。
- [ ] **M6-10** 测试：满电 12h→没电停机；休眠 4h 充满；普通 8h；5 种型号各干各的活；断电/EMP；存读档；多机器人排队；电网 4000W 冲击。

**M6 完成标准**：五种机器人能按型号工作，能量/充电/休眠闭环，满电 12h、休眠 4h、0% 停机可恢复，且不睡觉也能正常存档读档。

### M7 汉化集合（Localization）

- [x] **M7-0** 移植 DevMenuChinese 的开发者菜单汉化：外置 `Translations/DevMenuLabels.xml`、`DebugActionNode.LabelNow` Postfix、Debug 窗口 tooltip Transpiler、翻译方案设置页/编辑窗口、原 Mod 冲突检测。
	- 完成：2026-10-25；build 0/0、verify 0/0、zip 9 文件；`-quicktest` 自检 882/882 词条、tooltip/品质翻译返回中文。
- [ ] **M7-1** 规范：`docs/l10n.md` 写键命名、文件拆分、术语表、文风、DefInjected 目录规则；英文为唯一基准。
- [ ] **M7-2** 英文 Keyed/DefInjected 全量：6 个模块、所有建筑/研究/念头/提示/设置/拒绝原因。
- [ ] **M7-3** 简中 Keyed/DefInjected 全量；术语与文风按 `docs/l10n.md`，不出现 `Keyed.xxx`。
- [ ] **M7-4** 繁中（可选）：确认是否随首版发；不确认就留骨架，不进验收。
- [ ] **M7-5** `tools/check-l10n.ps1`：Keyed 使用 vs 定义、Def `defName` vs DefInjected、各语言对齐；接入 `verify.ps1`，缺简中失败。
- [ ] **M7-6** 中文排版：长句换行、标点、字体回退、数字/单位；实测有问题才加最小补丁，记录到 `docs/l10n.md`。
- [ ] **M7-7** 其他 Mod 汉化补丁集（可选）：确认是否要覆盖 DBH/RJW 等；确认后单列任务与目录，不混进本 Mod 正文。
- [ ] **M7-8** 测试：英文/简中各跑一遍，设置页、研究页、蓝图库、仓库 ITab、机器人能量条/充电 Gizmo 无缺键/截断。

**M7 完成标准**：简中/英文全覆盖，`check-l10n` 0 缺失，UI 无 `Keyed.xxx`。

### M8 集成、测试与发布

- [ ] **M8-1** 模块矩阵：每个模块单独启用、两两组合、全开，各跑一局；崩溃/红字记录并修复。
- [ ] **M8-2** 存档兼容：新增/软停用模块、旧版本读档、缺 Def、蓝图库 schema 迁移、机器人能量/停机状态。
- [ ] **M8-3** 性能：10 个殖民者 + 5 个机器人 + 100 栋建筑 + 3 倍速 10 分钟；水网/肥力/仓库/充电无 tick 尖峰。
- [ ] **M8-4** 兼容冒烟：DBH、Stack XXL、RJW（按实际安装）；有冲突给出明确提示，不静默坏档。
- [ ] **M8-5** `.\verify.ps1` 0 问题；`.\package.ps1` 出 zip；解压第一层是 `About/`、`Defs/`、`Assemblies/`。
- [ ] **M8-6** 文档：README 状态、`ARCHITECTURE.md`、`balance.md`、`l10n.md`、`decisions/*`、`docs/0000-索引.md` 与各功能页与实际代码一致。
- [ ] **M8-7** 版本与提交：`About.xml` 的 `modVersion`、变更记录、`git status` 只含本次相关文件。
- [ ] **M8-8** 干净环境验收：只开 Core + Harmony + 本 Mod，新开一局，六模块按 README 流程走一遍，`Player.log` 无 error/warning。

**M8 完成标准**：zip 在干净环境可加载，六模块按文档可玩，日志干净。

### 构想池（未排期）

> 记录「已确认想要、但还没排期」的构想；不占 M 编号、不改交付节奏。立项前先写 spike/ADR，通过 Go 条件才升级为正式里程碑。

- [ ] **G1 地下室（7000 候选）**：在地面殖民地建地下室入口，通往一张独立口袋地图；地下与地面经桥接共用**水、电**，**不受天气影响**，温度**近乎恒定**。

  **硬需求（2026-10-25 指定）**

  1. **水电接入地面**：地下室的电力、用水来自地面网络，经入口/管线井桥接；不允许凭空产电/产水；地面断水、断电时地下室同步停机；桥接有损耗和流量上限（数值进 `balance.md`）。
  2. **不受天气影响**：地下室不出现降雨、降雪、雷暴、大风等天气；季节、寒潮、热浪不改变地下室环境；全图厚岩顶。
  3. **近乎恒温**：默认 15°C，可设 10–20°C；无人为热源/火灾时，连续 10 天（含寒潮/热浪）保持目标 ±2°C。

  **MVP 范围**

  - `CK_BasementHatch`（3×3，每图一个）：建在地面，首次进入生成 50×50 口袋地图；`CK_BasementExit` 返回地面。
  - `CK_Basement` 地图：`isUnderground` + `RoofRockThick` + `Underground` biome/weather + `pocketMapProperties.temperature=15`。
  - `CK_UtilityBridge`（2×2）：地面 ↔ 地下电力/水网桥；可开关、显示流量；只搬运，不生产。
  - 恒温保障：`MapComponent_BasementClimate` 或 `MapTemperature.OutdoorTemp` Postfix；屏蔽地下室上的温度/天气 `GameCondition`。
  - 存读档、设置（目标温度/桥接损耗/地图尺寸）、Alert（断水/断电/温度异常）。

  **非目标（MVP 不做）**

  - 上层楼层/多层；跨图自动搬运、账单、种植、洗澡——工作仍按当前地图扫描，地下室以手动搬运 + 显式桥接为主。
  - 空投/袭击直达地下室、天窗、地下水培自动化、Gravship 多层。
  - Odyssey 硬依赖；如果自研贴图无法在无 DLC 下跑通，再评估声明 Odyssey 依赖。

  **验收**

  - [ ] 地面电网断电 → 地下室同步断电；地面水网断水 → 地下室同步断水；桥接不产生净能量/净水量。
  - [ ] 寒潮/热浪/风雨 10 天，地下室无人为热源时 15±2°C，无降雨/降雪/雷暴。
  - [ ] 存读档后地图、入口/出口、桥接、温度状态一致。
  - [ ] 50×50 地图 + 10 殖民者 3 倍速无 tick 尖峰；`Player.log` 无 error。
  - [ ] 无 DLC 环境可运行（或明确记录必须 Odyssey）。

  **前置**

  - 先做 [`docs/decisions/basement.md`](docs/decisions/basement.md) 的 2–3 天 spike；Go 条件不满足就停在构想池，不升级为 M9。
  - 水电桥接依赖 M1 水网；建议排在 M2/M3 之后。

  **参考**：[`docs/decisions/basement.md`](docs/decisions/basement.md)；Private Basement（口袋地图 + 水电桥/天窗）、MultiFloors / Z-Levels（跨图）、As above, So below 2（同图分层备选）。

---

## 6. 交付节奏

| 版本 | 内容 | 出口条件 |
|---|---|---|
| `0.1.0` | M0 框架 + M1 水网 + M2 淋浴 | 淋浴闭环可玩，存档正常 |
| `0.2.0` | M3 立体库 | 存/取闭环，原版账单限制写清 |
| `0.3.0` | M4 农业 | 灌溉/施肥对产量有可测影响 |
| `0.4.0` | M5 蓝图 | 捕获→存读档→重建闭环 |
| `0.5.0` | M6 智能机器人 | 12h/4h/充电/停机闭环 |
| `0.6.0` | M7 汉化 | 简中/英文全覆盖，校验脚本接入 |
| `1.0.0` | M8 集成与打磨 | 模块矩阵、性能、兼容、打包全过 |

**粗估**（单人、含调试与返工）：M0 3–5 人日，M1 4–6，M2 3–5，M3 6–10，M4 5–8，M5 4–7，M6 6–10，M7 3–5，M8 4–6，合计约 **38–62 人日**。立体库、农业和机器人的估算最不确定，M3-0/M4-0/M6-0 的 spike 结果会直接改工期。

---

## 7. 全局验收标准（Definition of Done）

1. `.\build.ps1` **0 错误 0 警告**；`.\verify.ps1` **0 问题**；`check-l10n` **0 缺失**。
2. 六个模块按 §4 的 MVP 全部可玩；每个模块单独启用时不依赖其他模块。
3. 机器人：满电 12h 行动、休眠 4h 充满、0% 停机可恢复、没有睡眠需求；存读档后能量/停机/充电状态一致。
4. 存档：装/卸模块、升级版本、缺 Def 时读档不崩；水网/肥力/仓库/蓝图/机器人数据可恢复。
5. 语言：英文 + 简中 Keyed/DefInjected 完整，UI 无 `Keyed.xxx`。
6. 兼容：不装 DBH/Stack XXL/RJW 时零报错；检测到重叠功能时有明确提示或软停用。
7. 包结构：`dist/ColonyKit-<version>.zip` 解开第一层就是 Mod 根，无 `Source/`、`obj/`、`bin/`。
8. 文档：`ARCHITECTURE.md`、`balance.md`、`l10n.md`、`decisions/*` 与代码一致。

---

## 8. 主要风险

| 风险 | 影响 | 应对 |
|---|---|---|
| 机器人 Pawn 架构选错 | 工作/需求/社交/生成全套返工 | M6-0 先做两条路线原型，再定稿 |
| 立体库存储内核选错 | 返工最大，可能推翻整个模块 | M3-0 先做 A/B/C 原型，再定稿；不先写 UI |
| 水网复杂度超预期 | 拖住淋浴和农业 | M1-0 选型；备选「自带水箱 + 运水」简化方案 |
| DBH 功能重叠 | 双重需求/双重灌溉，玩家困惑 | 检测 + 软停用 + 明确提示；必要时声明不兼容 |
| 肥力网格性能 | `CalculateFertilityAt` 调用频繁导致卡顿 | 脏格 + 缓存 + SectionLayer；M4-0 先压测 |
| 机器人电力冲击 | 4000W 充电桩拖垮电网或机器人过强 | 独占充电桩、成本/研究门槛、数量上限（待确认）、M6-8 平衡 |
| 机器人变成全能工 | 其他殖民者失业，平衡崩坏 | 型号固定工作 + 工作锁；M6-3/M6-8 调 |
| 蓝图跨 Mod 存档 | 缺 Def 读档报错/丢建筑 | schema 带版本；缺 Def 跳过 + 清单；不静默 |
| 六系统同时开工 | 六个半成品，长期不可玩 | 按 §6 一个模块一个版本，做完再开下一个 |
| 汉化范围蔓延 | 「汉化集合」变成给所有 Mod 打补丁 | M7-7 先确认；本 Mod 文本优先 |
| 1.6 小版本 API 变动 | Harmony 目标签名变化 | 补丁集中管理；升级前用新 `Assembly-CSharp` 核对 |

---

## 9. 待确认

> 未确认前按「当前草案」执行；确认后先改本文件，再动代码。

1. **名称**：`ColonyKit` / `local.colonykit` / 「殖民地套件」是否采用？`packageId` 发布后不能改，先定死。
2. **模块开关**：是否接受 §3.4 的「软停用」语义（Def 仍加载、行为停用）？还是要做「删目录重编」的硬移除？
3. **淋浴 vs DBH**：DBH 已有完整卫生/淋浴系统。本 Mod 是**替代**（声明不兼容）、**共存**（不做 hygiene 需求，只做清洁/心情），还是**检测到就软停用**？当前草案默认「独立实现 + 检测到 DBH 时软停用本模块需求」。
4. **立体库**：容量/自动化要到什么程度？原版账单是否必须能直接看到库内物品（决定 M3-0 选 A 还是 C）？
5. **农业**：MVP 只要灌溉 + 堆肥，还是必须带轮作/温室/新作物？与 DBH 灌溉重叠时谁优先？
6. **蓝图**：库是**每存档**还是**全局共享**？是否需要导入/导出 JSON？是否包含地形/物品？
7. **汉化集合**：只做本 Mod 的简中/繁中，还是也要给 DBH/RJW 等出汉化补丁？后者会显著扩大范围。
8. **发布渠道**：Steam 创意工坊还是仅本地/私人分发？影响 `PublishedFileId.txt` 与打包流程。
9. **美术**：首版占位色块可接受，还是需要先定图标/建筑贴图风格？
10. **机器人数量**：是否需要控制台/带宽上限，还是只靠成本和电力约束？上限会改变 M6-8 的配方与 UI。
11. **机器人工作**：默认锁定型号工作，还是允许玩家在 Work 页自由改？当前草案是「默认锁定 + 设置解锁」。
12. **充电数值**：普通对接 8h/2000W、休眠 4h/4000W 是否符合预期？用户只给了「休眠 4h 充满」和「满电 12h 行动」，普通充电时长是草案。
13. **战斗机器人**：只能手动 draft，还是要自动防御/巡逻？
14. **机器人损坏**：受伤后靠充电自修、需要维修 Job，还是直接留残骸可拆？是否要 Biotech 机械师联动（本机未装 DLC，默认不做）？
15. **地下室（G1 构想）**：已登记硬需求——地面水电接入、不受天气、近乎恒温；**未排期**。走口袋地图 + 水电桥（需先 spike）还是同图方案，待 spike 后定。见 [`docs/decisions/basement.md`](docs/decisions/basement.md)。

---

## 10. 相关文档

| 文档 | 内容 |
|---|---|
| [`../mod-template/README.md`](../mod-template/README.md) | 模板能力、目录、脚本用法 |
| [`docs/Index.md`](docs/Index.md) | 本工程功能文档索引（框架内核 / 设置 / 六模块 / 构建发布） |
| [`docs/参考源码映射.md`](docs/参考源码映射.md) | DBH / StackXXL / DevMenuChinese / RJW 四个来源的逐文件映射、移植顺序与风险 |
| [`docs/框架升级-0.0.2.md`](docs/框架升级-0.0.2.md) | 0.0.2 框架同步记录：已移植 / 跳过 / 下次流程 |
| [`docs/调查指南.md`](docs/调查指南.md) | 5 分钟定位：日志 tag、稳定标识、症状检查、数据位置 |
| [`docs/decisions/Index.md`](docs/decisions/Index.md) | 决策记录模板与六个待定选型（水网/存储/肥力/蓝图/机器人/淋浴） |
| [`../mod-template/docs/ARCHITECTURE.md`](../mod-template/docs/ARCHITECTURE.md) | 分层、生命周期、扩展点、常见失败模式（本工程架构文档的底稿） |
| [`../../docs/开发Mod须知.md`](../../docs/开发Mod须知.md) | 六条硬性规定、发布前自检 |
| [`../../docs/Mod加载机制.md`](../../docs/Mod加载机制.md) | 加载顺序、程序集/XML/Patch 流水线 |
| [`../dbh/1.6/Source/DubsBadHygiene/JobDriver_takeShower.cs`](../dbh/1.6/Source/DubsBadHygiene/JobDriver_takeShower.cs) | 淋浴 Job 参考（清污渍、需求恢复） |
| [`../dbh/1.6/Source/DubsBadHygiene.Patches/HarmonyPatches_Fertilizer.cs`](../dbh/1.6/Source/DubsBadHygiene.Patches/HarmonyPatches_Fertilizer.cs) | 肥力网格 Patch 参考（`FertilityGrid.CalculateFertilityAt`） |
| [`../dbh/1.6/Source/DubsBadHygiene/MapComponent_Hygiene.cs`](../dbh/1.6/Source/DubsBadHygiene/MapComponent_Hygiene.cs) | 污水/肥力网格与渲染参考 |
| [`../Index.md`](../Index.md) | sources 下各工程源码导航（M0-6 要更新） |
| [`docs/0000-索引.md`](docs/0000-索引.md) | 文档总入口：模块编号、模块索引、各功能使用页 |
| [`../../AGENTS.md`](../../AGENTS.md) | 工作区路径、提交与部署约定 |

---

## 11. 变更记录

| 日期 | 内容 |
|---|---|
| 2026-10-25 | 初始立项 `CustomEnhance`（自定义强化），仅有规划稿。 |
| 2026-10-25 | 目标调整为 `ColonyKit`：mod-template 模块化框架 + 淋浴/立体库/农业/蓝图/汉化五系统；目录更名，任务清单重写；骨架仍未生成。 |
| 2026-10-25 | 追加智能机器人（M6）：搬运/清洁/做饭/医疗/战斗五型号，能量 12h、休眠 4h 快充、4000W 充电桩；汉化与集成顺延为 M7/M8，任务 68 → 79。 |
| 2026-10-25 | 创建 `docs/` 编号文档体系：`0000-索引.md` + 6 个模块索引 + 26 个功能页（1001–1004、2001–2005、3001–3004、4001–4004、5001–5003、6001–6006），代码实现后逐页回填。 |
| 2026-10-25 | 26 个功能页统一为 11 节结构：TOC快速预览 / 简介 / 描述 / 绘制调用逻辑图 / 函数清单 / 函数或功能完成情况 / 涉及文件 / 所含功能 / 如何使用 / 常见问题 / 相关文档；模块索引保持导航结构。 |
| 2026-10-25 | 从 mod-template 0.0.1 复制生成 ColonyKit 0.0.1 骨架；注册 `hygiene` / `storage` / `localization` / `rjw` 四个参考模块占位；清理演示内容；build 0/0、verify 0/0；来源映射见 `docs/参考源码映射.md`。 |
| 2026-10-25 | 移植 `localization` 开发者菜单汉化：`Translations/DevMenuLabels.xml`（约 72 KB）、`DebugActionNode.LabelNow` Postfix、Debug 窗口 tooltip Transpiler、翻译方案设置页/编辑窗口；检测原 DevMenuChinese 冲突；build 0/0、verify 0/0、zip 9 文件；quicktest 自检 882/882 + tooltip/品质翻译通过。 |
| 2026-10-25 | 新增 `5004-汉化-开发者菜单汉化.md`，功能页总数 27；`0000-索引.md` 与 `5000-汉化索引.md` 同步。 |
| 2026-10-25 | 任务清单补 `M7-0`（开发者菜单汉化移植）并勾选，任务总数 79 → 80。 |
| 2026-10-25 | 扩展点自动发现：`ModuleRegistry.DiscoverFromAssembly` 替代 `Bootstrap` 硬编码；`SettingsController` 自动发现 `SettingsPage`；模块设置走 `IColonyKitModuleSettings` + `moduleSettings` 列表，不再改 `ColonyKitSettings` 字段。quicktest：`modules=10 (discovered=10)`、页面 6 个、设置 XML 正确写入 `Class=...LocalizationSettings`。 |
| 2026-10-25 | 同步 mod-template 0.0.2：build.ps1 改为 Rebuild/警告失败/`-MIR`，补 Notification/Dropdown/MainButton 守卫；版本升 0.0.2；模板 demo 抽屉/示例研究不引入，详见 `docs/框架升级-0.0.2.md`。 |
