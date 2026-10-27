"""Build the direct-use Mk-V asset from the supplied reference image."""

from __future__ import annotations

import os

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PAWNS = os.path.join(ROOT, "docs", "robot-textures", "pawns")
SOURCE = os.path.join(PAWNS, "MkV_source_reference.png")
os.makedirs(PAWNS, exist_ok=True)


def cutout() -> Image.Image:
    im = Image.open(SOURCE).convert("RGB")
    arr = np.array(im)
    mask = np.zeros(arr.shape[:2], np.uint8)
    rect = (8, 8, arr.shape[1] - 16, arr.shape[0] - 16)
    bgd = np.zeros((1, 65), np.float64)
    fgd = np.zeros((1, 65), np.float64)
    cv2.grabCut(arr, mask, rect, bgd, fgd, 6, cv2.GC_INIT_WITH_RECT)
    alpha = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
    kernel = np.ones((3, 3), np.uint8)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_CLOSE, kernel, iterations=2)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, kernel, iterations=1)
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


def make_sprite(cut: Image.Image) -> Image.Image:
    w, h = cut.size
    sprite = cut.resize((max(1, int(w * (124 / h))), 124), Image.LANCZOS)
    canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    shadow = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse((14, 116, 114, 126), fill=(0, 0, 0, 55))
    shadow = shadow.filter(ImageFilter.GaussianBlur(2))
    canvas = Image.alpha_composite(canvas, shadow)
    canvas.alpha_composite(sprite, ((128 - sprite.width) // 2, 128 - sprite.height))
    return canvas


def make_preview(cut: Image.Image):
    W, H = 760, 980
    panel = Image.new("RGBA", (W, H), (14, 20, 26, 255))
    d = ImageDraw.Draw(panel)
    for x in range(0, W, 40):
        d.line((x, 0, x, H), fill=(27, 40, 47, 70), width=1)
    for y in range(0, H, 40):
        d.line((0, y, W, y), fill=(27, 40, 47, 70), width=1)
    title = ImageFont.truetype(r"C:\Windows\Fonts\msyhbd.ttc", 30)
    small = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 14)
    d.text((28, 20), "Mk-V · DIRECT-USE ASSET", font=title, fill=(235, 243, 244, 255))
    d.text((30, 58), "参考图直接抠图 / 保留全身 / 仅做缩放与地面阴影", font=small, fill=(148, 174, 180, 255))
    x = (W - cut.width) // 2
    y = 100
    panel.alpha_composite(cut, (x, y))
    d.rectangle((x, y, x + cut.width, y + cut.height), outline=(48, 70, 79, 255), width=1)
    d.text((30, H - 110), "MkV_direct_512.png", font=small, fill=(232, 178, 83, 255))
    d.text((30, H - 84), "南向 128 px 版本：MkV_direct_south_128.png", font=small, fill=(148, 174, 180, 255))
    d.text((30, H - 58), "单张正面图只能直接生成南向；北/东/西需要按此风格补绘。", font=small, fill=(148, 174, 180, 255))
    panel.convert("RGB").save(os.path.join(PAWNS, "MkV_direct_preview.png"), quality=95)


def main():
    cut = cutout()
    cut.save(os.path.join(PAWNS, "MkV_direct_portrait.png"))
    w, h = cut.size
    cut.resize((max(1, int(w * (512 / h))), 512), Image.LANCZOS).save(os.path.join(PAWNS, "MkV_direct_512.png"))
    make_sprite(cut).save(os.path.join(PAWNS, "MkV_direct_south_128.png"))
    make_preview(cut)
    print("wrote direct-use Mk-V assets")


if __name__ == "__main__":
    main()
