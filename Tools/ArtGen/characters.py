"""Parametric pixel characters: eight-direction world sprites (48x80) and dialogue busts (64x64).

Every look is data (colours, hat, beard, garment, weapon), so a new NPC is one dictionary entry.
All art is PLACEHOLDER / NEEDS HISTORICAL REVIEW: shapes suggest silhouettes, not documented costume.
"""
from PIL import Image, ImageDraw

OUTLINE = (38, 22, 18)
GOLD = (217, 154, 85)
GOLD_LIGHT = (246, 204, 122)
GOLD_DARK = (150, 92, 42)
SKIN = (212, 158, 116)
SKIN_DARK = (176, 122, 88)
FUR = (236, 222, 194)
FUR_DARK = (190, 168, 136)
STEEL = (154, 156, 166)
STEEL_DARK = (104, 106, 120)
WOOD = (120, 78, 44)
WHITE = (242, 236, 222)
EYE = (36, 22, 20)


def shade(color, factor):
    return tuple(max(0, min(255, int(v * factor))) for v in color[:3])


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


LOOKS = {
    "kerey": dict(coat=(150, 30, 34), inner=(232, 214, 176), trim=GOLD, belt=(96, 40, 26), boots=(92, 58, 38),
                  hair=(46, 30, 24), beard="short", beard_color=(46, 30, 24), hat="khan", hat_main=(160, 32, 36),
                  collar="fur", fur=FUR, cuffs="fur"),
    "abylai": dict(coat=(40, 64, 124), inner=(226, 212, 180), trim=GOLD, belt=(70, 40, 30), boots=(86, 56, 36),
                   hair=(40, 30, 26), beard="full", beard_color=(58, 46, 40), hat="borik", hat_main=(40, 64, 124),
                   collar="fur", fur=(150, 104, 66), cuffs="trim"),
    "kabanbay": dict(coat=(128, 32, 32), inner=(226, 214, 190), trim=GOLD, belt=(90, 52, 30), boots=(88, 58, 36),
                     hair=(40, 28, 22), beard="short", beard_color=(40, 28, 22), hat="helmet", hat_main=GOLD,
                     plume=(170, 34, 40), collar="mantle", fur=(214, 196, 164), armor=True, cuffs="leather",
                     weapon="spear"),
    "bi": dict(coat=(78, 98, 62), inner=(212, 200, 170), trim=(200, 170, 110), belt=(70, 50, 36), boots=(70, 48, 34),
               hair=(214, 210, 200), beard="long", beard_color=(226, 222, 212), hat="kalpak", hat_main=WHITE,
               collar="plain", cuffs="trim", weapon="staff", old=True),
    "woman": dict(coat=(158, 36, 54), inner=(158, 36, 54), trim=GOLD, belt=(120, 70, 30), boots=(90, 50, 34),
                  hair=(40, 28, 24), beard="none", hat="kimeshek", hat_main=WHITE, collar="none", cuffs="trim",
                  dress=True, vest=(36, 52, 104)),
    "child": dict(coat=(232, 220, 196), inner=(232, 220, 196), trim=(170, 60, 40), belt=(140, 70, 40),
                  boots=(96, 60, 38), trousers=(70, 70, 92), hair=(40, 28, 24), beard="none", hat="takiya",
                  hat_main=(160, 40, 40), collar="none", cuffs="none", child=True, tunic=True, vest=(128, 66, 42)),
    "child_girl": dict(coat=(206, 90, 110), inner=(206, 90, 110), trim=GOLD, belt=(140, 70, 40), boots=(96, 60, 38),
                       trousers=(206, 90, 110), hair=(40, 28, 24), beard="none", hat="takiya", hat_main=(40, 60, 120),
                       uki=True, collar="none", cuffs="none", child=True, tunic=True, braids=True),
    "warrior": dict(coat=(112, 80, 52), inner=(180, 150, 110), trim=(160, 120, 70), belt=(60, 40, 28),
                    boots=(70, 46, 30), hair=(40, 28, 22), beard="short", beard_color=(52, 36, 28), hat="iron",
                    hat_main=STEEL, collar="plain", armor=True, armor_color=(128, 96, 64), cuffs="leather",
                    weapon="spear"),
    "envoy": dict(coat=(34, 104, 98), inner=(220, 206, 176), trim=GOLD, belt=(70, 44, 30), boots=(80, 52, 34),
                  hair=(40, 30, 24), beard="full", beard_color=(60, 44, 34), hat="tymak", hat_main=(34, 104, 98),
                  fur=(198, 118, 62), collar="fur", cuffs="trim"),
    "elder": dict(coat=(96, 74, 58), inner=(200, 186, 160), trim=(170, 130, 80), belt=(60, 44, 32), boots=(66, 46, 32),
                  hair=(176, 172, 164), beard="long", beard_color=(190, 186, 178), hat="tymak", hat_main=(80, 58, 42),
                  fur=(150, 120, 90), collar="plain", cuffs="trim", weapon="staff", old=True),
    "envoy2": dict(coat=(150, 70, 40), inner=(226, 210, 176), trim=GOLD, belt=(70, 40, 26), boots=(80, 52, 34),
                   hair=(40, 30, 24), beard="short", beard_color=(50, 38, 30), hat="borik", hat_main=(120, 44, 30),
                   fur=(90, 70, 56), collar="fur", cuffs="trim"),
    "raider": dict(coat=(62, 70, 86), inner=(160, 150, 130), trim=(120, 120, 128), belt=(40, 34, 30), boots=(56, 44, 36),
                   hair=(34, 26, 22), beard="short", beard_color=(34, 26, 22), hat="tymak", hat_main=(52, 56, 66),
                   fur=(84, 70, 58), collar="plain", armor=True, armor_color=(96, 100, 112), cuffs="leather",
                   weapon="spear", plume=(40, 40, 48)),
    "resident": dict(coat=(124, 92, 62), inner=(214, 198, 168), trim=(170, 130, 80), belt=(70, 48, 32),
                     boots=(76, 50, 32), hair=(40, 30, 24), beard="short", beard_color=(60, 44, 34), hat="tymak",
                     hat_main=(90, 66, 44), fur=(170, 140, 104), collar="plain", cuffs="none"),
}

ADULT = dict(feet=77, boot=68, hem=68, belt=49, top=35, chin=33, eye=27, head=20, fw=6, hand=57, sh=9, hw=11, leg=4)
CHILD = dict(feet=77, boot=72, hem=66, belt=60, top=51, chin=50, eye=44, head=37, fw=6, hand=64, sh=7, hw=8, leg=3)


class Canvas:
    """Drawing surface; <dy> shifts everything down, e.g. so a tall hat fits inside a portrait."""

    def __init__(self, width, height, dy=0):
        self.image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        self.width, self.height = width, height
        self.dy = dy

    def px(self, x, y, color):
        y += self.dy
        if 0 <= x < self.width and 0 <= y < self.height:
            self.image.putpixel((int(x), int(y)), tuple(color[:3]) + (255,))

    def rect(self, x0, y0, x1, y1, color):
        if x1 >= x0 and y1 >= y0:
            self.draw.rectangle([x0, y0 + self.dy, x1, y1 + self.dy], fill=tuple(color[:3]) + (255,))

    def poly(self, points, color):
        self.draw.polygon([(x, y + self.dy) for x, y in points], fill=tuple(color[:3]) + (255,))

    def ellipse(self, x0, y0, x1, y1, color):
        self.draw.ellipse([x0, y0 + self.dy, x1, y1 + self.dy], fill=tuple(color[:3]) + (255,))

    def line(self, x0, y0, x1, y1, color):
        self.draw.line([x0, y0 + self.dy, x1, y1 + self.dy], fill=tuple(color[:3]) + (255,))


def outlined(image, color=OUTLINE):
    width, height = image.size
    source = image.load()
    result = image.copy()
    target = result.load()
    for y in range(height):
        for x in range(width):
            if source[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < width and 0 <= ny < height and source[nx, ny][3]:
                    target[x, y] = color + (255,)
                    break
    return result


def hue_shift(color, amount):
    """Pixel-art shading: shadows lean cool and violet, highlights lean warm, instead of plain darker or lighter."""
    if amount < 0:
        k = min(1.0, -amount)
        return mix(shade(color, 1 - k * .5), (46, 32, 78), k * .3)
    k = min(1.0, amount)
    return mix(color, (255, 238, 196), k * .55)


def render(image, outline_strength=.42):
    """Light pass for every sprite: light from the upper left, each shape shaded like a cylinder, a warm rim on
    top edges, slightly darker toward the ground, and an outline tinted by the colour it surrounds."""
    width, height = image.size
    source = image.load()
    result = image.copy()
    target = result.load()
    for y in range(height):
        x = 0
        while x < width:
            if not source[x, y][3]:
                x += 1
                continue
            start = x
            while x < width and source[x, y][3]:
                x += 1
            end = x - 1
            run = end - start + 1
            for i in range(start, end + 1):
                color = source[i, y][:3]
                amount = 0.0
                if run >= 4:
                    t = (i - start) / (run - 1)
                    if t < .25:
                        amount += .3 * (1 - t / .25) + .08
                    elif t > .62:
                        amount -= .1 + .34 * (t - .62) / .38
                if y > 0 and not source[i, y - 1][3]:
                    amount += .4
                amount -= .16 * max(0.0, (y - height * .5) / (height * .5))
                if amount:
                    color = hue_shift(color, amount)
                target[i, y] = tuple(int(v) for v in color) + (255,)
    lit = result.copy()
    out = lit.load()
    shaded = result.load()
    for y in range(height):
        for x in range(width):
            if source[x, y][3]:
                continue
            neighbours = [shaded[nx, ny][:3] for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))
                          if 0 <= nx < width and 0 <= ny < height and source[nx, ny][3]]
            if neighbours:
                avg = tuple(sum(c[i] for c in neighbours) // len(neighbours) for i in range(3))
                out[x, y] = mix(shade(avg, outline_strength), OUTLINE, .55) + (255,)
    return lit


# ---------------------------------------------------------------- world sprites

def coat_half_width(L, y, sway=0):
    t = (y - L["top"]) / max(1, L["hem"] - L["top"])
    return round(L["sh"] + (L["hw"] - L["sh"]) * t)


def draw_boots(c, look, L, cx, step, dress):
    for side in (-1, 1):
        lift = 2 if (step == 1 and side == -1) or (step == -1 and side == 1) else 0
        x = cx + side * L["leg"]
        top = L["boot"] if not dress else L["hem"] - 1
        bottom = L["feet"] - lift
        boot = look["boots"] if side < 0 else shade(look["boots"], .8)
        if look.get("tunic"):
            c.rect(x - 2, L["hem"] - 1, x + 2, L["boot"], look.get("trousers", look["coat"]))
        c.rect(x - 2, top, x + 2, bottom, boot)
        c.rect(x - 2, bottom, x + 2, bottom, shade(look["boots"], .55))
        if not dress and not look.get("child"):
            c.px(x, top + 3, mix(boot, GOLD, .55))


def draw_arm(c, look, L, x, direction, swing, narrow=False, dark=False):
    """Sleeve from the shoulder to the wrist; direction -1 is screen-left."""
    sleeve = look["coat"] if not look.get("dress") else look["coat"]
    if look.get("vest") and look.get("tunic"):
        sleeve = look["coat"]
    if dark:
        sleeve = shade(sleeve, .78)
    top, hand = L["top"] + 1, L["hand"] - swing
    width = 2 if narrow else 3
    outer = x + direction * width
    c.poly([(x, top), (outer, top + 1), (outer + direction, hand - 1), (x + direction, hand - 1)], sleeve)
    cuff = look.get("cuffs", "none")
    cuff_color = {"fur": look.get("fur", FUR), "trim": look["trim"], "leather": (92, 60, 36)}.get(cuff)
    if cuff_color:
        c.rect(min(x + direction, outer + direction), hand - 3, max(x + direction, outer + direction), hand - 1, cuff_color)
    hx0, hx1 = sorted((x + direction, outer + direction))
    c.rect(hx0, hand, hx1, hand + 2, SKIN if not dark else SKIN_DARK)


def draw_face_front(c, look, L, hx, view):
    fw, top, chin, ey = L["fw"], L["head"], L["chin"], L["eye"]
    skin = SKIN
    c.rect(hx - 2, chin, hx + 2, L["top"], SKIN_DARK)
    c.rect(hx - fw, top + 1, hx + fw, chin - 1, skin)
    c.rect(hx - fw + 1, top, hx + fw - 1, chin, skin)
    c.rect(hx + fw - 1, top + 2, hx + fw, chin - 2, SKIN_DARK)
    ex = hx + (1 if view == "SE" else 0)
    left, right = (ex - 3, ex + 2) if view == "S" else (ex - 2, ex + 3)
    brow = look.get("beard_color", look["hair"]) if look.get("old") else look["hair"]
    for x in (left, right):
        c.rect(x, ey, x + 1, ey, EYE)
        c.rect(x, ey - 2, x + 1, ey - 2, brow)
    c.px(ex, ey + 2, SKIN_DARK)
    c.rect(ex - 1, ey + 4, ex + 1, ey + 4, (160, 96, 80))
    if look.get("old"):
        c.px(left - 1, ey + 1, SKIN_DARK)
        c.px(right + 2, ey + 1, SKIN_DARK)
    beard, color = look.get("beard", "none"), look.get("beard_color", look["hair"])
    if beard in ("short", "full", "long"):
        c.rect(ex - 2, ey + 3, ex + 2, ey + 3, color)
    if beard == "short":
        c.rect(ex - 1, ey + 5, ex + 1, chin + 2, color)
    elif beard == "full":
        c.poly([(hx - fw, ey + 1), (hx + fw, ey + 1), (hx + fw - 1, chin), (hx + 2, chin + 3), (hx - 2, chin + 3),
                (hx - fw + 1, chin)], color)
        c.rect(ex - 1, ey + 4, ex + 1, ey + 4, shade(color, .6))
        c.rect(hx - fw + 2, ey, hx - fw + 3, ey + 2, SKIN)
    elif beard == "long":
        c.poly([(hx - fw, ey + 1), (hx + fw, ey + 1), (hx + fw - 1, chin + 2), (hx + 1, L["top"] + 9),
                (hx - 1, L["top"] + 9), (hx - fw + 1, chin + 2)], color)
        c.line(hx, ey + 6, hx, L["top"] + 7, shade(color, .82))
        c.rect(ex - 1, ey + 4, ex + 1, ey + 4, shade(color, .7))


def draw_head_back(c, look, L, hx):
    fw, top, chin, ey = L["fw"], L["head"], L["chin"], L["eye"]
    c.rect(hx - 2, chin, hx + 2, L["top"], SKIN_DARK)
    if look["hat"] == "kimeshek":
        c.rect(hx - fw, top, hx + fw, chin, WHITE)
        c.rect(hx + fw - 2, top + 1, hx + fw, chin, shade(WHITE, .86))
        return
    hair = mix(look["hair"], SKIN_DARK, .2)
    c.rect(hx - fw + 1, top + 1, hx + fw - 1, ey + 3, hair)
    c.rect(hx - fw + 2, ey + 4, hx + fw - 2, chin - 2, SKIN_DARK)
    c.rect(hx - fw + 2, ey + 4, hx + fw - 2, ey + 4, shade(hair, .8))
    c.rect(hx - fw, ey, hx - fw, ey + 2, SKIN)
    c.rect(hx + fw, ey, hx + fw, ey + 2, SKIN_DARK)
    c.rect(hx + fw - 2, top + 2, hx + fw - 1, ey + 3, shade(hair, .75))


def draw_hat_front(c, look, L, hx, back):
    fw, top = L["fw"], L["head"]
    hat, main = look["hat"], look.get("hat_main", WHITE)
    if hat == "khan":
        c.poly([(hx - fw + 1, top), (hx + fw - 1, top), (hx + 2, top - 9), (hx - 2, top - 9)], main)
        c.poly([(hx + 1, top), (hx + fw - 1, top), (hx + 2, top - 9)], shade(main, .74))
        c.line(hx, top - 9, hx, top - 1, GOLD)
        c.rect(hx - 1, top - 11, hx + 1, top - 10, GOLD_LIGHT)
        c.ellipse(hx - fw - 2, top - 2, hx + fw + 2, top + 4, look["fur"])
        for x in range(hx - fw - 1, hx + fw + 2, 3):
            c.px(x, top + 2, FUR_DARK)
        if not back:
            c.px(hx, top + 1, (60, 150, 140))
            for i, (x, y) in enumerate([(-3, -3), (-4, -5), (-5, -7), (-5, -9), (-4, -11), (-6, -8)]):
                c.px(hx + x, top + y, WHITE if i % 2 == 0 else FUR)
    elif hat == "borik":
        c.ellipse(hx - fw + 1, top - 8, hx + fw - 1, top + 2, main)
        c.rect(hx + 1, top - 6, hx + fw - 1, top, shade(main, .74))
        c.line(hx, top - 8, hx, top - 2, GOLD)
        c.rect(hx - 1, top - 10, hx + 1, top - 9, GOLD_LIGHT)
        c.ellipse(hx - fw - 2, top - 2, hx + fw + 2, top + 4, look["fur"])
        for x in range(hx - fw - 1, hx + fw + 2, 2):
            c.px(x, top + 2 + (x % 3 == 0), shade(look["fur"], .75))
    elif hat == "helmet":
        c.rect(hx - fw - 1, top, hx - fw + 1, L["chin"] + 1, (104, 74, 44))
        c.rect(hx + fw - 1, top, hx + fw + 1, L["chin"] + 1, (84, 58, 34))
        if back:
            c.rect(hx - fw - 1, top, hx + fw + 1, L["chin"] + 2, (96, 68, 40))
        c.poly([(hx - fw - 1, top + 1), (hx + fw + 1, top + 1), (hx + 1, top - 10), (hx - 1, top - 10)], main)
        c.poly([(hx + 1, top + 1), (hx + fw + 1, top + 1), (hx + 1, top - 10)], GOLD_DARK)
        c.line(hx - 3, top - 3, hx - 1, top - 8, GOLD_LIGHT)
        c.ellipse(hx - fw - 2, top - 1, hx + fw + 2, top + 3, look["fur"])
        plume = look.get("plume", (170, 34, 40))
        for i, (x, y) in enumerate([(0, -12), (-1, -13), (-2, -13), (-3, -12), (-4, -11), (-5, -9), (-5, -8)]):
            c.px(hx + x, top + y, plume if i % 3 else shade(plume, 1.25))
        c.px(hx, top - 11, GOLD_LIGHT)
    elif hat == "iron":
        c.rect(hx - fw - 1, top, hx + fw + 1, top + 2, shade(main, .8))
        c.poly([(hx - fw, top + 1), (hx + fw, top + 1), (hx + 1, top - 8), (hx - 1, top - 8)], main)
        c.poly([(hx + 1, top + 1), (hx + fw, top + 1), (hx + 1, top - 8)], STEEL_DARK)
        c.px(hx, top - 9, STEEL_DARK)
        if back:
            c.rect(hx - fw, top + 2, hx + fw, L["chin"], (92, 70, 48))
    elif hat == "kalpak":
        c.poly([(hx - fw - 1, top + 2), (hx + fw + 1, top + 2), (hx + fw - 2, top - 8), (hx - fw + 2, top - 8)], main)
        c.ellipse(hx - fw + 2, top - 10, hx + fw - 2, top - 6, main)
        c.poly([(hx + 1, top + 2), (hx + fw + 1, top + 2), (hx + fw - 2, top - 8), (hx + 1, top - 9)], shade(main, .84))
        c.rect(hx - fw - 2, top + 1, hx + fw + 2, top + 3, main)
        c.rect(hx - fw - 2, top + 3, hx + fw + 2, top + 3, (40, 30, 30))
        if not back:
            c.px(hx, top + 1, (40, 30, 30))
            c.px(hx, top + 2, (40, 30, 30))
        c.px(hx - 1, top - 4, (70, 50, 40))
        c.px(hx + 1, top - 4, (70, 50, 40))
        c.px(hx, top - 5, (70, 50, 40))
    elif hat == "takiya":
        c.ellipse(hx - fw, top - 4, hx + fw, top + 3, main)
        c.rect(hx - fw, top + 1, hx + fw, top + 2, GOLD)
        for x in range(hx - fw + 2, hx + fw - 1, 3):
            c.px(x, top - 1, GOLD_LIGHT)
        if look.get("uki"):
            for x, y, col in [(0, -5, WHITE), (1, -6, (30, 30, 30)), (0, -7, WHITE), (-1, -8, (30, 30, 30)), (1, -8, WHITE)]:
                c.px(hx + x, top + y, col)
    elif hat == "tymak":
        c.ellipse(hx - fw + 1, top - 7, hx + fw - 1, top + 1, main)
        c.rect(hx + 1, top - 5, hx + fw - 1, top, shade(main, .75))
        c.ellipse(hx - fw - 2, top - 2, hx + fw + 2, top + 4, look["fur"])
        c.rect(hx - fw - 2, top + 2, hx - fw, L["chin"] - 1, look["fur"])
        c.rect(hx + fw, top + 2, hx + fw + 2, L["chin"] - 1, shade(look["fur"], .8))
        if back:
            c.rect(hx - fw - 1, top + 2, hx + fw + 1, L["chin"], shade(look["fur"], .88))
        for x in range(hx - fw - 1, hx + fw + 2, 2):
            c.px(x, top + 3, shade(look["fur"], .72))


def draw_kimeshek_base(c, look, L, hx, back):
    fw, top, chin = L["fw"], L["head"], L["chin"]
    c.ellipse(hx - fw - 3, top - 4, hx + fw + 3, chin + 6, WHITE)
    c.rect(hx + fw - 1, top, hx + fw + 3, chin + 3, shade(WHITE, .86))
    if not back:
        c.rect(hx - 3, chin + 2, hx + 3, L["top"] + 5, (150, 34, 48))
        c.rect(hx - 3, chin + 2, hx + 3, chin + 2, GOLD)
        c.px(hx, chin + 4, GOLD_LIGHT)
        c.px(hx - 2, chin + 5, GOLD)
        c.px(hx + 2, chin + 5, GOLD)


def draw_kimeshek_top(c, look, L, hx):
    fw, top = L["fw"], L["head"]
    c.ellipse(hx - fw - 2, top - 9, hx + fw + 2, top + 2, WHITE)
    for y in (top - 6, top - 3):
        c.line(hx - fw, y + 1, hx + fw, y - 1, shade(WHITE, .82))
    c.rect(hx + fw - 1, top - 6, hx + fw + 2, top + 1, shade(WHITE, .86))


def draw_armor_front(c, look, L, cx, top, back, width_fn):
    color = look.get("armor_color", STEEL)
    dark = shade(color, .7)
    bottom = L["belt"] + 7
    for y in range(top + 2, bottom + 1):
        w = width_fn(y) - 1
        c.rect(cx - w, y, cx + w, y, color)
        if (y - top) % 3 == 0:
            c.rect(cx - w, y, cx + w, y, dark)
        else:
            for x in range(cx - w + ((y // 3) % 2), cx + w + 1, 3):
                c.px(x, y, dark)
    w = width_fn(top + 2)
    c.rect(cx + w - 3, top + 2, cx + w - 1, bottom, shade(color, .78))
    if not back and "armor_color" not in look:
        c.rect(cx - 1, top + 5, cx + 1, top + 7, GOLD)
        c.px(cx, top + 6, GOLD_LIGHT)


def draw_weapon(c, look, L, x, front):
    weapon = look.get("weapon")
    if weapon == "spear":
        top = L["head"] - 16
        c.rect(x, top + 3, x, L["hem"] + 2, WOOD)
        c.poly([(x - 1, top + 3), (x + 1, top + 3), (x, top)], STEEL)
        c.px(x, top + 1, WHITE)
        c.rect(x - 1, top + 4, x + 1, top + 6, look.get("plume", (170, 34, 40)))
    elif weapon == "staff":
        top = L["head"] - 4
        c.rect(x, top, x, L["feet"], WOOD)
        c.rect(x, top - 1, x + 2, top, WOOD)
        c.px(x + 2, top + 1, WOOD)


def draw_frontal(c, look, L, view, step):
    back = view in ("N", "NE")
    turned = view in ("SE", "NE")
    cx = 24
    sh, hw = L["sh"] - turned, L["hw"] - turned
    top, hem, belt = L["top"], L["hem"], L["belt"]
    dress, tunic = look.get("dress"), look.get("tunic")
    sway = step
    coat = look["coat"]
    coat_dark = shade(coat, .72)
    width = lambda y: round(sh + (hw - sh) * (y - top) / max(1, hem - top))
    spear_x = cx + sh + 5
    if look.get("weapon") == "staff":
        spear_x = cx - sh - 5
    if back and look.get("weapon"):
        draw_weapon(c, look, L, spear_x, False)
    if turned:
        draw_arm(c, look, L, cx + sh - 1, 1, -step * 2, narrow=True, dark=True)
    draw_boots(c, look, L, cx + (1 if turned else 0), step, dress)
    hem_y = hem + (8 if dress else 0)
    hem_w = hw + (1 if dress else 0)
    c.poly([(cx - sh, top), (cx + sh, top), (cx + hem_w + sway, hem_y), (cx - hem_w + sway, hem_y)], coat)
    c.ellipse(cx - sh, top - 2, cx + sh, top + 4, coat)
    c.poly([(cx + sh - 3, top + 1), (cx + sh, top + 1), (cx + hem_w + sway, hem_y), (cx + hem_w - 4 + sway, hem_y)], coat_dark)
    c.line(cx - sh + 1, top + 2, cx - hem_w + 1 + sway, hem_y - 1, shade(coat, 1.15))
    trim = look["trim"]
    if not tunic:
        c.rect(cx - hem_w + sway + 1, hem_y - 2, cx + hem_w + sway - 1, hem_y - 1, trim)
        for x in range(cx - hem_w + sway + 2, cx + hem_w + sway - 1, 3):
            c.px(x, hem_y - 2, shade(trim, .7))
    if look.get("armor"):
        draw_armor_front(c, look, L, cx, top, back, width)
    ox = cx + (3 if turned else 0)
    if not back and not dress and not tunic and not look.get("armor"):
        inner = look["inner"]
        c.poly([(ox - 1, top + 3), (ox + 1, top + 3), (ox + 3 + sway, hem - 1), (ox - 3 + sway, hem - 1)], inner)
        c.line(ox - 1, top + 3, ox - 3 + sway, hem - 1, trim)
        c.line(ox + 1, top + 3, ox + 3 + sway, hem - 1, shade(trim, .8))
        if look.get("collar") == "fur":
            c.line(ox - 2, top + 3, ox - 4 + sway, hem - 3, look.get("fur", FUR))
    if back and not tunic and not dress:
        c.poly([(cx, top + 6), (cx + 3, top + 9), (cx, top + 12), (cx - 3, top + 9)], trim)
        c.poly([(cx, top + 8), (cx + 1, top + 9), (cx, top + 10), (cx - 1, top + 9)], coat)
    if look.get("vest"):
        vest = look["vest"]
        w = width(top + 1)
        bottom = belt + (0 if tunic else 2)
        c.rect(cx - w + 1, top + 1, cx + w - 1, bottom, vest)
        c.rect(cx + w - 3, top + 1, cx + w - 1, bottom, shade(vest, .72))
        if not back:
            c.rect(ox - 1, top + 2, ox + 1, bottom, look["coat"] if not tunic else look["coat"])
            c.line(ox - 2, top + 2, ox - 2, bottom, GOLD)
            c.line(ox + 2, top + 2, ox + 2, bottom, GOLD)
    w = width(belt)
    c.rect(cx - w, belt, cx + w, belt + 1, look["belt"])
    if not back:
        c.rect(ox - 1, belt - 1, ox + 1, belt + 2, GOLD)
        c.px(ox, belt, GOLD_LIGHT)
    if tunic:
        c.rect(cx - hw + sway, hem - 1, cx + hw + sway, hem, look["trim"])
    draw_arm(c, look, L, cx - sh + 1, -1, step * 2)
    if not turned:
        draw_arm(c, look, L, cx + sh - 1, 1, -step * 2, dark=True)
    collar = look.get("collar")
    hx = cx + (1 if view == "SE" else -1 if view == "NE" else 0)
    if collar == "fur":
        c.ellipse(cx - 7, top - 3, cx + 7, top + 3, look.get("fur", FUR))
        for x in range(cx - 6, cx + 7, 2):
            c.px(x, top + 1 + (x % 4 == 0), shade(look.get("fur", FUR), .8))
    elif collar == "mantle":
        c.ellipse(cx - sh - 3, top - 3, cx + sh + 3, top + 5, look["fur"])
        for x in range(cx - sh - 2, cx + sh + 3, 2):
            c.px(x, top + 3 + (x % 3 == 0), shade(look["fur"], .76))
    elif collar == "plain":
        c.rect(hx - 3, top - 1, hx + 3, top + 1, look["trim"])
    if look["hat"] == "kimeshek":
        draw_kimeshek_base(c, look, L, hx, back)
    if back:
        draw_head_back(c, look, L, hx)
    else:
        draw_face_front(c, look, L, hx, view)
    if look.get("braids"):
        c.rect(hx - L["fw"] - 1, L["eye"], hx - L["fw"], L["top"] + 6, look["hair"])
        c.rect(hx + L["fw"], L["eye"], hx + L["fw"] + 1, L["top"] + 6, shade(look["hair"], .8))
    if look["hat"] == "kimeshek":
        draw_kimeshek_top(c, look, L, hx)
    else:
        draw_hat_front(c, look, L, hx, back)
    if not back and look.get("weapon"):
        draw_weapon(c, look, L, spear_x, True)


def draw_profile(c, look, L, step):
    cx = 23
    top, hem, belt, feet = L["top"], L["hem"], L["belt"], L["feet"]
    dress, tunic = look.get("dress"), look.get("tunic")
    coat = look["coat"]
    trim = look["trim"]
    hem_y = hem + (8 if dress else 0)
    if look.get("weapon") == "staff":
        draw_weapon(c, look, L, cx + 7, True)
    far_arm_swing = step * 3
    c.poly([(cx - 1, top + 1), (cx + 1, top + 1), (cx + 1 + far_arm_swing, L["hand"] - 1), (cx - 1 + far_arm_swing, L["hand"] - 1)],
           shade(coat, .66))
    c.rect(cx - 1 + far_arm_swing, L["hand"], cx + 1 + far_arm_swing, L["hand"] + 2, SKIN_DARK)
    for near, offset in ((False, -3 * step), (True, 3 * step)):
        x = cx - 2 + offset + (0 if near else -1)
        boot = look["boots"] if near else shade(look["boots"], .75)
        if tunic:
            c.rect(x, hem - 1, x + 3, L["boot"], look.get("trousers", coat) if near else shade(look.get("trousers", coat), .8))
        c.rect(x, L["boot"] if not dress else hem_y - 1, x + 3, feet - 1, boot)
        c.rect(x + 3, feet - 2, x + 5, feet - 1, boot)
        c.rect(x, feet, x + 5, feet, shade(look["boots"], .55))
    c.poly([(cx - 6, top), (cx + 4, top), (cx + 6 + step, hem_y), (cx - 8 + step, hem_y)], coat)
    c.ellipse(cx - 6, top - 2, cx + 4, top + 4, coat)
    c.poly([(cx - 6, top + 1), (cx - 3, top + 1), (cx - 5 + step, hem_y), (cx - 8 + step, hem_y)], shade(coat, .78))
    if not tunic:
        c.rect(cx - 7 + step, hem_y - 2, cx + 5 + step, hem_y - 1, trim)
        c.line(cx + 4, top + 2, cx + 6 + step, hem_y - 1, trim)
    if look.get("armor"):
        color = look.get("armor_color", STEEL)
        for y in range(top + 2, belt + 8):
            c.rect(cx - 5, y, cx + 4, y, shade(color, .72) if (y - top) % 3 == 0 else color)
    if look.get("vest"):
        c.rect(cx - 5, top + 1, cx + 3, belt + (0 if tunic else 2), look["vest"])
        c.line(cx + 3, top + 2, cx + 3, belt, GOLD)
    c.rect(cx - 6, belt, cx + 5, belt + 1, look["belt"])
    c.rect(cx + 3, belt - 1, cx + 5, belt + 2, GOLD)
    if tunic:
        c.rect(cx - 8 + step, hem - 1, cx + 6 + step, hem, trim)
    collar = look.get("collar")
    if collar == "fur":
        c.ellipse(cx - 5, top - 3, cx + 4, top + 3, look.get("fur", FUR))
    elif collar == "mantle":
        c.ellipse(cx - 7, top - 3, cx + 5, top + 5, look["fur"])
    elif collar == "plain":
        c.rect(cx - 2, top - 1, cx + 3, top + 1, trim)
    hx, fw, head, chin, ey = cx, L["fw"], L["head"], L["chin"], L["eye"]
    if look["hat"] == "kimeshek":
        c.ellipse(hx - fw - 3, head - 4, hx + fw + 1, chin + 6, WHITE)
        c.rect(hx - fw - 3, head, hx - 1, chin + 5, shade(WHITE, .86))
    c.rect(hx - 1, chin, hx + 2, top, SKIN_DARK)
    c.rect(hx - fw + 1, head + 1, hx + fw - 1, chin - 1, SKIN)
    c.rect(hx - fw + 2, head, hx + fw - 2, chin, SKIN)
    c.rect(hx + fw - 1, ey + 1, hx + fw, ey + 2, SKIN)
    c.px(hx + fw, ey + 2, SKIN_DARK)
    if look["hat"] != "kimeshek":
        c.rect(hx - fw + 1, head + 1, hx - 2, chin - 3, look["hair"])
    c.px(hx - 1, ey + 1, SKIN_DARK)
    c.px(hx + 3, ey, EYE)
    c.px(hx + 3, ey - 2, look["hair"])
    c.px(hx + 4, ey - 2, look["hair"])
    c.rect(hx + 3, ey + 4, hx + 4, ey + 4, (160, 96, 80))
    beard, color = look.get("beard", "none"), look.get("beard_color", look["hair"])
    if beard != "none":
        c.rect(hx + 2, ey + 3, hx + fw - 1, ey + 3, color)
    if beard == "short":
        c.rect(hx + 3, ey + 5, hx + 4, chin + 2, color)
    elif beard in ("full", "long"):
        low = chin + 3 if beard == "full" else top + 8
        c.poly([(hx - 1, ey + 2), (hx + fw - 1, ey + 3), (hx + fw - 1, chin), (hx + 3, low), (hx + 1, low), (hx - 1, chin)], color)
        c.rect(hx + 3, ey + 4, hx + 4, ey + 4, shade(color, .6))
    if look.get("braids"):
        c.rect(hx - fw, ey, hx - fw + 1, top + 6, look["hair"])
    hat, main = look["hat"], look.get("hat_main", WHITE)
    top_ = head
    if hat == "khan":
        c.poly([(hx - fw + 1, top_), (hx + fw - 2, top_), (hx + 1, top_ - 9), (hx - 3, top_ - 9)], main)
        c.line(hx - 1, top_ - 9, hx - 1, top_ - 1, GOLD)
        c.rect(hx - 2, top_ - 11, hx, top_ - 10, GOLD_LIGHT)
        c.ellipse(hx - fw - 1, top_ - 2, hx + fw + 1, top_ + 4, look["fur"])
        for i, (x, y) in enumerate([(-4, -4), (-5, -6), (-6, -8), (-6, -10), (-5, -12)]):
            c.px(hx + x, top_ + y, WHITE if i % 2 == 0 else FUR)
    elif hat == "borik":
        c.ellipse(hx - fw + 1, top_ - 8, hx + fw - 1, top_ + 2, main)
        c.rect(hx - 2, top_ - 10, hx, top_ - 9, GOLD_LIGHT)
        c.ellipse(hx - fw - 1, top_ - 2, hx + fw + 1, top_ + 4, look["fur"])
    elif hat == "helmet":
        c.rect(hx - fw, top_, hx - 1, chin + 1, (96, 68, 40))
        c.poly([(hx - fw - 1, top_ + 1), (hx + fw, top_ + 1), (hx, top_ - 10), (hx - 2, top_ - 10)], main)
        c.poly([(hx - 1, top_ + 1), (hx + fw, top_ + 1), (hx, top_ - 10)], GOLD_DARK)
        c.ellipse(hx - fw - 1, top_ - 1, hx + fw + 1, top_ + 3, look["fur"])
        plume = look.get("plume", (170, 34, 40))
        for x, y in [(-1, -12), (-2, -12), (-3, -11), (-4, -10), (-5, -9), (-6, -8), (-7, -6), (-7, -5)]:
            c.px(hx + x, top_ + y, plume)
    elif hat == "iron":
        c.rect(hx - fw, top_ + 2, hx - 1, chin, (92, 70, 48))
        c.poly([(hx - fw, top_ + 1), (hx + fw, top_ + 1), (hx, top_ - 8), (hx - 2, top_ - 8)], main)
        c.rect(hx - fw - 1, top_, hx + fw, top_ + 2, shade(main, .8))
    elif hat == "kalpak":
        c.poly([(hx - fw, top_ + 2), (hx + fw, top_ + 2), (hx + fw - 3, top_ - 8), (hx - fw + 1, top_ - 8)], main)
        c.ellipse(hx - fw + 1, top_ - 10, hx + fw - 3, top_ - 6, main)
        c.rect(hx - fw - 1, top_ + 1, hx + fw + 1, top_ + 3, main)
        c.rect(hx - fw - 1, top_ + 3, hx + fw + 1, top_ + 3, (40, 30, 30))
        c.rect(hx + fw, top_ - 1, hx + fw + 1, top_ + 2, main)
    elif hat == "takiya":
        c.ellipse(hx - fw, top_ - 4, hx + fw - 1, top_ + 3, main)
        c.rect(hx - fw, top_ + 1, hx + fw - 1, top_ + 2, GOLD)
        if look.get("uki"):
            c.px(hx - 1, top_ - 5, WHITE)
            c.px(hx - 2, top_ - 6, (30, 30, 30))
            c.px(hx - 2, top_ - 7, WHITE)
    elif hat == "tymak":
        c.ellipse(hx - fw + 1, top_ - 7, hx + fw - 1, top_ + 1, main)
        c.ellipse(hx - fw - 1, top_ - 2, hx + fw + 1, top_ + 4, look["fur"])
        c.rect(hx - fw - 1, top_ + 2, hx, chin, look["fur"])
    elif hat == "kimeshek":
        c.ellipse(hx - fw - 2, head - 9, hx + fw + 1, head + 2, WHITE)
        c.line(hx - fw, head - 5, hx + fw - 1, head - 7, shade(WHITE, .82))
    c.poly([(cx + 1, top + 1), (cx + 4, top + 1), (cx + 4 - far_arm_swing, L["hand"] - 1), (cx + 1 - far_arm_swing, L["hand"] - 1)], coat)
    cuff = {"fur": look.get("fur", FUR), "trim": trim, "leather": (92, 60, 36)}.get(look.get("cuffs"))
    if cuff:
        c.rect(cx + 1 - far_arm_swing, L["hand"] - 3, cx + 4 - far_arm_swing, L["hand"] - 1, cuff)
    c.rect(cx + 1 - far_arm_swing, L["hand"], cx + 3 - far_arm_swing, L["hand"] + 2, SKIN)
    if look.get("weapon") == "spear":
        draw_weapon(c, look, L, cx + 6, True)


DIRECTIONS = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"]
MIRROR = {"SW": "SE", "W": "E", "NW": "NE"}


def sprite(look_id, direction, step):
    look = LOOKS[look_id]
    L = CHILD if look.get("child") else ADULT
    base = MIRROR.get(direction, direction)
    c = Canvas(48, 80)
    if base == "E":
        draw_profile(c, look, L, step)
    else:
        draw_frontal(c, look, L, base, step)
    image = render(c.image)
    if direction in MIRROR:
        image = image.transpose(Image.FLIP_LEFT_RIGHT)
    return image


# ---------------------------------------------------------------- dialogue busts

def bust(look_id):
    look = LOOKS[look_id]
    c = Canvas(64, 64, dy=5)
    cx = 32
    child = look.get("child")
    fw = 10 if not child else 11
    head, chin = (18, 44) if not child else (24, 48)
    ey = head + 12 if not child else head + 12
    coat = look["coat"]
    shoulder_top = chin + 5
    c.poly([(cx - 20, shoulder_top + 4), (cx + 20, shoulder_top + 4), (cx + 27, 64), (cx - 27, 64)], coat)
    c.ellipse(cx - 21, shoulder_top, cx + 21, shoulder_top + 12, coat)
    c.poly([(cx + 8, shoulder_top + 2), (cx + 21, shoulder_top + 4), (cx + 27, 64), (cx + 12, 64)], shade(coat, .74))
    if look.get("armor"):
        color = look.get("armor_color", STEEL)
        for y in range(shoulder_top + 6, 64):
            w = 14 + (y - shoulder_top) // 3
            c.rect(cx - w, y, cx + w, y, shade(color, .72) if y % 3 == 0 else color)
            if y % 3:
                for x in range(cx - w + (y // 3) % 2, cx + w, 3):
                    c.px(x, y, shade(color, .82))
        if "armor_color" not in look:
            c.ellipse(cx - 3, shoulder_top + 9, cx + 3, shoulder_top + 15, GOLD)
            c.px(cx, shoulder_top + 12, GOLD_LIGHT)
    elif look.get("vest"):
        c.poly([(cx - 16, shoulder_top + 5), (cx + 16, shoulder_top + 5), (cx + 21, 64), (cx - 21, 64)], look["vest"])
        c.poly([(cx - 4, shoulder_top + 5), (cx + 4, shoulder_top + 5), (cx + 6, 64), (cx - 6, 64)], coat)
        c.line(cx - 5, shoulder_top + 5, cx - 7, 63, GOLD)
        c.line(cx + 5, shoulder_top + 5, cx + 7, 63, GOLD)
    elif not look.get("dress"):
        c.poly([(cx - 3, shoulder_top + 3), (cx + 3, shoulder_top + 3), (cx + 9, 64), (cx - 9, 64)], look["inner"])
        c.line(cx - 3, shoulder_top + 3, cx - 9, 63, look["trim"])
        c.line(cx - 4, shoulder_top + 3, cx - 10, 63, look["trim"])
        c.line(cx + 3, shoulder_top + 3, cx + 9, 63, shade(look["trim"], .8))
    collar = look.get("collar")
    if collar == "fur":
        c.ellipse(cx - 17, shoulder_top - 3, cx + 17, shoulder_top + 9, look.get("fur", FUR))
        for x in range(cx - 15, cx + 16, 2):
            c.px(x, shoulder_top + 5 + (x % 4 == 0), shade(look.get("fur", FUR), .78))
    elif collar == "mantle":
        c.ellipse(cx - 25, shoulder_top - 2, cx + 25, shoulder_top + 12, look["fur"])
        for x in range(cx - 23, cx + 24, 2):
            c.px(x, shoulder_top + 8 + (x % 3 == 0), shade(look["fur"], .74))
    elif collar == "plain":
        c.rect(cx - 7, shoulder_top - 1, cx + 7, shoulder_top + 2, look["trim"])
    if look["hat"] == "kimeshek":
        c.ellipse(cx - fw - 6, head - 8, cx + fw + 6, chin + 12, WHITE)
        c.rect(cx + fw - 1, head - 2, cx + fw + 6, chin + 8, shade(WHITE, .86))
        c.rect(cx - 7, chin + 3, cx + 7, 63, (150, 34, 48))
        c.rect(cx - 7, chin + 3, cx + 7, chin + 4, GOLD)
        for x, y in [(0, 9), (-4, 12), (4, 12), (0, 15)]:
            c.rect(cx + x - 1, chin + y - 1, cx + x + 1, chin + y + 1, GOLD)
    c.rect(cx - 4, chin - 2, cx + 4, shoulder_top + 2, SKIN_DARK)
    c.rect(cx - fw, head + 3, cx + fw, chin - 4, SKIN)
    c.rect(cx - fw + 1, head + 1, cx + fw - 1, chin - 2, SKIN)
    c.rect(cx - fw + 3, head, cx + fw - 3, chin, SKIN)
    c.rect(cx + fw - 3, head + 4, cx + fw, chin - 5, SKIN_DARK)
    c.rect(cx + 2, chin - 3, cx + fw - 3, chin, SKIN_DARK)
    c.rect(cx - fw - 1, ey, cx - fw, ey + 4, SKIN_DARK)
    c.rect(cx + fw, ey, cx + fw + 1, ey + 4, SKIN_DARK)
    brow = look.get("beard_color") if look.get("old") else look["hair"]
    for side in (-1, 1):
        x = cx + side * 5
        c.rect(x - 2, ey, x + 2, ey + 1, WHITE)
        c.rect(x - 1 + (side > 0), ey, x + (side > 0), ey + 1, EYE)
        c.rect(x - 3, ey - 3, x + 2, ey - 2, brow)
        if look.get("old"):
            c.px(x + 3 * side, ey + 3, SKIN_DARK)
    c.rect(cx, ey + 2, cx, ey + 6, SKIN_DARK)
    c.rect(cx - 1, ey + 7, cx + 2, ey + 7, SKIN_DARK)
    c.rect(cx - 3, ey + 10, cx + 3, ey + 10, (150, 86, 72))
    if not look.get("beard") or look["beard"] == "none":
        c.px(cx - 6, ey + 6, (222, 140, 120))
        c.px(cx + 6, ey + 6, (222, 140, 120))
    beard, color = look.get("beard", "none"), look.get("beard_color", look["hair"])
    if beard != "none":
        c.rect(cx - 5, ey + 8, cx + 5, ey + 9, color)
        c.px(cx - 6, ey + 10, color)
        c.px(cx + 6, ey + 10, color)
    if beard == "short":
        c.rect(cx - 2, ey + 12, cx + 2, chin + 4, color)
        c.rect(cx - 1, chin + 5, cx + 1, chin + 6, color)
    elif beard == "full":
        c.poly([(cx - fw, ey + 4), (cx - fw + 3, ey + 9), (cx - 3, ey + 11), (cx + 3, ey + 11), (cx + fw - 3, ey + 9),
                (cx + fw, ey + 4), (cx + fw - 1, chin), (cx + 3, chin + 6), (cx - 3, chin + 6), (cx - fw + 1, chin)], color)
        c.rect(cx - 3, ey + 10, cx + 3, ey + 10, shade(color, .6))
        c.line(cx - 4, chin - 2, cx - 2, chin + 4, shade(color, .8))
    elif beard == "long":
        c.poly([(cx - fw, ey + 4), (cx - fw + 3, ey + 9), (cx - 3, ey + 11), (cx + 3, ey + 11), (cx + fw - 3, ey + 9),
                (cx + fw, ey + 4), (cx + fw - 1, chin + 2), (cx + 2, 63), (cx - 2, 63), (cx - fw + 1, chin + 2)], color)
        c.rect(cx - 3, ey + 10, cx + 3, ey + 10, shade(color, .7))
        for x in (cx - 4, cx, cx + 4):
            c.line(x, ey + 13, x + (0 if x == cx else (1 if x > cx else -1)), 60, shade(color, .84))
    if look.get("braids"):
        c.rect(cx - fw - 3, ey - 2, cx - fw - 1, 62, look["hair"])
        c.rect(cx + fw + 1, ey - 2, cx + fw + 3, 62, shade(look["hair"], .8))
    hat, main = look["hat"], look.get("hat_main", WHITE)
    fur = look.get("fur", FUR)
    if hat == "khan":
        c.poly([(cx - fw + 1, head + 1), (cx + fw - 1, head + 1), (cx + 4, head - 16), (cx - 4, head - 16)], main)
        c.poly([(cx + 2, head + 1), (cx + fw - 1, head + 1), (cx + 4, head - 16)], shade(main, .74))
        c.rect(cx - 1, head - 16, cx, head, GOLD)
        c.rect(cx - 2, head - 19, cx + 1, head - 17, GOLD_LIGHT)
        c.ellipse(cx - fw - 4, head - 3, cx + fw + 4, head + 7, fur)
        for x in range(cx - fw - 2, cx + fw + 3, 3):
            c.px(x, head + 4, FUR_DARK)
            c.px(x + 1, head, FUR_DARK)
        c.rect(cx - 1, head, cx + 1, head + 2, (60, 150, 140))
        for i, (x, y) in enumerate([(-6, -2), (-7, -5), (-8, -8), (-9, -11), (-9, -14), (-8, -17), (-10, -9), (-10, -12)]):
            c.rect(cx + x, head + y, cx + x + 1, head + y + 1, WHITE if i % 2 == 0 else FUR)
    elif hat == "borik":
        c.ellipse(cx - fw + 1, head - 14, cx + fw - 1, head + 4, main)
        c.rect(cx + 2, head - 10, cx + fw - 1, head + 1, shade(main, .74))
        c.rect(cx - 1, head - 14, cx, head, GOLD)
        c.rect(cx - 2, head - 17, cx + 1, head - 15, GOLD_LIGHT)
        c.ellipse(cx - fw - 4, head - 3, cx + fw + 4, head + 7, fur)
        for x in range(cx - fw - 2, cx + fw + 3, 2):
            c.px(x, head + 3 + (x % 3 == 0), shade(fur, .72))
    elif hat == "helmet":
        c.rect(cx - fw - 3, head + 2, cx - fw + 1, chin + 3, (104, 74, 44))
        c.rect(cx + fw - 1, head + 2, cx + fw + 3, chin + 3, (84, 58, 34))
        for y in range(head + 4, chin + 3, 3):
            c.px(cx - fw - 1, y, GOLD)
            c.px(cx + fw + 1, y, GOLD_DARK)
        c.poly([(cx - fw - 2, head + 2), (cx + fw + 2, head + 2), (cx + 1, head - 17), (cx - 1, head - 17)], main)
        c.poly([(cx + 1, head + 2), (cx + fw + 2, head + 2), (cx + 1, head - 17)], GOLD_DARK)
        c.line(cx - 5, head - 4, cx - 2, head - 13, GOLD_LIGHT)
        c.ellipse(cx - fw - 4, head - 2, cx + fw + 4, head + 5, fur)
        plume = look.get("plume", (170, 34, 40))
        for x, y in [(0, -19), (-2, -20), (-4, -19), (-6, -17), (-8, -15), (-9, -12), (-9, -10)]:
            c.rect(cx + x - 1, head + y, cx + x, head + y + 1, plume)
        c.rect(cx - 1, head - 18, cx, head - 17, GOLD_LIGHT)
    elif hat == "iron":
        c.rect(cx - fw - 2, head + 2, cx - fw, chin, (92, 70, 48))
        c.rect(cx + fw, head + 2, cx + fw + 2, chin, (80, 60, 40))
        c.poly([(cx - fw - 1, head + 2), (cx + fw + 1, head + 2), (cx + 1, head - 14), (cx - 1, head - 14)], main)
        c.poly([(cx + 1, head + 2), (cx + fw + 1, head + 2), (cx + 1, head - 14)], STEEL_DARK)
        c.rect(cx - fw - 2, head + 1, cx + fw + 2, head + 4, shade(main, .8))
        c.rect(cx - 1, head + 4, cx, ey + 5, shade(main, .8))
    elif hat == "kalpak":
        c.poly([(cx - fw - 2, head + 3), (cx + fw + 2, head + 3), (cx + fw - 3, head - 14), (cx - fw + 3, head - 14)], main)
        c.ellipse(cx - fw + 3, head - 18, cx + fw - 3, head - 10, main)
        c.poly([(cx + 2, head + 3), (cx + fw + 2, head + 3), (cx + fw - 3, head - 14), (cx + 2, head - 16)], shade(main, .86))
        c.rect(cx - fw - 4, head + 1, cx + fw + 4, head + 5, main)
        c.rect(cx - fw - 4, head + 5, cx + fw + 4, head + 5, (40, 30, 30))
        c.rect(cx - 1, head + 1, cx, head + 5, (40, 30, 30))
        for x, y in [(0, -8), (-2, -6), (2, -6), (0, -4)]:
            c.px(cx + x, head + y, (80, 56, 44))
    elif hat == "takiya":
        c.ellipse(cx - fw - 1, head - 6, cx + fw + 1, head + 6, main)
        c.rect(cx - fw - 1, head + 2, cx + fw + 1, head + 4, GOLD)
        for x in range(cx - fw + 2, cx + fw - 1, 4):
            c.rect(x, head - 2, x + 1, head - 1, GOLD_LIGHT)
        if look.get("uki"):
            for i, y in enumerate(range(head - 7, head - 15, -1)):
                c.px(cx + (i % 2), y, WHITE if i % 2 == 0 else (30, 30, 30))
                c.px(cx - 1 + (i % 2), y, (30, 30, 30) if i % 2 == 0 else WHITE)
    elif hat == "tymak":
        c.ellipse(cx - fw + 1, head - 12, cx + fw - 1, head + 3, main)
        c.rect(cx + 2, head - 8, cx + fw - 1, head + 1, shade(main, .74))
        c.ellipse(cx - fw - 4, head - 3, cx + fw + 4, head + 7, fur)
        c.rect(cx - fw - 4, head + 3, cx - fw - 1, chin - 2, fur)
        c.rect(cx + fw + 1, head + 3, cx + fw + 4, chin - 2, shade(fur, .8))
        for x in range(cx - fw - 3, cx + fw + 4, 2):
            c.px(x, head + 4, shade(fur, .72))
    elif hat == "kimeshek":
        c.ellipse(cx - fw - 5, head - 15, cx + fw + 5, head + 5, WHITE)
        for y in (head - 10, head - 5):
            c.line(cx - fw - 2, y + 2, cx + fw + 2, y - 2, shade(WHITE, .8))
        c.rect(cx + fw, head - 10, cx + fw + 5, head + 3, shade(WHITE, .88))
    if look.get("weapon") == "spear":
        c.rect(58, 0, 59, 64, WOOD)
        c.poly([(56, 8), (61, 8), (58, 0)], STEEL)
        c.rect(56, 9, 61, 12, look.get("plume", (170, 34, 40)))
    elif look.get("weapon") == "staff":
        c.rect(6, 6, 7, 64, WOOD)
        c.rect(6, 4, 10, 5, WOOD)
    return render(c.image, .5)
