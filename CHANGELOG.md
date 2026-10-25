# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 的基本格式，版本号使用 `0.0.x` 预发布序列。

## [Unreleased]

### Changed

- 设置页签精简为 **模块 / 立体库 / 汉化 / 常规设置 / 关于** 5 个；移除模板「常规 / 日志 / 通知」三个测试页。
- 新增「常规设置」页（位于「关于」前）：Mod 设置语言默认 **简体中文**，可切 Auto/English/简体中文；日志级别、间隔、前缀、测试日志和诊断入口移入本页。
- `GeneralSettings.language` 默认值与 `ModLoc` 回退语言改为 `ChineseSimplified`。

### Added

- 立体库互联：8 向相邻的仓库组成网络，任一仓库接履带即可让全网络自动入/出库；无履带网络时建筑上方闪烁红色履带警告图标。
- 立体库改为 **3×3**：北/东/南/西四个履带端口；自动入/出库必须至少连接一条履带；履带出口可直接把物品送入仓库。
- 立体库新增检索/滚动库存面板、底部「立体库」管理标签页（全仓库检索/选中），以及 `ResourceCounter` 补丁：库内资源计入原版底部资源列表。
- 立体库默认过滤不允许腐烂物品/尸体/堆块；履带方向箭头只在有物品时显示，空闲时隐藏。
- 立体库加入 `CompTempControl` 温控：默认 4°C，像原版空调一样用温度 Gizmo 调节，按房间温度工作。
- 1×1 `CK_ConveyorBelt` 双向履带：15 钢/1 零部件，支持正向/反向、30 tick/格移动，和 1×1/2×2 相邻履带互通。
- 履带跑马灯：`DrawAt` 只在履带上有物品时叠加移动箭头，沿出口方向循环；2×2 与 1×1 共用基类。`n- 1×1 履带自动按相邻履带对齐主轴（横向朝东、纵向朝北），避免默认朝向下物品只走一格。
- `CK_ConveyorHub` 2×2 全向履带实体：256×256 四向/转盘贴图、可旋转、可通行、`maxItemsInCell=1`、30 tick/格实际移动、建筑朝向/反向 Gizmo 控制出口；`tools/generate_conveyor_texture.py` 可重新生成。
- 立体库双向自动 Job：`WorkGiver_HaulToWarehouse`（库存区满/无库存区或仓库优先级更高时复用原版 `HaulToContainer`）+ `WorkGiver_WithdrawFromWarehouse`/`JobDriver_WithdrawFromWarehouse`（送到更高优先级库存区）；仓库默认 **Normal** 优先级。
- `CK_VerticalWarehouse` 接入原版 `IStoreSettingsParent`/`ITab_Storage`：过滤、优先级、搜索；`Accepts` 按过滤器拒收。
- 「关于」页移除科技树分组、提示和打开按钮，只保留版本/包名/说明。
- `storage` 第一阶段：全局堆叠规则（1–20×，默认 10×）、原值缓存/恢复、Stack XXL 共存检测、设置页（开关/倍率/逐 Def 日志/重新应用）。
- 163 个普通可堆叠 `ThingDef` 参与倍率；武器/衣物/尸体/最小化建筑/科技义体排除。
- `CK_VerticalWarehouse` 3×3 内部库存原型：24 堆容量、250W 电力、`ThingOwner` 存读档、库存 ITab、收集附近/全部取出 Gizmo、拆除掉落；占位贴图。
- 功能页 `docs/2006-立体库-堆叠规则.md`、`docs/2007-立体库-内部库存原型.md`；`2000` 索引与 `参考源码映射` 同步。
- G2 空中力量构想（未排期，8000 候选）：`docs/decisions/air-power.md`；六类单位/克制三角、13 个现有 Mod 调研、原版 1.6 飞行 API、三条主路线 + 世界地图备选与 spike 验收；README §5/§9、`0000` 索引、decisions 索引同步。

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
