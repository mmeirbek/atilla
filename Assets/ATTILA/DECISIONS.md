# Stage 1 decisions

- The playable hero is `TEMP_HERO`. It has no invented historical name, era, clothing, or biography.
- The aul, yurts, NPCs, portrait, and world props are simple primitive placeholders. They intentionally make no claim of historical accuracy.
- No third-party asset is imported. This keeps the prototype runnable without a licence/login flow.
- Gameplay direction is stored in `PlayerVisual` as eight directions; its visual root can later be replaced with Blender-rendered directional sprites without changing movement code.


# Stage 1.5 decisions

- Authority: ATTILA_Game_Design_Spec_v2.docx supplied by the user, specifically visual direction and sprite pipeline; the user's Stage 1.5 limits supersede the document's broader MVP implementation instructions.
- Selected 40-degree orthographic view, size 10, .18-second follow smoothing; kept +Z as north and the existing world-axis movement.
- Chose a code-drawn neutral soft-pixel placeholder: eight idle views and two walk frames per direction, plain blue shirt and dark trousers, no cultural/historical claim. All art is NEEDS HISTORICAL REVIEW.
- Used existing URP with unlit flat materials and two roof colors, with cast shadows disabled. This deliberately avoids a custom renderer and realistic shading.
- Kept all yurt, tree, rock, fence, path and NPC locations. Replaced yurt dome visuals with a shallow twelve-sided cone; moved the previously detached door to the wall (the old -3.42 offset exceeded the base's 1.75 radius).
- Removed visible capsule meshes for player and existing NPC placeholders, sharing one sprite data set for now. Their CharacterControllers remain. Removed only the duplicate primitive character and ground colliders associated with their former visuals.
- Kept the existing menu and character-select UI unchanged, including its existing portrait placeholder. Did not rebuild scenes, change PlayerController or edit project settings as part of this stage.
- No decisions outside the approved Stage 1.5 scope; exact camera, sprite resolution, palette shades, frame rate and shared NPC placeholder art are implementation choices within that scope.


# Main Menu visual polish decisions

- Preserved only the two menu actions that already worked: Play to Character Select and Encyclopedia. Removed the displayed disabled wardrobe and settings buttons instead of inventing navigation or functionality.
- Used original generated temporary pixel-art scenery and character files inside `Assets/ATTILA/Art/Menu/`; no third-party menu assets were downloaded or added.
- Chose a warm steppe sunset, distant mountains, small yurt silhouettes, a centered temporary batyr and restrained burgundy/gold pixel-framed controls. The batyr is a visual placeholder rather than a historical costume reference.
- The temporary character source arrived against a black background. A local UI black-key material makes that backdrop transparent at display time; this is a presentation bridge until a future approved transparent character sprite replaces it.
- Used a small code-drawn block pixel wordmark for ATTILA because no project pixel font existed and no third-party font was approved. A future approved logo sprite or font can replace this without changing menu navigation.


# Stage 1.6 UI decisions

- Added presentation-only Wardrobe and Settings overlays, plus a static demo Encyclopedia layout, because the requested screens need navigation but must not add inventory, audio, save or database systems.
- The Main Menu Quit button calls Unity's safe `Application.Quit`; it does nothing in the Editor and quits a standalone build.
- Batyr remains the only playable selection. Khan is a temporary selectable-looking card only, with no hero data, gameplay path or progression attached.
- The named encyclopedia cards are short static demo labels requested for the screen; no biographies, historical explanations or data layer were added.


# Stage 1.6A Main Menu decisions

- Removed the central batyr from Main Menu only, leaving the temporary character art available to Character Select and Wardrobe.
- Generated a replaceable transparent pixel logo asset in the requested Menu/Logo folder instead of using a Unity font for ATTILA.
- Kept the existing steppe image intentionally subdued behind a burgundy readability veil; the empty center is deliberate so the logo and five compact controls remain dominant.
