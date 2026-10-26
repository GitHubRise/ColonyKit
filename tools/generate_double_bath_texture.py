"""生成双人浴缸贴图：把单缸贴图裁边后取左半边拉伸并镜像，拼成一张 128x130 的宽缸。

单缸贴图内壁有左右对称的阴影，取左半边镜像后接缝落在缸体正中，
再加中央水龙头，看起来就是一整只双人浴缸（而不是两只缸拼在一起）。
用法：python tools/generate_double_bath_texture.py
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Textures" / "Things" / "Building" / "Hygiene" / "CK_Bath_south.png"
DST = ROOT / "Textures" / "Things" / "Building" / "Hygiene" / "CK_DoubleBath.png"

WIDTH = 128
HEIGHT = 130


def main() -> None:
    source = Image.open(SRC).convert("RGBA")
    tub = source.crop(source.split()[-1].getbbox())
    width, height = tub.size
    # 左半边（含左侧圆角、内壁阴影、以及半个水花）拉伸到一半宽度后镜像，
    # 中缝正好落在缸体中心，左右阴影完全对称。
    half = tub.crop((0, 0, width // 2, height)).resize((WIDTH // 2, HEIGHT), Image.NEAREST)
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    canvas.paste(half, (0, 0))
    canvas.paste(half.transpose(Image.FLIP_LEFT_RIGHT), (WIDTH // 2, 0))
    canvas.save(DST)
    print(f"{DST} {WIDTH}x{HEIGHT} drawSize=({WIDTH / 64:.2f},{HEIGHT / 64:.2f})")


if __name__ == "__main__":
    main()
