"""Generate a top-down concept board for all 19 robotics items."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BASE = os.path.join(ROOT, "docs", "robot-textures")
OUT = os.path.join(BASE, "CK_Robotics_TopView_Board.png")
os.makedirs(BASE, exist_ok=True)

W, H = 2400, 1780
MARGIN = 40
GAP = 34
CELL_W = (W - MARGIN * 2 - GAP * 4) // 5
CELL_H = 350
ROW_GAP = 46
TOP = 142

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

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


F_TITLE = font(42, True)
F_SUB = font(17)
F_LABEL = font(19, True)
F_SMALL = font(12)
F_TINY = font(10)


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
    "I": dict(shell=STEEL_LIGHT, shell2=STEEL_MID, accent=(61, 126, 166, 255), glow=CYAN, kind="round"),
    "II": dict(shell=(218, 228, 220, 255), shell2=(172, 194, 178, 255), accent=(83, 145, 104, 255), glow=GREEN, kind="module"),
    "III": dict(shell=(232, 184, 76, 255), shell2=(164, 129, 62, 255), accent=(52, 60, 68, 255), glow=AMBER_LIGHT, kind="heavy"),
    "IV": dict(shell=(226, 242, 242, 255), shell2=(168, 207, 211, 255), accent=(44, 139, 155, 255), glow=CYAN, kind="scout"),
    "V": dict(shell=(245, 238, 216, 255), shell2=(203, 185, 146, 255), accent=(198, 154, 58, 255), glow=AMBER_LIGHT, kind="royal"),
}


def top_robot(d, cx, cy, tier):
    p = TIERS[tier]
    ellipse(d, (cx - 95, cy + 75, cx + 95, cy + 95), (0, 0, 0, 50))
    # treads / wheels
    if tier == "IV":
        for side in (-1, 1):
            ellipse(d, (cx + side * 58 - 20, cy + 30, cx + side * 58 + 20, cy + 70), TREAD, OUTLINE, 2)
            ellipse(d, (cx + side * 58 - 7, cy + 43, cx + side * 58 + 7, cy + 57), STEEL_MID, OUTLINE, 1)
    else:
        for side in (-1, 1):
            tw = 30 if tier != "III" else 38
            tx = cx + side * 66
            rr(d, (tx - tw // 2, cy - 58, tx + tw // 2, cy + 66), 9, TREAD, OUTLINE, 2)
            for i in range(4):
                d.rectangle((tx - tw // 2 + 6, cy - 42 + i * 26, tx + tw // 2 - 6, cy - 36 + i * 26), fill=TREAD_LIGHT)
    # body top
    bw = 88 if tier not in ("III", "V") else 108
    bh = 104 if tier not in ("III", "V") else 116
    if tier == "IV":
        bw, bh = 76, 92
    rr(d, (cx - bw / 2, cy - bh / 2, cx + bw / 2, cy + bh / 2), 26, p["shell"], OUTLINE, 2)
    rr(d, (cx - bw / 2 + 8, cy - bh / 2 + 8, cx + bw / 2 - 8, cy + bh / 2 - 8), 20, p["shell2"], OUTLINE, 1)
    # shoulders / arms
    for side in (-1, 1):
        ax = cx + side * (bw / 2 + 4)
        rr(d, (ax - 12, cy - 34, ax + 12, cy + 38), 10, p["shell2"], OUTLINE, 2)
        ellipse(d, (ax - 15, cy + 30, ax + 15, cy + 56), p["shell"], OUTLINE, 1)
        bolt(d, ax, cy + 43, 2)
    # head dome from above
    hw = 40 if tier != "IV" else 32
    ellipse(d, (cx - hw, cy - 50, cx + hw, cy + 26), p["shell"], OUTLINE, 2)
    ellipse(d, (cx - hw + 7, cy - 42, cx + hw - 7, cy + 18), p["shell2"], OUTLINE, 1)
    if tier == "IV":
        ellipse(d, (cx - hw + 13, cy - 32, cx + hw - 13, cy + 8), (*CYAN[:3], 110), OUTLINE, 1)
    else:
        ellipse(d, (cx - 18, cy - 30, cx + 18, cy + 6), (20, 28, 37, 255), OUTLINE, 1)
        d.rectangle((cx - 12, cy - 14, cx + 12, cy - 8), fill=p["glow"])
    # neck ring / front marker
    ellipse(d, (cx - 16, cy + 2, cx + 16, cy + 26), DARK2, OUTLINE, 1)
    polygon(d, [(cx - 7, cy + 23), (cx + 7, cy + 23), (cx, cy + 34)], p["glow"], OUTLINE)
    # tier details
    if tier == "I":
        # broom along right side
        line(d, [(cx + 48, cy - 20), (cx + 84, cy + 62)], OUTLINE, 4)
        line(d, [(cx + 48, cy - 20), (cx + 84, cy + 62)], AMBER, 2)
        polygon(d, [(cx + 78, cy + 54), (cx + 96, cy + 50), (cx + 100, cy + 66), (cx + 82, cy + 72)], AMBER, OUTLINE)
    elif tier == "II":
        for side in (-1, 1):
            rr(d, (cx + side * 46 - 12, cy - 30, cx + side * 46 + 12, cy + 28), 5, p["accent"], OUTLINE, 1)
            d.rectangle((cx + side * 46 - 5, cy - 16, cx + side * 46 + 5, cy - 10), fill=GREEN)
    elif tier == "III":
        for side in (-1, 1):
            line(d, [(cx + side * 72, cy - 28), (cx + side * 96, cy - 8), (cx + side * 104, cy + 22)], OUTLINE, 9)
            line(d, [(cx + side * 72, cy - 28), (cx + side * 96, cy - 8), (cx + side * 104, cy + 22)], STEEL_LIGHT, 4)
            rr(d, (cx + side * 104 - 7, cy + 16, cx + side * 104 + 7, cy + 34), 3, AMBER, OUTLINE, 1)
    elif tier == "IV":
        # scout antennae
        for side in (-1, 1):
            line(d, [(cx + side * 18, cy - 26), (cx + side * 28, cy - 58)], OUTLINE, 2)
            ellipse(d, (cx + side * 28 - 5, cy - 64, cx + side * 28 + 5, cy - 54), CYAN_DIM, OUTLINE, 1)
        line(d, [(cx - 44, cy - 30), (cx - 62, cy - 14), (cx - 62, cy + 16)], OUTLINE, 3)
        ellipse(d, (cx - 69, cy + 16, cx - 55, cy + 30), CYAN_DIM, OUTLINE, 1)
    else:
        # crown ring and gold trim
        for i in range(8):
            a = math.radians(i * 45)
            d.rectangle((cx + math.cos(a) * 54 - 3, cy - 18 + math.sin(a) * 54 - 3,
                        cx + math.cos(a) * 54 + 3, cy - 18 + math.sin(a) * 54 + 3), fill=AMBER, outline=OUTLINE, width=1)
        rr(d, (cx - 42, cy + 40, cx + 42, cy + 50), 4, AMBER, OUTLINE, 1)
        line(d, [(cx - 56, cy + 10), (cx - 72, cy - 22)], OUTLINE, 3)
        line(d, [(cx - 56, cy + 10), (cx - 72, cy - 22)], AMBER, 2)
        ellipse(d, (cx - 80, cy - 30, cx - 64, cy - 14), AMBER_LIGHT, OUTLINE, 1)
    # tier badge
    rr(d, (cx - 14, cy + 72, cx + 14, cy + 86), 3, DARK2, OUTLINE, 1)
    d.text((cx - 5, cy + 73), tier, font=F_TINY, fill=p["glow"])


def top_charger(d, cx, cy, kind):
    ellipse(d, (cx - 80, cy + 70, cx + 80, cy + 94), (0, 0, 0, 45))
    if kind == "standard":
        ellipse(d, (cx - 88, cy - 48, cx + 88, cy + 72), DARK2, OUTLINE, 2)
        ellipse(d, (cx - 72, cy - 34, cx + 72, cy + 58), (*CYAN[:3], 50), OUTLINE, 1)
        ellipse(d, (cx - 38, cy - 38, cx + 38, cy + 38), STEEL, OUTLINE, 2)
        ellipse(d, (cx - 22, cy - 22, cx + 22, cy + 22), PANEL, OUTLINE, 1)
        ellipse(d, (cx - 10, cy - 10, cx + 10, cy + 10), CYAN, OUTLINE, 1)
        d.arc((cx + 22, cy + 10, cx + 92, cy + 72), 260, 100, fill=OUTLINE, width=4)
        d.arc((cx + 22, cy + 10, cx + 92, cy + 72), 260, 100, fill=AMBER, width=2)
        # cable plug
        rr(d, (cx + 84, cy + 58, cx + 100, cy + 72), 3, AMBER, OUTLINE, 1)
    elif kind == "detachable":
        rr(d, (cx - 62, cy - 56, cx + 62, cy + 65), 12, STEEL_MID, OUTLINE, 2)
        rr(d, (cx - 50, cy - 44, cx + 50, cy + 50), 8, PANEL, OUTLINE, 1)
        d.rectangle((cx - 36, cy - 24, cx + 36, cy - 8), fill=CYAN)
        d.rectangle((cx - 36, cy + 6, cx + 8, cy + 14), fill=GREEN)
        # handle
        d.arc((cx - 40, cy - 92, cx + 40, cy - 30), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 40, cy - 92, cx + 40, cy - 30), 180, 360, fill=AMBER, width=2)
        for side in (-1, 1):
            ellipse(d, (cx + side * 46 - 12, cy + 54, cx + side * 46 + 12, cy + 78), TREAD, OUTLINE, 2)
            ellipse(d, (cx + side * 46 - 4, cy + 62, cx + side * 46 + 4, cy + 70), STEEL_MID, OUTLINE, 1)
        line(d, [(cx - 62, cy + 30), (cx - 82, cy + 30), (cx - 82, cy - 4)], OUTLINE, 3)
    else:
        rr(d, (cx - 68, cy - 52, cx + 68, cy + 66), 10, (73, 91, 88, 255), OUTLINE, 2)
        # folded solar panel on top
        polygon(d, [(cx - 70, cy - 40), (cx - 2, cy - 92), (cx + 64, cy - 58), (cx - 4, cy - 8)], (31, 60, 77, 255), OUTLINE)
        for i in range(1, 4):
            line(d, [(cx - 64 + i * 20, cy - 36 - i * 8), (cx + 4 + i * 20, cy - 84 - i * 8)], CYAN_DIM, 2)
        rr(d, (cx - 52, cy + 14, cx + 52, cy + 52), 5, PANEL, OUTLINE, 1)
        d.rectangle((cx - 42, cy + 22, cx + 4, cy + 30), fill=CYAN)
        d.rectangle((cx - 42, cy + 38, cx - 12, cy + 43), fill=GREEN)
        d.arc((cx - 34, cy + 56, cx + 34, cy + 86), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 34, cy + 56, cx + 34, cy + 86), 180, 360, fill=AMBER, width=2)


def top_repair(d, cx, cy, kind):
    half = 92 if kind == "auto" else 82
    # 2x2 footprint
    rr(d, (cx - half, cy - half, cx + half, cy + half), 8, DARK, OUTLINE, 2)
    rr(d, (cx - half + 10, cy - half + 10, cx + half - 10, cy + half - 10), 5, PANEL, OUTLINE, 1)
    # bay opening south
    d.rectangle((cx - half + 28, cy + half - 18, cx + half - 28, cy + half), fill=BG, outline=None)
    # frame
    for side in (-1, 1):
        rr(d, (cx + side * (half - 16) - 8, cy - half + 8, cx + side * (half - 16) + 8, cy + half - 12), 3, STEEL, OUTLINE, 1)
    # robot from above
    ellipse(d, (cx - 30, cy - 30, cx + 30, cy + 25), STEEL_MID, OUTLINE, 1)
    ellipse(d, (cx - 19, cy - 19, cx + 19, cy + 12), (18, 27, 35, 255), OUTLINE, 1)
    d.rectangle((cx - 12, cy - 10, cx + 12, cy - 6), fill=CYAN)
    for side in (-1, 1):
        line(d, [(cx + side * 32, cy - 10), (cx + side * 5, cy + 22), (cx + side * 10, cy + 42)], OUTLINE, 4)
        line(d, [(cx + side * 32, cy - 10), (cx + side * 5, cy + 22), (cx + side * 10, cy + 42)], AMBER if kind == "auto" else STEEL_LIGHT, 2)
    hazard(d, int(cx - 44), int(cy + half - 8), int(cx + 44), int(cy + half), 5)
    d.text((cx - 18, cy + half - 26), "AUTO" if kind == "auto" else "FIELD", font=F_TINY,
           fill=CYAN if kind == "auto" else AMBER)


def top_factory(d, cx, cy, kind):
    # footprint cells
    if kind == "two-station":
        w, h = 280, 210
    elif kind == "auto":
        # 2 wide x 6 deep, drawn with a 40 px cell so it fits the cell height
        w, h = 80, 240
    elif kind == "frame":
        w, h = 140, 140
    else:
        w, h = 210, 210
    x1, y1, x2, y2 = cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2
    rr(d, (x1, y1, x2, y2), 10, DARK, OUTLINE, 2)
    # roof panels
    rr(d, (x1 + 10, y1 + 10, x2 - 10, y2 - 10), 6, STEEL_MID, OUTLINE, 1)
    rr(d, (x1 + 20, y1 + 20, x2 - 20, y2 - 20), 4, PANEL, OUTLINE, 1)
    # roof vents / skylights
    for i in range(3):
        d.rectangle((x1 + 28 + i * 30, y1 + 30, x1 + 44 + i * 30, y1 + 44), fill=CYAN_DIM, outline=OUTLINE, width=1)
    # south loading door
    d.rectangle((cx - 36, y2 - 8, cx + 36, y2 + 2), fill=AMBER, outline=OUTLINE, width=1)
    # hazard at door
    hazard(d, int(cx - 34), int(y2 - 18), int(cx + 34), int(y2 - 10), 4)
    if kind == "workshop":
        # chimney and gear
        ellipse(d, (x1 + 24, y1 + 24, x1 + 56, y1 + 56), DARK2, OUTLINE, 1)
        ellipse(d, (x1 + 34, y1 + 34, x1 + 46, y1 + 46), AMBER, OUTLINE, 1)
        d.rectangle((x2 - 44, y1 + 16, x2 - 22, y1 + 34), fill=STEEL, outline=OUTLINE, width=1)
    elif kind == "two-station":
        # two roof cells
        d.line((cx, y1 + 10, cx, y2 - 12), fill=LINE, width=3)
        for x in (cx - 80, cx + 20):
            d.rectangle((x, cy - 50, x + 60, cy + 26), fill=DARK2, outline=OUTLINE, width=1)
            d.rectangle((x + 10, cy - 38, x + 50, cy - 30), fill=AMBER if x < cx else CYAN)
            d.rectangle((x + 10, cy - 16, x + 40, cy - 10), fill=GREEN)
        d.text((cx - 34, y1 + 18), "2-STATION", font=F_TINY, fill=AMBER)
    elif kind == "frame":
        # open gantry
        rr(d, (x1 + 8, y1 + 8, x2 - 8, y2 - 8), 4, DARK2, OUTLINE, 1)
        line(d, [(cx - 40, cy - 36), (cx + 40, cy - 36), (cx + 40, cy + 40), (cx - 40, cy + 40), (cx - 40, cy - 36)], STEEL_LIGHT, 3)
        for x, y in ((cx - 40, cy - 36), (cx + 40, cy - 36), (cx - 40, cy + 40), (cx + 40, cy + 40)):
            ellipse(d, (x - 5, y - 5, x + 5, y + 5), AMBER, OUTLINE, 1)
        line(d, [(cx + 20, cy - 60), (cx + 42, cy - 30), (cx + 26, cy - 4)], OUTLINE, 4)
        line(d, [(cx + 20, cy - 60), (cx + 42, cy - 30), (cx + 26, cy - 4)], STEEL_LIGHT, 2)
    elif kind == "prefab":
        for i, (x, y, col) in enumerate(((x1 + 16, y1 + 16, STEEL), (x1 + 100, y1 + 16, AMBER), (x1 + 16, y1 + 100, CYAN_DIM), (x1 + 100, y1 + 100, STEEL_MID))):
            rr(d, (x, y, x + 70, y + 54), 4, col, OUTLINE, 1)
            d.text((x + 10, y + 24), f"M{i+1}", font=F_TINY, fill=TEXT if i != 2 else DARK2)
        line(d, [(x2 - 12, y2 - 20), (x2 - 12, y1 + 10), (x1 + 50, y1 + 10)], OUTLINE, 4)
    else:
        # 2x6 unmanned factory: narrow long hall, six roof modules, side service arms
        rr(d, (x1 + 10, y1 + 16, x2 - 10, y2 - 24), 6, DARK2, OUTLINE, 1)
        # 2 x 6 footprint grid
        for i in range(1, 6):
            d.line((x1 + 10, y1 + 16 + i * 40, x2 - 10, y1 + 16 + i * 40),
                   fill=(38, 58, 66, 160), width=1)
        d.line((cx, y1 + 16, cx, y2 - 24), fill=(38, 58, 66, 160), width=1)
        # six roof modules / skylights
        for i in range(6):
            yy = y1 + 26 + i * 38
            d.rectangle((cx - 26, yy, cx + 26, yy + 12), fill=(24, 83, 96, 255),
                        outline=OUTLINE, width=1)
            d.rectangle((cx - 20, yy + 3, cx + 20, yy + 5), fill=CYAN_DIM)
        # central core and short radial arms
        ellipse(d, (cx - 18, cy - 28, cx + 18, cy + 28), (25, 58, 70, 255), OUTLINE, 1)
        ellipse(d, (cx - 8, cy - 14, cx + 8, cy + 14), CYAN, None)
        for side in (-1, 1):
            line(d, [(cx + side * 34, cy + 32), (cx + side * 22, cy + 8), (cx + side * 10, cy - 8)], OUTLINE, 4)
            line(d, [(cx + side * 34, cy + 32), (cx + side * 22, cy + 8), (cx + side * 10, cy - 8)], STEEL_LIGHT, 2)
            ellipse(d, (cx + side * 10 - 4, cy - 12, cx + side * 10 + 4, cy - 4), AMBER, OUTLINE, 1)
        # south office / control box
        rr(d, (cx - 26, y2 - 54, cx + 26, y2 - 28), 4, PANEL, OUTLINE, 1)
        d.rectangle((cx - 18, y2 - 47, cx + 2, y2 - 41), fill=CYAN)
        d.rectangle((cx - 18, y2 - 37, cx - 2, y2 - 33), fill=GREEN)
        d.text((cx - 30, y2 - 22), "AUTO / 24H", font=F_TINY, fill=CYAN)


def top_module(d, cx, cy, kind):
    if kind == "welder":
        line(d, [(cx - 90, cy + 60), (cx + 30, cy - 34)], OUTLINE, 20)
        line(d, [(cx - 90, cy + 60), (cx + 30, cy - 34)], STEEL_MID, 12)
        rr(d, (cx - 110, cy + 38, cx - 72, cy + 78), 5, DARK, OUTLINE, 1)
        d.rectangle((cx - 102, cy + 48, cx - 80, cy + 56), fill=AMBER)
        polygon(d, [(cx + 18, cy - 46), (cx + 44, cy - 34), (cx + 40, cy + 2), (cx + 14, cy - 10)], STEEL_LIGHT, OUTLINE)
        line(d, [(cx + 38, cy - 4), (cx + 62, cy + 18), (cx + 44, cy + 36), (cx + 68, cy + 56)], CYAN, 3)
        ellipse(d, (cx + 24, cy + 8, cx + 62, cy + 44), (*CYAN[:3], 60))
    elif kind == "baton":
        line(d, [(cx - 100, cy + 65), (cx + 92, cy - 55)], OUTLINE, 22)
        line(d, [(cx - 100, cy + 65), (cx + 92, cy - 55)], DARK, 13)
        line(d, [(cx - 92, cy + 56), (cx - 34, cy + 10)], AMBER, 8)
        ellipse(d, (cx + 72, cy - 76, cx + 118, cy - 30), CYAN_DIM, OUTLINE, 2)
        ellipse(d, (cx + 82, cy - 66, cx + 108, cy - 40), CYAN, None)
        for i in range(3):
            d.arc((cx + 64 - i * 6, cy - 82 + i * 5, cx + 126 + i * 4, cy - 20 - i * 5), 200, 340, fill=CYAN, width=2)
    elif kind == "armor":
        polygon(d, [(cx - 62, cy - 78), (cx + 62, cy - 78), (cx + 46, cy + 36), (cx, cy + 72), (cx - 46, cy + 36)], STEEL, OUTLINE)
        polygon(d, [(cx - 46, cy - 64), (cx + 46, cy - 64), (cx + 32, cy + 24), (cx, cy + 54), (cx - 32, cy + 24)], STEEL_MID, OUTLINE)
        polygon(d, [(cx, cy - 26), (cx + 16, cy - 8), (cx, cy + 12), (cx - 16, cy - 8)], CYAN_DIM, OUTLINE)
        for side in (-1, 1):
            line(d, [(cx + side * 44, cy - 54), (cx + side * 64, cy + 10), (cx + side * 44, cy + 44)], OUTLINE, 7)
            line(d, [(cx + side * 44, cy - 54), (cx + side * 64, cy + 10), (cx + side * 44, cy + 44)], DARK, 3)
    else:
        # top-down arc rifle
        polygon(d, [(cx - 130, cy + 10), (cx - 92, cy - 6), (cx - 80, cy + 34), (cx - 120, cy + 50)], DARK, OUTLINE)
        rr(d, (cx - 92, cy - 24, cx + 38, cy + 28), 5, STEEL, OUTLINE, 2)
        rr(d, (cx - 78, cy - 16, cx + 26, cy + 16), 3, DARK, OUTLINE, 1)
        polygon(d, [(cx - 34, cy + 22), (cx - 16, cy + 22), (cx - 6, cy + 58), (cx - 26, cy + 60)], DARK, OUTLINE)
        polygon(d, [(cx - 6, cy + 20), (cx + 18, cy + 18), (cx + 12, cy + 52), (cx - 8, cy + 54)], DARK2, OUTLINE)
        d.rectangle((cx + 2, cy + 28, cx + 12, cy + 34), fill=CYAN)
        rr(d, (cx + 30, cy - 20, cx + 116, cy + 12), 4, STEEL_MID, OUTLINE, 2)
        for i in range(4):
            x = cx + 42 + i * 17
            ellipse(d, (x, cy - 26, x + 12, cy + 18), None, CYAN, 2)
        line(d, [(cx + 116, cy - 4), (cx + 158, cy - 4)], OUTLINE, 8)
        line(d, [(cx + 116, cy - 4), (cx + 158, cy - 4)], STEEL_LIGHT, 4)
        ellipse(d, (cx + 154, cy - 18, cx + 182, cy + 10), (*CYAN[:3], 80))


def panel(d, index, row, col, title, subtitle):
    x = MARGIN + col * (CELL_W + GAP)
    y = TOP + row * (CELL_H + ROW_GAP)
    rr(d, (x, y, x + CELL_W, y + CELL_H), 14, PANEL, LINE, 2)
    d.text((x + 12, y + 9), f"{index:02d}", font=F_TINY, fill=AMBER)
    d.text((x + 18, y + CELL_H - 46), title, font=F_LABEL, fill=TEXT)
    d.text((x + 18, y + CELL_H - 23), subtitle, font=F_SMALL, fill=MUTED)
    return x, y


def main():
    image = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(image)
    for x in range(0, W, 40):
        d.line((x, 0, x, H), fill=(27, 40, 47, 70), width=1)
    for y in range(0, H, 40):
        d.line((0, y, W, y), fill=(27, 40, 47, 70), width=1)
    d.text((MARGIN, 24), "CK ROBOTICS · TOP-DOWN VIEW BOARD", font=F_TITLE, fill=TEXT)
    d.text((MARGIN + 3, 77), "19 项设施与模块 · 俯视设定图 · 南向为正面（N ↑）", font=F_SUB, fill=MUTED)
    d.text((W - MARGIN - 180, 80), "TOP VIEW  ·  SOUTH ↓", font=F_SMALL, fill=AMBER)

    rows = [
        ("机器人 / ROBOTS", [
            (1, "Mk-I 标准保姆机", "1×1 · STANDARD", lambda d, x, y: top_robot(d, x, y, "I")),
            (2, "Mk-II 模块化保姆机", "1×1 · MODULAR", lambda d, x, y: top_robot(d, x, y, "II")),
            (3, "Mk-III 重载保姆机", "1×1 · HEAVY", lambda d, x, y: top_robot(d, x, y, "III")),
            (4, "Mk-IV 前哨保姆机", "1×1 · OUTPOST", lambda d, x, y: top_robot(d, x, y, "IV")),
            (5, "Mk-V 自维护保姆机", "1×1 · FLAGSHIP", lambda d, x, y: top_robot(d, x, y, "V")),
        ]),
        ("充电与维修 / CHARGING & REPAIR", [
            (6, "标准充电桩", "1×1 · STANDARD", lambda d, x, y: top_charger(d, x, y, "standard")),
            (7, "可拆卸充电桩", "1×1 · PORTABLE", lambda d, x, y: top_charger(d, x, y, "detachable")),
            (8, "轻量现场充电桩", "1×1 · FIELD", lambda d, x, y: top_charger(d, x, y, "field")),
            (9, "现场维修机", "2×2 · FIELD", lambda d, x, y: top_repair(d, x, y, "field")),
            (10, "高速自动维修机", "2×2 · AUTO", lambda d, x, y: top_repair(d, x, y, "auto")),
        ]),
        ("工厂 / FACTORIES", [
            (11, "工坊工厂", "3×3 · WORKSHOP", lambda d, x, y: top_factory(d, x, y, "workshop")),
            (12, "两工位工厂", "4×3 · TWO-STATION", lambda d, x, y: top_factory(d, x, y, "two-station")),
            (13, "骨架厂", "2×2 · FRAME", lambda d, x, y: top_factory(d, x, y, "frame")),
            (14, "预制工厂", "3×3 · PREFAB", lambda d, x, y: top_factory(d, x, y, "prefab")),
            (15, "无人自动工厂", "2×6 · UNMANNED", lambda d, x, y: top_factory(d, x, y, "auto")),
        ]),
        ("自卫模块 / DEFENSE MODULES", [
            (16, "电弧焊枪", "MODULE · WELDER", lambda d, x, y: top_module(d, x, y, "welder")),
            (17, "电击棒", "MODULE · BATON", lambda d, x, y: top_module(d, x, y, "baton")),
            (18, "轻甲", "MODULE · ARMOR", lambda d, x, y: top_module(d, x, y, "armor")),
            (19, "电弧发射器", "MODULE · EMITTER", lambda d, x, y: top_module(d, x, y, "emitter")),
        ]),
    ]
    for row, (group, items) in enumerate(rows):
        d.text((MARGIN, TOP + row * (CELL_H + ROW_GAP) - 28), group, font=F_SMALL, fill=AMBER)
        for col, (index, title, subtitle, draw) in enumerate(items):
            x, y = panel(d, index, row, col, title, subtitle)
            draw(d, x + CELL_W // 2, y + 138)

    d.text((MARGIN, H - 44), "Top-down concept board · south is the front · north is up · side/back details are in the turnaround boards.", font=F_TINY, fill=MUTED)
    image.convert("RGB").save(OUT, quality=96)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
