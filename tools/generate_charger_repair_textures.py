"""Generate 128 px charger and 256 px repair-bay texture sets."""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "buildings")
os.makedirs(OUT, exist_ok=True)

S = 4
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
STEEL = (134, 148, 160, 255)
STEEL_LIGHT = (197, 208, 215, 255)
STEEL_MID = (160, 173, 184, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)
TREAD = (38, 44, 52, 255)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, max(7, int(size)))


F_TITLE = font(32, True)
F_SUB = font(14)
F_LABEL = font(15, True)
F_SMALL = font(10)
F_TINY = font(8)


def P(v):
    return int(round(v * S))


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle((P(box[0]), P(box[1]), P(box[2]), P(box[3])), radius=P(r),
                        fill=fill, outline=outline, width=max(1, P(width)))


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse((P(box[0]), P(box[1]), P(box[2]), P(box[3])), fill=fill,
              outline=outline, width=max(1, P(width)))


def polygon(d, points, fill, outline=None):
    d.polygon([(P(x), P(y)) for x, y in points], fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line([(P(x), P(y)) for x, y in points], fill=fill, width=max(1, P(width)), joint="curve")


def hazard(d, x1, y1, x2, y2, n=4):
    d.rectangle((P(x1), P(y1), P(x2), P(y2)), fill=DARK2, outline=OUTLINE, width=P(1))
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


def bolt(d, x, y, r=1.5):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


def cable(d, kind, view):
    # exits the unit on the named side; top uses the south port
    if view == "north":
        pts = [(64, 36), (64, 22), (56, 12)]
    elif view == "east":
        pts = [(102, 76), (116, 76), (122, 68)]
    elif view == "west":
        pts = [(26, 76), (12, 76), (6, 68)]
    else:
        pts = [(64, 112), (64, 120), (72, 124)]
    line(d, pts, OUTLINE, 2)
    line(d, pts, AMBER, 1)
    x, y = pts[-1]
    if kind == "standard":
        rr(d, (x - 4, y - 3, x + 4, y + 3), 1, AMBER, OUTLINE, 1)
    else:
        rr(d, (x - 5, y - 4, x + 5, y + 4), 1, DARK2, OUTLINE, 1)
        d.rectangle((P(x - 2), P(y - 2), P(x + 2), P(y + 2)), fill=CYAN)


def charger(kind: str, view: str) -> Image.Image:
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ellipse(d, (20, 116, 108, 126), (0, 0, 0, 50))

    if kind == "standard":
        # floor pad
        ellipse(d, (18, 30, 110, 122), DARK, OUTLINE, 2)
        ellipse(d, (26, 38, 102, 114), (24, 83, 96, 255), OUTLINE, 1)
        ellipse(d, (38, 50, 90, 102), DARK2, OUTLINE, 1)
        # central post
        rr(d, (52, 42, 76, 104), 6, STEEL, OUTLINE, 2)
        rr(d, (57, 50, 71, 78), 4, PANEL, OUTLINE, 1)
        d.rectangle((P(61), P(56), P(67), P(72)), fill=CYAN)
        rr(d, (48, 34, 80, 42), 3, AMBER, OUTLINE, 1)
        # glow ring
        ellipse(d, (40, 52, 88, 100), None, (*CYAN[:3], 140), 2)
        cable(d, kind, view)
        # corner bolts
        for x, y in ((28, 40), (100, 40), (28, 112), (100, 112)):
            bolt(d, x, y, 2)
    elif kind == "detachable":
        # carry case
        rr(d, (30, 40, 98, 116), 10, STEEL_MID, OUTLINE, 2)
        rr(d, (38, 48, 90, 108), 6, PANEL, OUTLINE, 1)
        d.rectangle((P(44), P(58), P(84), P(66)), fill=CYAN)
        d.rectangle((P(44), P(76), P(72), P(82)), fill=GREEN)
        # handle
        d.arc((P(40), P(18), P(88), P(56)), 180, 360, fill=OUTLINE, width=P(4))
        d.arc((P(40), P(18), P(88), P(56)), 180, 360, fill=AMBER, width=P(2))
        # wheels
        for x in (40, 88):
            ellipse(d, (x - 7, 102, x + 7, 116), TREAD, OUTLINE, 2)
            ellipse(d, (x - 3, 106, x + 3, 112), STEEL_MID, OUTLINE, 1)
        # latch / port
        rr(d, (54, 104, 74, 114), 2, AMBER, OUTLINE, 1)
        cable(d, kind, view)
    else:
        # rugged field unit
        rr(d, (26, 44, 102, 116), 8, (73, 91, 88, 255), OUTLINE, 2)
        rr(d, (34, 52, 94, 108), 5, PANEL, OUTLINE, 1)
        # folded solar panel
        polygon(d, [(28, 40), (92, 22), (104, 48), (40, 66)], (31, 60, 77, 255), OUTLINE)
        for i in range(3):
            line(d, [(34 + i * 18, 38 - i * 4), (96 + i * 10, 24 + i * 4)], CYAN_DIM, 1)
        # screen / battery
        d.rectangle((P(42), P(72), P(84), P(79)), fill=CYAN)
        d.rectangle((P(42), P(88), P(64), P(93)), fill=GREEN)
        # handle
        d.arc((P(38), P(100), P(90), P(124)), 180, 360, fill=OUTLINE, width=P(4))
        d.arc((P(38), P(100), P(90), P(124)), 180, 360, fill=AMBER, width=P(2))
        cable(d, kind, view)
    return im


# ---------------------------------------------------------------- repair bay

def repair(kind: str, view: str) -> Image.Image:
    size = 256
    im = Image.new("RGBA", (size * S, size * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    accent = CYAN if kind == "auto" else AMBER

    if view == "top":
        ellipse(d, (18, 232, 238, 252), (0, 0, 0, 45))
        rr(d, (18, 18, 238, 238), 8, WALL, OUTLINE, 2)
        rr(d, (30, 30, 226, 226), 5, PANEL, OUTLINE, 1)
        # 2x2 frame / bay
        for x in (48, 208):
            d.rectangle((P(x - 8), P(48), P(x + 8), P(208)), fill=STEEL, outline=OUTLINE, width=P(1))
        d.rectangle((P(48), P(48), P(208), P(64)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
        # robot top
        ellipse(d, (98, 86, 158, 140), STEEL_MID, OUTLINE, 1)
        ellipse(d, (108, 94, 148, 128), DARK2, OUTLINE, 1)
        d.rectangle((P(116), P(106), P(140), P(112)), fill=CYAN)
        rr(d, (112, 140, 144, 198), 6, DARK, OUTLINE, 1)
        # arms
        for side in (-1, 1):
            line(d, [(128 + side * 52, 92), (128 + side * 26, 132), (128 + side * 18, 170)], OUTLINE, 4)
            line(d, [(128 + side * 52, 92), (128 + side * 26, 132), (128 + side * 18, 170)], accent, 2)
        # tool carts
        for x in (58, 176):
            rr(d, (x, 168, x + 32, 194), 3, DARK2, OUTLINE, 1)
            d.rectangle((P(x + 4), P(174), P(x + 28), P(180)), fill=accent)
        # south opening / hazard
        d.rectangle((P(104), P(218), P(152), P(238)), fill=(10, 16, 21, 255), outline=OUTLINE, width=P(1))
        hazard(d, 104, 206, 152, 216, 4)
        return im

    if view in ("south", "north"):
        ellipse(d, (16, 232, 240, 252), (0, 0, 0, 45))
        # bay base
        rr(d, (24, 40, 232, 232), 6, WALL, OUTLINE, 2)
        # frame posts / top beam
        for x in (36, 204):
            d.rectangle((P(x), P(56), P(x + 16), P(226)), fill=STEEL, outline=OUTLINE, width=P(1))
        d.rectangle((P(36), P(40), P(220), P(58)), fill=STEEL_MID, outline=OUTLINE, width=P(2))
        # robot
        body_fill = DARK
        if view == "south":
            ellipse(d, (104, 78, 152, 126), STEEL_MID, OUTLINE, 1)
            ellipse(d, (114, 88, 142, 116), DARK2, OUTLINE, 1)
            d.rectangle((P(120), P(98), P(136), P(104)), fill=CYAN)
            rr(d, (108, 126, 148, 196), 6, body_fill, OUTLINE, 1)
            # chest screen
            rr(d, (116, 138, 140, 168), 3, PANEL, OUTLINE, 1)
            d.rectangle((P(120), P(146), P(136), P(152)), fill=accent)
            # arms
            for side in (-1, 1):
                line(d, [(128 + side * 40, 128), (128 + side * 20, 158), (128 + side * 14, 192)], OUTLINE, 5)
                line(d, [(128 + side * 40, 128), (128 + side * 20, 158), (128 + side * 14, 192)], STEEL_LIGHT, 2)
        else:
            # rear: robot back, cables and rear doors
            rr(d, (108, 80, 148, 196), 6, body_fill, OUTLINE, 1)
            rr(d, (116, 94, 140, 176), 3, PANEL, OUTLINE, 1)
            for y in (106, 122, 138, 154, 170):
                d.rectangle((P(120), P(y), P(136), P(y + 3)), fill=STEEL_MID)
            d.rectangle((P(36), P(66), P(96), P(220)), fill=WALL_DARK, outline=OUTLINE, width=P(1))
            for y in (78, 94, 110, 126, 142):
                d.rectangle((P(44), P(y), P(88), P(y + 4)), fill=STEEL_MID)
            line(d, [(196, 78), (196, 166), (216, 166)], OUTLINE, 3)
        # side panels / hazard
        if view == "south":
            for x in (42, 194):
                d.rectangle((P(x), P(70), P(x + 8), P(212)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
            d.rectangle((P(50), P(210), P(206), P(226)), fill=DARK2, outline=OUTLINE, width=P(1))
            for x in range(60, 200, 18):
                d.rectangle((P(x), P(214), P(x + 8), P(222)), fill=STEEL_MID)
            hazard(d, 104, 226, 152, 234, 4)
        else:
            hazard(d, 108, 226, 148, 234, 4)
        return im

    if view == "west":
        return ImageOps.mirror(repair(kind, "east"))
    # side elevation
    ellipse(d, (16, 232, 240, 252), (0, 0, 0, 45))
    rr(d, (24, 40, 232, 232), 6, WALL, OUTLINE, 2)
    # side frame
    for x in (44, 196):
        d.rectangle((P(x), P(56), P(x + 16), P(226)), fill=STEEL, outline=OUTLINE, width=P(1))
    d.rectangle((P(44), P(40), P(212), P(58)), fill=STEEL_MID, outline=OUTLINE, width=P(2))
    # robot side
    ellipse(d, (92, 78, 146, 126), STEEL_MID, OUTLINE, 1)
    rr(d, (100, 126, 144, 196), 6, DARK, OUTLINE, 1)
    # side arm
    line(d, [(190, 120), (150, 150), (136, 182)], OUTLINE, 5)
    line(d, [(190, 120), (150, 150), (136, 182)], accent, 2)
    # side service panel / cart
    rr(d, (172, 156, 216, 220), 4, WALL_DARK, OUTLINE, 1)
    for y in (168, 180, 192, 204):
        d.rectangle((P(180), P(y), P(208), P(y + 4)), fill=STEEL_MID)
    rr(d, (56, 172, 94, 214), 3, DARK2, OUTLINE, 1)
    d.rectangle((P(62), P(180), P(88), P(186)), fill=accent)
    hazard(d, 104, 226, 152, 234, 4)
    return im


# ---------------------------------------------------------------- sheets

def sheet_chargers():
    names = [("StandardCharger", "标准充电桩"), ("DetachableCharger", "可拆卸充电桩"), ("FieldCharger", "轻量现场充电桩")]
    views = [("south", "南 / FRONT"), ("north", "北 / BACK"), ("east", "东 / RIGHT"), ("west", "西 / LEFT"), ("top", "俯视 / TOP")]
    im = Image.new("RGBA", (1680, 830), BG)
    d = ImageDraw.Draw(im)
    for x in range(0, 1680, 40):
        d.line((x, 0, x, 830), fill=(27, 40, 47, 70), width=1)
    d.text((24, 16), "CK ROBOTICS · CHARGER TEXTURES", font=F_TITLE, fill=TEXT)
    d.text((26, 52), "128×128 · south / north / east / west / top · candidate set", font=F_SUB, fill=MUTED)
    for r, (prefix, label) in enumerate(names):
        y = 80 + r * 240
        d.text((18, y + 80), label, font=F_LABEL, fill=AMBER)
        for c, (view, _) in enumerate(views):
            x = 150 + c * 300
            img = Image.open(os.path.join(OUT, f"{prefix}_{view}.png")).convert("RGBA").resize((170, 170), Image.LANCZOS)
            im.alpha_composite(img, (x, y))
            d.rectangle((x, y, x + 170, y + 170), outline=LINE, width=1)
            d.text((x, y + 176), views[c][1], font=F_SMALL, fill=MUTED)
    im.convert("RGB").save(os.path.join(OUT, "Charger_views.png"), quality=95)


def sheet_repair():
    names = [("FieldRepair", "现场维修机"), ("AutoRepair", "高速自动维修机")]
    views = [("south", "南 / FRONT"), ("north", "北 / BACK"), ("east", "东 / RIGHT"), ("west", "西 / LEFT"), ("top", "俯视 / TOP")]
    im = Image.new("RGBA", (1680, 620), BG)
    d = ImageDraw.Draw(im)
    for x in range(0, 1680, 40):
        d.line((x, 0, x, 620), fill=(27, 40, 47, 70), width=1)
    d.text((24, 16), "CK ROBOTICS · REPAIR BAY TEXTURES", font=F_TITLE, fill=TEXT)
    d.text((26, 52), "256×256 · south / north / east / west / top · candidate set", font=F_SUB, fill=MUTED)
    for r, (prefix, label) in enumerate(names):
        y = 80 + r * 250
        d.text((18, y + 90), label, font=F_LABEL, fill=AMBER)
        for c, (view, _) in enumerate(views):
            x = 150 + c * 300
            img = Image.open(os.path.join(OUT, f"{prefix}_{view}.png")).convert("RGBA").resize((190, 190), Image.LANCZOS)
            im.alpha_composite(img, (x, y))
            d.rectangle((x, y, x + 190, y + 190), outline=LINE, width=1)
            d.text((x, y + 196), views[c][1], font=F_SMALL, fill=MUTED)
    im.convert("RGB").save(os.path.join(OUT, "RepairBay_views.png"), quality=95)


def main():
    for kind, prefix in (("standard", "StandardCharger"), ("detachable", "DetachableCharger"), ("field", "FieldCharger")):
        for view in ("south", "north", "east", "west", "top"):
            charger(kind, view).resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, f"{prefix}_{view}.png"))
    sheet_chargers()

    for kind, prefix in (("field", "FieldRepair"), ("auto", "AutoRepair")):
        for view in ("south", "north", "east", "west", "top"):
            repair(kind, view).resize((256, 256), Image.LANCZOS).save(os.path.join(OUT, f"{prefix}_{view}.png"))
    sheet_repair()
    print("wrote", OUT)


if __name__ == "__main__":
    main()
