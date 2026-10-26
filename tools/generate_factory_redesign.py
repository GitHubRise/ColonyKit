"""Redesign concepts for the frame factory, two-station factory and prefab factory."""

from __future__ import annotations

import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures", "CK_Factory_Redesign.png")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

W, H = 2400, 1680
MARGIN = 40
LABEL_W = 300
FRONT_X = 360
FRONT_W = 700
TOP_X = 1095
TOP_W = 700
NOTE_X = 1830
NOTE_W = 530
ROW_H = 480
TOP_Y = 130

BG = (14, 20, 26, 255)
PANEL = (19, 28, 35, 255)
LINE = (48, 70, 79, 255)
TEXT = (230, 240, 242, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (25, 32, 40, 255)
DARK = (46, 56, 67, 255)
DARK2 = (34, 42, 51, 255)
WALL = (57, 68, 80, 255)
STEEL = (136, 150, 162, 255)
STEEL_LIGHT = (196, 207, 214, 255)
STEEL_MID = (163, 176, 187, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)
TREAD = (38, 44, 52, 255)

FONT = r"C:\Windows\Fonts\msyh.ttc"
FONT_B = r"C:\Windows\Fonts\msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT, size)


F_TITLE = font(38, True)
F_SUB = font(16)
F_GROUP = font(22, True)
F_LABEL = font(20, True)
F_SMALL = font(12)
F_TINY = font(10)


def rr(d, box, r, fill, outline=None, width=1):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)


def ellipse(d, box, fill, outline=None, width=1):
    d.ellipse(box, fill=fill, outline=outline, width=width)


def polygon(d, points, fill, outline=None):
    d.polygon(points, fill=fill, outline=outline)


def line(d, points, fill, width=1):
    d.line(points, fill=fill, width=width, joint="curve")


def bolt(d, x, y, r=3):
    ellipse(d, (x - r, y - r, x + r, y + r), STEEL_LIGHT, OUTLINE, 1)


def hazard(d, x1, y1, x2, y2, n=4):
    d.rectangle((x1, y1, x2, y2), fill=DARK2, outline=OUTLINE, width=1)
    step = (x2 - x1) / n
    for i in range(n):
        x = x1 + i * step
        polygon(d, [(x, y2), (x + step * 0.55, y2), (x + step * 0.9, y1), (x + step * 0.35, y1)], AMBER)


def spark(d, x, y, n=8, spread=28):
    for i in range(n):
        a = (i / max(1, n - 1) - 0.5) * 2.2
        length = spread * (0.45 + 0.55 * ((i * 37) % 10) / 10)
        line(d, [(x, y), (x + math.cos(a) * length, y + math.sin(a) * length)], ORANGE, 2)
        line(d, [(x, y), (x + math.cos(a) * length * 0.7, y + math.sin(a) * length * 0.7)], AMBER_LIGHT, 1)


# ---------------------------------------------------------------- frame fab

def frame_front(d, ox, oy):
    x, y = ox + 40, oy + 10
    # ground
    d.rectangle((x, y + 370, x + 620, y + 392), fill=(10, 16, 21, 255))
    hazard(d, x + 120, y + 356, x + 520, y + 370, 6)
    # side towers / open frame
    rr(d, (x + 70, y + 132, x + 160, y + 372), 5, DARK, OUTLINE, 2)
    rr(d, (x + 470, y + 132, x + 560, y + 372), 5, DARK, OUTLINE, 2)
    for yy in range(y + 160, y + 350, 30):
        d.rectangle((x + 82, yy, x + 148, yy + 5), fill=STEEL_MID)
        d.rectangle((x + 482, yy, x + 548, yy + 5), fill=STEEL_MID)
    # roof truss
    d.rectangle((x + 88, y + 100, x + 542, y + 120), fill=STEEL, outline=OUTLINE, width=2)
    for xx in range(x + 110, x + 540, 40):
        line(d, [(xx, y + 120), (xx + 32, y + 132)], STEEL_MID, 3)
    # top gantry rail
    d.rectangle((x + 112, y + 76, x + 520, y + 90), fill=STEEL_MID, outline=OUTLINE, width=1)
    d.rectangle((x + 296, y + 82, x + 330, y + 104), fill=AMBER, outline=OUTLINE, width=1)
    # hoist cable and frame
    line(d, [(x + 312, y + 104), (x + 312, y + 170)], OUTLINE, 3)
    polygon(d, [(x + 300, y + 168), (x + 324, y + 168), (x + 312, y + 184)], AMBER, OUTLINE)
    # robot frame skeleton
    line(d, [(x + 250, y + 180), (x + 376, y + 180), (x + 376, y + 286), (x + 250, y + 286), (x + 250, y + 180)], STEEL_LIGHT, 5)
    line(d, [(x + 250, y + 180), (x + 376, y + 286)], STEEL_LIGHT, 4)
    line(d, [(x + 376, y + 180), (x + 250, y + 286)], STEEL_LIGHT, 4)
    for px, py in ((x + 250, y + 180), (x + 376, y + 180), (x + 250, y + 286), (x + 376, y + 286)):
        ellipse(d, (px - 6, py - 6, px + 6, py + 6), AMBER, OUTLINE, 1)
    # welding arms
    for side, sx in ((-1, x + 160), (1, x + 470)):
        line(d, [(sx, y + 220), (x + 312 + side * 58, y + 240), (x + 312 + side * 30, y + 254)], OUTLINE, 8)
        line(d, [(sx, y + 220), (x + 312 + side * 58, y + 240), (x + 312 + side * 30, y + 254)], STEEL_LIGHT, 4)
        spark(d, x + 312 + side * 30, y + 254, 10, 36)
    # material rack
    for i in range(4):
        d.rectangle((x + 172, y + 300 + i * 12, x + 242, y + 306 + i * 12), fill=STEEL_MID, outline=OUTLINE, width=1)
    # sign
    rr(d, (x + 258, y + 44, x + 374, y + 72), 4, DARK2, OUTLINE, 1)
    d.text((x + 270, y + 49), "FRAME FAB", font=F_SMALL, fill=AMBER)
    # roof lights
    for xx in (x + 190, x + 430):
        ellipse(d, (xx - 5, y + 96, xx + 5, y + 106), AMBER, OUTLINE, 1)
    return


def frame_top(d, ox, oy):
    x, y = ox, oy
    # 2x2 footprint
    rr(d, (x + 120, y + 40, x + 580, y + 390), 8, DARK, OUTLINE, 2)
    rr(d, (x + 132, y + 52, x + 568, y + 378), 5, WALL, OUTLINE, 1)
    # open roof / canopy strips
    d.rectangle((x + 132, y + 52, x + 568, y + 82), fill=STEEL_MID, outline=OUTLINE, width=1)
    d.rectangle((x + 132, y + 348, x + 568, y + 378), fill=STEEL_MID, outline=OUTLINE, width=1)
    # gantry rails
    d.rectangle((x + 150, y + 90, x + 550, y + 102), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
    d.rectangle((x + 150, y + 330, x + 550, y + 342), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
    # crane trolley / hook
    d.rectangle((x + 316, y + 90, x + 384, y + 126), fill=AMBER, outline=OUTLINE, width=1)
    line(d, [(x + 350, y + 126), (x + 350, y + 174)], OUTLINE, 3)
    # frame jig
    line(d, [(x + 270, y + 176), (x + 430, y + 176), (x + 430, y + 290), (x + 270, y + 290), (x + 270, y + 176)], STEEL_LIGHT, 4)
    for px, py in ((x + 270, y + 176), (x + 430, y + 176), (x + 270, y + 290), (x + 430, y + 290)):
        ellipse(d, (px - 5, py - 5, px + 5, py + 5), AMBER, OUTLINE, 1)
    # side racks
    for side in (-1, 1):
        bx = x + 350 + side * 190
        for i in range(4):
            d.rectangle((bx - 28, y + 160 + i * 18, bx + 28, y + 166 + i * 18), fill=STEEL_MID, outline=OUTLINE, width=1)
    # sparks at jig corners
    spark(d, x + 270, y + 176, 7, 26)
    spark(d, x + 430, y + 176, 7, 26)
    # south opening / hazard
    d.rectangle((x + 300, y + 370, x + 400, y + 386), fill=(10, 16, 21, 255), outline=OUTLINE, width=1)
    hazard(d, x + 300, y + 360, x + 400, y + 370, 4)
    return


# ---------------------------------------------------------------- two-station

def two_front(d, ox, oy):
    x, y = ox + 20, oy + 10
    d.rectangle((x, y + 370, x + 660, y + 392), fill=(10, 16, 21, 255))
    hazard(d, x + 70, y + 356, x + 590, y + 370, 7)
    # main body
    rr(d, (x + 40, y + 160, x + 620, y + 372), 5, WALL, OUTLINE, 2)
    # twin sawtooth roof
    for sx, col in ((x + 40, STEEL), (x + 330, STEEL_MID)):
        polygon(d, [(sx, y + 160), (sx + 290, y + 160), (sx + 250, y + 96), (sx + 50, y + 96)], col, OUTLINE)
        polygon(d, [(sx + 30, y + 156), (sx + 260, y + 156), (sx + 228, y + 110), (sx + 58, y + 110)], DARK, OUTLINE)
        # skylight strip
        polygon(d, [(sx + 76, y + 118), (sx + 214, y + 118), (sx + 194, y + 128), (sx + 96, y + 128)], CYAN_DIM, OUTLINE)
    # central tool tower
    rr(d, (x + 304, y + 84, x + 356, y + 352), 4, DARK, OUTLINE, 2)
    for yy in (y + 104, y + 132, y + 272):
        d.rectangle((x + 312, yy, x + 348, yy + 6), fill=AMBER)
    d.rectangle((x + 314, y + 154, x + 346, y + 192), fill=CYAN_DIM, outline=OUTLINE, width=1)
    d.rectangle((x + 318, y + 164, x + 342, y + 172), fill=CYAN)
    # two doors
    for sx, col in ((x + 74, AMBER), (x + 382, CYAN)):
        rr(d, (sx, y + 226, sx + 206, y + 358), 4, DARK2, STEEL_LIGHT, 2)
        rr(d, (sx + 10, y + 236, sx + 196, y + 348), 2, (30, 38, 47, 255), OUTLINE, 1)
        for yy in range(y + 244, y + 344, 13):
            d.rectangle((sx + 18, yy, sx + 188, yy + 5), fill=STEEL_MID)
        d.rectangle((sx + 10, y + 236, sx + 196, y + 244), fill=col, outline=OUTLINE, width=1)
        # station light
        ellipse(d, (sx + 84, y + 208, sx + 124, y + 222), col, OUTLINE, 1)
    # conveyor at bottom
    d.rectangle((x + 74, y + 366, x + 590, y + 386), fill=DARK2, outline=OUTLINE, width=1)
    for xx in range(x + 90, x + 580, 24):
        d.rectangle((xx, y + 370, xx + 10, y + 382), fill=STEEL_MID)
    # sign
    rr(d, (x + 260, y + 52, x + 400, y + 78), 4, DARK2, OUTLINE, 1)
    d.text((x + 270, y + 57), "2-STATION", font=F_SMALL, fill=AMBER)
    return


def two_top(d, ox, oy):
    x, y = ox, oy
    # 4x3 footprint, 520x390
    rr(d, (x + 90, y + 30, x + 610, y + 390), 8, DARK, OUTLINE, 2)
    rr(d, (x + 102, y + 42, x + 598, y + 378), 5, WALL, OUTLINE, 1)
    # two roof cells, sawtooth strips
    for sx in (x + 102, x + 350):
        rr(d, (sx, y + 42, sx + 248, y + 260), 4, STEEL_MID, OUTLINE, 1)
        rr(d, (sx + 12, y + 54, sx + 236, y + 248), 3, PANEL, OUTLINE, 1)
        for i in range(4):
            d.rectangle((sx + 30, y + 78 + i * 38, sx + 218, y + 86 + i * 38), fill=CYAN_DIM, outline=OUTLINE, width=1)
        # south door
        d.rectangle((sx + 70, y + 344, sx + 178, y + 378), fill=AMBER if sx < x + 350 else CYAN, outline=OUTLINE, width=1)
        hazard(d, sx + 74, y + 330, sx + 174, y + 340, 4)
    # central shared tool tower / conveyor spine
    d.rectangle((x + 330, y + 42, x + 370, y + 344), fill=DARK, outline=OUTLINE, width=2)
    d.rectangle((x + 338, y + 70, x + 362, y + 154), fill=AMBER, outline=OUTLINE, width=1)
    d.rectangle((x + 338, y + 178, x + 362, y + 226), fill=CYAN_DIM, outline=OUTLINE, width=1)
    # material input north
    d.rectangle((x + 180, y + 42, x + 250, y + 70), fill=DARK2, outline=OUTLINE, width=1)
    d.rectangle((x + 450, y + 42, x + 520, y + 70), fill=DARK2, outline=OUTLINE, width=1)
    # output conveyor south
    d.rectangle((x + 180, y + 344, x + 520, y + 368), fill=DARK2, outline=OUTLINE, width=1)
    for xx in range(x + 190, x + 510, 24):
        d.rectangle((xx, y + 350, xx + 10, y + 362), fill=STEEL_MID)
    # station lights
    for sx, col in ((x + 180, AMBER), (x + 430, CYAN)):
        ellipse(d, (sx, y + 300, sx + 28, y + 316), col, OUTLINE, 1)
    return


# ---------------------------------------------------------------- prefab

def prefab_front(d, ox, oy):
    x, y = ox + 30, oy + 10
    d.rectangle((x, y + 370, x + 640, y + 392), fill=(10, 16, 21, 255))
    hazard(d, x + 90, y + 356, x + 550, y + 370, 6)
    # main hall
    rr(d, (x + 80, y + 150, x + 560, y + 370), 5, WALL, OUTLINE, 2)
    polygon(d, [(x + 70, y + 152), (x + 570, y + 152), (x + 530, y + 100), (x + 110, y + 100)], STEEL, OUTLINE)
    polygon(d, [(x + 100, y + 148), (x + 540, y + 148), (x + 506, y + 116), (x + 134, y + 116)], DARK, OUTLINE)
    # stacked containers on roof
    colors = (STEEL_MID, AMBER, CYAN_DIM)
    for i in range(3):
        cx = x + 110 + i * 116
        rr(d, (cx, y + 42, cx + 96, y + 92), 3, colors[i % 3], OUTLINE, 1)
        d.rectangle((cx + 10, y + 52, cx + 86, y + 58), fill=DARK2, outline=OUTLINE, width=1)
        d.text((cx + 14, y + 66), f"M{i+1}", font=F_TINY, fill=TEXT if i != 1 else DARK2)
    # yellow gantry crane
    line(d, [(x + 40, y + 340), (x + 40, y + 96), (x + 600, y + 96), (x + 600, y + 340)], OUTLINE, 12)
    line(d, [(x + 40, y + 340), (x + 40, y + 96), (x + 600, y + 96), (x + 600, y + 340)], AMBER, 6)
    d.rectangle((x + 260, y + 86, x + 350, y + 110), fill=AMBER, outline=OUTLINE, width=1)
    line(d, [(x + 305, y + 110), (x + 305, y + 174)], OUTLINE, 3)
    # hanging module panel
    polygon(d, [(x + 260, y + 174), (x + 350, y + 174), (x + 338, y + 250), (x + 272, y + 250)], STEEL_LIGHT, OUTLINE)
    for yy in (y + 190, y + 210, y + 230):
        d.rectangle((x + 278, yy, x + 332, yy + 4), fill=STEEL_MID)
    # main door
    rr(d, (x + 198, y + 242, x + 414, y + 358), 4, DARK2, STEEL_LIGHT, 2)
    rr(d, (x + 208, y + 252, x + 404, y + 348), 2, (30, 38, 47, 255), OUTLINE, 1)
    for yy in range(y + 260, y + 344, 13):
        d.rectangle((x + 216, yy, x + 396, yy + 5), fill=STEEL_MID)
    d.rectangle((x + 208, y + 252, x + 404, y + 260), fill=CYAN_DIM, outline=OUTLINE, width=1)
    # side stacks / pallets
    for i in range(3):
        d.rectangle((x + 76, y + 260 + i * 26, x + 150, y + 286 + i * 26), fill=STEEL_MID, outline=OUTLINE, width=1)
    d.rectangle((x + 500, y + 300, x + 570, y + 330), fill=(120, 92, 65, 255), outline=OUTLINE, width=1)
    # sign
    rr(d, (x + 250, y + 64, x + 390, y + 88), 4, DARK2, OUTLINE, 1)
    d.text((x + 262, y + 68), "PREFAB", font=F_SMALL, fill=AMBER)
    return


def prefab_top(d, ox, oy):
    x, y = ox, oy
    # 3x3 footprint 390x390
    rr(d, (x + 150, y + 30, x + 560, y + 390), 8, DARK, OUTLINE, 2)
    rr(d, (x + 162, y + 42, x + 548, y + 378), 5, WALL, OUTLINE, 1)
    rr(d, (x + 174, y + 54, x + 536, y + 366), 4, PANEL, OUTLINE, 1)
    # central assembly floor
    d.rectangle((x + 240, y + 100, x + 470, y + 320), fill=DARK2, outline=OUTLINE, width=1)
    # overhead crane rail
    d.rectangle((x + 186, y + 76, x + 524, y + 92), fill=STEEL_LIGHT, outline=OUTLINE, width=1)
    d.rectangle((x + 330, y + 76, x + 380, y + 112), fill=AMBER, outline=OUTLINE, width=1)
    line(d, [(x + 355, y + 112), (x + 355, y + 168)], OUTLINE, 3)
    # module on floor
    polygon(d, [(x + 300, y + 168), (x + 410, y + 168), (x + 398, y + 280), (x + 312, y + 280)], STEEL_LIGHT, OUTLINE)
    for yy in (y + 190, y + 216, y + 242):
        d.rectangle((x + 316, yy, x + 394, yy + 4), fill=STEEL_MID)
    # container input left
    for i in range(3):
        cx = x + 178 + (i % 2) * 40
        cy = y + 110 + i * 78
        rr(d, (cx, cy, cx + 54, cy + 50), 3, (STEEL_MID, AMBER, CYAN_DIM)[i], OUTLINE, 1)
        d.text((cx + 8, cy + 18), f"M{i+1}", font=F_TINY, fill=TEXT if i != 1 else DARK2)
    # output pallets right
    for i in range(3):
        cy = y + 110 + i * 78
        rr(d, (x + 482, cy, x + 540, cy + 50), 3, (120, 92, 65, 255), OUTLINE, 1)
        d.rectangle((x + 490, cy + 12, x + 532, cy + 18), fill=AMBER)
    # south door / hazard
    d.rectangle((x + 300, y + 352, x + 410, y + 378), fill=CYAN_DIM, outline=OUTLINE, width=1)
    hazard(d, x + 300, y + 336, x + 410, y + 348, 4)
    return


def label_panel(d, x, y, index, title, en, size, note):
    d.text((x, y + 14), f"{index:02d}", font=F_TINY, fill=AMBER)
    d.text((x, y + 38), title, font=F_LABEL, fill=TEXT)
    d.text((x, y + 66), en, font=F_SMALL, fill=MUTED)
    d.text((x, y + 92), f"占地 {size}", font=F_SMALL, fill=AMBER)
    d.text((x, y + 120), "设计变更", font=F_SMALL, fill=MUTED)
    lines = note.split("\n")
    for i, value in enumerate(lines):
        d.text((x, y + 144 + i * 22), value, font=F_SMALL, fill=TEXT)


def main():
    im = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(im)
    for x in range(0, W, 40):
        d.line((x, 0, x, H), fill=(27, 40, 47, 70), width=1)
    for y in range(0, H, 40):
        d.line((0, y, W, y), fill=(27, 40, 47, 70), width=1)
    d.text((MARGIN, 24), "CK ROBOTICS · FACTORY REDESIGN", font=F_TITLE, fill=TEXT)
    d.text((MARGIN + 3, 74), "骨架厂 / 两工位工厂 / 预制厂 · 正面与俯视重设计 · 保留原占地", font=F_SUB, fill=MUTED)
    d.text((W - MARGIN - 320, 82), "FRONT + TOP · REDESIGN 01", font=F_TINY, fill=AMBER)

    rows = [
        ("01", "骨架厂", "FRAME FAB", "2×2",
         "开放式桁架厂房：中央吊装工位、两侧焊接臂、外露\n机器人骨架。屋顶只保留边梁与检修天窗，远处就能\n看出“未封闭的骨架线”。",
         [("轮廓", "开放式桁架 / 中央吊装"),
          ("识别色", "钢灰 + 焊接橙"),
          ("关键件", "吊具、骨架夹具、双焊接臂、料架"),
          ("下一步", "确认后出四向与俯视正式贴图")],
         frame_front, frame_top),
        ("02", "两工位工厂", "TWO-STATION FACTORY", "4×3",
         "双锯齿屋顶 + 双独立卷帘门，中间共享工具塔和\n输送脊线；两条工位可独立开关，门灯分别为琥珀/青色。",
         [("轮廓", "双锯齿 / 双卷帘门"),
          ("识别色", "琥珀 + 青色工位"),
          ("关键件", "共享工具塔、输送脊、双工位门"),
          ("下一步", "确认后出四向与俯视正式贴图")],
         two_front, two_top),
        ("03", "预制厂", "PREFAB FACTORY", "3×3",
         "黄色龙门吊 + 屋顶集装箱堆场，中央吊装预制模块，\n左右分别为原料箱与成品托盘；强调“现场吊装”而不是\n封闭产线。",
         [("轮廓", "黄色龙门吊 / 集装箱堆场"),
          ("识别色", "工业黄 + 钢蓝"),
          ("关键件", "吊具、预制模块、原料箱、成品托盘"),
          ("下一步", "确认后出四向与俯视正式贴图")],
         prefab_front, prefab_top),
    ]
    for i, (index, name, en, size, note, notes, front_fn, top_fn) in enumerate(rows):
        y = TOP_Y + i * ROW_H
        if i % 2 == 0:
            d.rectangle((MARGIN, y - 10, W - MARGIN, y + ROW_H - 18), fill=(1, 1, 1, 20))
        # label
        label_panel(d, MARGIN + 10, y, int(index), name, en, size, note)
        # front panel
        rr(d, (FRONT_X, y, FRONT_X + FRONT_W, y + 400), 12, PANEL, LINE, 2)
        d.text((FRONT_X + 12, y + 10), "FRONT / 正面", font=F_SMALL, fill=AMBER)
        front_fn(d, FRONT_X, y + 12)
        # top panel
        rr(d, (TOP_X, y, TOP_X + TOP_W, y + 400), 12, PANEL, LINE, 2)
        d.text((TOP_X + 12, y + 10), "TOP / 俯视", font=F_SMALL, fill=AMBER)
        top_fn(d, TOP_X, y + 12)
        # note panel
        d.text((NOTE_X, y + 18), "DESIGN NOTES", font=F_SMALL, fill=AMBER)
        for j, (key, value) in enumerate(notes):
            d.text((NOTE_X, y + 54 + j * 54), key, font=F_TINY, fill=MUTED)
            d.text((NOTE_X, y + 72 + j * 54), value, font=F_SMALL, fill=TEXT)

    d.text((MARGIN, H - 46), "Concept redesign · footprints unchanged (2x2 / 4x3 / 3x3) · textures not yet regenerated.", font=F_TINY, fill=MUTED)
    im.convert("RGB").save(OUT, quality=96)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
