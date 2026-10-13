#!/usr/bin/env python3
"""SANLAQ main-menu logo in the style of the original provided logo.

Letters: a bold serif typeface rasterised without smoothing on a coarse grid, then filled cream-to-gold with a
dithered gradient, a warm top highlight, a burgundy outline and a dark drop shadow. The diamond ornaments at the
sides and the underline are cut from the original logo so the house style carries over.

Usage: python3 Tools/ArtGen/logo.py [font path] [font index]   (needs Pillow; the default font ships with macOS)
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LOGO_DIR = os.path.join(ROOT, "Assets", "ATTILA", "Art", "Menu", "Logo")
SOURCE = os.path.join(LOGO_DIR, "ProvidedAttilaLogo.png")
OUTPUT = os.path.join(LOGO_DIR, "SanlaqLogo.png")

TEXT = "SANLAQ"
CELL = 13                     # screen pixels per logo pixel, matching the original's pixel size
FACE_TOP = (250, 222, 160)
FACE_BOTTOM = (206, 138, 66)
HIGHLIGHT = (255, 240, 200)
OUTLINE = (112, 22, 16)
SHADOW = (39, 4, 8)
MOTIF = (120, 24, 18)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def letter_mask(font_path, index, height):
    """Binary mask of the word at logo-pixel resolution: no smoothing, so the edges are true pixel steps."""
    size = height
    font = ImageFont.truetype(font_path, size, index=index)
    while True:
        box = font.getbbox("H")
        if box[3] - box[1] >= height or size > height * 3:
            break
        size += 1
        font = ImageFont.truetype(font_path, size, index=index)
    spacing = max(2, height // 9)
    widths = [font.getbbox(ch)[2] - font.getbbox(ch)[0] for ch in TEXT]
    width = sum(widths) + spacing * (len(TEXT) - 1) + 8
    image = Image.new("1", (width, height + 8), 0)
    draw = ImageDraw.Draw(image)
    draw.fontmode = "1"
    x = 4
    top = font.getbbox("H")[1]
    for ch, w in zip(TEXT, widths):
        left = font.getbbox(ch)[0]
        draw.text((x - left, 4 - top), ch, font=font, fill=1)
        x += w + spacing
    return image.crop(image.getbbox())


def grow(mask, steps):
    """Dilates a binary mask by whole logo pixels (a 3x3 max filter per step)."""
    out = mask.convert("L")
    for _ in range(steps):
        out = out.filter(ImageFilter.MaxFilter(3))
    return out.point(lambda v: 255 if v else 0)


def shrink(mask, steps):
    out = mask.convert("L")
    for _ in range(steps):
        out = out.filter(ImageFilter.MinFilter(3))
    return out.point(lambda v: 255 if v else 0)


def word(mask):
    """Paints the word at logo-pixel size; scaled up with hard edges afterwards."""
    pad = 4
    w, h = mask.size
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2 + 2), (0, 0, 0, 0))
    base = Image.new("L", canvas.size, 0)
    base.paste(mask.convert("L").point(lambda v: 255 if v else 0), (pad, pad))
    shadow = grow(base, 2)
    canvas.paste(SHADOW + (255,), (1, 2), shadow)
    canvas.paste(OUTLINE + (255,), (0, 0), grow(base, 2))
    canvas.paste((96, 14, 12, 255), (0, 0), grow(base, 1))
    face = base.load()
    px = canvas.load()
    for y in range(canvas.size[1]):
        t = (y - pad) / max(1, h - 1)
        for x in range(canvas.size[0]):
            if not face[x, y]:
                continue
            level = min(1.0, max(0.0, t)) * 4 + BAYER[y % 4][x % 4] / 16 - .5
            k = min(1.0, max(0.0, round(level) / 4))
            color = tuple(int(FACE_TOP[i] + (FACE_BOTTOM[i] - FACE_TOP[i]) * k) for i in range(3))
            if y > 0 and not face[x, y - 1]:
                color = HIGHLIGHT
            elif x + 1 < canvas.size[0] and not face[x + 1, y]:
                color = tuple(int(c * .82) for c in color)
            px[x, y] = color + (255,)
    # A small diamond motif wherever a stroke is thick enough to carry one, like the original's stem patterns.
    core = shrink(base, 3).load()
    used = []
    for x in range(canvas.size[0]):
        for y in range(pad + h // 3, pad + h - h // 4):
            if core[x, y] and all(abs(x - ux) > 8 for ux, _ in used):
                cy = y + 2
                if core[x, min(canvas.size[1] - 1, cy + 2)]:
                    for dx, dy in ((0, -2), (-1, -1), (1, -1), (-2, 0), (2, 0), (-1, 1), (1, 1), (0, 2)):
                        px[x + dx, cy + dy] = MOTIF + (255,)
                    px[x, cy] = HIGHLIGHT + (255,)
                    used.append((x, cy))
                break
    return canvas


def piece(source, box, keep=None):
    part = source.crop(box)
    if keep:
        clear = Image.new("RGBA", part.size, (0, 0, 0, 0))
        clear.paste(part.crop(keep), keep[:2])
        part = clear
    return part.crop(part.getbbox())


def build(font_path, index=0):
    source = Image.open(SOURCE).convert("RGBA")
    left = piece(source, (40, 250, 345, 500), (0, 0, 196, 250))
    right = piece(source, (1890, 250, 2150, 500), (66, 0, 260, 250))
    under = piece(source, (660, 520, 1540, 630), (0, 28, 880, 110))

    letters = word(letter_mask(font_path, index, 30))
    letters = letters.resize((letters.size[0] * CELL, letters.size[1] * CELL), Image.NEAREST)

    width, height = source.size
    logo = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    lw, lh = letters.size
    scale = min(1.0, (width - left.size[0] - right.size[0] - 40) / lw)
    if scale < 1.0:
        letters = letters.resize((int(lw * scale), int(lh * scale)), Image.NEAREST)
        lw, lh = letters.size
    lx, ly = (width - lw) // 2, 120 + (400 - lh) // 2
    logo.alpha_composite(letters, (lx, ly))
    mid = ly + lh // 2
    logo.alpha_composite(left, (max(0, lx - left.size[0] - 6), mid - left.size[1] // 2))
    logo.alpha_composite(right, (min(width - right.size[0], lx + lw + 6), mid - right.size[1] // 2))
    logo.alpha_composite(under, ((width - under.size[0]) // 2, min(height - under.size[1], ly + lh + 10)))
    return logo


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "/System/Library/Fonts/Supplemental/Georgia Bold.ttf"
    index = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    out = sys.argv[3] if len(sys.argv) > 3 else OUTPUT
    build(path, index).save(out)
    print("wrote", out)
