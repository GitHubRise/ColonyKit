"""Generate three animated attack-VFX previews and a self-contained HTML spec sheet."""

from __future__ import annotations

import base64
import io
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "robot-textures")
HTML = os.path.join(ROOT, "docs", "robot-attack-vfx.html")
os.makedirs(OUT, exist_ok=True)

W, H = 760, 440
BG = (12, 18, 24, 255)
PANEL = (18, 27, 34, 255)
LINE = (44, 66, 75, 255)
TEXT = (232, 242, 244, 255)
MUTED = (142, 170, 176, 255)
OUTLINE = (24, 31, 39, 255)
DARK = (44, 54, 64, 255)
DARK2 = (32, 40, 49, 255)
STEEL = (134, 148, 160, 255)
STEEL_LIGHT = (198, 209, 216, 255)
STEEL_MID = (163, 176, 187, 255)
AMBER = (232, 178, 83, 255)
AMBER_LIGHT = (250, 213, 126, 255)
ORANGE = (211, 113, 67, 255)
CYAN = (98, 219, 235, 255)
CYAN_DIM = (45, 122, 145, 255)
GREEN = (126, 212, 147, 255)

FONT_PATH = r"C:\Windows\Fonts\consola.ttf"
FONT_BOLD = r"C:\Windows\Fonts\consolab.ttf"


def font(size, bold=False):
    try:
        return ImageFont.truetype(FONT_BOLD if bold else FONT_PATH, size)
    except OSError:
        return ImageFont.load_default()


F_LABEL = font(16, True)
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


def base_frame(title: str, accent: tuple) -> Image.Image:
    im = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(im)
    # floor
    d.rectangle((0, 350, W, H), fill=(10, 16, 21, 255))
    d.line((0, 350, W, 350), fill=(31, 47, 54, 255), width=2)
    # grid
    for x in range(0, W, 40):
        d.line((x, 0, x, 350), fill=(24, 37, 44, 90), width=1)
    for y in range(0, 350, 40):
        d.line((0, y, W, y), fill=(24, 37, 44, 90), width=1)
    # header strip
    d.rectangle((0, 0, W, 54), fill=(11, 17, 22, 220))
    d.line((0, 54, W, 54), fill=(35, 53, 61, 255), width=1)
    d.text((22, 16), title, font=F_LABEL, fill=TEXT)
    d.rectangle((W - 58, 19, W - 22, 35), fill=accent)
    d.text((W - 50, 20), "LOCK", font=F_TINY, fill=(15, 23, 28, 255))
    return im


def draw_target(d, flash: float, hit: bool = False):
    cx, cy = 574, 220
    # stand / base
    d.rectangle((cx - 10, cy + 55, cx + 10, 344), fill=DARK2, outline=OUTLINE, width=2)
    rr(d, (cx - 54, 334, cx + 54, 350), 5, DARK, OUTLINE, 2)
    # outer plate
    ellipse(d, (cx - 58, cy - 58, cx + 58, cy + 58), DARK, OUTLINE, 3)
    ellipse(d, (cx - 48, cy - 48, cx + 48, cy + 48), STEEL_MID, OUTLINE, 2)
    ellipse(d, (cx - 38, cy - 38, cx + 38, cy + 38), PANEL, OUTLINE, 2)
    ellipse(d, (cx - 24, cy - 24, cx + 24, cy + 24), DARK2, OUTLINE, 2)
    ellipse(d, (cx - 10, cy - 10, cx + 10, cy + 10), AMBER, OUTLINE, 2)
    ellipse(d, (cx - 4, cy - 4, cx + 4, cy + 4), AMBER_LIGHT, None)
    for angle in range(0, 360, 45):
        a = math.radians(angle)
        x = cx + math.cos(a) * 51
        y = cy + math.sin(a) * 51
        d.ellipse((x - 3, y - 3, x + 3, y + 3), STEEL_LIGHT, OUTLINE, 1)
    # cross braces
    line(d, [(cx - 36, cy), (cx + 36, cy)], (18, 27, 35, 255), 3)
    line(d, [(cx, cy - 36), (cx, cy + 36)], (18, 27, 35, 255), 3)
    if hit or flash > 0:
        overlay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        od = ImageDraw.Draw(overlay)
        od.ellipse((cx - 66, cy - 66, cx + 66, cy + 66), fill=(*CYAN[:3], int(150 * flash)))
        od.ellipse((cx - 28, cy - 28, cx + 28, cy + 28), fill=(255, 255, 255, int(220 * flash)))
        overlay = overlay.filter(ImageFilter.GaussianBlur(6))
        # caller composites the overlay


def draw_robot_body(d, kind: str):
    # left body / shoulder
    rr(d, (-40, 70, 122, 350), 18, DARK, OUTLINE, 3)
    rr(d, (-26, 88, 104, 320), 12, DARK2, OUTLINE, 1)
    d.rectangle((6, 110, 78, 132), fill=STEEL_MID, outline=OUTLINE, width=1)
    d.rectangle((10, 118, 40, 124), fill=AMBER, outline=OUTLINE, width=1)
    d.rectangle((46, 118, 72, 124), fill=CYAN_DIM, outline=OUTLINE, width=1)
    # shoulder
    ellipse(d, (70, 156, 132, 218), STEEL, OUTLINE, 2)
    ellipse(d, (82, 168, 120, 206), DARK2, OUTLINE, 1)
    if kind == "welder":
        # arm
        line(d, [(110, 188), (188, 206), (260, 216)], OUTLINE, 22)
        line(d, [(110, 188), (188, 206), (260, 216)], STEEL_MID, 13)
        rr(d, (244, 200, 292, 232), 6, DARK, OUTLINE, 2)
        rr(d, (284, 208, 326, 226), 4, STEEL, OUTLINE, 2)
        # torch and cable
        line(d, [(300, 217), (408, 217)], OUTLINE, 14)
        line(d, [(300, 217), (408, 217)], STEEL_MID, 8)
        line(d, [(408, 217), (452, 217)], OUTLINE, 9)
        line(d, [(408, 217), (452, 217)], STEEL_LIGHT, 4)
        d.arc((86, 212, 210, 330), 250, 80, fill=OUTLINE, width=3)
        d.arc((86, 212, 210, 330), 250, 80, fill=AMBER, width=1)
    elif kind == "baton":
        line(d, [(110, 192), (188, 212), (252, 204)], OUTLINE, 22)
        line(d, [(110, 192), (188, 212), (252, 204)], STEEL_MID, 13)
        # baton
        line(d, [(252, 204), (418, 216)], OUTLINE, 18)
        line(d, [(252, 204), (418, 216)], DARK, 11)
        line(d, [(270, 206), (330, 210)], AMBER, 7)
        for i in range(4):
            line(d, [(282 + i * 12, 206 + i), (286 + i * 12, 216 + i)], OUTLINE, 2)
        ellipse(d, (412, 198, 448, 234), CYAN_DIM, OUTLINE, 2)
        ellipse(d, (419, 205, 441, 227), CYAN, None)
    else:
        # shoulder rifle, clearly a gun in side profile
        polygon(d, [(98, 172), (146, 154), (158, 184), (112, 204)], DARK, OUTLINE)
        # receiver
        rr(d, (132, 154, 330, 192), 6, STEEL, OUTLINE, 2)
        rr(d, (144, 161, 312, 183), 4, DARK, OUTLINE, 1)
        # top rail / sight
        rr(d, (170, 146, 276, 156), 2, DARK2, OUTLINE, 1)
        rr(d, (196, 136, 228, 150), 2, PANEL, OUTLINE, 1)
        ellipse(d, (230, 139, 240, 149), CYAN, OUTLINE, 1)
        # stock cheek / butt
        polygon(d, [(96, 176), (138, 160), (146, 186), (106, 204)], DARK2, OUTLINE)
        # pistol grip
        polygon(d, [(232, 188), (254, 186), (264, 230), (238, 232)], DARK, OUTLINE)
        for i in range(3):
            line(d, [(242 + i * 5, 198 + i * 8), (252 + i * 5, 197 + i * 8)], STEEL_MID, 1)
        # magazine / capacitor
        polygon(d, [(260, 188), (286, 188), (279, 224), (256, 224)], DARK2, OUTLINE)
        d.rectangle((262, 198, 276, 204), fill=CYAN, outline=OUTLINE, width=1)
        # trigger guard
        d.arc((228, 190, 264, 224), 0, 180, fill=OUTLINE, width=3)
        # amber energy cell
        rr(d, (292, 160, 316, 188), 3, AMBER, OUTLINE, 1)
        d.rectangle((296, 165, 312, 169), fill=AMBER_LIGHT)
        # barrel shroud + induction coils
        rr(d, (306, 162, 390, 186), 4, STEEL_MID, OUTLINE, 2)
        rr(d, (318, 168, 382, 180), 3, DARK, OUTLINE, 1)
        for i in range(4):
            x = 326 + i * 12
            ellipse(d, (x, 164, x + 8, 184), None, OUTLINE, 2)
            ellipse(d, (x + 2, 167, x + 6, 181), None, CYAN, 1)
        line(d, [(382, 174), (414, 174)], OUTLINE, 8)
        line(d, [(382, 174), (414, 174)], STEEL_LIGHT, 4)
        # support arm
        line(d, [(118, 202), (186, 234), (250, 228)], OUTLINE, 16)
        line(d, [(118, 202), (186, 234), (250, 228)], STEEL_MID, 9)


def arc_points(x1, y1, x2, y2, rng, segments=8, amp=10):
    points = [(x1, y1)]
    for i in range(1, segments):
        t = i / segments
        x = x1 + (x2 - x1) * t
        y = y1 + (y2 - y1) * t
        # perpendicular offset
        dx, dy = x2 - x1, y2 - y1
        length = max(1, math.hypot(dx, dy))
        nx, ny = -dy / length, dx / length
        offset = rng.uniform(-amp, amp)
        points.append((x + nx * offset, y + ny * offset))
    points.append((x2, y2))
    return points


def draw_arc(d, points, color, core=(255, 255, 255, 255), width=3):
    for w, c in ((width + 5, (*color[:3], 90)), (width + 2, color), (1, core)):
        line(d, [(int(x), int(y)) for x, y in points], c, w)


def draw_sparks(d, x, y, rng, count, color, spread=60, speed=50):
    for _ in range(count):
        angle = rng.uniform(-math.pi * 0.95, -math.pi * 0.05) if rng.random() < 0.7 else rng.uniform(0, math.pi)
        length = rng.uniform(spread * 0.25, spread)
        x2 = x + math.cos(angle) * length
        y2 = y + math.sin(angle) * length
        line(d, [(x, y), (x2, y2)], color, 2)
        d.ellipse((x2 - 2, y2 - 2, x2 + 2, y2 + 2), color)


def draw_ring(d, x, y, radius, color, width=2):
    d.ellipse((x - radius, y - radius, x + radius, y + radius), outline=color, width=width)


def add_glow(base, layer):
    blurred = layer.filter(ImageFilter.GaussianBlur(5))
    return Image.alpha_composite(base, blurred)


def frame_welder(frame: int) -> Image.Image:
    rng = random.Random(1000 + frame)
    im = base_frame("ARC WELDER  /  T3  /  MELEE", CYAN)
    d = ImageDraw.Draw(im)
    draw_robot_body(d, "welder")
    d.text((24, 31), "contact weld", font=F_TINY, fill=MUTED)
    hit = frame >= 4
    flash = max(0.0, 1.0 - max(0, frame - 4) / 8.0) if frame >= 4 else 0.0
    draw_target(d, flash, hit)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    if frame < 4:
        r = 5 + frame * 5
        gd.ellipse((448 - r, 217 - r, 448 + r, 217 + r), fill=(*CYAN[:3], 90 + frame * 30))
    else:
        age = frame - 4
        # contact arc from torch to target
        for _ in range(3):
            pts = arc_points(452, 217, 520, 222, rng, 7, 8 + age)
            draw_arc(d, pts, CYAN)
        draw_sparks(d, 505, 222, rng, max(0, 15 - age), ORANGE, 65, 60)
        draw_sparks(d, 505, 222, rng, max(0, 8 - age // 2), CYAN, 45, 30)
        draw_ring(d, 505, 222, 10 + age * 4, (*AMBER[:3], max(0, 150 - age * 14)), 2)
        gd.ellipse((480, 190, 540, 254), fill=(*ORANGE[:3], max(0, 110 - age * 10)))
        if age > 5:
            for _ in range(3):
                sx = 480 + rng.uniform(-20, 30)
                sy = 210 + rng.uniform(-10, 20)
                gd.ellipse((sx, sy, sx + 22, sy + 14), fill=(110, 120, 128, max(0, 40 - age * 4)))
    im = add_glow(im, glow)
    return im


def frame_baton(frame: int) -> Image.Image:
    rng = random.Random(2000 + frame)
    im = base_frame("SHOCK BATON  /  T4  /  MELEE", CYAN)
    d = ImageDraw.Draw(im)
    draw_robot_body(d, "baton")
    d.text((24, 31), "capacitor discharge", font=F_TINY, fill=MUTED)
    flash = max(0.0, 1.0 - max(0, frame - 4) / 9.0) if frame >= 4 else 0.0
    draw_target(d, flash, frame >= 4)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    if frame < 4:
        r = 4 + frame * 5
        gd.ellipse((430 - r, 216 - r, 430 + r, 216 + r), fill=(*CYAN[:3], 100 + frame * 25))
    else:
        age = frame - 4
        # several jagged arcs from the baton tip to the target
        for _ in range(3):
            pts = arc_points(436, 216, 520, 220, rng, 8, 16 + age)
            draw_arc(d, pts, CYAN)
        draw_sparks(d, 436, 216, rng, max(0, 9 - age // 2), CYAN, 36, 28)
        draw_ring(d, 520, 220, 12 + age * 5, (*CYAN[:3], max(0, 170 - age * 15)), 2)
        draw_ring(d, 520, 220, 24 + age * 6, (*AMBER[:3], max(0, 90 - age * 9)), 1)
        gd.ellipse((496, 196, 544, 244), fill=(*CYAN[:3], max(0, 45 - age * 4)))
    im = add_glow(im, glow)
    return im


def frame_emitter(frame: int) -> Image.Image:
    rng = random.Random(3000 + frame)
    im = base_frame("ARC EMITTER  /  T5  /  RANGED", CYAN)
    d = ImageDraw.Draw(im)
    draw_robot_body(d, "emitter")
    d.text((24, 31), "triple arc burst", font=F_TINY, fill=MUTED)
    target_flash = 0.0
    if frame >= 8:
        target_flash = max(0.0, 1.0 - (frame - 8) / 10.0)
    draw_target(d, target_flash, frame >= 8)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)

    if frame < 4:
        r = 5 + frame * 7
        gd.ellipse((420 - r, 174 - r, 420 + r, 174 + r), fill=(*CYAN[:3], 90 + frame * 35))
    else:
        # muzzle flash
        if frame <= 7:
            gd.ellipse((404, 150, 454, 198), fill=(*AMBER[:3], 110))
            gd.ellipse((412, 158, 442, 190), fill=(255, 255, 240, 120))
        # three bolts, staggered, aimed at the target plate
        for bolt in range(3):
            start = 5 + bolt * 2
            travel = frame - start
            if travel < 0 or travel > 6:
                continue
            t = travel / 6.0
            x = 420 + t * 96
            y = 174 + t * 46 + (bolt - 1) * 6
            # trail
            for k in range(4):
                tx = x - (k + 1) * 10
                ty = y - (k + 1) * 4
                if tx > 414:
                    line(d, [(tx, ty), (x, y)], (*CYAN[:3], 130 - k * 26), 4 - k)
            ellipse(d, (x - 7, y - 7, x + 7, y + 7), (235, 255, 255, 255), CYAN, 2)
            pts = arc_points(x - 10, y, x + 16, y + rng.uniform(-6, 6), rng, 5, 4)
            line(d, [(int(px), int(py)) for px, py in pts], CYAN, 2)
        # impact
        if frame >= 8:
            age = frame - 8
            for _ in range(4):
                pts = arc_points(520, 220, 550 + rng.uniform(-15, 20), 185 + rng.uniform(-20, 50), rng, 7, 20)
                draw_arc(d, pts, CYAN)
            draw_sparks(d, 520, 220, rng, max(0, 18 - age), ORANGE, 75, 60)
            draw_sparks(d, 520, 220, rng, max(0, 12 - age // 2), CYAN, 55, 40)
            draw_ring(d, 520, 220, 10 + age * 5, (*CYAN[:3], max(0, 200 - age * 16)), 3)
            draw_ring(d, 520, 220, 20 + age * 7, (*AMBER[:3], max(0, 100 - age * 10)), 1)
            gd.ellipse((492, 192, 548, 248), fill=(*CYAN[:3], max(0, 80 - age * 8)))
        # muzzle residual
        if frame < 8:
            gd.ellipse((410, 155, 435, 195), fill=(*CYAN[:3], 80))
    im = add_glow(im, glow)
    return im


def make_gif(frames, path):
    frames[0].save(
        path,
        save_all=True,
        append_images=frames[1:],
        duration=65,
        loop=0,
        disposal=2,
        optimize=True,
    )


def data_uri(path):
    with open(path, "rb") as handle:
        return "data:image/gif;base64," + base64.b64encode(handle.read()).decode("ascii")


def main():
    specs = [
        ("welder", "电弧焊枪", "ARC WELDER", "T3 · 近战", "7", "1.2 格", "1.2 s", "30 tick", "白热接触点 + 橙/青火花 + 短弧", "接触瞬间", "火花 0.25s / 余辉 0.45s"),
        ("baton", "电击棒", "SHOCK BATON", "T4 · 近战", "9", "1.2 格", "1.5 s", "60 tick", "蓝白电弧 + 冲击环 + 电容余辉", "命中瞬间", "电弧 0.30s / 冲击环 0.45s"),
        ("emitter", "电弧发射器", "ARC EMITTER", "T5 · 远程", "8×3", "4.9 格", "2.5 s", "30 tick", "枪口蓄能 + 三连电弧弹 + 径向爆裂", "弹着瞬间", "飞行 0.15s / 爆裂 0.55s"),
    ]
    gifs = {}
    for key, *_ in specs:
        frames = []
        count = 18 if key == "welder" else 19
        for frame in range(count):
            if key == "welder":
                frames.append(frame_welder(frame))
            elif key == "baton":
                frames.append(frame_baton(frame))
            else:
                frames.append(frame_emitter(frame))
        path = os.path.join(OUT, f"CK_AttackVFX_{key}.gif")
        make_gif(frames, path)
        gifs[key] = path
        print("wrote", path)

    panels = []
    for key, name, en, tier, damage, rng, cd, stun, vfx, timing, duration in specs:
        uri = data_uri(gifs[key])
        panels.append(f"""
        <section class="panel{' active' if key == 'welder' else ''}" data-panel="{key}">
          <div class="vfx-wrap"><img src="{uri}" alt="{en} attack VFX"></div>
          <div class="spec">
            <p class="eyebrow">{tier} · {en}</p>
            <h2>{name}</h2>
            <div class="chips">
              <span>伤害 {damage}</span><span>射程 {rng}</span><span>冷却 {cd}</span><span>眩晕 {stun}</span>
            </div>
            <table>
              <tr><th>主视觉</th><td>{vfx}</td></tr>
              <tr><th>触发时机</th><td>{timing}</td></tr>
              <tr><th>持续时间</th><td>{duration}</td></tr>
              <tr><th>声音</th><td>蓄能/电弧/命中三层，短促、低于枪声，不盖过战斗音效</td></tr>
              <tr><th>实现</th><td>EffecterDef + MoteDef + SoundDef；远程再加 ProjectileDef</td></tr>
            </table>
            <div class="layers">
              <span class="swatch" style="background:#f5ffff"></span><span>白热核心</span>
              <span class="swatch" style="background:#62dbeb"></span><span>电弧青</span>
              <span class="swatch" style="background:#e8b253"></span><span>琥珀余辉</span>
              <span class="swatch" style="background:#d37143"></span><span>火花橙</span>
            </div>
          </div>
        </section>
        """)

    html = f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>机器人自卫武器 · 攻击特效锁定</title>
<style>
:root {{ color-scheme: dark; --bg:#0c1217; --panel:#141e25; --line:#30444d; --text:#e9f3f4; --muted:#9db1b5; --cyan:#62dbeb; --amber:#e8b253; --red:#d37143; }}
* {{ box-sizing:border-box; }}
body {{ margin:0; background:radial-gradient(900px 500px at 8% -10%, rgba(50,135,145,.22), transparent 65%), radial-gradient(700px 420px at 96% 4%, rgba(190,135,70,.15), transparent 70%), var(--bg); color:var(--text); font-family:"Segoe UI","Microsoft YaHei","PingFang SC",system-ui,sans-serif; }}
.wrap {{ max-width:1360px; margin:0 auto; padding:28px 22px 54px; }}
.eyebrow {{ margin:0 0 7px; color:var(--cyan); font-size:11px; letter-spacing:.16em; text-transform:uppercase; }}
h1 {{ margin:0; font-size:clamp(28px,3.4vw,46px); letter-spacing:-.03em; }}
.sub {{ margin:10px 0 0; color:var(--muted); line-height:1.7; max-width:920px; }}
.badges {{ display:flex; gap:7px; flex-wrap:wrap; margin-top:15px; }}
.badges span {{ border:1px solid var(--line); border-radius:999px; padding:5px 9px; color:var(--muted); font-size:11px; }}
.tabs {{ display:flex; gap:6px; flex-wrap:wrap; padding:8px; margin-top:18px; background:var(--panel); border:1px solid var(--line); border-radius:16px; }}
.tab {{ border:1px solid transparent; background:transparent; color:var(--muted); border-radius:10px; padding:9px 12px; cursor:pointer; font:inherit; }}
.tab:hover {{ background:#1b2a32; color:var(--text); }}
.tab.active {{ border-color:var(--cyan); background:#1b2a32; color:var(--text); }}
.panel {{ display:none; grid-template-columns:1.35fr .9fr; gap:18px; margin-top:14px; background:var(--panel); border:1px solid var(--line); border-radius:20px; padding:16px; }}
.panel.active {{ display:grid; animation:fade .18s ease; }}
@keyframes fade {{ from {{ opacity:0; transform:translateY(4px); }} to {{ opacity:1; transform:none; }} }}
.vfx-wrap {{ border:1px solid var(--line); border-radius:14px; overflow:hidden; background:#0d151a; }}
.vfx-wrap img {{ display:block; width:100%; height:auto; }}
.spec {{ padding:8px 6px; }}
h2 {{ margin:0 0 10px; font-size:clamp(24px,2.8vw,34px); }}
.chips {{ display:flex; gap:6px; flex-wrap:wrap; margin-bottom:12px; }}
.chips span {{ border:1px solid var(--line); border-radius:999px; padding:4px 8px; color:var(--muted); font-size:11px; }}
table {{ border-collapse:collapse; width:100%; font-size:12px; }}
th, td {{ text-align:left; padding:7px 0; border-bottom:1px solid rgba(80,110,120,.18); vertical-align:top; }}
th {{ color:var(--muted); font-weight:500; width:34%; }}
.layers {{ display:flex; align-items:center; gap:6px; flex-wrap:wrap; margin-top:14px; color:var(--muted); font-size:11px; }}
.swatch {{ width:16px; height:16px; border-radius:4px; border:1px solid rgba(255,255,255,.16); }}
.compare {{ margin-top:18px; border:1px solid var(--line); border-radius:16px; overflow:auto; }}
.compare table {{ min-width:1080px; table-layout:fixed; }}
.compare th, .compare td {{ padding:9px 10px; }}
.compare th:nth-child(1) {{ width:16%; }}
.compare th:nth-child(2) {{ width:10%; }}
.compare th:nth-child(3), .compare th:nth-child(5), .compare th:nth-child(6) {{ width:7%; }}
.compare th:nth-child(4) {{ width:8%; }}
.compare th:nth-child(7) {{ width:24%; }}
.compare th:nth-child(8) {{ width:21%; }}
.compare td:nth-child(3), .compare td:nth-child(4), .compare td:nth-child(5), .compare td:nth-child(6) {{ white-space:nowrap; }}
.compare td:nth-child(7), .compare td:nth-child(8) {{ white-space:normal; line-height:1.55; }}
.note {{ margin-top:16px; border:1px solid rgba(232,178,83,.35); background:rgba(232,178,83,.06); color:var(--muted); border-radius:12px; padding:12px 14px; font-size:12px; line-height:1.65; }}
@media (max-width:900px) {{ .panel.active {{ grid-template-columns:1fr; }} }}
</style>
</head>
<body>
<div class="wrap">
  <p class="eyebrow">ColonyKit · Self-Defense VFX Lock</p>
  <h1>机器人自卫武器 · 攻击特效锁定</h1>
  <p class="sub">三把武器的攻击特效定稿：统一“白热核心 + 青色电弧 + 琥珀余辉 + 橙色火花”，区别在于接触、放电和弹道。所有动画按 65 ms/帧循环，供后续 Effecter / Mote / Sound / Projectile Def 实现。</p>
  <div class="badges"><span>3 个特效</span><span>近战 ×2</span><span>远程 ×1</span><span>65 ms/帧</span><span>Effecter + Mote + Sound</span></div>

  <nav class="tabs">
    <button class="tab active" data-tab="welder" type="button">电弧焊枪</button>
    <button class="tab" data-tab="baton" type="button">电击棒</button>
    <button class="tab" data-tab="emitter" type="button">电弧发射器</button>
  </nav>

  {''.join(panels)}

  <div class="compare">
    <table>
      <thead><tr><th>武器</th><th>阶段</th><th>伤害</th><th>射程</th><th>冷却</th><th>眩晕</th><th>主视觉</th><th>声音</th></tr></thead>
      <tbody>
        <tr><td>电弧焊枪</td><td>T3 · 近战</td><td>7</td><td>1.2 格</td><td>1.2 s</td><td>30 tick</td><td>白热接触、短弧、火花</td><td>焊枪蜂鸣 + 金属接触</td></tr>
        <tr><td>电击棒</td><td>T4 · 近战</td><td>9</td><td>1.2 格</td><td>1.5 s</td><td>60 tick</td><td>电容放电、冲击环</td><td>电容充能 + 电击</td></tr>
        <tr><td>电弧发射器</td><td>T5 · 远程</td><td>8×3</td><td>4.9 格</td><td>2.5 s</td><td>30 tick</td><td>三连电弧弹、径向爆裂</td><td>蓄能 + 三连放电 + 爆裂</td></tr>
      </tbody>
    </table>
  </div>
  <div class="note"><strong>锁定规则：</strong>三把武器都只做“自卫尺度”的特效，不出现全屏闪白/长时间屏幕震动；电弧不穿透墙体；颜色不抢殖民者枪械的橙色弹道。下一步可直接按此表建 EffecterDef / MoteDef / SoundDef / ProjectileDef。</div>
</div>
<script>
const tabs = Array.from(document.querySelectorAll('.tab'));
const panels = Array.from(document.querySelectorAll('.panel'));
tabs.forEach((tab) => tab.addEventListener('click', () => {{
  tabs.forEach((t) => t.classList.toggle('active', t === tab));
  panels.forEach((p) => p.classList.toggle('active', p.dataset.panel === tab.dataset.tab));
}}));
</script>
</body>
</html>
"""
    with open(HTML, "w", encoding="utf-8") as handle:
        handle.write(html)
    print("wrote", HTML)


if __name__ == "__main__":
    main()
