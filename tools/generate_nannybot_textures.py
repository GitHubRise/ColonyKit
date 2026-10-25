from PIL import Image, ImageDraw
import os

ROOT = r"E:\RimWorld-WorkSpace\sources\ColonyKit\Textures"
SCALE = 4
SIZE = 128


def canvas():
    return Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))


def finish(img, path):
    out = img.resize((SIZE, SIZE), Image.LANCZOS)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out.save(path)
    print("wrote", path)


def rr(draw, box, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def robot(facing):
    img = canvas()
    d = ImageDraw.Draw(img)
    s = SCALE

    def P(x):
        return int(x * s)

    outline = (38, 46, 56, 255)
    shell = (222, 228, 236, 255)
    shell_dark = (184, 194, 206, 255)
    tread = (58, 66, 78, 255)
    tread_light = (92, 102, 116, 255)
    blue = (61, 126, 166, 255)
    blue_light = (110, 190, 225, 255)
    broom_wood = (150, 100, 55, 255)
    broom_bristle = (232, 188, 88, 255)

    # treads
    if facing in ("south", "north"):
        rr(d, (P(22), P(94), P(52), P(112)), P(8), tread, outline, P(1))
        rr(d, (P(76), P(94), P(106), P(112)), P(8), tread, outline, P(1))
        for x in (28, 36, 44, 82, 90, 98):
            d.rectangle((P(x), P(99), P(x + 4), P(107)), fill=tread_light)
    else:
        side_x = 22 if facing == "west" else 70
        rr(d, (P(side_x), P(94), P(side_x + 36), P(112)), P(8), tread, outline, P(1))
        for x in range(side_x + 6, side_x + 34, 8):
            d.rectangle((P(x), P(99), P(x + 4), P(107)), fill=tread_light)

    # body
    rr(d, (P(28), P(44), P(100), P(102)), P(18), shell, outline, P(1.5))
    rr(d, (P(34), P(50), P(94), P(96)), P(14), None, shell_dark, P(1))

    if facing == "south":
        # chest panel + status lights
        rr(d, (P(44), P(62), P(84), P(88)), P(8), blue, outline, P(1))
        for i, col in enumerate(((120, 220, 170, 255), (240, 205, 90, 255), (235, 120, 110, 255))):
            d.ellipse((P(50 + i * 11), P(73), P(56 + i * 11), P(79)), fill=col, outline=outline)
        # arms
        rr(d, (P(14), P(58), P(30), P(88)), P(7), shell_dark, outline, P(1))
        rr(d, (P(98), P(58), P(114), P(88)), P(7), shell_dark, outline, P(1))
        # broom in right hand
        d.line((P(106), P(74), P(118), P(112)), fill=outline, width=P(3))
        d.line((P(106), P(74), P(118), P(112)), fill=broom_wood, width=P(2))
        d.polygon([(P(112), P(108)), (P(124), P(112)), (P(120), P(122)), (P(108), P(118))],
                  fill=broom_bristle, outline=outline)
    elif facing == "north":
        # back hatch + vents
        rr(d, (P(42), P(58), P(86), P(92)), P(6), shell_dark, outline, P(1))
        for y in (66, 73, 80):
            d.rectangle((P(49), P(y), P(79), P(y + 3)), fill=tread)
        # arms
        rr(d, (P(14), P(58), P(30), P(88)), P(7), shell_dark, outline, P(1))
        rr(d, (P(98), P(58), P(114), P(88)), P(7), shell_dark, outline, P(1))
        # broom strap
        d.line((P(106), P(72), P(118), P(110)), fill=outline, width=P(3))
        d.line((P(106), P(72), P(118), P(110)), fill=broom_wood, width=P(2))
    else:
        flip = facing == "west"
        # side profile body shading
        rr(d, (P(34), P(50), P(96), P(96)), P(14), shell, outline, P(1))
        rr(d, (P(40), P(58), P(90), P(88)), P(10), blue, outline, P(1))
        # one arm visible
        arm_x = 60
        rr(d, (P(arm_x), P(56), P(arm_x + 16), P(88)), P(7), shell_dark, outline, P(1))
        # broom
        bx = 82 if not flip else 46
        d.line((P(bx), P(72), P(bx + (10 if not flip else -10)), P(112)), fill=outline, width=P(3))
        d.line((P(bx), P(72), P(bx + (10 if not flip else -10)), P(112)), fill=broom_wood, width=P(2))
        if flip:
            d.polygon([(P(48), P(108)), (P(36), P(112)), (P(40), P(122)), (P(52), P(118))],
                      fill=broom_bristle, outline=outline)
        else:
            d.polygon([(P(84), P(108)), (P(96), P(112)), (P(92), P(122)), (P(80), P(118))],
                      fill=broom_bristle, outline=outline)

    # head dome
    head_y = 20
    d.ellipse((P(34), P(head_y), P(94), P(head_y + 44)), fill=shell, outline=outline, width=P(1.5))
    d.ellipse((P(40), P(head_y + 5), P(88), P(head_y + 35)), fill=shell_dark, outline=None)

    if facing == "south":
        d.ellipse((P(42), P(head_y + 8), P(86), P(head_y + 32)), fill=(26, 32, 42, 255))
        d.ellipse((P(56), P(head_y + 14), P(72), P(head_y + 27)), fill=blue_light)
        d.ellipse((P(60), P(head_y + 17), P(66), P(head_y + 23)), fill=(230, 250, 255, 255))
    elif facing == "north":
        rr(d, (P(52), P(head_y + 12), P(76), P(head_y + 26)), P(4), (44, 52, 64, 255), outline)
        for x in (57, 64, 71):
            d.rectangle((P(x), P(head_y + 16), P(x + 3), P(head_y + 23)), fill=(110, 120, 134, 255))
    else:
        cx = 64
        d.ellipse((P(cx - 18), P(head_y + 8), P(cx + 18), P(head_y + 32)), fill=(26, 32, 42, 255))
        d.ellipse((P(cx + (4 if facing == "east" else -12)), P(head_y + 14),
                   P(cx + (16 if facing == "east" else 0)), P(head_y + 27)), fill=blue_light)

    # antenna
    d.line((P(64), P(head_y), P(64), P(12)), fill=outline, width=P(2))
    d.ellipse((P(60), P(8), P(68), P(16)), fill=(240, 205, 90, 255), outline=outline)

    finish(img, os.path.join(ROOT, "Things", "Pawn", "NannyBot", "NannyBot_%s.png" % facing))


def station():
    img = canvas()
    d = ImageDraw.Draw(img)
    s = SCALE

    def P(x):
        return int(x * s)

    outline = (36, 44, 54, 255)
    metal = (86, 96, 110, 255)
    metal_light = (128, 140, 156, 255)
    dark = (40, 48, 60, 255)
    blue = (54, 142, 190, 255)
    glow = (116, 206, 240, 255)

    d.ellipse((P(10), P(18), P(118), P(118)), fill=dark, outline=outline, width=P(2))
    d.ellipse((P(18), P(26), P(110), P(110)), fill=metal, outline=outline, width=P(1.5))
    d.ellipse((P(28), P(36), P(100), P(100)), fill=(58, 68, 82, 255), outline=blue, width=P(2))
    d.ellipse((P(38), P(46), P(90), P(90)), fill=(30, 40, 52, 255), outline=glow, width=P(1))

    # central cradle
    rr(d, (P(48), P(48), P(80), P(84)), P(8), metal_light, outline, P(1.5))
    d.rectangle((P(54), P(56), P(74), P(70)), fill=dark, outline=outline)
    d.ellipse((P(58), P(58), P(70), P(68)), fill=glow)
    # bolt icon
    d.polygon([(P(69), P(52)), (P(59), P(68)), (P(66), P(68)), (P(60), P(82)), (P(76), P(62)), (P(68), P(62))],
              fill=(245, 216, 96, 255), outline=outline)

    # side clamps
    rr(d, (P(24), P(56), P(40), P(76)), P(5), metal_light, outline, P(1))
    rr(d, (P(88), P(56), P(104), P(76)), P(5), metal_light, outline, P(1))

    # floor lights
    for i in range(8):
        import math
        a = math.radians(i * 45)
        x = 64 + math.cos(a) * 48
        y = 68 + math.sin(a) * 42
        d.ellipse((P(x - 3), P(y - 3), P(x + 3), P(y + 3)), fill=glow if i % 2 == 0 else blue)

    finish(img, os.path.join(ROOT, "Things", "Building", "Robotics", "CK_NannyBotStation.png"))


def icon(name, kind):
    size = 64
    img = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    s = 2
    white = (238, 243, 248, 255)
    gold = (244, 210, 92, 255)
    blue = (96, 186, 228, 255)
    brown = (170, 116, 62, 255)
    dark = (30, 36, 46, 255)

    def P(x):
        return int(x * s)

    if kind == "sweep":
        d.line((P(46), P(12), P(24), P(44)), fill=brown, width=P(4))
        d.line((P(46), P(12), P(24), P(44)), fill=dark, width=P(5))
        d.line((P(46), P(12), P(24), P(44)), fill=brown, width=P(3))
        d.polygon([(P(18), P(40)), (P(34), P(44)), (P(30), P(58)), (P(12), P(52))], fill=gold, outline=dark)
        d.line((P(16), P(48), P(32), P(52)), fill=dark, width=P(1))
    elif kind == "scrub":
        rr(d, (P(20), P(24), P(46), P(56)), P(6), blue, dark, P(2))
        d.line((P(43), P(26), P(52), P(14)), fill=dark, width=P(4))
        d.line((P(43), P(26), P(52), P(14)), fill=(150, 210, 240, 255), width=P(2))
        d.rectangle((P(25), P(30), P(41), P(38)), fill=white)
        for x, y in ((14, 18), (52, 30), (10, 40), (50, 44)):
            d.ellipse((P(x), P(y), P(x + 4), P(y + 4)), fill=white)
    elif kind == "haul":
        d.polygon([(P(12), P(24)), (P(32), P(16)), (P(52), P(24)), (P(32), P(32))], fill=(196, 152, 96, 255), outline=dark)
        d.polygon([(P(12), P(24)), (P(12), P(48)), (P(32), P(56)), (P(32), P(32))], fill=(156, 112, 68, 255), outline=dark)
        d.polygon([(P(52), P(24)), (P(52), P(48)), (P(32), P(56)), (P(32), P(32))], fill=(126, 88, 52, 255), outline=dark)
        d.line((P(12), P(24), P(32), P(32), P(52), P(24)), fill=dark, width=P(2))
    elif kind == "charge":
        d.polygon([(P(36), P(6)), (P(18), P(34)), (P(30), P(34)), (P(24), P(58)), (P(48), P(26)),
                   (P(35), P(26)), (P(44), P(6))], fill=gold, outline=dark)
        d.polygon([(P(36), P(12)), (P(24), P(32)), (P(33), P(32)), (P(28), P(48)), (P(42), P(28)),
                   (P(33), P(28)), (P(40), P(12))], fill=white)
    elif kind == "assemble":
        d.ellipse((P(8), P(8), P(56), P(56)), outline=white, width=P(3))
        d.ellipse((P(18), P(18), P(46), P(46)), outline=blue, width=P(3))
        d.line((P(32), P(18), P(32), P(46)), fill=white, width=P(3))
        d.line((P(18), P(32), P(46), P(32)), fill=white, width=P(3))
        d.ellipse((P(26), P(26), P(38), P(38)), fill=gold, outline=dark)

    out = img.resize((size, size), Image.LANCZOS)
    path = os.path.join(ROOT, "UI", "Commands", "CK_%s.png" % name)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    out.save(path)
    print("wrote", path)


for facing in ("south", "north", "east", "west"):
    robot(facing)
station()
for name, kind in (("Sweep", "sweep"), ("Scrub", "scrub"), ("Haul", "haul"),
                   ("Charge", "charge"), ("Assemble", "assemble")):
    icon(name, kind)
