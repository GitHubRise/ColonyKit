"""生成 2x2 双向转向履带的占位贴图。

贴图是 256x256（2x2 格，128 px/格）：深色金属底板 + 四向履带 +
中央转盘 + 双向箭头。四向都能进出，所以转向是“中央转盘”语义，
放置时旋转 90° 也不会破坏图案。
"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 1024  # 4x 超采样，最后缩到 256
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_ConveyorHub.png"

PLATE = (42, 47, 52, 255)
PLATE_DARK = (30, 34, 38, 255)
LANE = (22, 26, 29, 255)
RAIL = (126, 227, 240, 255)
RAIL_DIM = (74, 135, 145, 255)
AMBER = (232, 196, 90, 255)
BOLT = (120, 132, 140, 255)


def arrow(draw: ImageDraw.ImageDraw, points, color, width=14):
    draw.line(points, fill=color, width=width, joint="curve")


def main():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # 外底板
    d.rounded_rectangle((24, 24, S - 24, S - 24), radius=54, fill=PLATE, outline=(92, 104, 112, 255), width=11)
    d.rounded_rectangle((44, 44, S - 44, S - 44), radius=40, fill=PLATE_DARK, outline=(66, 74, 80, 255), width=6)

    # 四条履带（从四边到中央）
    lane_half = 112
    for rect in [
        (S // 2 - lane_half, 24, S // 2 + lane_half, S // 2),
        (S // 2 - lane_half, S // 2, S // 2 + lane_half, S - 24),
        (24, S // 2 - lane_half, S // 2, S // 2 + lane_half),
        (S // 2, S // 2 - lane_half, S - 24, S // 2 + lane_half),
    ]:
        d.rectangle(rect, fill=LANE)

    # 履带侧轨（青色），只在边缘附近画，避免盖住转盘
    rail = 7
    for x in (S // 2 - lane_half, S // 2 + lane_half - rail):
        d.rectangle((x, 24, x + rail, S // 2 - 180), fill=RAIL_DIM)
        d.rectangle((x, S // 2 + 180, x + rail, S - 24), fill=RAIL_DIM)
    for y in (S // 2 - lane_half, S // 2 + lane_half - rail):
        d.rectangle((24, y, S // 2 - 180, y + rail), fill=RAIL_DIM)
        d.rectangle((S // 2 + 180, y, S - 24, y + rail), fill=RAIL_DIM)

    # 四向双向箭头：每条履带一根双头箭头
    shaft = 10
    head = 28
    # 上
    d.line((S // 2, 80, S // 2, 300), fill=AMBER, width=shaft)
    d.polygon([(S // 2, 56), (S // 2 - head, 104), (S // 2 + head, 104)], fill=AMBER)
    d.polygon([(S // 2, 324), (S // 2 - head, 276), (S // 2 + head, 276)], fill=AMBER)
    # 下
    d.line((S // 2, S - 80, S // 2, S - 300), fill=AMBER, width=shaft)
    d.polygon([(S // 2, S - 56), (S // 2 - head, S - 104), (S // 2 + head, S - 104)], fill=AMBER)
    d.polygon([(S // 2, S - 324), (S // 2 - head, S - 276), (S // 2 + head, S - 276)], fill=AMBER)
    # 左
    d.line((80, S // 2, 300, S // 2), fill=AMBER, width=shaft)
    d.polygon([(56, S // 2), (104, S // 2 - head), (104, S // 2 + head)], fill=AMBER)
    d.polygon([(324, S // 2), (276, S // 2 - head), (276, S // 2 + head)], fill=AMBER)
    # 右
    d.line((S - 80, S // 2, S - 300, S // 2), fill=AMBER, width=shaft)
    d.polygon([(S - 56, S // 2), (S - 104, S // 2 - head), (S - 104, S // 2 + head)], fill=AMBER)
    d.polygon([(S - 324, S // 2), (S - 276, S // 2 - head), (S - 276, S // 2 + head)], fill=AMBER)

    # 中央转盘
    d.ellipse((328, 328, S - 328, S - 328), fill=(48, 55, 61, 255), outline=RAIL, width=13)
    d.ellipse((378, 378, S - 378, S - 378), fill=(28, 33, 37, 255), outline=(93, 108, 117, 255), width=8)
    d.ellipse((444, 444, S - 444, S - 444), fill=(38, 44, 49, 255), outline=RAIL_DIM, width=7)

    # 中央双向旋转箭头
    d.arc((410, 410, S - 410, S - 410), start=20, end=160, fill=RAIL, width=12)
    d.arc((410, 410, S - 410, S - 410), start=200, end=340, fill=AMBER, width=12)
    # 箭头尖
    d.polygon([(S // 2 + 170, 520), (S // 2 + 128, 490), (S // 2 + 138, 548)], fill=RAIL)
    d.polygon([(S // 2 - 170, 504), (S // 2 - 128, 534), (S // 2 - 138, 476)], fill=AMBER)

    # 四角螺栓
    for x, y in [(92, 92), (S - 92, 92), (92, S - 92), (S - 92, S - 92)]:
        d.ellipse((x - 12, y - 12, x + 12, y + 12), fill=BOLT, outline=(58, 64, 70, 255), width=3)

    # 边缘高光
    d.rounded_rectangle((30, 30, S - 30, S - 30), radius=48, outline=(118, 132, 141, 255), width=4)

    img = img.resize((256, 256), Image.Resampling.LANCZOS)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
