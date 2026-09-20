"""Environment textures, world props, painted scenes and icons for the ATTILA MVP.

Everything is drawn from code with a fixed seed, so reruns are reproducible. PLACEHOLDER / NEEDS HISTORICAL REVIEW.
"""
import colorsys
import math
import random

from PIL import Image, ImageDraw, ImageFont

from characters import Canvas, outlined, shade, mix, GOLD, GOLD_LIGHT, GOLD_DARK, WOOD, OUTLINE, WHITE

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
INK = (70, 42, 26)
BURGUNDY = (109, 29, 23)


def value_noise(width, height, cell, seed):
    rng = random.Random(seed)
    gw, gh = max(1, width // cell), max(1, height // cell)
    grid = [[rng.random() for _ in range(gw)] for _ in range(gh)]
    out = []
    for y in range(height):
        row = []
        for x in range(width):
            gx, gy = x / cell, y / cell
            x0, y0 = int(gx) % gw, int(gy) % gh
            x1, y1 = (x0 + 1) % gw, (y0 + 1) % gh
            tx, ty = gx - int(gx), gy - int(gy)
            tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
            a = grid[y0][x0] * (1 - tx) + grid[y0][x1] * tx
            b = grid[y1][x0] * (1 - tx) + grid[y1][x1] * tx
            row.append(a * (1 - ty) + b * ty)
        out.append(row)
    return out


def dither(value, x, y, levels):
    v = value * (levels - 1) + (BAYER[y % 4][x % 4] / 16 - .5)
    return max(0, min(levels - 1, int(round(v))))


def fractal(width, height, seed, cells=(16, 8, 4)):
    layers = [value_noise(width, height, cell, seed + i) for i, cell in enumerate(cells)]
    weights = [.55, .3, .15][:len(cells)]
    return [[sum(layer[y][x] * w for layer, w in zip(layers, weights)) for x in range(width)] for y in range(height)]


# ---------------------------------------------------------------- tiling textures

def ground_texture(seed=11):
    size = 64
    palette = [(92, 116, 54), (110, 134, 60), (128, 148, 68), (146, 160, 80), (162, 170, 94)]
    noise = fractal(size, size, seed)
    image = Image.new("RGB", (size, size))
    px = image.load()
    for y in range(size):
        for x in range(size):
            px[x, y] = palette[dither(noise[y][x], x, y, len(palette))]
    rng = random.Random(seed)
    for _ in range(90):
        x, y = rng.randrange(size), rng.randrange(size)
        tone = rng.choice([palette[0], palette[4], palette[3]])
        px[x, y] = tone
        px[x, (y + 1) % size] = shade(tone, .86)
    for _ in range(7):
        x, y = rng.randrange(size), rng.randrange(size)
        px[x, y] = rng.choice([(236, 214, 120), (236, 232, 214), (214, 120, 110)])
    return image


def path_texture(seed=21):
    size = 32
    palette = [(160, 126, 84), (176, 142, 96), (192, 158, 108), (206, 174, 122)]
    noise = fractal(size, size, seed, cells=(8, 4))
    image = Image.new("RGB", (size, size))
    px = image.load()
    for y in range(size):
        for x in range(size):
            px[x, y] = palette[dither(noise[y][x], x, y, len(palette))]
    rng = random.Random(seed)
    for _ in range(14):
        x, y = rng.randrange(size), rng.randrange(size)
        px[x, y] = (128, 104, 78)
        px[(x + 1) % size, y] = (222, 198, 150)
    return image


def ornament_band(draw, x0, y0, width, height, base, figure, step):
    """Repeating ram's-horn style scroll (қошқар мүйіз) simplified to pixels."""
    draw.rectangle([x0, y0, x0 + width - 1, y0 + height - 1], fill=base)
    draw.line([x0, y0, x0 + width - 1, y0], fill=GOLD)
    draw.line([x0, y0 + height - 1, x0 + width - 1, y0 + height - 1], fill=GOLD)
    cy = y0 + height // 2
    for x in range(x0 + step // 2, x0 + width, step):
        draw.polygon([(x, cy - 3), (x + 3, cy), (x, cy + 3), (x - 3, cy)], fill=figure)
        draw.point([(x, cy)], fill=base)
        draw.line([x + 4, cy - 2, x + 5, cy - 3], fill=figure)
        draw.line([x - 4, cy + 2, x - 5, cy + 3], fill=figure)
        draw.point([(x + 6, cy - 2), (x - 6, cy + 2)], fill=figure)


def yurt_wall_texture():
    width, height = 128, 48
    image = Image.new("RGB", (width, height))
    px = image.load()
    noise = fractal(width, height, 5, cells=(16, 4))
    felt = [(200, 184, 150), (214, 198, 164), (226, 212, 180), (236, 224, 194)]
    for y in range(height):
        for x in range(width):
            px[x, y] = felt[dither(noise[y][x] * .8 + .2 * (1 - y / height), x, y, len(felt))]
    draw = ImageDraw.Draw(image)
    for x in range(0, width, 16):
        draw.line([x, 12, x, height - 7], fill=(192, 174, 140))
    ornament_band(draw, 0, 2, width, 10, BURGUNDY, GOLD, 16)
    draw.rectangle([0, 0, width - 1, 1], fill=(92, 60, 40))
    draw.rectangle([0, height - 6, width - 1, height - 1], fill=(126, 44, 34))
    for x in range(2, width, 4):
        draw.point([(x, height - 4)], fill=GOLD)
    for x in range(0, width, 32):
        draw.line([x + 8, 12, x + 24, height - 7], fill=(188, 170, 136))
        draw.line([x + 24, 12, x + 8, height - 7], fill=(188, 170, 136))
    return image


def yurt_roof_texture():
    size = 96
    image = Image.new("RGB", (size, size), (228, 214, 182))
    px = image.load()
    noise = fractal(size, size, 9, cells=(12, 4))
    felt = [(206, 190, 156), (220, 204, 170), (232, 218, 188), (240, 230, 204)]
    c = size / 2
    for y in range(size):
        for x in range(size):
            px[x, y] = felt[dither(noise[y][x], x, y, len(felt))]
    draw = ImageDraw.Draw(image)
    for i in range(12):
        a = i * math.pi / 6 + math.pi / 12
        draw.line([c + math.cos(a) * 9, c + math.sin(a) * 9, c + math.cos(a) * 48, c + math.sin(a) * 48], fill=(150, 108, 72))
    draw.ellipse([c - 46, c - 46, c + 46, c + 46], outline=BURGUNDY, width=4)
    for i in range(24):
        a = i * math.pi / 12
        draw.point([(c + math.cos(a) * 44, c + math.sin(a) * 44)], fill=GOLD)
    draw.ellipse([c - 9, c - 9, c + 9, c + 9], fill=(96, 62, 38))
    draw.ellipse([c - 6, c - 6, c + 6, c + 6], fill=(58, 44, 40))
    draw.line([c - 6, c, c + 6, c], fill=(130, 88, 54), width=2)
    draw.line([c, c - 6, c, c + 6], fill=(130, 88, 54), width=2)
    return image


def door_texture():
    image = Image.new("RGB", (20, 32), (70, 44, 28))
    draw = ImageDraw.Draw(image)
    draw.rectangle([2, 2, 17, 31], fill=(178, 76, 40))
    draw.rectangle([10, 2, 10, 31], fill=(110, 46, 26))
    for x0 in (3, 11):
        draw.rectangle([x0, 4, x0 + 5, 29], outline=GOLD)
        draw.polygon([(x0 + 3, 9), (x0 + 5, 12), (x0 + 3, 15), (x0 + 1, 12)], fill=GOLD_LIGHT)
        draw.polygon([(x0 + 3, 19), (x0 + 5, 22), (x0 + 3, 25), (x0 + 1, 22)], fill=GOLD)
    draw.rectangle([0, 0, 19, 1], fill=(96, 62, 38))
    return image


# ---------------------------------------------------------------- world props (sprites)

def banner(cloth, figure):
    c = Canvas(20, 64)
    c.rect(3, 2, 4, 63, WOOD)
    c.rect(2, 0, 5, 2, GOLD)
    c.rect(5, 5, 17, 38, cloth)
    c.poly([(5, 38), (17, 38), (17, 46), (11, 41), (5, 46)], cloth)
    c.rect(14, 5, 17, 40, shade(cloth, .75))
    c.rect(5, 5, 17, 6, GOLD)
    for y in (12, 24):
        cx = 11
        c.poly([(cx, y), (cx + 4, y + 4), (cx, y + 8), (cx - 4, y + 4)], figure)
        c.poly([(cx, y + 2), (cx + 2, y + 4), (cx, y + 6), (cx - 2, y + 4)], cloth)
        c.px(cx, y + 4, figure)
    c.rect(6, 34, 16, 34, figure)
    c.rect(5, 40, 5, 44, GOLD)
    return outlined(c.image)


def horse(coat, mane, saddle, frame):
    c = Canvas(64, 48)
    dark = shade(coat, .72)
    head_drop = 2 if frame else 0
    for x, far in ((20, True), (44, True)):
        c.rect(x + 2, 30, x + 4, 44, shade(coat, .6))
        c.rect(x + 2, 44, x + 4, 45, (40, 30, 26))
    c.ellipse(14, 17, 50, 34, coat)
    c.rect(18, 28, 46, 32, dark)
    for x in (16, 40):
        c.rect(x, 30, x + 3, 44, coat)
        c.rect(x, 42, x + 3, 45, (40, 30, 26))
    c.poly([(42, 22), (50, 8 + head_drop), (55, 8 + head_drop), (50, 24)], coat)
    c.poly([(50, 7 + head_drop), (58, 10 + head_drop), (60, 15 + head_drop), (57, 17 + head_drop), (51, 14 + head_drop)], coat)
    c.px(59, 14 + head_drop, (40, 30, 26))
    c.px(54, 10 + head_drop, (30, 22, 20))
    c.rect(51, 5 + head_drop, 52, 7 + head_drop, coat)
    c.poly([(42, 20), (48, 7 + head_drop), (50, 7 + head_drop), (45, 22)], mane)
    c.poly([(14, 20), (9, 24), (7, 36), (10, 38), (13, 28)], mane)
    c.rect(24, 15, 38, 20, saddle)
    c.rect(24, 20, 38, 27, shade(saddle, .8))
    c.rect(24, 26, 38, 27, GOLD)
    for x in range(26, 38, 4):
        c.px(x, 23, GOLD_LIGHT)
    c.rect(28, 13, 34, 15, (90, 56, 34))
    return outlined(c.image)


def camel(frame):
    c = Canvas(64, 56)
    coat, dark = (196, 162, 108), (160, 128, 82)
    bob = 1 if frame else 0
    for x in (16, 40):
        c.rect(x, 34, x + 3, 50, dark)
        c.rect(x + 8, 34, x + 11, 50, coat)
        c.rect(x, 50, x + 3, 51, (70, 54, 38))
        c.rect(x + 8, 50, x + 11, 51, (70, 54, 38))
    c.ellipse(12, 20, 52, 38, coat)
    c.poly([(20, 22), (26, 8), (32, 22)], coat)
    c.poly([(34, 22), (40, 10), (46, 22)], coat)
    c.poly([(46, 24), (52, 12 + bob), (56, 12 + bob), (52, 26)], coat)
    c.ellipse(50, 6 + bob, 62, 16 + bob, coat)
    c.px(59, 10 + bob, (40, 30, 26))
    c.rect(52, 4 + bob, 54, 6 + bob, coat)
    c.rect(24, 16, 38, 22, (150, 30, 34))
    c.rect(24, 20, 38, 21, GOLD)
    c.poly([(12, 24), (8, 30), (10, 34), (13, 28)], dark)
    return outlined(c.image)


def sheep():
    c = Canvas(40, 30)
    wool, dark = (236, 230, 216), (196, 190, 176)
    c.ellipse(6, 6, 32, 22, wool)
    for x in range(8, 31, 4):
        c.px(x, 9 + (x % 3), dark)
        c.px(x + 1, 16, dark)
    c.ellipse(26, 4, 36, 14, (74, 62, 56))
    c.px(33, 8, (30, 24, 22))
    c.poly([(27, 4), (24, 1), (29, 2)], (60, 50, 44))
    for x in (10, 16, 24, 28):
        c.rect(x, 20, x + 2, 27, (74, 62, 56))
    return outlined(c.image)


def cart():
    c = Canvas(64, 48)
    wood, dark = (146, 102, 58), (108, 72, 40)
    c.rect(8, 18, 54, 30, wood)
    c.rect(8, 18, 54, 20, (176, 130, 78))
    for x in range(10, 54, 6):
        c.rect(x, 21, x + 1, 29, dark)
    c.poly([(10, 18), (52, 18), (46, 8), (16, 8)], (214, 198, 164))
    c.rect(16, 8, 46, 10, BURGUNDY)
    for x in range(18, 45, 6):
        c.px(x, 9, GOLD)
    for cx2 in (18, 44):
        c.ellipse(cx2 - 9, 28, cx2 + 9, 46, dark)
        c.ellipse(cx2 - 6, 31, cx2 + 6, 43, (170, 128, 78))
        c.ellipse(cx2 - 2, 35, cx2 + 2, 39, dark)
        for a in range(0, 360, 45):
            x2, y2 = cx2 + math.cos(math.radians(a)) * 7, 37 + math.sin(math.radians(a)) * 7
            c.line(cx2, 37, x2, y2, dark)
    c.rect(52, 20, 63, 22, wood)
    return outlined(c.image)


def well():
    c = Canvas(48, 52)
    stone, dark = (152, 146, 136), (112, 108, 100)
    c.ellipse(6, 26, 42, 46, stone)
    c.ellipse(12, 30, 36, 42, (58, 52, 50))
    for x in range(8, 41, 6):
        c.rect(x, 34, x + 4, 40, dark)
        c.rect(x + 1, 41, x + 5, 45, stone)
    c.rect(10, 6, 13, 30, WOOD)
    c.rect(35, 6, 38, 30, WOOD)
    c.poly([(6, 8), (42, 8), (38, 0), (10, 0)], (150, 104, 60))
    c.rect(6, 6, 42, 9, (120, 80, 44))
    c.rect(22, 9, 26, 18, (90, 74, 60))
    c.rect(20, 18, 28, 24, (110, 82, 54))
    return outlined(c.image)


def firewood():
    c = Canvas(40, 26)
    for i, (x, y) in enumerate([(4, 16), (12, 14), (20, 15), (28, 13)]):
        wood = (146, 102, 58) if i % 2 == 0 else (120, 82, 46)
        c.rect(x, y, x + 12, y + 5, wood)
        c.ellipse(x + 10, y, x + 14, y + 5, (176, 138, 92))
        c.px(x + 12, y + 2, (108, 74, 44))
    c.rect(2, 20, 36, 24, (132, 92, 52))
    c.rect(16, 10, 22, 24, (110, 70, 40))
    return outlined(c.image)


def torsyk():
    c = Canvas(28, 34)
    body = (150, 106, 62)
    c.ellipse(2, 8, 25, 32, body)
    c.ellipse(6, 12, 21, 26, shade(body, 1.12))
    c.rect(10, 2, 17, 10, (110, 76, 44))
    c.rect(9, 0, 18, 3, (86, 58, 34))
    for x, y in [(13, 18), (10, 22), (16, 22), (13, 26)]:
        c.px(x, y, GOLD)
    return outlined(c.image)


def kerege():
    c = Canvas(48, 34)
    for i in range(6):
        x = 3 + i * 8
        c.line(x, 30, x + 10, 4, (170, 130, 80))
        c.line(x + 10, 30, x, 4, (150, 112, 68))
    c.rect(1, 2, 46, 4, (120, 84, 48))
    c.rect(1, 29, 46, 31, (120, 84, 48))
    for i in range(6):
        c.px(7 + i * 8, 17, (90, 62, 38))
    return outlined(c.image)


def uyk():
    c = Canvas(44, 30)
    for i in range(5):
        y = 4 + i * 5
        c.line(2, y + 2, 40, y, (176, 136, 84) if i % 2 == 0 else (150, 112, 68))
    c.rect(18, 2, 24, 28, (150, 30, 34))
    c.rect(18, 12, 24, 16, GOLD)
    return outlined(c.image)


def shanyrak():
    c = Canvas(40, 30)
    c.ellipse(2, 4, 37, 26, (120, 84, 50))
    c.ellipse(6, 8, 33, 22, (58, 44, 40))
    for a in range(0, 360, 45):
        x = 19 + math.cos(math.radians(a)) * 14
        y = 15 + math.sin(math.radians(a)) * 9
        c.line(19, 15, x, y, (166, 122, 72))
    c.ellipse(15, 11, 23, 19, (120, 84, 50))
    return outlined(c.image)


def felt_roll():
    c = Canvas(44, 26)
    c.rect(4, 6, 38, 22, (226, 212, 180))
    c.ellipse(1, 5, 11, 23, (240, 228, 200))
    c.ellipse(32, 5, 42, 23, (210, 196, 164))
    c.ellipse(35, 8, 39, 20, (186, 172, 142))
    c.rect(14, 5, 18, 23, (150, 30, 34))
    c.rect(26, 5, 30, 23, (150, 30, 34))
    return outlined(c.image)


def post():
    c = Canvas(24, 44)
    c.rect(9, 6, 14, 42, (128, 88, 50))
    c.rect(9, 6, 14, 10, (96, 64, 36))
    c.ellipse(6, 0, 17, 9, (150, 106, 62))
    c.rect(3, 14, 20, 16, (106, 72, 42))
    c.px(5, 18, (70, 50, 34))
    c.px(18, 18, (70, 50, 34))
    return outlined(c.image)


def yurt_frame_texture():
    width, height = 128, 48
    image = Image.new("RGB", (width, height), (196, 178, 144))
    draw = ImageDraw.Draw(image)
    for x in range(-16, width + 16, 10):
        draw.line([x, height - 2, x + 26, 4], fill=(150, 112, 68), width=2)
        draw.line([x + 26, height - 2, x, 4], fill=(128, 94, 56), width=2)
    draw.rectangle([0, 0, width - 1, 3], fill=(108, 74, 44))
    draw.rectangle([0, height - 4, width - 1, height - 1], fill=(108, 74, 44))
    return image


def yurt_poles_texture():
    size = 96
    image = Image.new("RGB", (size, size), (200, 184, 150))
    draw = ImageDraw.Draw(image)
    c = size / 2
    for i in range(16):
        a = i * math.pi / 8
        draw.line([c + math.cos(a) * 9, c + math.sin(a) * 9, c + math.cos(a) * 48, c + math.sin(a) * 48], fill=(156, 116, 70), width=2)
    draw.ellipse([c - 10, c - 10, c + 10, c + 10], fill=(96, 62, 38))
    draw.ellipse([c - 7, c - 7, c + 7, c + 7], fill=(58, 44, 40))
    return image


def campfire_frames():
    frames = []
    for i in range(4):
        c = Canvas(32, 36)
        for x, y in [(6, 31), (10, 33), (16, 34), (22, 33), (26, 31)]:
            c.ellipse(x - 2, y - 2, x + 2, y + 1, (120, 116, 110))
        c.poly([(6, 30), (26, 26), (27, 28), (7, 32)], (98, 62, 36))
        c.poly([(6, 26), (26, 30), (25, 32), (5, 28)], (120, 78, 44))
        rng = random.Random(40 + i)
        sway = [0, 1, 0, -1][i]
        c.poly([(8, 28), (12 + sway, 12 + rng.randint(0, 3)), (16 + sway, 4 + rng.randint(0, 3)), (20, 12 + rng.randint(0, 3)),
                (24, 28)], (232, 104, 40))
        c.poly([(11, 28), (14 + sway, 16 + rng.randint(0, 2)), (17 + sway, 10 + rng.randint(0, 2)), (21, 28)], (250, 170, 60))
        c.poly([(13, 28), (16 + sway, 19 + rng.randint(0, 2)), (19, 28)], (255, 236, 150))
        for _ in range(3):
            c.px(rng.randint(9, 23), rng.randint(0, 10), (255, 200, 90))
        frames.append(outlined(c.image))
    return frames


def glow(size=96, color=(255, 170, 80), strength=.55):
    image = Image.new("RGBA", (size, size // 2), (0, 0, 0, 0))
    px = image.load()
    for y in range(size // 2):
        for x in range(size):
            dx, dy = (x + .5) / size * 2 - 1, (y + .5) / (size / 2) * 2 - 1
            d = math.sqrt(dx * dx + dy * dy)
            a = max(0.0, 1 - d)
            a = round(a * a * 5) / 5
            if a > 0:
                px[x, y] = color + (int(255 * a * strength),)
    return image


def shadow_blob():
    image = Image.new("RGBA", (24, 8), (0, 0, 0, 0))
    ImageDraw.Draw(image).ellipse([0, 0, 23, 7], fill=(24, 20, 16, 96))
    ImageDraw.Draw(image).ellipse([4, 1, 19, 6], fill=(24, 20, 16, 128))
    return image


def quest_marker():
    c = Canvas(16, 22)
    c.ellipse(1, 1, 14, 16, (250, 240, 214))
    c.poly([(5, 14), (10, 14), (7, 20)], (250, 240, 214))
    c.rect(6, 4, 9, 10, (178, 34, 34))
    c.rect(6, 12, 9, 13, (178, 34, 34))
    c.rect(11, 3, 13, 14, (222, 206, 176))
    return outlined(c.image)


def kazan():
    c = Canvas(28, 32)
    c.line(3, 30, 13, 2, WOOD)
    c.line(24, 30, 14, 2, WOOD)
    c.line(13, 2, 14, 30, shade(WOOD, .8))
    c.rect(13, 2, 14, 14, (60, 50, 44))
    c.ellipse(6, 14, 21, 26, (54, 50, 48))
    c.rect(6, 15, 21, 17, (84, 80, 76))
    c.rect(7, 16, 20, 16, (120, 90, 60))
    return outlined(c.image)


# ---------------------------------------------------------------- painted scenes (480x270, shown at 4x)

def _sky(image, top, bottom, horizon, seed):
    px = image.load()
    w, _ = image.size
    for y in range(horizon):
        t = y / horizon
        color = mix(top, bottom, t)
        for x in range(w):
            tone = dither(.5 + (BAYER[y % 4][x % 4] / 16 - .5) * .12, x, y, 3) - 1
            px[x, y] = shade(color, 1 + tone * .04)
    rng = random.Random(seed)
    for _ in range(160):
        x, y = rng.randrange(w), rng.randrange(int(horizon * .8))
        bright = rng.random()
        px[x, y] = (255, 248, 220) if bright > .8 else mix(bottom, (230, 230, 255), .6 + bright * .3)
        if bright > .95 and 1 < x < w - 2 and y > 1:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                px[x + dx, y + dy] = mix(bottom, (230, 230, 255), .5)


def _ridge(draw, width, base, amplitude, seed, color, step=12):
    rng = random.Random(seed)
    points = [(0, base)]
    for x in range(0, width + step, step):
        points.append((x, base - rng.randint(0, amplitude)))
    points += [(width, 400), (0, 400)]
    draw.polygon(points, fill=color)


def campfire_scene():
    w, h = 480, 270
    image = Image.new("RGB", (w, h))
    _sky(image, (14, 16, 38), (54, 42, 78), 140, 3)
    draw = ImageDraw.Draw(image)
    draw.ellipse([372, 26, 396, 50], fill=(244, 232, 190))
    draw.ellipse([379, 22, 403, 46], fill=mix((14, 16, 38), (54, 42, 78), .2))
    _ridge(draw, w, 120, 26, 5, (40, 38, 72), 18)
    _ridge(draw, w, 136, 14, 6, (30, 28, 52), 10)
    draw.rectangle([0, 138, w, h], fill=(28, 24, 34))
    px = image.load()
    fx, fy = 240, 160
    for y in range(120, h):
        for x in range(w):
            dx, dy = (x - fx) / 210, (y - fy) / 80
            d = math.sqrt(dx * dx + dy * dy)
            a = max(0.0, 1 - d)
            level = dither(a * a, x, y, 5) / 4
            if level > 0 and y >= 136:
                px[x, y] = mix(px[x, y], (184, 98, 46), level * .85)
    for x0, scale, lit in ((36, 1.0, 1), (356, .92, -1)):
        base = 170
        r = int(56 * scale)
        wall = int(24 * scale)
        body, roof = (44, 36, 50), (58, 48, 62)
        draw.rectangle([x0, base - wall, x0 + 2 * r, base], fill=body)
        dome = [(x0 - 3, base - wall)]
        for i in range(0, 21):
            t = i / 20
            dome.append((x0 - 3 + t * (2 * r + 6), base - wall - math.sin(t * math.pi) ** .7 * 34 * scale))
        dome.append((x0 + 2 * r + 3, base - wall))
        draw.polygon(dome, fill=roof)
        draw.rectangle([x0 + r - 6, base - wall - int(36 * scale), x0 + r + 6, base - wall - int(32 * scale)], fill=(34, 28, 38))
        for i in range(1, 6):
            t = i / 6
            draw.line([x0 + r, base - wall - int(33 * scale), x0 - 3 + t * (2 * r + 6), base - wall], fill=(50, 42, 56))
        draw.rectangle([x0, base - wall, x0 + 2 * r, base - wall + 3], fill=(96, 36, 34))
        for x in range(x0 + 2, x0 + 2 * r, 6):
            draw.point([(x, base - wall + 1)], fill=(170, 110, 60))
        rim_x = x0 + 2 * r if lit > 0 else x0
        draw.rectangle([rim_x - 2 if lit > 0 else rim_x, base - wall + 4, rim_x if lit > 0 else rim_x + 2, base], fill=(150, 88, 52))
        door_x = x0 + r - 9
        draw.rectangle([door_x, base - int(20 * scale), door_x + 18, base], fill=(210, 120, 50))
        draw.rectangle([door_x + 3, base - int(17 * scale), door_x + 15, base], fill=(250, 186, 90))
        for y in range(base, min(base + 30, 269)):
            spread = (y - base) * .6
            level = 1 - (y - base) / 30
            for x in range(int(door_x - spread), int(door_x + 18 + spread)):
                if 0 <= x < w and BAYER[y % 4][x % 4] / 16 < level * .45:
                    px[x, y] = mix(px[x, y], (190, 110, 50), .5)
    fx0 = 240
    for i in range(16):
        y = 150 - i * 5
        x = fx0 + int(math.sin(i * .7) * (2 + i * .6))
        tone = mix((90, 80, 90), (54, 42, 78), i / 16)
        draw.ellipse([x - 3 - i // 3, y - 3, x + 3 + i // 3, y + 2], fill=tone)
    rng = random.Random(8)
    for i, (x, y, hat) in enumerate([(176, 170, "kalpak"), (206, 186, "tymak"), (284, 186, "takiya"), (310, 168, "tymak"), (150, 150, "kimeshek")]):
        lit_side = 1 if x < fx else -1
        body = (34, 26, 32)
        draw.polygon([(x - 12, y + 22), (x + 12, y + 22), (x + 8, y + 2), (x - 8, y + 2)], fill=body)
        draw.ellipse([x - 7, y - 12, x + 7, y + 3], fill=body)
        if hat == "kalpak":
            draw.polygon([(x - 8, y - 8), (x + 8, y - 8), (x + 5, y - 22), (x - 5, y - 22)], fill=(60, 52, 56))
        elif hat == "tymak":
            draw.ellipse([x - 9, y - 16, x + 9, y - 4], fill=(56, 44, 44))
        elif hat == "takiya":
            draw.ellipse([x - 7, y - 15, x + 7, y - 7], fill=(70, 40, 40))
        elif hat == "kimeshek":
            draw.ellipse([x - 9, y - 16, x + 9, y + 6], fill=(92, 84, 88))
        rim, warm = (236, 140, 66), (96, 52, 40)
        draw.polygon([(x, y + 2), (x + 8 * lit_side, y + 2), (x + 12 * lit_side, y + 22), (x, y + 22)], fill=warm)
        draw.ellipse([x - 7, y - 12, x + 7, y + 3], fill=body)
        draw.chord([x - 7, y - 12, x + 7, y + 3], -60 if lit_side > 0 else 120, 60 if lit_side > 0 else 240, fill=warm)
        draw.line([x + 7 * lit_side, y - 7, x + 7 * lit_side, y], fill=rim)
        draw.line([x + 9 * lit_side, y + 4, x + 12 * lit_side, y + 21], fill=rim)
    for x, y in [(222, 168), (256, 170), (232, 176), (248, 158)]:
        draw.ellipse([x - 5, y - 3, x + 5, y + 3], fill=(96, 90, 88))
    draw.polygon([(222, 164), (258, 160), (260, 164), (224, 168)], fill=(110, 66, 36))
    draw.polygon([(222, 160), (258, 166), (256, 170), (220, 164)], fill=(130, 80, 44))
    return image.convert("RGBA")


def map_scene():
    w, h = 480, 270
    noise = fractal(w, h, 17, cells=(40, 12, 4))
    image = Image.new("RGB", (w, h))
    px = image.load()
    paper = [(186, 150, 102), (206, 174, 122), (222, 192, 140), (234, 208, 158), (242, 222, 176)]
    for y in range(h):
        for x in range(w):
            ex = min(x, w - 1 - x) / 40
            ey = min(y, h - 1 - y) / 30
            edge = min(1.0, min(ex, ey))
            v = noise[y][x] * .55 + .45 * edge
            px[x, y] = paper[dither(v, x, y, len(paper))]
    draw = ImageDraw.Draw(image)
    for x in (160, 320):
        draw.line([x, 8, x, h - 8], fill=(214, 182, 132))
    draw.line([8, 135, w - 8, 135], fill=(214, 182, 132))
    water, water_dark, river = (132, 160, 162), (104, 132, 138), (110, 140, 146)
    draw.polygon([(0, 70), (22, 76), (34, 110), (28, 150), (40, 190), (30, 230), (0, 250)], fill=water)
    draw.line([(22, 76), (34, 110), (28, 150), (40, 190), (30, 230)], fill=water_dark, width=2)
    draw.ellipse([88, 132, 134, 168], fill=water)
    draw.ellipse([96, 126, 124, 146], fill=water)
    draw.line([(262, 104), (290, 100), (320, 104), (350, 110), (376, 120)], fill=water, width=8)
    draw.ellipse([256, 96, 286, 116], fill=water)

    def river_line(points, width=2):
        draw.line(points, fill=river, width=width)

    river_line([(126, 158), (150, 172), (176, 186), (200, 196), (222, 202), (250, 214), (282, 228), (318, 244), (350, 262)])
    river_line([(372, 122), (392, 140), (414, 150), (440, 158), (470, 160)])
    river_line([(256, 186), (282, 190), (306, 196), (330, 202), (352, 210)])
    river_line([(420, 6), (412, 24), (398, 44), (392, 62)])
    river_line([(210, 40), (206, 70), (196, 96), (180, 120)])

    def mountains(points, snow=True, size=9):
        for x, y in points:
            draw.polygon([(x - size, y), (x, y - size - 3), (x + size, y)], fill=(150, 116, 80))
            draw.polygon([(x, y - size - 3), (x + size, y), (x + 2, y)], fill=(118, 88, 60))
            if snow:
                draw.polygon([(x - 3, y - size + 1), (x, y - size - 3), (x + 3, y - size + 1)], fill=(244, 236, 220))

    mountains([(306, 226), (324, 218), (344, 210), (366, 200), (388, 192), (410, 186), (432, 180), (454, 176), (470, 186)], size=11)
    mountains([(196, 180), (210, 174), (224, 170)], snow=False, size=7)
    mountains([(224, 92), (238, 88)], snow=False, size=6)
    mountains([(296, 174), (310, 172)], snow=False, size=6)
    rng = random.Random(4)
    for _ in range(46):
        x, y = rng.randint(150, 400), rng.randint(24, 84)
        draw.point([(x, y), (x + 1, y - 1), (x + 2, y)], fill=(150, 130, 74))
    for _ in range(60):
        x, y = rng.randint(100, 220), rng.randint(206, 250)
        draw.point([(x, y), (x + 1, y)], fill=(196, 150, 96))
    for _ in range(26):
        x, y = rng.randint(240, 300), rng.randint(152, 176)
        draw.point([(x, y), (x + 1, y)], fill=(196, 150, 96))
    def waves(x0, y0, x1, y1, count):
        for i in range(count):
            wx = rng.randint(x0, x1)
            wy = rng.randint(y0, y1)
            draw.line([(wx, wy), (wx + 3, wy - 1), (wx + 6, wy)], fill=(96, 124, 132))

    waves(4, 80, 26, 240, 22)
    waves(94, 136, 128, 164, 8)
    waves(268, 98, 370, 118, 10)

    def town(x, y, big=False):
        width = 11 if big else 8
        height = 8 if big else 6
        draw.rectangle([x - width, y - height, x + width, y + 2], fill=(206, 176, 130))
        draw.rectangle([x - width, y - height, x + width, y + 2], outline=INK)
        for tx in range(x - width, x + width, 4):
            draw.rectangle([tx, y - height - 3, tx + 2, y - height], fill=(206, 176, 130))
            draw.line([tx, y - height - 3, tx + 2, y - height - 3], fill=INK)
        if big:
            draw.polygon([(x - 4, y - height), (x, y - height - 9), (x + 4, y - height)], fill=(150, 116, 80))
            draw.point([(x, y - height - 10)], fill=GOLD)
        draw.rectangle([x - 2, y - 2, x + 1, y + 2], fill=(110, 76, 48))

    def camp(x, y, count=3):
        for i in range(count):
            dx, dy = i * 11 - count * 5, (i % 2) * 5
            draw.polygon([(x + dx - 5, y + dy), (x + dx + 5, y + dy), (x + dx + 3, y + dy - 4), (x + dx, y + dy - 7), (x + dx - 3, y + dy - 4)], fill=(232, 216, 180))
            draw.line([(x + dx - 5, y + dy), (x + dx + 5, y + dy)], fill=INK)
            draw.point([(x + dx, y + dy - 7)], fill=INK)

    def caravan(x, y):
        for i in range(3):
            cx2 = x + i * 11
            draw.line([(cx2, y), (cx2 + 5, y)], fill=(122, 86, 52))
            draw.line([(cx2 + 1, y - 3), (cx2 + 2, y - 1)], fill=(122, 86, 52))
            draw.line([(cx2 + 5, y), (cx2 + 6, y - 3)], fill=(122, 86, 52))
            draw.point([(cx2, y + 2), (cx2 + 5, y + 2)], fill=(122, 86, 52))

    def grove(x, y, count=4):
        for i in range(count):
            gx, gy = x + rng.randint(-8, 8), y + rng.randint(-5, 5)
            draw.line([(gx, gy), (gx, gy - 4)], fill=(96, 78, 48))
            draw.ellipse([gx - 3, gy - 8, gx + 3, gy - 3], fill=(118, 132, 76))

    town(182, 188)
    town(215, 206, True)
    town(243, 216)
    town(302, 226)
    camp(268, 66)
    camp(196, 116, 2)
    camp(352, 150, 2)
    camp(148, 196, 2)
    caravan(250, 196)
    caravan(170, 172)
    grove(140, 170)
    grove(206, 200, 3)
    grove(300, 208, 3)
    grove(392, 132, 3)
    cx, cy = 432, 50
    cx, cy = 432, 50
    draw.ellipse([cx - 22, cy - 22, cx + 22, cy + 22], outline=INK)
    draw.ellipse([cx - 17, cy - 17, cx + 17, cy + 17], outline=(150, 110, 70))
    for angle, length, color in ((0, 22, BURGUNDY), (90, 16, INK), (180, 16, INK), (270, 16, INK), (45, 10, INK), (135, 10, INK), (225, 10, INK), (315, 10, INK)):
        a = math.radians(angle - 90)
        tip = (cx + math.cos(a) * length, cy + math.sin(a) * length)
        side = math.radians(angle)
        draw.polygon([tip, (cx + math.cos(side) * 3, cy + math.sin(side) * 3), (cx - math.cos(side) * 3, cy - math.sin(side) * 3)], fill=color)
    draw.rectangle([4, 4, w - 5, h - 5], outline=INK)
    draw.rectangle([7, 7, w - 8, h - 8], outline=(150, 110, 70))
    for x, y in ((4, 4), (w - 13, 4), (4, h - 13), (w - 13, h - 13)):
        draw.rectangle([x, y, x + 8, y + 8], fill=BURGUNDY)
        draw.rectangle([x + 3, y + 3, x + 5, y + 5], fill=GOLD)
    draw.rectangle([16, h - 40, 112, h - 16], fill=(214, 186, 138), outline=INK)
    draw.line([(24, h - 24), (104, h - 24)], fill=INK)
    for x in range(24, 105, 20):
        draw.line([(x, h - 28), (x, h - 20)], fill=INK)
    return image.convert("RGBA")


def map_labels(image, font_path):
    font = ImageFont.truetype(font_path, 8)
    draw = ImageDraw.Draw(image)
    labels = [("Арал", 94, 136), ("Балқаш", 290, 84), ("Сырдария", 120, 208), ("Сарыарқа", 236, 46), ("Жетісу", 392, 148),
              ("Алатау", 380, 232), ("Ұлытау", 200, 96), ("Қаратау", 146, 152), ("Шу", 258, 186), ("Каспий", 30, 52),
              ("Сығанақ", 160, 176), ("Түркістан", 196, 218), ("Отырар", 232, 228), ("Тараз", 292, 238), ("Ертіс", 352, 18),
              ("Іле", 424, 158)]
    for text, x, y in labels:
        draw.text((x + 1, y + 1), text, font=font, fill=(236, 214, 170))
        draw.text((x, y), text, font=font, fill=INK)
    return image


def asyk_scene():
    w, h = 480, 270
    noise = fractal(w, h, 29, cells=(24, 8, 4))
    image = Image.new("RGB", (w, h))
    px = image.load()
    dirt = [(150, 118, 78), (166, 134, 90), (182, 150, 102), (196, 164, 114)]
    grass = [(98, 122, 56), (116, 138, 62), (134, 152, 72)]
    for y in range(h):
        for x in range(w):
            dx, dy = (x - 240) / 230, (y - 140) / 150
            d = math.sqrt(dx * dx + dy * dy) + (noise[y][x] - .5) * .25
            if d > .92:
                px[x, y] = grass[dither(noise[y][x], x, y, len(grass))]
            else:
                px[x, y] = dirt[dither(noise[y][x], x, y, len(dirt))]
    draw = ImageDraw.Draw(image)
    draw.ellipse([120, 50, 360, 230], outline=(110, 80, 52), width=2)
    draw.ellipse([122, 52, 358, 228], outline=(214, 186, 140))
    draw.line([150, 140, 330, 140], fill=(120, 90, 60))
    rng = random.Random(12)
    for _ in range(40):
        x, y = rng.randrange(w), rng.randrange(h)
        draw.point([(x, y), (x + 1, y)], fill=(120, 96, 70))
    return image.convert("RGBA")


def asyk_piece(dyed):
    c = Canvas(16, 14)
    body = (200, 60, 50) if dyed else (236, 224, 196)
    c.ellipse(1, 2, 14, 12, body)
    c.rect(4, 5, 11, 8, shade(body, .82))
    c.px(7, 6, shade(body, .6))
    c.rect(2, 3, 5, 4, shade(body, 1.08))
    return outlined(c.image)


# ---------------------------------------------------------------- icons (32x32)

def icon(name):
    c = Canvas(32, 32)
    red, blue = (150, 30, 34), (40, 64, 124)
    if name == "shapan":
        c.poly([(9, 5), (23, 5), (27, 29), (5, 29)], blue)
        c.poly([(3, 7), (9, 5), (9, 18), (4, 18)], blue)
        c.poly([(29, 7), (23, 5), (23, 18), (28, 18)], shade(blue, .75))
        c.poly([(14, 5), (18, 5), (20, 29), (12, 29)], (226, 212, 180))
        c.line(14, 5, 12, 29, GOLD)
        c.line(18, 5, 20, 29, GOLD)
        c.rect(5, 26, 27, 27, GOLD)
        c.rect(3, 16, 9, 18, GOLD)
        c.rect(23, 16, 28, 18, GOLD)
    elif name == "borik":
        c.ellipse(8, 4, 24, 22, blue)
        c.rect(15, 2, 16, 4, GOLD_LIGHT)
        c.line(16, 5, 16, 18, GOLD)
        c.ellipse(4, 16, 28, 28, (150, 104, 66))
        for x in range(6, 27, 2):
            c.px(x, 22 + (x % 3 == 0), (110, 76, 48))
    elif name == "etik":
        c.poly([(8, 3), (17, 3), (17, 22), (26, 24), (27, 29), (6, 29), (8, 20)], (92, 58, 38))
        c.rect(8, 3, 17, 6, (130, 84, 50))
        c.rect(6, 28, 27, 29, (50, 34, 26))
        c.px(12, 12, GOLD)
        c.px(12, 16, GOLD)
        c.px(11, 14, GOLD)
        c.px(13, 14, GOLD)
    elif name == "kimeshek":
        c.ellipse(5, 2, 27, 30, WHITE)
        c.ellipse(10, 8, 22, 22, (212, 158, 116))
        c.rect(12, 22, 20, 30, red)
        c.rect(12, 22, 20, 23, GOLD)
        c.px(16, 26, GOLD_LIGHT)
        c.ellipse(7, 1, 25, 10, shade(WHITE, .9))
    elif name == "kamzol":
        c.poly([(8, 4), (24, 4), (26, 28), (6, 28)], (36, 52, 104))
        c.poly([(13, 4), (19, 4), (17, 28), (15, 28)], (158, 36, 54))
        c.line(13, 4, 15, 28, GOLD)
        c.line(19, 4, 17, 28, GOLD)
        c.rect(6, 26, 26, 27, GOLD)
        for y in (10, 16, 22):
            c.px(12, y, GOLD_LIGHT)
            c.px(20, y, GOLD_LIGHT)
    elif name == "belbeu":
        c.rect(2, 12, 29, 19, (96, 40, 26))
        c.rect(2, 12, 29, 12, GOLD)
        c.rect(2, 19, 29, 19, GOLD)
        c.ellipse(11, 9, 20, 22, GOLD)
        c.ellipse(13, 11, 18, 20, GOLD_DARK)
        for x in (5, 8, 23, 26):
            c.px(x, 15, GOLD_LIGHT)
    elif name == "asyk":
        for ox, oy, flip in ((4, 6, False), (16, 14, True)):
            c.ellipse(ox, oy, ox + 12, oy + 10, (236, 222, 190))
            c.rect(ox + 3, oy + 3, ox + 9, oy + 6, (200, 180, 146))
            c.px(ox + (8 if flip else 4), oy + 4, (150, 120, 90))
        c.ellipse(20, 4, 27, 10, (200, 60, 50))
    elif name == "blindfold":
        c.ellipse(6, 5, 26, 28, (212, 158, 116))
        c.rect(4, 12, 28, 17, (150, 30, 34))
        c.rect(26, 15, 30, 24, (150, 30, 34))
        c.ellipse(8, 3, 24, 11, (40, 28, 24))
        c.rect(13, 22, 19, 22, (150, 86, 72))
    elif name == "banner":
        c.rect(7, 2, 8, 30, WOOD)
        c.rect(6, 1, 9, 2, GOLD)
        c.rect(9, 4, 26, 18, (150, 30, 34))
        c.poly([(9, 18), (26, 18), (26, 23), (17, 19), (9, 23)], (150, 30, 34))
        c.poly([(17, 6), (21, 11), (17, 16), (13, 11)], GOLD)
        c.poly([(17, 9), (18, 11), (17, 13), (16, 11)], (150, 30, 34))
    elif name == "swords":
        for flip in (False, True):
            pts = [(4, 26), (24, 4)] if not flip else [(28, 26), (8, 4)]
            c.line(pts[0][0], pts[0][1], pts[1][0], pts[1][1], (200, 204, 214))
            c.line(pts[0][0] + 1, pts[0][1], pts[1][0] + 1, pts[1][1], (150, 154, 166))
        c.rect(5, 21, 10, 22, GOLD)
        c.rect(22, 21, 27, 22, GOLD)
        c.rect(3, 26, 6, 28, (96, 60, 36))
        c.rect(26, 26, 29, 28, (96, 60, 36))
    elif name == "dombra":
        c.ellipse(4, 16, 18, 30, (170, 100, 50))
        c.ellipse(7, 19, 15, 27, (130, 74, 38))
        c.line(12, 18, 28, 2, (120, 78, 44))
        c.line(13, 18, 29, 2, (150, 98, 56))
        c.rect(26, 1, 29, 4, (96, 60, 36))
    elif name == "sauyt":
        c.poly([(8, 4), (24, 4), (26, 26), (16, 30), (6, 26)], (150, 152, 162))
        for y in range(6, 26, 4):
            c.rect(7, y, 25, y + 1, (110, 112, 124))
            for x in range(9, 25, 4):
                c.px(x, y + 2, GOLD)
        c.ellipse(13, 12, 19, 18, GOLD)
        c.px(16, 15, GOLD_LIGHT)
        c.rect(4, 4, 8, 12, (120, 74, 44))
        c.rect(24, 4, 28, 12, (100, 60, 36))
    elif name == "dulyga":
        c.poly([(6, 22), (26, 22), (17, 3), (15, 3)], GOLD)
        c.poly([(16, 22), (26, 22), (17, 3)], GOLD_DARK)
        c.rect(4, 20, 28, 25, (236, 222, 194))
        c.rect(4, 24, 8, 30, (120, 84, 50))
        c.rect(24, 24, 28, 30, (100, 68, 42))
        c.rect(15, 1, 17, 3, (170, 34, 40))
        c.px(14, 0, (170, 34, 40))
    elif name == "naiza":
        c.rect(15, 8, 16, 31, WOOD)
        c.poly([(13, 8), (18, 8), (16, 0)], (200, 204, 214))
        c.px(16, 3, (240, 244, 250))
        c.rect(13, 9, 18, 12, (170, 34, 40))
        c.px(12, 13, (170, 34, 40))
        c.px(19, 13, (170, 34, 40))
    elif name == "koilek":
        c.poly([(11, 5), (21, 5), (27, 29), (5, 29)], (170, 40, 60))
        c.poly([(19, 5), (21, 5), (27, 29), (21, 29)], shade((170, 40, 60), .78))
        c.rect(5, 26, 27, 27, GOLD)
        c.rect(12, 5, 20, 7, (236, 222, 194))
        c.poly([(3, 7), (11, 5), (11, 14), (4, 15)], (170, 40, 60))
        c.poly([(29, 7), (21, 5), (21, 14), (28, 15)], shade((170, 40, 60), .8))
        for x in range(9, 24, 5):
            c.px(x, 20, GOLD_LIGHT)
    elif name == "takiya":
        c.ellipse(6, 10, 26, 26, (160, 40, 40))
        c.rect(6, 20, 26, 24, GOLD)
        for x in range(9, 25, 4):
            c.px(x, 16, GOLD_LIGHT)
        c.rect(15, 4, 17, 9, (236, 232, 222))
        c.px(14, 3, (40, 30, 30))
        c.px(17, 2, (236, 232, 222))
    elif name == "tymak":
        c.ellipse(8, 6, 24, 20, (90, 66, 44))
        c.ellipse(3, 14, 29, 26, (170, 140, 104))
        c.rect(3, 22, 8, 31, (170, 140, 104))
        c.rect(24, 22, 29, 31, (140, 112, 82))
        for x in range(5, 28, 3):
            c.px(x, 20, (120, 96, 70))
    elif name == "yurt":
        c.poly([(3, 20), (29, 20), (22, 7), (10, 7)], (226, 212, 180))
        c.rect(5, 20, 27, 28, (214, 198, 164))
        c.rect(5, 19, 27, 21, BURGUNDY)
        for x in range(6, 27, 4):
            c.px(x, 20, GOLD)
        c.rect(13, 22, 19, 29, (178, 76, 40))
        c.rect(14, 5, 18, 7, (96, 62, 38))
        c.line(10, 7, 16, 20, (196, 180, 148))
        c.line(22, 7, 16, 20, (196, 180, 148))
    elif name == "zhuz":
        for cx2, cy2, col in ((16, 9, (150, 30, 34)), (9, 22, (40, 64, 124)), (23, 22, (34, 104, 98))):
            c.ellipse(cx2 - 7, cy2 - 7, cx2 + 7, cy2 + 7, col)
            c.ellipse(cx2 - 4, cy2 - 4, cx2 + 4, cy2 + 4, shade(col, 1.3))
            c.px(cx2, cy2, GOLD)
    elif name == "horse":
        c.ellipse(6, 12, 24, 22, (140, 84, 48))
        c.poly([(20, 14), (26, 5), (29, 6), (25, 16)], (140, 84, 48))
        c.poly([(24, 4), (26, 1), (27, 5)], (140, 84, 48))
        c.px(28, 8, (40, 30, 26))
        c.rect(8, 20, 10, 29, (110, 64, 36))
        c.rect(19, 20, 21, 29, (110, 64, 36))
        c.poly([(6, 13), (2, 17), (3, 25), (6, 20)], (50, 34, 26))
        c.rect(11, 10, 17, 13, (150, 30, 34))
    elif name == "lock":
        c.rect(8, 14, 23, 28, GOLD)
        c.rect(8, 14, 23, 15, GOLD_LIGHT)
        c.ellipse(10, 4, 21, 18, GOLD_DARK)
        c.ellipse(13, 7, 18, 16, (0, 0, 0))
        c.rect(15, 19, 16, 24, (80, 50, 26))
        image = outlined(c.image)
        px = image.load()
        for y in range(32):
            for x in range(32):
                if px[x, y][:3] == (0, 0, 0):
                    px[x, y] = (0, 0, 0, 0)
        return image
    return outlined(c.image)


ICONS = ["shapan", "borik", "etik", "kimeshek", "kamzol", "belbeu", "asyk", "blindfold", "banner", "swords", "dombra", "lock",
         "sauyt", "dulyga", "naiza", "koilek", "takiya", "tymak", "yurt", "zhuz", "horse"]


# ---------------------------------------------------------------- menu card composition

def recolor_red_to_blue(image):
    px = image.load()
    w, h = image.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if (hh > .93 or hh < .035) and ss > .45 and vv < .8:
                nr, ng, nb = colorsys.hsv_to_rgb(.615, min(1, ss * .9), min(1, vv * 1.08))
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
    return image


def key_black(image, threshold=26):
    """Removes the flat black backdrop connected to the image border."""
    image = image.convert("RGBA")
    w, h = image.size
    px = image.load()
    stack = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    seen = bytearray(w * h)
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y * w + x]:
            continue
        seen[y * w + x] = 1
        r, g, b, a = px[x, y]
        if max(r, g, b) > threshold:
            continue
        px[x, y] = (0, 0, 0, 0)
        stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return image


def compose_card(template, character, blank_character):
    card = template.copy()
    draw = ImageDraw.Draw(card)
    background = (63, 2, 7, 253)
    if blank_character:
        draw.rectangle([248, 96, 776, 1000], fill=background)
    lower = (78, 4, 9, 253)
    draw.rectangle([150, 1016, 874, 1395], fill=lower)
    draw.rectangle([150, 1016, 874, 1019], fill=(123, 10, 16, 254))
    if character is not None:
        alpha = character.getchannel("A").point(lambda a: 255 if a > 96 else 0)
        character = character.crop(alpha.getbbox())
        scale = min(880 / character.height, 520 / character.width)
        character = character.resize((int(character.width * scale), int(character.height * scale)), Image.LANCZOS)
        x = 512 - character.width // 2
        y = 998 - character.height
        card.alpha_composite(character, (x, y))
    return card
