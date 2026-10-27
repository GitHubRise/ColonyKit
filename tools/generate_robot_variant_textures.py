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


def bolt(d, x, y, r=1.5):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


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


def mkii_south():
    """Mk-II alternate style: low modular helmet, stacked plates, bolt-on shoulder pods."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (16, 114, 112, 124), (0, 0, 0, 50))
    # wide treads
    for x in (12, 78):
        rr(d, (x, 92, x + 38, 116), 8, TREAD, OUTLINE, 1)
        rr(d, (x - 2, 88, x + 40, 97), 4, STEEL, OUTLINE, 1)
        for i in range(3):
            d.rectangle((P(x + 9 + i * 9), P(99), P(x + 14 + i * 9), P(108)), fill=(82, 91, 102, 255))
        d.rectangle((P(x + 4), P(103), P(x + 10), P(107)), fill=AMBER, outline=OUTLINE, width=P(1))
    # lower base / hazard skirt
    rr(d, (26, 82, 102, 110), 6, DARK, OUTLINE, 1)
    for x in (34, 46, 58, 70, 82):
        d.rectangle((P(x), P(100), P(x + 8), P(107)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    hazard = [(36, 108), (44, 108), (40, 100), (32, 100)]
    for i in range(4):
        polygon(d, [(40 + i * 12, 108), (46 + i * 12, 108), (43 + i * 12, 100), (37 + i * 12, 100)], AMBER)
    # stacked modular torso
    rr(d, (30, 52, 98, 96), 8, p["shell"], OUTLINE, 2)
    rr(d, (36, 58, 92, 90), 5, p["shell2"], OUTLINE, 1)
    # plate seams
    for y in (64, 76):
        d.rectangle((P(38), P(y), P(90), P(y + 2)), fill=DARK2)
    # side rails with latches
    for x in (24, 94):
        rr(d, (x, 54, x + 10, 98), 3, DARK, OUTLINE, 1)
        for y in (62, 78):
            d.rectangle((P(x + 1), P(y), P(x + 9), P(y + 5)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # bolt-on shoulder pods
    for x in (14, 94):
        rr(d, (x, 58, x + 20, 82), 4, p["accent"], OUTLINE, 2)
        d.rectangle((P(x + 4), P(63), P(x + 16), P(68)), fill=GREEN)
        for bx, by in ((x + 4, 75), (x + 16, 75)):
            ellipse(d, (P(bx - 1), P(by - 1), P(bx + 1), P(by + 1)), AMBER_LIGHT, OUTLINE, 1)
    # arms below shoulder pods
    for x in (16, 98):
        rr(d, (x, 80, x + 14, 104), 4, p["shell"], OUTLINE, 1)
        ellipse(d, (x - 1, 100, x + 15, 110), p["shell2"], OUTLINE, 1)
    # chest module bay
    rr(d, (42, 60, 86, 86), 4, p["accent"], OUTLINE, 1)
    for i in range(3):
        rr(d, (47 + i * 12, 64, 57 + i * 12, 82), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(49 + i * 12), P(68), P(55 + i * 12), P(72)), fill=(GREEN, CYAN, AMBER)[i])
    d.rectangle((P(42), P(60), P(86), P(63)), fill=DARK2, outline=OUTLINE, width=P(1))
    # low wide helmet
    rr(d, (32, 14, 96, 54), 8, p["shell"], OUTLINE, 2)
    rr(d, (38, 20, 90, 48), 5, p["shell2"], OUTLINE, 1)
    rr(d, (38, 27, 90, 41), 4, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(44), P(31), P(84), P(36)), fill=GREEN)
    d.rectangle((P(50), P(36), P(78), P(39)), fill=STEEL_MID)
    # side sensor pods on helmet
    for x in (22, 94):
        rr(d, (x, 24, x + 12, 46), 3, DARK, OUTLINE, 1)
        d.rectangle((P(x + 3), P(29), P(x + 9), P(34)), fill=GREEN)
        ellipse(d, (P(x + 4), P(38), P(x + 8), P(42)), AMBER_LIGHT, None)
    # twin short antennae
    for x, lean in ((46, -4), (82, 4)):
        line(d, [(x, 14), (x + lean, 4)], OUTLINE, 2)
        ellipse(d, (x + lean - 3, 0, x + lean + 3, 6), GREEN, OUTLINE, 1)
    # small tool block on right arm
    rr(d, (106, 84, 118, 96), 2, DARK, OUTLINE, 1)
    d.rectangle((P(109), P(88), P(115), P(92)), fill=GREEN)
    return im


def mkii_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (16, 114, 112, 124), (0, 0, 0, 50))
    for x in (12, 78):
        rr(d, (x, 92, x + 38, 116), 8, TREAD, OUTLINE, 1)
        rr(d, (x - 2, 88, x + 40, 97), 4, STEEL, OUTLINE, 1)
        for i in range(3):
            d.rectangle((P(x + 9 + i * 9), P(99), P(x + 14 + i * 9), P(108)), fill=(82, 91, 102, 255))
    rr(d, (26, 82, 102, 110), 6, DARK, OUTLINE, 1)
    rr(d, (30, 52, 98, 96), 8, p["shell"], OUTLINE, 2)
    rr(d, (36, 58, 92, 90), 5, p["shell2"], OUTLINE, 1)
    # rear module rack
    for x in (44, 58, 72):
        rr(d, (x, 60, x + 10, 88), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(x + 2), P(64), P(x + 8), P(70)), fill=GREEN)
        d.rectangle((P(x + 2), P(78), P(x + 8), P(82)), fill=STEEL_MID)
    # rear cable / port
    d.rectangle((P(38), P(64), P(42), P(88)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    d.rectangle((P(88), P(64), P(92), P(88)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # shoulder pods
    for x in (14, 94):
        rr(d, (x, 58, x + 20, 82), 4, p["accent"], OUTLINE, 2)
        d.rectangle((P(x + 4), P(70), P(x + 16), P(75)), fill=STEEL_MID)
    for x in (16, 98):
        rr(d, (x, 80, x + 14, 104), 4, p["shell"], OUTLINE, 1)
    # low helmet rear vent
    rr(d, (32, 14, 96, 54), 8, p["shell"], OUTLINE, 2)
    rr(d, (38, 20, 90, 48), 5, p["shell2"], OUTLINE, 1)
    rr(d, (38, 25, 90, 43), 4, (20, 28, 37, 255), OUTLINE, 1)
    for x in (44, 53, 62, 71, 80):
        d.rectangle((P(x), P(30), P(x + 3), P(38)), fill=STEEL_MID)
    for x in (22, 94):
        rr(d, (x, 24, x + 12, 46), 3, DARK, OUTLINE, 1)
        d.rectangle((P(x + 3), P(31), P(x + 9), P(36)), fill=GREEN)
    for x, lean in ((46, -4), (82, 4)):
        line(d, [(x, 14), (x + lean, 4)], OUTLINE, 2)
        ellipse(d, (x + lean - 3, 0, x + lean + 3, 6), GREEN, OUTLINE, 1)
    return im


def mkii_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (20, 114, 108, 124), (0, 0, 0, 50))
    # one wide tread
    rr(d, (22, 92, 106, 116), 8, TREAD, OUTLINE, 1)
    rr(d, (20, 88, 108, 97), 4, STEEL, OUTLINE, 1)
    for x in range(32, 102, 9):
        d.rectangle((P(x), P(99), P(x + 5), P(108)), fill=(82, 91, 102, 255))
    # side body with stacked plates
    rr(d, (36, 50, 96, 100), 7, p["shell"], OUTLINE, 2)
    rr(d, (42, 56, 90, 94), 4, p["shell2"], OUTLINE, 1)
    for y in (64, 76):
        d.rectangle((P(44), P(y), P(88), P(y + 2)), fill=DARK2)
    # modular side bay
    rr(d, (48, 60, 74, 88), 3, p["accent"], OUTLINE, 1)
    for y in (66, 74, 82):
        d.rectangle((P(52), P(y), P(70), P(y + 3)), fill=GREEN)
    # shoulder pod
    rr(d, (78, 54, 98, 78), 3, p["accent"], OUTLINE, 1)
    d.rectangle((P(83), P(60), P(94), P(65)), fill=STEEL_MID)
    # arm
    rr(d, (90, 78, 104, 102), 4, p["shell"], OUTLINE, 1)
    ellipse(d, (90, 98, 106, 108), p["shell2"], OUTLINE, 1)
    # low helmet side
    rr(d, (38, 14, 96, 54), 8, p["shell"], OUTLINE, 2)
    rr(d, (44, 20, 90, 48), 5, p["shell2"], OUTLINE, 1)
    # side visor facing right
    rr(d, (70, 26, 90, 42), 4, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(76), P(31), P(87), P(36)), fill=GREEN)
    # side sensor pod
    rr(d, (92, 24, 104, 46), 3, DARK, OUTLINE, 1)
    d.rectangle((P(95), P(30), P(101), P(35)), fill=GREEN)
    # antenna
    for x, lean in ((54, -4), (84, 4)):
        line(d, [(x, 14), (x + lean, 4)], OUTLINE, 2)
        ellipse(d, (x + lean - 3, 0, x + lean + 3, 6), GREEN, OUTLINE, 1)
    # tool
    rr(d, (106, 82, 118, 94), 2, DARK, OUTLINE, 1)
    d.rectangle((P(109), P(86), P(115), P(90)), fill=GREEN)
    return im


def mkii_south():
    """Mk-II v2: wheeled modular cube, integrated sensor cap, no separate round head."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (12, 114, 116, 126), (0, 0, 0, 55))
    # rear wheels, partly hidden behind chassis
    for x in (42, 70):
        ellipse(d, (x, 82, x + 16, 102), TREAD, OUTLINE, 2)
        ellipse(d, (x + 4, 87, x + 12, 95), STEEL_MID, OUTLINE, 1)
    # chassis
    rr(d, (20, 76, 108, 112), 4, DARK, OUTLINE, 2)
    rr(d, (26, 82, 102, 108), 2, DARK2, OUTLINE, 1)
    # front wheels
    for x in (18, 86):
        ellipse(d, (x, 96, x + 26, 124), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, 104, x + 20, 118), STEEL_MID, OUTLINE, 2)
        ellipse(d, (x + 10, 108, x + 16, 114), DARK2, OUTLINE, 1)
        # tread blocks
        for i in range(4):
            d.rectangle((P(x + 3 + i * 6), P(119), P(x + 6 + i * 6), P(123)), fill=(82, 91, 102, 255))
    # suspension struts
    for x in (31, 99):
        line(d, [(x, 88), (x, 104)], OUTLINE, 3)
        line(d, [(x, 88), (x, 104)], STEEL_LIGHT, 1)
    # sharp modular torso block
    rr(d, (28, 36, 100, 88), 3, p["shell"], OUTLINE, 2)
    rr(d, (34, 42, 94, 82), 2, p["shell2"], OUTLINE, 1)
    # vertical plate seams + corner bolts
    for x in (46, 82):
        d.rectangle((P(x), P(44), P(x + 2), P(80)), fill=DARK2)
    for x, y in ((34, 42), (94, 42), (34, 82), (94, 82)):
        bolt(d, x, y, 1.5)
    # chest module bay, three swappable blocks
    rr(d, (40, 48, 88, 80), 3, p["accent"], OUTLINE, 1)
    for i in range(3):
        rr(d, (45 + i * 13, 53, 55 + i * 13, 75), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(47 + i * 13), P(58), P(53 + i * 13), P(63)), fill=(GREEN, CYAN, AMBER)[i])
        d.rectangle((P(47 + i * 13), P(69), P(53 + i * 13), P(72)), fill=STEEL_MID)
    # integrated wide sensor cap (not a round head)
    rr(d, (32, 16, 96, 42), 4, p["shell"], OUTLINE, 2)
    rr(d, (37, 21, 91, 38), 2, p["shell2"], OUTLINE, 1)
    rr(d, (40, 26, 88, 36), 2, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(46), P(29), P(82), P(33)), fill=GREEN)
    d.rectangle((P(52), P(34), P(76), P(36)), fill=STEEL_MID)
    # side sensor pods on the cap
    for x in (22, 96):
        rr(d, (x, 24, x + 10, 40), 2, DARK, OUTLINE, 1)
        d.rectangle((P(x + 3), P(28), P(x + 7), P(32)), fill=GREEN)
    # top carry cage / twin blades
    d.rectangle((P(38), P(12), P(90), P(17)), fill=DARK, outline=OUTLINE, width=P(1))
    for x, lean in ((40, -5), (88, 5)):
        line(d, [(x, 16), (x + lean, 4)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 4), (x + lean + 4, 4), (x + lean, 11)], GREEN, OUTLINE)
    # bolt-on shoulder sockets
    for x in (10, 98):
        rr(d, (x, 44, x + 20, 72), 3, p["accent"], OUTLINE, 2)
        ellipse(d, (x + 4, 51, x + 16, 63), DARK2, OUTLINE, 1)
        ellipse(d, (x + 8, 55, x + 12, 59), GREEN, None)
        bolt(d, x + 5, 68, 1.5)
        bolt(d, x + 15, 68, 1.5)
    # modular arms: left clamp, right tool socket
    rr(d, (12, 70, 28, 104), 3, p["shell"], OUTLINE, 1)
    rr(d, (100, 70, 116, 104), 3, p["shell"], OUTLINE, 1)
    # left clamp
    polygon(d, [(10, 104), (20, 104), (16, 116), (8, 112)], STEEL_LIGHT, OUTLINE)
    # right tool socket
    rr(d, (102, 102, 116, 114), 2, DARK, OUTLINE, 1)
    d.rectangle((P(105), P(106), P(113), P(110)), fill=GREEN)
    # base hazard
    for i in range(5):
        polygon(d, [(36 + i * 12, 112), (43 + i * 12, 112), (40 + i * 12, 105), (33 + i * 12, 105)], AMBER)
    return im


def mkii_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (12, 114, 116, 126), (0, 0, 0, 55))
    for x in (42, 70):
        ellipse(d, (x, 82, x + 16, 102), TREAD, OUTLINE, 2)
    rr(d, (20, 76, 108, 112), 4, DARK, OUTLINE, 2)
    for x in (18, 86):
        ellipse(d, (x, 96, x + 26, 124), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, 104, x + 20, 118), STEEL_MID, OUTLINE, 2)
    rr(d, (28, 36, 100, 88), 3, p["shell"], OUTLINE, 2)
    rr(d, (34, 42, 94, 82), 2, p["shell2"], OUTLINE, 1)
    # rear battery / module rack
    for x in (42, 58, 74):
        rr(d, (x, 46, x + 12, 80), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(x + 2), P(51), P(x + 10), P(57)), fill=GREEN)
        d.rectangle((P(x + 2), P(66), P(x + 10), P(71)), fill=STEEL_MID)
        bolt(d, x + 6, 76, 1)
    # rear ports
    d.rectangle((P(36), P(47), P(40), P(79)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    d.rectangle((P(88), P(47), P(92), P(79)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # rear sensor cap vent
    rr(d, (32, 16, 96, 42), 4, p["shell"], OUTLINE, 2)
    rr(d, (37, 21, 91, 38), 2, p["shell2"], OUTLINE, 1)
    rr(d, (40, 24, 88, 38), 2, (20, 28, 37, 255), OUTLINE, 1)
    for x in (45, 54, 63, 72, 81):
        d.rectangle((P(x), P(28), P(x + 3), P(35)), fill=STEEL_MID)
    for x in (22, 96):
        rr(d, (x, 24, x + 10, 40), 2, DARK, OUTLINE, 1)
        d.rectangle((P(x + 3), P(28), P(x + 7), P(32)), fill=GREEN)
    # cage / blades
    d.rectangle((P(38), P(12), P(90), P(17)), fill=DARK, outline=OUTLINE, width=P(1))
    for x, lean in ((40, -5), (88, 5)):
        line(d, [(x, 16), (x + lean, 4)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 4), (x + lean + 4, 4), (x + lean, 11)], GREEN, OUTLINE)
    # shoulder sockets
    for x in (10, 98):
        rr(d, (x, 44, x + 20, 72), 3, p["accent"], OUTLINE, 2)
        ellipse(d, (x + 4, 51, x + 16, 63), DARK2, OUTLINE, 1)
        bolt(d, x + 5, 68, 1.5)
        bolt(d, x + 15, 68, 1.5)
    # arms from behind
    for x in (12, 100):
        rr(d, (x, 70, x + 16, 104), 3, p["shell"], OUTLINE, 1)
    return im


def mkii_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (14, 114, 114, 126), (0, 0, 0, 55))
    # two visible wheels, side profile
    for x in (28, 82):
        ellipse(d, (x, 94, x + 28, 124), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, 102, x + 22, 116), STEEL_MID, OUTLINE, 2)
        ellipse(d, (x + 11, 107, x + 17, 113), DARK2, OUTLINE, 1)
    # chassis / suspension
    rr(d, (24, 76, 106, 108), 3, DARK, OUTLINE, 2)
    for x in (42, 96):
        line(d, [(x, 82), (x, 100)], OUTLINE, 3)
        line(d, [(x, 82), (x, 100)], STEEL_LIGHT, 1)
    # side torso block
    rr(d, (38, 36, 98, 88), 3, p["shell"], OUTLINE, 2)
    rr(d, (44, 42, 92, 82), 2, p["shell2"], OUTLINE, 1)
    # side module slots
    rr(d, (50, 48, 78, 80), 2, p["accent"], OUTLINE, 1)
    for y in (55, 64, 73):
        d.rectangle((P(54), P(y), P(74), P(y + 3)), fill=GREEN)
    # shoulder socket / arm
    rr(d, (80, 44, 100, 72), 3, p["accent"], OUTLINE, 2)
    ellipse(d, (84, 51, 96, 63), DARK2, OUTLINE, 1)
    rr(d, (94, 70, 110, 104), 3, p["shell"], OUTLINE, 1)
    polygon(d, [(108, 104), (118, 104), (114, 116), (106, 112)], STEEL_LIGHT, OUTLINE)
    # integrated sensor cap, side
    rr(d, (42, 16, 100, 42), 4, p["shell"], OUTLINE, 2)
    rr(d, (47, 21, 95, 38), 2, p["shell2"], OUTLINE, 1)
    rr(d, (72, 25, 94, 36), 2, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(78), P(29), P(92), P(33)), fill=GREEN)
    # side sensor pod
    rr(d, (96, 24, 108, 40), 2, DARK, OUTLINE, 1)
    d.rectangle((P(99), P(28), P(105), P(32)), fill=GREEN)
    # cage / blades
    d.rectangle((P(48), P(12), P(96), P(17)), fill=DARK, outline=OUTLINE, width=P(1))
    for x, lean in ((52, -5), (92, 5)):
        line(d, [(x, 16), (x + lean, 4)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 4), (x + lean + 4, 4), (x + lean, 11)], GREEN, OUTLINE)
    # side tool socket
    rr(d, (110, 82, 122, 94), 2, DARK, OUTLINE, 1)
    d.rectangle((P(113), P(86), P(119), P(90)), fill=GREEN)
    return im


def mkii3_south():
    """Mk-II v3: tri-wheel spindle with halo sensor ring and saddle modules."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (12, 116, 116, 128), (0, 0, 0, 55))
    # tri-wheel base: two front wheels, one rear center wheel
    for x, y in ((20, 98), (82, 98), (51, 94)):
        ellipse(d, (x, y, x + 26, y + 26), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, y + 6, x + 20, y + 20), STEEL_MID, OUTLINE, 2)
        ellipse(d, (x + 11, y + 11, x + 15, y + 15), DARK2, OUTLINE, 1)
        for i in range(4):
            d.rectangle((P(x + 4 + i * 6), P(y + 22), P(x + 7 + i * 6), P(y + 25)), fill=(82, 91, 102, 255))
    # central chassis / axle
    polygon(d, [(46, 86), (82, 86), (94, 104), (34, 104)], DARK, OUTLINE)
    polygon(d, [(52, 90), (76, 90), (84, 100), (44, 100)], DARK2, OUTLINE)
    line(d, [(64, 88), (64, 108)], STEEL_LIGHT, 2)
    # exposed vertical frame
    for x in (38, 84):
        d.rectangle((P(x), P(34), P(x + 7), P(86)), fill=STEEL, outline=OUTLINE, width=P(1))
    d.rectangle((P(38), P(34), P(91), P(40)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    d.rectangle((P(38), P(80), P(91), P(86)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # four swappable front modules
    for i, col in enumerate((GREEN, STEEL_MID, AMBER, GREEN)):
        y = 42 + i * 10
        rr(d, (44, y, 84, y + 8), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(48), P(y + 2), P(60), P(y + 6)), fill=col)
        d.rectangle((P(64), P(y + 2), P(80), P(y + 4)), fill=STEEL_MID)
        bolt(d, 46, y + 4, 1)
        bolt(d, 82, y + 4, 1)
    # saddle modules / tool pods
    for x in (14, 94):
        rr(d, (x, 46, x + 20, 84), 4, p["accent"], OUTLINE, 2)
        d.rectangle((P(x + 4), P(52), P(x + 16), P(57)), fill=GREEN)
        d.rectangle((P(x + 4), P(62), P(x + 16), P(68)), fill=DARK2)
        d.rectangle((P(x + 4), P(74), P(x + 16), P(78)), fill=STEEL_MID)
        bolt(d, x + 5, 48, 1)
        bolt(d, x + 15, 48, 1)
    # small manipulator arms below pods
    for x in (16, 96):
        rr(d, (x, 82, x + 14, 106), 3, p["shell"], OUTLINE, 1)
        polygon(d, [(x, 106), (x + 14, 106), (x + 10, 116), (x + 3, 116)], STEEL_LIGHT, OUTLINE)
    # halo sensor ring
    ellipse(d, (40, 8, 88, 48), STEEL_MID, OUTLINE, 2)
    ellipse(d, (47, 15, 81, 41), (20, 28, 37, 255), OUTLINE, 1)
    ellipse(d, (56, 21, 72, 35), GREEN, OUTLINE, 1)
    ellipse(d, (60, 25, 68, 31), (235, 255, 255, 255), None)
    # side sensor points on the ring
    for x in (42, 86):
        ellipse(d, (x - 2, 26, x + 2, 30), AMBER_LIGHT, OUTLINE, 1)
    # neck
    d.rectangle((P(58), P(38), P(70), P(46)), fill=DARK2, outline=OUTLINE, width=P(1))
    # twin blade antennae
    for x, lean in ((44, -6), (84, 6)):
        line(d, [(x, 10), (x + lean, 2)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 2), (x + lean + 4, 2), (x + lean, 8)], GREEN, OUTLINE)
    # hazard base
    for i in range(5):
        polygon(d, [(40 + i * 10, 104), (45 + i * 10, 104), (43 + i * 10, 98), (38 + i * 10, 98)], AMBER)
    return im


def mkii3_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (12, 116, 116, 128), (0, 0, 0, 55))
    for x, y in ((20, 98), (82, 98), (51, 94)):
        ellipse(d, (x, y, x + 26, y + 26), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, y + 6, x + 20, y + 20), STEEL_MID, OUTLINE, 2)
    polygon(d, [(46, 86), (82, 86), (94, 104), (34, 104)], DARK, OUTLINE)
    # rear frame
    for x in (38, 84):
        d.rectangle((P(x), P(34), P(x + 7), P(86)), fill=STEEL, outline=OUTLINE, width=P(1))
    d.rectangle((P(38), P(34), P(91), P(40)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # rear battery modules
    for i, col in enumerate((DARK2, STEEL_MID, GREEN)):
        y = 44 + i * 14
        rr(d, (44, y, 84, y + 11), 2, col, OUTLINE, 1)
        d.rectangle((P(48), P(y + 3), P(58), P(y + 7)), fill=GREEN if i != 1 else AMBER)
        d.rectangle((P(64), P(y + 3), P(80), P(y + 5)), fill=STEEL_MID)
    # rear ports
    for x in (42, 80):
        d.rectangle((P(x), P(44), P(x + 4), P(82)), fill=STEEL_LIGHT, outline=OUTLINE, width=P(1))
    # saddle modules
    for x in (14, 94):
        rr(d, (x, 46, x + 20, 84), 4, p["accent"], OUTLINE, 2)
        d.rectangle((P(x + 4), P(54), P(x + 16), P(60)), fill=STEEL_MID)
        d.rectangle((P(x + 4), P(70), P(x + 16), P(76)), fill=DARK2)
    for x in (16, 96):
        rr(d, (x, 82, x + 14, 106), 3, p["shell"], OUTLINE, 1)
    # halo ring rear
    ellipse(d, (40, 8, 88, 48), STEEL_MID, OUTLINE, 2)
    ellipse(d, (47, 15, 81, 41), (20, 28, 37, 255), OUTLINE, 1)
    for x in (56, 62, 68, 74):
        d.rectangle((P(x), P(22), P(x + 3), P(34)), fill=STEEL_MID)
    for x in (42, 86):
        ellipse(d, (x - 2, 26, x + 2, 30), AMBER_LIGHT, OUTLINE, 1)
    d.rectangle((P(58), P(38), P(70), P(46)), fill=DARK2, outline=OUTLINE, width=P(1))
    for x, lean in ((44, -6), (84, 6)):
        line(d, [(x, 10), (x + lean, 2)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 2), (x + lean + 4, 2), (x + lean, 8)], GREEN, OUTLINE)
    return im


def mkii3_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    p = TIERS["MkII"]
    ellipse(d, (14, 116, 114, 128), (0, 0, 0, 55))
    # two visible wheels in profile
    for x in (26, 80):
        ellipse(d, (x, 96, x + 28, 124), TREAD, OUTLINE, 2)
        ellipse(d, (x + 6, 102, x + 22, 118), STEEL_MID, OUTLINE, 2)
        ellipse(d, (x + 11, 107, x + 17, 113), DARK2, OUTLINE, 1)
    # chassis
    polygon(d, [(34, 84), (92, 84), (102, 104), (26, 104)], DARK, OUTLINE)
    line(d, [(64, 86), (64, 108)], STEEL_LIGHT, 2)
    # side frame / rails
    d.rectangle((P(44), P(34), P(51), P(86)), fill=STEEL, outline=OUTLINE, width=P(1))
    d.rectangle((P(84), P(34), P(91), P(86)), fill=STEEL, outline=OUTLINE, width=P(1))
    d.rectangle((P(44), P(34), P(91), P(40)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    # side modules
    for i, col in enumerate((GREEN, DARK2, STEEL_MID)):
        y = 44 + i * 11
        rr(d, (52, y, 84, y + 8), 2, DARK2, OUTLINE, 1)
        d.rectangle((P(56), P(y + 2), P(68), P(y + 6)), fill=col)
        d.rectangle((P(72), P(y + 2), P(80), P(y + 4)), fill=STEEL_MID)
    # saddle module
    rr(d, (78, 46, 98, 84), 4, p["accent"], OUTLINE, 2)
    d.rectangle((P(82), P(52), P(94), P(57)), fill=GREEN)
    d.rectangle((P(82), P(64), P(94), P(70)), fill=DARK2)
    rr(d, (88, 82, 102, 106), 3, p["shell"], OUTLINE, 1)
    polygon(d, [(100, 106), (110, 106), (106, 116), (98, 114)], STEEL_LIGHT, OUTLINE)
    # halo ring side
    ellipse(d, (42, 8, 90, 48), STEEL_MID, OUTLINE, 2)
    ellipse(d, (49, 15, 83, 41), (20, 28, 37, 255), OUTLINE, 1)
    rr(d, (74, 22, 88, 34), 2, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(78), P(26), P(86), P(30)), fill=GREEN)
    d.rectangle((P(60), P(38), P(72), P(46)), fill=DARK2, outline=OUTLINE, width=P(1))
    # antenna
    for x, lean in ((50, -6), (86, 6)):
        line(d, [(x, 10), (x + lean, 2)], OUTLINE, 2)
        polygon(d, [(x + lean - 4, 2), (x + lean + 4, 2), (x + lean, 8)], GREEN, OUTLINE)
    # side tool block
    rr(d, (104, 82, 116, 94), 2, DARK, OUTLINE, 1)
    d.rectangle((P(107), P(86), P(113), P(90)), fill=GREEN)
    return im


def robot_south(tier):
    if tier == "MkII":
        return mkii3_south()
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
    if tier == "MkII":
        return mkii3_north()
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
    if tier == "MkII":
        return mkii3_east()
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
