"""生成 2x2 立体库贴图（256x256），无履带接口。"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 1024
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_VerticalWarehouse.png"

PLATE = (42, 47, 52, 255)
PLATE_DARK = (28, 32, 36, 255)
RAIL = (126, 227, 240, 255)
RAIL_DIM = (74, 135, 145, 255)
AMBER = (232, 196, 90, 255)
BOLT = (120, 132, 140, 255)


def main():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((24, 24, S - 24, S - 24), radius=60, fill=PLATE, outline=(92, 104, 112, 255), width=12)
    d.rounded_rectangle((48, 48, S - 48, S - 48), radius=42, fill=PLATE_DARK, outline=(66, 74, 80, 255), width=7)

    for y in (230, 430, 630, 820):
        d.line((90, y, S - 90, y), fill=(45, 52, 58, 255), width=8)

    d.ellipse((S // 2 - 150, S // 2 - 150, S // 2 + 150, S // 2 + 150),
              fill=(38, 44, 49, 255), outline=RAIL, width=12)
    d.ellipse((S // 2 - 90, S // 2 - 90, S // 2 + 90, S // 2 + 90),
              fill=(26, 31, 35, 255), outline=RAIL_DIM, width=7)
    for angle in range(0, 360, 45):
        import math
        x1 = S // 2 + int(55 * math.cos(math.radians(angle)))
        y1 = S // 2 + int(55 * math.sin(math.radians(angle)))
        x2 = S // 2 + int(115 * math.cos(math.radians(angle)))
        y2 = S // 2 + int(115 * math.sin(math.radians(angle)))
        d.line((x1, y1, x2, y2), fill=RAIL, width=7)
    d.ellipse((S // 2 - 26, S // 2 - 26, S // 2 + 26, S // 2 + 26), fill=AMBER)

    for x, y in [(88, 88), (S - 88, 88), (88, S - 88), (S - 88, S - 88)]:
        d.ellipse((x - 14, y - 14, x + 14, y + 14), fill=BOLT, outline=(58, 64, 70, 255), width=3)

    d.rounded_rectangle((32, 32, S - 32, S - 32), radius=52, outline=(118, 132, 141, 255), width=5)
    img = img.resize((256, 256), Image.Resampling.LANCZOS)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
