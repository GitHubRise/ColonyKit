"""Generate north/east/west views for the maintenance robot and a four-view sheet."""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageOps

from generate_maintenance_bot_texture import (
    AMBER,
    AMBER_LIGHT,
    CYAN,
    CYAN_DIM,
    DARK,
    DARK2,
    GREEN,
    ORANGE,
    OUTLINE,
    OUT,
    P,
    PANEL,
    SIZE,
    STEEL,
    STEEL_LIGHT,
    STEEL_MID,
    TREAD,
    TREAD_LIGHT,
    canvas,
    draw_robot,
    ellipse,
    line,
    polygon,
    rr,
)

S = 4


def treads_front(d: ImageDraw.ImageDraw):
    for x in (14, 82):
        rr(d, (P(x), P(91), P(x + 32), P(114)), P(9), TREAD, OUTLINE, P(1.5))
        rr(d, (P(x - 1), P(88), P(x + 33), P(98)), P(5), STEEL, OUTLINE, P(1))
        for dx in (6, 14, 22):
            d.rectangle((P(x + dx), P(98), P(x + dx + 4), P(108)), fill=TREAD_LIGHT)
        d.rectangle((P(x + 2), P(103), P(x + 6), P(107)), fill=AMBER, outline=OUTLINE, width=P(1))


def bolts(d: ImageDraw.ImageDraw, coords):
    for x, y in coords:
        ellipse(d, (P(x - 1.4), P(y - 1.4), P(x + 1.4), P(y + 1.4)), STEEL_LIGHT, OUTLINE, P(1))


def draw_north() -> Image.Image:
    image = canvas()
    d = ImageDraw.Draw(image)
    d.ellipse((P(20), P(109), P(108), P(123)), fill=(0, 0, 0, 58))
    treads_front(d)

    # lower chassis
    rr(d, (P(38), P(82), P(90), P(105)), P(8), DARK, OUTLINE, P(1.5))
    rr(d, (P(44), P(87), P(84), P(101)), P(5), DARK2, OUTLINE, P(1))
    for x in (50, 60, 70, 78):
        d.rectangle((P(x), P(89), P(x + 3), P(99)), fill=STEEL_MID)

    # shoulder pods
    for x in (21, 93):
        rr(d, (P(x), P(46), P(x + 14), P(76)), P(5), DARK, OUTLINE, P(1.5))
        d.rectangle((P(x + 4), P(51), P(x + 10), P(55)), fill=AMBER, outline=OUTLINE, width=P(1))
        ellipse(d, (P(x + 4), P(61), P(x + 10), P(67)), CYAN_DIM, OUTLINE, P(1))
        d.rectangle((P(x + 3), P(71), P(x + 11), P(73)), fill=TREAD_LIGHT)

    # rear torso
    rr(d, (P(29), P(39), P(99), P(97)), P(13), STEEL, OUTLINE, P(1.5))
    rr(d, (P(35), P(45), P(93), P(91)), P(9), DARK, OUTLINE, P(1))

    # rear service hatch
    rr(d, (P(40), P(50), P(88), P(86)), P(6), PANEL, OUTLINE, P(1.5))
    d.rectangle((P(46), P(56), P(82), P(80)), fill=DARK2, outline=OUTLINE, width=P(1))
    # X brace
    line(d, [(P(48), P(58)), (P(80), P(78))], STEEL_MID, P(2))
    line(d, [(P(80), P(58)), (P(48), P(78))], STEEL_MID, P(2))
    # vent slats over hatch
    for y in (62, 67, 72, 77):
        d.rectangle((P(52), P(y), P(76), P(y + 3)), fill=TREAD_LIGHT)
    # amber status strip
    d.rectangle((P(46), P(82), P(82), P(85)), fill=AMBER, outline=OUTLINE, width=P(1))
    for x in (50, 64, 78):
        ellipse(d, (P(x - 2), P(81), P(x + 2), P(86)), AMBER_LIGHT, OUTLINE, P(1))

    # side hazard bands
    for x, sign in ((31, 1), (97, -1)):
        d.rectangle((P(x - 2), P(51), P(x + 4), P(83)), fill=DARK2, outline=OUTLINE, width=P(1))
        for y in range(53, 82, 8):
            polygon(d, [(P(x - 1), P(y)), (P(x + 5), P(y)), (P(x + 2), P(y + 5)), (P(x - 4), P(y + 5))], AMBER)

    # power cells / back canisters
    for x in (41, 68):
        rr(d, (P(x), P(48), P(x + 19), P(85)), P(8), STEEL_MID, OUTLINE, P(1.5))
        d.rectangle((P(x + 4), P(52), P(x + 15), P(57)), fill=AMBER, outline=OUTLINE, width=P(1))
        d.rectangle((P(x + 4), P(61), P(x + 15), P(64)), fill=CYAN_DIM, outline=OUTLINE, width=P(1))
        d.rectangle((P(x + 4), P(74), P(x + 15), P(80)), fill=DARK2, outline=OUTLINE, width=P(1))
    # cables between cells
    line(d, [(P(60), P(54)), (P(68), P(54)), (P(68), P(61))], OUTLINE, P(2))
    line(d, [(P(60), P(72)), (P(68), P(72))], OUTLINE, P(2))

    # lower rear exhaust
    for x in (46, 56, 66, 76):
        d.rectangle((P(x), P(91), P(x + 5), P(96)), fill=STEEL_MID, outline=OUTLINE, width=P(1))

    # head back
    head_points = [
        (P(40), P(14)), (P(88), P(14)), (P(95), P(21)), (P(95), P(39)),
        (P(88), P(46)), (P(40), P(46)), (P(33), P(39)), (P(33), P(21)),
    ]
    polygon(d, head_points, STEEL, OUTLINE)
    polygon(d, [
        (P(41), P(19)), (P(87), P(19)), (P(91), P(24)), (P(91), P(37)),
        (P(87), P(42)), (P(41), P(42)), (P(37), P(37)), (P(37), P(24)),
    ], DARK, OUTLINE)
    # rear vent bank
    rr(d, (P(43), P(24), P(85), P(37)), P(4), (21, 29, 37, 255), OUTLINE, P(1))
    for x in (48, 55, 62, 69, 76):
        d.rectangle((P(x), P(27), P(x + 3), P(35)), fill=STEEL_MID)
    # marker lights
    for x in (36, 92):
        ellipse(d, (P(x - 3), P(25), P(x + 3), P(31)), AMBER, OUTLINE, P(1))
    # top handle / antenna
    d.rectangle((P(54), P(11), P(74), P(15)), fill=DARK, outline=OUTLINE, width=P(1))
    line(d, [(P(64), P(11)), (P(64), P(5))], OUTLINE, P(2))
    ellipse(d, (P(60), P(1), P(68), P(9)), AMBER, OUTLINE, P(1))
    ellipse(d, (P(62), P(3), P(66), P(7)), AMBER_LIGHT, None)
    line(d, [(P(78), P(14)), (P(82), P(7))], OUTLINE, P(1.5))
    ellipse(d, (P(79), P(4), P(85), P(10)), CYAN_DIM, OUTLINE, P(1))

    # arms from behind
    for side in (-1, 1):
        x = 12 if side == -1 else 98
        ellipse(d, (P(x + 1), P(51), P(x + 15), P(65)), STEEL_MID, OUTLINE, P(1.2))
        rr(d, (P(x), P(60), P(x + 12), P(80)), P(5), STEEL, OUTLINE, P(1.2))
        elbow_x = x - 2 if side == -1 else x + 2
        rr(d, (P(elbow_x), P(77), P(elbow_x + 12), P(96)), P(4), DARK, OUTLINE, P(1.2))
        d.rectangle((P(elbow_x + 2), P(84), P(elbow_x + 10), P(87)), fill=AMBER, outline=OUTLINE, width=P(1))
        if side == -1:
            rr(d, (P(elbow_x - 1), P(93), P(elbow_x + 8), P(100)), P(3), STEEL_LIGHT, OUTLINE, P(1))
        else:
            rr(d, (P(elbow_x + 4), P(93), P(elbow_x + 13), P(100)), P(3), STEEL_LIGHT, OUTLINE, P(1))
            # torch holstered on the back
            rr(d, (P(104), P(76), P(113), P(92)), P(3), DARK2, OUTLINE, P(1))
            line(d, [(P(108), P(92)), (P(111), P(104))], OUTLINE, P(2))
            line(d, [(P(111), P(104)), (P(114), P(111))], CYAN, P(1.5))

    bolts(d, [(37, 50), (91, 50), (37, 84), (91, 84), (42, 47), (86, 47)])
    # highlights
    line(d, [(P(38), P(17)), (P(86), P(17))], (224, 233, 239, 255), P(1))
    line(d, [(P(31), P(45)), (P(31), P(88))], (224, 233, 239, 255), P(1))
    line(d, [(P(97), P(45)), (P(97), P(88))], (224, 233, 239, 255), P(1))
    return image


def draw_east() -> Image.Image:
    image = canvas()
    d = ImageDraw.Draw(image)
    d.ellipse((P(20), P(109), P(108), P(123)), fill=(0, 0, 0, 58))

    # one long side tread
    rr(d, (P(25), P(92), P(107), P(114)), P(9), TREAD, OUTLINE, P(1.5))
    rr(d, (P(25), P(88), P(107), P(98)), P(5), STEEL, OUTLINE, P(1))
    for x in range(31, 102, 10):
        d.rectangle((P(x), P(99), P(x + 5), P(108)), fill=TREAD_LIGHT)
    # side skirt
    rr(d, (P(34), P(84), P(103), P(99)), P(6), DARK, OUTLINE, P(1))
    for x in (43, 56, 69, 82):
        d.rectangle((P(x), P(88), P(x + 5), P(96)), fill=STEEL_MID, outline=OUTLINE, width=P(1))

    # rear power cell / backpack
    rr(d, (P(29), P(49), P(50), P(88)), P(7), DARK, OUTLINE, P(1.5))
    d.rectangle((P(33), P(54), P(46), P(60)), fill=AMBER, outline=OUTLINE, width=P(1))
    d.rectangle((P(33), P(65), P(46), P(69)), fill=CYAN_DIM, outline=OUTLINE, width=P(1))
    d.rectangle((P(33), P(76), P(46), P(82)), fill=DARK2, outline=OUTLINE, width=P(1))

    # side torso
    rr(d, (P(45), P(39), P(101), P(99)), P(12), STEEL, OUTLINE, P(1.5))
    rr(d, (P(51), P(45), P(95), P(93)), P(8), DARK, OUTLINE, P(1))
    # side armor panel with vent
    rr(d, (P(55), P(52), P(91), P(84)), P(6), PANEL, OUTLINE, P(1))
    for y in (58, 64, 70, 76):
        d.rectangle((P(60), P(y), P(86), P(y + 3)), fill=TREAD_LIGHT)
    # front edge chest screen (visible from the side)
    rr(d, (P(84), P(54), P(98), P(80)), P(4), PANEL, OUTLINE, P(1))
    d.rectangle((P(87), P(58), P(96), P(63)), fill=CYAN, outline=OUTLINE, width=P(1))
    d.rectangle((P(87), P(68), P(96), P(71)), fill=GREEN, outline=OUTLINE, width=P(1))
    # hazard band on side
    for y in range(88, 96, 6):
        polygon(d, [(P(52), P(y)), (P(56), P(y)), (P(54), P(y + 3)), (P(50), P(y + 3))], AMBER)

    # shoulder pod
    rr(d, (P(52), P(43), P(68), P(62)), P(5), STEEL_MID, OUTLINE, P(1.2))
    ellipse(d, (P(57), P(48), P(63), P(54)), CYAN_DIM, OUTLINE, P(1))
    d.rectangle((P(56), P(57), P(64), P(59)), fill=AMBER, outline=OUTLINE, width=P(1))

    # head side profile, facing right
    head_points = [
        (P(58), P(14)), (P(96), P(14)), (P(102), P(22)), (P(102), P(39)),
        (P(94), P(46)), (P(58), P(46)), (P(52), P(38)), (P(52), P(22)),
    ]
    polygon(d, head_points, STEEL, OUTLINE)
    polygon(d, [
        (P(60), P(19)), (P(94), P(19)), (P(98), P(24)), (P(98), P(37)),
        (P(92), P(42)), (P(60), P(42)), (P(56), P(37)), (P(56), P(24)),
    ], DARK, OUTLINE)
    # side visor strip
    rr(d, (P(84), P(24), P(98), P(36)), P(4), (14, 23, 31, 255), OUTLINE, P(1))
    d.rectangle((P(88), P(28), P(96), P(32)), fill=CYAN, outline=OUTLINE, width=P(1))
    # rear head vent
    for x in (62, 68, 74):
        d.rectangle((P(x), P(27), P(x + 3), P(36)), fill=STEEL_MID)
    # marker light
    ellipse(d, (P(57), P(24), P(63), P(30)), AMBER, OUTLINE, P(1))
    # top handle / antenna
    d.rectangle((P(67), P(11), P(85), P(15)), fill=DARK, outline=OUTLINE, width=P(1))
    line(d, [(P(80), P(11)), (P(80), P(5))], OUTLINE, P(2))
    ellipse(d, (P(76), P(1), P(84), P(9)), AMBER, OUTLINE, P(1))
    ellipse(d, (P(78), P(3), P(82), P(7)), AMBER_LIGHT, None)
    line(d, [(P(92), P(14)), (P(96), P(7))], OUTLINE, P(1.5))
    ellipse(d, (P(93), P(4), P(99), P(10)), CYAN_DIM, OUTLINE, P(1))

    # visible arm, facing right
    ellipse(d, (P(73), P(55), P(87), P(69)), STEEL_MID, OUTLINE, P(1.2))
    rr(d, (P(81), P(62), P(94), P(84)), P(5), STEEL, OUTLINE, P(1.2))
    rr(d, (P(87), P(80), P(99), P(96)), P(4), DARK, OUTLINE, P(1.2))
    d.rectangle((P(90), P(86), P(97), P(89)), fill=AMBER, outline=OUTLINE, width=P(1))
    rr(d, (P(96), P(92), P(106), P(100)), P(3), STEEL_LIGHT, OUTLINE, P(1))
    # torch in front
    rr(d, (P(105), P(83), P(114), P(99)), P(3), DARK, OUTLINE, P(1))
    polygon(d, [(P(107), P(98)), (P(112), P(98)), (P(113), P(108)), (P(106), P(108))], DARK2, OUTLINE)
    line(d, [(P(110), P(107)), (P(116), P(114)), (P(111), P(116))], CYAN, P(1.5))
    ellipse(d, (P(111), P(111), P(117), P(117)), (*CYAN[:3], 90))

    # rear wrench / tool rack
    line(d, [(P(40), P(58)), (P(47), P(78))], OUTLINE, P(3))
    line(d, [(P(40), P(58)), (P(47), P(78))], STEEL_LIGHT, P(1.5))
    ellipse(d, (P(36), P(54), P(44), P(62)), None, OUTLINE, P(3))

    bolts(d, [(48, 47), (92, 47), (48, 88), (92, 88)])
    line(d, [(P(57), P(17)), (P(94), P(17))], (224, 233, 239, 255), P(1))
    return image


def save_view(image: Image.Image, name: str):
    image.save(os.path.join(OUT, f"CK_MaintenanceBot_{name}_512.png"))
    image.resize((SIZE, SIZE), Image.LANCZOS).save(
        os.path.join(OUT, f"CK_MaintenanceBot_{name}_128.png")
    )


def main():
    front = draw_robot()
    north = draw_north()
    east = draw_east()
    west = ImageOps.mirror(east)

    save_view(front, "south")
    save_view(north, "north")
    save_view(east, "east")
    save_view(west, "west")

    # four-view presentation sheet
    panel_w, panel_h = 340, 340
    sheet = Image.new("RGBA", (panel_w * 4, 430), (15, 21, 27, 255))
    draw = ImageDraw.Draw(sheet)
    for x in range(0, sheet.width, 40):
        draw.line([(x, 0), (x, sheet.height)], fill=(28, 42, 49, 80), width=1)
    for y in range(0, sheet.height, 40):
        draw.line([(0, y), (sheet.width, y)], fill=(28, 42, 49, 80), width=1)
    views = [("south", "SOUTH / FRONT"), ("north", "NORTH / BACK"), ("east", "EAST / RIGHT"), ("west", "WEST / LEFT")]
    images = {"south": front, "north": north, "east": east, "west": west}
    for i, (key, label) in enumerate(views):
        sprite = images[key].resize((300, 300), Image.LANCZOS)
        x = i * panel_w + 20
        sheet.alpha_composite(sprite, (x, 40))
        draw.rounded_rectangle((i * panel_w + 10, 10, (i + 1) * panel_w - 10, 420),
                               radius=16, outline=(55, 80, 88, 255), width=1)
        draw.text((i * panel_w + 24, 360), label, fill=(174, 204, 210, 255))
    sheet.convert("RGB").save(os.path.join(OUT, "CK_MaintenanceBot_four_views.png"), quality=95)
    print("wrote four views")


if __name__ == "__main__":
    main()
