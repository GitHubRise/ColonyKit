"""生成 1x1 双向履带中性贴图（128x128）。

静态贴图只保留底板、履带、导轨、中央转轴；方向箭头由运行时在
履带上有物品时叠加。
"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 512
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_ConveyorBelt.png"

PLATE = (42, 47, 52, 255)
PLATE_DARK = (30, 34, 38, 255)
LANE = (22, 26, 29, 255)
RAIL = (126, 227, 240, 255)
RAIL_DIM = (74, 135, 145, 255)
BOLT = (120, 132, 140, 255)


def main():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    d.rounded_rectangle((14, 14, S - 14, S - 14), radius=34, fill=PLATE, outline=(92, 104, 112, 255), width=7)
    d.rounded_rectangle((26, 26, S - 26, S - 26), radius=24, fill=PLATE_DARK, outline=(66, 74, 80, 255), width=4)

    lane = S // 2
    half = 74
    d.rectangle((lane - half, 12, lane + half, S - 12), fill=LANE)

    rail = 5
    d.rectangle((lane - half, 12, lane - half + rail, S - 12), fill=RAIL_DIM)
    d.rectangle((lane + half - rail, 12, lane + half, S - 12), fill=RAIL_DIM)

    d.ellipse((lane - 30, S // 2 - 30, lane + 30, S // 2 + 30), fill=(38, 44, 49, 255), outline=RAIL, width=7)
    d.ellipse((lane - 12, S // 2 - 12, lane + 12, S // 2 + 12), fill=(24, 28, 32, 255), outline=RAIL_DIM, width=4)

    for x, y in [(58, 58), (S - 58, 58), (58, S - 58), (S - 58, S - 58)]:
        d.ellipse((x - 7, y - 7, x + 7, y + 7), fill=BOLT, outline=(58, 64, 70, 255), width=2)

    d.rounded_rectangle((19, 19, S - 19, S - 19), radius=30, outline=(118, 132, 141, 255), width=3)

    img = img.resize((128, 128), Image.Resampling.LANCZOS)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
