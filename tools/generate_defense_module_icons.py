"""Generate 128 px inventory icons for the four robot defense modules."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "items")
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
STEEL = (134, 148, 160, 255)
STEEL_LIGHT = (197, 208, 215, 255)
STEEL_MID = (160, 173, 184, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


F_TITLE = font(34, True)
F_SUB = font(14)
F_LABEL = font(16, True)
F_SMALL = font(11)


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


def icon() -> Image.Image:
    return Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))


def arc_welder() -> Image.Image:
    im = icon()
    d = ImageDraw.Draw(im)
    ellipse(d, (18, 108, 112, 122), (0, 0, 0, 45))
    # torch body diagonal
    line(d, [(28, 104), (82, 48)], OUTLINE, 14)
    line(d, [(28, 104), (82, 48)], STEEL_MID, 8)
    rr(d, (16, 92, 48, 118), 5, DARK, OUTLINE, 2)
    d.rectangle((P(22), P(99), P(42), P(105)), fill=AMBER)
    # nozzle
    polygon(d, [(76, 40), (92, 40), (98, 58), (74, 58)], STEEL_LIGHT, OUTLINE)
    polygon(d, [(82, 34), (94, 34), (98, 44), (78, 44)], AMBER, OUTLINE)
    # arc
    line(d, [(92, 58), (106, 70), (94, 78), (110, 94)], CYAN, 3)
    line(d, [(92, 58), (106, 70), (96, 84)], (235, 255, 255, 255), 2)
    for x, y in ((104, 68), (100, 86), (112, 92)):
        ellipse(d, (x - 3, y - 3, x + 3, y + 3), AMBER_LIGHT, None)
    return im


def shock_baton() -> Image.Image:
    im = icon()
    d = ImageDraw.Draw(im)
    ellipse(d, (18, 108, 112, 122), (0, 0, 0, 45))
    line(d, [(24, 102), (100, 26)], OUTLINE, 16)
    line(d, [(24, 102), (100, 26)], DARK, 9)
    # grip
    line(d, [(34, 92), (58, 68)], AMBER, 8)
    for i in range(4):
        line(d, [(38 + i * 6, 88 - i * 6), (42 + i * 6, 94 - i * 6)], OUTLINE, 2)
    # coil / tip
    ellipse(d, (86, 12, 116, 44), CYAN_DIM, OUTLINE, 2)
    ellipse(d, (92, 18, 110, 38), CYAN, None)
    for i in range(3):
        d.arc((P(84 - i * 4), P(10 + i * 4), P(118 + i * 4), P(46 - i * 4)), 200, 340,
              fill=CYAN, width=P(1))
    # sparks
    for x, y in ((116, 28), (112, 8), (104, 48)):
        line(d, [(104, 28), (x, y)], CYAN, 2)
    return im


def light_armor() -> Image.Image:
    im = icon()
    d = ImageDraw.Draw(im)
    ellipse(d, (20, 108, 108, 122), (0, 0, 0, 42))
    # chest plate
    polygon(d, [(64, 14), (110, 28), (104, 72), (64, 112), (24, 72), (18, 28)], STEEL, OUTLINE)
    polygon(d, [(64, 26), (98, 36), (92, 68), (64, 96), (36, 68), (30, 36)], STEEL_MID, OUTLINE)
    # center energy core
    polygon(d, [(64, 42), (80, 58), (64, 78), (48, 58)], CYAN_DIM, OUTLINE)
    polygon(d, [(64, 48), (73, 58), (64, 70), (55, 58)], CYAN, None)
    # straps
    for side in (-1, 1):
        line(d, [(64 + side * 40, 32), (64 + side * 58, 58), (64 + side * 38, 90)], OUTLINE, 6)
        line(d, [(64 + side * 40, 32), (64 + side * 58, 58), (64 + side * 38, 90)], DARK, 3)
    # rivets
    for x, y in ((36, 40), (92, 40), (40, 76), (88, 76)):
        ellipse(d, (x - 3, y - 3, x + 3, y + 3), STEEL_LIGHT, OUTLINE, 1)
    return im


def arc_emitter() -> Image.Image:
    im = icon()
    d = ImageDraw.Draw(im)
    ellipse(d, (10, 104, 118, 120), (0, 0, 0, 45))
    # stock / receiver
    polygon(d, [(12, 76), (34, 66), (40, 88), (20, 98)], DARK, OUTLINE)
    rr(d, (34, 58, 92, 92), 4, STEEL, OUTLINE, 2)
    rr(d, (42, 64, 86, 84), 2, DARK, OUTLINE, 1)
    # grip / magazine
    polygon(d, [(52, 90), (66, 90), (72, 116), (58, 116)], DARK, OUTLINE)
    polygon(d, [(66, 90), (80, 90), (78, 112), (62, 112)], DARK2, OUTLINE)
    d.rectangle((P(68), P(98), P(76), P(102)), fill=CYAN)
    # top sight
    rr(d, (52, 48, 82, 58), 2, DARK2, OUTLINE, 1)
    d.rectangle((P(66), P(52), P(72), P(56)), fill=CYAN)
    # barrel / coils
    rr(d, (88, 62, 116, 84), 3, STEEL_MID, OUTLINE, 2)
    for i in range(3):
        ellipse(d, (96 + i * 7, 60, 106 + i * 7, 86), None, CYAN, 2)
    line(d, [(114, 72), (124, 72)], OUTLINE, 6)
    line(d, [(114, 72), (124, 72)], STEEL_LIGHT, 3)
    ellipse(d, (120, 66, 128, 78), (*CYAN[:3], 90))
    # muzzle sparks
    line(d, [(124, 72), (122, 60)], CYAN, 2)
    line(d, [(124, 72), (126, 84)], CYAN, 2)
    return im


def main():
    items = [("CK_ArcWelder_icon", arc_welder, "电弧焊枪 / ARC WELDER"),
             ("CK_ShockBaton_icon", shock_baton, "电击棒 / SHOCK BATON"),
             ("CK_LightArmor_icon", light_armor, "轻甲 / LIGHT ARMOR"),
             ("CK_ArcEmitter_icon", arc_emitter, "电弧发射器 / ARC EMITTER")]
    for name, fn, _ in items:
        fn().resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, f"{name}.png"))

    sheet = Image.new("RGBA", (1500, 420), BG)
    d = ImageDraw.Draw(sheet)
    for x in range(0, 1500, 40):
        d.line((x, 0, x, 420), fill=(27, 40, 47, 70), width=1)
    d.text((24, 16), "CK ROBOTICS · DEFENSE MODULE ICONS", font=F_TITLE, fill=TEXT)
    d.text((26, 54), "128×128 inventory icons · candidate set", font=F_SUB, fill=MUTED)
    for i, (name, _, label) in enumerate(items):
        x = 30 + i * 360
        img = Image.open(os.path.join(OUT, f"{name}.png")).convert("RGBA").resize((220, 220), Image.LANCZOS)
        sheet.alpha_composite(img, (x, 100))
        d.rectangle((x, 100, x + 220, 320), outline=LINE, width=1)
        d.text((x, 330), label, font=F_LABEL, fill=TEXT)
        d.text((x, 354), f"{name}.png", font=F_SMALL, fill=MUTED)
    sheet.convert("RGB").save(os.path.join(OUT, "CK_DefenseModules_icons.png"), quality=95)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
