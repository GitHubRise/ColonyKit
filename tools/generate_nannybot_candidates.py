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
    return (28, 44, 100, 102), 18


def draw_body(draw: ImageDraw.ImageDraw, style: Style, facing: str):
    box, radius = body_box(style)
    x1, y1, x2, y2 = box
    rr(draw, (P(x1), P(y1), P(x2), P(y2)), P(radius), style.shell, OUTLINE, P(1.5))
    if style.body == "box":
        rr(draw, (P(x1 + 5), P(y1 + 5), P(x2 - 5), P(y2 - 5)), P(6), None, style.shell_dark, P(1))
        # corner bolts
        for bx, by in ((x1 + 4, y1 + 4), (x2 - 8, y1 + 4), (x1 + 4, y2 - 8), (x2 - 8, y2 - 8)):
            draw.ellipse((P(bx), P(by), P(bx + 4), P(by + 4)), fill=style.accent_light, outline=OUTLINE, width=P(1))
    else:
        rr(draw, (P(x1 + 6), P(y1 + 6), P(x2 - 6), P(y2 - 7)), P(max(6, radius - 4)), None, style.shell_dark, P(1))

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


def draw_head(draw: ImageDraw.ImageDraw, style: Style, facing: str):
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
    top = 14
    if style.head == "box":
        top = 13
    elif style.head == "bubble":
        top = 14
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


def render_html(images: dict[str, dict[str, Image.Image]]) -> str:
    tabs = []
    panels = []
    for index, style in enumerate(STYLES):
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
<title>保姆机器人贴图候选 · 10 选 1</title>
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
      <h1>保姆机器人贴图候选</h1>
      <p class="sub">10 个风格方案，每个方案含南 / 北 / 东 / 西四向。点击页签切换；选好后告诉我方案编号即可。</p>
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


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    images: dict[str, dict[str, Image.Image]] = {}
    for style in STYLES:
        images[style.slug] = {}
        for view in VIEWS:
            image = draw_robot(style, view)
            if view == "west":
                image = ImageOps.mirror(images[style.slug]["east"])
            images[style.slug][view] = image
            path = os.path.join(OUT_DIR, f"{style.number}_{style.slug}_{view}.png")
            image.save(path)
        print("wrote", style.number, style.slug)

    with open(HTML_PATH, "w", encoding="utf-8") as handle:
        handle.write(render_html(images))
    print("wrote", HTML_PATH)


if __name__ == "__main__":
    main()
