"""Generate front/side/back turnaround boards for the 19 robotics items."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASE = os.path.join(ROOT, "docs", "robot-textures")
FRONT_BOARD = os.path.join(BASE, "CK_Robotics_FrontView_Board.png")

W, H = 2400, 1950
MARGIN = 40
LABEL_W = 300
FRONT_W = 450
SIDE_W = 700
BACK_W = 700
GAP = 15
ROW_H = 350
TOP = 128

BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (25, 32, 40, 255)
DARK = (44, 54, 64, 255)
DARK2 = (34, 42, 51, 255)
STEEL = (136, 150, 162, 255)
STEEL_LIGHT = (196, 207, 214, 255)
STEEL_MID = (163, 176, 187, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)
TREAD = (38, 44, 52, 255)
TREAD_LIGHT = (82, 91, 102, 255)

FONT_PATH = r"C:\Windows\Fonts\msyh.ttc"
FONT_BOLD = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT_PATH, size)


F_TITLE = font(38, True)
F_SUB = font(16)
F_LABEL = font(20, True)
F_SMALL = font(12)
F_TINY = font(11)


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(d, points, fill, outline=None):
    d.polygon(points, fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line(points, fill=fill, width=width, joint="curve")


def bolt(d, x, y, r=3):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


def hazard(d, x1, y1, x2, y2, n=4):
    d.rectangle((x1, y1, x2, y2), fill=DARK2, outline=OUTLINE, width=1)
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


TIERS = {
    "I": dict(shell=STEEL_LIGHT, shell2=STEEL_MID, accent=(61, 126, 166, 255), glow=CYAN, head="dome", tool="broom"),
    "II": dict(shell=(218, 228, 220, 255), shell2=(172, 194, 178, 255), accent=(83, 145, 104, 255), glow=GREEN, head="round", tool="module"),
    "III": dict(shell=(232, 184, 76, 255), shell2=(164, 129, 62, 255), accent=(52, 60, 68, 255), glow=AMBER_LIGHT, head="box", tool="heavy"),
    "IV": dict(shell=(226, 242, 242, 255), shell2=(168, 207, 211, 255), accent=(44, 139, 155, 255), glow=CYAN, head="bubble", tool="scout"),
    "V": dict(shell=(245, 238, 216, 255), shell2=(203, 185, 146, 255), accent=(198, 154, 58, 255), glow=AMBER_LIGHT, head="dome", tool="crown"),
}


def draw_side_robot(d, cx, cy, tier):
    p = TIERS[tier]
    # shadow
    ellipse(d, (cx - 150, cy + 102, cx + 150, cy + 120), (0, 0, 0, 52))
    # one long tread
    rr(d, (cx - 125, cy + 55, cx + 120, cy + 105), 12, TREAD, OUTLINE, 2)
    for x in range(int(cx - 112), int(cx + 110), 18):
        d.rectangle((x, cy + 66, x + 7, cy + 96), fill=TREAD_LIGHT)
    rr(d, (cx - 130, cy + 48, cx + 125, cy + 62), 6, STEEL, OUTLINE, 2)
    # rear pack
    rr(d, (cx - 105, cy - 42, cx - 55, cy + 50), 8, DARK, OUTLINE, 2)
    d.rectangle((cx - 96, cy - 32, cx - 66, cy - 22), fill=AMBER, outline=OUTLINE, width=1)
    d.rectangle((cx - 96, cy - 8, cx - 66, cy - 2), fill=CYAN_DIM, outline=OUTLINE, width=1)
    # body profile
    rr(d, (cx - 70, cy - 62, cx + 88, cy + 62), 18, p["shell"], OUTLINE, 2)
    rr(d, (cx - 58, cy - 50, cx + 72, cy + 50), 13, p["shell2"], OUTLINE, 1)
    # side armor / screen
    rr(d, (cx - 38, cy - 42, cx + 40, cy + 40), 10, p["accent"], OUTLINE, 1)
    for y in (cy - 28, cy - 12, cy + 4, cy + 20):
        d.rectangle((cx - 28, y, cx + 24, y + 5), fill=p["shell"], outline=OUTLINE, width=1)
    # front edge screen
    rr(d, (cx + 52, cy - 32, cx + 80, cy + 18), 5, PANEL, OUTLINE, 1)
    d.rectangle((cx + 58, cy - 23, cx + 74, cy - 17), fill=p["glow"])
    d.rectangle((cx + 58, cy - 7, cx + 74, cy - 2), fill=GREEN)
    # head side
    hx1, hy = cx - 58, cy - 135
    if p["head"] == "box":
        rr(d, (hx1, hy, cx + 68, hy + 55), 8, p["shell"], OUTLINE, 2)
        rr(d, (hx1 + 8, hy + 7, cx + 58, hy + 45), 5, p["shell2"], OUTLINE, 1)
        rr(d, (cx + 24, hy + 16, cx + 60, hy + 34), 4, (18, 27, 35, 255), OUTLINE, 1)
        d.rectangle((cx + 32, hy + 23, cx + 52, hy + 27), fill=p["glow"])
    elif p["head"] == "bubble":
        ellipse(d, (hx1, hy, cx + 56, hy + 60), p["shell"], OUTLINE, 2)
        ellipse(d, (hx1 + 8, hy + 8, cx + 48, hy + 52), p["shell2"], OUTLINE, 1)
        ellipse(d, (cx + 14, hy + 17, cx + 48, hy + 42), (*CYAN[:3], 110), OUTLINE, 1)
        ellipse(d, (cx + 25, hy + 22, cx + 40, hy + 36), p["glow"])
    else:
        ellipse(d, (hx1, hy, cx + 56, hy + 60), p["shell"], OUTLINE, 2)
        ellipse(d, (hx1 + 8, hy + 8, cx + 48, hy + 52), p["shell2"], OUTLINE, 1)
        ellipse(d, (cx + 12, hy + 17, cx + 50, hy + 43), (20, 28, 37, 255), OUTLINE, 1)
        ellipse(d, (cx + 26, hy + 22, cx + 44, hy + 37), p["glow"])
    # rear head vent
    for x in (cx - 44, cx - 34, cx - 24):
        d.rectangle((x, hy + 18, x + 5, hy + 40), fill=STEEL_MID)
    # antenna
    line(d, [(cx - 6, hy + 4), (cx - 6, hy - 22)], OUTLINE, 3)
    ellipse(d, (cx - 15, hy - 38, cx + 3, hy - 20), AMBER if tier != "V" else AMBER_LIGHT, OUTLINE, 1)
    if tier == "V":
        polygon(d, [(cx - 26, hy - 22), (cx - 22, hy - 40), (cx - 10, hy - 30), (cx - 6, hy - 46), (cx + 2, hy - 30), (cx + 14, hy - 40), (cx + 18, hy - 22)], AMBER, OUTLINE)
    # visible arm
    line(d, [(cx + 42, cy - 20), (cx + 86, cy + 16), (cx + 106, cy + 48)], OUTLINE, 20)
    line(d, [(cx + 42, cy - 20), (cx + 86, cy + 16), (cx + 106, cy + 48)], STEEL_MID, 11)
    rr(d, (cx + 98, cy + 38, cx + 122, cy + 64), 4, STEEL, OUTLINE, 1)
    if tier == "III":
        line(d, [(cx + 120, cy + 50), (cx + 150, cy + 64)], OUTLINE, 6)
        line(d, [(cx + 120, cy + 50), (cx + 150, cy + 64)], AMBER, 3)
    elif tier == "IV":
        ellipse(d, (cx + 112, cy + 52, cx + 130, cy + 70), CYAN_DIM, OUTLINE, 1)
    elif tier == "V":
        line(d, [(cx + 120, cy + 50), (cx + 145, cy + 50)], OUTLINE, 5)
        line(d, [(cx + 120, cy + 50), (cx + 145, cy + 50)], CYAN, 2)
    # tier badge
    rr(d, (cx - 20, cy + 68, cx + 20, cy + 86), 4, DARK2, OUTLINE, 1)
    d.text((cx - 8, cy + 69), tier, font=F_TINY, fill=p["glow"])


def draw_back_robot(d, cx, cy, tier):
    p = TIERS[tier]
    ellipse(d, (cx - 120, cy + 100, cx + 120, cy + 118), (0, 0, 0, 52))
    for side in (-1, 1):
        tx = cx + side * 62
        rr(d, (tx - 28, cy + 55, tx + 28, cy + 105), 9, TREAD, OUTLINE, 2)
        for i in range(3):
            d.rectangle((tx - 20 + i * 13, cy + 66, tx - 14 + i * 13, cy + 96), fill=TREAD_LIGHT)
    # body back
    rr(d, (cx - 95, cy - 62, cx + 95, cy + 55), 18, p["shell"], OUTLINE, 2)
    rr(d, (cx - 80, cy - 48, cx + 80, cy + 43), 12, p["shell2"], OUTLINE, 1)
    # rear hatch
    rr(d, (cx - 58, cy - 38, cx + 58, cy + 30), 8, DARK2, OUTLINE, 2)
    rr(d, (cx - 46, cy - 28, cx + 46, cy + 20), 5, PANEL, OUTLINE, 1)
    for y in (cy - 18, cy - 8, cy + 2, cy + 12):
        d.rectangle((cx - 36, y, cx + 36, y + 4), fill=STEEL_MID)
    d.rectangle((cx - 36, cy + 20, cx + 36, cy + 24), fill=p["accent"], outline=OUTLINE, width=1)
    # power cells
    for x in (cx - 78, cx + 52):
        rr(d, (x, cy - 30, x + 24, cy + 30), 6, STEEL_MID, OUTLINE, 1)
        d.rectangle((x + 5, cy - 20, x + 19, cy - 14), fill=AMBER)
        d.rectangle((x + 5, cy - 4, x + 19, cy + 2), fill=CYAN_DIM)
    # arms at sides
    for side in (-1, 1):
        ax = cx + side * 108
        ellipse(d, (ax - 18, cy - 30, ax + 18, cy + 6), STEEL_MID, OUTLINE, 1)
        rr(d, (ax - 12, cy - 10, ax + 12, cy + 46), 5, STEEL, OUTLINE, 1)
        rr(d, (ax - 10, cy + 40, ax + 10, cy + 56), 3, STEEL_LIGHT, OUTLINE, 1)
    # head back
    hy = cy - 136
    if p["head"] == "box":
        rr(d, (cx - 62, hy, cx + 62, hy + 54), 9, p["shell"], OUTLINE, 2)
        rr(d, (cx - 52, hy + 8, cx + 52, hy + 45), 5, p["shell2"], OUTLINE, 1)
    else:
        ellipse(d, (cx - 56, hy, cx + 56, hy + 58), p["shell"], OUTLINE, 2)
        ellipse(d, (cx - 47, hy + 8, cx + 47, hy + 50), p["shell2"], OUTLINE, 1)
    rr(d, (cx - 38, hy + 16, cx + 38, hy + 34), 4, (20, 28, 37, 255), OUTLINE, 1)
    for x in (cx - 28, cx - 14, cx, cx + 14, cx + 28):
        d.rectangle((x - 3, hy + 20, x + 3, hy + 30), fill=STEEL_MID)
    for x in (cx - 65, cx + 58):
        ellipse(d, (x - 5, hy + 22, x + 5, hy + 32), AMBER, OUTLINE, 1)
    # antenna
    line(d, [(cx, hy + 3), (cx, hy - 24)], OUTLINE, 3)
    ellipse(d, (cx - 9, hy - 40, cx + 9, hy - 22), AMBER, OUTLINE, 1)
    # badge
    rr(d, (cx - 22, cy + 32, cx + 22, cy + 48), 4, DARK2, OUTLINE, 1)
    d.text((cx - 9, cy + 33), tier, font=F_TINY, fill=p["glow"])


def draw_side_charger(d, cx, cy, kind):
    ellipse(d, (cx - 120, cy + 100, cx + 120, cy + 118), (0, 0, 0, 45))
    if kind == "standard":
        # side pedestal
        ellipse(d, (cx - 90, cy + 78, cx + 90, cy + 104), DARK2, OUTLINE, 2)
        rr(d, (cx - 26, cy - 45, cx + 26, cy + 82), 12, STEEL, OUTLINE, 2)
        rr(d, (cx - 15, cy - 28, cx + 15, cy + 20), 7, PANEL, OUTLINE, 1)
        d.rectangle((cx - 8, cy - 20, cx + 8, cy + 12), fill=CYAN)
        d.arc((cx + 22, cy + 10, cx + 96, cy + 80), 270, 90, fill=OUTLINE, width=4)
        d.arc((cx + 22, cy + 10, cx + 96, cy + 80), 270, 90, fill=AMBER, width=2)
        rr(d, (cx - 22, cy - 58, cx + 22, cy - 48), 4, DARK, OUTLINE, 1)
    elif kind == "detachable":
        rr(d, (cx - 70, cy - 30, cx + 70, cy + 76), 12, STEEL_MID, OUTLINE, 2)
        rr(d, (cx - 58, cy - 18, cx + 58, cy + 58), 8, PANEL, OUTLINE, 1)
        d.rectangle((cx - 42, cy - 4, cx + 42, cy + 6), fill=CYAN)
        d.rectangle((cx - 42, cy + 18, cx + 10, cy + 24), fill=GREEN)
        d.arc((cx - 44, cy - 74, cx + 44, cy - 12), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 44, cy - 74, cx + 44, cy - 12), 180, 360, fill=AMBER, width=2)
        for side in (-1, 1):
            ellipse(d, (cx + side * 52 - 14, cy + 66, cx + side * 52 + 14, cy + 94), TREAD, OUTLINE, 2)
            ellipse(d, (cx + side * 52 - 5, cy + 75, cx + side * 52 + 5, cy + 85), STEEL_MID, OUTLINE, 1)
        # rear cable
        line(d, [(cx - 68, cy + 20), (cx - 100, cy + 20), (cx - 100, cy + 58)], OUTLINE, 3)
    else:
        rr(d, (cx - 78, cy - 10, cx + 78, cy + 76), 10, (73, 91, 88, 255), OUTLINE, 2)
        rr(d, (cx - 66, cy + 2, cx + 66, cy + 60), 6, PANEL, OUTLINE, 1)
        d.rectangle((cx - 52, cy + 14, cx - 4, cy + 24), fill=CYAN)
        d.rectangle((cx - 52, cy + 32, cx - 16, cy + 38), fill=GREEN)
        # solar panel side
        polygon(d, [(cx - 68, cy - 22), (cx + 10, cy - 62), (cx + 70, cy - 42), (cx - 10, cy - 2)], (31, 60, 77, 255), OUTLINE)
        for i in range(1, 4):
            line(d, [(cx - 62 + i * 18, cy - 18 - i * 7), (cx + 2 + i * 18, cy - 52 - i * 7)], CYAN_DIM, 2)
        d.arc((cx - 42, cy + 62, cx + 42, cy + 92), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 42, cy + 62, cx + 42, cy + 92), 180, 360, fill=AMBER, width=2)


def draw_back_charger(d, cx, cy, kind):
    ellipse(d, (cx - 110, cy + 100, cx + 110, cy + 118), (0, 0, 0, 45))
    if kind == "standard":
        ellipse(d, (cx - 85, cy + 78, cx + 85, cy + 104), DARK2, OUTLINE, 2)
        rr(d, (cx - 32, cy - 48, cx + 32, cy + 82), 12, STEEL, OUTLINE, 2)
        rr(d, (cx - 22, cy - 34, cx + 22, cy + 50), 8, DARK2, OUTLINE, 1)
        for y in (cy - 24, cy - 10, cy + 4, cy + 18, cy + 32):
            d.rectangle((cx - 15, y, cx + 15, y + 4), fill=STEEL_MID)
        rr(d, (cx - 24, cy - 60, cx + 24, cy - 50), 4, DARK, OUTLINE, 1)
        # rear cable port
        ellipse(d, (cx - 8, cy + 56, cx + 8, cy + 72), DARK2, OUTLINE, 1)
    elif kind == "detachable":
        rr(d, (cx - 70, cy - 30, cx + 70, cy + 78), 12, STEEL_MID, OUTLINE, 2)
        rr(d, (cx - 58, cy - 18, cx + 58, cy + 52), 8, DARK2, OUTLINE, 1)
        for y in (cy - 8, cy + 6, cy + 20):
            d.rectangle((cx - 42, y, cx + 42, y + 5), fill=STEEL_MID)
        d.arc((cx - 44, cy - 74, cx + 44, cy - 12), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 44, cy - 74, cx + 44, cy - 12), 180, 360, fill=AMBER, width=2)
        for side in (-1, 1):
            ellipse(d, (cx + side * 52 - 14, cy + 66, cx + side * 52 + 14, cy + 94), TREAD, OUTLINE, 2)
        # rear plug
        rr(d, (cx - 14, cy + 54, cx + 14, cy + 76), 3, AMBER, OUTLINE, 1)
    else:
        rr(d, (cx - 78, cy - 8, cx + 78, cy + 76), 10, (73, 91, 88, 255), OUTLINE, 2)
        rr(d, (cx - 66, cy + 4, cx + 66, cy + 62), 6, DARK2, OUTLINE, 1)
        for y in (cy + 14, cy + 30, cy + 46):
            d.rectangle((cx - 50, y, cx + 50, y + 5), fill=STEEL_MID)
        # solar panel back edge
        polygon(d, [(cx - 68, cy - 22), (cx + 10, cy - 62), (cx + 70, cy - 42), (cx - 10, cy - 2)], (26, 49, 62, 255), OUTLINE)
        d.arc((cx - 42, cy + 62, cx + 42, cy + 92), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 42, cy + 62, cx + 42, cy + 92), 180, 360, fill=AMBER, width=2)


def draw_side_repair(d, cx, cy, kind):
    ellipse(d, (cx - 130, cy + 100, cx + 130, cy + 118), (0, 0, 0, 45))
    # side base / ramp
    polygon(d, [(cx - 110, cy + 74), (cx + 110, cy + 74), (cx + 88, cy + 98), (cx - 88, cy + 98)], DARK2, OUTLINE)
    rr(d, (cx - 104, cy + 62, cx + 104, cy + 82), 6, PANEL, OUTLINE, 1)
    # single side frame
    rr(d, (cx - 96, cy - 70, cx - 72, cy + 80), 5, STEEL, OUTLINE, 2)
    rr(d, (cx + 82, cy - 70, cx + 106, cy + 80), 5, STEEL, OUTLINE, 2)
    rr(d, (cx - 100, cy - 82, cx + 110, cy - 62), 6, STEEL_MID, OUTLINE, 2)
    for x in (cx - 82, cx + 92):
        ellipse(d, (x - 6, cy - 74, x + 6, cy - 62), CYAN if kind == "auto" else AMBER, OUTLINE, 1)
    # robot side silhouette
    ellipse(d, (cx - 20, cy - 50, cx + 36, cy + 8), STEEL_MID, OUTLINE, 1)
    rr(d, (cx - 16, cy - 14, cx + 36, cy + 56), 8, DARK, OUTLINE, 1)
    rr(d, (cx + 10, cy - 28, cx + 30, cy - 8), 4, PANEL, OUTLINE, 1)
    d.rectangle((cx + 14, cy - 23, cx + 26, cy - 17), fill=CYAN)
    # arm
    if kind == "auto":
        for side in (-1, 1):
            line(d, [(cx + side * 88, cy - 40), (cx + side * 48, cy - 10), (cx + side * 30, cy + 22)], OUTLINE, 5)
            line(d, [(cx + side * 88, cy - 40), (cx + side * 48, cy - 10), (cx + side * 30, cy + 22)], STEEL_LIGHT, 2)
            rr(d, (cx + side * 30 - 8, cy + 18, cx + side * 30 + 8, cy + 32), 3, AMBER, OUTLINE, 1)
        d.text((cx - 40, cy + 88), "AUTO", font=F_TINY, fill=CYAN)
    else:
        line(d, [(cx + 92, cy - 30), (cx + 48, cy + 12), (cx + 30, cy + 48)], OUTLINE, 5)
        line(d, [(cx + 92, cy - 30), (cx + 48, cy + 12), (cx + 30, cy + 48)], STEEL_LIGHT, 2)
        rr(d, (cx - 96, cy + 50, cx - 42, cy + 84), 5, DARK, OUTLINE, 1)
        d.rectangle((cx - 88, cy + 58, cx - 50, cy + 64), fill=AMBER)
        d.text((cx - 96, cy + 88), "FIELD", font=F_TINY, fill=AMBER)
    hazard(d, int(cx - 38), int(cy + 84), int(cx + 38), int(cy + 92), 5)


def draw_back_repair(d, cx, cy, kind):
    ellipse(d, (cx - 130, cy + 100, cx + 130, cy + 118), (0, 0, 0, 45))
    polygon(d, [(cx - 110, cy + 74), (cx + 110, cy + 74), (cx + 88, cy + 98), (cx - 88, cy + 98)], DARK2, OUTLINE)
    rr(d, (cx - 104, cy + 62, cx + 104, cy + 82), 6, PANEL, OUTLINE, 1)
    # rear frame
    rr(d, (cx - 104, cy - 70, cx - 80, cy + 80), 5, STEEL, OUTLINE, 2)
    rr(d, (cx + 80, cy - 70, cx + 104, cy + 80), 5, STEEL, OUTLINE, 2)
    rr(d, (cx - 104, cy - 82, cx + 104, cy - 62), 6, STEEL_MID, OUTLINE, 2)
    # rear hatch
    rr(d, (cx - 58, cy - 42, cx + 58, cy + 56), 7, DARK2, OUTLINE, 2)
    rr(d, (cx - 47, cy - 32, cx + 47, cy + 44), 5, PANEL, OUTLINE, 1)
    for y in (cy - 22, cy - 12, cy - 2, cy + 8, cy + 18):
        d.rectangle((cx - 36, y, cx + 36, y + 4), fill=STEEL_MID)
    d.rectangle((cx - 36, cy + 30, cx + 36, cy + 35), fill=AMBER)
    # cables
    for side in (-1, 1):
        d.arc((cx + side * 80 - 14, cy - 36, cx + side * 80 + 14, cy + 24), 180, 360, fill=OUTLINE, width=3)
        rr(d, (cx + side * 80 - 12, cy + 50, cx + side * 80 + 12, cy + 78), 3, DARK, OUTLINE, 1)
    # lights
    for x in (cx - 90, cx + 90):
        ellipse(d, (x - 6, cy - 74, x + 6, cy - 62), CYAN if kind == "auto" else AMBER, OUTLINE, 1)
    hazard(d, int(cx - 38), int(cy + 84), int(cx + 38), int(cy + 92), 5)


def draw_side_factory(d, cx, cy, kind):
    ellipse(d, (cx - 160, cy + 100, cx + 160, cy + 118), (0, 0, 0, 45))
    # side wall block
    rr(d, (cx - 135, cy - 55, cx + 120, cy + 86), 8, DARK, OUTLINE, 2)
    # roof
    polygon(d, [(cx - 145, cy - 55), (cx + 130, cy - 55), (cx + 105, cy - 88), (cx - 120, cy - 88)], STEEL, OUTLINE)
    # side wall panels / door
    rr(d, (cx - 72, cy - 12, cx + 58, cy + 64), 5, PANEL, OUTLINE, 1)
    for x in range(int(cx - 62), int(cx + 50), 18):
        d.rectangle((x, cy + 10, x + 11, cy + 16), fill=STEEL_MID)
    # side vent
    for y in (cy - 38, cy - 28):
        d.rectangle((cx + 58, y, cx + 104, y + 5), fill=STEEL_MID)
    if kind == "workshop":
        rr(d, (cx + 82, cy - 126, cx + 106, cy - 66), 4, STEEL_MID, OUTLINE, 1)
        d.rectangle((cx + 80, cy - 132, cx + 108, cy - 120), fill=AMBER, outline=OUTLINE, width=1)
        # gear sign side
        ellipse(d, (cx + 40, cy - 54, cx + 78, cy - 16), AMBER, OUTLINE, 1)
        ellipse(d, (cx + 52, cy - 42, cx + 66, cy - 28), DARK2, OUTLINE, 1)
    elif kind == "two-station":
        # second side cell
        rr(d, (cx + 112, cy - 42, cx + 148, cy + 76), 6, DARK, OUTLINE, 2)
        d.rectangle((cx + 120, cy - 20, cx + 140, cy + 30), fill=CYAN_DIM, outline=OUTLINE, width=1)
        d.text((cx - 30, cy - 112), "2-STATION", font=F_TINY, fill=AMBER)
    elif kind == "frame":
        # gantry rail side
        rr(d, (cx - 145, cy - 82, cx + 140, cy - 70), 3, STEEL_LIGHT, OUTLINE, 1)
        line(d, [(cx - 70, cy - 74), (cx - 40, cy - 30), (cx - 20, cy + 4)], OUTLINE, 4)
        line(d, [(cx - 70, cy - 74), (cx - 40, cy - 30), (cx - 20, cy + 4)], STEEL_LIGHT, 2)
        line(d, [(cx - 20, cy + 4), (cx - 12, cy + 14)], CYAN, 2)
    elif kind == "prefab":
        # side containers
        for i, (x, y, col) in enumerate(((cx + 20, cy - 140, STEEL), (cx + 20, cy - 80, AMBER), (cx - 70, cy - 140, CYAN_DIM))):
            rr(d, (x, y, x + 66, y + 46), 4, col, OUTLINE, 2)
            d.rectangle((x + 8, y + 10, x + 58, y + 15), fill=DARK2, outline=OUTLINE, width=1)
            d.text((x + 10, y + 24), f"M{i+1}", font=F_TINY, fill=TEXT if i != 2 else DARK2)
        line(d, [(cx + 104, cy + 70), (cx + 104, cy - 150)], OUTLINE, 5)
        line(d, [(cx + 104, cy - 150), (cx + 40, cy - 150)], OUTLINE, 5)
        line(d, [(cx + 40, cy - 150), (cx + 40, cy - 120)], OUTLINE, 2)
    else:
        # auto factory side vents / arms
        for y in (cy - 30, cy - 6, cy + 18):
            d.rectangle((cx - 120, y, cx + 50, y + 5), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
        line(d, [(cx + 90, cy + 50), (cx + 40, cy + 10), (cx + 20, cy - 20)], OUTLINE, 5)
        line(d, [(cx + 90, cy + 50), (cx + 40, cy + 10), (cx + 20, cy - 20)], STEEL_LIGHT, 2)
        ellipse(d, (cx + 12, cy - 28, cx + 30, cy - 10), AMBER, OUTLINE, 1)
        d.text((cx - 20, cy + 90), "UNMANNED", font=F_TINY, fill=CYAN)


def draw_back_factory(d, cx, cy, kind):
    ellipse(d, (cx - 160, cy + 100, cx + 160, cy + 118), (0, 0, 0, 45))
    rr(d, (cx - 135, cy - 55, cx + 135, cy + 86), 8, DARK, OUTLINE, 2)
    polygon(d, [(cx - 145, cy - 55), (cx + 145, cy - 55), (cx + 120, cy - 88), (cx - 120, cy - 88)], STEEL, OUTLINE)
    # rear loading door
    rr(d, (cx - 70, cy - 10, cx + 70, cy + 64), 6, PANEL, OUTLINE, 1)
    for y in (cy + 2, cy + 14, cy + 26, cy + 38):
        d.rectangle((cx - 58, y, cx + 58, y + 5), fill=STEEL_MID)
    # rear vents / pipes
    for x in (cx - 116, cx + 100):
        rr(d, (x, cy - 40, x + 16, cy + 50), 4, STEEL_MID, OUTLINE, 1)
        d.rectangle((x + 3, cy - 30, x + 13, cy + 36), fill=DARK2)
    if kind == "workshop":
        rr(d, (cx + 70, cy - 126, cx + 94, cy - 66), 4, STEEL_MID, OUTLINE, 1)
        d.rectangle((cx + 68, cy - 132, cx + 96, cy - 120), fill=AMBER, outline=OUTLINE, width=1)
        ellipse(d, (cx - 38, cy - 52, cx + 0, cy - 14), AMBER, OUTLINE, 1)
        ellipse(d, (cx - 26, cy - 40, cx - 12, cy - 26), DARK2, OUTLINE, 1)
    elif kind == "two-station":
        d.text((cx - 38, cy - 112), "2-STATION", font=F_TINY, fill=AMBER)
        d.rectangle((cx - 54, cy - 34, cx - 10, cy - 24), fill=AMBER, outline=OUTLINE, width=1)
        d.rectangle((cx + 10, cy - 34, cx + 54, cy - 24), fill=CYAN_DIM, outline=OUTLINE, width=1)
    elif kind == "frame":
        rr(d, (cx - 145, cy - 82, cx + 140, cy - 70), 3, STEEL_LIGHT, OUTLINE, 1)
        # rear frame skeleton
        line(d, [(cx - 50, cy - 62), (cx + 50, cy - 62), (cx + 50, cy + 10), (cx - 50, cy + 10), (cx - 50, cy - 62)], STEEL_LIGHT, 3)
        for x in (cx - 50, cx + 50):
            ellipse(d, (x - 4, cy - 66, x + 4, cy - 58), AMBER, OUTLINE, 1)
    elif kind == "prefab":
        for i, (x, y, col) in enumerate(((cx - 120, cy - 140, STEEL), (cx - 50, cy - 140, AMBER), (cx - 120, cy - 80, CYAN_DIM), (cx - 50, cy - 80, STEEL_MID))):
            rr(d, (x, y, x + 66, y + 46), 4, col, OUTLINE, 2)
            d.rectangle((x + 8, y + 10, x + 58, y + 15), fill=DARK2, outline=OUTLINE, width=1)
            d.text((x + 10, y + 24), f"M{i+1}", font=F_TINY, fill=TEXT if i != 2 else DARK2)
    else:
        # auto factory rear: dark glass and central core
        for y in (cy - 30, cy - 6, cy + 18):
            d.rectangle((cx - 116, y, cx + 116, y + 6), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
            for x in range(int(cx - 110), int(cx + 110), 20):
                d.rectangle((x, y + 1, x + 10, y + 5), fill=CYAN_DIM)
        ellipse(d, (cx - 18, cy + 10, cx + 18, cy + 46), (25, 58, 70, 255), OUTLINE, 1)
        ellipse(d, (cx - 9, cy + 19, cx + 9, cy + 37), CYAN, None)
        d.text((cx - 52, cy + 92), "UNMANNED / 24H", font=F_TINY, fill=CYAN)


def draw_side_module(d, cx, cy, kind):
    if kind == "welder":
        # torch side
        line(d, [(cx - 100, cy + 70), (cx + 10, cy - 10)], OUTLINE, 26)
        line(d, [(cx - 100, cy + 70), (cx + 10, cy - 10)], STEEL_MID, 16)
        rr(d, (cx - 120, cy + 48, cx - 78, cy + 88), 6, DARK, OUTLINE, 2)
        d.rectangle((cx - 112, cy + 58, cx - 86, cy + 66), fill=AMBER)
        polygon(d, [(cx - 2, cy - 20), (cx + 26, cy - 20), (cx + 34, cy + 6), (cx + 6, cy + 6)], STEEL_LIGHT, OUTLINE)
        line(d, [(cx + 28, cy + 2), (cx + 52, cy + 22), (cx + 38, cy + 40), (cx + 64, cy + 60)], CYAN, 4)
        ellipse(d, (cx + 20, cy + 12, cx + 60, cy + 52), (*CYAN[:3], 70))
    elif kind == "baton":
        line(d, [(cx - 120, cy + 72), (cx + 94, cy - 50)], OUTLINE, 24)
        line(d, [(cx - 120, cy + 72), (cx + 94, cy - 50)], DARK, 15)
        line(d, [(cx - 110, cy + 64), (cx - 48, cy + 22)], AMBER, 9)
        ellipse(d, (cx + 70, cy - 74, cx + 122, cy - 22), CYAN_DIM, OUTLINE, 2)
        ellipse(d, (cx + 82, cy - 62, cx + 110, cy - 34), CYAN, None)
        for i in range(3):
            d.arc((cx + 62 - i * 6, cy - 80 + i * 5, cx + 130 + i * 4, cy - 16 - i * 5), 200, 340, fill=CYAN, width=2)
    elif kind == "armor":
        polygon(d, [(cx - 50, cy - 80), (cx + 50, cy - 80), (cx + 34, cy + 40), (cx, cy + 76), (cx - 34, cy + 40)], STEEL, OUTLINE)
        polygon(d, [(cx - 36, cy - 66), (cx + 36, cy - 66), (cx + 24, cy + 24), (cx, cy + 54), (cx - 24, cy + 24)], STEEL_MID, OUTLINE)
        for side in (-1, 1):
            line(d, [(cx + side * 40, cy - 60), (cx + side * 56, cy + 18), (cx + side * 40, cy + 48)], OUTLINE, 7)
            line(d, [(cx + side * 40, cy - 60), (cx + side * 56, cy + 18), (cx + side * 40, cy + 48)], DARK, 3)
    else:
        # arc rifle side profile
        polygon(d, [(cx - 130, cy + 20), (cx - 92, cy + 4), (cx - 80, cy + 44), (cx - 120, cy + 60)], DARK, OUTLINE)
        rr(d, (cx - 92, cy - 18, cx + 38, cy + 34), 5, STEEL, OUTLINE, 2)
        rr(d, (cx - 78, cy - 10, cx + 26, cy + 22), 3, DARK, OUTLINE, 1)
        polygon(d, [(cx - 34, cy + 28), (cx - 16, cy + 28), (cx - 6, cy + 66), (cx - 26, cy + 68)], DARK, OUTLINE)
        polygon(d, [(cx - 6, cy + 26), (cx + 18, cy + 24), (cx + 12, cy + 60), (cx - 8, cy + 62)], DARK2, OUTLINE)
        d.rectangle((cx + 2, cy + 34, cx + 12, cy + 40), fill=CYAN)
        rr(d, (cx + 30, cy - 14, cx + 116, cy + 18), 4, STEEL_MID, OUTLINE, 2)
        for i in range(4):
            x = cx + 42 + i * 17
            ellipse(d, (x, cy - 20, x + 12, cy + 20), None, CYAN, 2)
        line(d, [(cx + 116, cy + 2), (cx + 158, cy + 2)], OUTLINE, 8)
        line(d, [(cx + 116, cy + 2), (cx + 158, cy + 2)], STEEL_LIGHT, 4)
        ellipse(d, (cx + 154, cy - 12, cx + 182, cy + 16), (*CYAN[:3], 80))
        line(d, [(cx + 168, cy + 2), (cx + 190, cy - 10), (cx + 178, cy + 4)], CYAN, 2)


def draw_back_module(d, cx, cy, kind):
    if kind == "welder":
        # rear torch body / connector
        rr(d, (cx - 100, cy - 30, cx + 50, cy + 50), 10, DARK, OUTLINE, 2)
        rr(d, (cx - 86, cy - 18, cx + 36, cy + 36), 6, PANEL, OUTLINE, 1)
        for x in (cx - 70, cx - 40, cx - 10, cx + 20):
            d.rectangle((x, cy - 8, x + 12, cy + 20), fill=STEEL_MID)
        line(d, [(cx + 50, cy + 10), (cx + 110, cy + 10)], OUTLINE, 18)
        line(d, [(cx + 50, cy + 10), (cx + 110, cy + 10)], STEEL_MID, 10)
        d.arc((cx - 130, cy + 20, cx - 60, cy + 76), 180, 360, fill=OUTLINE, width=4)
    elif kind == "baton":
        line(d, [(cx - 110, cy + 70), (cx + 100, cy - 40)], OUTLINE, 24)
        line(d, [(cx - 110, cy + 70), (cx + 100, cy - 40)], DARK, 15)
        rr(d, (cx - 130, cy + 50, cx - 80, cy + 92), 6, STEEL_MID, OUTLINE, 2)
        rr(d, (cx - 122, cy + 60, cx - 88, cy + 68), 3, AMBER, OUTLINE, 1)
        ellipse(d, (cx + 80, cy - 68, cx + 132, cy - 16), CYAN_DIM, OUTLINE, 2)
        ellipse(d, (cx + 92, cy - 56, cx + 120, cy - 28), CYAN, None)
    elif kind == "armor":
        # inside of armor with straps
        polygon(d, [(cx - 62, cy - 80), (cx + 62, cy - 80), (cx + 46, cy + 40), (cx, cy + 78), (cx - 46, cy + 40)], STEEL_MID, OUTLINE)
        polygon(d, [(cx - 46, cy - 66), (cx + 46, cy - 66), (cx + 32, cy + 26), (cx, cy + 56), (cx - 32, cy + 26)], DARK2, OUTLINE)
        for side in (-1, 1):
            line(d, [(cx + side * 40, cy - 54), (cx + side * 64, cy + 8), (cx + side * 46, cy + 48)], OUTLINE, 8)
            line(d, [(cx + side * 40, cy - 54), (cx + side * 64, cy + 8), (cx + side * 46, cy + 48)], AMBER, 3)
        for x, y in ((cx - 30, cy - 46), (cx + 30, cy - 46), (cx - 20, cy + 20), (cx + 20, cy + 20)):
            bolt(d, x, y, 3)
    else:
        # rear of arc rifle: stock, capacitor, connectors
        polygon(d, [(cx - 150, cy + 16), (cx - 104, cy - 2), (cx - 92, cy + 38), (cx - 138, cy + 56)], DARK, OUTLINE)
        rr(d, (cx - 104, cy - 20, cx + 48, cy + 34), 5, STEEL, OUTLINE, 2)
        rr(d, (cx - 92, cy - 10, cx + 36, cy + 22), 3, DARK, OUTLINE, 1)
        rr(d, (cx - 70, cy + 40, cx - 30, cy + 72), 3, AMBER, OUTLINE, 1)
        for x in (cx - 44, cx - 12, cx + 20):
            d.rectangle((x, cy - 4, x + 10, cy + 18), fill=STEEL_MID)
        line(d, [(cx + 48, cy + 4), (cx + 120, cy + 4)], OUTLINE, 8)
        line(d, [(cx + 48, cy + 4), (cx + 120, cy + 4)], STEEL_LIGHT, 4)
        for i in range(4):
            x = cx + 58 + i * 15
            ellipse(d, (x, cy - 14, x + 10, cy + 22), None, CYAN, 2)


def crop_front(row, col):
    board = Image.open(FRONT_BOARD).convert("RGBA")
    cell_w, cell_h, gap, top = 420, 330, 55, 150
    x = MARGIN + col * (cell_w + gap)
    y = top + row * (cell_h + gap)
    crop = board.crop((x + 10, y + 18, x + cell_w - 10, y + cell_h - 40))
    return crop.resize((FRONT_W, 320), Image.LANCZOS)


def board(title, subtitle, items, path):
    height = TOP + len(items) * ROW_H + 60
    image = Image.new("RGBA", (W, height), BG)
    d = ImageDraw.Draw(image)
    for x in range(0, W, 40):
        d.line([(x, 0), (x, height)], fill=(27, 40, 47, 70), width=1)
    for y in range(0, height, 40):
        d.line([(0, y), (W, y)], fill=(27, 40, 47, 70), width=1)
    d.text((MARGIN, 26), title, font=F_TITLE, fill=TEXT)
    d.text((MARGIN + 3, 74), subtitle, font=F_SUB, fill=MUTED)
    # column headers
    x0 = MARGIN
    x1 = x0 + LABEL_W
    x2 = x1 + FRONT_W + GAP
    x3 = x2 + SIDE_W + GAP
    for x, label in ((x1, "FRONT / 参考"), (x2, "SIDE / 东-西"), (x3, "BACK / 北")):
        d.text((x + 12, TOP - 28), label, font=F_SMALL, fill=AMBER)
    for i, item in enumerate(items):
        y = TOP + i * ROW_H
        if i % 2 == 0:
            d.rectangle((MARGIN, y - 6, W - MARGIN, y + ROW_H - 10), fill=(1, 1, 1, 22))
        d.text((MARGIN + 10, y + 8), f"{item['index']:02d}", font=F_TINY, fill=AMBER)
        d.text((MARGIN + 10, y + 28), item["name"], font=F_LABEL, fill=TEXT)
        d.text((MARGIN + 10, y + 54), item["en"], font=F_SMALL, fill=MUTED)
        d.text((MARGIN + 10, y + 72), item["kind"], font=F_TINY, fill=MUTED)
        # front
        front = crop_front(item["row"], item["col"])
        image.alpha_composite(front, (x1, y + 8))
        d.rectangle((x1, y + 8, x1 + FRONT_W, y + 328), outline=LINE, width=1)
        # side/back
        side = Image.new("RGBA", (SIDE_W, 320), (0, 0, 0, 0))
        sd = ImageDraw.Draw(side)
        item["side"](sd, SIDE_W // 2, 168)
        image.alpha_composite(side, (x2, y + 8))
        d.rectangle((x2, y + 8, x2 + SIDE_W, y + 328), outline=LINE, width=1)
        back = Image.new("RGBA", (BACK_W, 320), (0, 0, 0, 0))
        bd = ImageDraw.Draw(back)
        item["back"](bd, BACK_W // 2, 168)
        image.alpha_composite(back, (x3, y + 8))
        d.rectangle((x3, y + 8, x3 + BACK_W, y + 328), outline=LINE, width=1)
    d.text((MARGIN, height - 46), "Turnaround concept board · side/back are approximate massing views, not final 128 px sprites.", font=F_TINY, fill=MUTED)
    image.convert("RGB").save(path, quality=96)
    print("wrote", path)


def main():
    robots = [
        ("01", "Mk-I 标准保姆机", "Mk-I STANDARD", "T1 · ROBOT", 0, 0, lambda d, x, y: draw_side_robot(d, x, y, "I"), lambda d, x, y: draw_back_robot(d, x, y, "I")),
        ("02", "Mk-II 模块化保姆机", "Mk-II MODULAR", "T2 · ROBOT", 0, 1, lambda d, x, y: draw_side_robot(d, x, y, "II"), lambda d, x, y: draw_back_robot(d, x, y, "II")),
        ("03", "Mk-III 重载保姆机", "Mk-III HEAVY", "T3 · ROBOT", 0, 2, lambda d, x, y: draw_side_robot(d, x, y, "III"), lambda d, x, y: draw_back_robot(d, x, y, "III")),
        ("04", "Mk-IV 前哨保姆机", "Mk-IV OUTPOST", "T4 · ROBOT", 0, 3, lambda d, x, y: draw_side_robot(d, x, y, "IV"), lambda d, x, y: draw_back_robot(d, x, y, "IV")),
        ("05", "Mk-V 自维护保姆机", "Mk-V SELF-MAINTAINING", "T5 · ROBOT", 0, 4, lambda d, x, y: draw_side_robot(d, x, y, "V"), lambda d, x, y: draw_back_robot(d, x, y, "V")),
    ]
    boards = [
        ("01–05", "机器人 · 侧面 / 背面", "ROBOTS · SIDE / BACK", robots, "CK_Turnaround_1_Robots.png"),
    ]
    items = [dict(index=int(i), name=n, en=e, kind=k, row=r, col=c, side=s, back=b) for i, n, e, k, r, c, s, b in robots]
    board("CK ROBOTICS · TURNAROUND 1", "机器人 · 侧面 / 背面 · 与正面参考对照", items, os.path.join(BASE, "CK_Turnaround_1_Robots.png"))

    chargers = [
        ("06", "标准充电桩", "STANDARD CHARGER", "CHARGING", 1, 0, lambda d, x, y: draw_side_charger(d, x, y, "standard"), lambda d, x, y: draw_back_charger(d, x, y, "standard")),
        ("07", "可拆卸充电桩", "DETACHABLE CHARGER", "CHARGING", 1, 1, lambda d, x, y: draw_side_charger(d, x, y, "detachable"), lambda d, x, y: draw_back_charger(d, x, y, "detachable")),
        ("08", "轻量现场充电桩", "LIGHT FIELD CHARGER", "CHARGING", 1, 2, lambda d, x, y: draw_side_charger(d, x, y, "field"), lambda d, x, y: draw_back_charger(d, x, y, "field")),
        ("09", "现场维修机", "FIELD REPAIR BAY", "REPAIR", 1, 3, lambda d, x, y: draw_side_repair(d, x, y, "field"), lambda d, x, y: draw_back_repair(d, x, y, "field")),
        ("10", "高速自动维修机", "HIGH-SPEED AUTO REPAIR", "REPAIR", 1, 4, lambda d, x, y: draw_side_repair(d, x, y, "auto"), lambda d, x, y: draw_back_repair(d, x, y, "auto")),
    ]
    items = [dict(index=int(i), name=n, en=e, kind=k, row=r, col=c, side=s, back=b) for i, n, e, k, r, c, s, b in chargers]
    board("CK ROBOTICS · TURNAROUND 2", "充电与维修 · 侧面 / 背面 · 与正面参考对照", items, os.path.join(BASE, "CK_Turnaround_2_ChargersRepair.png"))

    factories = [
        ("11", "工坊工厂", "WORKSHOP FACTORY", "FACTORY", 2, 0, lambda d, x, y: draw_side_factory(d, x, y, "workshop"), lambda d, x, y: draw_back_factory(d, x, y, "workshop")),
        ("12", "两工位工厂", "TWO-STATION FACTORY", "FACTORY", 2, 1, lambda d, x, y: draw_side_factory(d, x, y, "two-station"), lambda d, x, y: draw_back_factory(d, x, y, "two-station")),
        ("13", "骨架厂", "FRAME FAB", "FACTORY", 2, 2, lambda d, x, y: draw_side_factory(d, x, y, "frame"), lambda d, x, y: draw_back_factory(d, x, y, "frame")),
        ("14", "预制工厂", "PREFAB FACTORY", "FACTORY", 2, 3, lambda d, x, y: draw_side_factory(d, x, y, "prefab"), lambda d, x, y: draw_back_factory(d, x, y, "prefab")),
        ("15", "无人自动工厂", "UNMANNED AUTO FACTORY", "FACTORY", 2, 4, lambda d, x, y: draw_side_factory(d, x, y, "auto"), lambda d, x, y: draw_back_factory(d, x, y, "auto")),
    ]
    items = [dict(index=int(i), name=n, en=e, kind=k, row=r, col=c, side=s, back=b) for i, n, e, k, r, c, s, b in factories]
    board("CK ROBOTICS · TURNAROUND 3", "工厂 · 侧面 / 背面 · 与正面参考对照", items, os.path.join(BASE, "CK_Turnaround_3_Factories.png"))

    modules = [
        ("16", "电弧焊枪", "ARC WELDER", "DEFENSE MODULE", 3, 0, lambda d, x, y: draw_side_module(d, x, y, "welder"), lambda d, x, y: draw_back_module(d, x, y, "welder")),
        ("17", "电击棒", "SHOCK BATON", "DEFENSE MODULE", 3, 1, lambda d, x, y: draw_side_module(d, x, y, "baton"), lambda d, x, y: draw_back_module(d, x, y, "baton")),
        ("18", "轻甲", "LIGHT ARMOR", "DEFENSE MODULE", 3, 2, lambda d, x, y: draw_side_module(d, x, y, "armor"), lambda d, x, y: draw_back_module(d, x, y, "armor")),
        ("19", "电弧发射器", "ARC EMITTER", "DEFENSE MODULE", 3, 3, lambda d, x, y: draw_side_module(d, x, y, "emitter"), lambda d, x, y: draw_back_module(d, x, y, "emitter")),
    ]
    items = [dict(index=int(i), name=n, en=e, kind=k, row=r, col=c, side=s, back=b) for i, n, e, k, r, c, s, b in modules]
    board("CK ROBOTICS · TURNAROUND 4", "自卫模块 · 侧面 / 背面 · 与正面参考对照", items, os.path.join(BASE, "CK_Turnaround_4_Modules.png"))


if __name__ == "__main__":
    main()
