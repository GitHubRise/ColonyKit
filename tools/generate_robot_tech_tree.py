"""Generate the five-tier robot tech-tree HTML.

Run from the ColonyKit repo root:
    python tools/generate_robot_tech_tree.py
"""

from __future__ import annotations

import html
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-tech-tree.html")

TECHS = [
    {
        "id": "T1",
        "name": "机械化工坊",
        "research": "CK_RobotBasics · 机器人基础",
        "research_cost": "1,200",
        "prereq": "机械加工（Machining）",
        "tier": "T1 · 基础工坊式",
        "fit": "第一台机器人",
        "summary": "用一台 3×3 工坊完成总装；充电桩和维修机首次分家。机器人是标准 Mk-I，维修仍需要殖民者操作维修机。",
        "new": [
            "3×3 机器人工厂（单账单，12,000 work）",
            "1×1 可拆卸充电桩",
            "2×2 手动维修机",
            "Mk-I 标准保姆机器人",
        ],
        "reuse": [],
        "products": [
            ("CK_RobotFactory_T1", "建筑 · 工厂", "220 钢 + 8 零部件 + 2 高级零部件", "12,000 work / 350–900 W", "3×3，固定；总装 Mk-I。"),
            ("CK_RobotCharger", "建筑 · 充电桩", "80 钢 + 2 零部件", "50–4,000 W", "1×1，`minifiedDef` 可携带；不存电。"),
            ("CK_RobotRepairBay", "建筑 · 维修机", "180 钢 + 6 零部件 + 1 高级零部件", "600 tick / 250 耐久", "2×2，固定；殖民者操作。"),
            ("CK_NannyBot_MkI", "机器人 · Mk-I", "70 钢 + 6 零部件 + 1 高级零部件 + 2 塑钢", "4.8 kWh / 122 kg", "标准型；25 kg 载重；1000 耐久。"),
        ],
        "robot": {
            "name": "CK_NannyBot_MkI",
            "role": "标准保姆机",
            "durability": "1,000",
            "battery": "4,000 W·h（4.8 kWh）",
            "mass": "122 kg",
            "payload": "25 kg",
            "speed": "2.0",
            "special": "三色胸灯、扫帚；无自卫模块，遇敌只撤离；维修需殖民者操作维修机。",
        },
        "delta": "与现有版本相比：工厂、充电、维修三栋建筑分开；机器人耐久/电量沿用当前数值。",
        "materials": "70 / 6 / 1 / 2",
        "total": "550 钢 / 22 零部件 / 4 高级零部件",
    },
    {
        "id": "T2",
        "name": "模块化单元",
        "research": "CK_RobotModular · 模块化机器人",
        "research_cost": "2,000",
        "prereq": "T1 + 精密装配（Fabrication）",
        "tier": "T2 · 单元式",
        "fit": "3–8 台机器人",
        "summary": "工厂升级为 4×3 两工位：骨架与总装分开。维修机升级为自动停靠，不再占用殖民者。",
        "new": [
            "4×3 两工位工厂（骨架 + 总装）",
            "机器人骨架中间件（可备货）",
            "自动停靠维修机",
            "Mk-II 模块化保姆机器人",
        ],
        "reuse": [
            "T1 充电桩（继续沿用，可携带）",
        ],
        "products": [
            ("CK_RobotFactory_T2", "建筑 · 工厂", "260 钢 + 9 零部件 + 2 高级零部件", "6,000 + 9,000 work / 450–1,100 W", "4×3，固定；两条账单。"),
            ("CK_RobotFrame", "中间件 · 骨架", "40 钢 + 3 零部件", "6,000 work / 25 kg", "可堆叠 10；骨架可提前备货。"),
            ("CK_RobotRepairBay_Auto", "建筑 · 维修机", "180 钢 + 6 零部件 + 1 高级零部件", "1,200 tick / 250 耐久 / 600 W", "2×2，固定；机器人停靠自动维修。"),
            ("CK_NannyBot_MkII", "机器人 · Mk-II", "64 钢 + 7 零部件 + 1 高级零部件 + 4 塑钢", "4.8 kWh / 116 kg", "模块型；22 kg 载重；1250 耐久。"),
        ],
        "robot": {
            "name": "CK_NannyBot_MkII",
            "role": "模块化保姆机",
            "durability": "1,250",
            "battery": "4,800 W·h（4.8 kWh）",
            "mass": "116 kg",
            "payload": "22 kg",
            "speed": "2.1",
            "special": "骨架/总装分离；自动维修；无自卫模块，遇敌只撤离。",
        },
        "delta": "比 T1：+250 耐久、+800 W 电池、+0.1 移速；维修从人工改为自动。",
        "materials": "64 / 7 / 1 / 4",
        "total": "584 钢 / 24 零部件 / 4 高级零部件",
    },
    {
        "id": "T3",
        "name": "流水线生产",
        "research": "CK_RobotAssemblyLine · 流水线",
        "research_cost": "3,200",
        "prereq": "T2 + 精密装配（Fabrication）",
        "tier": "T3 · 流水线",
        "fit": "8+ 台机器人",
        "summary": "拆成 2×2 骨架厂 + 4×2 总装线，骨架与总装可并行/备货。机器人升级为重载 Mk-III。",
        "new": [
            "2×2 骨架厂",
            "4×2 总装线",
            "重载维修机",
            "Mk-III 重载保姆机器人",
            "Mk-III 基础自卫模块（电弧焊枪）",
        ],
        "reuse": [
            "T1/T2 充电桩",
            "T2 自动维修逻辑（换到重载维修机）",
        ],
        "products": [
            ("CK_RobotFrameFab", "建筑 · 骨架厂", "140 钢 + 4 零部件 + 1 高级零部件", "6,000 work / 500 W", "2×2，固定；批量骨架。"),
            ("CK_RobotAssemblyLine", "建筑 · 总装线", "240 钢 + 8 零部件 + 2 高级零部件", "12,000 work / 1,300 W", "4×2，固定；总装 Mk-III。"),
            ("CK_RobotRepairBay_Heavy", "建筑 · 维修机", "220 钢 + 7 零部件 + 2 高级零部件", "600 tick / 250 耐久 / 1,000 W", "2×2，固定；殖民者操作。"),
            ("CK_NannyBot_MkIII", "机器人 · Mk-III", "95 钢 + 8 零部件 + 2 高级零部件 + 8 塑钢（不含自卫模块）", "6.4 kWh / 146 kg", "重载型；35 kg 载重；1600 耐久。"),
            ("CK_RobotDefense_ArcWelder", "模块 · 自卫", "20 钢 + 2 零部件 + 1 高级零部件 + 2 塑钢", "1.2 s 冷却 / +4 kg", "电弧焊枪；7 伤害；1.2 格；眩晕 30 tick。"),
        ],
        "robot": {
            "name": "CK_NannyBot_MkIII",
            "role": "重载保姆机",
            "durability": "1,600",
            "battery": "6,400 W·h（6.4 kWh）",
            "mass": "150 kg（含自卫模块）",
            "payload": "33 kg",
            "speed": "1.8",
            "special": "重载履带；电弧焊枪基本反击；不主动索敌。",
        },
        "delta": "比 T2：+350 耐久、+1,600 W 电池、+11 kg 载重；-0.3 移速；新增电弧焊枪自卫模块（+4 kg）。",
        "materials": "115 / 10 / 3 / 10",
        "total": "795 钢 / 31 零部件 / 8 高级零部件",
        "combat": {
            "name": "基础自卫 · 电弧焊枪",
            "type": "近战",
            "damage": "7",
            "range": "1.2 格",
            "cooldown": "1.2 s",
            "stun": "30 tick",
            "armor": "无",
            "module": "CK_RobotDefense_ArcWelder",
            "module_cost": "20 钢 + 2 零部件 + 1 高级零部件 + 2 塑钢",
            "module_mass": "4 kg",
            "rule": "只在被攻击或敌人进入 2 格时反击；不追击超过 6 格。",
        },
    },
    {
        "id": "T4",
        "name": "现场保障",
        "research": "CK_RobotFieldSupport · 现场保障",
        "research_cost": "2,600",
        "prereq": "T3 + 微电子基础（MicroelectronicsBasics）",
        "tier": "T4 · 预制/现场",
        "fit": "多前哨 / 远征基地",
        "summary": "不是单纯数值升级，而是系统集成特化：工厂回到 3×3 预制模式，充电桩做成更轻的现场型号；机器人变轻、变快，适合随队。",
        "new": [
            "3×3 预制工厂",
            "轻量现场充电桩",
            "现场维修机",
            "Mk-IV 前哨保姆机器人",
            "Mk-IV 现场自卫模块（电击棒 + 轻甲）",
        ],
        "reuse": [
            "T2 自动维修逻辑（可选）",
            "T3 骨架中间件（可作为预制模块）",
        ],
        "products": [
            ("CK_RobotFactory_T4", "建筑 · 工厂", "200 钢 + 7 零部件 + 2 高级零部件", "10,000 work / 300–800 W", "3×3，固定；预制总装。"),
            ("CK_RobotCharger_Light", "建筑 · 充电桩", "60 钢 + 2 零部件", "50–4,000 W / 35 kg", "1×1，可携带；比 T1 更便宜。"),
            ("CK_RobotRepairBay_Field", "建筑 · 维修机", "180 钢 + 6 零部件 + 1 高级零部件", "600 tick / 250 耐久 / 900 W", "2×2，固定；殖民者操作；不提供野外维修。"),
            ("CK_NannyBot_MkIV", "机器人 · Mk-IV", "52 钢 + 5 零部件 + 1 高级零部件 + 2 塑钢（不含自卫模块）", "3.6 kWh / 96 kg", "轻量型；18 kg 载重；900 耐久。"),
            ("CK_RobotDefense_ShockBaton", "模块 · 自卫", "18 钢 + 3 零部件 + 1 高级零部件 + 3 塑钢", "1.5 s 冷却 / +3 kg", "电击棒；9 伤害；眩晕 60 tick；+0.08 锐器/+0.04 钝器护甲。"),
        ],
        "robot": {
            "name": "CK_NannyBot_MkIV",
            "role": "前哨保姆机",
            "durability": "900",
            "battery": "3,600 W·h（3.6 kWh）",
            "mass": "99 kg（含自卫模块）",
            "payload": "17 kg",
            "speed": "2.4",
            "special": "轻量、省电、移速最高；电击棒 + 轻甲；大修必须回主基地。",
        },
        "delta": "比 T3：-700 耐久、-2,800 W 电池、-16 kg 载重；+0.6 移速；新增现场自卫模块（+3 kg）。",
        "materials": "70 / 8 / 2 / 5",
        "total": "510 钢 / 23 零部件 / 5 高级零部件",
        "combat": {
            "name": "现场自卫 · 电击棒 + 轻甲",
            "type": "近战",
            "damage": "9",
            "range": "1.2 格",
            "cooldown": "1.5 s",
            "stun": "60 tick",
            "armor": "+0.08 锐器 / +0.04 钝器",
            "module": "CK_RobotDefense_ShockBaton",
            "module_cost": "18 钢 + 3 零部件 + 1 高级零部件 + 3 塑钢",
            "module_mass": "3 kg",
            "rule": "被攻击或 2 格内有敌人才反击；血量 <30% 时优先撤离。",
        },
    },
    {
        "id": "T5",
        "name": "全自动柔性制造",
        "research": "CK_RobotAutomation · 机器人自动化",
        "research_cost": "5,000",
        "prereq": "T4 + 精密装配（Fabrication）",
        "tier": "T5 · FMS",
        "fit": "10+ 台机器人 / 后期",
        "summary": "4×4 自动工厂 + 自动维修机，不需要殖民者。代价是 AI 核心、4 个高级零部件和 1.5 kW 级功率。",
        "new": [
            "4×4 自动工厂",
            "高速自动维修机",
            "Mk-V 高配保姆机器人",
            "Mk-V 自动防卫模块（电弧发射器）",
        ],
        "reuse": [
            "T1/T4 充电桩",
            "T2 自动维修逻辑（升级版）",
        ],
        "products": [
            ("CK_RobotAutofactory", "建筑 · 工厂", "420 钢 + 14 零部件 + 4 高级零部件 + 1 AI 核心", "30,000 tick / 600–1,500 W", "4×4，固定；无人自动生产。"),
            ("CK_RobotRepairBay_AutoMkII", "建筑 · 维修机", "220 钢 + 8 零部件 + 2 高级零部件", "800 tick / 250 耐久 / 1,500 W", "2×2，固定；自动停靠维修。"),
            ("CK_NannyBot_MkV", "机器人 · Mk-V", "80 钢 + 8 零部件 + 2 高级零部件 + 12 塑钢（不含自卫模块）", "6.0 kWh / 130 kg", "高配型；30 kg 载重；1800 耐久。"),
            ("CK_RobotDefense_ArcEmitter", "模块 · 自卫", "28 钢 + 4 零部件 + 2 高级零部件 + 4 塑钢", "2.5 s 冷却 / +5 kg", "电弧发射器；8×3 伤害；4.9 格；眩晕 30 tick；+0.10 锐器/+0.06 钝器护甲。"),
            ("CK_RobotCharger", "建筑 · 充电桩", "沿用 T1（80 钢 + 2 零部件）", "50–4,000 W", "可携带；自动分配最近空闲桩。"),
        ],
        "robot": {
            "name": "CK_NannyBot_MkV",
            "role": "自维护型",
            "durability": "1,800",
            "battery": "6,000 W·h（6.0 kWh）",
            "mass": "135 kg（含自卫模块）",
            "payload": "28 kg",
            "speed": "2.0",
            "special": "高配复合外壳；电弧发射器 + 近战；自动维修；后期旗舰。",
        },
        "delta": "比 T4：+900 耐久、+2,400 W 电池、+11 kg 载重；-0.4 移速；新增自动防卫模块（+5 kg）。",
        "materials": "108 / 12 / 4 / 16",
        "total": "828 钢 / 36 零部件 / 10 高级零部件 + AI 核心",
        "combat": {
            "name": "自动防卫 · 电弧发射器",
            "type": "远程 + 近战",
            "damage": "8×3 / 近战 8",
            "range": "4.9 格",
            "cooldown": "2.5 s",
            "stun": "30 tick",
            "armor": "+0.10 锐器 / +0.06 钝器",
            "module": "CK_RobotDefense_ArcEmitter",
            "module_cost": "28 钢 + 4 零部件 + 2 高级零部件 + 4 塑钢",
            "module_mass": "5 kg",
            "rule": "只打敌对目标；不穿透墙；被近身后切换近战；血量 <30% 停火撤离。",
        },
    },
]


def esc(value: str) -> str:
    return html.escape(str(value))


def render_tree() -> str:
    nodes = []
    for index, tech in enumerate(TECHS):
        nodes.append(
            f'<button class="tree-node" data-tech="{tech["id"]}" type="button">'
            f'<span class="tree-id">{tech["id"]}</span>'
            f'<span class="tree-name">{esc(tech["name"])}</span>'
            f'<span class="tree-product">{esc(tech["robot"]["name"].replace("CK_NannyBot_", ""))}</span>'
            f'<span class="tree-cost">{tech["research_cost"]} 点</span>'
            f'</button>'
        )
    return '<span class="tree-arrow">→</span>'.join(nodes)


def mode_text(tech: dict) -> str:
    if tech["id"] in ("T1", "T2"):
        return "休闲 / 常规 / 高功耗（三档）"
    return "休闲 / 常规 / 高功耗 / 极限（四档；极限默认关闭，20 次上限）"


def render_tech(tech: dict) -> str:
    products = "".join(
        f"<tr><td>{esc(a)}</td><td>{esc(b)}</td><td>{esc(c)}</td><td>{esc(d)}</td><td>{esc(e)}</td></tr>"
        for a, b, c, d, e in tech["products"]
    )
    new = "".join(f"<li>{esc(item)}</li>" for item in tech["new"])
    reuse = "".join(f"<li>{esc(item)}</li>" for item in tech["reuse"]) or "<li>无（本阶段首次引入）</li>"
    r = tech["robot"]
    combat = tech.get("combat")
    combat_html = ""
    if combat:
        combat_html = f"""
      <div class="combat-strip">
        <strong>反击能力：</strong>{esc(combat["name"])} · {esc(combat["type"])} · {esc(combat["damage"])} 伤害 · {esc(combat["range"])} · {esc(combat["cooldown"])} · 眩晕 {esc(combat["stun"])} · 护甲 {esc(combat["armor"])}
        <br><span>{esc(combat["rule"])}</span>
      </div>
        """
    return f"""
    <section class="panel" data-panel="{tech["id"]}">
      <div class="tier-head">
        <div>
          <p class="eyebrow">{esc(tech["tier"])} · {esc(tech["research"])}</p>
          <h2>{esc(tech["name"])}</h2>
          <p class="lead">{esc(tech["summary"])}</p>
        </div>
        <div class="tier-meta">
          <div><span>研究</span><strong>{esc(tech["research_cost"])} 点</strong></div>
          <div><span>前置</span><strong>{esc(tech["prereq"])}</strong></div>
          <div><span>适用</span><strong>{esc(tech["fit"])}</strong></div>
        </div>
      </div>

      <div class="tier-columns">
        <div>
          <h3>本阶段新增产物</h3>
          <ul class="new-list">{new}</ul>
          <h3>沿用上一阶段</h3>
          <ul class="reuse-list">{reuse}</ul>
        </div>
        <div class="robot-card">
          <p class="eyebrow">本阶段机器人产物</p>
          <h3>{esc(r["name"])}</h3>
          <p class="role">{esc(r["role"])}</p>
          <table class="stats">
            <tr><th>耐久</th><td>{esc(r["durability"])}</td></tr>
            <tr><th>电池</th><td>{esc(r["battery"])}</td></tr>
            <tr><th>整机质量</th><td>{esc(r["mass"])}</td></tr>
            <tr><th>载重</th><td>{esc(r["payload"])}</td></tr>
            <tr><th>移动</th><td>{esc(r["speed"])}</td></tr>
            <tr><th>材料</th><td>{esc(tech["materials"])}</td></tr>
            <tr><th>运作模式</th><td>{esc(mode_text(tech))}</td></tr>
          </table>
          <p class="special">{esc(r["special"])}</p>
        </div>
      </div>
      {combat_html}

      <h3>产物 / 材料 / 功率明细</h3>
      <div class="table-wrap">
        <table class="products">
          <thead><tr><th>产物</th><th>类型</th><th>所需材料</th><th>工作 / 功率</th><th>说明</th></tr></thead>
          <tbody>{products}</tbody>
        </table>
      </div>

      <div class="delta"><strong>与上一级差异：</strong>{esc(tech["delta"])}<br>
        <strong>首台总投入（不含研究）：</strong>{esc(tech["total"])}</div>
    </section>
    """


def render_combat() -> str:
    rows = "".join(
        f"<tr><td><strong>{tech['id']}</strong> {esc(tech['name'])}</td>"
        f"<td>{esc(tech['combat']['name'])}</td><td>{esc(tech['combat']['type'])}</td>"
        f"<td>{esc(tech['combat']['damage'])}</td><td>{esc(tech['combat']['range'])}</td>"
        f"<td>{esc(tech['combat']['cooldown'])}</td><td>{esc(tech['combat']['stun'])}</td>"
        f"<td>{esc(tech['combat']['armor'])}</td><td>{esc(tech['combat']['module_mass'])}</td></tr>"
        for tech in TECHS if tech.get("combat")
    )
    modules = "".join(
        f"<tr><td>{esc(tech['combat']['module'])}</td><td>{esc(tech['combat']['module_cost'])}</td>"
        f"<td>{esc(tech['combat']['module_mass'])}</td><td>{esc(tech['combat']['damage'])} / {esc(tech['combat']['range'])}</td></tr>"
        for tech in TECHS if tech.get("combat")
    )
    return f"""
    <section class="panel" data-panel="C">
      <div class="tier-head">
        <div>
          <p class="eyebrow">T3–T5 · BASIC SELF-DEFENSE</p>
          <h2>反击能力 · 三档自卫</h2>
          <p class="lead">T1/T2 仍然以撤离为主；T3 起给机器人“被打能还手”的最低能力，T4 加轻甲，T5 才允许短程射击。目标是自卫，不是战斗机器人：不主动索敌、不长距离追击、不参与进攻。</p>
        </div>
        <div class="tier-meta">
          <div><span>适用</span><strong>T3 / T4 / T5</strong></div>
          <div><span>默认模式</span><strong>仅反击</strong></div>
          <div><span>主动索敌</span><strong>否</strong></div>
        </div>
      </div>

      <div class="combat-grid">
        <div class="combat-card">
          <p class="eyebrow">T3 · 基础自卫</p>
          <h3>电弧焊枪</h3>
          <p>把维修用的电弧焊枪当近战武器：7 伤害、1.2 格、1.2 秒冷却、眩晕 30 tick。只在被攻击或敌人贴到 2 格内时还手。</p>
          <span class="combat-tag">+4 kg · 20 钢 + 2 零件 + 1 高级 + 2 塑钢</span>
        </div>
        <div class="combat-card">
          <p class="eyebrow">T4 · 现场自卫</p>
          <h3>电击棒 + 轻甲</h3>
          <p>9 伤害、眩晕 60 tick，额外 +0.08 锐器 / +0.04 钝器护甲。血量低于 30% 时优先撤离，不追击。</p>
          <span class="combat-tag">+3 kg · 18 钢 + 3 零件 + 1 高级 + 3 塑钢</span>
        </div>
        <div class="combat-card">
          <p class="eyebrow">T5 · 自动防卫</p>
          <h3>电弧发射器</h3>
          <p>4.9 格、8×3 连发、眩晕 30 tick，近身自动切近战。只打敌对目标，不穿墙，不开火打友军/中立。</p>
          <span class="combat-tag">+5 kg · 28 钢 + 4 零件 + 2 高级 + 4 塑钢</span>
        </div>
      </div>

      <h3>三档对比</h3>
      <div class="table-wrap">
        <table>
          <thead><tr><th>阶段</th><th>模块</th><th>类型</th><th>伤害</th><th>射程</th><th>冷却</th><th>眩晕</th><th>护甲</th><th>增重</th></tr></thead>
          <tbody>{rows}</tbody>
        </table>
      </div>

      <div class="tier-columns">
        <div>
          <h3>交战规则</h3>
          <ul class="new-list">
            <li><strong>只反击：</strong>被攻击，或敌人进入 2 格，才启动；不主动巡逻、不主动索敌。</li>
            <li><strong>不追击：</strong>目标离开 6 格或自身开始撤离，立即脱战。</li>
            <li><strong>血量优先：</strong>耐久/生命低于 30%，或电量低于 10%，停火撤离。</li>
            <li><strong>目标过滤：</strong>只打敌对 Pawn；不打友军、中立、建筑、动物（除非动物敌对）。</li>
            <li><strong>T5 远程限制：</strong>需要视线，不穿墙；冷却 2.5 秒；不打断救援/灭火的紧急判断。</li>
          </ul>
          <h3>模块材料</h3>
          <div class="table-wrap">
            <table>
              <thead><tr><th>模块</th><th>材料</th><th>增重</th><th>伤害 / 射程</th></tr></thead>
              <tbody>{modules}</tbody>
            </table>
          </div>
        </div>
        <div>
          <h3>RimWorld 实现映射</h3>
          <ul class="reuse-list">
            <li><code>HediffDef</code> + <code>HediffCompProperties_VerbGiver</code>：给 Mk-III+ 加 <code>tools</code>（近战）和 T5 的 <code>verbs</code>（远程）。</li>
            <li><code>PawnKindDef.startingHediffs</code>：Mk-III/IV/V 分别带 `CK_RobotDefense_ArcWelder` / `ShockBaton` / `ArcEmitter`。</li>
            <li><code>JobGiver_NannyBotDefend</code>：放在 ThinkTree 的 Flee 之前；只在被攻击/近敌时生成 `AttackMelee` 或 `UseVerbOnThing`。</li>
            <li><code>CompNannyBot</code> 记录 `lastAttackedTick`；<code>selfDefenseMode</code> 默认 Retaliate，可切 Off。</li>
            <li>T5 弹丸 `CK_ArcBolt` 用 `Verb_Shoot`；射程 4.9，三连发，冷却 2.5 秒；只选敌对目标。</li>
            <li>护甲用 Hediff 的 <code>statOffsets</code>，不改基础 ThingDef；T4/T5 才加。</li>
          </ul>
          <h3>平衡与风险</h3>
          <ul class="reuse-list">
            <li>T3 的 7 伤害只是“能还手”，打不过正规枪手；遇到强敌仍要撤。</li>
            <li>T5 远程是后期能力，必须限制目标过滤与冷却，避免自动清图。</li>
            <li>需要测试：友军误伤、墙后目标、救援中开火、断电/低电、维修期间自卫是否关闭。</li>
          </ul>
        </div>
      </div>
    </section>
    """


def render_modes() -> str:
    return """
    <section class="panel" data-panel="M">
      <div class="tier-head">
        <div>
          <p class="eyebrow">T1–T5 · OPERATING MODES</p>
          <h2>运作模式 · 休闲 / 常规 / 高功耗 / 极限</h2>
          <p class="lead">休闲模式就是低功耗档：降低速度与耗电，把单次充电的运行时间从 12 小时拉长到约 34 小时。默认由机器人自动判定当前模式；玩家可随时强制覆盖。T1/T2 只开放前三档；T3+ 解锁极限模式，但极限默认关闭且永远不会被自动选中，只能手动强制。每台机器人只有有限次极限开启次数，默认 20 次，用尽即彻底报废。</p>
        </div>
        <div class="tier-meta">
          <div><span>默认策略</span><strong>自动判定</strong></div>
          <div><span>玩家覆盖</span><strong>可强制</strong></div>
          <div><span>极限自动</span><strong>永不</strong></div>
          <div><span>极限次数</span><strong>20（可调 20–200）</strong></div>
        </div>
      </div>

      <div class="auto-banner">
        <div>
          <p class="eyebrow">AUTO MODE · DEFAULT</p>
          <h3>机器人先自己判断，玩家再强制覆盖</h3>
          <p><strong>空闲 / 低电 → 休闲；普通家务 → 常规；灭火 / 救援 / 战斗 → 高功耗；极限 → 永不自动。</strong> 默认策略是自动；玩家点选某一档后进入“强制模式”，直到点“恢复自动”或触发安全回退。</p>
        </div>
        <div class="auto-steps">
          <span>空闲</span><b>→</b><span class="casual-text">休闲</span>
          <span>工作</span><b>→</b><span class="normal-text">常规</span>
          <span>紧急</span><b>→</b><span class="high-text">高功耗</span>
          <span>极限</span><b>→</b><span class="extreme-text">仅手动</span>
        </div>
      </div>

      <div class="mode-grid">
        <div class="mode-card casual">
          <p class="eyebrow">LOW POWER</p>
          <h3>休闲 / 低功耗</h3>
          <p>慢速做家务、慢速移动，耗电降到 35%。单次充电约 34 小时，适合夜间、待命、低强度巡逻。</p>
          <span class="mode-tag">45% 工作 · 70% 移速 · 35% 耗电 · 34 h</span>
        </div>
        <div class="mode-card normal">
          <p class="eyebrow">NORMAL</p>
          <h3>常规模式</h3>
          <p>标准工作速度与耗电，12 小时续航。没有额外耐久损耗，是日常默认档。</p>
          <span class="mode-tag">100% 工作 · 100% 移速 · 100% 耗电 · 12 h</span>
        </div>
        <div class="mode-card high">
          <p class="eyebrow">HIGH POWER</p>
          <h3>高功耗</h3>
          <p>赶工/灭火/救援时使用；工作速度 +60%，耗电 220%，续航约 5.5 小时。不额外扣耐久。</p>
          <span class="mode-tag">160% 工作 · 115% 移速 · 220% 耗电 · 5.5 h</span>
        </div>
        <div class="mode-card extreme">
          <p class="eyebrow">OVERDRIVE · DEFAULT OFF</p>
          <h3>极限模式</h3>
          <p>超频电机与电池，工作速度 +140%，耗电 450%，续航约 2.7 小时；耐久快速衰减。默认关闭。每台机器人默认只有 20 次开启额度，用完即报废。</p>
          <span class="mode-tag">240% 工作 · 130% 移速 · 450% 耗电 · 2.7 h · 耐久 6/250（T5 4/250）· 20 次</span>
        </div>
      </div>

      <h3>四档参数</h3>
      <div class="table-wrap">
        <table>
          <thead><tr><th>模式</th><th>工作速度</th><th>移速</th><th>耗电</th><th>单次续航</th><th>等效常规工作量</th><th>耐久损耗</th><th>开启次数上限</th><th>自动判定</th><th>可用阶段</th></tr></thead>
          <tbody>
            <tr><td>休闲 / 低功耗</td><td>45%</td><td>70%</td><td>35%</td><td>约 34 h</td><td>约 15.4 h</td><td>0</td><td>不适用</td><td>空闲 / 低电</td><td>T1+</td></tr>
            <tr><td>常规</td><td>100%</td><td>100%</td><td>100%</td><td>12 h</td><td>12 h</td><td>0</td><td>不适用</td><td>普通家务</td><td>T1+</td></tr>
            <tr><td>高功耗</td><td>160%</td><td>115%</td><td>220%</td><td>约 5.5 h</td><td>约 8.8 h</td><td>0</td><td>不适用</td><td>灭火 / 救援 / 战斗</td><td>T1+</td></tr>
            <tr><td>极限</td><td>240%</td><td>130%</td><td>450%</td><td>约 2.7 h</td><td>约 6.5 h</td><td>T3/T4：6/250 tick<br>T5：4/250 tick</td><td>每台 20 次（可调 20–200）</td><td><strong>永不自动</strong></td><td>T3+，设置页允许，手动强制</td></tr>
          </tbody>
        </table>
      </div>

      <div class="tier-columns">
        <div>
          <h3>设置页开关</h3>
          <div class="settings-mock">
            <p class="eyebrow">设置 → 机器人 → 运作模式</p>
            <label><input type="checkbox"> 允许极限模式（默认关闭）</label>
            <p>只对 T3+ 生效。关闭时：极限按钮灰掉；正在极限的机器人立即切回高功耗。</p>
            <div class="limit-row">
              <span>每台极限次数上限</span>
              <input type="range" min="20" max="200" step="5" value="20">
              <output>20 次</output>
            </div>
            <p>范围 20–200，默认 20；步长 5。降低到低于某台已用次数时，会先弹确认，确认后该机器人立即报废。</p>
            <label><input type="checkbox" checked> 低电量自动切休闲（默认开启，阈值 15%）</label>
            <label><input type="checkbox" checked> 极限耐久保护（默认开启，耐久 20% 自动退出）</label>
            <label><input type="checkbox" checked> 充电/维修期间暂停模式加成（默认开启）</label>
          </div>
          <h3>机器人 Gizmo（示意）</h3>
          <div class="mode-row">
            <span>当前模式</span>
            <button type="button">休闲</button>
            <button class="active" type="button">常规</button>
            <button type="button">高功耗</button>
            <button disabled type="button">极限 8/20 🔒</button>
          </div>
          <p class="mode-note">极限按钮在设置关闭、T3 以下或次数用尽时灰掉；打开设置后，T3+ 机器人才能选择。按钮上显示剩余/上限。</p>
        </div>
        <div>
          <h3>自动规则</h3>
          <ul class="new-list">
            <li><strong>默认常规：</strong>新造/读档机器人没有记录时一律普通模式。</li>
            <li><strong>低电降档：</strong>电量 ≤15% 自动切休闲；≤5% 停止工作去充电。</li>
            <li><strong>极限保护：</strong>耐久 ≤20% 自动退出极限并报警；≤10% 强制休闲。</li>
            <li><strong>次数计数：</strong>进入极限并持续 1 秒才计 1 次；同一轮反复点不算；第 20 次仍可完成当前极限周期，退出极限/充电/维修/低电时执行报废。</li>
            <li><strong>彻底报废：</strong>达到次数上限即不可维修、不可复位；拆成 40 钢 + 3 零部件，只能由工厂重造。</li>
            <li><strong>降低上限：</strong>新上限低于某台已用次数时先弹确认；确认后超限机器人立即报废，取消则回滚设置。</li>
            <li><strong>设置关闭：</strong>全局关闭极限时，所有正在极限的机器人立即切回高功耗，不扣额外耐久。</li>
            <li><strong>充电/维修：</strong>模式加成暂停；充电功率不受模式影响；维修期间不扣极限耐久。</li>
            <li><strong>战斗：</strong>被攻击时允许用当前模式反击；血量 <30% 强制退出极限并撤离。</li>
          </ul>
          <h3>耐久账</h3>
          <div class="formula">
            <p>T3/T4：6 耐久 / 250 tick；连续极限约 41,700 tick ≈ 0.69 游戏日耗尽 1000 耐久。</p>
            <p>T5：4 耐久 / 250 tick；连续极限约 62,500 tick ≈ 1.04 游戏日耗尽 1000 耐久。</p>
            <p>按 2.7 小时续航计算：T3/T4 一个完整极限充电周期约损失 233 耐久；T5 约损失 155 耐久。</p>
            <p><strong>硬上限：</strong>即使耐久还有剩余，每台机器人默认只有 20 次极限开启；用尽后直接报废，维修机无法修复。</p>
          </div>
        </div>
      </div>

      <div class="tier-columns">
        <div>
          <h3>平衡口径</h3>
          <ul class="reuse-list">
            <li>休闲不是停机：仍然接家务，只是慢；想完全待机可同时暂停家务。</li>
            <li>低功耗的“延长时间”来自耗电 35%，不是免费电量；单次 34 小时，等效常规工作量约 15.4 小时。</li>
            <li>高功耗是“更快但更费电”，不扣耐久；极限是“最快但伤机器”，默认关闭。</li>
            <li>极限不改变充电功率；充满仍需约 4 小时，不能靠反复插拔免费续命。</li>
            <li>20 次是硬寿命上限：不是“修满耐久就能无限用”；极限次数用尽后维修机也救不回来。</li>
          </ul>
        </div>
        <div>
          <h3>RimWorld 实现映射</h3>
          <ul class="impl">
            <li><code>RoboticsSettings.extremeModeEnabled</code> 默认 false；设置页加“允许极限模式”复选框。</li>
            <li><code>RoboticsSettings.extremeUseLimit</code> 默认 20，范围 20–200，步长 5；设置页滑块 + 确认对话框。</li>
            <li><code>CompNannyBot.overdriveUses</code> 每台独立计数；进入极限 60 tick 后 +1；达到上限后标记 <code>overdriveSpent</code>，退出极限时 <code>Scrap()</code>。</li>
            <li>报废路径：不可维修、不可复位、不保留“已用次数”到新机器人；拆成 40 钢 + 3 零部件。</li>
            <li><code>CompNannyBot.operatingMode</code> 枚举 + Scribe；默认 Normal；旧存档无字段按 Normal。</li>
            <li><code>Need_RobotEnergy</code> 按模式乘耗电系数；<code>CompNannyBot.CompTick</code> 在极限时扣耐久。</li>
            <li>工作/移速用 <code>HediffDef</code> 的 <code>statFactors</code>（WorkSpeedGlobal / MoveSpeed）；模式切换时增删 Hediff。</li>
            <li>Gizmo 四按钮；极限按钮显示“剩余/上限”，<code>Disabled</code> 并给 disabledReason；设置关闭/次数用尽时强制切回 High。</li>
            <li>验收：默认关闭、T1/T2 无极限、T3+ 打开后可用、关闭设置立即降档、低电/低耐久自动降档、充电/维修暂停。</li>
          </ul>
        </div>
      </div>

      <div class="note"><strong>名称说明：</strong>你列了“低功耗、常规、高功耗、极限”四个名称；这里按四档实现，其中“休闲模式”=低功耗档。T1/T2 先开放前三档，T3+ 解锁极限；极限默认关闭。如果你确实只想保留三档，把“休闲”并入“低功耗”即可。</div>
    </section>
    """


def main():
    tabs = []
    panels = []
    for index, tech in enumerate(TECHS):
        active = " active" if index == 0 else ""
        tabs.append(
            f'<button class="tab{active}" data-tab="{tech["id"]}" type="button">'
            f'<span class="tab-id">{tech["id"]}</span><span>{esc(tech["name"])}</span>'
            f'</button>'
        )
        panel = render_tech(tech).replace('<section class="panel"', f'<section class="panel{active}"', 1)
        panels.append(panel)

    tabs.append('<button class="tab" data-tab="C" type="button"><span class="tab-id">⚔</span><span>反击能力</span></button>')
    panels.append(render_combat())
    tabs.append('<button class="tab" data-tab="M" type="button"><span class="tab-id">◐</span><span>运作模式</span></button>')
    panels.append(render_modes())

    summary_rows = "".join(
        f"<tr><td><strong>{tech['id']}</strong> {esc(tech['name'])}</td><td>{esc(tech['research'])}</td><td>{esc(tech['research_cost'])}</td>"
        f"<td>{esc(tech['robot']['name'])}</td><td>{esc(tech['robot']['durability'])}</td><td>{esc(tech['robot']['battery'])}</td>"
        f"<td>{esc(tech['robot']['mass'])} / {esc(tech['robot']['payload'])}</td><td>{esc(tech['materials'])}</td>"
        f"<td>{esc(tech['combat']['name']) if tech.get('combat') else '无（仅撤离）'}</td>"
        f"<td>{esc(mode_text(tech))}</td></tr>"
        for tech in TECHS
    )

    css = """
:root { color-scheme:dark; --bg:#0d1317; --panel:#151d22; --panel2:#1b252b; --line:#2c3d46; --text:#e9f3f2; --muted:#9cafb2; --teal:#66d2c9; --amber:#eab35f; --red:#e17b6b; --green:#8dcc91; }
* { box-sizing:border-box; }
body { margin:0; background:radial-gradient(900px 500px at 8% -10%, rgba(48,135,134,.22), transparent 65%), radial-gradient(700px 420px at 95% 4%, rgba(196,139,67,.17), transparent 70%), var(--bg); color:var(--text); font-family:"Segoe UI","Microsoft YaHei","PingFang SC",system-ui,sans-serif; }
.wrap { max-width:1360px; margin:0 auto; padding:28px 22px 54px; }
.eyebrow { margin:0 0 7px; color:var(--teal); font-size:11px; letter-spacing:.16em; text-transform:uppercase; }
h1 { margin:0; font-size:clamp(28px,3.4vw,46px); letter-spacing:-.03em; }
.sub { color:var(--muted); line-height:1.7; max-width:980px; margin:10px 0 0; }
.banner { margin-top:14px; border:1px solid rgba(234,179,95,.38); background:rgba(234,179,95,.06); border-radius:12px; padding:12px 14px; color:var(--muted); font-size:12px; line-height:1.65; }
.tree { display:flex; align-items:stretch; gap:8px; overflow-x:auto; padding:14px 12px; margin:18px 0 6px; border:1px solid var(--line); border-radius:16px; background:rgba(0,0,0,.16); }
.tree-node { flex:1 0 168px; border:1px solid var(--line); background:var(--panel2); border-radius:12px; padding:11px 10px; color:var(--text); text-align:left; cursor:pointer; font:inherit; display:flex; flex-direction:column; gap:3px; }
.tree-node:hover { border-color:var(--teal); transform:translateY(-1px); }
.tree-node.active { border-color:var(--teal); background:rgba(102,210,201,.12); box-shadow:inset 0 0 0 1px rgba(102,210,201,.18); }
.tree-id { color:var(--amber); font-size:11px; font-weight:800; letter-spacing:.08em; }
.tree-name { font-size:14px; font-weight:700; }
.tree-product { color:var(--teal); font-size:11px; }
.tree-cost { color:var(--muted); font-size:10px; }
.tree-arrow { align-self:center; color:var(--amber); font-weight:800; }
.tabs { display:flex; gap:6px; flex-wrap:wrap; padding:8px; background:var(--panel); border:1px solid var(--line); border-radius:16px; margin-top:14px; }
.tab { border:1px solid transparent; background:transparent; color:var(--muted); border-radius:10px; padding:9px 11px; display:flex; align-items:center; gap:7px; cursor:pointer; font:inherit; }
.tab:hover { background:var(--panel2); color:var(--text); }
.tab.active { border-color:var(--teal); background:var(--panel2); color:var(--text); }
.tab-id { color:var(--amber); font-size:11px; font-weight:800; }
.panel { display:none; margin-top:14px; background:var(--panel); border:1px solid var(--line); border-radius:20px; padding:22px; }
.panel.active { display:block; animation:fade .18s ease; }
@keyframes fade { from { opacity:0; transform:translateY(4px); } to { opacity:1; transform:none; } }
.tier-head { display:flex; justify-content:space-between; align-items:flex-start; gap:20px; }
h2 { margin:0; font-size:clamp(24px,3vw,34px); letter-spacing:-.02em; }
.lead { margin:9px 0 0; color:var(--muted); line-height:1.7; max-width:860px; }
.tier-meta { flex:0 0 280px; border:1px solid var(--line); border-radius:12px; background:var(--panel2); padding:11px 12px; }
.tier-meta div { display:flex; justify-content:space-between; gap:12px; padding:4px 0; border-bottom:1px solid rgba(80,110,120,.18); font-size:12px; }
.tier-meta div:last-child { border-bottom:0; }
.tier-meta span { color:var(--muted); }
.tier-meta strong { text-align:right; font-weight:600; }
.tier-columns { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin:18px 0 6px; }
h3 { margin:18px 0 9px; font-size:15px; color:var(--teal); }
.new-list, .reuse-list { margin:0; padding-left:18px; color:var(--muted); font-size:12px; line-height:1.7; }
.new-list li::marker { color:var(--green); }
.reuse-list li::marker { color:var(--teal); }
.robot-card { border:1px solid rgba(102,210,201,.35); background:linear-gradient(150deg,rgba(102,210,201,.08),rgba(0,0,0,.12)); border-radius:14px; padding:14px; }
.robot-card h3 { margin:0 0 2px; color:var(--text); font-size:18px; }
.role { margin:0 0 10px; color:var(--amber); font-size:12px; }
.stats { border-collapse:collapse; width:100%; font-size:12px; }
.stats th, .stats td { text-align:left; padding:5px 0; border-bottom:1px solid rgba(80,110,120,.18); }
.stats th { color:var(--muted); font-weight:500; width:36%; }
.special { margin:10px 0 0; color:var(--muted); font-size:12px; line-height:1.6; }
.table-wrap { overflow-x:auto; border:1px solid var(--line); border-radius:12px; }
.table-wrap table { min-width:900px; }
table { border-collapse:collapse; width:100%; font-size:12px; }
th, td { text-align:left; padding:9px 10px; border-bottom:1px solid rgba(80,110,120,.22); vertical-align:top; }
th { color:var(--muted); font-weight:600; background:rgba(0,0,0,.16); white-space:nowrap; }
tbody tr:hover { background:rgba(102,210,201,.045); }
.delta { margin-top:14px; border:1px solid var(--line); border-radius:12px; padding:11px 13px; color:var(--muted); font-size:12px; line-height:1.7; }
.delta strong { color:var(--text); }
.combat-grid { display:grid; grid-template-columns:repeat(3,1fr); gap:12px; margin:14px 0 4px; }
.combat-card { border:1px solid rgba(224,123,107,.35); background:linear-gradient(150deg,rgba(224,123,107,.10),rgba(0,0,0,.12)); border-radius:14px; padding:13px; }
.combat-card h3 { margin:0 0 7px; color:var(--text); font-size:17px; }
.combat-card p { margin:0; color:var(--muted); font-size:12px; line-height:1.65; }
.combat-tag { display:inline-block; margin-top:9px; border:1px solid rgba(234,179,95,.35); color:var(--amber); border-radius:999px; padding:4px 8px; font-size:10px; }
.combat-strip { margin-top:14px; border:1px solid rgba(224,123,107,.35); background:rgba(224,123,107,.06); border-radius:12px; padding:11px 13px; font-size:12px; line-height:1.65; color:var(--muted); }
.combat-strip strong { color:var(--text); }
.auto-banner { display:flex; justify-content:space-between; align-items:center; gap:18px; border:1px solid rgba(102,210,201,.4); background:linear-gradient(120deg,rgba(102,210,201,.10),rgba(0,0,0,.12)); border-radius:14px; padding:13px 15px; margin:16px 0 4px; }
.auto-banner h3 { margin:0 0 5px; color:var(--text); font-size:17px; }
.auto-banner p { margin:0; color:var(--muted); font-size:12px; line-height:1.65; max-width:760px; }
.auto-banner p strong { color:var(--text); }
.auto-steps { display:flex; gap:6px; align-items:center; flex-wrap:wrap; justify-content:flex-end; font-size:11px; }
.auto-steps span { border:1px solid var(--line); border-radius:999px; padding:4px 7px; color:var(--muted); }
.auto-steps b { color:var(--amber); }
.casual-text { color:var(--teal) !important; border-color:rgba(102,210,201,.4) !important; }
.normal-text { color:var(--green) !important; border-color:rgba(141,204,145,.4) !important; }
.high-text { color:var(--amber) !important; border-color:rgba(234,179,95,.4) !important; }
.extreme-text { color:var(--red) !important; border-color:rgba(224,123,107,.4) !important; }
.mode-grid { display:grid; grid-template-columns:repeat(4,1fr); gap:10px; margin:14px 0 4px; }
.mode-card { border:1px solid var(--line); background:var(--panel2); border-radius:14px; padding:13px; }
.mode-card.casual { border-top:3px solid var(--teal); }
.mode-card.normal { border-top:3px solid var(--green); }
.mode-card.high { border-top:3px solid var(--amber); }
.mode-card.extreme { border-top:3px solid var(--red); background:linear-gradient(150deg,rgba(224,123,107,.10),rgba(0,0,0,.12)); }
.mode-card h3 { margin:0 0 7px; color:var(--text); font-size:16px; }
.mode-card p { margin:0; color:var(--muted); font-size:12px; line-height:1.65; }
.mode-tag { display:inline-block; margin-top:9px; border:1px solid var(--line); color:var(--muted); border-radius:999px; padding:4px 7px; font-size:10px; }
.mode-card.extreme .mode-tag { border-color:rgba(224,123,107,.4); color:var(--red); }
.settings-mock { border:1px solid var(--line); border-radius:12px; background:var(--panel2); padding:12px; }
.settings-mock label { display:block; color:var(--muted); font-size:12px; padding:5px 0; }
.settings-mock input { accent-color:var(--teal); margin-right:7px; }
.settings-mock p { margin:5px 0 9px; color:var(--muted); font-size:11px; line-height:1.6; }
.limit-row { display:flex; align-items:center; gap:8px; padding:7px 0; color:var(--muted); font-size:11px; }
.limit-row input[type="range"] { flex:1; accent-color:var(--red); min-width:70px; }
.limit-row output { min-width:58px; text-align:right; color:var(--red); font-weight:700; font-size:12px; }
.mode-row { display:flex; gap:6px; align-items:center; flex-wrap:wrap; padding:10px; border:1px solid var(--line); border-radius:12px; background:var(--panel2); }
.mode-row span { color:var(--muted); font-size:11px; margin-right:3px; }
.mode-row button { border:1px solid var(--line); background:#0d151a; color:var(--muted); border-radius:8px; padding:6px 9px; font:inherit; font-size:11px; cursor:pointer; }
.mode-row button.active { border-color:var(--teal); color:var(--teal); background:rgba(102,210,201,.10); }
.mode-row button:disabled { opacity:.4; cursor:not-allowed; }
.mode-note { color:var(--muted); font-size:11px; line-height:1.6; margin:8px 0 0; }
.formula { border:1px solid var(--line); border-radius:12px; padding:10px 12px; color:var(--muted); font-size:12px; line-height:1.7; margin-bottom:10px; }
.formula p { margin:4px 0; }
.summary { margin-top:18px; }
.summary h3 { margin-top:0; }
.note { margin-top:14px; border:1px solid rgba(234,179,95,.35); border-radius:12px; background:rgba(234,179,95,.06); padding:12px 14px; color:var(--muted); font-size:12px; line-height:1.65; }
@media (max-width:900px) { .tier-head, .tier-columns, .mode-grid, .auto-banner { grid-template-columns:1fr; display:block; } .mode-card { margin-bottom:8px; } .tier-meta, .auto-steps { margin-top:12px; justify-content:flex-start; } }
"""

    body = f"""
<div class="wrap">
  <header>
    <p class="eyebrow">ColonyKit · Robotics Tech Tree</p>
    <h1>机器人科技树 · 五级技术产物</h1>
    <p class="sub">5 种工厂/充电/维修方案全部追加，不互斥；按 T1→T5 作为五个技术阶段。每个阶段有独立的机器人工厂、机器人等级、维修能力和材料表；旧阶段保留可建造，方便低科技基地和旧存档。</p>
    <div class="banner"><strong>追加原则：</strong>不删除旧方案。T1 工坊式 → T2 单元式 → T3 流水线 → T4 现场保障 → T5 全自动；研究逐级前置，建筑和机器人按阶段解锁。可在设置里开启“只显示最高级建筑”减少建造菜单。<br><strong>运作模式：</strong>休闲（低功耗）/ 常规 / 高功耗对所有阶段开放；极限模式只对 T3+ 开放且默认关闭，需在设置页手动允许。每台机器人默认只有 20 次极限开启额度（可调 20–200），用尽即彻底报废。<br><strong>旧存档兼容：</strong>现有 `CK_NannyBot` 研究映射为 T1；旧一体桩保留为 legacy，现有机器人按 Mk-I 处理，`homeStation` 迁移到 `homeCharger`。</div>
  </header>

  <div class="tree">{render_tree()}</div>

  <nav class="tabs" role="tablist" aria-label="科技阶段">
    {''.join(tabs)}
  </nav>

  {''.join(panels)}

  <div class="summary">
    <h3>五级横向总表</h3>
    <div class="table-wrap">
      <table>
        <thead><tr><th>阶段</th><th>研究</th><th>研究点</th><th>机器人产物</th><th>耐久</th><th>电池</th><th>质量 / 载重</th><th>材料 钢/零件/高级/塑钢</th><th>反击能力</th><th>运作模式</th></tr></thead>
        <tbody>{summary_rows}</tbody>
      </table>
    </div>
    <div class="note"><strong>研究总量：</strong>T1–T5 合计 14,000 研究点（不含前置的机械加工/精密装配/微电子基础）。<br><strong>材料口径：</strong>表中机器人物料为“新增产物”的直接材料，不含工厂/充电桩/维修机的一次性投入；首台总投入见各阶段页。质量与配重沿用工程口径：电池低置、载重 ≤ 整机 25%、倾覆角 ≥ 35°、接地压力 ≤ 10 kPa。</div>
  </div>
</div>
<script>
const tabs = Array.from(document.querySelectorAll('.tab'));
const panels = Array.from(document.querySelectorAll('.panel'));
const treeNodes = Array.from(document.querySelectorAll('.tree-node'));
function show(id) {{
  tabs.forEach((tab) => tab.classList.toggle('active', tab.dataset.tab === id));
  panels.forEach((panel) => panel.classList.toggle('active', panel.dataset.panel === id));
  treeNodes.forEach((node) => node.classList.toggle('active', node.dataset.tech === id));
}}
tabs.forEach((tab) => tab.addEventListener('click', () => show(tab.dataset.tab)));
treeNodes.forEach((node) => node.addEventListener('click', () => show(node.dataset.tech)));
document.addEventListener('keydown', (event) => {{
  const active = tabs.find((tab) => tab.classList.contains('active'));
  const index = tabs.indexOf(active);
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
<title>机器人科技树 · 五级技术产物</title>
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
