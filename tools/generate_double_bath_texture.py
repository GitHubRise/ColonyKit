"""把单缸浴缸贴图裁掉透明边后左右镜像拼成双人浴缸贴图。

每块缸体约 66x130 像素（64 像素/格），拼起来正好落在 2x2 的占地上。
用法：python tools/generate_double_bath_texture.py
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Textures" / "Things" / "Building" / "Hygiene" / "CK_Bath_south.png"
DST = ROOT / "Textures" / "Things" / "Building" / "Hygiene" / "CK_DoubleBath.png"


def main() -> None:
    source = Image.open(SRC).convert("RGBA")
    bbox = source.split()[-1].getbbox()
    tub = source.crop(bbox)
    width, height = tub.size
    canvas = Image.new("RGBA", (width * 2, height), (0, 0, 0, 0))
    canvas.paste(tub, (0, 0))
    canvas.paste(tub.transpose(Image.FLIP_LEFT_RIGHT), (width, 0))
    canvas.save(DST)
    print(f"{DST} {canvas.size[0]}x{canvas.size[1]} drawSize=({canvas.size[0] / 64:.2f},{canvas.size[1] / 64:.2f})")


if __name__ == "__main__":
    main()
