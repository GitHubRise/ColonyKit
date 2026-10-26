"""Generate directional texture candidates for the 2x2 workshop and 2x6 unmanned factory."""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "buildings")
os.makedirs(OUT, exist_ok=True)

S = 1
BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (25, 32, 40, 255)
DARK = (44, 54, 64, 255)
DARK2 = (34, 42, 51, 255)
WALL = (57, 68, 80, 255)
WALL_DARK = (39, 48, 58, 255)
STEEL = (126, 140, 152, 255)
STEEL_LIGHT = (196, 207, 214, 255)
STEEL_MID = (158, 171, 182, 255)
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
    return ImageFont.truetype(FONT_B if bold else FONT, max(8, int(size)))


F_TITLE = font(34, True)
F_SUB = font(14)
F_LABEL = font(17, True)
F_SMALL = font(11)
F_TINY = font(9)


def P(v):
    return int(round(v * S))


def canvas(w, h):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))


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


def text(d, xy, value, fill, size=8, bold=True):
    d.text((P(xy[0]), P(xy[1])), value, font=font(size * S, bold), fill=fill)


def hazard(d, x1, y1, x2, y2, n=5):
    d.rectangle((P(x1), P(y1), P(x2), P(y2)), fill=DARK2, outline=OUTLINE, width=P(1))
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


def shadow(d, x1, y1, x2, y2):
    ellipse(d, (x1, y1, x2, y2), (0, 0, 0, 52))


def gear(d, cx, cy, r, fill=AMBER, inner=DARK2):
    for i in range(8):
        a = i * 3.14159 / 4
        x1 = cx + __import__("math").cos(a) * r
        y1 = cy + __import__("math").sin(a) * r
        x2 = cx + __import__("math").cos(a) * (r + 7)
        y2 = cy + __import__("math").sin(a) * (r + 7)
        line(d, [(x1, y1), (x2, y2)], fill, 5)
    ellipse(d, (cx - r, cy - r, cx + r, cy + r), fill, OUTLINE, 1)
    ellipse(d, (cx - r * 0.42, cy - r * 0.42, cx + r * 0.42, cy + r * 0.42), inner, OUTLINE, 1)


def draw_workshop(view: str) -> Image.Image:
    w = h = 256
    im = canvas(w, h)
    d = ImageDraw.Draw(im)
    shadow(d, 16, 210, 240, 232)

    if view == "top":
        # 2x2 roof plan
        rr(d, (22, 22, 234, 234), 8, DARK, OUTLINE, 2)
        rr(d, (32, 32, 224, 224), 5, STEEL_MID, OUTLINE, 1)
        rr(d, (44, 44, 212, 212), 4, PANEL, OUTLINE, 1)
        d.line((128, 32, 128, 224), fill=LINE, width=2)
        d.line((32, 128, 224, 128), fill=LINE, width=2)
        gear(d, 90, 88, 22)
        rr(d, (176, 48, 198, 92), 3, STEEL, OUTLINE, 1)
        d.rectangle((174, 42, 200, 52), fill=AMBER, outline=OUTLINE, width=1)
        for x in (54, 92, 176):
            d.rectangle((x, 160, x + 18, 172), fill=CYAN_DIM, outline=OUTLINE, width=1)
        d.rectangle((88, 210, 168, 224), fill=AMBER, outline=OUTLINE, width=1)
        hazard(d, 92, 202, 164, 210, 5)
        return im

    if view == "south":
        # wall + gable roof
        rr(d, (20, 96, 236, 204), 5, WALL, OUTLINE, 2)
        polygon(d, [(10, 98), (246, 98), (214, 60), (42, 60)], STEEL, OUTLINE)
        polygon(d, [(32, 94), (224, 94), (204, 72), (52, 72)], STEEL_MID, OUTLINE)
        # roof skylight, drawn inside the roof plane
        polygon(d, [(86, 80), (156, 80), (166, 90), (96, 90)], CYAN_DIM, OUTLINE)
        # chimney
        rr(d, (198, 42, 222, 94), 3, STEEL_MID, OUTLINE, 1)
        d.rectangle((195, 36, 225, 47), fill=AMBER, outline=OUTLINE, width=1)
        # wall pillars and windows
        for x in (30, 208):
            d.rectangle((x, 108, x + 14, 194), fill=STEEL_MID, outline=OUTLINE, width=1)
        for x in (44, 190):
            rr(d, (x, 124, x + 22, 146), 2, (24, 83, 96, 255), OUTLINE, 1)
            d.rectangle((x + 3, 128, x + 19, 132), fill=CYAN)
        # gear sign above the door
        gear(d, 128, 118, 23)
        # roll-up door with frame and visible slats
        rr(d, (58, 142, 198, 200), 3, WALL_DARK, STEEL_LIGHT, 2)
        rr(d, (66, 149, 190, 196), 2, (30, 38, 47, 255), OUTLINE, 1)
        for y in range(154, 194, 8):
            d.rectangle((70, y, 186, y + 4), fill=STEEL_MID)
        d.rectangle((70, 149, 186, 154), fill=CYAN_DIM, outline=OUTLINE, width=1)
        hazard(d, 62, 198, 194, 206, 5)
        d.rectangle((22, 188, 34, 200), fill=ORANGE, outline=OUTLINE, width=1)
        # door bolts
        for x, y in ((61, 146), (195, 146), (61, 196), (195, 196)):
            ellipse(d, (x - 3, y - 3, x + 3, y + 3), STEEL_LIGHT, OUTLINE, 1)
        return im

    if view == "north":
        rr(d, (20, 96, 236, 204), 5, WALL, OUTLINE, 2)
        polygon(d, [(10, 98), (246, 98), (214, 60), (42, 60)], STEEL, OUTLINE)
        polygon(d, [(32, 94), (224, 94), (204, 72), (52, 72)], STEEL_MID, OUTLINE)
        rr(d, (198, 42, 222, 94), 3, STEEL_MID, OUTLINE, 1)
        d.rectangle((195, 36, 225, 47), fill=AMBER, outline=OUTLINE, width=1)
        # rear service door
        rr(d, (94, 140, 162, 198), 4, WALL_DARK, OUTLINE, 1)
        for y in (150, 162, 174, 186):
            d.rectangle((104, y, 152, y + 4), fill=STEEL_MID)
        # vents / pipes
        for x in (42, 62, 82):
            d.rectangle((x, 116, x + 8, 138), fill=STEEL_MID, outline=OUTLINE, width=1)
        line(d, [(196, 126), (196, 164), (214, 164)], OUTLINE, 4)
        hazard(d, 98, 198, 158, 206, 4)
        return im

    # east / west side
    if view == "west":
        return ImageOps.mirror(draw_workshop("east"))
    rr(d, (24, 100, 232, 204), 5, WALL, OUTLINE, 2)
    polygon(d, [(14, 102), (242, 102), (210, 64), (46, 64)], STEEL, OUTLINE)
    polygon(d, [(36, 98), (220, 98), (200, 76), (56, 76)], STEEL_MID, OUTLINE)
    # side roof vent
    polygon(d, [(66, 84), (150, 84), (138, 92), (78, 92)], CYAN_DIM, OUTLINE)
    rr(d, (196, 44, 220, 94), 3, STEEL_MID, OUTLINE, 1)
    d.rectangle((193, 38, 223, 49), fill=AMBER, outline=OUTLINE, width=1)
    # side door / windows
    rr(d, (86, 142, 146, 200), 4, WALL_DARK, OUTLINE, 1)
    for y in (152, 164, 176, 188):
        d.rectangle((96, y, 136, y + 4), fill=STEEL_MID)
    for x in (40, 166):
        rr(d, (x, 118, x + 22, 136), 2, (24, 83, 96, 255), OUTLINE, 1)
        d.rectangle((x + 3, 122, x + 19, 126), fill=CYAN)
    # gear shadow / side sign
    gear(d, 174, 128, 15)
    hazard(d, 90, 200, 142, 208, 4)
    return im


def draw_unmanned(view: str) -> Image.Image:
    if view in ("south", "north"):
        w = h = 256
        im = canvas(w, h)
        d = ImageDraw.Draw(im)
        shadow(d, 18, 210, 238, 232)
        rr(d, (24, 94, 232, 204), 5, WALL, OUTLINE, 2)
        polygon(d, [(14, 96), (242, 96), (214, 58), (42, 58)], STEEL, OUTLINE)
        polygon(d, [(34, 92), (222, 92), (202, 70), (54, 70)], DARK, OUTLINE)
        # roof beacons
        for x in (50, 206):
            ellipse(d, (x - 8, 47, x + 8, 63), AMBER, OUTLINE, 1)
            ellipse(d, (x - 3, 52, x + 3, 58), AMBER_LIGHT, None)
        # facade strips
        for y in (106, 118):
            d.rectangle((34, y, 222, y + 5), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
        if view == "south":
            # main door with a bright frame, slats and cyan guards
            rr(d, (74, 126, 182, 202), 4, WALL_DARK, STEEL_LIGHT, 2)
            rr(d, (82, 134, 174, 196), 3, (30, 38, 47, 255), OUTLINE, 1)
            for y in range(140, 192, 8):
                d.rectangle((88, y, 168, y + 4), fill=STEEL_MID)
            d.rectangle((88, 134, 168, 140), fill=CYAN_DIM, outline=OUTLINE, width=1)
            d.rectangle((148, 134, 174, 196), fill=DARK2, outline=OUTLINE, width=1)
            text(d, (103, 177), "24H", CYAN, 8, True)
            # control box
            rr(d, (190, 128, 224, 184), 3, STEEL, OUTLINE, 1)
            d.rectangle((196, 136, 218, 144), fill=CYAN)
            d.rectangle((196, 152, 208, 158), fill=GREEN)
            d.rectangle((196, 168, 218, 176), fill=AMBER)
        else:
            # rear service doors / pipes
            rr(d, (74, 130, 150, 202), 4, WALL_DARK, OUTLINE, 1)
            for y in (142, 156, 170, 184):
                d.rectangle((84, y, 140, y + 4), fill=STEEL_MID)
            rr(d, (168, 116, 202, 202), 3, STEEL_MID, OUTLINE, 1)
            for y in (126, 142, 158, 174, 190):
                d.rectangle((176, y, 194, y + 4), fill=DARK2)
            line(d, [(60, 116), (60, 164), (42, 164)], OUTLINE, 4)
        hazard(d, 80, 202, 176, 210, 5)
        return im

    if view == "top":
        w, h = 256, 768
        im = canvas(w, h)
        d = ImageDraw.Draw(im)
        shadow(d, 18, 730, 238, 756)
        # 2x6 footprint
        rr(d, (20, 24, 236, 738), 8, DARK, OUTLINE, 2)
        rr(d, (32, 36, 224, 726), 5, STEEL_MID, OUTLINE, 1)
        rr(d, (44, 48, 212, 714), 4, DARK2, OUTLINE, 1)
        # footprint grid
        d.line((128, 48, 128, 714), fill=LINE, width=2)
        for i in range(1, 6):
            d.line((44, 48 + i * 111, 212, 48 + i * 111), fill=LINE, width=1)
        # six roof modules
        for i in range(6):
            y = 62 + i * 108
            d.rectangle((58, y, 198, y + 22), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
            d.rectangle((70, y + 7, 186, y + 12), fill=CYAN_DIM)
            d.rectangle((58, y + 32, 82, y + 42), fill=STEEL_MID, outline=OUTLINE, width=1)
            d.rectangle((174, y + 32, 198, y + 42), fill=STEEL_MID, outline=OUTLINE, width=1)
        # central core / arms
        ellipse(d, (96, 318, 160, 414), (25, 58, 70, 255), OUTLINE, 2)
        ellipse(d, (112, 342, 144, 390), CYAN, None)
        for side in (-1, 1):
            line(d, [(128 + side * 66, 402), (128 + side * 38, 360), (128 + side * 18, 336)], OUTLINE, 5)
            line(d, [(128 + side * 66, 402), (128 + side * 38, 360), (128 + side * 18, 336)], STEEL_LIGHT, 2)
        # south control room
        rr(d, (74, 650, 182, 704), 4, PANEL, OUTLINE, 1)
        d.rectangle((86, 662, 142, 670), fill=CYAN)
        d.rectangle((86, 682, 120, 688), fill=GREEN)
        d.rectangle((154, 662, 170, 688), fill=AMBER, outline=OUTLINE, width=1)
        hazard(d, 80, 722, 176, 732, 5)
        return im

    # east / west long side
    if view == "west":
        return ImageOps.mirror(draw_unmanned("east"))
    w, h = 768, 256
    im = canvas(w, h)
    d = ImageDraw.Draw(im)
    shadow(d, 16, 212, 752, 236)
    # long wall
    rr(d, (20, 96, 748, 206), 5, WALL, OUTLINE, 2)
    polygon(d, [(10, 98), (758, 98), (730, 60), (38, 60)], STEEL, OUTLINE)
    polygon(d, [(30, 94), (738, 94), (718, 72), (50, 72)], DARK, OUTLINE)
    # roof beacons
    for x in (70, 382, 694):
        ellipse(d, (x - 7, 50, x + 7, 64), AMBER, OUTLINE, 1)
        ellipse(d, (x - 3, 54, x + 3, 60), AMBER_LIGHT, None)
    # six roof / wall bays
    for i in range(6):
        x = 36 + i * 118
        d.rectangle((x + 10, 104, x + 92, 112), fill=(24, 83, 96, 255), outline=OUTLINE, width=1)
        d.rectangle((x + 10, 120, x + 72, 126), fill=CYAN_DIM, outline=OUTLINE, width=1)
        # service door
        rr(d, (x + 22, 146, x + 76, 200), 3, WALL_DARK, OUTLINE, 1)
        for y in (156, 168, 180):
            d.rectangle((x + 30, y, x + 68, y + 4), fill=STEEL_MID)
        d.rectangle((x + 30, 146, x + 68, 151), fill=CYAN_DIM, outline=OUTLINE, width=1)
        # exhaust
        d.rectangle((x + 96, 138, x + 108, 176), fill=STEEL_MID, outline=OUTLINE, width=1)
        hazard(d, x + 26, 198, x + 70, 206, 3)
    # south-end control box
    rr(d, (690, 130, 742, 198), 4, STEEL, OUTLINE, 1)
    d.rectangle((700, 142, 732, 150), fill=CYAN)
    d.rectangle((700, 162, 720, 168), fill=GREEN)
    d.rectangle((700, 180, 732, 186), fill=AMBER)
    return im


def save_sprite(image: Image.Image, name: str, w: int, h: int):
    image.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name))


def compose_workshop():
    views = [("south", "南 / FRONT"), ("north", "北 / BACK"), ("east", "东 / RIGHT"), ("west", "西 / LEFT"), ("top", "俯视 / TOP")]
    sheet = Image.new("RGBA", (1500, 500), BG)
    d = ImageDraw.Draw(sheet)
    for x in range(0, 1500, 40):
        d.line((x, 0, x, 500), fill=(27, 40, 47, 70), width=1)
    d.text((28, 20), "CK_WORKSHOPFACTORY · 2×2 DIRECTIONAL TEXTURES", font=F_TITLE, fill=TEXT)
    d.text((30, 62), "128 px/cell · south/north/east/west + top · candidate set", font=F_SUB, fill=MUTED)
    for i, (key, label) in enumerate(views):
        x = 30 + i * 290
        img = Image.open(os.path.join(OUT, f"CK_WorkshopFactory_{key}.png")).convert("RGBA").resize((250, 250), Image.LANCZOS)
        sheet.alpha_composite(img, (x, 120))
        d.rectangle((x, 120, x + 250, 370), outline=LINE, width=1)
        d.text((x, 385), label, font=F_LABEL, fill=TEXT)
        d.text((x, 410), f"CK_WorkshopFactory_{key}.png", font=F_SMALL, fill=MUTED)
    sheet.convert("RGB").save(os.path.join(OUT, "CK_WorkshopFactory_views.png"), quality=95)


def compose_unmanned():
    sheet = Image.new("RGBA", (1700, 1220), BG)
    d = ImageDraw.Draw(sheet)
    for x in range(0, 1700, 40):
        d.line((x, 0, x, 1220), fill=(27, 40, 47, 70), width=1)
    d.text((28, 20), "CK_UNMANNEDFACTORY · 2×6 DIRECTIONAL TEXTURES", font=F_TITLE, fill=TEXT)
    d.text((30, 62), "128 px/cell · south/north 2-wide · east/west 6-long · top 2×6", font=F_SUB, fill=MUTED)

    # front/back
    for i, key in enumerate(("south", "north")):
        img = Image.open(os.path.join(OUT, f"CK_UnmannedFactory_{key}.png")).convert("RGBA").resize((250, 250), Image.LANCZOS)
        x = 40 + i * 280
        sheet.alpha_composite(img, (x, 105))
        d.rectangle((x, 105, x + 250, 355), outline=LINE, width=1)
        d.text((x, 370), "南 / FRONT" if key == "south" else "北 / BACK", font=F_LABEL, fill=TEXT)
        d.text((x, 395), f"CK_UnmannedFactory_{key}.png", font=F_SMALL, fill=MUTED)

    # top
    top = Image.open(os.path.join(OUT, "CK_UnmannedFactory_top.png")).convert("RGBA").resize((190, 570), Image.LANCZOS)
    sheet.alpha_composite(top, (620, 105))
    d.rectangle((620, 105, 810, 675), outline=LINE, width=1)
    d.text((620, 690), "俯视 / TOP", font=F_LABEL, fill=TEXT)
    d.text((620, 715), "CK_UnmannedFactory_top.png", font=F_SMALL, fill=MUTED)

    # side views
    for i, key in enumerate(("east", "west")):
        img = Image.open(os.path.join(OUT, f"CK_UnmannedFactory_{key}.png")).convert("RGBA").resize((760, 253), Image.LANCZOS)
        y = 790 + i * 120
        sheet.alpha_composite(img, (40, y))
        d.rectangle((40, y, 800, y + 253), outline=LINE, width=1)
        d.text((820, y + 90), "东 / RIGHT" if key == "east" else "西 / LEFT", font=F_LABEL, fill=TEXT)
        d.text((820, y + 115), f"CK_UnmannedFactory_{key}.png", font=F_SMALL, fill=MUTED)
    sheet.convert("RGB").save(os.path.join(OUT, "CK_UnmannedFactory_views.png"), quality=95)


def main():
    for view in ("south", "north", "east", "west", "top"):
        img = draw_workshop(view)
        save_sprite(img, f"CK_WorkshopFactory_{view}.png", 256, 256)
    compose_workshop()

    for view in ("south", "north", "east", "west", "top"):
        img = draw_unmanned(view)
        if view in ("east", "west"):
            save_sprite(img, f"CK_UnmannedFactory_{view}.png", 768, 256)
        elif view == "top":
            save_sprite(img, f"CK_UnmannedFactory_{view}.png", 256, 768)
        else:
            save_sprite(img, f"CK_UnmannedFactory_{view}.png", 256, 256)
    compose_unmanned()
    print("wrote", OUT)


if __name__ == "__main__":
    main()
