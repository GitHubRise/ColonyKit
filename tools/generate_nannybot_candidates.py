"""Generate ten nanny-robot texture candidates and a self-contained HTML chooser.

Run from the ColonyKit repo root:
    python tools/generate_nannybot_candidates.py
"""

from __future__ import annotations

import base64
import html
import io
import math
import os
from dataclasses import dataclass, field
from typing import Iterable

from PIL import Image, ImageDraw, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "docs", "nannybot-textures")
HTML_PATH = os.path.join(ROOT, "docs", "nannybot-textures.html")

SCALE = 4
SIZE = 128
VIEWS = ("south", "north", "east", "west")

OUTLINE = (35, 42, 52, 255)
WHITE = (246, 249, 252, 255)


def P(value: float) -> int:
    return int(round(value * SCALE))


def rr(draw: ImageDraw.ImageDraw, box, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def ellipse(draw: ImageDraw.ImageDraw, box, fill, outline=None, width=1):
    draw.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(draw: ImageDraw.ImageDraw, points, fill, outline=None):
    draw.polygon(points, fill=fill, outline=outline)


@dataclass(frozen=True)
class Style:
    slug: str
    number: str
    name: str
    subtitle: str
    description: str
    shell: tuple
    shell_dark: tuple
    accent: tuple
    accent_light: tuple
    tread: tuple
    body: str
    head: str
    base: str
    antenna: str
    badge: str
    tool: str = ""
    eyes: str = "round"
    tags: tuple = field(default_factory=tuple)


STYLES = [
    Style(
        slug="classic",
        number="01",
        name="经典白蓝",
        subtitle="最初的保姆机",
        description="白色圆壳、蓝色胸板与三色状态灯，拿扫帚。最接近现在的默认保姆机器人，辨识度最高。",
        shell=(224, 230, 238, 255),
        shell_dark=(182, 193, 207, 255),
        accent=(61, 126, 166, 255),
        accent_light=(110, 190, 225, 255),
        tread=(58, 66, 78, 255),
        body="round",
        head="dome",
        base="treads",
        antenna="ball",
        badge="lights",
        tool="broom",
        tags=("默认", "白蓝", "扫帚"),
    ),
    Style(
        slug="nurse",
        number="02",
        name="粉白护士",
        subtitle="温柔照护型",
        description="奶白外壳配玫瑰粉胸板，头顶护士帽与十字徽章。适合送餐、照顾囚犯和救援定位。",
        shell=(248, 235, 238, 255),
        shell_dark=(224, 194, 202, 255),
        accent=(205, 95, 126, 255),
        accent_light=(248, 170, 190, 255),
        tread=(83, 69, 78, 255),
        body="round",
        head="dome",
        base="treads",
        antenna="cross",
        badge="cross",
        eyes="soft",
        tags=("护士", "粉色", "十字"),
    ),
    Style(
        slug="gardener",
        number="03",
        name="薄荷园丁",
        subtitle="庭院与温室",
        description="奶油白配薄荷绿，叶片天线和浇水壶。适合温室、花园和需要频繁清洁的区域。",
        shell=(232, 238, 220, 255),
        shell_dark=(186, 204, 172, 255),
        accent=(83, 145, 104, 255),
        accent_light=(151, 205, 151, 255),
        tread=(67, 78, 65, 255),
        body="round",
        head="dome",
        base="treads",
        antenna="leaf",
        badge="leaf",
        tool="watering",
        tags=("园丁", "薄荷", "浇水壶"),
    ),
    Style(
        slug="industrial",
        number="04",
        name="工业黄黑",
        subtitle="重活与维修",
        description="工程黄外壳、深灰护甲和警示条纹，方头护目镜。给发电机加燃料、屠宰和搬运时更醒目。",
        shell=(238, 184, 66, 255),
        shell_dark=(164, 131, 63, 255),
        accent=(48, 57, 66, 255),
        accent_light=(248, 213, 92, 255),
        tread=(43, 48, 55, 255),
        body="box",
        head="box",
        base="treads",
        antenna="warning",
        badge="hazard",
        tool="wrench",
        tags=("工业", "黄黑", "扳手"),
    ),
    Style(
        slug="stealth",
        number="05",
        name="暗夜低语",
        subtitle="静音与夜巡",
        description="炭黑机身、青色窄护目镜、悬浮底座。轮廓最紧凑，适合夜间巡逻和危险区域。",
        shell=(69, 78, 92, 255),
        shell_dark=(43, 50, 62, 255),
        accent=(52, 166, 188, 255),
        accent_light=(114, 229, 240, 255),
        tread=(28, 33, 42, 255),
        body="slim",
        head="visor",
        base="hover",
        antenna="fin",
        badge="line",
        eyes="visor",
        tags=("暗夜", "青色", "悬浮"),
    ),
    Style(
        slug="steampunk",
        number="06",
        name="蒸汽铜锈",
        subtitle="黄铜与齿轮",
        description="铜锈色圆壳、黄铜铆钉和压力表，经典履带。像从旧工坊里走出来的机械管家。",
        shell=(188, 125, 85, 255),
        shell_dark=(132, 85, 62, 255),
        accent=(215, 166, 84, 255),
        accent_light=(244, 204, 124, 255),
        tread=(63, 48, 42, 255),
        body="round",
        head="dome",
        base="treads",
        antenna="gauge",
        badge="gear",
        tool="pipe",
        tags=("蒸汽", "铜色", "齿轮"),
    ),
    Style(
        slug="lavender",
        number="07",
        name="薰衣草",
        subtitle="轻巧家用型",
        description="淡紫白圆壳、心形天线和小轮底座，胸口是柔和的心形灯。适合家庭氛围的殖民地。",
        shell=(238, 229, 248, 255),
        shell_dark=(197, 181, 216, 255),
        accent=(139, 101, 185, 255),
        accent_light=(216, 174, 235, 255),
        tread=(70, 62, 83, 255),
        body="round",
        head="bubble",
        base="wheels",
        antenna="heart",
        badge="heart",
        eyes="soft",
        tags=("薰衣草", "紫色", "心形"),
    ),
    Style(
        slug="emergency",
        number="08",
        name="红色应急",
        subtitle="灭火与救援",
        description="红白高对比、方头警示灯和随身灭火罐。救火、救援和危险规避时最容易被一眼看到。",
        shell=(226, 95, 83, 255),
        shell_dark=(166, 65, 63, 255),
        accent=(248, 241, 228, 255),
        accent_light=(255, 193, 103, 255),
        tread=(57, 48, 49, 255),
        body="box",
        head="box",
        base="treads",
        antenna="warning",
        badge="stripe",
        tool="extinguisher",
        eyes="alert",
        tags=("应急", "红色", "灭火罐"),
    ),
    Style(
        slug="aqua",
        number="09",
        name="深海青",
        subtitle="水域与清洁",
        description="青白配色、泡泡头罩和背部水罐，底部悬浮环。适合潮湿环境、浴室和污水处理区。",
        shell=(226, 242, 242, 255),
        shell_dark=(168, 207, 211, 255),
        accent=(44, 139, 155, 255),
        accent_light=(115, 219, 230, 255),
        tread=(45, 68, 76, 255),
        body="slim",
        head="bubble",
        base="hover",
        antenna="drop",
        badge="wave",
        tool="tank",
        eyes="soft",
        tags=("深海", "青色", "水罐"),
    ),
    Style(
        slug="royal",
        number="10",
        name="皇家白金",
        subtitle="殖民地礼宾",
        description="象牙白外壳、金色包边和皇冠天线，胸口金色宝石。适合作为主基地的旗舰保姆机。",
        shell=(247, 240, 219, 255),
        shell_dark=(210, 194, 159, 255),
        accent=(198, 154, 58, 255),
        accent_light=(249, 216, 118, 255),
        tread=(73, 62, 45, 255),
        body="round",
        head="dome",
        base="treads",
        antenna="crown",
        badge="gem",
        eyes="royal",
        tags=("皇家", "白金", "皇冠"),
    ),
    Style(
        slug="penguin",
        number="11",
        name="企鹅信使",
        subtitle="蛋形搬运机",
        description="黑白蛋形机身、橙色喙和脚蹼，像一只会走路的小企鹅。适合搬运和送餐，轮廓最圆润。",
        shell=(39, 47, 60, 255),
        shell_dark=(240, 245, 248, 255),
        accent=(239, 145, 57, 255),
        accent_light=(255, 205, 116, 255),
        tread=(43, 51, 64, 255),
        body="egg",
        head="none",
        base="biped",
        antenna="tuft",
        badge="lights",
        eyes="penguin",
        tags=("企鹅", "黑白", "蛋形"),
    ),
    Style(
        slug="spider",
        number="12",
        name="蜘蛛检修",
        subtitle="窄身多足",
        description="紫黑窄身、六条检修足和红色传感眼，能钻进狭窄管道。拿扳手，适合维修与危险区域。",
        shell=(79, 66, 105, 255),
        shell_dark=(48, 42, 70, 255),
        accent=(205, 80, 94, 255),
        accent_light=(246, 158, 122, 255),
        tread=(39, 35, 54, 255),
        body="slim",
        head="visor",
        base="spider",
        antenna="antennae",
        badge="sixdots",
        tool="wrench",
        eyes="visor",
        tags=("蜘蛛", "紫黑", "多足"),
    ),
    Style(
        slug="monowheel",
        number="13",
        name="独轮信使",
        subtitle="单轮快递",
        description="青绿色瘦身机、单只大轮和橙色包裹架，转向灵活。专为送餐、送件和短途跑腿设计。",
        shell=(62, 139, 149, 255),
        shell_dark=(38, 91, 101, 255),
        accent=(235, 145, 60, 255),
        accent_light=(255, 205, 110, 255),
        tread=(32, 39, 44, 255),
        body="slim",
        head="visor",
        base="monowheel",
        antenna="flag",
        badge="package",
        tool="package",
        eyes="visor",
        tags=("独轮", "青色", "包裹"),
    ),
    Style(
        slug="butler",
        number="14",
        name="双足侍者",
        subtitle="礼宾与送餐",
        description="黑礼服、白手套、金色领结，双腿站立并端着托盘。适合餐厅、会客区和正式场合。",
        shell=(50, 53, 63, 255),
        shell_dark=(232, 236, 242, 255),
        accent=(168, 126, 67, 255),
        accent_light=(242, 216, 150, 255),
        tread=(35, 36, 42, 255),
        body="slim",
        head="dome",
        base="biped",
        antenna="ball",
        badge="bowtie",
        tool="tray",
        eyes="round",
        tags=("侍者", "黑金", "托盘"),
    ),
    Style(
        slug="hex",
        number="15",
        name="六臂清洁塔",
        subtitle="高塔多臂",
        description="白色高塔机身、六条清洁臂和薄荷色拖把，一次覆盖更大面积。适合大厅和走廊。",
        shell=(228, 241, 238, 255),
        shell_dark=(164, 198, 196, 255),
        accent=(44, 148, 143, 255),
        accent_light=(112, 220, 206, 255),
        tread=(49, 72, 73, 255),
        body="tower",
        head="dome",
        base="treads",
        antenna="ring",
        badge="sixdots",
        tool="mop",
        eyes="round",
        tags=("六臂", "高塔", "拖把"),
    ),
    Style(
        slug="owl",
        number="16",
        name="猫头鹰观测",
        subtitle="夜视与巡视",
        description="棕羽色圆壳、猫头鹰面盘、耳羽和大眼睛，悬浮巡视。适合夜间看护和发现异常。",
        shell=(153, 113, 78, 255),
        shell_dark=(218, 192, 154, 255),
        accent=(235, 168, 63, 255),
        accent_light=(252, 218, 136, 255),
        tread=(68, 54, 45, 255),
        body="round",
        head="owl",
        base="hover",
        antenna="none",
        badge="feather",
        eyes="owl",
        tags=("猫头鹰", "棕金", "夜视"),
    ),
    Style(
        slug="beetle",
        number="17",
        name="甲虫搬运",
        subtitle="重载六足",
        description="墨绿甲壳、铜色六足和背部货箱，低重心。适合搬运重物、进矿洞和走崎岖地形。",
        shell=(74, 104, 62, 255),
        shell_dark=(43, 63, 44, 255),
        accent=(188, 118, 53, 255),
        accent_light=(232, 180, 92, 255),
        tread=(38, 44, 34, 255),
        body="beetle",
        head="beetle",
        base="beetle",
        antenna="antennae",
        badge="shell",
        tool="crate",
        eyes="beetle",
        tags=("甲虫", "墨绿", "货箱"),
    ),
    Style(
        slug="mushroom",
        number="18",
        name="蘑菇疗愈",
        subtitle="柔和陪护",
        description="奶油色圆身、红色蘑菇帽和白色斑点，提着小篮子。气质最柔和，适合病房和儿童房。",
        shell=(232, 216, 187, 255),
        shell_dark=(197, 172, 135, 255),
        accent=(196, 75, 74, 255),
        accent_light=(246, 235, 220, 255),
        tread=(83, 64, 57, 255),
        body="round",
        head="mushroom",
        base="wheels",
        antenna="none",
        badge="spots",
        tool="basket",
        eyes="soft",
        tags=("蘑菇", "奶油", "篮子"),
    ),
    Style(
        slug="frost",
        number="19",
        name="霜冻冷藏",
        subtitle="冷库与保鲜",
        description="冰白方形机身、蓝色冷光门缝和冰锥天线，像一台会走路的冷柜。适合冷库和食品区。",
        shell=(222, 241, 248, 255),
        shell_dark=(164, 199, 216, 255),
        accent=(64, 143, 188, 255),
        accent_light=(177, 235, 250, 255),
        tread=(50, 66, 78, 255),
        body="fridge",
        head="fridge",
        base="treads",
        antenna="icicle",
        badge="snow",
        tool="ice",
        eyes="visor",
        tags=("霜冻", "冰白", "冷柜"),
    ),
    Style(
        slug="solar",
        number="20",
        name="太阳花",
        subtitle="太阳能园丁",
        description="向日葵花瓣头、绿叶机身和背部太阳能板，明亮又省电。适合温室和户外基地。",
        shell=(235, 213, 96, 255),
        shell_dark=(163, 186, 87, 255),
        accent=(62, 132, 82, 255),
        accent_light=(255, 238, 146, 255),
        tread=(63, 78, 45, 255),
        body="round",
        head="flower",
        base="wheels",
        antenna="none",
        badge="sun",
        tool="solar",
        eyes="soft",
        tags=("太阳花", "黄绿", "太阳能"),
    ),
]


def draw_shadow(draw: ImageDraw.ImageDraw, y: int = 111):
    draw.ellipse(
        (P(22), P(y), P(106), P(y + 10)),
        fill=(0, 0, 0, 48),
    )


def draw_base(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    if style.base == "hover":
        glow = style.accent_light
        draw.ellipse((P(17), P(101), P(111), P(121)), fill=(*glow[:3], 50))
        draw.ellipse((P(24), P(104), P(104), P(118)), fill=(*glow[:3], 95))
        draw.ellipse((P(34), P(107), P(94), P(116)), fill=(*glow[:3], 160))
        draw.ellipse((P(43), P(109), P(85), P(114)), fill=(240, 255, 255, 205))
        return

    tread = style.tread
    rollers = (min(tread[0] + 39, 255), min(tread[1] + 39, 255), min(tread[2] + 39, 255), 255)

    if style.base in ("spider", "beetle"):
        thick = 3 if style.base == "spider" else 5
        for side in (-1, 1):
            for index, y in enumerate((62, 76, 90)):
                if facing in ("east", "west") and side > 0:
                    continue
                x1 = 42 if side < 0 else 86
                knee_x = x1 + side * (12 + index * 2)
                foot_x = x1 + side * (24 + index * 3)
                foot_y = y + 4 + index * 2
                draw.line((P(x1), P(y), P(knee_x), P(y - 3), P(foot_x), P(foot_y)),
                          fill=OUTLINE, width=P(thick + 2), joint="curve")
                draw.line((P(x1), P(y), P(knee_x), P(y - 3), P(foot_x), P(foot_y)),
                          fill=tread, width=P(thick), joint="curve")
                draw.ellipse((P(foot_x - 4), P(foot_y - 2), P(foot_x + 4), P(foot_y + 5)),
                             fill=style.accent_light, outline=OUTLINE, width=P(1))
        return

    if style.base == "monowheel":
        centre = 64 if facing in ("south", "north") else 58
        draw.line((P(centre), P(82), P(centre), P(100)), fill=OUTLINE, width=P(5))
        draw.line((P(centre), P(82), P(centre), P(100)), fill=style.accent, width=P(3))
        draw.ellipse((P(centre - 22), P(80), P(centre + 22), P(124)), fill=tread, outline=OUTLINE, width=P(2))
        draw.ellipse((P(centre - 15), P(87), P(centre + 15), P(117)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        draw.ellipse((P(centre - 5), P(97), P(centre + 5), P(107)), fill=style.accent, outline=OUTLINE, width=P(1))
        return

    if style.base == "biped":
        if facing in ("south", "north"):
            for x in (46, 77):
                rr(draw, (P(x), P(91), P(x + 12), P(113)), P(5), tread, OUTLINE, P(1.5))
                rr(draw, (P(x - 5), P(108), P(x + 18), P(118)), P(5), style.accent, OUTLINE, P(1))
        else:
            rr(draw, (P(52), P(91), P(64), P(113)), P(5), tread, OUTLINE, P(1.5))
            rr(draw, (P(46), P(108), P(72), P(118)), P(5), style.accent, OUTLINE, P(1))
        return

    if style.base == "wheels":
        if facing in ("south", "north"):
            for x in (34, 82):
                draw.ellipse((P(x), P(94), P(x + 20), P(114)), fill=tread, outline=OUTLINE, width=P(1.5))
                draw.ellipse((P(x + 6), P(100), P(x + 14), P(108)), fill=rollers, outline=OUTLINE, width=P(1))
        else:
            draw.ellipse((P(44), P(94), P(64), P(114)), fill=tread, outline=OUTLINE, width=P(1.5))
            draw.ellipse((P(51), P(101), P(57), P(107)), fill=rollers, outline=OUTLINE, width=P(1))
            draw.ellipse((P(73), P(96), P(89), P(112)), fill=tread, outline=OUTLINE, width=P(1.5))
        return

    # treads
    if facing in ("south", "north"):
        for x in (21, 75):
            rr(draw, (P(x), P(94), P(x + 32), P(112)), P(8), tread, OUTLINE, P(1.5))
            for dx in (7, 15, 23):
                draw.rectangle((P(x + dx), P(99), P(x + dx + 4), P(107)), fill=rollers)
    else:
        x = 72
        rr(draw, (P(x), P(94), P(x + 34), P(112)), P(8), tread, OUTLINE, P(1.5))
        for dx in (7, 15, 23):
            draw.rectangle((P(x + dx), P(99), P(x + dx + 4), P(107)), fill=rollers)


def body_box(style: Style):
    if style.body == "box":
        return (25, 45, 103, 101), 9
    if style.body == "slim":
        return (34, 46, 94, 101), 20
    if style.body == "egg":
        return (31, 34, 97, 102), 32
    if style.body == "tower":
        return (42, 28, 86, 102), 14
    if style.body == "beetle":
        return (26, 44, 102, 100), 32
    if style.body == "fridge":
        return (26, 34, 100, 101), 10
    return (28, 44, 100, 102), 18


def draw_body(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    box, radius = body_box(style)
    x1, y1, x2, y2 = box

    if style.body == "egg":
        ellipse(draw, (P(x1), P(y1), P(x2), P(y2)), style.shell, OUTLINE, P(1.5))
        ellipse(draw, (P(x1 + 8), P(y1 + 10), P(x2 - 8), P(y2 - 7)), style.shell_dark, OUTLINE, P(1))
        if facing in ("south", "north"):
            for side in (-1, 1):
                bx = x1 - 4 if side < 0 else x2 - 12
                ellipse(draw, (P(bx), P(66), P(bx + 16), P(88)), style.accent, OUTLINE, P(1))
        else:
            ellipse(draw, (P(18), P(66), P(34), P(88)), style.accent, OUTLINE, P(1))
        return

    if style.body == "beetle":
        ellipse(draw, (P(x1), P(y1), P(x2), P(y2)), style.shell, OUTLINE, P(2))
        ellipse(draw, (P(x1 + 8), P(y1 + 8), P(x2 - 8), P(y2 - 8)), style.shell_dark, OUTLINE, P(1))
        draw.line((P(64), P(y1 + 4), P(64), P(y2 - 4)), fill=OUTLINE, width=P(2))
        return

    if style.body == "fridge":
        rr(draw, (P(x1), P(y1), P(x2), P(y2)), P(radius), style.shell, OUTLINE, P(1.8))
        rr(draw, (P(x1 + 5), P(y1 + 5), P(x2 - 5), P(y2 - 5)), P(6), None, style.shell_dark, P(1))
        draw.rectangle((P(x1 + 7), P(y1 + 8), P(x2 - 7), P(y1 + 12)), fill=style.accent_light)
        draw.line((P(x1 + 8), P(60), P(x2 - 8), P(60)), fill=style.accent, width=P(2))
        draw.rectangle((P(x2 - 14), P(64), P(x2 - 11), P(76)), fill=style.accent, outline=OUTLINE, width=P(1))
        return

    if style.body == "tower":
        rr(draw, (P(x1), P(y1), P(x2), P(y2)), P(radius), style.shell, OUTLINE, P(1.8))
        rr(draw, (P(x1 + 5), P(y1 + 5), P(x2 - 5), P(y2 - 5)), P(10), None, style.shell_dark, P(1))
        for y in (56, 72, 88):
            draw.rectangle((P(x1 + 8), P(y), P(x2 - 8), P(y + 3)), fill=style.accent_light)
        for side in (-1, 1):
            for index, y in enumerate((58, 73, 88)):
                ax = x1 - 12 if side < 0 else x2 - 4
                ay = y - 3 - index * 2
                rr(draw, (P(ax), P(ay), P(ax + 18), P(ay + 8)), P(4), style.shell_dark, OUTLINE, P(1))
                draw.ellipse((P(ax + (16 if side > 0 else 0) - 2), P(ay + 1),
                              P(ax + (16 if side > 0 else 0) + 4), P(ay + 7)),
                             fill=style.accent_light, outline=OUTLINE, width=P(1))
        return

    rr(draw, (P(x1), P(y1), P(x2), P(y2)), P(radius), style.shell, OUTLINE, P(1.5))
    if style.body == "box":
        rr(draw, (P(x1 + 5), P(y1 + 5), P(x2 - 5), P(y2 - 5)), P(6), None, style.shell_dark, P(1))
        # corner bolts
        for bx, by in ((x1 + 4, y1 + 4), (x2 - 8, y1 + 4), (x1 + 4, y2 - 8), (x2 - 8, y2 - 8)):
            draw.ellipse((P(bx), P(by), P(bx + 4), P(by + 4)), fill=style.accent_light, outline=OUTLINE, width=P(1))
    else:
        rr(draw, (P(x1 + 6), P(y1 + 6), P(x2 - 6), P(y2 - 7)), P(max(6, radius - 4)), None, style.shell_dark, P(1))

    if style.base in ("spider", "beetle"):
        return

    arms = style.shell_dark
    if facing in ("south", "north"):
        rr(draw, (P(11), P(58), P(27), P(88)), P(7), arms, OUTLINE, P(1))
        rr(draw, (P(101), P(58), P(117), P(88)), P(7), arms, OUTLINE, P(1))
        if style.body == "box":
            draw.rectangle((P(15), P(65), P(23), P(81)), fill=style.accent, outline=OUTLINE, width=P(1))
            draw.rectangle((P(105), P(65), P(113), P(81)), fill=style.accent, outline=OUTLINE, width=P(1))
    else:
        rr(draw, (P(62), P(59), P(78), P(89)), P(7), arms, OUTLINE, P(1))


def draw_chest(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    if facing == "north":
        # back hatch and vents
        rr(draw, (P(41), P(57), P(87), P(91)), P(7), style.shell_dark, OUTLINE, P(1))
        for y in (65, 73, 81):
            draw.rectangle((P(48), P(y), P(80), P(y + 3)), fill=style.accent)
        if style.tool == "tank":
            rr(draw, (P(42), P(50), P(86), P(78)), P(12), style.accent, OUTLINE, P(1.5))
            draw.rectangle((P(49), P(54), P(79), P(57)), fill=style.accent_light)
            draw.ellipse((P(58), P(64), P(70), P(76)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        return

    if facing == "east":
        # side panel
        rr(draw, (P(43), P(62), P(85), P(88)), P(9), style.accent, OUTLINE, P(1))
        draw.rectangle((P(52), P(68), P(78), P(72)), fill=style.accent_light)
        draw.ellipse((P(58), P(76), P(72), P(84)), fill=style.accent_light)
        return

    # south chest panel
    panel = (44, 62, 84, 90)
    rr(draw, (P(panel[0]), P(panel[1]), P(panel[2]), P(panel[3])), P(8), style.accent, OUTLINE, P(1))
    badge = style.badge
    cx, cy = 64, 76
    if badge == "lights":
        for i, color in enumerate(((120, 220, 170, 255), (242, 205, 90, 255), (235, 120, 110, 255))):
            draw.ellipse((P(50 + i * 11), P(73), P(56 + i * 11), P(79)), fill=color, outline=OUTLINE, width=P(1))
    elif badge == "cross":
        draw.rectangle((P(cx - 3), P(cy - 9), P(cx + 3), P(cy + 9)), fill=style.accent_light)
        draw.rectangle((P(cx - 9), P(cy - 3), P(cx + 9), P(cy + 3)), fill=style.accent_light)
        draw.rectangle((P(cx - 3), P(cy - 9), P(cx + 3), P(cy + 9)), outline=OUTLINE, width=P(1))
        draw.rectangle((P(cx - 9), P(cy - 3), P(cx + 9), P(cy + 3)), outline=OUTLINE, width=P(1))
    elif badge == "leaf":
        draw.ellipse((P(cx - 8), P(cy - 6), P(cx + 7), P(cy + 8)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        draw.line((P(cx - 7), P(cy + 7), P(cx + 8), P(cy - 6)), fill=style.accent, width=P(1))
    elif badge == "hazard":
        for i in range(4):
            x = 46 + i * 10
            draw.polygon(
                [(P(x), P(84)), (P(x + 6), P(66)), (P(x + 12), P(84))],
                fill=style.accent_light if i % 2 == 0 else style.accent,
            )
        draw.rectangle((P(44), P(84), P(84), P(88)), fill=style.accent, outline=OUTLINE, width=P(1))
    elif badge == "line":
        draw.rectangle((P(48), P(73), P(80), P(77)), fill=style.accent_light)
        draw.rectangle((P(48), P(80), P(70), P(83)), fill=style.accent_light)
    elif badge == "gear":
        draw.ellipse((P(cx - 8), P(cy - 8), P(cx + 8), P(cy + 8)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        draw.ellipse((P(cx - 3), P(cy - 3), P(cx + 3), P(cy + 3)), fill=style.accent, outline=OUTLINE, width=P(1))
        for angle in range(0, 360, 45):
            a = math.radians(angle)
            draw.line((P(cx + math.cos(a) * 10), P(cy + math.sin(a) * 10),
                       P(cx + math.cos(a) * 13), P(cy + math.sin(a) * 13)),
                      fill=style.accent_light, width=P(2))
    elif badge == "heart":
        heart = [
            (P(cx), P(cy + 9)),
            (P(cx - 10), P(cy - 1)),
            (P(cx - 9), P(cy - 7)),
            (P(cx - 4), P(cy - 10)),
            (P(cx), P(cy - 6)),
            (P(cx + 4), P(cy - 10)),
            (P(cx + 9), P(cy - 7)),
            (P(cx + 10), P(cy - 1)),
        ]
        polygon(draw, heart, style.accent_light, OUTLINE)
    elif badge == "stripe":
        for i in range(3):
            draw.rectangle((P(46 + i * 14), P(66), P(54 + i * 14), P(86)), fill=style.accent_light)
    elif badge == "wave":
        for i in range(2):
            y = 71 + i * 9
            draw.arc((P(48), P(y), P(80), P(y + 12)), 200, 340, fill=style.accent_light, width=P(2))
    elif badge == "gem":
        draw.polygon([(P(cx), P(cy - 10)), (P(cx + 10), P(cy)), (P(cx), P(cy + 10)), (P(cx - 10), P(cy))],
                     fill=style.accent_light, outline=OUTLINE)
        draw.line((P(cx), P(cy - 7), P(cx + 5), P(cy), P(cx), P(cy + 7), P(cx - 5), P(cy), P(cx), P(cy - 7)),
                  fill=style.accent, width=P(1))
    elif badge == "package":
        rr(draw, (P(cx - 10), P(cy - 9), P(cx + 10), P(cy + 9)), P(3), style.accent_light, OUTLINE, P(1))
        draw.rectangle((P(cx - 2), P(cy - 9), P(cx + 2), P(cy + 9)), fill=style.accent)
        draw.rectangle((P(cx - 10), P(cy - 2), P(cx + 10), P(cy + 2)), fill=style.accent)
    elif badge == "bowtie":
        polygon(draw, [(P(cx - 12), P(cy - 6)), (P(cx - 2), P(cy)), (P(cx - 12), P(cy + 6))],
                style.accent_light, OUTLINE)
        polygon(draw, [(P(cx + 12), P(cy - 6)), (P(cx + 2), P(cy)), (P(cx + 12), P(cy + 6))],
                style.accent_light, OUTLINE)
        draw.ellipse((P(cx - 3), P(cy - 3), P(cx + 3), P(cy + 3)), fill=style.accent, outline=OUTLINE, width=P(1))
    elif badge == "sixdots":
        for i in range(6):
            x = cx - 9 + (i % 3) * 9
            y = cy - 7 + (i // 3) * 12
            draw.ellipse((P(x - 3), P(y - 3), P(x + 3), P(y + 3)), fill=style.accent_light, outline=OUTLINE, width=P(1))
    elif badge == "feather":
        for i in range(3):
            y = cy - 7 + i * 7
            draw.line((P(cx - 9), P(y + 4), P(cx), P(y), P(cx + 9), P(y + 4)),
                      fill=style.accent_light, width=P(2), joint="curve")
    elif badge == "shell":
        draw.line((P(cx), P(cy - 10), P(cx), P(cy + 10)), fill=style.accent_light, width=P(2))
        for side in (-1, 1):
            draw.arc((P(cx + side * 12 - 14), P(cy - 10), P(cx + side * 12 + 14), P(cy + 10)),
                     110 if side < 0 else 250, 250 if side < 0 else 390,
                     fill=style.accent, width=P(2))
    elif badge == "spots":
        for x, y, r in ((cx - 8, cy - 6, 4), (cx + 7, cy - 2, 3), (cx - 2, cy + 7, 3)):
            draw.ellipse((P(x - r), P(y - r), P(x + r), P(y + r)), fill=style.accent_light, outline=OUTLINE, width=P(1))
    elif badge == "snow":
        for angle in range(0, 180, 60):
            a = math.radians(angle)
            draw.line((P(cx - math.cos(a) * 10), P(cy - math.sin(a) * 10),
                       P(cx + math.cos(a) * 10), P(cy + math.sin(a) * 10)),
                      fill=style.accent_light, width=P(2))
            for sign in (-1, 1):
                draw.line((
                    P(cx + math.cos(a) * 7), P(cy + math.sin(a) * 7),
                    P(cx + math.cos(a) * 7 + math.cos(a + sign * 0.8) * 5),
                    P(cy + math.sin(a) * 7 + math.sin(a + sign * 0.8) * 5),
                ), fill=style.accent_light, width=P(1))
    elif badge == "sun":
        draw.ellipse((P(cx - 7), P(cy - 7), P(cx + 7), P(cy + 7)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        for angle in range(0, 360, 45):
            a = math.radians(angle)
            draw.line((P(cx + math.cos(a) * 10), P(cy + math.sin(a) * 10),
                       P(cx + math.cos(a) * 14), P(cy + math.sin(a) * 14)),
                      fill=style.accent_light, width=P(2))


def draw_head(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    if style.head == "none":
        # 企鹅：脸直接长在蛋形身体上
        if facing == "north":
            rr(draw, (P(48), P(42), P(80), P(54)), P(5), style.accent, OUTLINE, P(1))
            return
        if facing == "east":
            ellipse(draw, (P(66), P(42), P(82), P(56)), (25, 31, 40, 255), OUTLINE, P(1))
            ellipse(draw, (P(71), P(45), P(78), P(52)), style.accent_light)
            polygon(draw, [(P(82), P(48)), (P(94), P(52)), (P(82), P(56))], style.accent, OUTLINE)
            return
        ellipse(draw, (P(47), P(41), P(59), P(53)), (25, 31, 40, 255), OUTLINE, P(1))
        ellipse(draw, (P(69), P(41), P(81), P(53)), (25, 31, 40, 255), OUTLINE, P(1))
        ellipse(draw, (P(51), P(44), P(56), P(49)), style.accent_light)
        ellipse(draw, (P(73), P(44), P(78), P(49)), style.accent_light)
        polygon(draw, [(P(58), P(54)), (P(70), P(54)), (P(64), P(62))], style.accent, OUTLINE)
        return

    if style.head == "beetle":
        ellipse(draw, (P(44), P(32), P(84), P(60)), style.shell_dark, OUTLINE, P(1.5))
        if facing == "north":
            draw.rectangle((P(52), P(40), P(76), P(48)), fill=style.accent)
            return
        if facing == "east":
            ellipse(draw, (P(67), P(39), P(81), P(53)), (24, 30, 39, 255), OUTLINE, P(1))
            ellipse(draw, (P(72), P(42), P(79), P(49)), style.accent_light)
            return
        for x in (52, 63, 74):
            ellipse(draw, (P(x), P(40), P(x + 8), P(48)), style.accent_light, OUTLINE, P(1))
        return

    if style.head == "fridge":
        if facing == "north":
            for x in (44, 54, 64, 74):
                draw.rectangle((P(x), P(40), P(x + 5), P(54)), fill=style.accent)
            return
        if facing == "east":
            rr(draw, (P(48), P(39), P(88), P(56)), P(5), style.shell_dark, OUTLINE, P(1))
            draw.line((P(72), P(42), P(80), P(47), P(72), P(52)), fill=style.accent_light, width=P(2))
            return
        rr(draw, (P(34), P(38), P(94), P(56)), P(6), style.shell_dark, OUTLINE, P(1))
        ellipse(draw, (P(44), P(44), P(54), P(54)), style.accent_light)
        ellipse(draw, (P(74), P(44), P(84), P(54)), style.accent_light)
        draw.line((P(64), P(42), P(64), P(54)), fill=style.accent, width=P(1))
        return

    if style.head == "owl":
        # 耳羽
        polygon(draw, [(P(30), P(22)), (P(38), P(2)), (P(48), P(20))], style.shell_dark, OUTLINE)
        polygon(draw, [(P(98), P(22)), (P(90), P(2)), (P(80), P(20))], style.shell_dark, OUTLINE)
        ellipse(draw, (P(28), P(10), P(100), P(64)), style.shell, OUTLINE, P(1.8))
        if facing == "north":
            ellipse(draw, (P(42), P(24), P(86), P(52)), style.shell_dark)
            for x in (50, 64, 78):
                draw.line((P(x), P(28), P(x), P(48)), fill=style.accent, width=P(1))
            return
        if facing == "east":
            ellipse(draw, (P(42), P(22), P(70), P(50)), style.shell_dark, OUTLINE, P(1))
            ellipse(draw, (P(48), P(28), P(64), P(44)), (25, 31, 40, 255), OUTLINE, P(1))
            ellipse(draw, (P(54), P(32), P(62), P(40)), style.accent_light)
            polygon(draw, [(P(68), P(39)), (P(82), P(42)), (P(68), P(45))], style.accent, OUTLINE)
            return
        for x in (38, 68):
            ellipse(draw, (P(x), P(22), P(x + 28), P(50)), style.shell_dark, OUTLINE, P(1))
            ellipse(draw, (P(x + 6), P(28), P(x + 22), P(44)), (25, 31, 40, 255), OUTLINE, P(1))
            ellipse(draw, (P(x + 11), P(33), P(x + 17), P(39)), style.accent_light)
        polygon(draw, [(P(59), P(45)), (P(69), P(45)), (P(64), P(54))], style.accent, OUTLINE)
        return

    if style.head == "mushroom":
        # 蘑菇帽
        ellipse(draw, (P(16), P(2), P(112), P(52)), style.accent, OUTLINE, P(1.8))
        ellipse(draw, (P(24), P(10), P(104), P(44)), style.accent, None)
        for x, y, r in ((38, 22, 6), (60, 14, 5), (84, 24, 7), (52, 36, 4), (76, 37, 4)):
            ellipse(draw, (P(x - r), P(y - r), P(x + r), P(y + r)), style.accent_light)
        if facing == "north":
            return
        ellipse(draw, (P(40), P(30), P(88), P(56)), style.shell_dark, OUTLINE, P(1))
        if facing == "east":
            ellipse(draw, (P(66), P(34), P(82), P(50)), (25, 31, 40, 255), OUTLINE, P(1))
            ellipse(draw, (P(72), P(38), P(78), P(44)), style.accent_light)
            return
        ellipse(draw, (P(50), P(38), P(58), P(46)), (25, 31, 40, 255))
        ellipse(draw, (P(70), P(38), P(78), P(46)), (25, 31, 40, 255))
        ellipse(draw, (P(53), P(40), P(56), P(43)), WHITE)
        ellipse(draw, (P(73), P(40), P(76), P(43)), WHITE)
        return

    if style.head == "flower":
        # 花瓣
        for i in range(12):
            a = math.radians(i * 30)
            x = 64 + math.cos(a) * 20
            y = 33 + math.sin(a) * 20
            ellipse(draw, (P(x - 10), P(y - 10), P(x + 10), P(y + 10)), style.accent_light, OUTLINE, P(1))
        ellipse(draw, (P(44), P(13), P(84), P(53)), style.shell, OUTLINE, P(1.5))
        if facing == "north":
            draw.ellipse((P(54), P(25), P(74), P(45)), fill=style.shell_dark, outline=OUTLINE, width=P(1))
            return
        if facing == "east":
            ellipse(draw, (P(58), P(26), P(78), P(46)), style.shell_dark, OUTLINE, P(1))
            ellipse(draw, (P(65), P(31), P(75), P(41)), style.accent, OUTLINE, P(1))
            return
        ellipse(draw, (P(50), P(24), P(58), P(32)), (25, 31, 40, 255))
        ellipse(draw, (P(70), P(24), P(78), P(32)), (25, 31, 40, 255))
        draw.arc((P(56), P(32), P(72), P(44)), 20, 160, fill=style.accent, width=P(1))
        return

    if style.head == "box":
        rr(draw, (P(27), P(13), P(101), P(60)), P(13), style.shell, OUTLINE, P(1.5))
        rr(draw, (P(33), P(19), P(95), P(53)), P(9), style.shell_dark, OUTLINE, P(1))
    elif style.head == "visor":
        rr(draw, (P(30), P(18), P(98), P(58)), P(16), style.shell, OUTLINE, P(1.5))
        rr(draw, (P(36), P(25), P(92), P(51)), P(10), (25, 30, 39, 255), OUTLINE, P(1))
    elif style.head == "bubble":
        ellipse(draw, (P(32), P(14), P(96), P(63)), style.shell, OUTLINE, P(1.5))
        ellipse(draw, (P(38), P(19), P(90), P(57)), style.shell_dark, OUTLINE, P(1))
        ellipse(draw, (P(43), P(24), P(85), P(52)), (*style.accent_light[:3], 150), None)
        draw.arc((P(44), P(25), P(84), P(51)), 195, 320, fill=(255, 255, 255, 220), width=P(2))
    else:
        ellipse(draw, (P(32), P(14), P(96), P(64)), style.shell, OUTLINE, P(1.5))
        ellipse(draw, (P(38), P(19), P(90), P(58)), style.shell_dark, OUTLINE, P(1))

    # face
    if facing == "north":
        if style.head == "box":
            for x in (47, 57, 67, 77):
                draw.rectangle((P(x), P(29), P(x + 5), P(45)), fill=style.accent)
        else:
            rr(draw, (P(51), P(27), P(77), P(44)), P(5), style.accent, OUTLINE, P(1))
            for x in (57, 64, 71):
                draw.rectangle((P(x), P(32), P(x + 3), P(40)), fill=style.accent_light)
        return

    eye_y = 35 if style.head != "box" else 33
    if facing == "east":
        # right-facing side eye / visor
        if style.head == "box":
            rr(draw, (P(48), P(26), P(88), P(48)), P(7), (24, 30, 39, 255), OUTLINE, P(1))
            ellipse(draw, (P(72), P(30), P(84), P(42)), style.accent_light)
        else:
            ellipse(draw, (P(49), P(27), P(85), P(51)), fill=(25, 31, 40, 255))
            ellipse(draw, (P(70), P(32), P(82), P(44)), style.accent_light)
            ellipse(draw, (P(74), P(34), P(78), P(38)), WHITE)
        return

    # south face
    if style.head == "box":
        rr(draw, (P(36), P(25), P(92), P(48)), P(7), (24, 30, 39, 255), OUTLINE, P(1))
        if style.eyes == "alert":
            ellipse(draw, (P(45), P(30), P(55), P(40)), (255, 201, 112, 255))
            ellipse(draw, (P(73), P(30), P(83), P(40)), (255, 201, 112, 255))
        else:
            ellipse(draw, (P(45), P(30), P(55), P(40)), style.accent_light)
            ellipse(draw, (P(73), P(30), P(83), P(40)), style.accent_light)
        draw.rectangle((P(52), P(44), P(76), P(46)), fill=style.accent)
    elif style.head == "visor":
        rr(draw, (P(38), P(27), P(90), P(47)), P(8), (22, 28, 37, 255), OUTLINE, P(1))
        draw.rectangle((P(44), P(34), P(84), P(40)), fill=style.accent_light)
    else:
        ellipse(draw, (P(42), P(24), P(86), P(52)), fill=(25, 31, 40, 255))
        if style.eyes == "soft":
            ellipse(draw, (P(49), P(30), P(58), P(39)), style.accent_light)
            ellipse(draw, (P(70), P(30), P(79), P(39)), style.accent_light)
        elif style.eyes == "royal":
            ellipse(draw, (P(47), P(29), P(59), P(41)), style.accent_light)
            ellipse(draw, (P(69), P(29), P(81), P(41)), style.accent_light)
            ellipse(draw, (P(52), P(33), P(55), P(36)), WHITE)
            ellipse(draw, (P(74), P(33), P(77), P(36)), WHITE)
        else:
            ellipse(draw, (P(56), P(29), P(72), P(44)), style.accent_light)
            ellipse(draw, (P(60), P(32), P(66), P(38)), (232, 251, 255, 255))

    # small mouth/grill
    if style.head != "box":
        rr(draw, (P(54), P(47), P(74), P(52)), P(2), (45, 53, 64, 255), None)


def draw_antenna(draw: ImageDraw.ImageDraw, style: Style):
    if style.antenna == "none":
        return
    top = 14
    if style.head == "box":
        top = 13
    elif style.head == "bubble":
        top = 14
    elif style.head in ("beetle", "fridge", "none"):
        top = 34

    if style.antenna == "antennae":
        for side in (-1, 1):
            draw.line((P(64 + side * 4), P(top), P(64 + side * 13), P(top - 12)),
                      fill=OUTLINE, width=P(2))
            draw.ellipse((P(64 + side * 16), P(top - 17), P(64 + side * 22), P(top - 11)),
                         fill=style.accent_light, outline=OUTLINE, width=P(1))
        return

    if style.antenna == "tuft":
        polygon(draw, [(P(58), P(34)), (P(64), P(25)), (P(70), P(34))],
                style.accent, OUTLINE)
        return

    if style.antenna == "icicle":
        draw.line((P(64), P(34), P(64), P(8)), fill=OUTLINE, width=P(2))
        polygon(draw, [(P(58), P(34)), (P(64), P(8)), (P(70), P(34))],
                style.accent_light, OUTLINE)
        draw.line((P(64), P(12), P(64), P(30)), fill=style.accent, width=P(1))
        return

    draw.line((P(64), P(top), P(64), P(5 if style.antenna != "crown" else 2)), fill=OUTLINE, width=P(2))
    if style.antenna == "ball":
        ellipse(draw, (P(59), P(2), P(69), P(12)), (244, 208, 92, 255), OUTLINE, P(1))
    elif style.antenna == "cross":
        rr(draw, (P(57), P(2), P(71), P(12)), P(5), style.accent, OUTLINE, P(1))
        draw.rectangle((P(63), P(4), P(65), P(10)), fill=WHITE)
        draw.rectangle((P(60), P(6), P(68), P(8)), fill=WHITE)
    elif style.antenna == "leaf":
        draw.line((P(64), P(8), P(70), P(1)), fill=OUTLINE, width=P(2))
        ellipse(draw, (P(65), P(0), P(77), P(9)), style.accent_light, OUTLINE, P(1))
        draw.line((P(67), P(6), P(75), P(2)), fill=style.accent, width=P(1))
    elif style.antenna == "warning":
        rr(draw, (P(58), P(2), P(70), P(12)), P(4), style.accent_light, OUTLINE, P(1))
        draw.rectangle((P(60), P(5), P(68), P(8)), fill=style.accent)
    elif style.antenna == "fin":
        polygon(draw, [(P(64), P(10)), (P(76), P(0)), (P(64), P(3))], style.accent_light, OUTLINE)
    elif style.antenna == "gauge":
        ellipse(draw, (P(56), P(0), P(72), P(14)), style.accent_light, OUTLINE, P(1))
        draw.line((P(64), P(7), P(68), P(3)), fill=style.accent, width=P(1))
    elif style.antenna == "heart":
        ellipse(draw, (P(58), P(2), P(64), P(8)), style.accent_light, OUTLINE, P(1))
        ellipse(draw, (P(64), P(2), P(70), P(8)), style.accent_light, OUTLINE, P(1))
        polygon(draw, [(P(58), P(6)), (P(70), P(6)), (P(64), P(13))], style.accent_light, OUTLINE)
    elif style.antenna == "drop":
        draw.polygon([(P(64), P(0)), (P(72), P(9)), (P(64), P(14)), (P(56), P(9))],
                     style.accent_light, OUTLINE)
    elif style.antenna == "crown":
        polygon(draw, [(P(51), P(9)), (P(55), P(0)), (P(61), P(6)), (P(64), P(-2)),
                       (P(67), P(6)), (P(73), P(0)), (P(77), P(9))],
                style.accent_light, OUTLINE)
        draw.rectangle((P(51), P(8), P(77), P(11)), fill=style.accent, outline=OUTLINE, width=P(1))
    elif style.antenna == "tuft":
        polygon(draw, [(P(58), P(34)), (P(64), P(25)), (P(70), P(34))],
                style.accent, OUTLINE)
    elif style.antenna == "flag":
        polygon(draw, [(P(64), P(14)), (P(64), P(4)), (P(78), P(7)), (P(64), P(10))],
                style.accent_light, OUTLINE)
    elif style.antenna == "ring":
        draw.ellipse((P(55), P(1), P(73), P(15)), None, style.accent_light, P(2))
        draw.ellipse((P(61), P(5), P(67), P(11)), fill=style.accent, outline=OUTLINE, width=P(1))
    elif style.antenna == "icicle":
        polygon(draw, [(P(58), P(34)), (P(64), P(8)), (P(70), P(34))],
                style.accent_light, OUTLINE)
        draw.line((P(64), P(12), P(64), P(30)), fill=style.accent, width=P(1))


def draw_tool(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    if not style.tool:
        return
    if style.tool == "broom":
        if facing in ("south", "north"):
            bx = 108 if facing == "south" else 105
            draw.line((P(bx), P(76), P(bx + 10), P(115)), fill=OUTLINE, width=P(3))
            draw.line((P(bx), P(76), P(bx + 10), P(115)), fill=(150, 100, 55, 255), width=P(2))
            polygon(draw, [(P(bx + 6), P(111)), (P(bx + 18), P(115)), (P(bx + 14), P(124)), (P(bx + 2), P(120))],
                    (232, 188, 88, 255), OUTLINE)
        else:
            draw.line((P(86), P(76), P(98), P(115)), fill=OUTLINE, width=P(3))
            draw.line((P(86), P(76), P(98), P(115)), fill=(150, 100, 55, 255), width=P(2))
            polygon(draw, [(P(94), P(111)), (P(106), P(115)), (P(102), P(124)), (P(90), P(120))],
                    (232, 188, 88, 255), OUTLINE)
    elif style.tool == "watering":
        if facing == "east":
            rr(draw, (P(92), P(82), P(112), P(104)), P(6), style.accent, OUTLINE, P(1))
            draw.line((P(110), P(86), P(122), P(80)), fill=OUTLINE, width=P(3))
            draw.line((P(110), P(86), P(122), P(80)), fill=style.accent_light, width=P(2))
            draw.line((P(122), P(80), P(126), P(86)), fill=style.accent_light, width=P(2))
        elif facing in ("south", "north"):
            rr(draw, (P(102), P(84), P(120), P(106)), P(6), style.accent, OUTLINE, P(1))
            draw.line((P(118), P(88), P(126), P(82)), fill=style.accent_light, width=P(2))
    elif style.tool == "wrench":
        if facing in ("south", "north"):
            draw.line((P(108), P(78), P(118), P(110)), fill=OUTLINE, width=P(4))
            draw.line((P(108), P(78), P(118), P(110)), fill=(190, 198, 208, 255), width=P(2))
            ellipse(draw, (P(104), P(72), P(114), P(82)), None, OUTLINE, P(3))
        elif facing == "east":
            draw.line((P(94), P(78), P(106), P(110)), fill=OUTLINE, width=P(4))
            draw.line((P(94), P(78), P(106), P(110)), fill=(190, 198, 208, 255), width=P(2))
            ellipse(draw, (P(90), P(72), P(100), P(82)), None, OUTLINE, P(3))
    elif style.tool == "pipe":
        if facing in ("south", "north"):
            draw.line((P(108), P(72), P(108), P(100)), fill=style.accent, width=P(5))
            draw.line((P(104), P(72), P(112), P(72)), fill=style.accent_light, width=P(3))
            draw.ellipse((P(104), P(98), P(112), P(106)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        else:
            draw.line((P(88), P(72), P(88), P(102)), fill=style.accent, width=P(5))
            draw.ellipse((P(84), P(98), P(92), P(106)), fill=style.accent_light, outline=OUTLINE, width=P(1))
    elif style.tool == "extinguisher":
        if facing == "north":
            rr(draw, (P(88), P(58), P(106), P(92)), P(8), style.accent, OUTLINE, P(1))
            draw.rectangle((P(92), P(62), P(102), P(66)), fill=style.accent_light)
            draw.line((P(97), P(58), P(97), P(50)), fill=OUTLINE, width=P(2))
        elif facing in ("south", "east"):
            rr(draw, (P(100), P(72), P(116), P(102)), P(7), style.accent, OUTLINE, P(1))
            draw.rectangle((P(104), P(76), P(112), P(80)), fill=style.accent_light)
            draw.line((P(108), P(72), P(108), P(66)), fill=OUTLINE, width=P(2))
    elif style.tool == "tank":
        if facing == "north":
            rr(draw, (P(42), P(48), P(86), P(79)), P(12), style.accent, OUTLINE, P(1.5))
            draw.rectangle((P(48), P(52), P(80), P(55)), fill=style.accent_light)
            draw.ellipse((P(58), P(62), P(70), P(74)), fill=style.accent_light, outline=OUTLINE, width=P(1))
        elif facing == "east":
            rr(draw, (P(36), P(50), P(56), P(82)), P(9), style.accent, OUTLINE, P(1))
            draw.rectangle((P(40), P(55), P(52), P(58)), fill=style.accent_light)
    elif style.tool == "package":
        if facing in ("south", "north"):
            rr(draw, (P(100), P(92), P(124), P(112)), P(4), (171, 120, 69, 255), OUTLINE, P(1))
            draw.rectangle((P(110), P(92), P(114), P(112)), fill=style.accent_light)
            draw.rectangle((P(100), P(100), P(124), P(104)), fill=style.accent_light)
        else:
            rr(draw, (P(90), P(92), P(114), P(112)), P(4), (171, 120, 69, 255), OUTLINE, P(1))
            draw.rectangle((P(100), P(92), P(104), P(112)), fill=style.accent_light)
    elif style.tool == "tray":
        if facing in ("south", "north"):
            rr(draw, (P(96), P(96), P(126), P(103)), P(4), (198, 156, 92, 255), OUTLINE, P(1))
            rr(draw, (P(104), P(84), P(114), P(96)), P(3), WHITE, OUTLINE, P(1))
            draw.ellipse((P(107), P(87), P(111), P(91)), fill=style.accent)
        else:
            rr(draw, (P(92), P(96), P(126), P(103)), P(4), (198, 156, 92, 255), OUTLINE, P(1))
            rr(draw, (P(102), P(84), P(112), P(96)), P(3), WHITE, OUTLINE, P(1))
    elif style.tool == "mop":
        if facing in ("south", "north"):
            draw.line((P(110), P(74), P(116), P(116)), fill=OUTLINE, width=P(3))
            draw.line((P(110), P(74), P(116), P(116)), fill=(168, 184, 190, 255), width=P(2))
            polygon(draw, [(P(106), P(112)), (P(126), P(116)), (P(122), P(126)), (P(102), P(122))],
                    style.accent_light, OUTLINE)
        else:
            draw.line((P(90), P(74), P(100), P(116)), fill=OUTLINE, width=P(3))
            draw.line((P(90), P(74), P(100), P(116)), fill=(168, 184, 190, 255), width=P(2))
            polygon(draw, [(P(92), P(112)), (P(112), P(116)), (P(108), P(126)), (P(88), P(122))],
                    style.accent_light, OUTLINE)
    elif style.tool == "crate":
        if facing == "north":
            rr(draw, (P(38), P(44), P(90), P(80)), P(4), (171, 120, 69, 255), OUTLINE, P(1.5))
            draw.rectangle((P(38), P(58), P(90), P(61)), fill=style.accent_light)
            draw.line((P(50), P(44), P(50), P(80)), fill=style.accent_light, width=P(1))
            draw.line((P(78), P(44), P(78), P(80)), fill=style.accent_light, width=P(1))
        elif facing in ("south", "east"):
            rr(draw, (P(98), P(80), P(120), P(104)), P(4), (171, 120, 69, 255), OUTLINE, P(1))
            draw.rectangle((P(98), P(88), P(120), P(91)), fill=style.accent_light)
    elif style.tool == "basket":
        if facing in ("south", "north"):
            draw.arc((P(100), P(84), P(122), P(104)), 180, 360, fill=OUTLINE, width=P(2))
            rr(draw, (P(98), P(92), P(124), P(108)), P(5), (188, 142, 85, 255), OUTLINE, P(1))
            draw.line((P(102), P(96), P(102), P(104)), fill=style.accent_light, width=P(1))
            draw.line((P(110), P(96), P(110), P(104)), fill=style.accent_light, width=P(1))
            draw.line((P(118), P(96), P(118), P(104)), fill=style.accent_light, width=P(1))
        else:
            rr(draw, (P(92), P(93), P(116), P(108)), P(5), (188, 142, 85, 255), OUTLINE, P(1))
            draw.line((P(96), P(97), P(96), P(105)), fill=style.accent_light, width=P(1))
    elif style.tool == "ice":
        if facing in ("south", "north"):
            polygon(draw, [(P(108), P(75)), (P(116), P(96)), (P(108), P(92)), (P(100), P(102))],
                    style.accent_light, OUTLINE)
            draw.line((P(108), P(80), P(108), P(98)), fill=WHITE, width=P(1))
        else:
            polygon(draw, [(P(96), P(76)), (P(108), P(96)), (P(96), P(92)), (P(88), P(104))],
                    style.accent_light, OUTLINE)
    elif style.tool == "solar":
        if facing == "north":
            rr(draw, (P(34), P(43), P(94), P(76)), P(4), (58, 96, 132, 255), OUTLINE, P(1.5))
            for x in (44, 60, 76):
                draw.line((P(x), P(46), P(x), P(73)), fill=style.accent_light, width=P(1))
            for y in (52, 62):
                draw.line((P(37), P(y), P(91), P(y)), fill=style.accent_light, width=P(1))
        elif facing == "east":
            rr(draw, (P(32), P(44), P(58), P(78)), P(4), (58, 96, 132, 255), OUTLINE, P(1))
            for y in (52, 62):
                draw.line((P(35), P(y), P(55), P(y)), fill=style.accent_light, width=P(1))


def draw_robot(style: Style, facing: str) -> Image.Image:
    image = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw_shadow(draw)
    draw_base(draw, style, facing)
    draw_body(draw, style, facing)
    draw_chest(draw, style, facing)
    draw_head(draw, style, facing)
    draw_antenna(draw, style)
    draw_tool(draw, style, facing)
    return image.resize((SIZE, SIZE), Image.LANCZOS)


def image_data_uri(image: Image.Image) -> str:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    encoded = base64.b64encode(buffer.getvalue()).decode("ascii")
    return "data:image/png;base64," + encoded


def render_html(
    images: dict[str, dict[str, Image.Image]],
    styles: list[Style],
    title: str,
    subtitle: str,
) -> str:
    tabs = []
    panels = []
    for index, style in enumerate(styles):
        active = " active" if index == 0 else ""
        tabs.append(
            f'<button class="tab{active}" data-tab="{index}" type="button">'
            f'<span class="tab-num">{style.number}</span>'
            f'<span class="tab-name">{html.escape(style.name)}</span>'
            f'</button>'
        )
        cards = []
        for view, label in (("south", "南 / 正面"), ("north", "北 / 背面"), ("east", "东 / 侧面"), ("west", "西 / 侧面")):
            uri = image_data_uri(images[style.slug][view])
            cards.append(
                f'<figure class="sprite-card">'
                f'<div class="sprite-frame"><img src="{uri}" alt="{html.escape(style.name)} {view}" width="256" height="256"></div>'
                f'<figcaption>{label}</figcaption>'
                f'</figure>'
            )
        tags = "".join(f'<span class="tag">{html.escape(tag)}</span>' for tag in style.tags)
        panels.append(
            f'<section class="panel{active}" data-panel="{index}">'
            f'<div class="panel-head">'
            f'<div><p class="eyebrow">方案 {style.number} · {html.escape(style.subtitle)}</p>'
            f'<h2>{html.escape(style.name)}</h2>'
            f'<p class="desc">{html.escape(style.description)}</p>'
            f'<div class="tags">{tags}</div></div>'
            f'<button class="choose" type="button" data-name="{html.escape(style.number + " " + style.name)}">'
            f'选择这个方案</button>'
            f'</div>'
            f'<div class="sprite-row">{"".join(cards)}</div>'
            f'<div class="palette" aria-label="配色">'
            f'<span class="swatch" style="background: rgb({style.shell[0]},{style.shell[1]},{style.shell[2]})"></span>'
            f'<span class="swatch" style="background: rgb({style.shell_dark[0]},{style.shell_dark[1]},{style.shell_dark[2]})"></span>'
            f'<span class="swatch" style="background: rgb({style.accent[0]},{style.accent[1]},{style.accent[2]})"></span>'
            f'<span class="swatch" style="background: rgb({style.accent_light[0]},{style.accent_light[1]},{style.accent_light[2]})"></span>'
            f'<span class="palette-note">外壳 / 阴影 / 主色 / 高光</span>'
            f'</div>'
            f'</section>'
        )

    return f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{html.escape(title)} · {len(styles)} 选 1</title>
<style>
:root {{
  color-scheme: dark;
  --bg: #10151c;
  --panel: #18212b;
  --panel-2: #1d2833;
  --line: #314252;
  --text: #e9f1f7;
  --muted: #9db0c0;
  --accent: #74d6e8;
  --accent-2: #f3cc67;
}}
* {{ box-sizing: border-box; }}
body {{
  margin: 0;
  min-height: 100vh;
  font-family: "Segoe UI", "Microsoft YaHei", "PingFang SC", system-ui, sans-serif;
  background:
    radial-gradient(900px 520px at 8% -10%, rgba(63, 131, 153, .35), transparent 65%),
    radial-gradient(800px 500px at 96% 4%, rgba(179, 128, 54, .22), transparent 70%),
    var(--bg);
  color: var(--text);
}}
body.light {{
  color-scheme: light;
  --bg: #eef4f2;
  --panel: #ffffff;
  --panel-2: #f4f8f6;
  --line: #c9d8d4;
  --text: #17242a;
  --muted: #526a70;
  --accent: #137b8c;
  --accent-2: #9a6b17;
  background:
    radial-gradient(900px 520px at 8% -10%, rgba(89, 173, 184, .28), transparent 65%),
    radial-gradient(800px 500px at 96% 4%, rgba(211, 166, 83, .22), transparent 70%),
    var(--bg);
}}
.wrap {{ max-width: 1320px; margin: 0 auto; padding: 28px 22px 44px; }}
header {{ display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; margin-bottom: 18px; }}
.eyebrow {{ margin: 0 0 8px; color: var(--accent); font-size: 12px; letter-spacing: .16em; text-transform: uppercase; }}
h1 {{ margin: 0; font-size: clamp(26px, 3vw, 40px); letter-spacing: -.02em; }}
.sub {{ margin: 8px 0 0; color: var(--muted); max-width: 720px; line-height: 1.6; }}
.toolbar {{ display: flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }}
button {{ font: inherit; cursor: pointer; }}
.ghost {{
  border: 1px solid var(--line); background: var(--panel); color: var(--text);
  border-radius: 999px; padding: 9px 14px; transition: .15s ease;
}}
.ghost:hover {{ border-color: var(--accent); transform: translateY(-1px); }}
.tabs {{ display: flex; gap: 6px; flex-wrap: wrap; padding: 8px; background: var(--panel); border: 1px solid var(--line); border-radius: 18px; }}
.tab {{
  border: 1px solid transparent; background: transparent; color: var(--muted);
  border-radius: 12px; padding: 9px 11px; display: flex; align-items: center; gap: 7px;
}}
.tab:hover {{ color: var(--text); background: var(--panel-2); }}
.tab.active {{ color: var(--text); border-color: var(--accent); background: var(--panel-2); box-shadow: inset 0 0 0 1px rgba(116, 214, 232, .15); }}
.tab-num {{ font-variant-numeric: tabular-nums; font-size: 11px; color: var(--accent); }}
.tab.active .tab-num {{ color: var(--accent-2); }}
.tab-name {{ font-size: 13px; white-space: nowrap; }}
.panel {{ display: none; margin-top: 16px; background: var(--panel); border: 1px solid var(--line); border-radius: 22px; padding: 22px; box-shadow: 0 24px 70px rgba(0,0,0,.18); }}
.panel.active {{ display: block; animation: fade .18s ease; }}
@keyframes fade {{ from {{ opacity: 0; transform: translateY(4px); }} to {{ opacity: 1; transform: none; }} }}
.panel-head {{ display: flex; justify-content: space-between; align-items: flex-start; gap: 18px; margin-bottom: 18px; }}
.panel-head h2 {{ margin: 0 0 7px; font-size: clamp(22px, 2.4vw, 31px); }}
.desc {{ margin: 0; color: var(--muted); line-height: 1.65; max-width: 780px; }}
.tags {{ display: flex; gap: 6px; flex-wrap: wrap; margin-top: 12px; }}
.tag {{ border: 1px solid var(--line); color: var(--muted); border-radius: 999px; padding: 4px 9px; font-size: 11px; }}
.choose {{
  flex: none; border: 1px solid var(--accent); color: #06222a; background: var(--accent);
  border-radius: 12px; padding: 11px 16px; font-weight: 700; white-space: nowrap;
}}
.choose:hover {{ filter: brightness(1.08); transform: translateY(-1px); }}
.sprite-row {{ display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }}
.sprite-card {{ margin: 0; background: var(--panel-2); border: 1px solid var(--line); border-radius: 16px; padding: 10px; }}
.sprite-frame {{
  aspect-ratio: 1; display: grid; place-items: center; overflow: hidden; border-radius: 12px;
  background:
    linear-gradient(135deg, rgba(255,255,255,.05) 25%, transparent 25%) 0 0 / 22px 22px,
    linear-gradient(315deg, rgba(255,255,255,.05) 25%, transparent 25%) 0 0 / 22px 22px,
    linear-gradient(145deg, #26392f, #19231f);
}}
body.light .sprite-frame {{ background:
    linear-gradient(135deg, rgba(0,0,0,.05) 25%, transparent 25%) 0 0 / 22px 22px,
    linear-gradient(315deg, rgba(0,0,0,.05) 25%, transparent 25%) 0 0 / 22px 22px,
    linear-gradient(145deg, #dfeae0, #cddfd0); }}
.sprite-frame img {{ width: 100%; height: 100%; object-fit: contain; image-rendering: auto; filter: drop-shadow(0 10px 12px rgba(0,0,0,.28)); }}
figcaption {{ text-align: center; color: var(--muted); font-size: 12px; padding-top: 8px; }}
.palette {{ display: flex; align-items: center; gap: 7px; margin-top: 16px; flex-wrap: wrap; }}
.swatch {{ width: 22px; height: 22px; border-radius: 7px; border: 1px solid rgba(255,255,255,.18); box-shadow: inset 0 0 0 1px rgba(0,0,0,.12); }}
.palette-note {{ margin-left: 4px; color: var(--muted); font-size: 11px; }}
.toast {{
  position: fixed; left: 50%; bottom: 26px; transform: translate(-50%, 14px); opacity: 0;
  background: #0c151b; color: #e9f7fa; border: 1px solid #3c6a74; border-radius: 999px;
  padding: 10px 16px; pointer-events: none; transition: .2s ease; z-index: 20; font-size: 13px;
}}
.toast.show {{ opacity: 1; transform: translate(-50%, 0); }}
@media (max-width: 860px) {{
  header {{ flex-direction: column; align-items: flex-start; }}
  .toolbar {{ justify-content: flex-start; }}
  .sprite-row {{ grid-template-columns: repeat(2, minmax(0, 1fr)); }}
  .panel-head {{ flex-direction: column; }}
}}
</style>
</head>
<body>
<div class="wrap">
  <header>
    <div>
      <p class="eyebrow">ColonyKit · Nanny Robot Redraw</p>
      <h1>{html.escape(title)}</h1>
      <p class="sub">{html.escape(subtitle)}</p>
    </div>
    <div class="toolbar">
      <button class="ghost" id="theme" type="button">浅色预览</button>
      <button class="ghost" id="prev" type="button">← 上一个</button>
      <button class="ghost" id="next" type="button">下一个 →</button>
    </div>
  </header>

  <nav class="tabs" role="tablist" aria-label="机器人贴图方案">
    {''.join(tabs)}
  </nav>

  {''.join(panels)}
</div>
<div class="toast" id="toast" role="status"></div>
<script>
const tabs = Array.from(document.querySelectorAll('.tab'));
const panels = Array.from(document.querySelectorAll('.panel'));
let current = 0;
function show(index) {{
  current = (index + tabs.length) % tabs.length;
  tabs.forEach((tab, i) => tab.classList.toggle('active', i === current));
  panels.forEach((panel, i) => panel.classList.toggle('active', i === current));
  document.querySelector('.tabs').scrollIntoView({{ behavior: 'smooth', block: 'nearest' }});
}}
tabs.forEach((tab, i) => tab.addEventListener('click', () => show(i)));
document.getElementById('prev').addEventListener('click', () => show(current - 1));
document.getElementById('next').addEventListener('click', () => show(current + 1));
document.addEventListener('keydown', (event) => {{
  if (event.key === 'ArrowLeft') show(current - 1);
  if (event.key === 'ArrowRight') show(current + 1);
}});
document.getElementById('theme').addEventListener('click', (event) => {{
  document.body.classList.toggle('light');
  event.currentTarget.textContent = document.body.classList.contains('light') ? '深色预览' : '浅色预览';
}});
const toast = document.getElementById('toast');
let toastTimer = null;
document.querySelectorAll('.choose').forEach((button) => {{
  button.addEventListener('click', async () => {{
    const name = button.dataset.name;
    const text = '我选方案 ' + name;
    try {{ await navigator.clipboard.writeText(text); }} catch (err) {{ /* clipboard may be blocked */ }}
    toast.textContent = '已记录：' + name + '（可粘贴给开发者）';
    toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('show'), 2200);
  }});
}});
</script>
</body>
</html>
"""


STYLES_BATCH1 = STYLES[:10]
STYLES_BATCH2 = STYLES[10:]


def generate(styles: list[Style], out_dir: str, html_path: str, title: str, subtitle: str):
    os.makedirs(out_dir, exist_ok=True)
    images: dict[str, dict[str, Image.Image]] = {}
    for style in styles:
        images[style.slug] = {}
        for view in VIEWS:
            image = draw_robot(style, view)
            if view == "west":
                image = ImageOps.mirror(images[style.slug]["east"])
            images[style.slug][view] = image
            path = os.path.join(out_dir, f"{style.number}_{style.slug}_{view}.png")
            image.save(path)
        print("wrote", style.number, style.slug)

    with open(html_path, "w", encoding="utf-8") as handle:
        handle.write(render_html(images, styles, title, subtitle))
    print("wrote", html_path)


def main():
    generate(
        STYLES_BATCH1,
        OUT_DIR,
        HTML_PATH,
        "保姆机器人贴图候选",
        "10 个风格方案，每个方案含南 / 北 / 东 / 西四向。点击页签切换；选好后告诉我方案编号即可。",
    )


if __name__ == "__main__":
    main()
