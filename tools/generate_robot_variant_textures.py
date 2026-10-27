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


def mkii4_south():
    """Mk-II v4: low quadruped module carrier, periscope sensor mast, no head dome."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (218, 228, 214, 255)
    shell2 = (151, 173, 154, 255)
    accent = (62, 120, 92, 255)
    eye = (119, 224, 196, 255)
    ellipse(d, (14, 114, 114, 126), (0, 0, 0, 55))

    # rear legs, drawn first
    for hip, knee, foot in (((42, 80), (36, 98), (40, 114)), ((86, 80), (92, 98), (88, 114))):
        line(d, [hip, knee, foot], OUTLINE, 7)
        line(d, [hip, knee, foot], (84, 96, 84, 255), 3)
        rr(d, (foot[0] - 6, foot[1] - 2, foot[0] + 8, foot[1] + 4), 2, DARK, OUTLINE, 1)
    # low body slab
    rr(d, (24, 52, 104, 92), 7, shell, OUTLINE, 2)
    rr(d, (30, 58, 98, 88), 4, shell2, OUTLINE, 1)
    d.rectangle((P(24), P(84), P(104), P(92)), fill=DARK, outline=OUTLINE, width=P(1))
    # top deck / three swappable cartridges
    rr(d, (30, 44, 98, 62), 3, DARK2, OUTLINE, 1)
    for i, col in enumerate((GREEN, STEEL_MID, AMBER)):
        x = 35 + i * 21
        rr(d, (x, 47, x + 17, 60), 2, col, OUTLINE, 1)
        d.rectangle((P(x + 3), P(51), P(x + 14), P(54)), fill=shell if i != 2 else DARK2)
    # front legs
    for hip, knee, foot in (((28, 80), (18, 96), (22, 116)), ((100, 80), (110, 96), (106, 116))):
        line(d, [hip, knee, foot], OUTLINE, 7)
        line(d, [hip, knee, foot], STEEL_LIGHT, 3)
        rr(d, (foot[0] - 7, foot[1] - 2, foot[0] + 9, foot[1] + 5), 2, DARK, OUTLINE, 1)
        bolt(d, knee[0], knee[1], 1.5)
    # small front utility arms
    for x, sign in ((20, -1), (108, 1)):
        line(d, [(x, 66), (x + sign * 2, 86)], OUTLINE, 4)
        line(d, [(x, 66), (x + sign * 2, 86)], shell2, 2)
        rr(d, (x + sign * 2 - 4, 84, x + sign * 2 + 6, 94), 2, accent, OUTLINE, 1)
        d.rectangle((P(x + sign * 2 - 1), P(88), P(x + sign * 2 + 3), P(91)), fill=eye)
    # periscope sensor mast
    rr(d, (48, 16, 80, 52), 4, DARK, OUTLINE, 2)
    rr(d, (53, 21, 75, 47), 3, shell2, OUTLINE, 1)
    rr(d, (52, 27, 76, 39), 2, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(56), P(31), P(72), P(36)), fill=eye)
    ellipse(d, (60, 32, 68, 38), (235, 255, 255, 255), None)
    # side sensor dots
    for x in (50, 78):
        ellipse(d, (x - 2, 22, x + 2, 26), AMBER_LIGHT, OUTLINE, 1)
    # mast antenna
    line(d, [(64, 16), (64, 8)], OUTLINE, 2)
    ellipse(d, (60, 3, 68, 11), GREEN, OUTLINE, 1)
    # deck rails / bolts
    d.rectangle((P(30), P(42), P(98), P(45)), fill=STEEL_MID, outline=OUTLINE, width=P(1))
    for x in (34, 94):
        bolt(d, x, 46, 1)
    # hazard strip on lower front
    for i in range(5):
        polygon(d, [(34 + i * 12, 92), (39 + i * 12, 92), (37 + i * 12, 86), (32 + i * 12, 86)], AMBER)
    return im


def mkii4_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (218, 228, 214, 255)
    shell2 = (151, 173, 154, 255)
    accent = (62, 120, 92, 255)
    ellipse(d, (14, 114, 114, 126), (0, 0, 0, 55))
    for hip, knee, foot in (((42, 80), (36, 98), (40, 114)), ((86, 80), (92, 98), (88, 114))):
        line(d, [hip, knee, foot], OUTLINE, 7)
        line(d, [hip, knee, foot], (84, 96, 84, 255), 3)
        rr(d, (foot[0] - 6, foot[1] - 2, foot[0] + 8, foot[1] + 4), 2, DARK, OUTLINE, 1)
    rr(d, (24, 52, 104, 92), 7, shell, OUTLINE, 2)
    rr(d, (30, 58, 98, 88), 4, shell2, OUTLINE, 1)
    # rear module rack
    for i, col in enumerate((DARK2, GREEN, STEEL_MID)):
        x = 35 + i * 21
        rr(d, (x, 62, x + 17, 84), 2, col, OUTLINE, 1)
        d.rectangle((P(x + 3), P(66), P(x + 14), P(69)), fill=STEEL_MID)
        d.rectangle((P(x + 3), P(76), P(x + 14), P(79)), fill=GREEN if i != 1 else AMBER)
    # rear ports / cable
    for x in (32, 94):
        d.rectangle((P(x), P(62), P(x + 4), P(84)), fill=STEEL_LIGHT, outline=OUTLINE, width=P(1))
    # front legs from behind
    for hip, knee, foot in (((28, 80), (18, 96), (22, 116)), ((100, 80), (110, 96), (106, 116))):
        line(d, [hip, knee, foot], OUTLINE, 7)
        line(d, [hip, knee, foot], STEEL_LIGHT, 3)
        rr(d, (foot[0] - 7, foot[1] - 2, foot[0] + 9, foot[1] + 5), 2, DARK, OUTLINE, 1)
    # mast rear vent
    rr(d, (48, 16, 80, 52), 4, DARK, OUTLINE, 2)
    rr(d, (53, 21, 75, 47), 3, shell2, OUTLINE, 1)
    rr(d, (54, 24, 74, 42), 2, (20, 28, 37, 255), OUTLINE, 1)
    for x in (58, 63, 68):
        d.rectangle((P(x), P(28), P(x + 3), P(39)), fill=STEEL_MID)
    for x in (50, 78):
        ellipse(d, (x - 2, 22, x + 2, 26), AMBER_LIGHT, OUTLINE, 1)
    line(d, [(64, 16), (64, 8)], OUTLINE, 2)
    ellipse(d, (60, 3, 68, 11), GREEN, OUTLINE, 1)
    # rear shoulders / arms
    for x, sign in ((20, -1), (108, 1)):
        line(d, [(x, 66), (x + sign * 2, 86)], OUTLINE, 4)
        line(d, [(x, 66), (x + sign * 2, 86)], shell2, 2)
        rr(d, (x + sign * 2 - 4, 84, x + sign * 2 + 6, 94), 2, accent, OUTLINE, 1)
    return im


def mkii4_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (218, 228, 214, 255)
    shell2 = (151, 173, 154, 255)
    accent = (62, 120, 92, 255)
    eye = (119, 224, 196, 255)
    ellipse(d, (14, 114, 114, 126), (0, 0, 0, 55))
    # two visible legs in profile
    for hip, knee, foot in (((42, 80), (32, 98), (36, 116)), ((88, 80), (98, 98), (94, 116))):
        line(d, [hip, knee, foot], OUTLINE, 7)
        line(d, [hip, knee, foot], STEEL_LIGHT, 3)
        rr(d, (foot[0] - 7, foot[1] - 2, foot[0] + 9, foot[1] + 5), 2, DARK, OUTLINE, 1)
    # side body slab
    rr(d, (30, 52, 100, 92), 6, shell, OUTLINE, 2)
    rr(d, (36, 58, 94, 88), 4, shell2, OUTLINE, 1)
    d.rectangle((P(30), P(84), P(100), P(92)), fill=DARK, outline=OUTLINE, width=P(1))
    # top cartridges in profile
    rr(d, (36, 44, 96, 62), 3, DARK2, OUTLINE, 1)
    for i, col in enumerate((GREEN, STEEL_MID, AMBER)):
        x = 40 + i * 18
        rr(d, (x, 47, x + 14, 60), 2, col, OUTLINE, 1)
        d.rectangle((P(x + 3), P(51), P(x + 11), P(54)), fill=shell if i != 2 else DARK2)
    # side mast / eye strip
    rr(d, (46, 16, 84, 52), 4, DARK, OUTLINE, 2)
    rr(d, (51, 21, 79, 47), 3, shell2, OUTLINE, 1)
    rr(d, (66, 27, 82, 39), 2, (20, 28, 37, 255), OUTLINE, 1)
    d.rectangle((P(71), P(31), P(80), P(36)), fill=eye)
    # side arm / tool pod
    line(d, [(92, 66), (104, 84), (100, 96)], OUTLINE, 5)
    line(d, [(92, 66), (104, 84), (100, 96)], shell2, 2)
    rr(d, (96, 94, 110, 104), 2, accent, OUTLINE, 1)
    d.rectangle((P(100), P(97), P(106), P(100)), fill=eye)
    # mast antenna
    line(d, [(64, 16), (64, 8)], OUTLINE, 2)
    ellipse(d, (60, 3, 68, 11), GREEN, OUTLINE, 1)
    # side sensor dot
    ellipse(d, (42, 22, 46, 26), AMBER_LIGHT, OUTLINE, 1)
    # hazard
    for i in range(4):
        polygon(d, [(42 + i * 12, 92), (47 + i * 12, 92), (45 + i * 12, 86), (40 + i * 12, 86)], AMBER)
    return im


def mkii5_south():
    """Mk-II v5: soft toy robot, black glass face, two blue eyes, gold ears and arms."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 211, 202, 255)
    shell_hi = (238, 235, 226, 255)
    shell_lo = (166, 163, 156, 255)
    face = (17, 19, 21, 255)
    body = (48, 49, 52, 255)
    body_hi = (66, 67, 71, 255)
    gold = (190, 151, 89, 255)
    gold_hi = (222, 184, 116, 255)
    eye = (111, 190, 255, 255)
    eye_hi = (196, 232, 255, 255)
    ellipse(d, (22, 112, 106, 126), (0, 0, 0, 55))

    # gold capsule arms behind the body
    for side in (-1, 1):
        x1 = 8 if side == -1 else 92
        x2 = 30 if side == -1 else 114
        rr(d, (x1, 68, x2, 108), 11, gold, OUTLINE, 2)
        rr(d, (x1 + 4, 72, x2 - 4, 96), 8, gold_hi, None)
        ellipse(d, (x1 + 1, 98, x2 - 1, 112), gold, OUTLINE, 2)

    # soft rounded body
    rr(d, (36, 58, 92, 112), 22, body, OUTLINE, 2)
    ellipse(d, (38, 98, 90, 116), body, OUTLINE, 2)
    # chest V seam
    line(d, [(52, 76), (64, 84), (76, 76)], (78, 79, 84, 255), 2)
    line(d, [(52, 76), (64, 84), (76, 76)], (35, 36, 39, 255), 1)
    # body top highlight
    ellipse(d, (44, 62, 84, 76), body_hi, None)

    # neck
    ellipse(d, (54, 64, 74, 80), (35, 36, 39, 255), OUTLINE, 1)

    # gold ears (behind the head rim)
    for x in (8, 100):
        ellipse(d, (x, 32, x + 20, 58), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 37, x + 16, 53), gold_hi, None)
        ellipse(d, (x + 7, 41, x + 13, 49), (118, 91, 54, 255), None)

    # head shell
    rr(d, (20, 14, 108, 76), 24, shell, OUTLINE, 2)
    rr(d, (25, 19, 103, 71), 20, shell_hi, None)
    rr(d, (26, 21, 102, 69), 19, shell, OUTLINE, 1)
    # black glass face
    rr(d, (28, 24, 100, 66), 17, face, OUTLINE, 2)
    rr(d, (32, 28, 96, 62), 14, (12, 14, 16, 255), None)

    # two blue eyes
    for x in (38, 70):
        rr(d, (x, 38, x + 20, 54), 5, eye, None)
        rr(d, (x + 3, 41, x + 17, 47), 3, eye_hi, None)
        ellipse(d, (x + 6, 42, x + 15, 50), (225, 247, 255, 255), None)

    # head rim highlight
    line(d, [(30, 18), (92, 18)], (250, 248, 240, 255), 2)
    # hover base shadow ring
    ellipse(d, (40, 108, 88, 118), (32, 33, 36, 255), None)
    return im


def mkii5_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 211, 202, 255)
    shell_hi = (238, 235, 226, 255)
    shell_lo = (166, 163, 156, 255)
    body = (48, 49, 52, 255)
    body_hi = (66, 67, 71, 255)
    gold = (190, 151, 89, 255)
    gold_hi = (222, 184, 116, 255)
    ellipse(d, (22, 112, 106, 126), (0, 0, 0, 55))
    for side in (-1, 1):
        x1 = 8 if side == -1 else 92
        x2 = 30 if side == -1 else 114
        rr(d, (x1, 68, x2, 108), 11, gold, OUTLINE, 2)
        ellipse(d, (x1 + 1, 98, x2 - 1, 112), gold, OUTLINE, 2)
    rr(d, (36, 58, 92, 112), 22, body, OUTLINE, 2)
    ellipse(d, (38, 98, 90, 116), body, OUTLINE, 2)
    # rear panel / seam
    rr(d, (46, 68, 82, 104), 10, (38, 39, 42, 255), OUTLINE, 1)
    line(d, [(64, 72), (64, 100)], (72, 74, 78, 255), 2)
    for x in (10, 98):
        ellipse(d, (x, 32, x + 20, 58), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 37, x + 16, 53), gold_hi, None)
    # head rear shell
    rr(d, (20, 14, 108, 76), 24, shell, OUTLINE, 2)
    rr(d, (25, 19, 103, 71), 20, shell_hi, None)
    rr(d, (26, 21, 102, 69), 19, shell, OUTLINE, 1)
    # rear vent
    rr(d, (40, 30, 88, 58), 9, (38, 39, 42, 255), OUTLINE, 1)
    for x in (48, 57, 66, 75):
        d.rectangle((P(x), P(35), P(x + 3), P(52)), fill=(105, 104, 100, 255))
    # gold back dot / seam
    ellipse(d, (60, 60, 68, 68), gold, OUTLINE, 1)
    line(d, [(30, 18), (92, 18)], (250, 248, 240, 255), 2)
    return im


def mkii5_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 211, 202, 255)
    shell_hi = (238, 235, 226, 255)
    face = (17, 19, 21, 255)
    body = (48, 49, 52, 255)
    body_hi = (66, 67, 71, 255)
    gold = (190, 151, 89, 255)
    gold_hi = (222, 184, 116, 255)
    eye = (111, 190, 255, 255)
    eye_hi = (196, 232, 255, 255)
    ellipse(d, (24, 112, 104, 126), (0, 0, 0, 55))
    # one visible gold arm, front
    rr(d, (86, 68, 112, 108), 11, gold, OUTLINE, 2)
    rr(d, (91, 72, 107, 96), 8, gold_hi, None)
    ellipse(d, (88, 98, 110, 112), gold, OUTLINE, 2)
    # side body
    rr(d, (42, 58, 94, 112), 20, body, OUTLINE, 2)
    ellipse(d, (44, 98, 92, 116), body, OUTLINE, 2)
    line(d, [(58, 76), (68, 84), (78, 76)], (78, 79, 84, 255), 2)
    ellipse(d, (50, 62, 86, 76), body_hi, None)
    # side head
    rr(d, (34, 14, 104, 76), 23, shell, OUTLINE, 2)
    rr(d, (39, 19, 99, 71), 19, shell_hi, None)
    # side face panel, wrapping the front
    rr(d, (68, 24, 102, 66), 14, face, OUTLINE, 2)
    rr(d, (76, 30, 98, 61), 10, (12, 14, 16, 255), None)
    # one visible eye
    rr(d, (80, 38, 98, 54), 4, eye, None)
    rr(d, (83, 41, 95, 47), 2, eye_hi, None)
    # gold ear, side
    ellipse(d, (42, 32, 62, 58), gold, OUTLINE, 2)
    ellipse(d, (46, 37, 58, 53), gold_hi, None)
    ellipse(d, (49, 41, 55, 49), (118, 91, 54, 255), None)
    # neck
    ellipse(d, (58, 64, 76, 80), (35, 36, 39, 255), OUTLINE, 1)
    # rim highlight
    line(d, [(44, 18), (96, 18)], (250, 248, 240, 255), 2)
    return im


def mkii6_south():
    """Mk-II v6: cleaner redraw of the toy reference, closer head/body proportions."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (213, 211, 203, 255)
    shell_hi = (240, 238, 230, 255)
    shell_lo = (163, 161, 154, 255)
    face = (16, 18, 20, 255)
    body = (48, 49, 52, 255)
    body_hi = (67, 68, 72, 255)
    gold = (188, 150, 89, 255)
    gold_hi = (222, 184, 116, 255)
    gold_lo = (128, 98, 56, 255)
    eye = (109, 190, 255, 255)
    eye_hi = (198, 232, 255, 255)
    ellipse(d, (24, 114, 104, 126), (0, 0, 0, 52))

    # smaller gold capsule arms, behind body
    for side in (-1, 1):
        x1 = 12 if side == -1 else 96
        x2 = 30 if side == -1 else 114
        rr(d, (x1, 74, x2, 108), 9, gold, OUTLINE, 2)
        rr(d, (x1 + 4, 78, x2 - 4, 98), 7, gold_hi, None)
        ellipse(d, (x1 + 1, 100, x2 - 1, 112), gold, OUTLINE, 2)

    # rounded body
    rr(d, (34, 60, 94, 112), 20, body, OUTLINE, 2)
    ellipse(d, (36, 98, 92, 116), body, OUTLINE, 2)
    line(d, [(52, 78), (64, 86), (76, 78)], (78, 80, 84, 255), 2)
    line(d, [(52, 78), (64, 86), (76, 78)], (34, 35, 38, 255), 1)
    ellipse(d, (44, 64, 84, 78), body_hi, None)

    # neck
    ellipse(d, (54, 66, 74, 82), (34, 35, 38, 255), OUTLINE, 1)

    # small gold ears
    for x in (16, 96):
        ellipse(d, (x, 36, x + 18, 56), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 40, x + 14, 52), gold_hi, None)
        ellipse(d, (x + 6, 44, x + 12, 50), gold_lo, None)

    # head shell
    rr(d, (24, 16, 104, 76), 22, shell, OUTLINE, 2)
    rr(d, (28, 20, 100, 72), 19, shell_hi, None)
    rr(d, (29, 21, 99, 71), 18, shell, OUTLINE, 1)
    # black glass face
    rr(d, (32, 24, 96, 68), 16, face, OUTLINE, 2)
    rr(d, (36, 28, 92, 64), 13, (12, 14, 16, 255), None)

    # two rounded blue eyes
    for x in (42, 68):
        rr(d, (x, 40, x + 18, 55), 4, eye, None)
        rr(d, (x + 3, 42, x + 15, 48), 3, eye_hi, None)
        ellipse(d, (x + 5, 43, x + 13, 51), (226, 247, 255, 255), None)

    # shell highlight
    line(d, [(32, 20), (94, 20)], (252, 250, 242, 255), 2)
    # hover base
    ellipse(d, (42, 108, 86, 118), (31, 32, 35, 255), None)
    return im


def mkii6_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (213, 211, 203, 255)
    shell_hi = (240, 238, 230, 255)
    body = (48, 49, 52, 255)
    gold = (188, 150, 89, 255)
    gold_hi = (222, 184, 116, 255)
    ellipse(d, (24, 114, 104, 126), (0, 0, 0, 52))
    for side in (-1, 1):
        x1 = 12 if side == -1 else 96
        x2 = 30 if side == -1 else 114
        rr(d, (x1, 74, x2, 108), 9, gold, OUTLINE, 2)
        ellipse(d, (x1 + 1, 100, x2 - 1, 112), gold, OUTLINE, 2)
    rr(d, (34, 60, 94, 112), 20, body, OUTLINE, 2)
    ellipse(d, (36, 98, 92, 116), body, OUTLINE, 2)
    rr(d, (48, 70, 80, 104), 10, (38, 39, 42, 255), OUTLINE, 1)
    line(d, [(64, 74), (64, 100)], (74, 76, 80, 255), 2)
    for x in (16, 96):
        ellipse(d, (x, 36, x + 18, 56), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 40, x + 14, 52), gold_hi, None)
    rr(d, (24, 16, 104, 76), 22, shell, OUTLINE, 2)
    rr(d, (28, 20, 100, 72), 19, shell_hi, None)
    rr(d, (29, 21, 99, 71), 18, shell, OUTLINE, 1)
    # rear vent panel
    rr(d, (42, 30, 86, 58), 9, (38, 39, 42, 255), OUTLINE, 1)
    for x in (50, 58, 66, 74):
        d.rectangle((P(x), P(35), P(x + 3), P(52)), fill=(107, 106, 102, 255))
    ellipse(d, (60, 61, 68, 69), gold, OUTLINE, 1)
    line(d, [(32, 20), (94, 20)], (252, 250, 242, 255), 2)
    return im


def mkii6_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (213, 211, 203, 255)
    shell_hi = (240, 238, 230, 255)
    face = (16, 18, 20, 255)
    body = (48, 49, 52, 255)
    body_hi = (67, 68, 72, 255)
    gold = (188, 150, 89, 255)
    gold_hi = (222, 184, 116, 255)
    eye = (109, 190, 255, 255)
    eye_hi = (198, 232, 255, 255)
    ellipse(d, (26, 114, 102, 126), (0, 0, 0, 52))
    # visible gold arm
    rr(d, (90, 74, 114, 108), 9, gold, OUTLINE, 2)
    rr(d, (95, 78, 109, 98), 7, gold_hi, None)
    ellipse(d, (92, 100, 112, 112), gold, OUTLINE, 2)
    # side body
    rr(d, (42, 60, 94, 112), 18, body, OUTLINE, 2)
    ellipse(d, (44, 98, 92, 116), body, OUTLINE, 2)
    line(d, [(56, 78), (68, 86), (80, 78)], (78, 80, 84, 255), 2)
    ellipse(d, (50, 64, 86, 78), body_hi, None)
    # side head
    rr(d, (36, 16, 104, 76), 21, shell, OUTLINE, 2)
    rr(d, (40, 20, 100, 72), 18, shell_hi, None)
    # side face panel
    rr(d, (74, 24, 102, 68), 14, face, OUTLINE, 2)
    rr(d, (79, 29, 98, 63), 10, (12, 14, 16, 255), None)
    rr(d, (84, 40, 98, 54), 4, eye, None)
    rr(d, (87, 42, 95, 48), 3, eye_hi, None)
    # gold ear on the side shell
    ellipse(d, (46, 36, 64, 56), gold, OUTLINE, 2)
    ellipse(d, (50, 40, 60, 52), gold_hi, None)
    ellipse(d, (52, 44, 58, 50), (128, 98, 56, 255), None)
    # neck
    ellipse(d, (60, 66, 78, 82), (34, 35, 38, 255), OUTLINE, 1)
    line(d, [(44, 20), (96, 20)], (252, 250, 242, 255), 2)
    return im


def mkii7_south():
    """Mk-II v7: clean toy reference redraw with softer shell, gloss eyes and angled arms."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 212, 204, 255)
    shell_hi = (242, 240, 233, 255)
    shell_lo = (160, 158, 152, 255)
    face = (15, 17, 19, 255)
    body = (47, 48, 51, 255)
    body_hi = (61, 62, 66, 255)
    gold = (188, 149, 87, 255)
    gold_hi = (225, 187, 119, 255)
    gold_lo = (126, 96, 54, 255)
    eye = (108, 189, 255, 255)
    eye_hi = (204, 236, 255, 255)
    ellipse(d, (22, 112, 106, 126), (0, 0, 0, 55))

    # angled gold capsule arms, drawn behind the body
    for side in (-1, 1):
        sx = 38 if side == -1 else 90
        ex = 18 if side == -1 else 110
        line(d, [(sx, 74), (ex, 102)], OUTLINE, 21)
        line(d, [(sx, 74), (ex, 102)], gold, 15)
        line(d, [(sx, 76), (ex - side, 98)], gold_hi, 6)
        ellipse(d, (ex - 8, 95, ex + 8, 111), gold, OUTLINE, 2)
        ellipse(d, (ex - 4, 98, ex + 4, 106), gold_hi, None)

    # rounded body
    rr(d, (36, 62, 92, 110), 22, body, OUTLINE, 2)
    ellipse(d, (38, 100, 90, 116), body, OUTLINE, 2)
    ellipse(d, (42, 64, 86, 76), body_hi, None)
    # V seam
    line(d, [(52, 80), (64, 88), (76, 80)], (76, 78, 82, 255), 2)
    line(d, [(52, 80), (64, 88), (76, 80)], (33, 34, 37, 255), 1)
    # neck
    ellipse(d, (54, 68, 74, 84), (34, 35, 38, 255), OUTLINE, 1)

    # small gold cylinder ears
    for x in (14, 96):
        ellipse(d, (x, 36, x + 18, 56), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 40, x + 14, 52), gold_hi, None)
        ellipse(d, (x + 6, 44, x + 12, 50), gold_lo, None)

    # head shell with bevel
    rr(d, (22, 14, 106, 76), 24, shell, OUTLINE, 2)
    rr(d, (26, 18, 102, 72), 21, shell_hi, None)
    rr(d, (28, 20, 100, 70), 19, shell, OUTLINE, 1)
    # black glass face
    rr(d, (32, 24, 96, 68), 17, face, OUTLINE, 2)
    rr(d, (35, 27, 93, 65), 14, (11, 13, 15, 255), None)
    # soft blue eyes
    for x in (42, 68):
        rr(d, (x, 41, x + 18, 56), 5, eye, None)
        rr(d, (x + 2, 43, x + 14, 48), 3, eye_hi, None)
        rr(d, (x + 4, 51, x + 16, 54), 2, (48, 125, 190, 255), None)
    # shell top highlight
    line(d, [(31, 18), (95, 18)], (252, 250, 243, 255), 2)
    # soft body shadow at the bottom
    ellipse(d, (44, 108, 86, 118), (30, 31, 34, 255), None)
    return im


def mkii7_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 212, 204, 255)
    shell_hi = (242, 240, 233, 255)
    body = (47, 48, 51, 255)
    body_hi = (61, 62, 66, 255)
    gold = (188, 149, 87, 255)
    gold_hi = (225, 187, 119, 255)
    ellipse(d, (22, 112, 106, 126), (0, 0, 0, 55))
    for side in (-1, 1):
        sx = 38 if side == -1 else 90
        ex = 18 if side == -1 else 110
        line(d, [(sx, 74), (ex, 102)], OUTLINE, 21)
        line(d, [(sx, 74), (ex, 102)], gold, 15)
        ellipse(d, (ex - 8, 95, ex + 8, 111), gold, OUTLINE, 2)
    rr(d, (36, 62, 92, 110), 22, body, OUTLINE, 2)
    ellipse(d, (38, 100, 90, 116), body, OUTLINE, 2)
    rr(d, (48, 72, 80, 104), 10, (38, 39, 42, 255), OUTLINE, 1)
    line(d, [(64, 76), (64, 100)], (74, 76, 80, 255), 2)
    for x in (14, 96):
        ellipse(d, (x, 36, x + 18, 56), gold, OUTLINE, 2)
        ellipse(d, (x + 4, 40, x + 14, 52), gold_hi, None)
    rr(d, (22, 14, 106, 76), 24, shell, OUTLINE, 2)
    rr(d, (26, 18, 102, 72), 21, shell_hi, None)
    rr(d, (28, 20, 100, 70), 19, shell, OUTLINE, 1)
    rr(d, (42, 30, 86, 58), 9, (38, 39, 42, 255), OUTLINE, 1)
    for x in (50, 58, 66, 74):
        d.rectangle((P(x), P(35), P(x + 3), P(52)), fill=(107, 106, 102, 255))
    ellipse(d, (60, 61, 68, 69), gold, OUTLINE, 1)
    line(d, [(31, 18), (95, 18)], (252, 250, 243, 255), 2)
    return im


def mkii7_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (214, 212, 204, 255)
    shell_hi = (242, 240, 233, 255)
    face = (15, 17, 19, 255)
    body = (47, 48, 51, 255)
    body_hi = (61, 62, 66, 255)
    gold = (188, 149, 87, 255)
    gold_hi = (225, 187, 119, 255)
    eye = (108, 189, 255, 255)
    eye_hi = (204, 236, 255, 255)
    ellipse(d, (26, 112, 102, 126), (0, 0, 0, 55))
    # visible angled arm
    line(d, [(84, 74), (104, 100)], OUTLINE, 21)
    line(d, [(84, 74), (104, 100)], gold, 15)
    line(d, [(86, 78), (102, 96)], gold_hi, 5)
    ellipse(d, (96, 94, 112, 110), gold, OUTLINE, 2)
    # side body
    rr(d, (42, 62, 94, 110), 18, body, OUTLINE, 2)
    ellipse(d, (44, 100, 92, 116), body, OUTLINE, 2)
    ellipse(d, (48, 64, 88, 76), body_hi, None)
    line(d, [(56, 80), (68, 88), (80, 80)], (76, 78, 82, 255), 2)
    # side head
    rr(d, (34, 14, 106, 76), 23, shell, OUTLINE, 2)
    rr(d, (38, 18, 102, 72), 20, shell_hi, None)
    # side glass, wrapping the front edge
    rr(d, (74, 24, 102, 68), 14, face, OUTLINE, 2)
    rr(d, (78, 28, 98, 64), 11, (11, 13, 15, 255), None)
    rr(d, (84, 41, 98, 56), 5, eye, None)
    rr(d, (86, 43, 95, 48), 3, eye_hi, None)
    # gold ear cylinder on the side
    ellipse(d, (44, 36, 64, 56), gold, OUTLINE, 2)
    ellipse(d, (48, 40, 60, 52), gold_hi, None)
    ellipse(d, (51, 44, 57, 50), (126, 96, 54, 255), None)
    # neck
    ellipse(d, (60, 68, 78, 84), (34, 35, 38, 255), OUTLINE, 1)
    line(d, [(42, 18), (98, 18)], (252, 250, 243, 255), 2)
    return im


def mkiii_ref_south():
    """Mk-III v2: white/orange humanoid service robot on a wheeled base."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    white = (241, 238, 232, 255)
    white_hi = (255, 253, 247, 255)
    white_lo = (203, 200, 194, 255)
    orange = (232, 94, 54, 255)
    orange_hi = (247, 128, 88, 255)
    screen = (18, 20, 22, 255)
    dark = (52, 55, 59, 255)
    joint = (91, 94, 98, 255)
    eye = (244, 245, 240, 255)
    gold = (232, 180, 76, 255)
    ellipse(d, (18, 116, 110, 128), (0, 0, 0, 55))

    # wheeled orange base
    rr(d, (22, 100, 106, 118), 8, orange, OUTLINE, 2)
    rr(d, (26, 96, 102, 108), 5, orange_hi, None)
    rr(d, (28, 110, 100, 122), 4, (226, 222, 214, 255), OUTLINE, 1)
    for x in (30, 86):
        ellipse(d, (x, 108, x + 18, 124), (34, 36, 39, 255), OUTLINE, 2)
        ellipse(d, (x + 4, 112, x + 14, 120), joint, OUTLINE, 1)
    # skirt
    rr(d, (34, 96, 94, 106), 4, white_lo, OUTLINE, 1)

    # legs / hip block (mostly hidden by base and torso)
    rr(d, (50, 88, 64, 106), 4, dark, OUTLINE, 1)
    rr(d, (68, 88, 82, 106), 4, dark, OUTLINE, 1)

    # torso
    rr(d, (38, 46, 90, 98), 12, white, OUTLINE, 2)
    rr(d, (43, 51, 85, 93), 9, white_hi, None)
    # black chest screen
    rr(d, (50, 58, 78, 90), 6, screen, OUTLINE, 2)
    rr(d, (54, 62, 74, 86), 4, (10, 11, 13, 255), None)
    # orange shoulder pads
    rr(d, (31, 52, 43, 66), 4, orange, OUTLINE, 1)
    rr(d, (85, 52, 97, 66), 4, orange, OUTLINE, 1)
    # gold chest badge
    rr(d, (58, 48, 70, 55), 2, gold, OUTLINE, 1)

    # neck
    rr(d, (58, 42, 70, 50), 3, joint, OUTLINE, 1)

    # head shell
    rr(d, (36, 14, 92, 50), 13, white, OUTLINE, 2)
    rr(d, (41, 19, 87, 46), 10, white_hi, None)
    # black face screen
    rr(d, (44, 21, 84, 43), 7, screen, OUTLINE, 2)
    rr(d, (48, 25, 80, 39), 5, (10, 11, 13, 255), None)
    # two white eyes
    rr(d, (52, 28, 60, 36), 3, eye, None)
    rr(d, (68, 28, 76, 36), 3, eye, None)
    # orange ear discs
    for x in (28, 92):
        ellipse(d, (x, 26, x + 14, 42), orange, OUTLINE, 2)
        ellipse(d, (x + 4, 30, x + 10, 38), orange_hi, None)

    # left arm, hanging with gripper
    line(d, [(38, 64), (28, 82), (34, 96)], OUTLINE, 10)
    line(d, [(38, 64), (28, 82), (34, 96)], white, 6)
    ellipse(d, (26, 78, 36, 88), joint, OUTLINE, 1)
    ellipse(d, (28, 92, 38, 102), joint, OUTLINE, 1)
    # right arm, extended with a folded cloth
    line(d, [(90, 64), (104, 78), (110, 88)], OUTLINE, 10)
    line(d, [(90, 64), (104, 78), (110, 88)], white, 6)
    ellipse(d, (100, 74, 110, 84), joint, OUTLINE, 1)
    rr(d, (106, 86, 120, 96), 3, (200, 166, 112, 255), OUTLINE, 1)
    d.rectangle((P(109), P(89), P(117), P(92)), fill=(224, 190, 136, 255))
    # small gold wrist band
    d.rectangle((P(104), P(82), P(108), P(86)), fill=gold, outline=OUTLINE, width=P(1))
    # base front light
    ellipse(d, (60, 112, 68, 118), (244, 238, 214, 255), None)
    return im


def mkiii_ref_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    white = (241, 238, 232, 255)
    white_hi = (255, 253, 247, 255)
    white_lo = (203, 200, 194, 255)
    orange = (232, 94, 54, 255)
    screen = (18, 20, 22, 255)
    dark = (52, 55, 59, 255)
    joint = (91, 94, 98, 255)
    gold = (232, 180, 76, 255)
    ellipse(d, (18, 116, 110, 128), (0, 0, 0, 55))
    rr(d, (22, 100, 106, 118), 8, orange, OUTLINE, 2)
    rr(d, (28, 110, 100, 122), 4, (226, 222, 214, 255), OUTLINE, 1)
    for x in (30, 86):
        ellipse(d, (x, 108, x + 18, 124), (34, 36, 39, 255), OUTLINE, 2)
    rr(d, (34, 96, 94, 106), 4, white_lo, OUTLINE, 1)
    rr(d, (50, 88, 64, 106), 4, dark, OUTLINE, 1)
    rr(d, (68, 88, 82, 106), 4, dark, OUTLINE, 1)
    # back torso with orange battery panel
    rr(d, (38, 46, 90, 98), 12, white, OUTLINE, 2)
    rr(d, (43, 51, 85, 93), 9, white_hi, None)
    rr(d, (52, 58, 76, 88), 6, dark, OUTLINE, 1)
    rr(d, (56, 62, 72, 82), 4, orange, OUTLINE, 1)
    d.rectangle((P(60), P(66), P(68), P(70)), fill=gold, outline=OUTLINE, width=P(1))
    # neck / head back
    rr(d, (58, 42, 70, 50), 3, joint, OUTLINE, 1)
    rr(d, (36, 14, 92, 50), 13, white, OUTLINE, 2)
    rr(d, (41, 19, 87, 46), 10, white_hi, None)
    # rear vent
    rr(d, (48, 24, 80, 40), 4, screen, OUTLINE, 1)
    for x in (53, 60, 67, 74):
        d.rectangle((P(x), P(28), P(x + 3), P(36)), fill=(112, 111, 107, 255))
    for x in (28, 92):
        ellipse(d, (x, 26, x + 14, 42), orange, OUTLINE, 2)
    # arms from behind
    for x in (28, 100):
        line(d, [(x, 64), (x - 4, 82), (x, 96)], OUTLINE, 10)
        line(d, [(x, 64), (x - 4, 82), (x, 96)], white, 6)
        ellipse(d, (x - 5, 78, x + 5, 88), joint, OUTLINE, 1)
    return im


def mkiii_ref_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    white = (241, 238, 232, 255)
    white_hi = (255, 253, 247, 255)
    orange = (232, 94, 54, 255)
    orange_hi = (247, 128, 88, 255)
    screen = (18, 20, 22, 255)
    joint = (91, 94, 98, 255)
    eye = (244, 245, 240, 255)
    gold = (232, 180, 76, 255)
    ellipse(d, (20, 116, 108, 128), (0, 0, 0, 55))
    # wheeled base side
    rr(d, (20, 100, 108, 118), 8, orange, OUTLINE, 2)
    rr(d, (24, 96, 104, 108), 5, orange_hi, None)
    rr(d, (26, 110, 104, 122), 4, (226, 222, 214, 255), OUTLINE, 1)
    for x in (28, 84):
        ellipse(d, (x, 108, x + 18, 124), (34, 36, 39, 255), OUTLINE, 2)
        ellipse(d, (x + 4, 112, x + 14, 120), joint, OUTLINE, 1)
    # side torso
    rr(d, (42, 46, 94, 98), 12, white, OUTLINE, 2)
    rr(d, (47, 51, 89, 93), 9, white_hi, None)
    # front chest screen edge
    rr(d, (78, 58, 94, 90), 5, screen, OUTLINE, 2)
    d.rectangle((P(83), P(64), P(90), P(84)), fill=(10, 11, 13, 255))
    # shoulder pad
    rr(d, (60, 48, 82, 60), 4, orange, OUTLINE, 1)
    # gold badge
    rr(d, (54, 54, 64, 60), 2, gold, OUTLINE, 1)
    # side head
    rr(d, (36, 14, 96, 50), 13, white, OUTLINE, 2)
    rr(d, (41, 19, 91, 46), 10, white_hi, None)
    rr(d, (72, 21, 94, 43), 7, screen, OUTLINE, 2)
    rr(d, (78, 27, 91, 37), 4, eye, None)
    # orange ear disc
    ellipse(d, (44, 26, 58, 42), orange, OUTLINE, 2)
    ellipse(d, (48, 30, 54, 38), orange_hi, None)
    # neck
    rr(d, (62, 42, 74, 50), 3, joint, OUTLINE, 1)
    # extended arm
    line(d, [(88, 64), (104, 78), (112, 88)], OUTLINE, 10)
    line(d, [(88, 64), (104, 78), (112, 88)], white, 6)
    ellipse(d, (100, 74, 110, 84), joint, OUTLINE, 1)
    rr(d, (108, 86, 122, 96), 3, (200, 166, 112, 255), OUTLINE, 1)
    d.rectangle((P(111), P(89), P(119), P(92)), fill=(224, 190, 136, 255))
    return im


def mkiv_ref_south():
    """Mk-IV v2: dark outpost tower robot with binocular eyes and wheeled base."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (67, 69, 73, 255)
    shell_hi = (91, 94, 98, 255)
    shell_lo = (42, 44, 48, 255)
    orange = (232, 116, 48, 255)
    lens = (23, 25, 28, 255)
    lens_rim = (145, 148, 151, 255)
    pillow = (236, 229, 212, 255)
    pillow_lo = (200, 192, 176, 255)
    ellipse(d, (18, 116, 110, 128), (0, 0, 0, 55))

    # wheeled base
    rr(d, (24, 98, 104, 116), 7, shell, OUTLINE, 2)
    rr(d, (30, 104, 98, 112), 4, shell_hi, None)
    for x in (26, 84):
        ellipse(d, (x, 108, x + 18, 124), (30, 32, 35, 255), OUTLINE, 2)
        ellipse(d, (x + 4, 112, x + 14, 120), shell_hi, OUTLINE, 1)
    # orange hazard band
    for i in range(6):
        polygon(d, [(32 + i * 11, 114), (37 + i * 11, 114), (35 + i * 11, 108), (30 + i * 11, 108)], orange)
    # front drawer
    rr(d, (46, 108, 82, 120), 2, shell_lo, OUTLINE, 1)
    d.rectangle((P(52), P(112), P(76), P(114)), fill=orange)

    # tall column / body
    rr(d, (54, 44, 74, 102), 5, shell, OUTLINE, 2)
    rr(d, (58, 50, 70, 98), 3, shell_hi, None)
    d.rectangle((P(60), P(52), P(62), P(96)), fill=orange)
    for y in (58, 70, 82):
        d.rectangle((P(64), P(y), P(70), P(y + 2)), fill=shell_lo)

    # shoulder block
    rr(d, (46, 40, 82, 58), 5, shell, OUTLINE, 2)
    rr(d, (50, 44, 78, 54), 3, shell_hi, None)
    d.rectangle((P(56), P(43), P(72), P(46)), fill=shell_lo)

    # angular head with two binocular lenses
    polygon(d, [(48, 10), (78, 10), (86, 18), (86, 38), (78, 45), (48, 45), (40, 38), (40, 18)], shell, OUTLINE)
    polygon(d, [(52, 15), (74, 15), (80, 21), (80, 35), (74, 40), (52, 40), (46, 35), (46, 21)], shell_hi, OUTLINE)
    # binocular eyes
    for x in (54, 68):
        ellipse(d, (x, 22, x + 12, 34), lens_rim, OUTLINE, 1)
        ellipse(d, (x + 2, 24, x + 10, 32), lens, None)
        ellipse(d, (x + 4, 26, x + 8, 30), (135, 208, 230, 255), None)
    d.rectangle((P(52), P(37), P(80), P(40)), fill=shell_lo)
    # orange side markers
    for x in (38, 86):
        d.rectangle((P(x), P(24), P(x + 4), P(31)), fill=orange, outline=OUTLINE, width=P(1))

    # long articulated arms
    for side in (-1, 1):
        sx = 46 if side == -1 else 82
        ex = 44 if side == -1 else 84
        elbow = (32 if side == -1 else 96, 66)
        line(d, [(sx, 52), elbow, (ex, 84)], OUTLINE, 8)
        line(d, [(sx, 52), elbow, (ex, 84)], shell_hi, 4)
        ellipse(d, (elbow[0] - 5, elbow[1] - 5, elbow[0] + 5, elbow[1] + 5), shell_lo, OUTLINE, 1)
    # pillow held across the front
    rr(d, (42, 74, 86, 94), 10, pillow, OUTLINE, 2)
    rr(d, (46, 77, 82, 88), 7, pillow_lo, None)
    line(d, [(48, 80), (80, 80)], (251, 246, 232, 255), 2)
    line(d, [(48, 86), (80, 86)], (220, 212, 194, 255), 1)
    # gripper fingers clamp the pillow edges
    for x in (44, 84):
        line(d, [(x - 4, 82), (x - 1, 91)], OUTLINE, 2)
        line(d, [(x + 4, 82), (x + 1, 91)], OUTLINE, 2)
        ellipse(d, (x - 4, 88, x + 4, 94), shell_lo, OUTLINE, 1)
    return im


def mkiv_ref_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (67, 69, 73, 255)
    shell_hi = (91, 94, 98, 255)
    shell_lo = (42, 44, 48, 255)
    orange = (232, 116, 48, 255)
    ellipse(d, (18, 116, 110, 128), (0, 0, 0, 55))
    rr(d, (24, 98, 104, 116), 7, shell, OUTLINE, 2)
    rr(d, (30, 104, 98, 112), 4, shell_hi, None)
    for x in (26, 84):
        ellipse(d, (x, 108, x + 18, 124), (30, 32, 35, 255), OUTLINE, 2)
    for i in range(6):
        polygon(d, [(32 + i * 11, 114), (37 + i * 11, 114), (35 + i * 11, 108), (30 + i * 11, 108)], orange)
    # rear column
    rr(d, (54, 44, 74, 102), 5, shell, OUTLINE, 2)
    rr(d, (58, 50, 70, 98), 3, shell_hi, None)
    rr(d, (60, 56, 68, 92), 2, shell_lo, OUTLINE, 1)
    for y in (62, 74, 86):
        d.rectangle((P(61), P(y), P(67), P(y + 3)), fill=shell_hi)
    rr(d, (46, 40, 82, 58), 5, shell, OUTLINE, 2)
    # rear head
    polygon(d, [(48, 10), (78, 10), (86, 18), (86, 38), (78, 45), (48, 45), (40, 38), (40, 18)], shell, OUTLINE)
    polygon(d, [(52, 15), (74, 15), (80, 21), (80, 35), (74, 40), (52, 40), (46, 35), (46, 21)], shell_hi, OUTLINE)
    d.rectangle((P(54), P(22), P(74), P(36)), fill=shell_lo, outline=OUTLINE, width=P(1))
    for x in (57, 62, 67, 72):
        d.rectangle((P(x), P(25), P(x + 2), P(33)), fill=shell_hi)
    for x in (38, 86):
        d.rectangle((P(x), P(24), P(x + 4), P(31)), fill=orange, outline=OUTLINE, width=P(1))
    # rear arms
    for side in (-1, 1):
        sx = 46 if side == -1 else 82
        ex = 34 if side == -1 else 94
        line(d, [(sx, 52), (32 if side == -1 else 96, 66), (ex, 82)], OUTLINE, 8)
        line(d, [(sx, 52), (32 if side == -1 else 96, 66), (ex, 82)], shell_hi, 4)
        rr(d, (ex - 6, 80, ex + 6, 88), 2, shell_lo, OUTLINE, 1)
    return im


def mkiv_ref_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    shell = (67, 69, 73, 255)
    shell_hi = (91, 94, 98, 255)
    shell_lo = (42, 44, 48, 255)
    orange = (232, 116, 48, 255)
    lens = (23, 25, 28, 255)
    lens_rim = (145, 148, 151, 255)
    pillow = (236, 229, 212, 255)
    ellipse(d, (16, 116, 112, 128), (0, 0, 0, 55))
    # side base
    rr(d, (22, 100, 106, 118), 7, shell, OUTLINE, 2)
    rr(d, (28, 106, 100, 114), 4, shell_hi, None)
    for x in (30, 82):
        ellipse(d, (x, 110, x + 18, 126), (30, 32, 35, 255), OUTLINE, 2)
        ellipse(d, (x + 4, 114, x + 14, 122), shell_hi, OUTLINE, 1)
    for i in range(5):
        polygon(d, [(34 + i * 11, 116), (39 + i * 11, 116), (37 + i * 11, 110), (32 + i * 11, 110)], orange)
    # side column
    rr(d, (52, 44, 74, 102), 5, shell, OUTLINE, 2)
    rr(d, (57, 50, 69, 98), 3, shell_hi, None)
    d.rectangle((P(61), P(52), P(63), P(96)), fill=orange)
    # shoulder block
    rr(d, (46, 40, 82, 58), 5, shell, OUTLINE, 2)
    # side head, one lens visible
    polygon(d, [(50, 10), (80, 10), (88, 18), (88, 38), (80, 45), (50, 45), (42, 38), (42, 18)], shell, OUTLINE)
    polygon(d, [(54, 15), (76, 15), (82, 21), (82, 35), (76, 40), (54, 40), (48, 35), (48, 21)], shell_hi, OUTLINE)
    ellipse(d, (74, 22, 86, 34), lens_rim, OUTLINE, 1)
    ellipse(d, (76, 24, 84, 32), lens, None)
    ellipse(d, (78, 26, 82, 30), (135, 208, 230, 255), None)
    d.rectangle((P(54), P(37), P(82), P(40)), fill=shell_lo)
    # side arm and pillow
    line(d, [(76, 52), (96, 66), (90, 84)], OUTLINE, 8)
    line(d, [(76, 52), (96, 66), (90, 84)], shell_hi, 4)
    ellipse(d, (91, 61, 101, 71), shell_lo, OUTLINE, 1)
    rr(d, (54, 74, 96, 94), 10, pillow, OUTLINE, 2)
    rr(d, (58, 77, 92, 88), 7, (200, 192, 176, 255), None)
    # visible gripper fingers on the pillow edge
    line(d, [(86, 82), (89, 91)], OUTLINE, 2)
    line(d, [(94, 82), (91, 91)], OUTLINE, 2)
    ellipse(d, (86, 88, 94, 94), shell_lo, OUTLINE, 1)
    return im


def mkv_ref_south():
    """Mk-V v2: white-apron android with black ponytail, blue eyes and robotic arms."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (236, 202, 184, 255)
    skin_hi = (250, 224, 208, 255)
    hair = (28, 30, 34, 255)
    hair_hi = (58, 60, 66, 255)
    apron = (241, 239, 233, 255)
    apron_lo = (207, 205, 199, 255)
    silver = (185, 190, 194, 255)
    silver_hi = (226, 230, 232, 255)
    dark = (55, 58, 62, 255)
    blue = (83, 184, 225, 255)
    blue_hi = (190, 235, 250, 255)
    lip = (181, 100, 92, 255)
    ellipse(d, (24, 116, 104, 128), (0, 0, 0, 45))

    # legs / feet
    rr(d, (48, 104, 58, 122), 4, dark, OUTLINE, 1)
    rr(d, (70, 104, 80, 122), 4, dark, OUTLINE, 1)
    rr(d, (44, 118, 60, 124), 3, (35, 37, 40, 255), outline=OUTLINE, width=1)
    rr(d, (68, 118, 84, 124), 3, (35, 37, 40, 255), outline=OUTLINE, width=1)

    # apron / dress
    polygon(d, [(46, 62), (82, 62), (88, 112), (40, 112)], apron, OUTLINE)
    polygon(d, [(50, 68), (78, 68), (82, 108), (46, 108)], apron, None)
    line(d, [(50, 68), (50, 108)], apron_lo, 2)
    line(d, [(78, 68), (78, 108)], apron_lo, 2)
    # apron bib
    rr(d, (52, 54, 76, 70), 3, apron, OUTLINE, 1)
    # apron straps
    line(d, [(54, 56), (48, 46)], OUTLINE, 3)
    line(d, [(74, 56), (80, 46)], OUTLINE, 3)
    # dark under-suit at the sides
    rr(d, (42, 58, 50, 104), 3, dark, OUTLINE, 1)
    rr(d, (78, 58, 86, 104), 3, dark, OUTLINE, 1)

    # robotic shoulders
    for x in (32, 82):
        ellipse(d, (x, 54, x + 18, 70), silver, OUTLINE, 2)
        ellipse(d, (x + 5, 58, x + 13, 66), blue, OUTLINE, 1)
        ellipse(d, (x + 7, 60, x + 11, 64), blue_hi, None)
    # segmented arms
    for side in (-1, 1):
        sx = 40 if side == -1 else 88
        ex = 30 if side == -1 else 98
        line(d, [(sx, 64), (ex, 82), (ex + side * 6, 96)], OUTLINE, 9)
        line(d, [(sx, 64), (ex, 82), (ex + side * 6, 96)], silver, 5)
        ellipse(d, (ex - 5, 78, ex + 5, 88), silver_hi, OUTLINE, 1)
        ellipse(d, (ex + side * 6 - 5, 92, ex + side * 6 + 5, 102), silver_hi, OUTLINE, 1)
    # left hand holds a small bowl
    ellipse(d, (22, 98, 42, 110), (226, 222, 214, 255), OUTLINE, 1)
    ellipse(d, (25, 100, 39, 106), (118, 154, 96, 255), None)
    # right hand holds a kitchen knife
    line(d, [(94, 98), (104, 88)], OUTLINE, 4)
    line(d, [(94, 98), (104, 88)], silver_hi, 2)
    polygon(d, [(103, 84), (112, 88), (106, 96), (100, 92)], silver, OUTLINE)

    # mechanical neck
    rr(d, (56, 48, 72, 62), 3, silver, OUTLINE, 1)
    for y in (52, 56):
        d.rectangle((P(58), P(y), P(70), P(y + 2)), fill=dark)

    # head / face
    ellipse(d, (48, 18, 80, 54), skin, OUTLINE, 2)
    ellipse(d, (51, 21, 77, 45), skin_hi, None)
    # black hair cap and bangs
    polygon(d, [(47, 28), (47, 21), (52, 15), (76, 15), (81, 21), (81, 30), (74, 24), (68, 20), (58, 22), (52, 27)], hair, OUTLINE)
    # side hair
    line(d, [(48, 28), (46, 44)], hair, 4)
    line(d, [(80, 28), (82, 44)], hair, 4)
    # ponytail behind
    ellipse(d, (76, 18, 96, 34), hair, OUTLINE, 1)
    ellipse(d, (82, 22, 100, 58), hair, OUTLINE, 1)
    line(d, [(88, 26), (92, 50)], hair_hi, 2)
    # eyes
    for x in (56, 68):
        ellipse(d, (x, 31, x + 7, 38), (250, 250, 248, 255), None)
        ellipse(d, (x + 2, 32, x + 6, 37), blue, None)
        ellipse(d, (x + 3, 33, x + 5, 35), (20, 30, 40, 255), None)
    # brows / nose / mouth
    line(d, [(55, 28), (62, 27)], hair, 1)
    line(d, [(69, 27), (76, 28)], hair, 1)
    line(d, [(63, 38), (64, 41)], (198, 160, 145, 255), 1)
    line(d, [(60, 45), (68, 45)], lip, 2)
    # android cheek seams
    line(d, [(49, 36), (52, 42)], (190, 155, 140, 255), 1)
    line(d, [(79, 36), (76, 42)], (190, 155, 140, 255), 1)
    return im


def mkv_ref_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (236, 202, 184, 255)
    hair = (28, 30, 34, 255)
    hair_hi = (58, 60, 66, 255)
    apron = (241, 239, 233, 255)
    apron_lo = (207, 205, 199, 255)
    silver = (185, 190, 194, 255)
    dark = (55, 58, 62, 255)
    blue = (83, 184, 225, 255)
    ellipse(d, (24, 116, 104, 128), (0, 0, 0, 45))
    rr(d, (48, 104, 58, 122), 4, dark, OUTLINE, 1)
    rr(d, (70, 104, 80, 122), 4, dark, OUTLINE, 1)
    # back of apron / ties
    polygon(d, [(46, 62), (82, 62), (88, 112), (40, 112)], apron, OUTLINE)
    polygon(d, [(50, 68), (78, 68), (82, 108), (46, 108)], apron, None)
    line(d, [(58, 70), (64, 86), (70, 70)], apron_lo, 2)
    line(d, [(64, 86), (64, 104)], apron_lo, 2)
    # under-suit
    rr(d, (42, 58, 50, 104), 3, dark, OUTLINE, 1)
    rr(d, (78, 58, 86, 104), 3, dark, OUTLINE, 1)
    # shoulders
    for x in (32, 82):
        ellipse(d, (x, 54, x + 18, 70), silver, OUTLINE, 2)
        ellipse(d, (x + 5, 58, x + 13, 66), blue, OUTLINE, 1)
    # arms from behind
    for side in (-1, 1):
        sx = 40 if side == -1 else 88
        ex = 30 if side == -1 else 98
        line(d, [(sx, 64), (ex, 82), (ex + side * 6, 96)], OUTLINE, 9)
        line(d, [(sx, 64), (ex, 82), (ex + side * 6, 96)], silver, 5)
        ellipse(d, (ex + side * 6 - 5, 92, ex + side * 6 + 5, 102), silver, OUTLINE, 1)
    # neck
    rr(d, (56, 48, 72, 62), 3, silver, OUTLINE, 1)
    # back of head / hair
    ellipse(d, (48, 18, 80, 54), skin, OUTLINE, 2)
    ellipse(d, (49, 17, 81, 50), hair, OUTLINE, 2)
    ellipse(d, (75, 18, 97, 44), hair, OUTLINE, 1)
    ellipse(d, (80, 22, 98, 60), hair, OUTLINE, 1)
    line(d, [(54, 24), (72, 20)], hair_hi, 2)
    line(d, [(58, 44), (64, 52), (70, 44)], (190, 155, 140, 255), 1)
    return im


def mkv_ref_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (236, 202, 184, 255)
    skin_hi = (250, 224, 208, 255)
    hair = (28, 30, 34, 255)
    hair_hi = (58, 60, 66, 255)
    apron = (241, 239, 233, 255)
    apron_lo = (207, 205, 199, 255)
    silver = (185, 190, 194, 255)
    silver_hi = (226, 230, 232, 255)
    dark = (55, 58, 62, 255)
    blue = (83, 184, 225, 255)
    blue_hi = (190, 235, 250, 255)
    lip = (181, 100, 92, 255)
    ellipse(d, (26, 116, 102, 128), (0, 0, 0, 45))
    rr(d, (50, 104, 60, 122), 4, dark, OUTLINE, 1)
    rr(d, (72, 104, 82, 122), 4, dark, OUTLINE, 1)
    # side apron / dress
    polygon(d, [(48, 62), (80, 62), (86, 112), (44, 112)], apron, OUTLINE)
    polygon(d, [(52, 68), (76, 68), (80, 108), (50, 108)], apron, None)
    line(d, [(54, 70), (54, 106)], apron_lo, 2)
    # side dark under-suit
    rr(d, (44, 58, 52, 104), 3, dark, OUTLINE, 1)
    # shoulder / arm
    ellipse(d, (78, 54, 96, 70), silver, OUTLINE, 2)
    ellipse(d, (83, 58, 91, 66), blue, OUTLINE, 1)
    ellipse(d, (85, 60, 89, 64), blue_hi, None)
    line(d, [(88, 64), (102, 82), (98, 96)], OUTLINE, 9)
    line(d, [(88, 64), (102, 82), (98, 96)], silver, 5)
    ellipse(d, (97, 78, 107, 88), silver_hi, OUTLINE, 1)
    # knife
    line(d, [(98, 98), (108, 88)], OUTLINE, 4)
    line(d, [(98, 98), (108, 88)], silver_hi, 2)
    polygon(d, [(107, 84), (116, 88), (110, 96), (104, 92)], silver, OUTLINE)
    # neck
    rr(d, (56, 48, 72, 62), 3, silver, OUTLINE, 1)
    # side head / face
    ellipse(d, (46, 18, 80, 54), skin, OUTLINE, 2)
    ellipse(d, (50, 21, 77, 45), skin_hi, None)
    # hair cap, bangs and ponytail behind
    polygon(d, [(46, 28), (46, 21), (52, 15), (76, 15), (82, 22), (82, 30), (74, 23), (66, 19), (56, 22), (50, 27)], hair, OUTLINE)
    ellipse(d, (34, 20, 56, 46), hair, OUTLINE, 1)
    ellipse(d, (26, 26, 44, 66), hair, OUTLINE, 1)
    line(d, [(36, 32), (32, 56)], hair_hi, 2)
    # visible eye, nose, mouth
    ellipse(d, (66, 31, 74, 38), (250, 250, 248, 255), None)
    ellipse(d, (68, 32, 72, 37), blue, None)
    ellipse(d, (69, 33, 71, 35), (20, 30, 40, 255), None)
    line(d, [(64, 27), (73, 27)], hair, 1)
    line(d, [(74, 38), (76, 41)], (198, 160, 145, 255), 1)
    line(d, [(70, 45), (77, 45)], lip, 2)
    line(d, [(47, 36), (50, 42)], (190, 155, 140, 255), 1)
    return im


def mkv8_south():
    """Mk-V v3: refined service android with layered hair, tailored apron and articulated arms."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (238, 205, 187, 255)
    skin_hi = (252, 228, 212, 255)
    skin_lo = (210, 170, 154, 255)
    hair = (26, 28, 32, 255)
    hair_hi = (67, 69, 76, 255)
    apron = (244, 242, 236, 255)
    apron_hi = (255, 254, 250, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (183, 189, 193, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (52, 55, 60, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (179, 95, 92, 255)
    rose = (214, 145, 138, 255)
    food_green = (104, 142, 83, 255)
    food_orange = (212, 124, 64, 255)
    ellipse(d, (22, 116, 106, 128), (0, 0, 0, 50))

    # feet and lower legs
    for x in (50, 70):
        rr(d, (x, 104, x + 8, 119), 4, dark, OUTLINE, 1)
        rr(d, (x - 3, 117, x + 11, 125), 4, (36, 38, 42, 255), OUTLINE, 1)
        d.rectangle((P(x - 1), P(120), P(x + 9), P(122)), fill=silver, outline=OUTLINE, width=P(1))

    # ponytail and back hair behind the body
    polygon(d, [(76, 16), (92, 18), (98, 34), (94, 58), (86, 64), (84, 42), (78, 30)], hair, OUTLINE)
    line(d, [(86, 24), (92, 38), (91, 54)], hair_hi, 2)
    ellipse(d, (76, 14, 94, 32), hair, OUTLINE, 1)

    # fitted under-suit
    rr(d, (48, 54, 80, 106), 8, dark, OUTLINE, 2)
    # waist seam
    rr(d, (48, 74, 80, 79), 2, silver, OUTLINE, 1)
    d.rectangle((P(56), P(75), P(72), P(77)), fill=silver_hi)

    # apron skirt with side shading
    polygon(d, [(46, 78), (82, 78), (89, 116), (39, 116)], apron, OUTLINE)
    polygon(d, [(46, 78), (52, 78), (48, 116), (39, 116)], apron_lo, None)
    polygon(d, [(78, 78), (82, 78), (89, 116), (80, 116)], apron_lo, None)
    polygon(d, [(53, 80), (75, 80), (79, 111), (49, 111)], apron_hi, None)
    line(d, [(48, 108), (80, 108)], apron_lo, 1)
    # apron pocket
    rr(d, (58, 88, 74, 98), 2, apron_lo, OUTLINE, 1)
    line(d, [(58, 89), (74, 89)], apron_hi, 1)

    # bib and straps
    rr(d, (54, 52, 74, 74), 3, apron, OUTLINE, 1)
    line(d, [(54, 53), (48, 43)], OUTLINE, 3)
    line(d, [(74, 53), (80, 43)], OUTLINE, 3)
    d.rectangle((P(61), P(56), P(67), P(59)), fill=apron_hi)
    d.rectangle((P(63), P(68), P(65), P(71)), fill=silver)
    # apron waist tie
    line(d, [(46, 78), (82, 78)], OUTLINE, 2)

    # robotic shoulder caps
    for x in (32, 84):
        ellipse(d, (x, 54, x + 16, 70), silver, OUTLINE, 2)
        ellipse(d, (x + 4, 58, x + 12, 66), dark, OUTLINE, 1)
        ellipse(d, (x + 6, 60, x + 10, 64), blue, None)
        ellipse(d, (x + 7, 61, x + 9, 63), blue_hi, None)

    # articulated arms: left bent to tray, right holding a knife
    # upper arms
    line(d, [(40, 64), (32, 80)], OUTLINE, 9)
    line(d, [(40, 64), (32, 80)], silver, 5)
    line(d, [(88, 64), (96, 80)], OUTLINE, 9)
    line(d, [(88, 64), (96, 80)], silver, 5)
    # elbows
    ellipse(d, (27, 76, 37, 86), silver_hi, OUTLINE, 1)
    ellipse(d, (91, 76, 101, 86), silver_hi, OUTLINE, 1)
    # forearms
    line(d, [(32, 80), (36, 94)], OUTLINE, 8)
    line(d, [(32, 80), (36, 94)], silver, 4)
    line(d, [(96, 80), (92, 94)], OUTLINE, 8)
    line(d, [(96, 80), (92, 94)], silver, 4)
    # wrists / hands
    rr(d, (32, 92, 42, 102), 2, silver, OUTLINE, 1)
    rr(d, (86, 92, 96, 102), 2, silver, OUTLINE, 1)
    # fingers
    for i in range(3):
        line(d, [(35 + i * 3, 101), (35 + i * 3, 105)], OUTLINE, 1)
        line(d, [(88 + i * 3, 101), (88 + i * 3, 105)], OUTLINE, 1)

    # tray with food in the left hand
    ellipse(d, (24, 94, 58, 106), (230, 227, 220, 255), OUTLINE, 2)
    ellipse(d, (28, 96, 54, 102), (250, 248, 241, 255), None)
    ellipse(d, (32, 96, 38, 101), food_green, None)
    ellipse(d, (40, 95, 46, 101), food_orange, None)
    ellipse(d, (46, 97, 50, 101), food_green, None)
    # knife in the right hand
    line(d, [(92, 102), (100, 112)], OUTLINE, 3)
    line(d, [(92, 102), (100, 112)], silver_hi, 2)
    polygon(d, [(98, 110), (106, 112), (104, 118), (96, 114)], silver, OUTLINE)
    d.rectangle((P(91), P(99), P(95), P(104)), fill=dark, outline=OUTLINE, width=P(1))

    # mechanical neck
    rr(d, (56, 46, 72, 58), 3, silver, OUTLINE, 1)
    for y in (49, 53):
        d.rectangle((P(58), P(y), P(70), P(y + 2)), fill=dark)
    # neck collar
    rr(d, (54, 53, 74, 58), 2, silver_hi, OUTLINE, 1)

    # head / face
    ellipse(d, (50, 16, 78, 48), skin, OUTLINE, 2)
    ellipse(d, (54, 20, 74, 44), skin_hi, None)
    ellipse(d, (76, 22, 80, 44), skin_lo, None)
    # hair cap and side locks
    ellipse(d, (47, 12, 80, 36), hair, OUTLINE, 2)
    ellipse(d, (48, 14, 64, 34), hair, None)
    polygon(d, [(48, 24), (52, 16), (78, 14), (82, 22), (74, 20), (66, 27), (58, 21), (51, 27)], hair, OUTLINE)
    ellipse(d, (46, 24, 52, 40), hair, OUTLINE, 1)
    ellipse(d, (77, 24, 83, 42), hair, OUTLINE, 1)
    line(d, [(54, 16), (58, 24)], hair_hi, 1)
    # android ear / cheek seam
    ellipse(d, (47, 31, 51, 38), silver, OUTLINE, 1)
    line(d, [(51, 34), (53, 41)], (196, 158, 144, 255), 1)
    # eyes
    for x in (56, 67):
        ellipse(d, (x, 29, x + 8, 36), (249, 249, 246, 255), None)
        ellipse(d, (x + 2, 30, x + 6, 35), blue, None)
        ellipse(d, (x + 3, 31, x + 5, 34), (24, 35, 50, 255), None)
        line(d, [(x - 1, 28), (x + 8, 27)], hair, 1)
        line(d, [(x + 1, 37), (x + 8, 37)], rose, 1)
    # nose / lips / chin shadow
    line(d, [(63, 36), (64, 39)], (198, 160, 145, 255), 1)
    line(d, [(60, 42), (68, 42)], lip, 2)
    d.rectangle((P(62), P(42), P(66), P(43)), fill=lip)
    ellipse(d, (60, 45, 68, 49), (224, 184, 168, 255), None)
    # hair strand over the forehead
    line(d, [(61, 20), (66, 24), (72, 22)], hair_hi, 1)
    # blue hair ornament
    ellipse(d, (75, 16, 81, 22), blue, OUTLINE, 1)
    ellipse(d, (77, 18, 79, 20), blue_hi, None)
    return im


def mkv8_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (238, 205, 187, 255)
    hair = (26, 28, 32, 255)
    hair_hi = (67, 69, 76, 255)
    apron = (244, 242, 236, 255)
    apron_hi = (255, 254, 250, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (183, 189, 193, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (52, 55, 60, 255)
    blue = (74, 176, 220, 255)
    ellipse(d, (22, 116, 106, 128), (0, 0, 0, 50))
    for x in (50, 70):
        rr(d, (x, 104, x + 8, 119), 4, dark, OUTLINE, 1)
        rr(d, (x - 3, 117, x + 11, 125), 4, (36, 38, 42, 255), OUTLINE, 1)
    # back hair and ponytail
    ellipse(d, (46, 12, 82, 42), hair, OUTLINE, 2)
    polygon(d, [(76, 16), (94, 20), (100, 36), (94, 60), (84, 66), (82, 44), (78, 30)], hair, OUTLINE)
    line(d, [(86, 26), (93, 40), (91, 55)], hair_hi, 2)
    # back apron
    polygon(d, [(46, 78), (82, 78), (89, 116), (39, 116)], apron, OUTLINE)
    polygon(d, [(52, 80), (76, 80), (79, 111), (49, 111)], apron_hi, None)
    line(d, [(48, 108), (80, 108)], apron_lo, 1)
    # apron ties / bow
    line(d, [(48, 56), (44, 70)], OUTLINE, 3)
    line(d, [(80, 56), (84, 70)], OUTLINE, 3)
    ellipse(d, (58, 76, 64, 82), apron, OUTLINE, 1)
    polygon(d, [(58, 78), (50, 74), (50, 82)], apron, OUTLINE)
    polygon(d, [(70, 78), (78, 74), (78, 82)], apron, OUTLINE)
    # shoulders
    for x in (32, 84):
        ellipse(d, (x, 54, x + 16, 70), silver, OUTLINE, 2)
        ellipse(d, (x + 6, 60, x + 10, 64), blue, None)
    # arms from behind
    for sx, ex in ((40, 36), (88, 92)):
        line(d, [(sx, 64), (ex, 80), (ex + 4, 96)], OUTLINE, 9)
        line(d, [(sx, 64), (ex, 80), (ex + 4, 96)], silver, 5)
        ellipse(d, (ex + 4 - 4, 92, ex + 4 + 4, 100), silver, OUTLINE, 1)
    # neck
    rr(d, (56, 46, 72, 58), 3, silver, OUTLINE, 1)
    # back of head
    ellipse(d, (50, 16, 78, 48), skin, OUTLINE, 2)
    ellipse(d, (48, 12, 82, 44), hair, OUTLINE, 2)
    ellipse(d, (56, 40, 72, 48), (214, 178, 162, 255), None)
    # hair highlight / ornament
    line(d, [(54, 18), (68, 14), (78, 22)], hair_hi, 2)
    ellipse(d, (74, 16, 80, 22), blue, OUTLINE, 1)
    return im


def mkv8_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (238, 205, 187, 255)
    skin_hi = (252, 228, 212, 255)
    skin_lo = (210, 170, 154, 255)
    hair = (26, 28, 32, 255)
    hair_hi = (67, 69, 76, 255)
    apron = (244, 242, 236, 255)
    apron_hi = (255, 254, 250, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (183, 189, 193, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (52, 55, 60, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (179, 95, 92, 255)
    rose = (214, 145, 138, 255)
    food_green = (104, 142, 83, 255)
    food_orange = (212, 124, 64, 255)
    ellipse(d, (24, 116, 104, 128), (0, 0, 0, 50))
    # feet
    for x in (50, 70):
        rr(d, (x, 104, x + 8, 119), 4, dark, OUTLINE, 1)
        rr(d, (x - 3, 117, x + 11, 125), 4, (36, 38, 42, 255), OUTLINE, 1)
    # ponytail behind
    polygon(d, [(56, 16), (40, 20), (32, 36), (36, 62), (46, 66), (48, 44), (52, 30)], hair, OUTLINE)
    line(d, [(44, 28), (38, 42), (40, 56)], hair_hi, 2)
    # side body / apron
    polygon(d, [(48, 78), (80, 78), (85, 116), (44, 116)], apron, OUTLINE)
    polygon(d, [(54, 80), (76, 80), (79, 111), (50, 111)], apron_hi, None)
    line(d, [(46, 108), (81, 108)], apron_lo, 1)
    rr(d, (58, 88, 74, 98), 2, apron_lo, OUTLINE, 1)
    # side under-suit
    rr(d, (46, 54, 78, 106), 7, dark, OUTLINE, 2)
    # shoulder and arm
    ellipse(d, (78, 54, 94, 70), silver, OUTLINE, 2)
    ellipse(d, (82, 58, 90, 66), dark, OUTLINE, 1)
    ellipse(d, (84, 60, 88, 64), blue, None)
    line(d, [(86, 64), (98, 80), (94, 94)], OUTLINE, 9)
    line(d, [(86, 64), (98, 80), (94, 94)], silver, 5)
    ellipse(d, (93, 76, 103, 86), silver_hi, OUTLINE, 1)
    rr(d, (90, 92, 100, 102), 2, silver, OUTLINE, 1)
    # tray and food
    ellipse(d, (50, 94, 84, 106), (230, 227, 220, 255), OUTLINE, 2)
    ellipse(d, (54, 96, 80, 102), (250, 248, 241, 255), None)
    ellipse(d, (58, 96, 64, 101), food_green, None)
    ellipse(d, (66, 95, 72, 101), food_orange, None)
    # knife at the side
    line(d, [(98, 102), (106, 112)], OUTLINE, 3)
    line(d, [(98, 102), (106, 112)], silver_hi, 2)
    polygon(d, [(104, 110), (112, 112), (110, 118), (102, 114)], silver, OUTLINE)
    # neck
    rr(d, (56, 46, 72, 58), 3, silver, OUTLINE, 1)
    # side head / face
    ellipse(d, (50, 16, 80, 48), skin, OUTLINE, 2)
    ellipse(d, (54, 20, 76, 44), skin_hi, None)
    ellipse(d, (76, 22, 80, 44), skin_lo, None)
    # hair cap, bangs, ponytail
    ellipse(d, (46, 12, 82, 38), hair, OUTLINE, 2)
    polygon(d, [(46, 24), (50, 16), (78, 14), (84, 22), (76, 20), (68, 27), (60, 21), (52, 27)], hair, OUTLINE)
    ellipse(d, (42, 22, 50, 42), hair, OUTLINE, 1)
    # eye
    ellipse(d, (66, 29, 74, 36), (249, 249, 246, 255), None)
    ellipse(d, (68, 30, 72, 35), blue, None)
    ellipse(d, (69, 31, 71, 34), (24, 35, 50, 255), None)
    line(d, [(64, 27), (74, 27)], hair, 1)
    line(d, [(65, 37), (73, 37)], rose, 1)
    # nose / lips
    line(d, [(78, 35), (80, 38)], (198, 160, 145, 255), 1)
    line(d, [(72, 42), (79, 42)], lip, 2)
    # cheek seam / ornament
    line(d, [(50, 34), (52, 41)], (196, 158, 144, 255), 1)
    ellipse(d, (44, 16, 50, 22), blue, OUTLINE, 1)
    return im


def mkv9_south():
    """Mk-V v4: full-body slim service android, elegant apron and articulated arms."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (239, 207, 189, 255)
    skin_hi = (253, 230, 215, 255)
    skin_lo = (213, 173, 157, 255)
    hair = (27, 29, 33, 255)
    hair_hi = (64, 66, 72, 255)
    apron = (245, 243, 237, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (205, 202, 194, 255)
    silver = (184, 190, 194, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (181, 96, 92, 255)
    rose = (217, 148, 140, 255)
    food_green = (102, 141, 82, 255)
    food_orange = (210, 122, 63, 255)
    ellipse(d, (22, 118, 106, 128), (0, 0, 0, 45))

    # high ponytail behind the head, not covering the face
    polygon(d, [(74, 14), (86, 16), (92, 28), (90, 43), (82, 48), (78, 32)], hair, OUTLINE)
    line(d, [(80, 20), (86, 30), (84, 42)], hair_hi, 2)
    ellipse(d, (72, 12, 86, 24), hair, OUTLINE, 1)

    # slim legs and shoes
    rr(d, (54, 104, 60, 120), 3, dark, OUTLINE, 1)
    rr(d, (68, 104, 74, 120), 3, dark, OUTLINE, 1)
    rr(d, (50, 117, 62, 125), 3, (36, 38, 42, 255), OUTLINE, 1)
    rr(d, (66, 117, 78, 125), 3, (36, 38, 42, 255), OUTLINE, 1)
    d.rectangle((P(51), P(120), P(60), P(122)), fill=silver, outline=OUTLINE, width=P(1))
    d.rectangle((P(68), P(120), P(77), P(122)), fill=silver, outline=OUTLINE, width=P(1))

    # dark under-suit torso
    rr(d, (54, 46, 74, 82), 6, dark, OUTLINE, 2)
    # fitted apron skirt
    polygon(d, [(50, 68), (78, 68), (84, 108), (44, 108)], apron, OUTLINE)
    polygon(d, [(50, 68), (55, 68), (50, 108), (44, 108)], apron_lo, None)
    polygon(d, [(75, 68), (78, 68), (84, 108), (78, 108)], apron_lo, None)
    polygon(d, [(56, 71), (73, 71), (78, 104), (50, 104)], apron_hi, None)
    # apron pocket / hem
    rr(d, (58, 82, 72, 94), 2, apron_lo, OUTLINE, 1)
    line(d, [(58, 83), (72, 83)], apron_hi, 1)
    line(d, [(48, 102), (80, 102)], apron_lo, 1)
    # waist belt
    rr(d, (52, 64, 76, 69), 2, silver, OUTLINE, 1)
    d.rectangle((P(58), P(65), P(70), P(67)), fill=silver_hi)

    # apron bib and straps
    rr(d, (56, 48, 72, 66), 3, apron, OUTLINE, 1)
    line(d, [(57, 49), (52, 40)], OUTLINE, 3)
    line(d, [(71, 49), (76, 40)], OUTLINE, 3)
    d.rectangle((P(61), P(52), P(67), P(55)), fill=apron_hi)
    d.rectangle((P(63), P(61), P(65), P(64)), fill=silver)

    # shoulder caps
    for x in (42, 74):
        ellipse(d, (x, 46, x + 16, 62), silver, OUTLINE, 2)
        ellipse(d, (x + 4, 50, x + 12, 58), dark, OUTLINE, 1)
        ellipse(d, (x + 6, 52, x + 10, 56), blue, None)
        ellipse(d, (x + 7, 53, x + 9, 55), blue_hi, None)

    # left arm bent to tray
    line(d, [(50, 54), (42, 72), (48, 88)], OUTLINE, 8)
    line(d, [(50, 54), (42, 72), (48, 88)], silver, 4)
    ellipse(d, (39, 68, 47, 76), silver_hi, OUTLINE, 1)
    rr(d, (45, 86, 53, 94), 2, silver, OUTLINE, 1)
    # right arm to knife
    line(d, [(76, 54), (85, 72), (81, 88)], OUTLINE, 8)
    line(d, [(76, 54), (85, 72), (81, 88)], silver, 4)
    ellipse(d, (82, 68, 90, 76), silver_hi, OUTLINE, 1)
    rr(d, (78, 86, 86, 94), 2, silver, OUTLINE, 1)

    # tray with food
    ellipse(d, (32, 92, 64, 103), (231, 228, 220, 255), OUTLINE, 2)
    ellipse(d, (36, 94, 60, 100), (252, 250, 243, 255), None)
    ellipse(d, (40, 94, 46, 99), food_green, None)
    ellipse(d, (48, 93, 54, 99), food_orange, None)
    ellipse(d, (53, 96, 57, 100), food_green, None)
    # knife
    line(d, [(84, 94), (92, 106)], OUTLINE, 3)
    line(d, [(84, 94), (92, 106)], silver_hi, 2)
    polygon(d, [(90, 104), (98, 106), (96, 112), (88, 108)], silver, OUTLINE)
    d.rectangle((P(82), P(90), P(86), P(96)), fill=dark, outline=OUTLINE, width=P(1))

    # neck
    rr(d, (58, 42, 70, 50), 3, silver, OUTLINE, 1)
    d.rectangle((P(60), P(44), P(68), P(46)), fill=dark)

    # head / face
    ellipse(d, (52, 15, 76, 43), skin, OUTLINE, 2)
    ellipse(d, (55, 18, 73, 40), skin_hi, None)
    ellipse(d, (73, 20, 77, 39), skin_lo, None)
    # hair cap, bangs and side locks
    ellipse(d, (49, 11, 79, 32), hair, OUTLINE, 2)
    polygon(d, [(49, 23), (52, 15), (77, 13), (81, 21), (73, 19), (66, 25), (59, 20), (52, 25)], hair, OUTLINE)
    ellipse(d, (49, 22, 53, 36), hair, OUTLINE, 1)
    ellipse(d, (75, 22, 79, 36), hair, OUTLINE, 1)
    line(d, [(56, 15), (60, 22), (66, 20)], hair_hi, 1)
    # blue hair tie
    ellipse(d, (76, 14, 82, 20), blue, OUTLINE, 1)
    ellipse(d, (78, 16, 80, 18), blue_hi, None)
    # android ear / cheek seam
    ellipse(d, (48, 29, 52, 36), silver, OUTLINE, 1)
    line(d, [(52, 33), (54, 39)], (197, 159, 145, 255), 1)
    # eyes
    for x in (56, 66):
        ellipse(d, (x, 27, x + 7, 34), (249, 249, 246, 255), None)
        ellipse(d, (x + 2, 28, x + 6, 33), blue, None)
        ellipse(d, (x + 3, 29, x + 5, 32), (24, 35, 50, 255), None)
        line(d, [(x - 1, 26), (x + 7, 25)], hair, 1)
        line(d, [(x + 1, 35), (x + 7, 35)], rose, 1)
    # nose, lips, chin
    line(d, [(63, 34), (64, 37)], (198, 160, 145, 255), 1)
    line(d, [(60, 39), (67, 39)], lip, 2)
    ellipse(d, (61, 41, 67, 44), (224, 184, 168, 255), None)
    return im


def mkv9_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    hair = (27, 29, 33, 255)
    hair_hi = (64, 66, 72, 255)
    apron = (245, 243, 237, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (205, 202, 194, 255)
    silver = (184, 190, 194, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    skin = (239, 207, 189, 255)
    ellipse(d, (22, 118, 106, 128), (0, 0, 0, 45))
    for x in (54, 68):
        rr(d, (x, 104, x + 6, 120), 3, dark, OUTLINE, 1)
        rr(d, (x - 3, 117, x + 9, 125), 3, (36, 38, 42, 255), OUTLINE, 1)
    # back hair and ponytail
    ellipse(d, (48, 11, 80, 40), hair, OUTLINE, 2)
    polygon(d, [(72, 14), (86, 18), (92, 30), (88, 46), (80, 50), (78, 32)], hair, OUTLINE)
    line(d, [(78, 20), (86, 30), (84, 42)], hair_hi, 2)
    # apron back
    polygon(d, [(50, 68), (78, 68), (84, 108), (44, 108)], apron, OUTLINE)
    polygon(d, [(56, 71), (73, 71), (78, 104), (50, 104)], apron_hi, None)
    line(d, [(48, 102), (80, 102)], apron_lo, 1)
    # waist bow
    line(d, [(52, 58), (46, 68)], OUTLINE, 3)
    line(d, [(76, 58), (82, 68)], OUTLINE, 3)
    ellipse(d, (58, 68, 64, 74), apron, OUTLINE, 1)
    polygon(d, [(58, 70), (50, 66), (50, 74)], apron, OUTLINE)
    polygon(d, [(70, 70), (78, 66), (78, 74)], apron, OUTLINE)
    # shoulders / arms
    for x in (42, 74):
        ellipse(d, (x, 46, x + 16, 62), silver, OUTLINE, 2)
        ellipse(d, (x + 6, 52, x + 10, 56), blue, None)
    for sx, ex in ((50, 44), (76, 82)):
        line(d, [(sx, 54), (ex, 72), (ex + 4, 88)], OUTLINE, 8)
        line(d, [(sx, 54), (ex, 72), (ex + 4, 88)], silver, 4)
    # neck
    rr(d, (58, 42, 70, 50), 3, silver, OUTLINE, 1)
    # back of head
    ellipse(d, (52, 15, 76, 43), skin, OUTLINE, 2)
    ellipse(d, (49, 11, 79, 40), hair, OUTLINE, 2)
    line(d, [(56, 16), (70, 13), (78, 20)], hair_hi, 2)
    ellipse(d, (76, 14, 82, 20), blue, OUTLINE, 1)
    return im


def mkv9_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (239, 207, 189, 255)
    skin_hi = (253, 230, 215, 255)
    skin_lo = (213, 173, 157, 255)
    hair = (27, 29, 33, 255)
    hair_hi = (64, 66, 72, 255)
    apron = (245, 243, 237, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (205, 202, 194, 255)
    silver = (184, 190, 194, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (181, 96, 92, 255)
    rose = (217, 148, 140, 255)
    food_green = (102, 141, 82, 255)
    food_orange = (210, 122, 63, 255)
    ellipse(d, (24, 118, 104, 128), (0, 0, 0, 45))
    # legs
    for x in (54, 68):
        rr(d, (x, 104, x + 6, 120), 3, dark, OUTLINE, 1)
        rr(d, (x - 3, 117, x + 9, 125), 3, (36, 38, 42, 255), OUTLINE, 1)
    # ponytail behind
    polygon(d, [(54, 14), (40, 20), (34, 34), (38, 48), (46, 50), (48, 34), (52, 24)], hair, OUTLINE)
    line(d, [(42, 26), (38, 36), (40, 46)], hair_hi, 2)
    # apron / body
    polygon(d, [(50, 68), (78, 68), (84, 108), (44, 108)], apron, OUTLINE)
    polygon(d, [(56, 71), (73, 71), (78, 104), (50, 104)], apron_hi, None)
    line(d, [(48, 102), (80, 102)], apron_lo, 1)
    rr(d, (58, 82, 72, 94), 2, apron_lo, OUTLINE, 1)
    rr(d, (54, 46, 74, 82), 6, dark, OUTLINE, 2)
    # shoulder / arm
    ellipse(d, (74, 46, 90, 62), silver, OUTLINE, 2)
    ellipse(d, (78, 50, 86, 58), dark, OUTLINE, 1)
    ellipse(d, (80, 52, 84, 56), blue, None)
    line(d, [(80, 54), (90, 72), (86, 88)], OUTLINE, 8)
    line(d, [(80, 54), (90, 72), (86, 88)], silver, 4)
    ellipse(d, (87, 68, 95, 76), silver_hi, OUTLINE, 1)
    rr(d, (83, 86, 91, 94), 2, silver, OUTLINE, 1)
    # tray / food
    ellipse(d, (48, 92, 80, 103), (231, 228, 220, 255), OUTLINE, 2)
    ellipse(d, (52, 94, 76, 100), (252, 250, 243, 255), None)
    ellipse(d, (56, 94, 62, 99), food_green, None)
    ellipse(d, (64, 93, 70, 99), food_orange, None)
    # knife
    line(d, [(88, 94), (96, 106)], OUTLINE, 3)
    line(d, [(88, 94), (96, 106)], silver_hi, 2)
    polygon(d, [(94, 104), (102, 106), (100, 112), (92, 108)], silver, OUTLINE)
    # neck
    rr(d, (58, 42, 70, 50), 3, silver, OUTLINE, 1)
    # side head
    ellipse(d, (52, 15, 78, 43), skin, OUTLINE, 2)
    ellipse(d, (56, 18, 74, 40), skin_hi, None)
    ellipse(d, (76, 20, 80, 39), skin_lo, None)
    # hair cap, bangs, ponytail
    ellipse(d, (48, 11, 82, 36), hair, OUTLINE, 2)
    polygon(d, [(48, 23), (52, 15), (77, 13), (83, 21), (75, 19), (68, 26), (60, 20), (52, 25)], hair, OUTLINE)
    ellipse(d, (44, 20, 50, 38), hair, OUTLINE, 1)
    ellipse(d, (76, 14, 82, 20), blue, OUTLINE, 1)
    # eye / nose / lips
    ellipse(d, (66, 27, 74, 34), (249, 249, 246, 255), None)
    ellipse(d, (68, 28, 72, 33), blue, None)
    ellipse(d, (69, 29, 71, 32), (24, 35, 50, 255), None)
    line(d, [(64, 25), (74, 25)], hair, 1)
    line(d, [(65, 35), (73, 35)], rose, 1)
    line(d, [(77, 33), (79, 36)], (198, 160, 145, 255), 1)
    line(d, [(72, 39), (79, 39)], lip, 2)
    line(d, [(52, 32), (54, 39)], (197, 159, 145, 255), 1)
    return im


def mkv10_south():
    """Mk-V v5: refined small-head full-body android, long apron line and slim limbs."""
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (239, 207, 189, 255)
    skin_hi = (254, 231, 216, 255)
    skin_lo = (214, 174, 158, 255)
    hair = (28, 30, 34, 255)
    hair_hi = (66, 68, 74, 255)
    apron = (246, 244, 238, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (184, 190, 194, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (181, 96, 92, 255)
    food_green = (102, 141, 82, 255)
    food_orange = (210, 122, 63, 255)
    ellipse(d, (22, 118, 106, 128), (0, 0, 0, 45))

    # high ponytail behind
    polygon(d, [(74, 12), (86, 15), (90, 27), (84, 36), (78, 26)], hair, OUTLINE)
    line(d, [(80, 18), (85, 27)], hair_hi, 2)

    # slim legs / shoes
    rr(d, (56, 102, 62, 119), 3, dark, OUTLINE, 1)
    rr(d, (66, 102, 72, 119), 3, dark, OUTLINE, 1)
    rr(d, (52, 116, 64, 124), 3, (36, 38, 42, 255), OUTLINE, 1)
    rr(d, (64, 116, 76, 124), 3, (36, 38, 42, 255), OUTLINE, 1)
    d.rectangle((P(53), P(119), P(62), P(121)), fill=silver, outline=OUTLINE, width=P(1))
    d.rectangle((P(66), P(119), P(75), P(121)), fill=silver, outline=OUTLINE, width=P(1))

    # under-suit / torso
    rr(d, (55, 46, 73, 78), 5, dark, OUTLINE, 2)
    # fitted apron skirt
    polygon(d, [(52, 66), (76, 66), (82, 104), (46, 104)], apron, OUTLINE)
    polygon(d, [(52, 66), (56, 66), (51, 104), (46, 104)], apron_lo, None)
    polygon(d, [(74, 66), (76, 66), (82, 104), (76, 104)], apron_lo, None)
    polygon(d, [(57, 69), (72, 69), (76, 100), (52, 100)], apron_hi, None)
    rr(d, (58, 78, 70, 88), 2, apron_lo, OUTLINE, 1)
    line(d, [(50, 100), (78, 100)], apron_lo, 1)
    # waist belt
    rr(d, (53, 62, 75, 67), 2, silver, OUTLINE, 1)
    d.rectangle((P(59), P(63), P(69), P(65)), fill=silver_hi)

    # bib / straps
    rr(d, (57, 47, 71, 64), 3, apron, OUTLINE, 1)
    line(d, [(58, 48), (54, 40)], OUTLINE, 3)
    line(d, [(70, 48), (74, 40)], OUTLINE, 3)
    d.rectangle((P(62), P(50), P(66), P(53)), fill=apron_hi)
    d.rectangle((P(63), P(59), P(65), P(62)), fill=silver)

    # shoulder caps
    for x in (44, 72):
        ellipse(d, (x, 45, x + 16, 61), silver, OUTLINE, 2)
        ellipse(d, (x + 4, 49, x + 12, 57), dark, OUTLINE, 1)
        ellipse(d, (x + 6, 51, x + 10, 55), blue, None)
        ellipse(d, (x + 7, 52, x + 9, 54), blue_hi, None)
    # arms
    line(d, [(52, 53), (44, 70), (48, 86)], OUTLINE, 8)
    line(d, [(52, 53), (44, 70), (48, 86)], silver, 4)
    ellipse(d, (41, 66, 49, 74), silver_hi, OUTLINE, 1)
    rr(d, (45, 84, 53, 92), 2, silver, OUTLINE, 1)
    line(d, [(76, 53), (85, 70), (81, 86)], OUTLINE, 8)
    line(d, [(76, 53), (85, 70), (81, 86)], silver, 4)
    ellipse(d, (82, 66, 90, 74), silver_hi, OUTLINE, 1)
    rr(d, (78, 84, 86, 92), 2, silver, OUTLINE, 1)

    # tray / knife
    ellipse(d, (34, 90, 64, 100), (231, 228, 220, 255), OUTLINE, 2)
    ellipse(d, (38, 92, 60, 97), (252, 250, 243, 255), None)
    ellipse(d, (42, 92, 48, 97), food_green, None)
    ellipse(d, (50, 91, 56, 97), food_orange, None)
    line(d, [(84, 92), (92, 104)], OUTLINE, 3)
    line(d, [(84, 92), (92, 104)], silver_hi, 2)
    polygon(d, [(90, 102), (98, 104), (96, 110), (88, 106)], silver, OUTLINE)

    # neck
    rr(d, (59, 40, 69, 47), 3, silver, OUTLINE, 1)
    # head, smaller and higher
    ellipse(d, (53, 14, 75, 41), skin, OUTLINE, 2)
    ellipse(d, (56, 17, 72, 38), skin_hi, None)
    ellipse(d, (72, 19, 76, 37), skin_lo, None)
    # hair cap and bangs, kept above the eyes
    ellipse(d, (50, 10, 78, 30), hair, OUTLINE, 2)
    polygon(d, [(50, 21), (53, 14), (75, 13), (78, 19), (71, 17), (65, 22), (58, 18), (53, 22)], hair, OUTLINE)
    ellipse(d, (50, 20, 53, 32), hair, OUTLINE, 1)
    ellipse(d, (75, 20, 78, 32), hair, OUTLINE, 1)
    line(d, [(57, 14), (61, 20), (67, 18)], hair_hi, 1)
    ellipse(d, (75, 12, 81, 18), blue, OUTLINE, 1)
    ellipse(d, (77, 14, 79, 16), blue_hi, None)
    # ear seam
    ellipse(d, (50, 27, 54, 34), silver, OUTLINE, 1)
    line(d, [(53, 30), (55, 36)], (198, 160, 145, 255), 1)
    # eyes, brows, nose, lips
    for x in (57, 66):
        ellipse(d, (x, 25, x + 7, 31), (249, 249, 246, 255), None)
        ellipse(d, (x + 2, 26, x + 6, 30), blue, None)
        ellipse(d, (x + 3, 27, x + 5, 29), (24, 35, 50, 255), None)
        line(d, [(x - 1, 23), (x + 7, 23)], hair, 1)
    line(d, [(63, 31), (64, 34)], (198, 160, 145, 255), 1)
    line(d, [(60, 36), (67, 36)], lip, 1)
    ellipse(d, (62, 38, 66, 40), (226, 186, 170, 255), None)
    return im


def mkv10_north():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    hair = (28, 30, 34, 255)
    hair_hi = (66, 68, 74, 255)
    apron = (246, 244, 238, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (184, 190, 194, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    skin = (239, 207, 189, 255)
    ellipse(d, (22, 118, 106, 128), (0, 0, 0, 45))
    for x in (56, 66):
        rr(d, (x, 102, x + 6, 119), 3, dark, OUTLINE, 1)
        rr(d, (x - 3, 116, x + 9, 124), 3, (36, 38, 42, 255), OUTLINE, 1)
    # back hair / ponytail
    ellipse(d, (50, 10, 78, 34), hair, OUTLINE, 2)
    polygon(d, [(72, 12), (86, 16), (90, 30), (84, 40), (78, 26)], hair, OUTLINE)
    line(d, [(78, 18), (85, 28)], hair_hi, 2)
    # back apron
    polygon(d, [(52, 66), (76, 66), (82, 104), (46, 104)], apron, OUTLINE)
    polygon(d, [(57, 69), (72, 69), (76, 100), (52, 100)], apron_hi, None)
    line(d, [(50, 100), (78, 100)], apron_lo, 1)
    # waist bow
    line(d, [(54, 54), (48, 64)], OUTLINE, 3)
    line(d, [(74, 54), (80, 64)], OUTLINE, 3)
    ellipse(d, (60, 64, 66, 70), apron, OUTLINE, 1)
    polygon(d, [(60, 66), (52, 62), (52, 70)], apron, OUTLINE)
    polygon(d, [(72, 66), (80, 62), (80, 70)], apron, OUTLINE)
    # shoulders / arms
    for x in (44, 72):
        ellipse(d, (x, 45, x + 16, 61), silver, OUTLINE, 2)
        ellipse(d, (x + 6, 51, x + 10, 55), blue, None)
    for sx, ex in ((52, 44), (76, 82)):
        line(d, [(sx, 53), (ex, 70), (ex + 4, 86)], OUTLINE, 8)
        line(d, [(sx, 53), (ex, 70), (ex + 4, 86)], silver, 4)
    # neck
    rr(d, (59, 40, 69, 47), 3, silver, OUTLINE, 1)
    # back of head
    ellipse(d, (53, 14, 75, 41), skin, OUTLINE, 2)
    ellipse(d, (50, 10, 78, 34), hair, OUTLINE, 2)
    line(d, [(57, 15), (68, 12), (76, 20)], hair_hi, 2)
    ellipse(d, (75, 12, 81, 18), blue, OUTLINE, 1)
    return im


def mkv10_east():
    im = Image.new("RGBA", (128 * S, 128 * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    skin = (239, 207, 189, 255)
    skin_hi = (254, 231, 216, 255)
    skin_lo = (214, 174, 158, 255)
    hair = (28, 30, 34, 255)
    hair_hi = (66, 68, 74, 255)
    apron = (246, 244, 238, 255)
    apron_hi = (255, 254, 251, 255)
    apron_lo = (206, 203, 195, 255)
    silver = (184, 190, 194, 255)
    silver_hi = (228, 233, 235, 255)
    dark = (50, 53, 58, 255)
    blue = (74, 176, 220, 255)
    blue_hi = (187, 233, 248, 255)
    lip = (181, 96, 92, 255)
    food_green = (102, 141, 82, 255)
    food_orange = (210, 122, 63, 255)
    ellipse(d, (24, 118, 104, 128), (0, 0, 0, 45))
    for x in (56, 66):
        rr(d, (x, 102, x + 6, 119), 3, dark, OUTLINE, 1)
        rr(d, (x - 3, 116, x + 9, 124), 3, (36, 38, 42, 255), OUTLINE, 1)
    # ponytail behind
    polygon(d, [(56, 12), (42, 16), (36, 28), (40, 40), (48, 42), (50, 28), (53, 20)], hair, OUTLINE)
    line(d, [(44, 20), (39, 30), (41, 38)], hair_hi, 2)
    # apron / body
    polygon(d, [(52, 66), (76, 66), (82, 104), (46, 104)], apron, OUTLINE)
    polygon(d, [(57, 69), (72, 69), (76, 100), (52, 100)], apron_hi, None)
    line(d, [(50, 100), (78, 100)], apron_lo, 1)
    rr(d, (58, 78, 70, 88), 2, apron_lo, OUTLINE, 1)
    rr(d, (55, 46, 73, 78), 5, dark, OUTLINE, 2)
    # shoulder / arm
    ellipse(d, (72, 45, 88, 61), silver, OUTLINE, 2)
    ellipse(d, (76, 49, 84, 57), dark, OUTLINE, 1)
    ellipse(d, (78, 51, 82, 55), blue, None)
    line(d, [(78, 53), (88, 70), (84, 86)], OUTLINE, 8)
    line(d, [(78, 53), (88, 70), (84, 86)], silver, 4)
    ellipse(d, (85, 66, 93, 74), silver_hi, OUTLINE, 1)
    rr(d, (81, 84, 89, 92), 2, silver, OUTLINE, 1)
    # tray / food
    ellipse(d, (48, 90, 78, 100), (231, 228, 220, 255), OUTLINE, 2)
    ellipse(d, (52, 92, 74, 97), (252, 250, 243, 255), None)
    ellipse(d, (56, 92, 62, 97), food_green, None)
    ellipse(d, (64, 91, 70, 97), food_orange, None)
    # knife
    line(d, [(86, 92), (94, 104)], OUTLINE, 3)
    line(d, [(86, 92), (94, 104)], silver_hi, 2)
    polygon(d, [(92, 102), (100, 104), (98, 110), (90, 106)], silver, OUTLINE)
    # neck
    rr(d, (59, 40, 69, 47), 3, silver, OUTLINE, 1)
    # side head
    ellipse(d, (54, 14, 78, 41), skin, OUTLINE, 2)
    ellipse(d, (57, 17, 74, 38), skin_hi, None)
    ellipse(d, (75, 19, 79, 37), skin_lo, None)
    # hair cap / bangs / ponytail
    ellipse(d, (50, 10, 80, 32), hair, OUTLINE, 2)
    polygon(d, [(50, 21), (53, 14), (76, 13), (81, 20), (73, 18), (66, 23), (59, 18), (53, 22)], hair, OUTLINE)
    ellipse(d, (44, 18, 50, 34), hair, OUTLINE, 1)
    ellipse(d, (75, 12, 81, 18), blue, OUTLINE, 1)
    # eye / nose / lips
    ellipse(d, (65, 25, 73, 31), (249, 249, 246, 255), None)
    ellipse(d, (67, 26, 71, 30), blue, None)
    ellipse(d, (68, 27, 70, 29), (24, 35, 50, 255), None)
    line(d, [(63, 23), (73, 23)], hair, 1)
    line(d, [(64, 35), (72, 35)], lip, 1)
    line(d, [(77, 31), (79, 34)], (198, 160, 145, 255), 1)
    line(d, [(51, 30), (53, 36)], (198, 160, 145, 255), 1)
    return im


def robot_south(tier):
    if tier == "MkII":
        return mkii7_south()
    if tier == "MkIII":
        return mkiii_ref_south()
    if tier == "MkIV":
        return mkiv_ref_south()
    if tier == "MkV":
        return mkv10_south()
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
        return mkii7_north()
    if tier == "MkIII":
        return mkiii_ref_north()
    if tier == "MkIV":
        return mkiv_ref_north()
    if tier == "MkV":
        return mkv10_north()
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
        return mkii7_east()
    if tier == "MkIII":
        return mkiii_ref_east()
    if tier == "MkIV":
        return mkiv_ref_east()
    if tier == "MkV":
        return mkv10_east()
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
    # Mk-V is a direct-use asset made from the supplied renders. Do not overwrite it here.
    for tier in [t for t in tiers if t != "MkV"]:
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
