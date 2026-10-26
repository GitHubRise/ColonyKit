"""Draw a front-facing maintenance/automation robot sprite (128 px game asset + preview)."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures")
os.makedirs(OUT, exist_ok=True)

SIZE = 128
S = 4
W = SIZE * S


def P(value: float) -> int:
    return int(round(value * S))


OUTLINE = (27, 33, 41, 255)
DARK = (57, 66, 78, 255)
DARK2 = (43, 51, 61, 255)
STEEL = (134, 147, 159, 255)
STEEL_LIGHT = (190, 200, 209, 255)
STEEL_MID = (158, 170, 181, 255)
PANEL = (48, 58, 70, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (213, 112, 66, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (42, 121, 144, 255)
GREEN = (126, 212, 147, 255)
TREAD = (38, 44, 52, 255)
TREAD_LIGHT = (82, 91, 102, 255)


def canvas() -> Image.Image:
    return Image.new("RGBA", (W, W), (0, 0, 0, 0))


def rr(draw: ImageDraw.ImageDraw, box, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def ellipse(draw: ImageDraw.ImageDraw, box, fill, outline=None, width=1):
    draw.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(draw: ImageDraw.ImageDraw, points, fill, outline=None):
    draw.polygon(points, fill=fill, outline=outline)


def line(draw: ImageDraw.ImageDraw, points, fill, width=1):
    draw.line(points, fill=fill, width=width, joint="curve")


def bolt(draw: ImageDraw.ImageDraw, x: float, y: float, radius: float = 1.6):
    ellipse(draw, (P(x - radius), P(y - radius), P(x + radius), P(y + radius)),
            STEEL_LIGHT, OUTLINE, P(1))


def hazard_stripe(draw: ImageDraw.ImageDraw, x1, y1, x2, y2, step=5):
    # diagonal amber/dark strip inside a rectangular band
    draw.rectangle((P(x1), P(y1), P(x2), P(y2)), fill=DARK2, outline=OUTLINE, width=P(1))
    y = y1
    while y < y2:
        polygon(draw, [
            (P(x1 + 1), P(y)),
            (P(x1 + step), P(y)),
            (P(x1 + step - 3), P(min(y + 5, y2))),
            (P(x1 + 1), P(min(y + 5, y2))),
        ], AMBER)
        y += 8


def draw_robot() -> Image.Image:
    image = canvas()
    d = ImageDraw.Draw(image)

    # ground shadow
    d.ellipse((P(20), P(109), P(108), P(123)), fill=(0, 0, 0, 58))

    # treads
    for x in (14, 82):
        rr(d, (P(x), P(91), P(x + 32), P(114)), P(9), TREAD, OUTLINE, P(1.5))
        # top fender
        rr(d, (P(x - 1), P(88), P(x + 33), P(98)), P(5), STEEL, OUTLINE, P(1))
        # rollers
        for dx in (6, 14, 22):
            d.rectangle((P(x + dx), P(98), P(x + dx + 4), P(108)), fill=TREAD_LIGHT)
        # amber side marker
        d.rectangle((P(x + 2), P(103), P(x + 6), P(107)), fill=AMBER, outline=OUTLINE, width=P(1))

    # lower chassis / hydraulics
    rr(d, (P(38), P(82), P(90), P(105)), P(8), DARK, OUTLINE, P(1.5))
    rr(d, (P(44), P(87), P(84), P(101)), P(5), DARK2, OUTLINE, P(1))
    for x in (50, 60, 70, 78):
        d.rectangle((P(x), P(89), P(x + 3), P(99)), fill=STEEL_MID)
    # piston highlights
    line(d, [(P(46), P(94)), (P(56), P(94))], AMBER, P(2))
    line(d, [(P(82), P(94)), (P(88), P(94))], AMBER, P(2))

    # backpack / shoulder pods behind the torso
    for x in (21, 93):
        rr(d, (P(x), P(46), P(x + 14), P(76)), P(5), DARK, OUTLINE, P(1.5))
        d.rectangle((P(x + 4), P(51), P(x + 10), P(55)), fill=AMBER, outline=OUTLINE, width=P(1))
        ellipse(d, (P(x + 4), P(61), P(x + 10), P(67)), CYAN_DIM, OUTLINE, P(1))
        d.rectangle((P(x + 3), P(71), P(x + 11), P(73)), fill=TREAD_LIGHT)

    # main torso
    rr(d, (P(29), P(39), P(99), P(97)), P(13), STEEL, OUTLINE, P(1.5))
    # armor inset
    rr(d, (P(35), P(45), P(93), P(91)), P(9), DARK, OUTLINE, P(1))
    # lower skirt
    polygon(d, [(P(35), P(88)), (P(93), P(88)), (P(87), P(101)), (P(41), P(101))],
            DARK2, OUTLINE)
    for x in (43, 55, 67, 79):
        d.rectangle((P(x), P(93), P(x + 5), P(98)), fill=STEEL_MID, outline=OUTLINE, width=P(1))

    # side hazard bands
    hazard_stripe(d, 31, 51, 38, 83, step=4)
    hazard_stripe(d, 90, 51, 97, 83, step=4)

    # chest diagnostic panel
    rr(d, (P(42), P(52), P(86), P(82)), P(7), PANEL, OUTLINE, P(1.5))
    rr(d, (P(46), P(56), P(82), P(72)), P(4), (18, 27, 35, 255), OUTLINE, P(1))
    # heartbeat / waveform
    points = [(48, 65), (52, 65), (54, 60), (57, 69), (60, 63), (63, 66), (66, 63), (69, 67), (72, 61), (75, 66), (80, 66)]
    line(d, [(P(x), P(y)) for x, y in points], CYAN, P(1.8))
    d.rectangle((P(48), P(74), P(60), P(77)), fill=GREEN, outline=OUTLINE, width=P(1))
    d.rectangle((P(63), P(74), P(75), P(77)), fill=AMBER, outline=OUTLINE, width=P(1))
    ellipse(d, (P(78), P(74), P(82), P(78)), ORANGE, OUTLINE, P(1))

    # repair emblem: gear + wrench
    cx, cy = 64, 88
    ellipse(d, (P(cx - 6), P(cy - 6), P(cx + 6), P(cy + 6)), AMBER, OUTLINE, P(1))
    ellipse(d, (P(cx - 2), P(cy - 2), P(cx + 2), P(cy + 2)), DARK2, OUTLINE, P(1))
    for angle in range(0, 360, 60):
        a = math.radians(angle)
        line(d, [(P(cx + math.cos(a) * 6), P(cy + math.sin(a) * 6)),
                 (P(cx + math.cos(a) * 9), P(cy + math.sin(a) * 9))], AMBER, P(2))
    # small wrench diagonal over gear
    line(d, [(P(cx + 2), P(cy - 8)), (P(cx + 8), P(cy - 2))], STEEL_LIGHT, P(2))
    line(d, [(P(cx + 8), P(cy - 2)), (P(cx + 10), P(cy - 4))], STEEL_LIGHT, P(2))

    # arms (front-facing, segmented)
    for side in (-1, 1):
        x = 14 if side == -1 else 96
        # shoulder ball
        ellipse(d, (P(x + 1), P(51), P(x + 15), P(65)), STEEL_MID, OUTLINE, P(1.2))
        # upper arm
        rr(d, (P(x), P(60), P(x + 12), P(80)), P(5), STEEL, OUTLINE, P(1.2))
        # elbow / forearm
        elbow_x = x - 2 if side == -1 else x + 2
        rr(d, (P(elbow_x), P(77), P(elbow_x + 12), P(96)), P(4), DARK, OUTLINE, P(1.2))
        # wrist band
        d.rectangle((P(elbow_x + 2), P(84), P(elbow_x + 10), P(87)), fill=AMBER, outline=OUTLINE, width=P(1))
        # clamp hand
        if side == -1:
            rr(d, (P(elbow_x - 1), P(93), P(elbow_x + 8), P(100)), P(3), STEEL_LIGHT, OUTLINE, P(1))
            rr(d, (P(elbow_x + 6), P(90), P(elbow_x + 12), P(96)), P(2), ORANGE, OUTLINE, P(1))
        else:
            rr(d, (P(elbow_x + 4), P(93), P(elbow_x + 13), P(100)), P(3), STEEL_LIGHT, OUTLINE, P(1))
            rr(d, (P(elbow_x - 1), P(90), P(elbow_x + 5), P(96)), P(2), ORANGE, OUTLINE, P(1))

    # right tool: plasma cutter / welding torch
    torch_x = 111
    rr(d, (P(torch_x), P(83), P(torch_x + 9), P(97)), P(3), DARK, OUTLINE, P(1))
    rr(d, (P(torch_x + 1), P(95), P(torch_x + 8), P(102)), P(2), STEEL_LIGHT, OUTLINE, P(1))
    # nozzle
    polygon(d, [(P(torch_x + 2), P(100)), (P(torch_x + 7), P(100)), (P(torch_x + 8), P(107)), (P(torch_x + 1), P(107))],
            DARK2, OUTLINE)
    # tiny cyan arc
    line(d, [(P(torch_x + 5), P(106)), (P(torch_x + 2), P(112)), (P(torch_x + 7), P(110))], CYAN, P(1.5))
    ellipse(d, (P(torch_x + 3), P(109), P(torch_x + 8), P(114)), (*CYAN[:3], 90))
    # tool cable
    line(d, [(P(104), P(80)), (P(108), P(84)), (P(108), P(88))], OUTLINE, P(2))

    # shoulders / collar
    for x in (23, 91):
        polygon(d, [(P(x), P(40)), (P(x + 14), P(40)), (P(x + 12), P(51)), (P(x + 2), P(51))],
                STEEL_MID, OUTLINE)
        bolt(d, x + 7, 46)

    # head (angular maintenance helmet)
    head_points = [
        (P(40), P(14)), (P(88), P(14)), (P(95), P(21)), (P(95), P(39)),
        (P(88), P(46)), (P(40), P(46)), (P(33), P(39)), (P(33), P(21)),
    ]
    polygon(d, head_points, STEEL, OUTLINE)
    # inner helmet plate
    polygon(d, [
        (P(41), P(19)), (P(87), P(19)), (P(91), P(24)), (P(91), P(37)),
        (P(87), P(42)), (P(41), P(42)), (P(37), P(37)), (P(37), P(24)),
    ], DARK, OUTLINE)
    # visor
    rr(d, (P(40), P(23), P(88), P(37)), P(5), (14, 23, 31, 255), OUTLINE, P(1))
    # wide horizontal sensor eye
    d.rectangle((P(44), P(28), P(84), P(33)), fill=CYAN, outline=OUTLINE, width=P(1))
    d.rectangle((P(48), P(29), P(72), P(31)), fill=(226, 252, 255, 255))
    # side sensor lights
    for x in (36, 92):
        ellipse(d, (P(x - 3), P(25), P(x + 3), P(31)), AMBER, OUTLINE, P(1))
    # lower grille
    for x in (55, 61, 67, 73):
        d.rectangle((P(x), P(39), P(x + 3), P(42)), fill=STEEL_MID)

    # top handle / antenna
    d.rectangle((P(54), P(11), P(74), P(15)), fill=DARK, outline=OUTLINE, width=P(1))
    line(d, [(P(64), P(11)), (P(64), P(5))], OUTLINE, P(2))
    ellipse(d, (P(60), P(1), P(68), P(9)), AMBER, OUTLINE, P(1))
    ellipse(d, (P(62), P(3), P(66), P(7)), AMBER_LIGHT, None)
    # secondary antenna
    line(d, [(P(78), P(14)), (P(82), P(7))], OUTLINE, P(1.5))
    ellipse(d, (P(79), P(4), P(85), P(10)), CYAN_DIM, OUTLINE, P(1))

    # chest / head rivets
    for x, y in ((36, 50), (92, 50), (36, 84), (92, 84), (40, 44), (88, 44)):
        bolt(d, x, y, 1.4)

    # top rim highlights
    line(d, [(P(38), P(17)), (P(86), P(17))], (224, 233, 239, 255), P(1))
    line(d, [(P(31), P(45)), (P(31), P(88))], (224, 233, 239, 255), P(1))
    line(d, [(P(97), P(45)), (P(97), P(88))], (224, 233, 239, 255), P(1))

    # subtle soft glow behind visor/screen (before downsample)
    glow = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((P(42), P(21), P(86), P(39)), fill=(*CYAN[:3], 34))
    gd.ellipse((P(44), P(54), P(80), P(78)), fill=(*CYAN[:3], 22))
    glow = glow.filter(ImageFilter.GaussianBlur(P(1.8)))
    image = Image.alpha_composite(glow, image)
    return image


def main():
    image = draw_robot()
    front_512 = os.path.join(OUT, "CK_MaintenanceBot_front_512.png")
    front_128 = os.path.join(OUT, "CK_MaintenanceBot_front_128.png")
    image.save(front_512)
    image.resize((SIZE, SIZE), Image.LANCZOS).save(front_128)

    # presentation preview on a dark workshop-style panel
    preview = Image.new("RGBA", (720, 720), (15, 21, 27, 255))
    pd = ImageDraw.Draw(preview)
    # subtle vignette / glow
    for i in range(18, 0, -1):
        alpha = int(5 + (18 - i) * 0.9)
        pd.ellipse((360 - i * 18, 340 - i * 15, 360 + i * 18, 340 + i * 15),
                   fill=(36, 74, 82, alpha))
    # grid
    for x in range(0, 720, 40):
        pd.line([(x, 0), (x, 720)], fill=(28, 42, 49, 80), width=1)
    for y in range(0, 720, 40):
        pd.line([(0, y), (720, y)], fill=(28, 42, 49, 80), width=1)
    # panel frame
    pd.rounded_rectangle((35, 35, 685, 685), radius=24, outline=(55, 80, 88, 255), width=2)
    sprite = image.resize((560, 560), Image.LANCZOS)
    preview.alpha_composite(sprite, (80, 72))
    # top/bottom labels
    pd.text((55, 50), "CK-M7  MAINTENANCE / AUTOMATION", fill=(157, 188, 194, 255))
    pd.text((55, 648), "FRONT VIEW  ·  128 px game sprite  ·  candidate", fill=(105, 139, 146, 255))
    preview.convert("RGB").save(os.path.join(OUT, "CK_MaintenanceBot_front_preview.png"), quality=95)

    print("wrote", front_512)
    print("wrote", front_128)
    print("wrote", os.path.join(OUT, "CK_MaintenanceBot_front_preview.png"))


if __name__ == "__main__":
    main()
