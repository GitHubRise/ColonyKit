# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 的基本格式，版本号使用 `0.0.x` 预发布序列。

## [Unreleased]

### Fixed

- **开发者菜单「半英半中」乱码**：翻译表中 204 条由逐词替换生成的条目（`绘制河流Debug`、`Material报告`、`名称FromRulepack`、`小人生成Histogram` 等）全部改写为通顺中文；`DevLabelTranslations` 删掉 `HumanizeDefName` 逐词硬拼逻辑，Def/SoundDef 没有译名时直接显示英文原名，不再拼接「中文·英文」的杂交结果。
- **开发者菜单物品名仍是英文**：尤兰（174 个）与龙族（27 个）两个没有中文包的种族 Mod 全量补齐中文词条（`menu="spawning"`），含服装、武器、建筑、食物、树、光效（Mote）、式神、包材等；另补萌螈飞花、古迹搜寻器、`FirefoamPopper`、`CloacalAnus/Penis/Vagina` 等杂项。词条同时按英文 label 与 defName 建立 key（共 342 个 key），列表显示 label 或 defName 都能命中。
- **Def 名称回退**：`Translate` 在 defName 回退到本地化 label 后，会再按该英文 label 查一次表，避免「列表显示 defName、表里只有中文词条」时整列英文。
- **音效/特效 Def 不再显示英文原名**：删除逐词硬拼后会暴露 30 个没有 `label` 的 `SoundDef`/`EffecterDef`（如 `Impact_YuranFlower_With_Slash`、`DRBowSound`），现补上中文（尤兰花斩命中特效、龙族弓弦声、萌螈剑气·超级等）。

### Changed

- **立体库堆叠分层**：一级研究只提供全局物品堆叠（固定 ×10，不再乘设置页倍率）；二级/三级的 ×3/×2 改为只放大立体库内部每堆，不再修改全局 `ThingDef.stackLimit`，普通架子/储存区/地面堆不受影响。库内每堆上限 = 当前全局上限 × 3（二级）/ × 6（三级）。
- **立体库设置页**：完成一级后隐藏堆叠倍率滑条并显示固定 ×10 提示；库面板/检视字符串显示当前库内堆叠倍率（模块关闭或 Stack XXL 共存时不显示）。
- **更新说明规范**：`About/About.xml` 描述新增「最近更新（YYYY-MM-DD）：」小节，每次玩家可见更新写 3–6 条；旧内容留在 CHANGELOG，下一次替换该小节。规范同步到 `AGENTS.md` 与 README §2.4。
- 设置页签为 **模块 / 卫生 / 立体库 / 蓝图 / 常规设置 / 关于** 6 个；移除模板「常规 / 日志 / 通知」三个测试页和「汉化」页。
- 翻译方案并入常规设置；方案编辑窗口新增「翻译表配置」：显示翻译表路径/词条统计，并提供重新加载按钮。
- 新增「常规设置」页（位于「关于」前）：Mod 设置语言默认 **简体中文**，可切 Auto/English/简体中文；日志级别、间隔、前缀、测试日志和诊断入口移入本页。
- `GeneralSettings.language` 默认值与 `ModLoc` 回退语言改为 `ChineseSimplified`。

### Added

- **立体库实时温度浮字**：`Building_VerticalWarehouse.DrawGUIOverlay` 在贴图中央圆环下方（约 0.27 格）显示当前房间/室外温度，随温度实时刷新；≤0°C 偏蓝、≥25°C 偏橙；常规/近景视角常驻，拉远后只在选中时显示，避免小图上文字互相压住。
- **厚墙加厚屋顶**：新增 `Patch_ThickRoof`。薄屋顶漏热（`ThinRoofEqualizationTempChangePerInterval`）在房间最薄外墙达到 `roofMinWallLayers`（默认 3，即“墙体大于 2”）时，按同一 `f(d)` 缩放；`d` 取所有墙边里最薄的一侧。无顶项与厚岩顶项不变；设置页新增「墙体够厚时加厚屋顶」开关与阈值滑条（2–8）。
- **多墙体保温模块**：新增 `Insulation` 模块与「保温」设置页。原版 `RoomTempTracker.WallEqualizationTempChangePerInterval` 只取房间外第 2 格，双层墙与空地同值；模块在 Harmony Postfix 里按房间四周的连续墙层数 d 乘保温系数 `f(d) = (1 + r) / (1 + r × d)`（f(1)=1，与原版一致）。默认 r=3：2 层约 57%、3 层 40%、4 层 31%；可调 r（0.5–10）、最大层数（1–10）与「自然岩层算不算墙」。屋顶、门、通风口仍按原版；复用 `RoomTempTracker.EqualizeCellsForReading` 的缓存外圈格采样（上限 256 格），逐方向只统计朝向房间外的边。
- **淋浴等待 Job**：新增 `CK_WaitForShower`；`JobGiver_HaveWash` 在没有空淋浴器/浴缸但有一台正在使用且供水正常的淋浴器时，让 Pawn 到门口等待，占用者结束后自动接手 `CK_TakeShower`。不是严格 FIFO，多个等待者会竞争同一台淋浴器。
- **洁具房间占用限制**：`Building_AssignableFixture` 新增 Gizmo「房间占用限制」；开启后房间里有其他 Pawn 时该洁具视为占用。`OccupancyCheck` 不再是死字段。
- **锅炉功率档位与恒温**：`CompWaterHeater` 增加低/标准/高三档（供热能力 125/250/375，耗电同比例），热水罐最低温 ≥0.98 时进入恒温待机、≤0.85 或任意罐 `LowCap` 时重启；`LowCap` 不再未使用。
- **洁具占用/排队 Alert**：`Alert_HygieneShowerBusy` 重命名为 `Alert_HygieneFixtureBusy`，扫描淋浴/浴缸/洗手池/马桶；占用 ≥1 小时或有人需要洗澡/上厕所但没有空位时提示。
- **内置蓝图「永恒温室」**：`Blueprints/永恒温室.xml`，37×38 占地、35×33 单室冷库；北墙 18 台空调（冷侧朝库内、热侧朝北侧露天排气带）、南侧双门气闸、6 组十字形屋顶支撑柱；花岗岩墙体，适合在厚岩顶下按「先深冷、再断电」的方式做长时间保冷测试。
- **调试动作「蓝图工具 → 全场空调设为 -273」**：`DebugActions_Blueprint`，一键把当前地图所有玩家空调的 `CompTempControl.TargetTemperature` 设为 -273°C，避免逐台点温度 Gizmo。
- **肠道感染**：新增 `CK_GutBug` Hediff（呕吐、膀胱消耗加速、约 1 天恢复、可治疗）；`GiveNastyGutBug` 不再空实现，卫生污染累计后会真正致病并发送负面消息。
- **淋浴后清理**：移植 `ShowerCleanup`，淋浴成功结束后清掉淋浴器周围 2.5 格的 RJW 精液污渍，并把小人自己房间最多 12 格污渍排成一条原版 `JobDefOf.Clean` 工作（`playerForced`）。
- **热水澡与如厕隐私**：`CK_ShowerHot` 现在会在热水淋浴完成后实际施加；新增 `CK_ToiletPrivacy` 念头，`ToiletPrivacyLOS` 按视线/情人/动物/机械体/囚犯规则判断，不再恒返回 false。
- **卫生设施 Alert**：新增 `Alert_HygieneFixture`，玩家淋浴器、浴缸、洗手池或马桶断水/堵塞/断电时提示，点击可跳到问题建筑。
- **洁具占用计时**：`occupiedSinceTick` 上移到 `Building_AssignableFixture`，淋浴/浴缸/马桶/洗手池共用并随存档保存。
- **DBH 式热水模型**：新增 `CompHeatStore` 热水罐（0–100% 归一化水温，`HeaterTemp` 随存档保存）；热水器/锅炉提供 `BoilerCapacity`，管网按 `HeatingCap = clamp01(BoilerCapacitySum / HeatStoreCapacitySum)` 决定目标水温；热水罐按 `RiseRate/FallRate` 升温/降温，洗澡时 `PullHotWater` 把 `heatPerTick × 热水用量倍率` 平均分摊到所有还有热水的罐。
- **热水罐建筑 `CK_HotWaterTank`**：1×1、需要接到水网/热水器，可扩充热水容量；热水器 `CK_DbhWaterHeater` 改为 250 供热能力并自带一个小型热水罐，旧存档加载时会自动补齐新组件。
- **热水用完会中途变冷**：淋浴每 tick 跟随 `PullHotWater` 结果更新 `cold`，浴缸同样；`CK.Hygiene.HotWaterTemp` 显示热水罐温度。
- **热水用量设置**：卫生设置页新增「热水消耗倍率」（0.10–2.00×），对应 DBH 的 `WaterHeatingRate`。
- **蓝图内置默认模板**：`Blueprints/*.xml` 随 Mod 发布 12 张默认方案（3 张 19×19 空白房间、普通双人大小卧室、极简单人/纯木单人/纯木双人小卧室、低版本永恒温室（41×41）、四人单间公共室（13×13）、一组电池（x10，10×3）、小型牧场（20x20，27×28））；`BlueprintTemplateStorage.LoadAll` 合并「玩家模板目录 + 内置目录」，按显示名排序，同名时玩家版本优先，内置模板只读（只能载入、不能删除），并在模板行显示「内置」标记；`build.ps1` / `package.ps1` 的运行期目录加入 `Blueprints`。
- **高铁（G3 构想，未排期）**：新增 [`docs/decisions/high-speed-rail.md`](docs/decisions/high-speed-rail.md)，登记「世界地图铁路网 + 车站/端点 + 商队列车化」方案：用原版 `RoadDef`/`OverlayRoad`/`WorldPathing` 复用寻路、速度、渲染与存档，先做 R0 铺轨/渲染 spike，再进 R1 车站/线路 MVP；含四种方案对比、数值草案、R0–R3 验收与风险。README §5/§9、0000 索引、decisions 索引同步。
- **卫生设置页**：新增「卫生」页签（位于「模块」与「立体库」之间），可调卫生/膀胱需求下降速度（0–5×）、洁具耗水倍率（0.25–4×）、脏水污染概率（0–300%）、DBH 策略（软停用/始终并存/完全禁用）与需求豁免（囚犯、访客、非人形、种族/体型/Hediff 名单）；策略或豁免变化后可用「应用到现有小人」立即重新评估需求。
- **卫生参数接入**：需求速率、耗水倍率、污染概率、DBH 策略与豁免全部从 `HygieneSettings` 读取；设置随 Mod 设置落盘，重置本页恢复默认。
- **智能机器人（M6 首版）**：新增「家政机器人」研究（前置机械加工）与「机器人」建造菜单；1×1 `CK_NannyBotStation` 建成后自动组装 `CK_RobotNanny`，损毁后 1 天冷却重装；机器人不吃饭不睡觉，自带 `CK_RobotEnergy`（满电约 12 小时、充电约 4 小时、25% 自动回桩、0% 停机但不死亡）；三项可开关家务：扫地（家区域内污渍）、清洁（家区域外污渍）、搬运（复用原版 `HaulToCell`，跳过尸体）；自研 ThinkTree/JobGiver/JobDriver，四向机器人贴图、充电桩与命令图标；中英 `Keyed`/`DefInjected`。
- 开发探针 `-colonykit-nannybot-test`：在 quicktest 新图上放桩、组装机器人，并生成污渍/钢铁堆验证扫地与搬运 JobGiver。
- **简易淋浴器接电**：`CK_Shower` 重新加入 `CompPowerTrader`（75 W）+ 开关；通电为热水，断电时回退同网热水器，两者都没有则洗冷水但仍可使用。电热水淋浴器 `CK_ShowerPower` 仍必须通电。
- **简易淋浴器移除贴墙限制**：删除 `Placeworker_AttachedToWall`、`isAttachment`、`altitudeLayer=BuildingOnTop` 和 `terrainAffordanceNeeded=null`，1×1 可独立放在任何可用地面上。
- **立体库默认物资**：默认过滤在食物/草药/干草基础上追加木头（`WoodLog`）、植物类（`PlantMatter`）和麦芽（`Wort`）；旧 v4 仓库首次加载只追加这三类，保留玩家自定义过滤。
- **卫生建造菜单**：新增 `CK_Hygiene` 建筑师分类（默认排在「家具」与「电力」之间），淋浴、浴缸、洗手池、马桶、污水管、化粪池与水网设施全部归入「卫生」。
- **1×1 淋浴器**：`CK_Shower` 从 2×2 改为 1×1，可独立放置（后续已移除贴墙限制）；贴图沿用 DBH simple shower 四向图。
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

- **立体库温控只加热不制冷**：`CK_VerticalWarehouse` 的 `CompProperties_TempControl.energyPerSecond` 写成了正数 `21`（正=加热），房间高于目标温度时 `GenTemperature.ControlTemperatureTempChange` 返回 0，所以 28°C 的房间永远降不到 4°C，表现为「通电、有目标温度、但制冷不生效」。改为与原版空调一致的 `-21`，制冷恢复。
- **设置页分页翻译回退英文**：`ModLoc` 之前只加载 `Languages/<Lang>/Keyed/ColonyKit.xml`，而「保温」「蓝图」「机器人」等分页的键在各自的 Keyed 文件里；当 Mod 语言=简中、游戏语言=英文时，这些页会回退成英文（例如 Insulation 页整页英文）。现在按 `Keyed` 目录加载全部 XML，设置页各分页统一使用所选语言；蓝图材料策略（原材质/就手/最便宜）与卫生 DBH 策略下拉框也从 `.Translate()` 改为 `.Loc()`，不再漏成英文。
- **立体库面板重复数量**：`ITab_VerticalWarehouse` 之前用 `thing.LabelCap + " x" + stackCount`，而 `LabelCap` 对可堆叠物已包含 `x75`，显示成「Wood x75 x75」；现在只显示 `LabelCap`。
- **木头/钢铁等材料不参与堆叠**：`StorageStackRules.CanMultiply` 把 `IsWeapon`/`isTechHediff` 直接当排除条件，而 1.6 的原版木头（以及钢铁等 IsStuff 材料）两者都为 true，导致 176 个 Def 里漏掉木头；现在只在「非 IsStuff 的真正武器/科技义体」时排除。
- **库内超限堆取回**：`TryWithdraw` 现在按全局 `stackLimit` 分块取出，避免把 4500 的库内堆整堆丢到地图上被 `GenPlace` 截成 750；`WithdrawAll` 循环取到空，失败时用强制回滚保留物品。
- **立体库堆叠影响库外**：二级/三级不再改全局 `ThingDef.stackLimit`，普通架子/储存区只受一级的全局 ×10 影响。
- **清理死字段/死接口**：删除 `CompWaterStorage` 的 `AutoGenRate`/`Tickrate`/`Auto`/`AutoOnRain`、`Need_Bladder.GivingUpAndShittingOnTheFloorFunction()`、无参 `Building_Shower.TryUseWater()`；`OccupancyCheck` 通过洁具 Gizmo 真正可切换。
- **淋浴/厕所自动工作重新生效**：`Patches/Hygiene/ThinkTree.xml` 的 xpath 缺少 `thinkRoot[@Class="ThinkNode_Tagger"]`，实际从未命中，殖民者不会自动去洗澡/上厕所；现在补上并改回 `success=Normal`。
- **堵塞可修复**：堵塞现在真正阻止洁具使用；玩家的洁具新增「疏通堵塞」Gizmo，避免永久锁死。
- **中断淋浴不再结算**：只有卫生刷满才清携带污渍、清污染标记、给浴室/水温念头；断水、断电、征兆或精神崩溃中断都不再算洗完。
- **DBH 共存**：手动补需求、自动洗澡/上厕所 JobGiver 与 Gizmo 都遵守 DBH/模块开关，读档不会再把已停用的需求加回来。
- **水网开关**：关闭的管件不再读取旧 `pipeNet`；关闭的水箱不再供水/进水；水污染判定改为缓存，避免水塔每 tick 扫描全图。
- **浴缸/洗手池**：不再无管网取水；浴缸要 190 L 才能开始放水；浴后清污染标记。
- **机器人研究 XML**：修正 `Defs/Robotics/Research.xml` 的 `ResearchProjectDef` 闭合标签，避免读 Def 时报错。
- **淋浴器必须真正接水**：`Building_Shower.TryUseWater` 之前在没有 `pipeNet` 时直接返回成功，没接水网也能无限洗澡，还会被当成热水；现在 `CanUse` 和每次取水都要求同网有水，没水网/水箱空不再可选。
- **断水、堵塞、电热水淋浴断电会真正拦截**：淋浴可用性检查改走 `Working()`（堵塞、`PowerRequiredToUse`、水箱存量）；简易淋浴仍可在断电时洗冷水，`CK_ShowerPower` 断电则拒绝使用。
- **洗后清除污染标记**：淋浴完成时调用 `Need_Hygiene.CleanContamination()`；如果本次用的是脏水，随后的 `ContaminationCheckWater` 会重新标记。
- 失禁（`Need_Bladder.crapPants`）不再引用 DBH 的 `SoiledSelf`：改用新 Def `CK_SoiledSelf`（-8 心情、持续 1 天、最多叠 3 层），并保留 DBH 同名 Def 作为回退；此前 Def 不存在，`ThoughtDef.Named` 返回 null，`TryGainMemory` 每次失禁都抛 NRE，`CurLevel` 无法复位，导致该 Pawn 每 150 tick 刷一次「Exception ticking …」。
- 卫生间心情（`SanitationUtil.ApplyBathroomThought`）：finish action 在设施已被拆除/搬运后仍会执行，`fixture.Map` 为 null 时 `GetRoom` 抛 NRE；现在先判 `Spawned`/`Map`。`Building_AssignableFixture.Occupied` 同样加 `Spawned` 保护。
- 膀胱枚举比较方向：本项目的 `BowelCategory` 是 Empty/needBathroom/Busting，与 DBH 的相反，移植时照抄了 `<=` 比较——卧床分支对「没尿意」的 Pawn 也触发、`ticksAtZero` 无条件累加导致 `givenUp` 永不复位；改为 `>=` 后与 DBH 一致。
- 卫生设施检查面板空行：`Building_AssignableFixture.GetInspectString` 在 `base.GetInspectString()` 为空（马桶这类没有 comp 文案的建筑）时仍拼 `"\n" + 原因`，产生前导空行，触发原版 `Inspect string for CK_Toilet… contains empty lines`；现在先判空再换行，并把 `Working()` 的两次调用合并为一次。

### Validated

- 隔离 quicktest（Core + ColonyKit，临时 `-colonykit-storage-test` 探针）：完成 T1/T2/T3 后木头全局 `stackLimit=750`、库内上限 `4500`；5×1000 木头入库合并为 `[4500, 500]`；取出第一堆后剩 `[3750, 500]`、地图出现 750 的一堆；日志显示 `stack rules applied: 176 defs x10; warehouse internal x6`。探针验证后已移除。
- 隔离 quicktest（Core + Harmony + ColonyKit，`-colonykit-nannybot-test`）：工作站与机器人生成成功；`needs=1`、`Moving/Manipulation/Consciousness=1`、`cleanJob=CK_NannyCleanFilth`、`haulJob=HaulToCell`；无 XML、贴图或运行时异常。未覆盖：长期存读档、4000W 充电电网、手动 UI 点击。
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
