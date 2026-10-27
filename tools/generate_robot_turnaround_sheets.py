"""Create one all-direction review sheet per Mk robot variant."""

from __future__ import annotations

import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "robot-textures", "pawns")
OUT = SRC

BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
AMBER = (232, 178, 83, 255)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


F_TITLE = font(34, True)
F_SUB = font(14)
F_LABEL = font(17, True)
F_SMALL = font(11)
F_TINY = font(9)

ROBOTS = [
    ("MkI", "Mk-I 标准保姆机", "STANDARD NANNY", "T1 · 1×1 · 蓝色标准型"),
    ("MkII", "Mk-II 模块化保姆机", "MODULAR NANNY", "T2 · 1×1 · 绿色模块型"),
    ("MkIII", "Mk-III 重载保姆机", "HEAVY-DUTY NANNY", "T3 · 1×1 · 琥珀重载型"),
    ("MkIV", "Mk-IV 前哨保姆机", "OUTPOST NANNY", "T4 · 1×1 · 暗灰/橙前哨型"),
    ("MkV", "Mk-V 自维护保姆机", "SELF-MAINTAINING NANNY", "T5 · 1×1 · 象牙/金旗舰型"),
]


def sheet(tier: str, name: str, en: str, spec: str):
    W, H = 1400, 560
    im = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(im)
    for x in range(0, W, 40):
        d.line((x, 0, x, H), fill=(27, 40, 47, 70), width=1)
    d.text((28, 20), f"{tier} · {en} · ALL DIRECTIONS", font=F_TITLE, fill=TEXT)
    d.text((30, 64), f"{name} · {spec} · 128 px game sprites", font=F_SUB, fill=MUTED)
    d.text((W - 180, 64), "SOUTH ↓", font=F_SMALL, fill=AMBER)

    views = [("south", "南 / SOUTH"), ("north", "北 / NORTH"), ("east", "东 / EAST"), ("west", "西 / WEST")]
    for i, (key, label) in enumerate(views):
        x = 30 + i * 340
        y = 110
        img = Image.open(os.path.join(SRC, f"{tier}_{key}.png")).convert("RGBA")
        # Pixel-art review: keep hard pixels, exactly 2.5x.
        img = img.resize((320, 320), Image.NEAREST)
        im.alpha_composite(img, (x, y))
        d.rectangle((x, y, x + 320, y + 320), outline=LINE, width=1)
        d.text((x, y + 335), label, font=F_LABEL, fill=TEXT)
        d.text((x, y + 362), f"{tier}_{key}.png · 128×128", font=F_SMALL, fill=MUTED)
    d.text((30, H - 48), "Direction review sheet · south-facing gameplay sprite · north/east/west are the other three rotations.", font=F_TINY, fill=MUTED)
    im.convert("RGB").save(os.path.join(OUT, f"{tier}_turnaround.png"), quality=96)
    print("wrote", f"{tier}_turnaround.png")


def main():
    for tier, name, en, spec in ROBOTS:
        sheet(tier, name, en, spec)


if __name__ == "__main__":
    main()
