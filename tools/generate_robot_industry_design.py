"""Generate the factory / charger / repair-bay design comparison HTML.

Run from the ColonyKit repo root:
    python tools/generate_robot_industry_design.py
"""

from __future__ import annotations

import html
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-industry-design.html")

INVARIANTS = [
    (
        "机器人工厂",
        "固定建筑",
        "负责制造机器人；`minifiedDef` 为空，不能打包搬运。默认拆除只返 50% 材料；严格模式可设 `deconstructible=false` 完全不可拆。",
    ),
    (
        "充电桩",
        "可拆卸携带",
        "`minifiedDef=MinifiedThing`，卸载后成为物品，可被搬运、重装；重装后必须重新接入电网，桩体不保存电量。",
    ),
    (
        "维修机",
        "固定建筑",
        "机器人耐久未满时必须进入维修机；维修期间停靠、断电保护，不能充电或接新家务。`minifiedDef` 为空，不可打包；拆除返 50% 材料。",
    ),
]

SCHEMES = [
    {
        "id": "A",
        "slug": "job-shop",
        "title": "工坊式拆分",
        "subtitle": "Job Shop · 单机多用途",
        "fit": "1–5 台机器人 / 小基地",
        "analogy": "现实中少量、多品种的 job shop：一台多用途工坊完成总装，设备投资低，节拍慢。",
        "story": "先把现有“组装+充电一体桩”拆成三件套：3×3 工厂、1×1 充电桩、2×2 维修机。工厂只保留一条总装账单。",
        "factory": {
            "name": "CK_RobotFactory",
            "size": "3×3",
            "cost": "220 钢 + 8 零部件 + 2 高级零部件",
            "power": "350 W 待机 / 900 W 工作",
            "tech": "机器人制造",
            "fixed": True,
            "detail": "单账单：组装保姆机器人，12,000 work，手工 5；成品落在交互格。",
        },
        "charger": {
            "name": "CK_RobotCharger",
            "size": "1×1",
            "cost": "80 钢 + 2 零部件",
            "power": "50 W 待机 / 4000 W 充电",
            "tech": "机器人制造",
            "fixed": False,
            "detail": "4 小时充满；可卸载携带；不存电，重装必须接电。",
        },
        "repair": {
            "name": "CK_RobotRepairBay",
            "size": "2×2",
            "cost": "180 钢 + 6 零部件 + 1 高级零部件",
            "power": "250 W 待机 / 800 W 维修",
            "tech": "机器人维护",
            "fixed": True,
            "detail": "殖民者操作；600 tick 恢复 250 耐久；每次消耗 1 零部件 + 10 钢。",
        },
        "robot": "标准型 122 kg / 25 kg 载重 / 4.8 kWh",
        "robot_cost": "70 钢 + 6 零部件 + 1 高级零部件 + 2 塑钢",
        "flow": ["工厂总装", "成品出厂", "绑定充电桩", "损坏→维修机"],
        "metrics": [
            ("单台节拍", "约 4.8 小时（12,000 work）"),
            ("首台总投入", "550 钢 / 22 零部件 / 4 高级零部件"),
            ("理论峰值功率", "5.7 kW（工厂+充电+维修同时运行）"),
            ("维修方式", "殖民者操作维修机"),
            ("代码量", "中：1 个工厂账单 + 1 个维修 Job"),
        ],
        "plan": [
            (1, 1, 3, 3, "工厂 3×3", "factory"),
            (5, 1, 1, 1, "充电桩", "charger"),
            (5, 2, 2, 2, "维修机 2×2", "repair"),
        ],
        "pros": [
            "改动最小，最容易从现有 `CompNannyBotStation` 迁移。",
            "只新增一个成品账单，配方/材料/工时都直观。",
            "充电桩可携带，早期就能支撑一个小型前哨。",
        ],
        "cons": [
            "工厂没有中间件，无法批量预制骨架。",
            "维修占用殖民者时间，机器人越多越明显。",
            "单台节拍和产能最低。",
        ],
        "impl": [
            "`CK_RobotFactory : Building_WorkTable`，`BillStack` + `RecipeWorker_RobotAssembly`。",
            "`RecipeWorker.Notify_IterationCompleted` 中 `PawnGenerator.GeneratePawn` + `GenSpawn.Spawn`，配方 `products` 留空；账单仅用 Forever/RepeatCount。",
            "`CK_RobotCharger`：`minifiedDef=MinifiedThing`，`CompPowerTrader` + `CompRobotCharger`。",
            "`CK_RobotRepairBay`：`CompRobotRepairBay` + `CompPowerTrader`；`WorkGiver_OperateRepairBay`。",
            "删除/停用旧 `WorkGiver_RepairNannyBot`，机器人 `homeStation` 迁移为 `homeCharger`。",
        ],
    },
    {
        "id": "B",
        "slug": "cellular",
        "title": "单元式两工位",
        "subtitle": "Cellular Manufacturing · 骨架 + 总装",
        "fit": "3–8 台机器人 / 中型基地",
        "analogy": "现实中的单元制造：把“焊接骨架”和“总装”放在同一生产单元内，中间件可批量周转。",
        "story": "工厂仍是一栋 4×3 建筑，但拆成两条账单：先做机器人骨架，再总装。维修机改为自动停靠维修，不再占用殖民者。",
        "factory": {
            "name": "CK_RobotFactory",
            "size": "4×3",
            "cost": "260 钢 + 9 零部件 + 2 高级零部件",
            "power": "450 W 待机 / 1100 W 工作",
            "tech": "机器人制造",
            "fixed": True,
            "detail": "两账单：骨架 6,000 work（40 钢 + 3 零部件）→ 总装 9,000 work（骨架 + 3 零部件 + 1 高级零部件）。",
        },
        "charger": {
            "name": "CK_RobotCharger",
            "size": "1×1",
            "cost": "80 钢 + 2 零部件",
            "power": "50 W 待机 / 4000 W 充电",
            "tech": "机器人制造",
            "fixed": False,
            "detail": "与方案 A 相同；可携带、可重装，不存电。",
        },
        "repair": {
            "name": "CK_RobotRepairBay",
            "size": "2×2",
            "cost": "180 钢 + 6 零部件 + 1 高级零部件",
            "power": "250 W 待机 / 600 W 维修",
            "tech": "机器人维护",
            "fixed": True,
            "detail": "自动停靠维修；1,200 tick 恢复 250 耐久；每次 1 零部件 + 5 钢；无需殖民者。",
        },
        "robot": "模块型 116 kg / 22 kg 载重 / 4.8 kWh",
        "robot_cost": "64 钢 + 7 零部件 + 1 高级零部件 + 4 塑钢",
        "flow": ["骨架 6,000", "中间件入库", "总装 9,000", "绑定充电桩", "损坏→自动维修"],
        "metrics": [
            ("单台节拍", "约 6 小时（6,000 + 9,000 work）"),
            ("首台总投入", "584 钢 / 24 零部件 / 4 高级零部件"),
            ("理论峰值功率", "5.7 kW（工厂+充电+维修同时运行）"),
            ("维修方式", "机器人停靠，维修机自动修"),
            ("代码量", "中高：中间件 ThingDef + 2 个账单 + 自动维修"),
        ],
        "plan": [
            (1, 1, 4, 3, "工厂 4×3", "factory"),
            (6, 1, 1, 1, "充电桩", "charger"),
            (6, 2, 2, 2, "维修机 2×2", "repair"),
        ],
        "pros": [
            "中间件让“骨架备货/批量生产”成为可能，节拍比 A 稳。",
            "维修自动停靠，机器人多时不会占满殖民者。",
            "工厂、充电、维修职责彻底分离，符合你提出的结构。",
        ],
        "cons": [
            "中间件需要入库/搬运，工厂周围要留物流空间。",
            "自动维修需要额外研究，且必须消耗零部件。",
            "两条账单的空闲/排队需要玩家管理。",
        ],
        "impl": [
            "`CK_RobotFrame` 作为普通 `ThingDef`（25 kg，可堆叠 10），由骨架账单产出。",
            "`CK_AssembleNannyBot` 账单消耗 `CK_RobotFrame`；`RecipeWorker` 总装完成后生成 Pawn。",
            "维修机 `CompRobotRepairBay` 自动扫描停靠位上的机器人，独占预约，维修期间机器人 Job 被暂停。",
            "`CompNannyBot` 新增 `homeCharger`，并保留 `IRobotDock` 接口兼容旧站。",
        ],
    },
    {
        "id": "C",
        "slug": "assembly-line",
        "title": "流水线拆分",
        "subtitle": "Assembly Line · 骨架厂 + 总装线",
        "fit": "8+ 台机器人 / 大型基地",
        "analogy": "现实中的流水线：骨架和总装分开，各自专机、并行节拍，单位成本低但换型慢。",
        "story": "把工厂拆成两栋固定建筑：2×2 骨架厂 + 4×2 总装线。中间件用 hauling 连接；充电桩和维修机仍独立。",
        "factory": {
            "name": "骨架厂 + 总装线",
            "size": "2×2 + 4×2",
            "cost": "140 钢 + 4 零部件 + 1 高级（骨架）/ 240 钢 + 8 零部件 + 2 高级（总装）",
            "power": "500 W + 1300 W 工作",
            "tech": "机器人制造 + 流水线",
            "fixed": True,
            "detail": "骨架 6,000 work；总装 12,000 work；两栋都可独立排队，骨架可先备货。",
        },
        "charger": {
            "name": "CK_RobotCharger",
            "size": "1×1",
            "cost": "80 钢 + 2 零部件",
            "power": "50 W 待机 / 4000 W 充电",
            "tech": "机器人制造",
            "fixed": False,
            "detail": "同上；多台机器人建议 2 台充电桩，避免排队。",
        },
        "repair": {
            "name": "CK_RobotRepairBay",
            "size": "2×2",
            "cost": "180 钢 + 6 零部件 + 1 高级零部件",
            "power": "250 W 待机 / 1000 W 维修",
            "tech": "机器人维护",
            "fixed": True,
            "detail": "殖民者操作；600 tick 恢复 250 耐久；每次 1 零部件 + 8 钢。",
        },
        "robot": "重载型 146 kg / 35 kg 载重 / 6.4 kWh",
        "robot_cost": "95 钢 + 8 零部件 + 2 高级零部件 + 8 塑钢",
        "flow": ["骨架厂 6,000", "haul 中间件", "总装线 12,000", "绑定充电桩", "损坏→维修机"],
        "metrics": [
            ("单台节拍", "约 7.2 小时（可并行备骨架）"),
            ("首台总投入", "735 钢 / 28 零部件 / 6 高级零部件"),
            ("理论峰值功率", "6.8 kW（两栋工厂+充电+维修）"),
            ("维修方式", "殖民者操作维修机"),
            ("代码量", "高：两栋工厂 + 中间件物流 + 独立配方"),
        ],
        "plan": [
            (1, 1, 2, 2, "骨架厂 2×2", "factory"),
            (4, 1, 4, 2, "总装线 4×2", "factory"),
            (3, 1, 1, 2, "WIP", "wip"),
            (2, 4, 1, 1, "充电桩", "charger"),
            (5, 4, 2, 2, "维修机 2×2", "repair"),
        ],
        "pros": [
            "产能最高，骨架和总装可并行/备货。",
            "职责最清楚：骨架、总装、充电、维修各自独立。",
            "适合把机器人当“装备”批量生产的后期基地。",
        ],
        "cons": [
            "占地、功率、物流和代码量都最高。",
            "机器人需求少时，流水线会长期闲置。",
            "两栋工厂都要固定，基地搬迁成本高。",
        ],
        "impl": [
            "两个 `Building_WorkTable` Def；`CK_RobotFrame` 中间件；两套 `RecipeDef`。",
            "总装线需要 `PlaceWorker` 检查骨架输入口/输出口，避免中间件堵死。",
            "维修 Job 与方案 A 共用；充电桩与方案 A 共用。",
            "建议给总装线 `BillStack` 设上限（例如 5 台），避免无限生产。",
        ],
    },
    {
        "id": "D",
        "slug": "prefab",
        "title": "预制 + 现场保障",
        "subtitle": "Prefab · 固定工厂 + 便携充电网络",
        "fit": "多前哨 / 远征基地",
        "analogy": "现实中的预制件+现场保障：工厂集中生产，充电桩是可搬运的现场基础设施，维修集中在固定维修站。",
        "story": "工厂和维修机固定在主基地，充电桩做成可携带的 1×1 设备。机器人可以带充电桩去前哨，但大修必须回主基地。",
        "factory": {
            "name": "CK_RobotFactory",
            "size": "3×3",
            "cost": "200 钢 + 7 零部件 + 2 高级零部件",
            "power": "300 W 待机 / 800 W 工作",
            "tech": "机器人制造",
            "fixed": True,
            "detail": "单账单：10,000 work；成品机器人直接出厂。",
        },
        "charger": {
            "name": "CK_RobotCharger",
            "size": "1×1",
            "cost": "60 钢 + 2 零部件",
            "power": "50 W 待机 / 4000 W 充电",
            "tech": "机器人制造",
            "fixed": False,
            "detail": "35 kg，可被一名殖民者搬运；不存电；前哨需要发电机/电网。建议主基地 1 台、前哨 1 台。",
        },
        "repair": {
            "name": "CK_RobotRepairBay",
            "size": "2×2",
            "cost": "180 钢 + 6 零部件 + 1 高级零部件",
            "power": "250 W 待机 / 900 W 维修",
            "tech": "机器人维护",
            "fixed": True,
            "detail": "殖民者操作；600 tick 恢复 250 耐久；每次 1 零部件 + 10 钢；不提供野外维修。",
        },
        "robot": "轻量型 96 kg / 18 kg 载重 / 3.6 kWh",
        "robot_cost": "52 钢 + 5 零部件 + 1 高级零部件 + 2 塑钢",
        "flow": ["主基地制造", "成品出厂", "携带充电桩去前哨", "前哨充电", "大修回主基地"],
        "metrics": [
            ("单台节拍", "约 4 小时（10,000 work）"),
            ("首台总投入", "552 钢 / 22 零部件 / 4 高级零部件"),
            ("理论峰值功率", "5.7–9.7 kW（1–2 台充电桩同时充电）"),
            ("维修方式", "固定维修机，殖民者操作"),
            ("代码量", "中：1 个工厂账单 + 可搬运充电桩 + 维修机"),
        ],
        "plan": [
            (1, 1, 3, 3, "工厂 3×3", "factory"),
            (5, 1, 1, 1, "充电桩 A", "charger"),
            (7, 1, 1, 1, "充电桩 B", "charger"),
            (5, 2, 2, 2, "维修机 2×2", "repair"),
        ],
        "pros": [
            "最符合“充电桩可拆卸携带”的现场保障思路。",
            "前哨不用建工厂，降低远征基地门槛。",
            "机器人轻，载重/能耗低，充电桩容易被一名殖民者搬运。",
        ],
        "cons": [
            "前哨机器人损坏后必须回主基地，维修物流长。",
            "多台充电桩会拉高电网峰值（每台 4000 W）。",
            "轻量机器人载重较低，不适合重物搬运。",
        ],
        "impl": [
            "充电桩 Def 设 `<minifiedDef>MinifiedThing</minifiedDef>`；卸载后是 35 kg 物品。",
            "重装时 `CompPowerTrader` 自动寻网；若前哨无电，桩体亮红/不工作。",
            "机器人 `homeCharger` 可指向任意充电桩；换桩只改引用。",
            "维修机固定；`CompRobotRepairBay` 不生成野外维修 Job。",
        ],
    },
    {
        "id": "E",
        "slug": "fms",
        "title": "全自动柔性制造",
        "subtitle": "FMS · 无人工厂 + 自动维修",
        "fit": "10+ 台机器人 / 后期基地",
        "analogy": "现实中的柔性制造系统：高投资、高功率、低人工，换型靠软件，适合稳定的大批量生产。",
        "story": "工厂变成 4×4 自动化单元，不需要殖民者；维修机也自动完成停靠维修。代价是高级零部件、AI 核心和 1.5 kW 级功率。",
        "factory": {
            "name": "CK_RobotAutofactory",
            "size": "4×4",
            "cost": "420 钢 + 14 零部件 + 4 高级零部件 + 1 AI 核心",
            "power": "600 W 待机 / 1500 W 工作",
            "tech": "机器人自动化",
            "fixed": True,
            "detail": "自定义 `CompRobotFactory` 自动生产，30,000 tick/台（约 12 小时）；无需殖民者；建议全图上限 1 台。",
        },
        "charger": {
            "name": "CK_RobotCharger",
            "size": "1×1",
            "cost": "80 钢 + 2 零部件",
            "power": "50 W 待机 / 4000 W 充电",
            "tech": "机器人制造",
            "fixed": False,
            "detail": "与方案 A/B 相同；自动分配最近空闲桩。",
        },
        "repair": {
            "name": "CK_RobotRepairBay",
            "size": "2×2",
            "cost": "180 钢 + 6 零部件 + 1 高级零部件",
            "power": "300 W 待机 / 1500 W 维修",
            "tech": "机器人自动化",
            "fixed": True,
            "detail": "自动停靠维修；800 tick 恢复 250 耐久；每次 1 零部件 + 5 钢；无需殖民者。",
        },
        "robot": "高配型 130 kg / 30 kg 载重 / 6.0 kWh",
        "robot_cost": "80 钢 + 8 零部件 + 2 高级零部件 + 12 塑钢",
        "flow": ["自动工厂 30,000", "成品出厂", "自动充电", "自动维修"],
        "metrics": [
            ("单台节拍", "约 12 小时（30,000 tick，无人）"),
            ("首台总投入", "760 钢 / 30 零部件 / 7 高级零部件 + AI 核心"),
            ("理论峰值功率", "7.0 kW（工厂+充电+维修同时运行）"),
            ("维修方式", "维修机自动完成，无殖民者"),
            ("代码量", "高：自定义工厂 Comp + 自动维修 + 电网压力测试"),
        ],
        "plan": [
            (1, 1, 4, 4, "自动工厂 4×4", "factory"),
            (6, 1, 1, 1, "充电桩", "charger"),
            (6, 2, 2, 2, "维修机 2×2", "repair"),
        ],
        "pros": [
            "后期完全解放殖民者，适合大规模机器人队。",
            "工厂/充电/维修全部自动，节拍稳定。",
            "机器人高配，载重和续航最好。",
        ],
        "cons": [
            "高级零部件、AI 核心、功率门槛都高。",
            "容易让后期劳动/维修压力消失，需要严格限产。",
            "自定义 Comp 的存档/断电/拆卸边界最多，测试成本最高。",
        ],
        "impl": [
            "`CompRobotFactory` 自己维护进度/配方/断电暂停，不依赖 `BillStack`；`CompTick` 每 250 tick 结算。",
            "`CompRobotRepairBay` 自动预约停靠位；维修时机器人 Job 暂停，充满才释放。",
            "限产：每张地图最多 1 台自动工厂；每 3 天最多 1 台；或消耗 AI 核心。",
            "必须测试断电重连、拆除中途、存档中断、机器人满编。",
        ],
    },
]

MATERIAL_ROWS = [
    ("A 工坊式", "标准型", 122, 25, "4.8 kWh / 38 kg", "70 / 6 / 1 / 2", "0.266", "0.306", "0.52", "44.3° / 40.4°", "6.8 / 8.2 kPa"),
    ("B 单元式", "模块型", 116, 22, "4.8 kWh / 37 kg", "64 / 7 / 1 / 4", "0.260", "0.298", "0.52", "45.0° / 41.1°", "6.6 / 7.8 kPa"),
    ("C 流水线", "重载型", 146, 35, "6.4 kWh / 42 kg", "95 / 8 / 2 / 8", "0.300", "0.339", "0.60", "45.0° / 41.5°", "6.4 / 8.0 kPa"),
    ("D 预制保障", "轻量型", 96, 18, "3.6 kWh / 28 kg", "52 / 5 / 1 / 2", "0.250", "0.290", "0.48", "43.8° / 39.6°", "6.3 / 7.5 kPa"),
    ("E 全自动", "高配型", 130, 30, "6.0 kWh / 42 kg", "80 / 8 / 2 / 12", "0.280", "0.321", "0.54", "44.0° / 40.1°", "6.7 / 8.2 kPa"),
]


def esc(text: str) -> str:
    return html.escape(str(text))


def render_plan(blocks) -> str:
    cell = 34
    width = 9 * cell
    height = 6 * cell
    items = []
    for x, y, w, h, label, kind in blocks:
        left = (x - 1) * cell
        top = (y - 1) * cell
        items.append(
            f'<div class="block {kind}" style="left:{left}px;top:{top}px;width:{w * cell - 4}px;height:{h * cell - 4}px">'
            f'<span>{esc(label)}</span></div>'
        )
    return (
        f'<div class="plan-board" style="width:{width}px;height:{height}px">'
        f'{"".join(items)}</div>'
    )


def render_scheme(s) -> str:
    factory = s["factory"]
    charger = s["charger"]
    repair = s["repair"]

    def card(item, kind):
        fixed = "fixed" if item["fixed"] else "portable"
        fixed_label = "固定 · 不可携带" if item["fixed"] else "可拆卸 · 可携带"
        return f"""
        <article class="building {kind}">
          <div class="building-top">
            <span class="building-icon">{kind.upper()}</span>
            <span class="badge {fixed}">{fixed_label}</span>
          </div>
          <h4>{esc(item["name"])}</h4>
          <div class="building-size">{esc(item["size"])}</div>
          <dl>
            <div><dt>造价</dt><dd>{esc(item["cost"])}</dd></div>
            <div><dt>功率</dt><dd>{esc(item["power"])}</dd></div>
            <div><dt>科技</dt><dd>{esc(item["tech"])}</dd></div>
          </dl>
          <p>{esc(item["detail"])}</p>
        </article>
        """

    flow = '<span class="arrow">→</span>'.join(f'<span class="flow-step">{esc(step)}</span>' for step in s["flow"])
    metrics = "".join(f"<tr><th>{esc(k)}</th><td>{esc(v)}</td></tr>" for k, v in s["metrics"])
    pros = "".join(f"<li>{esc(item)}</li>" for item in s["pros"])
    cons = "".join(f"<li>{esc(item)}</li>" for item in s["cons"])
    impl = "".join(f"<li>{esc(item)}</li>" for item in s["impl"])

    return f"""
    <section class="panel" data-panel="{s["id"]}">
      <div class="scheme-head">
        <div>
          <p class="eyebrow">方案 {s["id"]} · {esc(s["subtitle"])}</p>
          <h2>{esc(s["title"])}</h2>
          <p class="analogy">{esc(s["analogy"])}</p>
        </div>
        <div class="fit">{esc(s["fit"])}</div>
      </div>

      <p class="story">{esc(s["story"])}</p>

      <div class="building-grid">
        {card(factory, "factory")}
        {card(charger, "charger")}
        {card(repair, "repair")}
      </div>

      <div class="flow">{flow}</div>

      <div class="split">
        <div>
          <h3>布局示意</h3>
          {render_plan(s["plan"])}
          <p class="legend"><span class="dot factory"></span>工厂 <span class="dot charger"></span>充电桩 <span class="dot repair"></span>维修机 <span class="dot wip"></span>中间件</p>
        </div>
        <div>
          <h3>关键数据</h3>
          <table class="metrics">{metrics}</table>
          <p class="robot-line"><strong>本方案机器人：</strong>{esc(s["robot"])}<br><span>{esc(s["robot_cost"])}</span></p>
        </div>
      </div>

      <div class="split bottom">
        <div>
          <h3>优点</h3>
          <ul class="good">{pros}</ul>
          <h3>代价 / 风险</h3>
          <ul class="bad">{cons}</ul>
        </div>
        <div>
          <h3>RimWorld 落地映射</h3>
          <ul class="impl">{impl}</ul>
        </div>
      </div>
    </section>
    """


def render_materials() -> str:
    rows = "".join(
        f"<tr><td>{esc(a)}</td><td>{esc(b)}</td><td>{m}</td><td>{p}</td><td>{esc(batt)}</td><td>{esc(cost)}</td><td>{cog1}</td><td>{cog2}</td><td>{track}</td><td>{tilt}</td><td>{ground}</td></tr>"
        for a, b, m, p, batt, cost, cog1, cog2, track, tilt, ground in MATERIAL_ROWS
    )
    return f"""
    <section class="panel" data-panel="M">
      <div class="scheme-head">
        <div>
          <p class="eyebrow">附页 · 工程口径</p>
          <h2>材料使用与重量配重</h2>
          <p class="analogy">RimWorld 的 W 是抽象功率；这里用“游戏材料成本 + 现实质量/重心”两套口径，避免把 4000 W 直接当成现实 4 kW 充电。</p>
        </div>
        <div class="fit">质量 / 重心 / 稳定性</div>
      </div>

      <div class="split">
        <div>
          <h3>标准型质量预算（方案 A，122 kg）</h3>
          <table class="metrics">
            <tr><th>部件</th><th>质量</th><th>重心高度</th></tr>
            <tr><td>钢制底盘 / 框架</td><td>34 kg</td><td>0.32 m</td></tr>
            <tr><td>外壳</td><td>9 kg</td><td>0.55 m</td></tr>
            <tr><td>履带 / 轮组</td><td>14 kg</td><td>0.08 m</td></tr>
            <tr><td>电机 / 减速器</td><td>14 kg</td><td>0.16 m</td></tr>
            <tr><td>4.8 kWh 磷酸铁锂电池</td><td>38 kg</td><td>0.18 m（最低）</td></tr>
            <tr><td>电子 / 传感器 / 计算</td><td>4 kg</td><td>0.70 m</td></tr>
            <tr><td>手臂 / 工具</td><td>6 kg</td><td>0.45 m</td></tr>
            <tr><td>紧固件 / 线束</td><td>3 kg</td><td>0.30 m</td></tr>
            <tr><td><strong>合计</strong></td><td><strong>122 kg</strong></td><td><strong>0.266 m</strong></td></tr>
          </table>
        </div>
        <div>
          <h3>稳定性与承载</h3>
          <div class="formula">
            <p>倾覆角 θ = atan((轮距 / 2) / 重心高度)</p>
            <p>接地压力 p = (整机质量 + 载重) × g / 履带接地面积</p>
            <p>设计目标：θ ≥ 35°，接地压力 ≤ 10 kPa，载重 ≤ 整机质量的 25%。</p>
          </div>
          <ul class="impl">
            <li>电池包放在底盘最低处、纵向居中；这是把重心从 0.34 m 压到 0.266 m 的关键。</li>
            <li>右侧扫帚约 2.5 kg、偏置 0.38 m；左侧加 1.5 kg 钢配重，残余横向偏移约 3 mm。</li>
            <li>手臂举升 5 kg × 0.45 m 产生约 22 N·m 倾覆力矩，履带支撑面可覆盖。</li>
            <li>满 25 kg 载重时重心升到 0.306 m，倾覆角仍有 40.4°，高于 15° 最大作业坡度。</li>
            <li>接地压力 6.8–8.2 kPa，低于普通室内楼板 10 kPa 的设计上限。</li>
          </ul>
        </div>
      </div>

      <h3>五种方案的机器人物料 / 配重</h3>
      <div class="table-wrap">
        <table class="compare">
          <thead><tr><th>方案</th><th>构型</th><th>整机 kg</th><th>载重 kg</th><th>电池</th><th>钢 / 零部件 / 高级 / 塑钢</th><th>空载重心 m</th><th>满载重心 m</th><th>轮距 m</th><th>倾覆角 空/满</th><th>接地压力 空/满</th></tr></thead>
          <tbody>{rows}</tbody>
        </table>
      </div>

      <div class="split bottom">
        <div>
          <h3>材料选择依据</h3>
          <ul class="good">
            <li><strong>钢</strong>：底盘、框架、履带护板；便宜、密度高、重心低，适合做配重。</li>
            <li><strong>塑钢</strong>：外壳、关节、工具挂点；减重但不下移重心，只用于非承力件。</li>
            <li><strong>零部件</strong>：电机、传感器、电源管理、线束。</li>
            <li><strong>高级零部件</strong>：AI 核心、电池 BMS、自动维修控制器；不回收。</li>
            <li><strong>不采用铀/铅配重</strong>：游戏内昂贵且没有对应材料；钢配重足够，且可回收。</li>
          </ul>
        </div>
        <div>
          <h3>游戏内材料平衡</h3>
          <ul class="impl">
            <li>机器人报废回收 40 钢 + 3 零部件：相对 70/6/1 的 BOM，钢回收约 57%、零部件 50%、高级零部件 0%。</li>
            <li>维修消耗 1 零部件 + 5–10 钢/250 耐久；按 1000 耐久，满修约 4 零部件 + 20–40 钢，低于重造。</li>
            <li>充电桩可拆卸但重装不保存电量；拆下再装等于重新接电，不产生免费储能。</li>
            <li>工厂/维修机不打包，避免玩家把重工业建筑塞进远征队；拆除只返 50% 材料。</li>
          </ul>
        </div>
      </div>

      <div class="note">
        <strong>口径说明：</strong> 4.8 kWh / 平均 400 W ≈ 12 小时，1.2 kW DC 充电 ≈ 4 小时；游戏内 4000 W 是抽象功率，包含变流/散热/待机损耗，不等同于现实 4 kW 恒功率。质量与重心用于设定/美术/平衡，不改变 RimWorld 的平面寻路。
      </div>
    </section>
    """


def main():
    tabs = []
    panels = []
    for index, scheme in enumerate(SCHEMES):
        active = " active" if index == 0 else ""
        tabs.append(
            f'<button class="tab{active}" data-tab="{scheme["id"]}" type="button">'
            f'<span class="tab-id">{scheme["id"]}</span><span>{esc(scheme["title"])}</span>'
            f'</button>'
        )
        panel = render_scheme(scheme).replace('<section class="panel"', f'<section class="panel{active}"', 1)
        panels.append(panel)

    tabs.append('<button class="tab" data-tab="M" type="button"><span class="tab-id">附</span><span>材料与配重</span></button>')
    panels.append(render_materials())

    compare_rows = "".join(
        f"<tr><td><strong>{s['id']}</strong> {esc(s['title'])}</td>"
        f"<td>{esc(s['factory']['size'])}</td><td>{esc(s['charger']['size'])}</td><td>{esc(s['repair']['size'])}</td>"
        f"<td>{esc(s['metrics'][0][1])}</td><td>{esc(s['metrics'][2][1])}</td><td>{esc(s['fit'])}</td></tr>"
        for s in SCHEMES
    )

    css = """
:root { color-scheme: dark; --bg:#0f1418; --panel:#171e24; --panel2:#1d262d; --line:#30414c; --text:#eaf2f4; --muted:#9db0b8; --teal:#69d3d0; --amber:#e9b85f; --red:#e07a6a; --green:#8fcf93; }
* { box-sizing:border-box; }
body { margin:0; background:radial-gradient(900px 500px at 10% -10%, rgba(60,140,145,.22), transparent 65%), radial-gradient(700px 420px at 95% 4%, rgba(196,139,67,.18), transparent 68%), var(--bg); color:var(--text); font-family:"Segoe UI","Microsoft YaHei","PingFang SC",system-ui,sans-serif; }
.wrap { max-width:1360px; margin:0 auto; padding:28px 22px 54px; }
.eyebrow { margin:0 0 7px; color:var(--teal); font-size:11px; letter-spacing:.16em; text-transform:uppercase; }
h1 { margin:0; font-size:clamp(28px,3.4vw,46px); letter-spacing:-.03em; }
.sub { color:var(--muted); line-height:1.65; max-width:880px; margin:10px 0 0; }
.badges { display:flex; gap:8px; flex-wrap:wrap; margin-top:16px; }
.badges span { border:1px solid var(--line); background:var(--panel); border-radius:999px; padding:6px 10px; color:var(--muted); font-size:12px; }
.invariants { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; margin:18px 0; }
.invariant { border:1px solid var(--line); background:linear-gradient(150deg,var(--panel2),var(--panel)); border-radius:16px; padding:14px; }
.invariant h3 { margin:0 0 5px; font-size:15px; }
.invariant b { color:var(--teal); font-size:11px; letter-spacing:.08em; text-transform:uppercase; }
.invariant p { margin:8px 0 0; color:var(--muted); font-size:12px; line-height:1.6; }
h2 { margin:0; font-size:clamp(24px,3vw,34px); letter-spacing:-.02em; }
h3 { margin:20px 0 10px; font-size:15px; color:var(--teal); }
.compare-wrap { overflow-x:auto; border:1px solid var(--line); border-radius:16px; }
table { border-collapse:collapse; width:100%; font-size:12px; }
th, td { text-align:left; padding:9px 10px; border-bottom:1px solid rgba(80,110,120,.25); vertical-align:top; }
th { color:var(--muted); font-weight:600; background:rgba(0,0,0,.16); white-space:nowrap; }
tbody tr:hover { background:rgba(105,211,208,.045); }
.tabs { display:flex; gap:6px; flex-wrap:wrap; padding:8px; background:var(--panel); border:1px solid var(--line); border-radius:18px; margin-top:18px; }
.tab { border:1px solid transparent; background:transparent; color:var(--muted); border-radius:11px; padding:9px 11px; display:flex; align-items:center; gap:7px; cursor:pointer; font:inherit; }
.tab:hover { background:var(--panel2); color:var(--text); }
.tab.active { border-color:var(--teal); background:var(--panel2); color:var(--text); }
.tab-id { color:var(--amber); font-size:11px; font-weight:800; }
.panel { display:none; margin-top:14px; background:var(--panel); border:1px solid var(--line); border-radius:22px; padding:22px; }
.panel.active { display:block; animation:fade .18s ease; }
@keyframes fade { from { opacity:0; transform:translateY(4px); } to { opacity:1; transform:none; } }
.scheme-head { display:flex; justify-content:space-between; align-items:flex-start; gap:20px; }
.analogy { margin:9px 0 0; color:var(--muted); line-height:1.65; max-width:900px; }
.fit { flex:none; border:1px solid var(--teal); color:var(--teal); border-radius:12px; padding:9px 12px; font-size:12px; }
.story { color:var(--text); line-height:1.7; margin:14px 0 18px; padding-left:12px; border-left:3px solid var(--amber); }
.building-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; }
.building { border:1px solid var(--line); background:var(--panel2); border-radius:15px; padding:14px; }
.building.factory { border-top:3px solid var(--amber); }
.building.charger { border-top:3px solid var(--teal); }
.building.repair { border-top:3px solid var(--red); }
.building-top { display:flex; align-items:center; justify-content:space-between; gap:8px; }
.building-icon { font-size:10px; letter-spacing:.12em; color:var(--muted); font-weight:800; }
.badge { font-size:10px; border-radius:999px; padding:4px 7px; border:1px solid var(--line); color:var(--muted); }
.badge.fixed { color:var(--amber); border-color:rgba(233,184,95,.4); }
.badge.portable { color:var(--teal); border-color:rgba(105,211,208,.4); }
.building h4 { margin:9px 0 2px; font-size:16px; }
.building-size { color:var(--muted); font-size:12px; }
dl { margin:10px 0 0; }
dl div { display:flex; gap:8px; margin:4px 0; }
dt { color:var(--muted); font-size:11px; min-width:36px; }
dd { margin:0; font-size:12px; }
.building p { margin:10px 0 0; color:var(--muted); font-size:12px; line-height:1.55; }
.flow { display:flex; align-items:center; gap:7px; flex-wrap:wrap; margin:18px 0; padding:12px; border:1px dashed var(--line); border-radius:14px; }
.flow-step { background:#0d151a; border:1px solid var(--line); color:var(--text); border-radius:9px; padding:7px 9px; font-size:12px; }
.arrow { color:var(--amber); font-weight:800; }
.split { display:grid; grid-template-columns:1fr 1fr; gap:22px; }
.split.bottom { margin-top:8px; }
.plan-board { position:relative; width:306px; height:204px; border:1px solid var(--line); border-radius:12px; overflow:hidden; background:repeating-linear-gradient(0deg, transparent 0 33px, rgba(80,110,120,.12) 33px 34px), repeating-linear-gradient(90deg, transparent 0 33px, rgba(80,110,120,.12) 33px 34px), #0c1317; }
.block { position:absolute; border-radius:6px; padding:5px; font-size:10px; line-height:1.25; display:flex; align-items:center; justify-content:center; text-align:center; border:1px solid rgba(255,255,255,.16); color:#0c1418; font-weight:800; }
.block.factory { background:rgba(233,184,95,.9); }
.block.charger { background:rgba(105,211,208,.9); }
.block.repair { background:rgba(224,122,106,.9); }
.block.wip { background:rgba(160,180,190,.65); }
.legend { color:var(--muted); font-size:11px; display:flex; align-items:center; gap:6px; flex-wrap:wrap; }
.dot { width:8px; height:8px; border-radius:50%; display:inline-block; margin-left:6px; }
.dot.factory { background:var(--amber); } .dot.charger { background:var(--teal); } .dot.repair { background:var(--red); } .dot.wip { background:#a0b4be; }
.metrics th { width:42%; }
.robot-line { margin-top:12px; color:var(--muted); font-size:12px; line-height:1.6; }
.robot-line strong { color:var(--text); }
.good, .bad, .impl { margin:0; padding-left:18px; color:var(--muted); font-size:12px; line-height:1.65; }
.good li::marker { color:var(--green); }
.bad li::marker { color:var(--red); }
.impl li::marker { color:var(--teal); }
.formula { border:1px solid var(--line); border-radius:12px; padding:10px 12px; color:var(--muted); font-size:12px; line-height:1.7; margin-bottom:10px; }
.formula p { margin:4px 0; }
.table-wrap { overflow-x:auto; border:1px solid var(--line); border-radius:14px; }
.table-wrap table { min-width:980px; }
.note { margin-top:18px; border:1px solid rgba(233,184,95,.35); background:rgba(233,184,95,.06); border-radius:12px; padding:12px 14px; color:var(--muted); font-size:12px; line-height:1.65; }
.reco { margin-top:18px; border:1px solid rgba(105,211,208,.45); background:linear-gradient(120deg,rgba(105,211,208,.10),rgba(105,211,208,.03)); border-radius:16px; padding:16px; }
.reco h3 { margin:0 0 6px; color:var(--teal); }
.reco p { margin:0; color:var(--muted); line-height:1.65; font-size:13px; }
@media (max-width:900px) { .invariants,.building-grid,.split { grid-template-columns:1fr; } .scheme-head { flex-direction:column; } .plan-board { max-width:100%; } }
"""

    body = f"""
<div class="wrap">
  <header>
    <p class="eyebrow">ColonyKit · Robotics Industry Design</p>
    <h1>机器人工厂 / 充电桩 / 维修机</h1>
    <p class="sub">按“制造、充电、维修”三权分立设计的 5 种落地方案。工厂与维修机固定不可携带；充电桩可拆卸携带。每种方案都给出建筑拆分、节拍、材料、配重、实现映射和风险。</p>
    <div class="badges">
      <span>5 种方案</span><span>工厂固定</span><span>维修机固定</span><span>充电桩可携带</span><span>含材料 / 重心 / 稳定性</span>
    </div>
  </header>

  <div class="invariants">
    {''.join(f'<div class="invariant"><b>{esc(k)}</b><h3>{esc(v)}</h3><p>{esc(d)}</p></div>' for k, v, d in INVARIANTS)}
  </div>

  <h3>横向对比</h3>
  <div class="compare-wrap">
    <table class="compare">
      <thead><tr><th>方案</th><th>工厂</th><th>充电桩</th><th>维修机</th><th>单台节拍</th><th>峰值功率</th><th>适用</th></tr></thead>
      <tbody>{compare_rows}</tbody>
    </table>
  </div>

  <nav class="tabs" role="tablist" aria-label="方案页签">
    {''.join(tabs)}
  </nav>

  {''.join(panels)}

  <div class="reco">
    <h3>推荐：方案 B「单元式两工位」</h3>
    <p>它在“改动量、产能、材料可维护性”之间最平衡：工厂拆成骨架/总装两条账单，能体现真实制造；充电桩可携带；维修机自动停靠，解决现在“殖民者维修和充电互相打断”的问题。若只想先做最小可用版，选 A；若目标是后期大规模机器人队，再考虑 C/E。</p>
  </div>
</div>
<script>
const tabs = Array.from(document.querySelectorAll('.tab'));
const panels = Array.from(document.querySelectorAll('.panel'));
function show(id) {{
  tabs.forEach((tab) => tab.classList.toggle('active', tab.dataset.tab === id));
  panels.forEach((panel) => panel.classList.toggle('active', panel.dataset.panel === id));
}}
tabs.forEach((tab) => tab.addEventListener('click', () => show(tab.dataset.tab)));
document.addEventListener('keydown', (event) => {{
  const index = tabs.findIndex((tab) => tab.classList.contains('active'));
  if (event.key === 'ArrowRight') show(tabs[(index + 1) % tabs.length].dataset.tab);
  if (event.key === 'ArrowLeft') show(tabs[(index - 1 + tabs.length) % tabs.length].dataset.tab);
}});
</script>
"""

    html_doc = f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>机器人工厂 / 充电桩 / 维修机 · 5 种方案</title>
<style>{css}</style>
</head>
<body>
{body}
</body>
</html>
"""
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as handle:
        handle.write(html_doc)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
