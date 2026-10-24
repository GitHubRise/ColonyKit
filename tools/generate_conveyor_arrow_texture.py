"""生成履带跑马灯箭头（64x64，透明背景，指向北）。"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 256
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_ConveyorArrow.png"

img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 外发光
d.polygon([(S // 2, 30), (S - 42, 116), (S - 78, 116), (S - 78, 170),
           (S // 2 + 24, 170), (S // 2 + 24, 224), (S // 2 - 24, 224),
           (S // 2 - 24, 170), (78, 170), (78, 116), (42, 116)],
          fill=(255, 222, 110, 70))
# 主体
d.polygon([(S // 2, 48), (S - 66, 126), (S - 96, 126), (S - 96, 184),
           (S // 2 + 30, 184), (S // 2 + 30, 216), (S // 2 - 30, 216),
           (S // 2 - 30, 184), (96, 184), (96, 126), (66, 126)],
          fill=(255, 236, 160, 245), outline=(255, 196, 60, 255), width=8)

img = img.resize((64, 64), Image.Resampling.LANCZOS)
OUT.parent.mkdir(parents=True, exist_ok=True)
img.save(OUT)
print(OUT)
