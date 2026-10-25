"""生成 2x2 全向履带中性贴图（256x256）。

静态贴图只保留金属底板、四向履带、中央转盘；方向箭头由运行时
Building_ConveyorBase.DrawAt 在履带上有物品时叠加，避免空闲时显示错误方向。
"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 1024
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_ConveyorHub.png"

PLATE = (42, 47, 52, 255)
PLATE_DARK = (30, 34, 38, 255)
LANE = (22, 26, 29, 255)
RAIL = (126, 227, 240, 255)
RAIL_DIM = (74, 135, 145, 255)
BOLT = (120, 132, 140, 255)


def main():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    d.rounded_rectangle((24, 24, S - 24, S - 24), radius=54, fill=PLATE, outline=(92, 104, 112, 255), width=11)
    d.rounded_rectangle((44, 44, S - 44, S - 44), radius=40, fill=PLATE_DARK, outline=(66, 74, 80, 255), width=6)

    lane_half = 112
    for rect in [
        (S // 2 - lane_half, 24, S // 2 + lane_half, S // 2),
        (S // 2 - lane_half, S // 2, S // 2 + lane_half, S - 24),
        (24, S // 2 - lane_half, S // 2, S // 2 + lane_half),
        (S // 2, S // 2 - lane_half, S - 24, S // 2 + lane_half),
    ]:
        d.rectangle(rect, fill=LANE)

    rail = 7
    for x in (S // 2 - lane_half, S // 2 + lane_half - rail):
        d.rectangle((x, 24, x + rail, S // 2 - 180), fill=RAIL_DIM)
        d.rectangle((x, S // 2 + 180, x + rail, S - 24), fill=RAIL_DIM)
    for y in (S // 2 - lane_half, S // 2 + lane_half - rail):
        d.rectangle((24, y, S // 2 - 180, y + rail), fill=RAIL_DIM)
        d.rectangle((S // 2 + 180, y, S - 24, y + rail), fill=RAIL_DIM)

    d.ellipse((328, 328, S - 328, S - 328), fill=(48, 55, 61, 255), outline=RAIL, width=13)
    d.ellipse((378, 378, S - 378, S - 378), fill=(28, 33, 37, 255), outline=(93, 108, 117, 255), width=8)
    d.ellipse((444, 444, S - 444, S - 444), fill=(38, 44, 49, 255), outline=RAIL_DIM, width=7)

    for x, y in [(92, 92), (S - 92, 92), (92, S - 92), (S - 92, S - 92)]:
        d.ellipse((x - 12, y - 12, x + 12, y + 12), fill=BOLT, outline=(58, 64, 70, 255), width=3)

    d.rounded_rectangle((30, 30, S - 30, S - 30), radius=48, outline=(118, 132, 141, 255), width=4)

    img = img.resize((256, 256), Image.Resampling.LANCZOS)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
