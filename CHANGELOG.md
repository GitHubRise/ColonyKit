# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 的基本格式，版本号使用 `0.0.x` 预发布序列。

## [Unreleased]

### Changed

- 设置页签为 **模块 / 立体库 / 蓝图 / 常规设置 / 关于** 5 个；移除模板「常规 / 日志 / 通知」三个测试页和「汉化」页。
- 翻译方案并入常规设置；方案编辑窗口新增「翻译表配置」：显示翻译表路径/词条统计，并提供重新加载按钮。
- 新增「常规设置」页（位于「关于」前）：Mod 设置语言默认 **简体中文**，可切 Auto/English/简体中文；日志级别、间隔、前缀、测试日志和诊断入口移入本页。
- `GeneralSettings.language` 默认值与 `ModLoc` 回退语言改为 `ChineseSimplified`。

### Added

- **卫生建造菜单**：新增 `CK_Hygiene` 建筑师分类（默认排在「家具」与「电力」之间），淋浴、浴缸、洗手池、马桶、污水管、化粪池与水网设施全部归入「卫生」。
- **1×1 壁挂淋浴器**：`CK_Shower` 从 2×2 改为 1×1，使用 `Placeworker_AttachedToWall`，必须贴墙安装并自动朝墙旋转；贴图沿用 DBH simple shower 四向图。
- **水网建筑**：1×1 水管（`isEdifice=false`、`Conduits` 层、`Standable`，可铺墙下，`Default1D` 拖拽布线）、1×1 水箱（2000 L）、2×2 巨型水箱（8000 L）、1×1 水泵（1500 L/天、250 W、可故障）、1×1 水井（2000 L/天）、1×1 热水器（250 W）。水管/水箱/水泵/水井贴图取自本机 Dubs Bad Hygiene。
- **蓝图上帝模式直建**：开启 `DebugSettings.godMode` 后，放置蓝图库中的蓝图会直接 `ThingMaker.MakeThing` + `GenSpawn.Spawn` 成完工建筑，不再生成施工蓝图；消息改用「上帝模式：已直接建成」。
- **水网修正**：产水改为每网每 tick 推入一次（避免多水箱成倍产水）；水泵必须有同网水井才产水；水箱存量/水质仍随存档保存。
- **蓝图模块（M5 首版）**：底部「蓝图」页；拉框捕获已建好的玩家建筑（跳过蓝图/框架/矿物/物品），幽灵预览 + Q/E 旋转 + B 镜像 + 逐条校验；放置后设计器保留，可连续复刻多份；三档材料策略（原材质优先/就地取材/最便宜），缺料红色提示与替换说明；每存档蓝图库；「另存为模板」写到 `Config/ColonyKit/Blueprints/*.xml` 并跨存档载入；设置页可调策略/尺寸/条目/库上限。不含地形与物品，实机验收待做。
- 内置 RJW 1.6 本体：源码迁入 `Source/Rjw` 独立工程，Defs/Patches/Languages/Textures/Sounds 合并进 ColonyKit；`RJW.dll` 随 ColonyKit 一起构建、打包、部署。外部 `rim.job.world` 与内置版不兼容。
- 迁移时修正 RJW 对自身包名的引用（`rim.job.world` → `local.colonykit` 回退），避免 `First` 静态构造空引用与配方过滤遗漏。
- 立体库互联：8 向相邻的仓库组成网络，任一仓库接履带即可让全网络自动入/出库；无履带网络时建筑上方闪烁红色履带警告图标。
- 立体库改为 **3×3**：北/东/南/西四个履带端口；自动入/出库必须至少连接一条履带；履带出口可直接把物品送入仓库。
- 立体库新增检索/滚动库存面板、底部「立体库」管理标签页（全仓库检索/选中），以及 `ResourceCounter` 补丁：库内资源计入原版底部资源列表。
- 立体库默认过滤不允许腐烂物品/尸体/堆块；履带方向箭头只在有物品时显示，空闲时隐藏。`n- 立体库新增「自动输出到履带」Gizmo（默认开，每 250 tick 输出第一堆）；研究菜单新增「立体仓库」页签与 T1/T2/T3 链式科技：×10/×30/×60 堆叠、二级解锁核心、三级解锁履带并开放 -20～30°C 温控。
- 立体库加入 `CompTempControl` 温控：默认 4°C，像原版空调一样用温度 Gizmo 调节，按房间温度工作。
- 1×1 `CK_ConveyorBelt` 双向履带：15 钢/1 零部件，支持正向/反向、30 tick/格移动，和 1×1 双向履带/转向盘 相邻履带互通。
- 履带跑马灯：`DrawAt` 只在履带上有物品时叠加移动箭头，沿出口方向循环；1×1 双向/转向盘共用基类。`n- 1×1 履带从仓库端口或上游履带推断流向；端口朝外=输出、朝仓库=输入，选中时显示方向箭头。
- `CK_ConveyorHub` 1×1 转向盘实体：256×256 四向/转盘贴图、可旋转、可通行、`maxItemsInCell=1`、30 tick/格实际移动、建筑朝向/反向 Gizmo 控制出口；`tools/generate_conveyor_texture.py` 可重新生成。
- 履带与转向盘加入 `minifiedDef`：可拆除，也可像家具一样打包搬走并重新安装。
- 立体库双向自动 Job：`WorkGiver_HaulToWarehouse`（库存区满/无库存区或仓库优先级更高时复用原版 `HaulToContainer`）+ `WorkGiver_WithdrawFromWarehouse`/`JobDriver_WithdrawFromWarehouse`（送到更高优先级库存区）；仓库默认 **Normal** 优先级。
- `CK_VerticalWarehouse` 接入原版 `IStoreSettingsParent`/`ITab_Storage`：过滤、优先级、搜索；`Accepts` 按过滤器拒收。
- 「关于」页移除科技树分组、提示和打开按钮，只保留版本/包名/说明。
- `storage` 第一阶段：全局堆叠规则（1–20×，默认 10×）、原值缓存/恢复、Stack XXL 共存检测、设置页（开关/倍率/逐 Def 日志/重新应用）。
- 163 个普通可堆叠 `ThingDef` 参与倍率；武器/衣物/尸体/最小化建筑/科技义体排除。
- `CK_VerticalWarehouse` 3×3 内部库存原型：24 堆容量、250W 电力、`ThingOwner` 存读档、库存 ITab、收集附近/全部取出 Gizmo、拆除掉落；占位贴图。
- 设置页新增「立体库容量」滑条（1–200，默认 24）：所有已建和新建仓库的堆数上限立即生效；调低不会丢失已存物品。
- 功能页 `docs/2006-立体库-堆叠规则.md`、`docs/2007-立体库-内部库存原型.md`；`2000` 索引与 `参考源码映射` 同步。
- G2 空中力量构想（未排期，8000 候选）：`docs/decisions/air-power.md`；六类单位/克制三角、13 个现有 Mod 调研、原版 1.6 飞行 API、三条主路线 + 世界地图备选与 spike 验收；README §5/§9、`0000` 索引、decisions 索引同步。

### Fixed

- 失禁（`Need_Bladder.crapPants`）不再引用 DBH 的 `SoiledSelf`：改用新 Def `CK_SoiledSelf`（-8 心情、持续 1 天、最多叠 3 层），并保留 DBH 同名 Def 作为回退；此前 Def 不存在，`ThoughtDef.Named` 返回 null，`TryGainMemory` 每次失禁都抛 NRE，`CurLevel` 无法复位，导致该 Pawn 每 150 tick 刷一次「Exception ticking …」。
- 卫生间心情（`SanitationUtil.ApplyBathroomThought`）：finish action 在设施已被拆除/搬运后仍会执行，`fixture.Map` 为 null 时 `GetRoom` 抛 NRE；现在先判 `Spawned`/`Map`。`Building_AssignableFixture.Occupied` 同样加 `Spawned` 保护。
- 膀胱枚举比较方向：本项目的 `BowelCategory` 是 Empty/needBathroom/Busting，与 DBH 的相反，移植时照抄了 `<=` 比较——卧床分支对「没尿意」的 Pawn 也触发、`ticksAtZero` 无条件累加导致 `givenUp` 永不复位；改为 `>=` 后与 DBH 一致。

### Validated

- quicktest（只开 Core + Harmony + ColonyKit）：`modules=10`；Blueprint 模块加载、库初始化正常。
- 临时实机探针：在地图空地生成 5×5 墙圈 + 中心 1×2 床，捕获 17 条 → 平放/旋转 90° 各生成 17 个原版 `Blueprint_Build`；旋转后床蓝图朝向为 East；模板保存/读取/删除通过。
- 未覆盖：10×10 基地、存读档、缺 Mod 场景、真实鼠标 UI 点击/拖框。

### Removed

- 移除底部「科技树」按钮与整套模板科技树：`MainButton_TechTree` Def/Worker、`Source/TechTree` 源码、`tech-tree` 模块、`GeneralSettings.showTechTreeButton`、科技树 Keyed/DefInjected 文案与 UI 自动化场景。
- 「关于」页不再列出科技树；模块数从 10 降为 9。

## [0.0.2] - 2026-10-25

同步 `mod-template` 0.0.2 的框架修复与构建改进；**不引入**模板的 demo 抽屉/示例研究内容。

### Changed

- `About.xml` / `ColonyKit.csproj` 版本升到 `0.0.2`。
- `build.ps1`：默认 `-t:Rebuild`、警告视为失败、新增 `-NoRebuild`、部署改 `/MIR`、结束重置 `LASTEXITCODE`、无 `.tools` 时回退 PATH 的 dotnet。
- 同步 0.0.2 的防御性修复：
  - `NotificationService.ThrottledMessage` 在 `Current.Game == null` 时不访问 `Find.TickManager`。
  - `SettingsWidgets.Dropdown` 对 `null` / 空 `options` 直接返回。
  - `MainButtonService.ApplyOrder` 对反射字段缺失/类型不符用 `ModLog.Once` 警告。
- 保留 ColonyKit 已有改动：模块/设置页/模块设置自动发现、开发者菜单汉化、`Translations` 打包。

### Skipped

- `ModDrawerWindow` / `MainButton_Demo` / 底部抽屉：模板演示交互，不属于 ColonyKit 内容。
- `Defs/Research/ModTemplate_ResearchExamples.xml` 与 15 个示例研究：模板演示内容，不引入。
- 0.0.2 的「研究树」玩家文案改名：ColonyKit 仍使用模板科技树演示，暂不跟改。
- `smoke-test.ps1` / GitHub Actions：ColonyKit 的冒烟流程后续单独补。

### Validated

- `build.ps1 -Clean`：0 error / 0 warning。
- `verify.ps1`：0 问题、0 警告。
- quicktest（只开 Core + Harmony + ColonyKit）：`modules=10 (discovered=10)`；翻译表 882/882；页面 6 个；设置 XML 写入并回读。

## [0.0.1] - 2026-10-25

首个可运行版本：从 `sources/mod-template` 0.0.1 **复制**出独立工程，保留框架能力，加入四个参考模块的占位。

### Added

- 从 mod-template 0.0.1 复制并改名为 `ColonyKit`（`packageId=local.colonykit`）。
- 框架内核：模块注册/生命周期、多页签设置、结构化日志、诊断报告、设置迁移。
- 四个参考模块骨架：`hygiene`（淋浴，参考 DBH）、`storage`（立体库/物品堆叠，参考 StackXXL）、`localization`（汉化，参考 DevMenuChinese）、`rjw`（RJW 集成，只做兼容）；其中 `localization` 已移植，其余三个仍为占位。
- `localization` 模块已移植开发者菜单汉化：外置 `Translations/DevMenuLabels.xml`、`DebugActionNode.LabelNow` Postfix、Debug 窗口 tooltip Transpiler、翻译方案设置页/编辑窗口；检测到原 `DevMenuChinese` 时仍加载完整表，两个补丁都运行（建议只保留一个）。
- `docs/参考源码映射.md`：逐文件记录四个来源可复用的部分、目标模块、移植顺序与风险。
- `docs/ARCHITECTURE.md`：0.0.1 实际骨架、生命周期、模块契约与软停用。

### Changed

- `About/About.xml` 改为「殖民地套件」，版本 `0.0.1`，描述当前框架与占位状态。
- 移除模板演示内容：`DemoConfig`、`ColonyKitDefOf`、`Patch_Pawn_SpawnSetup`、`Patches/OptionalCompatibility.xml`、`DemoGameComponent`（改为 `ColonyKitGameComponent`）。
- 设置页分类名改为「殖民地套件」。

### Validated

- `build.ps1 -Clean`：0 error / 0 warning。
- `verify.ps1`：0 问题、0 警告。
- `package.ps1`：`dist/ColonyKit-0.0.1.zip`，9 个文件，第一层 `About/Assemblies/Defs/Languages/Translations`。
- `-Deploy` + `-quicktest`（只开 Core + Harmony + 本 Mod）：`[ColonyKit][Bootstrap] loaded; modules=10`；`[ColonyKit][Localization] translation table loaded: 882 entries, 34 menus, 882 enabled`；自检 `tooltip="打开调试日志。" quality="良好"`。

### Known limitations

- `localization` 的翻译表/翻译函数已通过 quicktest 自检；**未人工打开 Debug 菜单并逐项点击**方案切换/设置页，UI 交互仍待验证。
- 其余三个模块仍只有 `OnDefsLoaded` 日志。
- 翻译表来自 `sources/devmenu-zh/Translations/DevMenuLabels.xml` 当前版本；后续新增词条要同步复制。
- 模板自带的 `pawn-menu` / `notifications` / `build-control` / `tech-tree` / `diagnostics` 模块保留，作为框架能力；后续按需裁剪。
- 未添加对 DBH / StackXXL / RJW 的硬依赖；移植对应模块时再决定依赖或复制。
