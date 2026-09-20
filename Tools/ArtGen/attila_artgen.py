#!/usr/bin/env python3
"""Writes every generated ATTILA MVP art file into Assets/ATTILA/Art/Generated.

Usage: python3 Tools/ArtGen/attila_artgen.py   (needs Pillow)
Then run ATTILA > Art > Import Generated Art in Unity to apply import settings and rebuild the data assets.
Final art can replace any PNG at the same path; the importer keeps references intact.
"""
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
import characters  # noqa: E402
import environment as env  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "ATTILA", "Art", "Generated")
MENU = os.path.join(ROOT, "Assets", "ATTILA", "Art")
FONT = os.path.join(ROOT, "Assets", "ATTILA", "Resources", "UI", "PressStart2P-Regular.ttf")


def save(image, *parts):
    path = os.path.join(OUT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)
    return path


def main():
    for look in characters.LOOKS:
        for direction in characters.DIRECTIONS:
            save(characters.sprite(look, direction, 0), "Characters", look, f"{look}_idle_{direction}_00.png")
            save(characters.sprite(look, direction, 1), "Characters", look, f"{look}_walk_{direction}_00.png")
            save(characters.sprite(look, direction, -1), "Characters", look, f"{look}_walk_{direction}_01.png")
        save(characters.bust(look), "Portraits", f"{look}.png")

    save(env.ground_texture(), "Environment", "ground.png")
    save(env.path_texture(), "Environment", "path.png")
    save(env.yurt_wall_texture(), "Environment", "yurt_wall.png")
    save(env.yurt_roof_texture(), "Environment", "yurt_roof.png")
    save(env.door_texture(), "Environment", "yurt_door.png")
    save(env.banner((150, 30, 34), env.GOLD), "Props", "banner_red.png")
    save(env.banner((40, 64, 124), env.GOLD), "Props", "banner_blue.png")
    save(env.banner((96, 26, 30), (230, 220, 200)), "Props", "banner_dark.png")
    for frame in range(2):
        save(env.horse((140, 84, 48), (50, 34, 26), (150, 30, 34), frame), "Props", f"horse_bay_{frame}.png")
        save(env.horse((176, 170, 160), (90, 86, 84), (40, 64, 124), frame), "Props", f"horse_grey_{frame}.png")
    for i, frame in enumerate(env.campfire_frames()):
        save(frame, "Props", f"campfire_{i}.png")
    save(env.glow(), "Props", "glow.png")
    save(env.shadow_blob(), "Props", "shadow.png")
    save(env.quest_marker(), "Props", "marker.png")
    save(env.kazan(), "Props", "kazan.png")

    save(env.campfire_scene(), "Scenes", "campfire.png")
    save(env.asyk_scene(), "Scenes", "asyk_ring.png")
    save(env.asyk_piece(False), "Props", "asyk.png")
    save(env.asyk_piece(True), "Props", "saka.png")
    save(env.map_labels(env.map_scene(), FONT), "Scenes", "map.png")
    for name in env.ICONS:
        save(env.icon(name), "Icons", f"{name}.png")

    template = Image.open(os.path.join(MENU, "Characters", "Menu", "ProvidedKereyKhanCard.png")).convert("RGBA")
    khan = env.key_black(Image.open(os.path.join(MENU, "Characters", "Menu", "TemporaryKhan.png")))
    batyr = Image.open(os.path.join(MENU, "Menu", "Character", "TemporaryBatyr.png")).convert("RGBA")
    save(env.compose_card(template, None, False), "Cards", "card_kerey.png")
    save(env.compose_card(template, env.recolor_red_to_blue(khan), True), "Cards", "card_abylai.png")
    save(env.compose_card(template, batyr, True), "Cards", "card_kabanbay.png")
    print("ATTILA art written to", OUT)


if __name__ == "__main__":
    main()
