from PIL import Image, ImageDraw
import os

ROOT = r"E:\RimWorld-WorkSpace\sources\ColonyKit\Textures\UI\Commands"
os.makedirs(ROOT, exist_ok=True)


def make(name, draw):
    size = 64
    img = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    draw(d, 2)
    img.resize((size, size), Image.LANCZOS).save(os.path.join(ROOT, "CK_%s.png" % name))


def recall(d, s):
    def p(v):
        return int(v * s)

    white = (238, 243, 248, 255)
    dark = (30, 36, 46, 255)
    blue = (96, 186, 228, 255)
    d.polygon([(p(32), p(8)), (p(56), p(28)), (p(48), p(28)), (p(48), p(52)),
               (p(16), p(52)), (p(16), p(28)), (p(8), p(28))], fill=white, outline=dark)
    d.rectangle((p(28), p(36), p(36), p(52)), fill=blue, outline=dark)
    d.polygon([(p(20), p(20)), (p(32), p(20)), (p(26), p(14))], fill=dark)


def goto(d, s):
    def p(v):
        return int(v * s)

    white = (238, 243, 248, 255)
    dark = (30, 36, 46, 255)
    red = (230, 96, 88, 255)
    d.ellipse((p(18), p(8), p(46), p(36)), fill=red, outline=dark, width=p(2))
    d.ellipse((p(27), p(17), p(37), p(27)), fill=white, outline=dark)
    d.polygon([(p(24), p(30)), (p(40), p(30)), (p(32), p(56))], fill=red, outline=dark)


def rename(d, s):
    def p(v):
        return int(v * s)

    gold = (244, 210, 92, 255)
    dark = (30, 36, 46, 255)
    white = (238, 243, 248, 255)
    d.polygon([(p(12), p(50)), (p(16), p(38)), (p(44), p(10)), (p(54), p(20)),
               (p(26), p(48))], fill=gold, outline=dark)
    d.rectangle((p(10), p(52), p(54), p(58)), fill=white, outline=dark)
    d.line((p(44), p(10), p(54), p(20)), fill=dark, width=p(2))


def paint(d, s):
    def p(v):
        return int(v * s)

    dark = (30, 36, 46, 255)
    white = (238, 243, 248, 255)
    d.ellipse((p(8), p(10), p(56), p(54)), fill=white, outline=dark, width=p(2))
    for color, xy in (
        ((220, 80, 80, 255), (20, 20)),
        ((80, 180, 220, 255), (32, 16)),
        ((110, 200, 120, 255), (42, 24)),
        ((240, 200, 80, 255), (26, 36)),
        ((180, 120, 230, 255), (42, 40)),
    ):
        d.ellipse((p(xy[0]), p(xy[1]), p(xy[0] + 10), p(xy[1] + 10)), fill=color, outline=dark)


make("Recall", recall)
make("Goto", goto)
make("Rename", rename)
make("Paint", paint)
print("done")
