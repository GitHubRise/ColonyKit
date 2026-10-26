"""Generate 128 px four-direction textures for the five Mk robot variants."""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "pawns")
os.makedirs(OUT, exist_ok=True)

S = 4
BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (25, 32, 40, 255)
DARK = (44, 54, 64, 255)
DARK2 = (33, 41, 50, 255)
STEEL = (136, 150, 162, 255)
STEEL_LIGHT = (197, 208, 215, 255)
STEEL_MID = (163, 176, 187, 255)
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
    return ImageFont.truetype(FONT_B if bold else FONT, size)


F_TITLE = font(34, True)
F_SUB = font(14)
F_LABEL = font(16, True)
F_SMALL = font(11)
F_TINY = font(9)


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


def text(d, xy, value, fill, size=8, bold=True):
    d.text((P(xy[0]), P(xy[1])), value, font=font(size * S, bold), fill=fill)


TIERS = {
    "MkI": dict(shell=STEEL_LIGHT, shell2=STEEL_MID, accent=(62, 126, 166, 255), glow=CYAN,
                head="round", body=(30, 44, 98, 104), tread=30),
    "MkII": dict(shell=(218, 228, 220, 255), shell2=(172, 194, 178, 255), accent=(83, 145, 104, 255), glow=GREEN,
                 head="box", body=(28, 42, 100, 104), tread=32),
    "MkIII": dict(shell=(232, 184, 76, 255), shell2=(164, 129, 62, 255), accent=(52, 60, 68, 255), glow=AMBER_LIGHT,
                  head="box", body=(24, 40, 104, 106), tread=38),
    "MkIV": dict(shell=(226, 242, 242, 255), shell2=(168, 207, 211, 255), accent=(44, 139, 155, 255), glow=CYAN,
                 head="bubble", body=(34, 46, 94, 102), tread=26),
    "MkV": dict(shell=(245, 238, 216, 255), shell2=(203, 185, 146, 255), accent=(198, 154, 58, 255), glow=AMBER_LIGHT,
                head="round", body=(26, 40, 102, 106), tread=36),
}


def antenna(d, cx, tier):
    line(d, [(cx, 18), (cx, 8)], OUTLINE, 2)
    if tier == "MkV":
        polygon(d, [(cx - 10, 9), (cx - 6, 2), (cx - 2, 8), (cx + 2, 0), (cx + 6, 8), (cx + 10, 2), (cx + 10, 10)],
                AMBER, OUTLINE)
    else:
        ellipse(d, (cx - 4, 4, cx + 4, 12), AMBER if tier != "MkIV" else CYAN, OUTLINE, 1)
        ellipse(d, (cx - 2, 6, cx + 2, 10), AMBER_LIGHT if tier != "MkIV" else (230, 255, 255, 255), None)


def treads(d, tier):
    p = TIERS[tier]
    tw = p["tread"]
    for x in (20, 108 - tw):
        rr(d, (x, 92, x + tw, 114), 8, TREAD, OUTLINE, 1)
        for i in range(3):
            d.rectangle((P(x + 6 + i * 8), P(98), P(x + 10 + i * 8), P(108)), fill=(82, 91, 102, 255))
        d.rectangle((P(x + 3), P(102), P(x + 8), P(106)), fill=AMBER, outline=OUTLINE, width=P(1))


def body(d, tier):
    p = TIERS[tier]
    x1, y1, x2, y2 = p["body"]
    rr(d, (x1, y1, x2, y2), 14, p["shell"], OUTLINE, 2)
    rr(d, (x1 + 6, y1 + 7, x2 - 6, y2 - 6), 10, p["shell2"], OUTLINE, 1)


def arms(d, tier):
    p = TIERS[tier]
    x1, _, x2, _ = p["body"]
    # left
    rr(d, (x1 - 13, 58, x1 - 1, 94), 6, p["shell"], OUTLINE, 2)
    ellipse(d, (x1 - 14, 90, x1, 102), p["shell2"], OUTLINE, 1)
    # right
    rr(d, (x2 + 1, 58, x2 + 13, 94), 6, p["shell"], OUTLINE, 2)
    ellipse(d, (x2, 90, x2 + 14, 102), p["shell2"], OUTLINE, 1)


def head_south(d, tier):
    p = TIERS[tier]
    h = p["head"]
    if h == "box":
        rr(d, (38, 14, 90, 56), 8, p["shell"], OUTLINE, 2)
        rr(d, (44, 20, 84, 50), 5, p["shell2"], OUTLINE, 1)
        rr(d, (46, 26, 82, 44), 4, (20, 28, 37, 255), OUTLINE, 1)
        d.rectangle((P(54), P(32), P(74), P(37)), fill=p["glow"])
        d.rectangle((P(58), P(40), P(70), P(43)), fill=STEEL_MID)
    elif h == "bubble":
        ellipse(d, (38, 12, 90, 60), p["shell"], OUTLINE, 2)
        ellipse(d, (44, 18, 84, 54), p["shell2"], OUTLINE, 1)
        ellipse(d, (48, 24, 80, 46), (*CYAN[:3], 120), OUTLINE, 1)
        ellipse(d, (54, 29, 64, 39), p["glow"])
        ellipse(d, (66, 29, 76, 39), p["glow"])
    else:
        ellipse(d, (36, 12, 92, 60), p["shell"], OUTLINE, 2)
        ellipse(d, (43, 19, 85, 54), p["shell2"], OUTLINE, 1)
        ellipse(d, (46, 24, 82, 46), (20, 28, 37, 255), OUTLINE, 1)
        ellipse(d, (54, 28, 66, 41), p["glow"])
        ellipse(d, (70, 28, 78, 38), (*p["glow"][:3], 180), None)
    # side sensor lights
    for x in (35, 91):
        ellipse(d, (x - 3, 27, x + 3, 33), AMBER if tier != "MkIV" else CYAN, OUTLINE, 1)
    antenna(d, 64, tier)


def head_north(d, tier):
    p = TIERS[tier]
    if p["head"] == "box":
        rr(d, (38, 14, 90, 56), 8, p["shell"], OUTLINE, 2)
        rr(d, (44, 20, 84, 50), 5, p["shell2"], OUTLINE, 1)
    else:
        ellipse(d, (38, 12, 90, 60), p["shell"], OUTLINE, 2)
        ellipse(d, (44, 18, 84, 54), p["shell2"], OUTLINE, 1)
    rr(d, (48, 25, 80, 42), 4, (20, 28, 37, 255), OUTLINE, 1)
    for x in (54, 61, 68, 75):
        d.rectangle((P(x), P(30), P(x + 3), P(37)), fill=STEEL_MID)
    # rear marker
    for x in (36, 92):
        ellipse(d, (x - 3, 30, x + 3, 36), AMBER, OUTLINE, 1)
    antenna(d, 64, tier)


def head_east(d, tier):
    p = TIERS[tier]
    if p["head"] == "box":
        rr(d, (40, 14, 94, 56), 8, p["shell"], OUTLINE, 2)
        rr(d, (46, 20, 88, 50), 5, p["shell2"], OUTLINE, 1)
    else:
        ellipse(d, (40, 12, 94, 60), p["shell"], OUTLINE, 2)
        ellipse(d, (46, 18, 88, 54), p["shell2"], OUTLINE, 1)
    # side visor facing right
    rr(d, (72, 25, 90, 43), 4, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(78), P(31), P(88), P(37)), fill=p["glow"])
    for x in (48, 56):
        d.rectangle((P(x), P(28), P(x + 3), P(40)), fill=STEEL_MID)
    antenna(d, 72, tier)


def chest(d, tier):
    p = TIERS[tier]
    if tier == "MkIII":
        hazard = True
    else:
        hazard = False
    rr(d, (42, 60, 86, 88), 6, p["accent"], OUTLINE, 1)
    if tier == "MkI":
        for i, col in enumerate((GREEN, AMBER, ORANGE)):
            ellipse(d, (50 + i * 10, 70, 56 + i * 10, 76), col, OUTLINE, 1)
    elif tier == "MkII":
        for y in (66, 74, 82):
            d.rectangle((P(50), P(y), P(78), P(y + 3)), fill=p["shell"], outline=OUTLINE, width=P(1))
    elif tier == "MkIII":
        # hazard strip
        for i in range(4):
            polygon(d, [(48 + i * 8, 82), (52 + i * 8, 82), (49 + i * 8, 72), (45 + i * 8, 72)], AMBER)
        d.rectangle((P(48), P(62), P(80), P(68)), fill=DARK2, outline=OUTLINE, width=P(1))
    elif tier == "MkIV":
        for y in (66, 73, 80):
            d.arc((P(48), P(y - 4), P(80), P(y + 8)), 200, 340, fill=CYAN, width=P(1))
    else:
        polygon(d, [(64, 64), (78, 74), (64, 84), (50, 74)], AMBER, OUTLINE)
        polygon(d, [(64, 68), (72, 74), (64, 80), (56, 74)], STEEL_LIGHT, OUTLINE)


def tool(d, tier, facing="south"):
    p = TIERS[tier]
    if facing == "south":
        if tier == "MkI":
            line(d, [(103, 74), (118, 106)], OUTLINE, 2)
            line(d, [(103, 74), (118, 106)], (120, 92, 65, 255), 1)
            polygon(d, [(114, 103), (124, 103), (124, 113), (112, 113)], AMBER, OUTLINE)
        elif tier == "MkII":
            rr(d, (98, 58, 110, 72), 3, p["accent"], OUTLINE, 1)
            d.rectangle((P(101), P(62), P(107), P(66)), fill=GREEN)
        elif tier == "MkIII":
            line(d, [(101, 78), (118, 86)], OUTLINE, 4)
            line(d, [(101, 78), (118, 86)], AMBER, 2)
            polygon(d, [(116, 82), (124, 85), (122, 92), (114, 89)], AMBER, OUTLINE)
        elif tier == "MkIV":
            line(d, [(102, 62), (118, 54)], OUTLINE, 2)
            ellipse(d, (114, 48, 124, 58), CYAN_DIM, OUTLINE, 1)
        else:
            rr(d, (96, 58, 112, 72), 3, DARK, OUTLINE, 1)
            line(d, [(112, 65), (124, 65)], OUTLINE, 4)
            line(d, [(112, 65), (124, 65)], CYAN, 2)
    elif facing == "east":
        if tier == "MkI":
            line(d, [(99, 78), (116, 102)], OUTLINE, 2)
            line(d, [(99, 78), (116, 102)], (120, 92, 65, 255), 1)
            polygon(d, [(112, 100), (122, 100), (122, 110), (110, 110)], AMBER, OUTLINE)
        elif tier == "MkII":
            rr(d, (94, 60, 106, 76), 3, p["accent"], OUTLINE, 1)
        elif tier == "MkIII":
            line(d, [(96, 76), (114, 84)], OUTLINE, 4)
            line(d, [(96, 76), (114, 84)], AMBER, 2)
        elif tier == "MkIV":
            line(d, [(100, 62), (116, 56)], OUTLINE, 2)
            ellipse(d, (112, 50, 122, 60), CYAN_DIM, OUTLINE, 1)
        else:
            rr(d, (94, 60, 108, 74), 3, DARK, OUTLINE, 1)
            line(d, [(108, 67), (120, 67)], OUTLINE, 4)
            line(d, [(108, 67), (120, 67)], CYAN, 2)


def robot_south(tier):
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ellipse(d, (20, 114, 108, 124), (0, 0, 0, 50))
    treads(d, tier)
    body(d, tier)
    arms(d, tier)
    chest(d, tier)
    head_south(d, tier)
    tool(d, tier, "south")
    return im


def robot_north(tier):
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ellipse(d, (20, 114, 108, 124), (0, 0, 0, 50))
    treads(d, tier)
    body(d, tier)
    arms(d, tier)
    # back panel
    p = TIERS[tier]
    rr(d, (42, 60, 86, 94), 5, p["accent"], OUTLINE, 1)
    for y in (66, 74, 82):
        d.rectangle((P(50), P(y), P(78), P(y + 4)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # rear power cell
    rr(d, (50, 96, 78, 106), 3, DARK2, OUTLINE, 1)
    head_north(d, tier)
    return im


def robot_east(tier):
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ellipse(d, (22, 114, 106, 124), (0, 0, 0, 50))
    p = TIERS[tier]
    # single tread
    rr(d, (24, 92, 104, 114), 8, TREAD, OUTLINE, 1)
    for x in range(32, 98, 8):
        d.rectangle((P(x), P(98), P(x + 4), P(108)), fill=(82, 91, 102, 255))
    # side body
    rr(d, (38, 46, 92, 102), 14, p["shell"], OUTLINE, 2)
    rr(d, (44, 53, 86, 96), 10, p["shell2"], OUTLINE, 1)
    # side panel
    rr(d, (48, 62, 78, 90), 5, p["accent"], OUTLINE, 1)
    for y in (68, 75, 82):
        d.rectangle((P(54), P(y), P(72), P(y + 3)), fill=p["shell"], outline=OUTLINE, width=P(1))
    # arm
    rr(d, (88, 60, 101, 94), 5, p["shell"], OUTLINE, 1)
    ellipse(d, (88, 90, 102, 102), p["shell2"], OUTLINE, 1)
    # head side
    head_east(d, tier)
    tool(d, tier, "east")
    return im


def main():
    tiers = ["MkI", "MkII", "MkIII", "MkIV", "MkV"]
    for tier in tiers:
        robot_south(tier).resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, f"{tier}_south.png"))
        robot_north(tier).resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, f"{tier}_north.png"))
        east = robot_east(tier)
        east.resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, f"{tier}_east.png"))
        ImageOps.mirror(east.resize((128, 128), Image.LANCZOS)).save(os.path.join(OUT, f"{tier}_west.png"))

    # contact sheet
    sheet = Image.new("RGBA", (1500, 1080), BG)
    d = ImageDraw.Draw(sheet)
    for x in range(0, 1500, 40):
        d.line((x, 0, x, 1080), fill=(27, 40, 47, 70), width=1)
    d.text((26, 18), "CK ROBOTICS · MK I–V FOUR-DIRECTION SPRITES", font=F_TITLE, fill=TEXT)
    d.text((28, 58), "128×128 · south / north / east / west · candidate set", font=F_SUB, fill=MUTED)
    labels = ["南 / SOUTH", "北 / NORTH", "东 / EAST", "西 / WEST"]
    for i, name in enumerate(tiers):
        y = 100 + i * 190
        d.text((18, y + 70), name, font=F_LABEL, fill=AMBER)
        for j, view in enumerate(("south", "north", "east", "west")):
            x = 150 + j * 330
            img = Image.open(os.path.join(OUT, f"{name}_{view}.png")).convert("RGBA").resize((170, 170), Image.LANCZOS)
            sheet.alpha_composite(img, (x, y))
            d.rectangle((x, y, x + 170, y + 170), outline=LINE, width=1)
            d.text((x, y + 176), labels[j], font=F_SMALL, fill=MUTED)
    sheet.convert("RGB").save(os.path.join(OUT, "CK_RobotVariants_views.png"), quality=95)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
