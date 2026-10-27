"""生成便携脸盆的物品贴图（128x128，透明底）。

用法：python tools/generate_washbasin_texture.py
"""
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Textures", "Things", "Item", "CK_WashBasin.png")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 外沿（金属盆沿）
d.ellipse([16, 34, 112, 98], fill=(168, 170, 176, 255), outline=(92, 94, 100, 255), width=4)
# 内壁
d.ellipse([25, 42, 103, 90], fill=(86, 92, 102, 255))
# 水面
d.ellipse([29, 47, 99, 85], fill=(78, 152, 202, 240))
# 水面高光
d.arc([31, 50, 97, 83], 195, 255, fill=(214, 238, 252, 255), width=3)
# 盆沿高光
d.arc([18, 36, 110, 96], 200, 250, fill=(236, 238, 242, 255), width=3)
# 盆底阴影
d.arc([30, 86, 83, 96], 20, 150, fill=(50, 54, 60, 200), width=3)

img.save(OUT)
print("wrote", OUT, img.size)
