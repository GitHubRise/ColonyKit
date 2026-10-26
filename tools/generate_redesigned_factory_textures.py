"""Directional texture set for the redesigned frame, two-station and prefab factories."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "buildings")
os.makedirs(OUT, exist_ok=True)

BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (25, 32, 40, 255)
DARK = (45, 55, 66, 255)
DARK2 = (33, 41, 50, 255)
WALL = (57, 68, 80, 255)
WALL_DARK = (38, 47, 57, 255)
STEEL = (132, 146, 158, 255)
STEEL_LIGHT = (197, 208, 215, 255)
STEEL_MID = (158, 171, 182, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, max(7, int(size)))


F_TITLE = font(34, True)
F_SUB = font(14)
F_LABEL = font(16, True)
F_SMALL = font(11)
F_TINY = font(9)


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(d, points, fill, outline=None):
    d.polygon(points, fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line(points, fill=fill, width=width, joint="curve")


def bolt(d, x, y, r=2):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


def hazard(d, x1, y1, x2, y2, n=5):
    d.rectangle((x1, y1, x2, y2), fill=DARK2, outline=OUTLINE, width=1)
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


def shadow(d, x1, y1, x2, y2):
    ellipse(d, (x1, y1, x2, y2), (0, 0, 0, 52))


def spark(d, x, y, n=8, spread=18):
    for i in range(n):
        a = (i / max(1, n - 1) - 0.5) * 2.0
        length = spread * (0.5 + 0.5 * ((i * 37) % 10) / 10)
        line(d, [(x, y), (x + math.cos(a) * length, y + math.sin(a) * length)], ORANGE, 2)
        line(d, [(x, y), (x + math.cos(a) * length * 0.65, y + math.sin(a) * length * 0.65)], AMBER_LIGHT, 1)


# ---------------------------------------------------------------- frame factory

def frame_front(view: str) -> Image.Image:
    if view == "top":
        w = h = 256
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        shadow(d, 16, 224, 240, 244)
        rr(d, (22, 22, 234, 234), 7, DARK, OUTLINE, 2)
        rr(d, (32, 32, 224, 224), 4, WALL, OUTLINE, 1)
        # canopy strips
        d.rectangle((32, 32, 224, 58), fill=STEEL_MID, outline=OUTLINE, width=1)
        d.rectangle((32, 198, 224, 224), fill=STEEL_MID, outline=OUTLINE, width=1)
        # gantry rails
        d.rectangle((46, 64, 210, 74), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
        d.rectangle((46, 184, 210, 194), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
        # trolley / hook
        d.rectangle((112, 64, 144, 88), fill=AMBER, outline=OUTLINE, width=1)
        line(d, [(128, 88), (128, 112)], OUTLINE, 2)
        # frame jig
        line(d, [(88, 112), (168, 112), (168, 170), (88, 170), (88, 112)], STEEL_LIGHT, 3)
        for x, y in ((88, 112), (168, 112), (88, 170), (168, 170)):
            ellipse(d, (x - 4, y - 4, x + 4, y + 4), AMBER, OUTLINE, 1)
        # side racks
        for side in (-1, 1):
            bx = 128 + side * 66
            for i in range(4):
                d.rectangle((bx - 18, 108 + i * 14, bx + 18, 112 + i * 14), fill=STEEL_MID, outline=OUTLINE, width=1)
        spark(d, 88, 112, 6, 16)
        spark(d, 168, 170, 6, 16)
        # south opening
        d.rectangle((104, 206, 152, 224), fill=(10, 16, 21, 255), outline=OUTLINE, width=1)
        hazard(d, 104, 198, 152, 206, 4)
        return im

    w = h = 256
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shadow(d, 18, 226, 238, 246)

    if view in ("south", "north"):
        # towers
        for x in (22, 194):
            rr(d, (x, 94, x + 40, 232), 4, DARK, OUTLINE, 2)
            for y in range(108, 220, 20):
                d.rectangle((x + 7, y, x + 33, y + 4), fill=STEEL_MID)
            d.rectangle((x + 5, 200, x + 35, 214), fill=WALL_DARK, outline=OUTLINE, width=1)
        # roof truss
        d.rectangle((36, 78, 220, 96), fill=STEEL, outline=OUTLINE, width=2)
        for x in range(52, 214, 24):
            line(d, [(x, 96), (x + 14, 108)], STEEL_MID, 2)
        # gantry rail
        d.rectangle((52, 62, 204, 72), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
        d.rectangle((112, 66, 144, 88), fill=AMBER, outline=OUTLINE, width=1)
        line(d, [(128, 88), (128, 118)], OUTLINE, 2)
        if view == "south":
            # hanging frame and welding
            line(d, [(80, 126), (176, 126), (176, 204), (80, 204), (80, 126)], STEEL_LIGHT, 4)
            line(d, [(80, 126), (176, 204)], STEEL_LIGHT, 3)
            line(d, [(176, 126), (80, 204)], STEEL_LIGHT, 3)
            for x, y in ((80, 126), (176, 126), (80, 204), (176, 204)):
                ellipse(d, (x - 4, y - 4, x + 4, y + 4), AMBER, OUTLINE, 1)
            for side, sx in ((-1, 62), (1, 194)):
                line(d, [(sx, 154), (128 + side * 34, 168)], OUTLINE, 5)
                line(d, [(sx, 154), (128 + side * 34, 168)], STEEL_LIGHT, 2)
                spark(d, 128 + side * 34, 168, 7, 16)
            # material rack
            for i in range(4):
                d.rectangle((64, 184 + i * 8, 100, 188 + i * 8), fill=STEEL_MID, outline=OUTLINE, width=1)
            text = "FRAME FAB"
            rr(d, (98, 38, 158, 56), 3, DARK2, OUTLINE, 1)
            d.text((104, 41), text, font=F_TINY, fill=AMBER)
        else:
            # rear service side: jig back, cabinets, cables
            rr(d, (84, 126, 172, 204), 4, WALL_DARK, OUTLINE, 1)
            for y in (140, 156, 172, 188):
                d.rectangle((96, y, 160, y + 5), fill=STEEL_MID)
            for x in (46, 206):
                d.rectangle((x - 10, 168, x + 10, 204), fill=DARK, outline=OUTLINE, width=1)
                d.rectangle((x - 6, 174, x + 6, 180), fill=CYAN_DIM)
            line(d, [(76, 188), (84, 160), (96, 132)], OUTLINE, 2)
        hazard(d, 104, 220, 152, 230, 4)
        return im

    if view == "west":
        return ImageOps.mirror(frame_front("east"))
    # side elevation: two open towers, long truss, side frame and rack
    rr(d, (34, 94, 62, 232), 3, DARK, OUTLINE, 2)
    rr(d, (194, 94, 222, 232), 3, DARK, OUTLINE, 2)
    d.rectangle((44, 64, 212, 82), fill=STEEL, outline=OUTLINE, width=2)
    for x in (50, 78, 106, 134, 162, 190):
        line(d, [(x, 82), (x + 12, 94)], STEEL_MID, 2)
    d.rectangle((54, 52, 202, 60), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
    # side frame
    line(d, [(76, 118), (176, 118), (176, 194), (76, 194), (76, 118)], STEEL_LIGHT, 4)
    line(d, [(76, 118), (176, 194)], STEEL_LIGHT, 2)
    line(d, [(176, 118), (76, 194)], STEEL_LIGHT, 2)
    for x, y in ((76, 118), (176, 118), (76, 194), (176, 194)):
        ellipse(d, (x - 4, y - 4, x + 4, y + 4), AMBER, OUTLINE, 1)
    # side welding arm and rack
    line(d, [(62, 148), (94, 168), (126, 180)], OUTLINE, 5)
    line(d, [(62, 148), (94, 168), (126, 180)], STEEL_LIGHT, 2)
    spark(d, 126, 180, 7, 16)
    for i in range(4):
        d.rectangle((28, 178 + i * 9, 58, 183 + i * 9), fill=STEEL_MID, outline=OUTLINE, width=1)
    hazard(d, 104, 220, 152, 230, 4)
    return im


# ---------------------------------------------------------------- two-station

def two_front(view: str) -> Image.Image:
    if view == "top":
        w, h = 512, 384
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        shadow(d, 12, 350, 500, 374)
        rr(d, (16, 16, 496, 368), 8, DARK, OUTLINE, 2)
        rr(d, (28, 28, 484, 356), 4, WALL, OUTLINE, 1)
        # two cells
        for sx in (28, 262):
            rr(d, (sx, 28, sx + 222, 260), 4, STEEL_MID, OUTLINE, 1)
            rr(d, (sx + 10, 38, sx + 212, 250), 3, PANEL, OUTLINE, 1)
            for i in range(4):
                d.rectangle((sx + 26, 60 + i * 40, sx + 196, 68 + i * 40), fill=CYAN_DIM, outline=OUTLINE, width=1)
            d.rectangle((sx + 60, 320, sx + 162, 350), fill=AMBER if sx < 262 else CYAN, outline=OUTLINE, width=1)
            hazard(d, sx + 64, 306, sx + 158, 316, 4)
        # shared tower / spine
        d.rectangle((240, 28, 272, 320), fill=DARK, outline=OUTLINE, width=2)
        d.rectangle((246, 56, 266, 140), fill=AMBER, outline=OUTLINE, width=1)
        d.rectangle((246, 164, 266, 210), fill=CYAN_DIM, outline=OUTLINE, width=1)
        # input / output
        d.rectangle((90, 28, 150, 54), fill=DARK2, outline=OUTLINE, width=1)
        d.rectangle((360, 28, 420, 54), fill=DARK2, outline=OUTLINE, width=1)
        d.rectangle((96, 324, 420, 352), fill=DARK2, outline=OUTLINE, width=1)
        for x in range(108, 414, 24):
            d.rectangle((x, 330, x + 10, 344), fill=STEEL_MID)
        return im

    if view in ("south", "north"):
        w, h = 512, 256
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        shadow(d, 14, 222, 498, 244)
        # body
        rr(d, (18, 118, 494, 232), 4, WALL, OUTLINE, 2)
        # twin sawtooth roof
        for sx, col in ((18, STEEL), (256, STEEL_MID)):
            polygon(d, [(sx, 118), (sx + 238, 118), (sx + 208, 64), (sx + 30, 64)], col, OUTLINE)
            polygon(d, [(sx + 24, 114), (sx + 214, 114), (sx + 190, 80), (sx + 48, 80)], DARK, OUTLINE)
            polygon(d, [(sx + 66, 84), (sx + 172, 84), (sx + 154, 94), (sx + 84, 94)], CYAN_DIM, OUTLINE)
        # central tool tower
        rr(d, (238, 52, 274, 226), 3, DARK, OUTLINE, 2)
        d.rectangle((244, 70, 268, 140), fill=AMBER, outline=OUTLINE, width=1)
        d.rectangle((244, 158, 268, 196), fill=CYAN_DIM, outline=OUTLINE, width=1)
        d.rectangle((248, 166, 264, 174), fill=CYAN)
        # doors
        for sx, col in ((44, AMBER), (294, CYAN)):
            rr(d, (sx, 142, sx + 174, 226), 3, WALL_DARK, STEEL_LIGHT, 2)
            rr(d, (sx + 8, 150, sx + 166, 218), 2, (30, 38, 47, 255), OUTLINE, 1)
            for y in range(156, 214, 9):
                d.rectangle((sx + 16, y, sx + 158, y + 4), fill=STEEL_MID)
            d.rectangle((sx + 8, 150, sx + 166, 158), fill=col, outline=OUTLINE, width=1)
            ellipse(d, (sx + 76, 128, sx + 100, 138), col, OUTLINE, 1)
        if view == "south":
            d.rectangle((44, 228, 476, 244), fill=DARK2, outline=OUTLINE, width=1)
            for x in range(56, 464, 20):
                d.rectangle((x, 232, x + 9, 240), fill=STEEL_MID)
            rr(d, (208, 32, 304, 50), 3, DARK2, OUTLINE, 1)
            d.text((216, 35), "2-STATION", font=F_TINY, fill=AMBER)
        else:
            # rear service doors / vents
            for sx in (44, 294):
                rr(d, (sx, 146, sx + 120, 222), 3, WALL_DARK, OUTLINE, 1)
                for y in (158, 174, 190, 206):
                    d.rectangle((sx + 12, y, sx + 106, y + 4), fill=STEEL_MID)
            for x in (180, 338):
                d.rectangle((x, 130, x + 14, 190), fill=STEEL_MID, outline=OUTLINE, width=1)
        hazard(d, 74, 222, 190, 230, 4)
        hazard(d, 324, 222, 440, 230, 4)
        return im

    if view == "west":
        return ImageOps.mirror(two_front("east"))
    w, h = 384, 256
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shadow(d, 10, 222, 374, 244)
    rr(d, (14, 118, 370, 232), 4, WALL, OUTLINE, 2)
    # side sawtooth roof
    for sx in (14, 198):
        polygon(d, [(sx, 118), (sx + 172, 118), (sx + 148, 64), (sx + 24, 64)], STEEL if sx < 198 else STEEL_MID, OUTLINE)
        polygon(d, [(sx + 18, 114), (sx + 154, 114), (sx + 136, 80), (sx + 38, 80)], DARK, OUTLINE)
    # side service doors
    for sx, col in ((36, AMBER), (212, CYAN)):
        rr(d, (sx, 146, sx + 118, 226), 3, WALL_DARK, OUTLINE, 1)
        for y in (158, 174, 190, 206):
            d.rectangle((sx + 12, y, sx + 104, y + 4), fill=STEEL_MID)
        d.rectangle((sx + 12, 146, sx + 104, 152), fill=col, outline=OUTLINE, width=1)
    # central tower edge
    d.rectangle((168, 64, 204, 220), fill=DARK, outline=OUTLINE, width=2)
    d.rectangle((176, 80, 196, 144), fill=AMBER, outline=OUTLINE, width=1)
    # conveyor end / control
    d.rectangle((28, 228, 350, 244), fill=DARK2, outline=OUTLINE, width=1)
    for x in range(40, 340, 22):
        d.rectangle((x, 232, x + 9, 240), fill=STEEL_MID)
    rr(d, (298, 70, 356, 118), 3, STEEL, OUTLINE, 1)
    d.rectangle((308, 80, 346, 86), fill=CYAN)
    d.rectangle((308, 96, 330, 102), fill=GREEN)
    return im


# ---------------------------------------------------------------- prefab

def prefab_front(view: str) -> Image.Image:
    if view == "top":
        w = h = 384
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        shadow(d, 12, 350, 372, 374)
        rr(d, (16, 16, 368, 368), 8, DARK, OUTLINE, 2)
        rr(d, (28, 28, 356, 356), 4, WALL, OUTLINE, 1)
        rr(d, (40, 40, 344, 344), 3, PANEL, OUTLINE, 1)
        # central assembly bay
        d.rectangle((108, 96, 276, 296), fill=DARK2, outline=OUTLINE, width=1)
        # crane rail
        d.rectangle((52, 62, 332, 76), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
        d.rectangle((178, 62, 208, 92), fill=AMBER, outline=OUTLINE, width=1)
        line(d, [(193, 92), (193, 128)], OUTLINE, 2)
        # module
        polygon(d, [(150, 128), (238, 128), (228, 232), (160, 232)], STEEL_LIGHT, OUTLINE)
        for y in (148, 170, 192):
            d.rectangle((164, y, 224, y + 4), fill=STEEL_MID)
        # input / output
        for i, (x, y) in enumerate(((48, 96), (48, 168), (48, 240))):
            rr(d, (x, y, x + 46, y + 46), 2, (STEEL_MID, AMBER, CYAN_DIM)[i], OUTLINE, 1)
            d.text((x + 7, y + 16), f"M{i+1}", font=F_TINY, fill=TEXT if i != 1 else DARK2)
        for i in range(3):
            rr(d, (292, 96 + i * 72, 334, 142 + i * 72), 2, (120, 92, 65, 255), OUTLINE, 1)
            d.rectangle((298, 108 + i * 72, 328, 114 + i * 72), fill=AMBER)
        # south door / hazard
        d.rectangle((158, 330, 250, 356), fill=CYAN_DIM, outline=OUTLINE, width=1)
        hazard(d, 158, 314, 250, 326, 4)
        return im

    if view in ("south", "north"):
        w, h = 384, 256
        im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        shadow(d, 10, 222, 374, 244)
        rr(d, (22, 118, 362, 232), 4, WALL, OUTLINE, 2)
        polygon(d, [(12, 120), (372, 120), (342, 76), (42, 76)], STEEL, OUTLINE)
        polygon(d, [(34, 116), (350, 116), (324, 88), (60, 88)], DARK, OUTLINE)
        # roof containers
        for i, col in enumerate((STEEL_MID, AMBER, CYAN_DIM)):
            cx = 34 + i * 106
            rr(d, (cx, 32, cx + 88, 76), 2, col, OUTLINE, 1)
            d.rectangle((cx + 8, 40, cx + 80, 46), fill=DARK2, outline=OUTLINE, width=1)
            d.text((cx + 10, 52), f"M{i+1}", font=F_TINY, fill=TEXT if i != 1 else DARK2)
        # yellow gantry
        line(d, [(18, 220), (18, 48), (366, 48), (366, 220)], OUTLINE, 10)
        line(d, [(18, 220), (18, 48), (366, 48), (366, 220)], AMBER, 5)
        d.rectangle((158, 40, 226, 62), fill=AMBER, outline=OUTLINE, width=1)
        line(d, [(192, 62), (192, 108)], OUTLINE, 2)
        # hanging module
        polygon(d, [(158, 108), (226, 108), (216, 170), (168, 170)], STEEL_LIGHT, OUTLINE)
        for y in (122, 138, 154):
            d.rectangle((170, y, 214, y + 4), fill=STEEL_MID)
        if view == "south":
            # main door
            rr(d, (112, 150, 272, 226), 3, WALL_DARK, STEEL_LIGHT, 2)
            rr(d, (122, 158, 262, 218), 2, (30, 38, 47, 255), OUTLINE, 1)
            for y in range(164, 214, 9):
                d.rectangle((130, y, 254, y + 4), fill=STEEL_MID)
            d.rectangle((122, 158, 262, 166), fill=CYAN_DIM, outline=OUTLINE, width=1)
            # side pallets
            for i in range(3):
                rr(d, (30, 164 + i * 20, 96, 182 + i * 20), 2, (120, 92, 65, 255), OUTLINE, 1)
            rr(d, (278, 174, 348, 210), 2, (120, 92, 65, 255), OUTLINE, 1)
            d.text((286, 186), "OUT", font=F_TINY, fill=AMBER_LIGHT)
            rr(d, (160, 28, 224, 46), 3, DARK2, OUTLINE, 1)
            d.text((168, 31), "PREFAB", font=F_TINY, fill=AMBER)
        else:
            # rear service
            rr(d, (112, 152, 214, 226), 3, WALL_DARK, OUTLINE, 1)
            for y in (166, 182, 198, 212):
                d.rectangle((124, y, 202, y + 4), fill=STEEL_MID)
            rr(d, (242, 144, 292, 226), 3, STEEL_MID, OUTLINE, 1)
            for y in (156, 174, 192, 210):
                d.rectangle((250, y, 284, y + 4), fill=DARK2)
            line(d, [(78, 136), (78, 184), (58, 184)], OUTLINE, 3)
        hazard(d, 120, 222, 264, 230, 4)
        return im

    if view == "west":
        return ImageOps.mirror(prefab_front("east"))
    w, h = 384, 256
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shadow(d, 10, 222, 374, 244)
    rr(d, (22, 118, 362, 232), 4, WALL, OUTLINE, 2)
    polygon(d, [(12, 120), (372, 120), (342, 76), (42, 76)], STEEL, OUTLINE)
    polygon(d, [(34, 116), (350, 116), (324, 88), (60, 88)], DARK, OUTLINE)
    # side containers
    for i, col in enumerate((STEEL_MID, AMBER)):
        cx = 46 + i * 104
        rr(d, (cx, 36, cx + 82, 76), 2, col, OUTLINE, 1)
        d.rectangle((cx + 8, 44, cx + 74, 50), fill=DARK2, outline=OUTLINE, width=1)
    # gantry side
    line(d, [(18, 220), (18, 50), (366, 50), (366, 220)], OUTLINE, 10)
    line(d, [(18, 220), (18, 50), (366, 50), (366, 220)], AMBER, 5)
    d.rectangle((170, 42, 224, 64), fill=AMBER, outline=OUTLINE, width=1)
    # side service door / windows
    rr(d, (136, 150, 236, 226), 3, WALL_DARK, OUTLINE, 1)
    for y in (164, 180, 196, 212):
        d.rectangle((148, y, 224, y + 4), fill=STEEL_MID)
    for x in (58, 280):
        rr(d, (x, 140, x + 26, 158), 2, (24, 83, 96, 255), OUTLINE, 1)
        d.rectangle((x + 3, 144, x + 23, 148), fill=CYAN)
    rr(d, (300, 174, 350, 210), 2, (120, 92, 65, 255), OUTLINE, 1)
    return im


# ---------------------------------------------------------------- sheets

def sheet(prefix, title, subtitle, names, max_h=320):
    imgs = [Image.open(os.path.join(OUT, f"{prefix}_{key}.png")).convert("RGBA") for key in names]
    cols = len(names)
    cell_w = 310
    sheet_im = Image.new("RGBA", (40 + cols * (cell_w + 18), max_h + 160), BG)
    d = ImageDraw.Draw(sheet_im)
    for x in range(0, sheet_im.width, 40):
        d.line((x, 0, x, sheet_im.height), fill=(27, 40, 47, 70), width=1)
    d.text((24, 16), title, font=F_TITLE, fill=TEXT)
    d.text((26, 54), subtitle, font=F_SUB, fill=MUTED)
    for i, key in enumerate(names):
        img = imgs[i]
        scale = min((cell_w - 10) / img.width, max_h / img.height)
        img = img.resize((max(1, int(img.width * scale)), max(1, int(img.height * scale))), Image.LANCZOS)
        x = 30 + i * (cell_w + 18)
        y = 100 + (max_h - img.height) // 2
        sheet_im.alpha_composite(img, (x, y))
        d.rectangle((x, 100, x + cell_w, 100 + max_h), outline=LINE, width=1)
        labels = ["南 / FRONT", "北 / BACK", "东 / RIGHT", "西 / LEFT", "俯视 / TOP"]
        d.text((x, 112 + max_h), labels[i], font=F_LABEL, fill=TEXT)
        d.text((x, 138 + max_h), f"{prefix}_{key}.png · {imgs[i].width}x{imgs[i].height}", font=F_SMALL, fill=MUTED)
    sheet_im.convert("RGB").save(os.path.join(OUT, f"{prefix}_views.png"), quality=95)


def main():
    for view in ("south", "north", "east", "west", "top"):
        frame_front(view).save(os.path.join(OUT, f"FrameFactory_{view}.png"))
        two_front(view).save(os.path.join(OUT, f"TwoStationFactory_{view}.png"))
        prefab_front(view).save(os.path.join(OUT, f"PrefabFactory_{view}.png"))

    sheet("FrameFactory", "CK_FRAMEFACTORY · 2×2 FULL DIRECTION SET",
          "south / north / east / west / top · 128 px/cell · open truss frame fab",
          ("south", "north", "east", "west", "top"))
    sheet("TwoStationFactory", "CK_TWOSTATIONFACTORY · 4×3 FULL DIRECTION SET",
          "south / north / east / west / top · 128 px/cell · twin sawtooth + shared tower",
          ("south", "north", "east", "west", "top"))
    sheet("PrefabFactory", "CK_PREFABFACTORY · 3×3 FULL DIRECTION SET",
          "south / north / east / west / top · 128 px/cell · yellow gantry + container yard",
          ("south", "north", "east", "west", "top"))
    print("wrote", OUT)


if __name__ == "__main__":
    main()
