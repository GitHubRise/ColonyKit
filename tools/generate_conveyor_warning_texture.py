"""生成「未连接履带」闪烁警告图标（64x64）。"""

from pathlib import Path
from PIL import Image, ImageDraw

S = 256
OUT = Path(__file__).resolve().parents[1] / "Textures" / "Things" / "Building" / "Storage" / "CK_ConveyorWarning.png"

img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 红色圆底
d.ellipse((12, 12, S - 12, S - 12), fill=(44, 26, 24, 235), outline=(255, 92, 70, 255), width=10)

# 履带符号（简化：两段横轨 + 轮子）
d.rounded_rectangle((58, 96, S - 58, 154), radius=18, fill=(255, 210, 120, 255), outline=(255, 150, 60, 255), width=5)
d.line((76, 125, S - 76, 125), fill=(60, 38, 26, 255), width=8)
for x in (86, 128, 170):
    d.ellipse((x - 9, 116, x + 9, 134), fill=(60, 38, 26, 255))

# 红色感叹号
d.rounded_rectangle((S // 2 - 10, 42, S // 2 + 10, 86), radius=8, fill=(255, 80, 64, 255))
d.ellipse((S // 2 - 12, 176, S // 2 + 12, 200), fill=(255, 80, 64, 255))

img = img.resize((64, 64), Image.Resampling.LANCZOS)
OUT.parent.mkdir(parents=True, exist_ok=True)
img.save(OUT)
print(OUT)
