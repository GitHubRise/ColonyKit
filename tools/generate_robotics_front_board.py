"""Draw a front-view concept board for robots, chargers, repair bays, factories and modules."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "CK_Robotics_FrontView_Board.png")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

W, H = 2400, 1720
MARGIN = 40
GAP = 55
CELL_W = (W - MARGIN * 2 - GAP * 4) // 5
CELL_H = 330
TOP = 150

BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
PANEL2 = (24, 34, 42, 255)
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


F_TITLE = font(44, True)
F_SUB = font(18)
F_GROUP = font(20, True)
F_LABEL = font(20, True)
F_SUB_LABEL = font(13)
F_TINY = font(12)


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(d, points, fill, outline=None):
    d.polygon(points, fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line(points, fill=fill, width=width, joint="curve")


def hazard(d, x1, y1, x2, y2, n=4):
    d.rectangle((x1, y1, x2, y2), fill=DARK2, outline=OUTLINE, width=1)
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


def bolt(d, x, y, r=3):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


def panel(d, index, row, col, title, subtitle):
    x = MARGIN + col * (CELL_W + GAP)
    y = TOP + row * (CELL_H + GAP)
    rr(d, (x, y, x + CELL_W, y + CELL_H), 15, PANEL, LINE, 2)
    d.text((x + 14, y + 10), f"{index:02d}", font=F_TINY, fill=AMBER)
    d.text((x + 18, y + CELL_H - 43), title, font=F_LABEL, fill=TEXT)
    d.text((x + 18, y + CELL_H - 20), subtitle, font=F_SUB_LABEL, fill=MUTED)


def draw_robot(d, cx, cy, tier):
    """Front elevation, roughly 220 px tall."""
    # shadow
    ellipse(d, (cx - 88, cy + 88, cx + 88, cy + 104), (0, 0, 0, 55))
    if tier == "I":
        shell, shell2, accent, glow = STEEL_LIGHT, STEEL_MID, (61, 126, 166, 255), CYAN
        body_w, body_h, head = 76, 90, "dome"
    elif tier == "II":
        shell, shell2, accent, glow = (218, 228, 220, 255), (172, 194, 178, 255), (83, 145, 104, 255), GREEN
        body_w, body_h, head = 84, 92, "rounded"
    elif tier == "III":
        shell, shell2, accent, glow = (232, 184, 76, 255), (164, 129, 62, 255), (52, 60, 68, 255), AMBER_LIGHT
        body_w, body_h, head = 100, 96, "box"
    elif tier == "IV":
        shell, shell2, accent, glow = (226, 242, 242, 255), (168, 207, 211, 255), (44, 139, 155, 255), CYAN
        body_w, body_h, head = 72, 86, "bubble"
    else:
        shell, shell2, accent, glow = (245, 238, 216, 255), (203, 185, 146, 255), (198, 154, 58, 255), AMBER_LIGHT
        body_w, body_h, head = 94, 100, "dome"

    # treads / wheels
    if tier == "IV":
        for side in (-1, 1):
            wx = cx + side * 42
            ellipse(d, (wx - 23, cy + 78, wx + 23, cy + 112), TREAD, OUTLINE, 2)
            ellipse(d, (wx - 11, cy + 90, wx + 11, cy + 104), STEEL_MID, OUTLINE, 1)
    else:
        tread_w = 34 if tier != "III" else 40
        for side in (-1, 1):
            tx = cx + side * (body_w / 2 + 8)
            rr(d, (tx - tread_w / 2, cy + 72, tx + tread_w / 2, cy + 108), 9, TREAD, OUTLINE, 2)
            for i in range(3):
                d.rectangle((tx - tread_w / 2 + 6 + i * 9, cy + 80, tx - tread_w / 2 + 11 + i * 9, cy + 100), fill=TREAD_LIGHT)
            d.rectangle((tx - 5, cy + 84, tx + 5, cy + 88), fill=AMBER, outline=OUTLINE, width=1)

    # body
    bx1, bx2 = cx - body_w / 2, cx + body_w / 2
    by1, by2 = cy - 12, cy + 84
    rr(d, (bx1, by1, bx2, by2), 16, shell, OUTLINE, 2)
    rr(d, (bx1 + 8, by1 + 8, bx2 - 8, by2 - 8), 12, shell2, OUTLINE, 1)

    # arms
    arm_y = cy + 8
    for side in (-1, 1):
        ax = cx + side * (body_w / 2 + 8)
        rr(d, (ax - 10, arm_y, ax + 10, arm_y + 40), 6, shell2, OUTLINE, 2)
        # tool differences
        if tier == "I" and side == 1:
            line(d, [(ax + 8, arm_y + 30), (ax + 24, arm_y + 72)], OUTLINE, 3)
            polygon(d, [(ax + 18, arm_y + 66), (ax + 30, arm_y + 70), (ax + 26, arm_y + 82), (ax + 14, arm_y + 78)], AMBER, OUTLINE)
        elif tier == "II":
            rr(d, (ax - 12, arm_y + 30, ax + 12, arm_y + 50), 5, accent, OUTLINE, 1)
        elif tier == "III":
            rr(d, (ax - 13, arm_y + 26, ax + 13, arm_y + 48), 4, DARK, OUTLINE, 1)
            d.rectangle((ax - 8, arm_y + 32, ax + 8, arm_y + 35), fill=AMBER)
        elif tier == "IV":
            line(d, [(ax, arm_y + 26), (ax, arm_y + 52)], OUTLINE, 3)
            ellipse(d, (ax - 5, arm_y + 48, ax + 5, arm_y + 58), CYAN_DIM, OUTLINE, 1)
        else:
            rr(d, (ax - 11, arm_y + 22, ax + 11, arm_y + 42), 4, DARK, OUTLINE, 1)
            line(d, [(ax, arm_y + 40), (ax, arm_y + 68)], OUTLINE, 3)
            line(d, [(ax, arm_y + 62), (ax + 8, arm_y + 72)], CYAN, 2)

    # chest panel
    chest = (cx - body_w / 2 + 16, cy + 10, cx + body_w / 2 - 16, cy + 58)
    rr(d, chest, 8, accent, OUTLINE, 2)
    if tier == "I":
        for i, col in enumerate((GREEN, AMBER, ORANGE)):
            ellipse(d, (chest[0] + 12 + i * 12, cy + 32, chest[0] + 18 + i * 12, cy + 38), col, OUTLINE, 1)
    elif tier == "II":
        for y in (cy + 22, cy + 32, cy + 42):
            d.rectangle((chest[0] + 12, y, chest[2] - 12, y + 6), fill=GLOW_GREEN if False else shell, outline=OUTLINE, width=1)
    elif tier == "III":
        hazard(d, int(chest[0] + 12), int(cy + 24), int(chest[2] - 12), int(cy + 44), 4)
    elif tier == "IV":
        for y in (cy + 25, cy + 35, cy + 45):
            d.arc((chest[0] + 12, y - 6, chest[2] - 12, y + 10), 200, 340, fill=CYAN, width=2)
    else:
        polygon(d, [(cx, cy + 20), (cx + 14, cy + 34), (cx, cy + 48), (cx - 14, cy + 34)], AMBER, OUTLINE)
        polygon(d, [(cx, cy + 24), (cx + 8, cy + 34), (cx, cy + 42), (cx - 8, cy + 34)], STEEL_LIGHT, OUTLINE)

    # head
    hy = cy - 62
    if head == "dome":
        ellipse(d, (cx - 38, hy, cx + 38, hy + 54), shell, OUTLINE, 2)
        ellipse(d, (cx - 30, hy + 7, cx + 30, hy + 45), shell2, OUTLINE, 1)
        ellipse(d, (cx - 26, hy + 14, cx + 26, hy + 36), (20, 28, 37, 255), OUTLINE, 1)
        ellipse(d, (cx - 7, hy + 20, cx + 7, hy + 33), glow, OUTLINE, 1)
        ellipse(d, (cx - 3, hy + 23, cx + 3, hy + 29), (235, 252, 255, 255), None)
    elif head == "rounded":
        rr(d, (cx - 34, hy + 2, cx + 34, hy + 50), 13, shell, OUTLINE, 2)
        rr(d, (cx - 27, hy + 9, cx + 27, hy + 42), 9, shell2, OUTLINE, 1)
        rr(d, (cx - 23, hy + 16, cx + 23, hy + 34), 7, (20, 28, 37, 255), OUTLINE, 1)
        d.rectangle((cx - 15, hy + 22, cx + 15, hy + 26), fill=glow)
    elif head == "box":
        rr(d, (cx - 38, hy + 2, cx + 38, hy + 50), 8, shell, OUTLINE, 2)
        rr(d, (cx - 30, hy + 9, cx + 30, hy + 42), 5, shell2, OUTLINE, 1)
        rr(d, (cx - 26, hy + 15, cx + 26, hy + 33), 4, (19, 27, 36, 255), OUTLINE, 1)
        d.rectangle((cx - 17, hy + 22, cx + 17, hy + 26), fill=glow)
    elif head == "bubble":
        ellipse(d, (cx - 34, hy, cx + 34, hy + 52), shell, OUTLINE, 2)
        ellipse(d, (cx - 27, hy + 7, cx + 27, hy + 45), shell2, OUTLINE, 1)
        ellipse(d, (cx - 22, hy + 13, cx + 22, hy + 38), (*CYAN[:3], 110), None)
        ellipse(d, (cx - 15, hy + 19, cx - 4, hy + 31), glow)
        ellipse(d, (cx + 4, hy + 19, cx + 15, hy + 31), glow)
    else:
        ellipse(d, (cx - 38, hy, cx + 38, hy + 54), shell, OUTLINE, 2)
        ellipse(d, (cx - 30, hy + 7, cx + 30, hy + 46), shell2, OUTLINE, 1)
        ellipse(d, (cx - 25, hy + 14, cx + 25, hy + 37), (20, 28, 37, 255), OUTLINE, 1)
        ellipse(d, (cx - 13, hy + 20, cx + 13, hy + 33), AMBER_LIGHT, OUTLINE, 1)
        ellipse(d, (cx - 4, hy + 23, cx + 4, hy + 30), (25, 33, 42, 255), None)

    # antenna
    line(d, [(cx, hy + 4), (cx, hy - 14)], OUTLINE, 3)
    if tier == "V":
        polygon(d, [(cx - 20, hy - 6), (cx - 16, hy - 20), (cx - 8, hy - 12), (cx, hy - 24), (cx + 8, hy - 12), (cx + 16, hy - 20), (cx + 20, hy - 6)], AMBER, OUTLINE)
        d.rectangle((cx - 20, hy - 7, cx + 20, hy - 3), fill=AMBER, outline=OUTLINE, width=1)
    else:
        ellipse(d, (cx - 8, hy - 20, cx + 8, hy - 4), AMBER if tier != "IV" else CYAN, OUTLINE, 1)
        ellipse(d, (cx - 3, hy - 15, cx + 3, hy - 9), AMBER_LIGHT if tier != "IV" else (230, 255, 255, 255), None)
    if tier == "IV":
        line(d, [(cx + 22, hy + 2), (cx + 30, hy - 14)], OUTLINE, 2)
        ellipse(d, (cx + 26, hy - 20, cx + 36, hy - 10), CYAN_DIM, OUTLINE, 1)

    # tier badge
    badge = {"I": "I", "II": "II", "III": "III", "IV": "IV", "V": "V"}[tier]
    rr(d, (cx - 18, cy + 66, cx + 18, cy + 82), 4, DARK2, OUTLINE, 1)
    d.text((cx - 7, cy + 67), badge, font=F_SUB_LABEL, fill=AMBER_LIGHT if tier != "IV" else CYAN)


def draw_charger(d, cx, cy, kind):
    ellipse(d, (cx - 100, cy + 94, cx + 100, cy + 112), (0, 0, 0, 45))
    if kind == "standard":
        # floor ring, pedestal, cable
        ellipse(d, (cx - 88, cy + 74, cx + 88, cy + 104), DARK2, OUTLINE, 2)
        ellipse(d, (cx - 72, cy + 80, cx + 72, cy + 98), (*CYAN[:3], 55), OUTLINE, 1)
        rr(d, (cx - 22, cy - 36, cx + 22, cy + 76), 14, STEEL, OUTLINE, 2)
        rr(d, (cx - 14, cy - 22, cx + 14, cy + 20), 8, PANEL, OUTLINE, 1)
        rr(d, (cx - 8, cy - 14, cx + 8, cy + 12), 4, CYAN, OUTLINE, 1)
        # cable loop
        d.arc((cx + 18, cy + 8, cx + 90, cy + 76), 270, 90, fill=OUTLINE, width=3)
        d.arc((cx + 18, cy + 8, cx + 90, cy + 76), 270, 90, fill=AMBER, width=1)
        # top handle
        rr(d, (cx - 18, cy - 48, cx + 18, cy - 40), 4, DARK, OUTLINE, 1)
    elif kind == "detachable":
        # wheeled portable unit with handle
        rr(d, (cx - 56, cy - 18, cx + 56, cy + 78), 16, STEEL_MID, OUTLINE, 2)
        rr(d, (cx - 46, cy - 8, cx + 46, cy + 58), 10, PANEL, OUTLINE, 1)
        rr(d, (cx - 34, cy + 4, cx + 34, cy + 34), 6, (18, 27, 35, 255), OUTLINE, 1)
        d.rectangle((cx - 24, cy + 11, cx + 24, cy + 17), fill=CYAN)
        d.rectangle((cx - 22, cy + 23, cx + 10, cy + 27), fill=GREEN)
        # handle
        d.arc((cx - 38, cy - 54, cx + 38, cy + 2), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 38, cy - 54, cx + 38, cy + 2), 180, 360, fill=AMBER, width=2)
        # wheels
        for side in (-1, 1):
            ellipse(d, (cx + side * 42 - 12, cy + 70, cx + side * 42 + 12, cy + 94), TREAD, OUTLINE, 2)
            ellipse(d, (cx + side * 42 - 4, cy + 78, cx + side * 42 + 4, cy + 86), STEEL_MID, OUTLINE, 1)
        # cable plug
        line(d, [(cx + 50, cy + 18), (cx + 78, cy + 18), (cx + 78, cy + 46)], OUTLINE, 3)
        rr(d, (cx + 72, cy + 46, cx + 84, cy + 58), 3, AMBER, OUTLINE, 1)
        # latch
        d.rectangle((cx - 8, cy + 58, cx + 8, cy + 64), fill=AMBER, outline=OUTLINE, width=1)
    else:
        # rugged light field charger, solar panel
        rr(d, (cx - 60, cy - 2, cx + 60, cy + 78), 12, (73, 91, 88, 255), OUTLINE, 2)
        rr(d, (cx - 50, cy + 8, cx + 50, cy + 64), 8, PANEL, OUTLINE, 1)
        # solar panel tilted
        polygon(d, [(cx - 52, cy - 18), (cx + 10, cy - 52), (cx + 56, cy - 34), (cx - 8, cy + 2)], (31, 60, 77, 255), OUTLINE)
        for i in range(1, 4):
            line(d, [(cx - 52 + i * 18, cy - 12 - i * 7), (cx + 4 + i * 18, cy - 44 - i * 7)], CYAN_DIM, 2)
        line(d, [(cx + 26, cy - 34), (cx + 36, cy + 8)], OUTLINE, 3)
        # screen / battery
        d.rectangle((cx - 40, cy + 18, cx + 2, cy + 28), fill=CYAN)
        d.rectangle((cx - 40, cy + 34, cx - 12, cy + 40), fill=GREEN)
        # handle
        d.arc((cx - 34, cy + 62, cx + 34, cy + 94), 180, 360, fill=OUTLINE, width=5)
        d.arc((cx - 34, cy + 62, cx + 34, cy + 94), 180, 360, fill=AMBER, width=2)
        # small feet
        for side in (-1, 1):
            d.rectangle((cx + side * 44 - 8, cy + 82, cx + side * 44 + 8, cy + 88), fill=TREAD, outline=OUTLINE, width=1)


def draw_repair(d, cx, cy, kind):
    ellipse(d, (cx - 120, cy + 96, cx + 120, cy + 112), (0, 0, 0, 45))
    wide = 112 if kind == "auto" else 96
    # bay base / ramp
    polygon(d, [(cx - wide, cy + 76), (cx + wide, cy + 76), (cx + wide - 16, cy + 98), (cx - wide + 16, cy + 98)], DARK2, OUTLINE)
    rr(d, (cx - wide + 8, cy + 62, cx + wide - 8, cy + 82), 8, PANEL, OUTLINE, 1)
    # frame
    rr(d, (cx - wide, cy - 66, cx - wide + 16, cy + 78), 4, STEEL, OUTLINE, 2)
    rr(d, (cx + wide - 16, cy - 66, cx + wide, cy + 78), 4, STEEL, OUTLINE, 2)
    rr(d, (cx - wide, cy - 74, cx + wide, cy - 54), 6, STEEL_MID, OUTLINE, 2)
    # top lights
    for x in (cx - wide + 24, cx + wide - 24):
        ellipse(d, (x - 5, cy - 66, x + 5, cy - 56), CYAN if kind == "auto" else AMBER, OUTLINE, 1)
    # robot silhouette in bay
    rr(d, (cx - 28, cy - 28, cx + 28, cy + 58), 10, DARK, OUTLINE, 1)
    ellipse(d, (cx - 22, cy - 48, cx + 22, cy - 8), STEEL_MID, OUTLINE, 1)
    ellipse(d, (cx - 14, cy - 40, cx + 14, cy - 18), (18, 27, 35, 255), OUTLINE, 1)
    d.rectangle((cx - 10, cy - 33, cx + 10, cy - 29), fill=CYAN)
    # arms
    if kind == "auto":
        for side in (-1, 1):
            line(d, [(cx + side * (wide - 10), cy - 34), (cx + side * 26, cy - 10), (cx + side * 18, cy + 20)], OUTLINE, 5)
            line(d, [(cx + side * (wide - 10), cy - 34), (cx + side * 26, cy - 10), (cx + side * 18, cy + 20)], STEEL_LIGHT, 2)
            rr(d, (cx + side * 24 - 8, cy + 18, cx + side * 24 + 8, cy + 32), 3, AMBER, OUTLINE, 1)
        # twin screens
        rr(d, (cx - 42, cy + 62, cx - 6, cy + 86), 4, DARK2, OUTLINE, 1)
        rr(d, (cx + 6, cy + 62, cx + 42, cy + 86), 4, DARK2, OUTLINE, 1)
        d.rectangle((cx - 36, cy + 68, cx - 12, cy + 72), fill=CYAN)
        d.rectangle((cx + 12, cy + 68, cx + 36, cy + 72), fill=GREEN)
        d.text((cx - 44, cy + 90), "AUTO", font=F_TINY, fill=CYAN)
    else:
        # manual console and toolbox
        line(d, [(cx + wide - 18, cy - 20), (cx + 46, cy + 18), (cx + 34, cy + 52)], OUTLINE, 5)
        line(d, [(cx + wide - 18, cy - 20), (cx + 46, cy + 18), (cx + 34, cy + 52)], STEEL_LIGHT, 2)
        rr(d, (cx - wide + 10, cy + 54, cx - wide + 60, cy + 88), 5, DARK, OUTLINE, 1)
        d.rectangle((cx - wide + 17, cy + 60, cx - wide + 53, cy + 68), fill=AMBER)
        d.rectangle((cx - wide + 17, cy + 73, cx - wide + 40, cy + 77), fill=GREEN)
        # field crate
        rr(d, (cx + wide - 58, cy + 52, cx + wide - 8, cy + 86), 5, (73, 91, 88, 255), OUTLINE, 1)
        d.rectangle((cx + wide - 50, cy + 58, cx + wide - 16, cy + 62), fill=AMBER)
        d.text((cx - wide + 10, cy + 94), "FIELD", font=F_TINY, fill=AMBER)
    # hazard at base
    hazard(d, int(cx - 40), int(cy + 84), int(cx + 40), int(cy + 92), 5)


def draw_factory(d, cx, cy, kind):
    ellipse(d, (cx - 150, cy + 96, cx + 150, cy + 112), (0, 0, 0, 45))
    if kind == "workshop":
        # single 3x3 workshop with chimney
        rr(d, (cx - 105, cy - 48, cx + 105, cy + 86), 10, DARK, OUTLINE, 2)
        polygon(d, [(cx - 115, cy - 48), (cx + 115, cy - 48), (cx + 88, cy - 82), (cx - 88, cy - 82)], STEEL, OUTLINE)
        d.rectangle((cx - 78, cy - 8, cx + 78, cy + 52), fill=PANEL, outline=OUTLINE, width=1)
        # roll-up door
        for y in range(4, 52, 9):
            d.rectangle((cx - 60, cy + y, cx + 60, cy + y + 3), fill=STEEL_MID)
        # gear sign
        ellipse(d, (cx - 18, cy - 38, cx + 18, cy - 2), AMBER, OUTLINE, 1)
        ellipse(d, (cx - 6, cy - 26, cx + 6, cy - 14), DARK2, OUTLINE, 1)
        for angle in range(0, 360, 45):
            a = math.radians(angle)
            line(d, [(cx + math.cos(a) * 18, cy - 20 + math.sin(a) * 18), (cx + math.cos(a) * 24, cy - 20 + math.sin(a) * 24)], AMBER, 3)
        # chimney
        rr(d, (cx + 58, cy - 112, cx + 82, cy - 60), 4, STEEL_MID, OUTLINE, 1)
        d.rectangle((cx + 56, cy - 116, cx + 84, cy - 106), fill=AMBER, outline=OUTLINE, width=1)
    elif kind == "two-station":
        # two connected cells
        for x, col in ((cx - 100, AMBER), (cx + 4, CYAN)):
            rr(d, (x, cy - 62, x + 96, cy + 82), 8, DARK, OUTLINE, 2)
            polygon(d, [(x - 6, cy - 62), (x + 102, cy - 62), (x + 86, cy - 88), (x + 10, cy - 88)], STEEL, OUTLINE)
            d.rectangle((x + 14, cy - 22, x + 82, cy + 46), fill=PANEL, outline=OUTLINE, width=1)
            d.rectangle((x + 24, cy - 10, x + 72, cy + 4), fill=col, outline=OUTLINE, width=1)
            d.rectangle((x + 24, cy + 12, x + 60, cy + 18), fill=GREEN)
            d.rectangle((x + 24, cy + 28, x + 48, cy + 34), fill=STEEL_MID)
        # connector / conveyor
        rr(d, (cx - 10, cy + 10, cx + 10, cy + 58), 4, STEEL_MID, OUTLINE, 1)
        for y in range(16, 54, 8):
            d.rectangle((cx - 7, cy + y, cx + 7, cy + y + 3), fill=AMBER)
        # shared roof sign
        d.text((cx - 34, cy - 104), "2-STATION", font=F_TINY, fill=AMBER)
    elif kind == "frame":
        # gantry frame fab
        rr(d, (cx - 100, cy - 52, cx + 100, cy + 86), 8, DARK, OUTLINE, 2)
        rr(d, (cx - 110, cy - 58, cx + 110, cy - 44), 4, STEEL, OUTLINE, 2)
        for x in (cx - 88, cx + 88):
            rr(d, (x - 8, cy - 60, x + 8, cy + 78), 3, STEEL_MID, OUTLINE, 1)
        # frame skeleton hanging
        line(d, [(cx - 40, cy - 34), (cx + 40, cy - 34), (cx + 40, cy + 18), (cx - 40, cy + 18), (cx - 40, cy - 34)], (170, 183, 194, 255), 5)
        line(d, [(cx - 40, cy - 34), (cx + 40, cy - 34), (cx + 40, cy + 18), (cx - 40, cy + 18), (cx - 40, cy - 34)], OUTLINE, 1)
        # joints
        for x, y in ((cx - 40, cy - 34), (cx + 40, cy - 34), (cx - 40, cy + 18), (cx + 40, cy + 18)):
            ellipse(d, (x - 5, y - 5, x + 5, y + 5), AMBER, OUTLINE, 1)
        # welder arm
        line(d, [(cx + 70, cy - 50), (cx + 40, cy - 14), (cx + 28, cy + 4)], OUTLINE, 4)
        line(d, [(cx + 70, cy - 50), (cx + 40, cy - 14), (cx + 28, cy + 4)], STEEL_LIGHT, 2)
        line(d, [(cx + 28, cy + 4), (cx + 20, cy + 12)], CYAN, 2)
        d.rectangle((cx - 70, cy + 52, cx + 70, cy + 82), fill=PANEL, outline=OUTLINE, width=1)
        for x in (cx - 56, cx - 36, cx - 16, cx + 4, cx + 24, cx + 44):
            d.rectangle((x, cy + 61, x + 10, cy + 66), fill=STEEL_MID)
    elif kind == "prefab":
        # stacked modular containers and crane
        for i, (x, y, col) in enumerate(((cx - 90, cy - 50, STEEL), (cx - 20, cy - 50, AMBER), (cx - 90, cy + 8, CYAN_DIM), (cx - 20, cy + 8, STEEL_MID))):
            rr(d, (x, y, x + 68, y + 48), 4, col, OUTLINE, 2)
            d.rectangle((x + 8, y + 10, x + 60, y + 15), fill=DARK2, outline=OUTLINE, width=1)
            d.text((x + 10, y + 24), f"M{i+1}", font=F_TINY, fill=TEXT if i != 2 else DARK2)
        # crane
        line(d, [(cx + 74, cy + 70), (cx + 74, cy - 62), (cx + 12, cy - 62)], OUTLINE, 6)
        line(d, [(cx + 74, cy + 70), (cx + 74, cy - 62), (cx + 12, cy - 62)], AMBER, 3)
        line(d, [(cx + 12, cy - 62), (cx + 12, cy - 16)], OUTLINE, 2)
        polygon(d, [(cx + 8, cy - 18), (cx + 16, cy - 18), (cx + 12, cy - 8)], AMBER, OUTLINE)
        # pallet
        rr(d, (cx + 52, cy + 38, cx + 98, cy + 82), 3, (120, 92, 65, 255), OUTLINE, 1)
    else:
        # unmanned automatic factory: wide dark hall
        rr(d, (cx - 120, cy - 58, cx + 120, cy + 86), 10, DARK2, OUTLINE, 2)
        polygon(d, [(cx - 130, cy - 58), (cx + 130, cy - 58), (cx + 108, cy - 90), (cx - 108, cy - 90)], STEEL, OUTLINE)
        # dark glass strips
        for y in (cy - 30, cy - 6, cy + 18):
            d.rectangle((cx - 96, y, cx + 96, y + 6), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
            for x in range(int(cx - 92), int(cx + 92), 18):
                d.rectangle((x, y + 1, x + 10, y + 5), fill=CYAN_DIM)
        # robot arms inside
        for side in (-1, 1):
            line(d, [(cx + side * 76, cy + 52), (cx + side * 40, cy + 10), (cx + side * 26, cy - 16)], OUTLINE, 6)
            line(d, [(cx + side * 76, cy + 52), (cx + side * 40, cy + 10), (cx + side * 26, cy - 16)], STEEL_LIGHT, 2)
            ellipse(d, (cx + side * 26 - 7, cy - 23, cx + side * 26 + 7, cy - 9), AMBER, OUTLINE, 1)
        # central core
        ellipse(d, (cx - 18, cy + 12, cx + 18, cy + 48), (25, 58, 70, 255), OUTLINE, 1)
        ellipse(d, (cx - 9, cy + 21, cx + 9, cy + 39), CYAN, None)
        # roof beacon
        for x in (cx - 70, cx + 70):
            ellipse(d, (x - 6, cy - 102, x + 6, cy - 90), AMBER, OUTLINE, 1)
        d.text((cx - 58, cy + 96), "UNMANNED / 24H", font=F_TINY, fill=CYAN)


def draw_module(d, cx, cy, kind):
    if kind == "welder":
        # torch body
        line(d, [(cx - 54, cy + 56), (cx - 8, cy - 12)], OUTLINE, 16)
        line(d, [(cx - 54, cy + 56), (cx - 8, cy - 12)], STEEL_MID, 11)
        rr(d, (cx - 70, cy + 40, cx - 42, cy + 74), 5, DARK, OUTLINE, 2)
        d.rectangle((cx - 64, cy + 48, cx - 48, cy + 54), fill=AMBER)
        # nozzle
        polygon(d, [(cx - 14, cy - 18), (cx + 2, cy - 18), (cx + 8, cy - 2), (cx - 8, cy - 2)], STEEL_LIGHT, OUTLINE)
        # arc
        line(d, [(cx + 4, cy - 2), (cx + 16, cy + 10), (cx + 8, cy + 18), (cx + 22, cy + 34)], CYAN, 3)
        ellipse(d, (cx + 4, cy + 6, cx + 28, cy + 32), (*CYAN[:3], 70))
        # cable
        d.arc((cx - 88, cy + 54, cx - 22, cy + 108), 270, 90, fill=OUTLINE, width=4)
    elif kind == "baton":
        # shock baton diagonal
        line(d, [(cx - 66, cy + 64), (cx + 32, cy - 34)], OUTLINE, 18)
        line(d, [(cx - 66, cy + 64), (cx + 32, cy - 34)], DARK, 12)
        # grip
        line(d, [(cx - 62, cy + 58), (cx - 34, cy + 30)], AMBER, 8)
        for i in range(4):
            line(d, [(cx - 54 + i * 6, cy + 50 - i * 6), (cx - 48 + i * 6, cy + 56 - i * 6)], OUTLINE, 2)
        # tip coil
        ellipse(d, (cx + 20, cy - 48, cx + 50, cy - 18), CYAN_DIM, OUTLINE, 2)
        ellipse(d, (cx + 27, cy - 41, cx + 43, cy - 25), CYAN, None)
        for i in range(3):
            d.arc((cx + 18 - i * 3, cy - 50 + i * 3, cx + 52 + i * 3, cy - 16 - i * 3), 200, 340, fill=CYAN, width=2)
        # sparks
        line(d, [(cx + 48, cy - 30), (cx + 60, cy - 20)], CYAN, 2)
        line(d, [(cx + 48, cy - 30), (cx + 58, cy - 38)], CYAN, 2)
    elif kind == "armor":
        # chest armor plate
        polygon(d, [(cx - 62, cy - 52), (cx + 62, cy - 52), (cx + 50, cy + 40), (cx, cy + 72), (cx - 50, cy + 40)], STEEL, OUTLINE)
        polygon(d, [(cx - 48, cy - 40), (cx + 48, cy - 40), (cx + 38, cy + 30), (cx, cy + 56), (cx - 38, cy + 30)], STEEL_MID, OUTLINE)
        # collar
        rr(d, (cx - 30, cy - 62, cx + 30, cy - 44), 5, DARK, OUTLINE, 1)
        # chest light
        polygon(d, [(cx, cy - 24), (cx + 16, cy - 6), (cx, cy + 12), (cx - 16, cy - 6)], CYAN_DIM, OUTLINE)
        polygon(d, [(cx, cy - 17), (cx + 8, cy - 6), (cx, cy + 5), (cx - 8, cy - 6)], CYAN, None)
        # straps
        for side in (-1, 1):
            line(d, [(cx + side * 46, cy - 36), (cx + side * 64, cy + 24), (cx + side * 50, cy + 56)], OUTLINE, 7)
            line(d, [(cx + side * 46, cy - 36), (cx + side * 64, cy + 24), (cx + side * 50, cy + 56)], DARK, 3)
        # rivets
        for x, y in ((cx - 40, cy - 30), (cx + 40, cy - 30), (cx - 30, cy + 28), (cx + 30, cy + 28)):
            bolt(d, x, y, 3)
    else:
        # shoulder arc emitter
        rr(d, (cx - 72, cy - 14, cx + 40, cy + 46), 10, DARK, OUTLINE, 2)
        rr(d, (cx - 62, cy - 4, cx + 28, cy + 36), 7, PANEL, OUTLINE, 1)
        # barrel
        line(d, [(cx + 30, cy + 10), (cx + 82, cy + 10)], OUTLINE, 18)
        line(d, [(cx + 30, cy + 10), (cx + 82, cy + 10)], STEEL_MID, 12)
        rr(d, (cx + 68, cy - 2, cx + 88, cy + 22), 4, AMBER, OUTLINE, 1)
        # coils
        for i in range(4):
            ellipse(d, (cx + 40 + i * 10, cy + 2, cx + 54 + i * 10, cy + 18), None, OUTLINE, 2)
            ellipse(d, (cx + 42 + i * 10, cy + 4, cx + 52 + i * 10, cy + 16), None, CYAN, 1)
        # muzzle glow
        ellipse(d, (cx + 82, cy + 1, cx + 102, cy + 21), (*CYAN[:3], 80))
        line(d, [(cx + 92, cy + 11), (cx + 106, cy + 4)], CYAN, 2)
        line(d, [(cx + 92, cy + 11), (cx + 106, cy + 18)], CYAN, 2)
        # shoulder mount
        rr(d, (cx - 62, cy + 34, cx - 20, cy + 58), 5, STEEL, OUTLINE, 1)
        d.rectangle((cx - 54, cy + 41, cx - 28, cy + 46), fill=AMBER)


def main():
    image = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(image)
    # grid
    for x in range(0, W, 40):
        d.line([(x, 0), (x, H)], fill=(27, 40, 47, 70), width=1)
    for y in range(0, H, 40):
        d.line([(0, y), (W, y)], fill=(27, 40, 47, 70), width=1)

    d.text((MARGIN, 26), "CK ROBOTICS · FRONT VIEW BOARD", font=F_TITLE, fill=TEXT)
    d.text((MARGIN + 3, 78), "机器人 / 充电 / 维修 / 工厂 / 自卫模块 · 正面设定图 · 1 cell 比例", font=F_SUB, fill=MUTED)

    # row groups
    group_y = [TOP - 30, TOP + (CELL_H + GAP) - 30, TOP + 2 * (CELL_H + GAP) - 30, TOP + 3 * (CELL_H + GAP) - 30]
    groups = ["机器人 / ROBOTS", "充电与维修 / CHARGING & REPAIR", "工厂 / FACTORIES", "自卫模块 / DEFENSE MODULES"]
    for y, label in zip(group_y, groups):
        d.text((MARGIN, y), label, font=F_GROUP, fill=AMBER)

    # row 1: robots
    robots = [("Mk-I", "Mk-I 标准保姆机", "STANDARD"), ("Mk-II", "Mk-II 模块化保姆机", "MODULAR"),
              ("Mk-III", "Mk-III 重载保姆机", "HEAVY-DUTY"), ("Mk-IV", "Mk-IV 前哨保姆机", "OUTPOST"),
              ("Mk-V", "Mk-V 自维护保姆机", "SELF-MAINTAINING")]
    for i, (tier, title, sub) in enumerate(robots):
        panel(d, i + 1, 0, i, title, sub)
        x = MARGIN + i * (CELL_W + GAP)
        y = TOP
        draw_robot(d, x + CELL_W // 2, y + 160, tier.replace("Mk-", ""))

    # row 2: chargers + repairs
    row2 = [
        ("标准充电桩", "STANDARD CHARGER", "charger", "standard"),
        ("可拆卸充电桩", "DETACHABLE CHARGER", "charger", "detachable"),
        ("轻量现场充电桩", "LIGHT FIELD CHARGER", "charger", "field"),
        ("现场维修机", "FIELD REPAIR BAY", "repair", "field"),
        ("高速自动维修机", "HIGH-SPEED AUTO REPAIR", "repair", "auto"),
    ]
    for i, (title, sub, kind, variant) in enumerate(row2):
        panel(d, i + 6, 1, i, title, sub)
        x = MARGIN + i * (CELL_W + GAP)
        y = TOP + CELL_H + GAP
        if kind == "charger":
            draw_charger(d, x + CELL_W // 2, y + 160, variant)
        else:
            draw_repair(d, x + CELL_W // 2, y + 160, variant)

    # row 3: factories
    row3 = [
        ("工坊工厂", "WORKSHOP FACTORY", "workshop"),
        ("两工位工厂", "TWO-STATION FACTORY", "two-station"),
        ("骨架厂", "FRAME FAB", "frame"),
        ("预制工厂", "PREFAB FACTORY", "prefab"),
        ("无人自动工厂", "UNMANNED AUTO FACTORY", "auto"),
    ]
    for i, (title, sub, variant) in enumerate(row3):
        panel(d, i + 11, 2, i, title, sub)
        x = MARGIN + i * (CELL_W + GAP)
        y = TOP + 2 * (CELL_H + GAP)
        draw_factory(d, x + CELL_W // 2, y + 160, variant)

    # row 4: modules
    row4 = [
        ("电弧焊枪", "ARC WELDER", "welder"),
        ("电击棒", "SHOCK BATON", "baton"),
        ("轻甲", "LIGHT ARMOR", "armor"),
        ("电弧发射器", "ARC EMITTER", "emitter"),
    ]
    for i, (title, sub, variant) in enumerate(row4):
        panel(d, i + 16, 3, i, title, sub)
        x = MARGIN + i * (CELL_W + GAP)
        y = TOP + 3 * (CELL_H + GAP)
        draw_module(d, x + CELL_W // 2, y + 150, variant)

    # legend / footer
    d.text((MARGIN, H - 58), "Front elevation concept board · not final in-game sprites · 128 px game versions to follow after approval", font=F_TINY, fill=MUTED)
    d.text((W - MARGIN - 260, H - 58), "CK-M7 / COLONYKIT ROBOTICS", font=F_TINY, fill=AMBER)

    image.convert("RGB").save(OUT, quality=96)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
