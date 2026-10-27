"""Build the final Mk-V turnaround from the supplied front/side/back renders plus a top plan."""

from __future__ import annotations

import os

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageOps

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PAWNS = os.path.join(ROOT, "docs", "robot-textures", "pawns")
FRONT = os.path.join(PAWNS, "MkV_direct_portrait.png")
SIDE = os.path.join(PAWNS, "MkV_ref_side.png")
BACK = os.path.join(PAWNS, "MkV_ref_back.png")
TOP = os.path.join(PAWNS, "MkV_ref_true_top.png")

SUIT = (232, 240, 245, 255)
SUIT_HI = (252, 254, 255, 255)
SUIT_LO = (171, 189, 202, 255)
SEAM = (196, 212, 224, 255)
DARK = (33, 36, 42, 255)
JOINT = (79, 84, 94, 255)
SILVER = (186, 195, 204, 255)
PURPLE = (156, 112, 255, 255)
HAIR = (22, 23, 27, 255)
HAIR_HI = (56, 59, 67, 255)
SKIN = (232, 204, 190, 255)
SKIN_HI = (250, 228, 214, 255)
LIP = (170, 94, 91, 255)
FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


def cutout(path: str) -> Image.Image:
    im = Image.open(path).convert("RGB")
    arr = np.array(im)
    mask = np.zeros(arr.shape[:2], np.uint8)
    rect = (6, 6, arr.shape[1] - 12, arr.shape[0] - 12)
    bgd = np.zeros((1, 65), np.float64)
    fgd = np.zeros((1, 65), np.float64)
    cv2.grabCut(arr, mask, rect, bgd, fgd, 6, cv2.GC_INIT_WITH_RECT)
    alpha = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
    k = np.ones((3, 3), np.uint8)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_CLOSE, k, iterations=2)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, k, iterations=1)
    contours, _ = cv2.findContours(alpha, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if contours:
        largest = max(contours, key=cv2.contourArea)
        filled = np.zeros_like(alpha)
        cv2.drawContours(filled, [largest], -1, 255, thickness=cv2.FILLED)
        alpha = filled
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0)
    rgba = np.dstack([arr, alpha])
    cut = Image.fromarray(rgba, "RGBA")
    return cut.crop(cut.getbbox())


def normalize(cut: Image.Image, height: int = 700) -> Image.Image:
    w, h = cut.size
    return cut.resize((max(1, int(w * height / h)), height), Image.LANCZOS)


def canvas_from(cut: Image.Image) -> Image.Image:
    im = Image.new("RGBA", (512, 760), (0, 0, 0, 0))
    im.alpha_composite(cut, ((512 - cut.width) // 2, 750 - cut.height))
    return im


def sprite_from(cut: Image.Image) -> Image.Image:
    w, h = cut.size
    figure = cut.resize((max(1, int(w * 124 / h)), 124), Image.LANCZOS)
    canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    shadow = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse((14, 116, 114, 126), fill=(0, 0, 0, 50))
    shadow = shadow.filter(ImageFilter.GaussianBlur(2))
    canvas = Image.alpha_composite(canvas, shadow)
    canvas.alpha_composite(figure, ((128 - figure.width) // 2, 128 - figure.height))
    return canvas


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(d, points, fill, outline=None):
    d.polygon(points, fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line(points, fill=fill, width=width, joint="curve")


def top_plan() -> Image.Image:
    im = Image.new("RGBA", (512, 760), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ellipse(d, (124, 708, 388, 744), (0, 0, 0, 55))
    # legs / feet
    for x in (218, 264):
        rr(d, (x, 410, x + 40, 690), 16, SUIT, SEAM, 1)
        rr(d, (x + 6, 418, x + 34, 660), 10, SUIT_HI, None)
        rr(d, (x + 2, 500, x + 38, 530), 8, DARK, None)
        rr(d, (x + 2, 680, x + 38, 726), 10, SUIT_LO, SEAM, 1)
        d.rectangle((x + 10, 708, x + 30, 716), fill=(250, 252, 253, 255))
    # hips
    ellipse(d, (196, 382, 316, 470), SUIT, SEAM)
    ellipse(d, (208, 392, 304, 458), SUIT_HI, None)
    line(d, [(256, 390), (256, 466)], PURPLE, 2)
    # torso
    rr(d, (214, 208, 298, 396), 20, SUIT, SEAM, 1)
    rr(d, (224, 218, 288, 386), 16, SUIT_HI, None)
    line(d, [(256, 220), (256, 386)], SEAM, 2)
    line(d, [(256, 220), (256, 386)], PURPLE, 1)
    # shoulders
    for x in (174, 286):
        ellipse(d, (x, 214, x + 64, 282), SUIT, SEAM)
        ellipse(d, (x + 12, 228, x + 52, 270), SUIT_HI, None)
        ellipse(d, (x + 24, 238, x + 40, 254), JOINT, None)
        ellipse(d, (x + 29, 243, x + 35, 249), PURPLE, None)
    # arms
    for x in (162, 334):
        rr(d, (x, 272, x + 30, 504), 14, SUIT, SEAM, 1)
        rr(d, (x + 6, 280, x + 24, 486), 8, SUIT_HI, None)
        rr(d, (x + 2, 374, x + 28, 404), 8, DARK, None)
        rr(d, (x + 1, 498, x + 29, 524), 7, SUIT_LO, SEAM, 1)
    # neck
    ellipse(d, (228, 158, 284, 226), JOINT, None)
    ellipse(d, (236, 166, 276, 218), SILVER, None)
    # head top / hair
    ellipse(d, (224, 56, 290, 154), HAIR, None)
    ellipse(d, (232, 64, 282, 146), HAIR_HI, None)
    # ponytail
    polygon(d, [(282, 68), (330, 88), (344, 142), (322, 236), (296, 220), (300, 160), (280, 120)], HAIR, None)
    line(d, [(306, 88), (326, 146), (314, 210)], HAIR_HI, 3)
    # head panel / seams
    ellipse(d, (248, 78, 266, 96), PURPLE, None)
    line(d, [(256, 102), (256, 146)], SEAM, 1)
    # chest / shoulder seams
    line(d, [(214, 244), (180, 256)], SEAM, 2)
    line(d, [(298, 244), (332, 256)], SEAM, 2)
    line(d, [(220, 340), (292, 340)], SEAM, 1)
    return im


def compose(south: Image.Image, north: Image.Image, east: Image.Image, west: Image.Image, top: Image.Image):
    sheet = Image.new("RGBA", (2000, 1040), (14, 20, 26, 255))
    d = ImageDraw.Draw(sheet)
    for x in range(0, 2000, 40):
        d.line((x, 0, x, 1040), fill=(27, 40, 47, 70), width=1)
    d.text((28, 18), "Mk-V · FINAL TURNAROUND", font=font(34, True), fill=(235, 243, 244, 255))
    d.text((30, 58), "front / side / back / high-angle top from the supplied renders · white armor · black ponytail · purple seams", font=font(13), fill=(148, 174, 180, 255))
    items = [("south", "南 / FRONT", south), ("north", "北 / BACK", north), ("east", "东 / RIGHT", east), ("west", "西 / LEFT", west), ("top", "俯视 / TOP", top)]
    for i, (key, label, img) in enumerate(items):
        x = 24 + i * 394
        max_w = 220 if key == "top" else 350
        scale = min(max_w / img.width, 880 / img.height)
        thumb = img.resize((int(img.width * scale), int(img.height * scale)), Image.LANCZOS)
        y = 90 + (max(0, 880 - thumb.height) // 2 if key == "top" else 0)
        sheet.alpha_composite(thumb, (x, y))
        d.text((x, 978), label, font=font(16, True), fill=(230, 240, 242, 255))
        d.text((x, 1002), f"MkV_{key}_512.png", font=font(10), fill=(142, 170, 176, 255))
    sheet.convert("RGB").save(os.path.join(PAWNS, "MkV_turnaround_final.png"), quality=95)


def main():
    front_cut = Image.open(FRONT).convert("RGBA")
    side_cut = cutout(SIDE)
    back_cut = cutout(BACK)

    front = normalize(front_cut)
    side = normalize(side_cut)
    back = normalize(back_cut)
    # The latest supplied render is the true high-angle top view.
    top = normalize(cutout(TOP))

    south = canvas_from(front)
    north = canvas_from(back)
    west = canvas_from(side)              # side render faces left
    east = canvas_from(ImageOps.mirror(side))
    top_canvas = canvas_from(top)

    for key, img, raw in (("south", south, front), ("north", north, back),
                          ("east", east, ImageOps.mirror(side)),
                          ("west", west, side), ("top", top_canvas, top)):
        img.save(os.path.join(PAWNS, f"MkV_{key}_512.png"))
        sprite_from(raw).save(os.path.join(PAWNS, f"MkV_{key}_128.png"))

    compose(south, north, east, west, top_canvas)
    print("wrote final Mk-V turnaround")


if __name__ == "__main__":
    main()
