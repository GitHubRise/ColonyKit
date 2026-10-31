"""生成热水罐贴图（64x128，透明底）。

和水箱同一套圆柱剪影（半球顶 + 直筒身 + 圆角底 + 黑色描边），配色跟着水箱的深棕走：
上半截暗红珐琅、下半截铜色，中间一道黄铜接缝，两道铁箍，红顶上一块压力表，顶部排汽帽，
右侧两个短管接头。贴着水箱放能看出是一家人，又不会认错。

用法：python tools/generate_hot_water_tank_texture.py
"""
import os

from PIL import Image, ImageChops, ImageDraw

S = 8                       # 超采样倍率
W, H = 64, 128              # 最终尺寸（与 CK_WaterTank.png 一致）
CW, CH = W * S, H * S


def P(v):
    return int(round(v * S))


def box(x0, y0, x1, y1):
    return [P(x0), P(y0), P(x1), P(y1)]


# ── 配色：低饱和、偏暗，和水箱 / 绿色热水器同一档明度 ────────────────
OUTLINE = (8, 7, 7, 255)
RED_HI = (166, 80, 60)
RED = (134, 58, 44)
RED_DK = (96, 40, 32)
COPPER_HI = (172, 114, 76)
COPPER = (148, 94, 60)
COPPER_DK = (100, 60, 40)
BRASS_HI = (206, 172, 112)
BRASS = (168, 132, 74)
BRASS_DK = (110, 84, 46)
IRON_HI = (92, 94, 98)
IRON = (62, 64, 68)
IRON_DK = (40, 41, 44)
CREAM = (228, 222, 204)
NEEDLE = (150, 46, 36)

# 圆柱几何：和水箱可见范围（x 7..57、y 15..99）对齐
X0, X1 = 9.0, 55.0
TOP, BOT = 15.5, 96.0
R = (X1 - X0) / 2
CAP_BOT = TOP + R              # 肩线（y=38.5）：半球到此为止，下面是直筒身
SEAM_Y = 39.5                  # 红顶 / 铜身分界（正好在肩线上）

img = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))


def grad_v(top, bottom):
    im = Image.new("RGBA", (CW, CH))
    gd = ImageDraw.Draw(im)
    for y in range(CH):
        t = y / max(1, CH - 1)
        c = tuple(int(round(top[i] + (bottom[i] - top[i]) * t)) for i in range(3))
        gd.line([(0, y), (CW, y)], fill=c + (255,))
    return im


def mask_of(fn):
    m = Image.new("L", (CW, CH), 0)
    fn(ImageDraw.Draw(m))
    return m


def dilate(mask, radius):
    """圆形结构元膨胀；radius 用 64px 坐标系单位。"""
    acc = Image.new("L", (CW, CH), 0)
    r = max(1, int(round(radius * S)))
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy > r * r:
                continue
            sh = Image.new("L", (CW, CH), 0)
            sh.paste(mask, (dx, dy))
            acc = ImageChops.lighter(acc, sh)
    return acc


def capsule():
    """半球顶 + 直筒身 + 收窄的圆角底（水箱也是这种下缘微微收进去的桶身）。"""
    def f(m):
        m.ellipse(box(X0, TOP, X1, TOP + 2 * R), fill=255)
        m.rectangle(box(X0, CAP_BOT - 1, X1, BOT - 12), fill=255)
        m.rounded_rectangle(box(X0 + 0.8, BOT - 16, X1 - 0.8, BOT), radius=P(6.5), fill=255)
    return mask_of(f)


def above_seam():
    """分界弧线以上的区域（弧线在圆柱面上向下弯 4px）。"""
    def f(m):
        m.rectangle(box(0, 0, 64, SEAM_Y), fill=255)
        m.chord(box(X0, SEAM_Y - 3, X1, SEAM_Y + 3), 0, 180, fill=255)
    return mask_of(f)


body = capsule()

# 右侧两个短管接头：上（出水）下（回水）
stubs = mask_of(lambda m: (
    m.rounded_rectangle(box(50.2, 46.0, 55.6, 51.4), radius=P(1.6), fill=255),
    m.rounded_rectangle(box(50.2, 87.0, 55.6, 92.4), radius=P(1.6), fill=255),
))
stack = mask_of(lambda m: (
    m.rectangle(box(29.2, 10.6, 34.2, 16.6), fill=255),
    m.rounded_rectangle(box(27.6, 7.4, 35.8, 11.6), radius=P(1.5), fill=255),
))
feet = mask_of(lambda m: (
    m.rounded_rectangle(box(13.2, 92.6, 22.8, 98.8), radius=P(1.8), fill=255),
    m.rounded_rectangle(box(41.2, 92.6, 50.8, 98.8), radius=P(1.8), fill=255),
))

silhouette = body
for extra in (stubs, stack, feet):
    silhouette = ImageChops.lighter(silhouette, extra)

# 1) 统一黑色描边
img.paste(Image.new("RGBA", (CW, CH), OUTLINE), (0, 0), dilate(silhouette, 1.15))

# 2) 罐身：铜色竖直渐变
img.paste(grad_v(COPPER_HI, COPPER_DK), (0, 0), body)

# 3) 上半截：红色珐琅
img.paste(grad_v(RED_HI, RED_DK), (0, 0), ImageChops.multiply(body, above_seam()))

# 4) 圆柱明暗
shade = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
sd = ImageDraw.Draw(shade)
sd.rounded_rectangle(box(12.8, 26, 20.6, 90), radius=P(4), fill=(255, 236, 212, 30))
sd.ellipse(box(14.0, 19.0, 37.0, 39.0), fill=(255, 206, 178, 26))
sd.rounded_rectangle(box(46.5, 20, 57.0, 94), radius=P(6), fill=(46, 20, 8, 58))
sd.ellipse(box(30.0, 82.0, 58.0, 100.0), fill=(40, 18, 8, 78))
img.alpha_composite(Image.composite(shade, Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), body))

# 5) 分界黄铜环（顺着弧线）
seam = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
sm = ImageDraw.Draw(seam)
sm.arc(box(X0, SEAM_Y - 4, X1, SEAM_Y + 4), 0, 180, fill=BRASS + (255,), width=P(3.2))
sm.arc(box(X0, SEAM_Y - 5.2, X1, SEAM_Y + 2.8), 0, 180, fill=BRASS_HI + (255,), width=P(0.9))
sm.arc(box(X0, SEAM_Y - 1.8, X1, SEAM_Y + 6.2), 0, 180, fill=BRASS_DK + (255,), width=P(1.0))
img.alpha_composite(seam)

# 6) 铁箍
straps = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
st = ImageDraw.Draw(straps)
for y in (66.0, 82.0):
    st.rectangle(box(X0 - 0.6, y, X1 + 0.6, y + 4.4), fill=IRON + (255,))
    st.rectangle(box(X0 - 0.6, y, X1 + 0.6, y + 1.1), fill=IRON_HI + (255,))
    st.rectangle(box(X0 - 0.6, y + 3.4, X1 + 0.6, y + 4.4), fill=IRON_DK + (255,))
    for x in (13.8, 50.2):
        st.ellipse(box(x - 0.7, y + 1.8, x + 0.7, y + 3.2), fill=IRON_DK + (255,))
img.alpha_composite(Image.composite(straps, Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), body))

# 7) 压力表：放在红顶上（绿热水器也是把表放在这个位置）
gx, gy, gr = 22.5, 54.0, 6.4
gauge = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
gd = ImageDraw.Draw(gauge)
gd.ellipse(box(gx - gr, gy - gr, gx + gr, gy + gr), fill=BRASS_DK + (255,))
gd.ellipse(box(gx - gr + 0.8, gy - gr + 0.8, gx + gr - 0.8, gy + gr - 0.8), fill=BRASS + (255,))
gd.ellipse(box(gx - gr + 1.9, gy - gr + 1.9, gx + gr - 1.9, gy + gr - 1.9), fill=CREAM + (255,))
gd.line([P(gx - 0.4), P(gy + 0.4), P(gx + 3.4), P(gy - 3.0)], fill=NEEDLE + (255,), width=P(0.95))
gd.ellipse(box(gx - 0.7, gy - 0.7, gx + 0.7, gy + 0.7), fill=IRON_DK + (255,))
gd.arc([P(gx - 3.4), P(gy - 3.4), P(gx + 3.4), P(gy + 3.4)], 302, 350, fill=NEEDLE + (255,), width=P(0.8))
img.alpha_composite(Image.composite(gauge, Image.new("RGBA", (CW, CH), (0, 0, 0, 0)), body))

# 8) 短管接头、排汽帽、底脚
img.paste(grad_v(BRASS_HI, BRASS_DK), (0, 0), stubs)
img.paste(grad_v(BRASS_HI, BRASS_DK), (0, 0), stack)
img.paste(grad_v(IRON, IRON_DK), (0, 0), feet)

detail = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
dd = ImageDraw.Draw(detail)
# 短管：开口 + 高光
for y in (46.0, 87.0):
    dd.ellipse(box(53.6, y + 0.6, 55.6, y + 5.2), fill=(74, 54, 28, 255))
    dd.rectangle(box(50.6, y + 0.8, 51.6, y + 4.0), fill=BRASS_HI + (170,))
# 排汽帽：顶面开口 + 管身高光
dd.ellipse(box(27.8, 7.7, 35.6, 10.1), fill=(70, 52, 28, 255))
dd.rectangle(box(27.6, 9.9, 35.8, 11.6), fill=BRASS + (255,))
dd.rectangle(box(29.6, 12.0, 30.6, 16.0), fill=BRASS_HI + (160,))
dd.rectangle(box(33.0, 12.0, 34.0, 16.0), fill=(96, 72, 38, 160))
# 底脚高光
dd.rectangle(box(13.6, 92.9, 22.4, 94.0), fill=IRON_HI + (170,))
dd.rectangle(box(41.6, 92.9, 50.4, 94.0), fill=IRON_HI + (170,))
img.alpha_composite(detail)

# 9) 短管与罐身之间压一道深色边
edge = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
ed = ImageDraw.Draw(edge)
for y in (45.8, 86.8):
    ed.line([P(50.0), P(y), P(50.0), P(y + 5.8)], fill=OUTLINE[:3] + (225,), width=P(1.0))
img.alpha_composite(edge)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Textures", "Things", "Building", "Hygiene", "CK_HotWaterTank.png")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

img.resize((W, H), Image.LANCZOS).save(OUT)
print("wrote", OUT, (W, H))
