"""Draw a soft, toy-like companion robot inspired by the supplied reference.

This is a separate third gallery; it does not touch the first two candidate sets.
"""

from __future__ import annotations

import os
import sys
from dataclasses import dataclass

from PIL import Image, ImageDraw, ImageFont, ImageOps

sys.dont_write_bytecode = True
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools"))

from generate_nannybot_candidates import (  # noqa: E402
    ROOT as REPO_ROOT,
    Style,
    VIEWS,
    image_data_uri,
    render_html,
)

SIZE = 128
SCALE = 4
OUT_DIR = os.path.join(REPO_ROOT, "docs", "nannybot-textures-3")
HTML_PATH = os.path.join(REPO_ROOT, "docs", "nannybot-textures-3.html")

OUTLINE = (76, 98, 104, 255)
WHITE = (250, 252, 252, 255)


@dataclass(frozen=True)
class SoftStyle:
    slug: str
    number: str
    name: str
    subtitle: str
    description: str
    shell: tuple
    shade: tuple
    accent: tuple
    accent_light: tuple
    face: tuple
    eye: tuple
    label: str
    tags: tuple


STYLES = [
    SoftStyle(
        slug="soft-mist",
        number="21",
        name="雪团 · 雾青",
        subtitle="参考图原色",
        description="奶白软壳、青绿色顶盖和侧臂，黑色整块面屏配两枚青色月牙眼。最贴近参考图的清透配色。",
        shell=(239, 245, 247, 255),
        shade=(197, 215, 220, 255),
        accent=(86, 196, 204, 255),
        accent_light=(153, 235, 238, 255),
        face=(15, 20, 24, 255),
        eye=(73, 232, 236, 255),
        label="CK",
        tags=("软体", "雾青", "月牙眼"),
    ),
    SoftStyle(
        slug="soft-sakura",
        number="22",
        name="雪团 · 樱粉",
        subtitle="柔和陪护色",
        description="奶白软壳换成樱花粉顶盖和侧臂，面屏仍是深色，青色眼睛改成粉色月牙。适合病房和儿童房。",
        shell=(249, 241, 245, 255),
        shade=(221, 199, 211, 255),
        accent=(219, 130, 162, 255),
        accent_light=(250, 184, 208, 255),
        face=(21, 16, 21, 255),
        eye=(255, 157, 192, 255),
        label="CK",
        tags=("软体", "樱粉", "陪护"),
    ),
    SoftStyle(
        slug="soft-mint",
        number="23",
        name="雪团 · 薄荷",
        subtitle="清爽园丁色",
        description="奶白软壳配薄荷绿顶盖与侧臂，眼睛是薄荷色月牙。比雾青更偏植物与清洁氛围。",
        shell=(241, 247, 240, 255),
        shade=(198, 217, 202, 255),
        accent=(96, 186, 144, 255),
        accent_light=(169, 233, 192, 255),
        face=(17, 23, 21, 255),
        eye=(113, 242, 180, 255),
        label="CK",
        tags=("软体", "薄荷", "清爽"),
    ),
]


def P(value: float) -> int:
    return int(round(value * SCALE))


def rr(draw: ImageDraw.ImageDraw, box, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def soft_eye(draw: ImageDraw.ImageDraw, style: SoftStyle, x: float, y: float, flip: bool = False):
    """A thick cyan/pink crescent eye, like the reference's sleepy smile eyes."""
    box = (P(x), P(y), P(x + 21), P(y + 15))
    start, end = (200, 340) if not flip else (200, 340)
    draw.arc(box, start, end, fill=style.eye, width=P(6))


def draw_softbot(style: SoftStyle, facing: str) -> Image.Image:
    image = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # ground shadow
    draw.ellipse((P(30), P(109), P(98), P(121)), fill=(0, 0, 0, 42))

    # tiny hidden wheels / base
    if facing in ("south", "north"):
        for x in (44, 70):
            rr(draw, (P(x), P(102), P(x + 14), P(113)), P(6), (57, 68, 72, 255), OUTLINE, P(1))
            draw.ellipse((P(x + 4), P(106), P(x + 10), P(112)), fill=style.accent, outline=OUTLINE, width=P(1))
    else:
        rr(draw, (P(50), P(102), P(76), P(113)), P(6), (57, 68, 72, 255), OUTLINE, P(1))
        draw.ellipse((P(58), P(106), P(68), P(112)), fill=style.accent, outline=OUTLINE, width=P(1))

    # side arms (soft, rounded, tucked against the body)
    if facing in ("south", "north"):
        for x in (20, 88):
            rr(draw, (P(x), P(70), P(x + 20), P(104)), P(10), style.shell, OUTLINE, P(1.3))
            rr(draw, (P(x + 3), P(78), P(x + 12), P(96)), P(5), style.accent, None)
    elif facing == "east":
        rr(draw, (P(88), P(71), P(108), P(104)), P(10), style.shell, OUTLINE, P(1.3))
        rr(draw, (P(93), P(79), P(102), P(96)), P(5), style.accent, None)

    # small torso
    rr(draw, (P(40), P(66), P(88), P(107)), P(17), style.shell, OUTLINE, P(1.5))
    rr(draw, (P(45), P(71), P(83), P(102)), P(13), None, style.shade, P(1))

    if facing == "south":
        # teal collar and chest drop
        rr(draw, (P(42), P(66), P(86), P(75)), P(5), style.accent, None)
        draw.ellipse((P(58), P(76), P(70), P(88)), fill=style.accent)
        draw.polygon([(P(58), P(84)), (P(70), P(84)), (P(64), P(99))], fill=style.accent)
        draw.ellipse((P(60), P(78), P(68), P(84)), fill=style.accent_light)
        # tiny chest label
        try:
            font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", P(4.6))
        except Exception:
            font = ImageFont.load_default()
        draw.text((P(70), P(87)), style.label, font=font, fill=(82, 101, 105, 255))
    elif facing == "north":
        rr(draw, (P(47), P(77), P(81), P(101)), P(8), style.accent, OUTLINE, P(1))
        for y in (83, 89, 95):
            draw.rectangle((P(53), P(y), P(75), P(y + 2)), fill=style.accent_light)
        for x in (43, 77):
            rr(draw, (P(x), P(70), P(x + 12), P(97)), P(6), style.shade, OUTLINE, P(1))
    else:
        rr(draw, (P(46), P(76), P(84), P(99)), P(9), style.accent, OUTLINE, P(1))
        draw.rectangle((P(54), P(82), P(78), P(85)), fill=style.accent_light)
        draw.ellipse((P(60), P(89), P(72), P(96)), fill=style.accent_light)

    # big soft head
    if facing == "east":
        rr(draw, (P(34), P(8), P(98), P(76)), P(34), style.shell, OUTLINE, P(1.5))
        rr(draw, (P(50), P(19), P(94), P(62)), P(20), style.shade, None)
        # side profile face screen
        rr(draw, (P(55), P(25), P(97), P(64)), P(16), style.face, OUTLINE, P(1))
        soft_eye(draw, style, 70, 36)
        # antenna / top cap
        rr(draw, (P(60), P(5), P(92), P(14)), P(4), style.accent, OUTLINE, P(1))
    else:
        rr(draw, (P(22), P(8), P(106), P(77)), P(35), style.shell, OUTLINE, P(1.5))
        rr(draw, (P(28), P(13), P(100), P(71)), P(30), style.shade, None)
        # top teal cap band
        rr(draw, (P(45), P(5), P(83), P(15)), P(5), style.accent, OUTLINE, P(1))
        draw.rectangle((P(51), P(8), P(77), P(10)), fill=style.accent_light)
        # side ear pads
        for x in (18, 102):
            rr(draw, (P(x), P(33), P(x + 10), P(56)), P(5), style.accent, OUTLINE, P(1))

        if facing == "south":
            # large black face screen
            rr(draw, (P(33), P(23), P(95), P(67)), P(24), style.face, OUTLINE, P(1))
            soft_eye(draw, style, 44, 38)
            soft_eye(draw, style, 69, 38)
            draw.arc((P(54), P(51), P(74), P(60)), 20, 160, fill=style.accent_light, width=P(1))
        else:
            # back of head: white shell + teal hatch
            rr(draw, (P(42), P(24), P(86), P(61)), P(13), style.shade, OUTLINE, P(1))
            rr(draw, (P(51), P(30), P(77), P(52)), P(7), style.accent, OUTLINE, P(1))
            for y in (36, 42, 48):
                draw.rectangle((P(56), P(y), P(72), P(y + 2)), fill=style.accent_light)

    return image.resize((SIZE, SIZE), Image.LANCZOS)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    images = {}
    styles = []
    for soft in STYLES:
        images[soft.slug] = {}
        for view in VIEWS:
            image = draw_softbot(soft, view)
            if view == "west":
                image = ImageOps.mirror(images[soft.slug]["east"])
            images[soft.slug][view] = image
            image.save(os.path.join(OUT_DIR, f"{soft.number}_{soft.slug}_{view}.png"))
        styles.append(
            Style(
                slug=soft.slug,
                number=soft.number,
                name=soft.name,
                subtitle=soft.subtitle,
                description=soft.description,
                shell=soft.shell,
                shell_dark=soft.shade,
                accent=soft.accent,
                accent_light=soft.accent_light,
                tread=(57, 68, 72, 255),
                body="soft",
                head="soft",
                base="soft",
                antenna="none",
                badge="none",
                tags=soft.tags,
            )
        )
        print("wrote", soft.number, soft.slug)

    html = render_html(
        images,
        styles,
        "雪团 · 软体保姆机",
        "参考图的柔光软体风格，3 个配色，每个配色含南 / 北 / 东 / 西四向。",
    )
    with open(HTML_PATH, "w", encoding="utf-8") as handle:
        handle.write(html)
    print("wrote", HTML_PATH)


if __name__ == "__main__":
    main()
