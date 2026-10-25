"""Generate the two blueprint designator icons.

Run from the repository root:

    python tools/generate_blueprint_icons.py

Outputs 64x64 RGBA PNGs under Textures/UI/Blueprint/. Drawn at 4x and
downsampled for cheap antialiasing, matching the flat line-art style of the
other ColonyKit placeholder textures.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

SCALE = 4
SIZE = 64

INK = (28, 38, 50, 255)
CYAN = (126, 206, 255, 255)
CYAN_SOFT = (126, 206, 255, 170)
WHITE = (236, 244, 250, 255)
GREEN = (150, 226, 150, 255)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def s(value: float) -> int:
    return int(round(value * SCALE))


def line(draw: ImageDraw.ImageDraw, points, fill, width=1.0):
    draw.line([(s(x), s(y)) for x, y in points], fill=fill, width=s(width), joint="curve")


def dashed_rect(draw: ImageDraw.ImageDraw, box, fill, width=1.4, dash=4.0, gap=3.0):
    x0, y0, x1, y1 = box
    edges = [
        ((x0, y0), (x1, y0)),
        ((x1, y0), (x1, y1)),
        ((x1, y1), (x0, y1)),
        ((x0, y1), (x0, y0)),
    ]
    for (ax, ay), (bx, by) in edges:
        length = math.hypot(bx - ax, by - ay)
        if length <= 0:
            continue
        ux, uy = (bx - ax) / length, (by - ay) / length
        position = 0.0
        while position < length:
            end = min(position + dash, length)
            line(
                draw,
                [
                    (ax + ux * position, ay + uy * position),
                    (ax + ux * end, ay + uy * end),
                ],
                fill,
                width,
            )
            position = end + gap


def house(draw: ImageDraw.ImageDraw, cx: float, cy: float, half: float, color, width=1.4):
    left = cx - half
    right = cx + half
    bottom = cy + half
    eave = cy - half * 0.05
    ridge = cy - half * 1.15

    # walls + roof
    line(draw, [(left, eave), (left, bottom), (right, bottom), (right, eave)], color, width)
    line(draw, [(left - half * 0.18, eave), (cx, ridge), (right + half * 0.18, eave)], color, width)
    # door
    door = half * 0.36
    line(
        draw,
        [(cx - door, bottom), (cx - door, bottom - half * 0.55), (cx + door, bottom - half * 0.55), (cx + door, bottom)],
        color,
        width * 0.85,
    )


def corners(draw: ImageDraw.ImageDraw, box, color, arm=4.0, width=1.6):
    x0, y0, x1, y1 = box
    for x, y, dx, dy in (
        (x0, y0, 1, 1),
        (x1, y0, -1, 1),
        (x0, y1, 1, -1),
        (x1, y1, -1, -1),
    ):
        line(draw, [(x, y), (x + dx * arm, y)], color, width)
        line(draw, [(x, y), (x, y + dy * arm)], color, width)


def capture_icon() -> Image.Image:
    image, draw = canvas()
    box = (10, 10, 54, 54)

    # drop shadow / dark backing so the icon reads on the architect panel
    dashed_rect(draw, (box[0] + 0.8, box[1] + 0.8, box[2] + 0.8, box[3] + 0.8), INK, 2.2)
    dashed_rect(draw, box, CYAN, 1.4)
    corners(draw, box, WHITE)

    house(draw, 32, 35, 10, WHITE, 1.5)

    # tiny grid dots
    for x in (22, 32, 42):
        for y in (46,):
            draw.ellipse(
                (s(x) - s(0.8), s(y) - s(0.8), s(x) + s(0.8), s(y) + s(0.8)),
                fill=CYAN_SOFT,
            )
    return image


def place_icon() -> Image.Image:
    image, draw = canvas()

    # ground grid
    line(draw, [(12, 48), (52, 48)], INK, 2.4)
    line(draw, [(12, 48), (52, 48)], CYAN_SOFT, 1.2)
    for x in range(16, 53, 8):
        line(draw, [(x, 45), (x, 51)], CYAN_SOFT, 0.9)

    house(draw, 32, 33, 11, WHITE, 1.5)

    # downward placement arrow
    line(draw, [(46, 16), (46, 28)], GREEN, 1.8)
    draw.polygon(
        [(s(42), s(27)), (s(50), s(27)), (s(46), s(34))],
        fill=GREEN,
    )
    return image


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((SIZE, SIZE), Image.LANCZOS).save(path)


def main() -> None:
    root = Path(__file__).resolve().parent.parent
    out = root / "Textures" / "UI" / "Blueprint"
    save(capture_icon(), out / "Capture.png")
    save(place_icon(), out / "Place.png")
    print(f"wrote {out / 'Capture.png'}")
    print(f"wrote {out / 'Place.png'}")


if __name__ == "__main__":
    main()
