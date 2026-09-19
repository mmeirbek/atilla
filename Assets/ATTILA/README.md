# ATTILA - Stage 1.5

Open `Assets/ATTILA/Scenes/Boot.unity` and enter Play Mode. The existing flow remains Boot -> MainMenu -> CharacterSelect -> AulPrototype. Use WASD to move. No scene rebuild or asset generation is needed; the generated PNGs, import settings, sprite data and material are included.

## Presentation

- Orthographic camera: X rotation 40 degrees, Y/Z 0; size 10; near/far .1/100.
- Camera follows the controller center, 24 units along the inverse camera forward axis: offset approximately (0, 15.427, -18.385). At spawn (0, 1, 0), camera starts at (0, 16.427, -18.385).
- Follow smoothing is .18 seconds, with immediate initial positioning. No perspective zoom or camera orbit.
- Existing aul positions, path, fence and four yurts remain. Unlit, matte color areas and two-tone shallow roofs replace sphere shading. Directional light intensity .55, flat ambient illumination, no fog, post-processing or cast shadows.
- All people in the aul use the same neutral temporary sprite set. PlayerController remains unchanged; CharacterController continues to handle ground and obstacles. Redundant primitive capsule colliders are no longer created for the people, and the redundant ground box collider was removed; the ground mesh collider remains.

## Character visual data

The unchanged controller continues to call `PlayerVisual.SetMovement(direction, moving)` with its movement vector. The visual component owns its child SpriteRenderer, fixed camera-facing rotation, facing state and frame playback. Its pivot sits at the controller feet. Idle retains the last movement direction rather than following the controller's smoothed yaw.

Direction order in `DirectionalSpriteSet` is N, NE, E, SE, S, SW, W, NW. +Z is N and +X is E, matching the fixed camera and existing controls. atan2(x, z), rounded into eight 45-degree sectors, chooses the direction. Each entry has one idle sprite and an optional walk array; missing walk frames fall back to idle. The temporary set uses two walk frames at 6 fps.

The included data is `Resources/Data/TemporaryDirections.asset`. Images are in `Art/Characters/RenderedSprites/Placeholder/`: 48 x 80 transparent PNGs, 30 pixels per unit, bottom-center pivot, point filtering, no mipmaps and no compression. They depict only a neutral shirt, trousers and shoes, with no historical identity or costume claim.

## Later Blender-rendered replacements

No Blender integration or scripts were added. Later, import consistently sized transparent PNG frames under `Art/Characters/RenderedSprites/`, use matching foot pivots and pixels per unit, and replace the references in the existing DirectionalSpriteSet asset. The arrays accept longer walk cycles. Adjust visualScale and framesPerSecond in the data if needed. Higher resolution art can use a proportionally higher pixels-per-unit value and bilinear filtering if desired. Movement, collision, camera and gameplay code need no changes. The serialized PlayerVisual spriteSet field also accepts another DirectionalSpriteSet for future prefabs.

`ATTILA/Generate Stage 1.5 Placeholder Sprites` is an optional editor-only regeneration command. It overwrites the TEMP_HERO placeholder PNGs and TemporaryDirections data; do not run it after replacing that data with final art. It does not rebuild scenes. `MvpSceneBuilder` was not changed or run.

## Validation

Unity 6000.6.0f1 compiled the project and ran the editor-only Stage15Validation scene-flow checks. They exercise Boot, Play, Back, Start, the enabled movement component, ground contact, controller displacement, camera follow/framing, yurt/NPC/rock/fence/tree collisions, all eight facings, idle persistence and both walk-frame references. Rendered menu, character-select and aul views were inspected. The runtime check reported no console errors. The pre-existing UiFactory obsolete-API warning remains.

The automated movement check drives CharacterController.Move; it does not inject physical keyboard events. Final manual check: open Boot, click Play and Start, use WASD and diagonal key pairs, release to confirm idle direction, and walk against a yurt. No setup steps are required.

To rerun automation, start Unity with `-batchmode -projectPath <project> -executeMethod ATTILA.Editor.Stage15Validation.Run -logFile <log>` (no `-quit` and no `-nographics`). It exits after checking; logs and QA PNGs are written under the ignored `Logs/Stage15/` directory. It never saves or rebuilds scenes or rewrites art data.

Stage 1.5 only. All character and environment art remains PLACEHOLDER / NEEDS HISTORICAL REVIEW. No Stage 2 systems were added.

## Main Menu visual polish

`MainMenuScreen` still owns only the existing navigation: **ОЙНАУ** loads Character Select and **ЭНЦИКЛОПЕДИЯ** opens the existing encyclopedia. The inactive wardrobe and settings placeholders were removed from this screen because they have no actions. Boot and scene routing are unchanged.

The menu is built at runtime beneath `Main Menu Canvas`, using a 1920 x 1080 Canvas Scaler reference and a .5 width/height match. Its upper center contains a temporary code-drawn pixel ATTILA wordmark and tagline; the center contains the temporary batyr foreground; lower center contains the two existing actions. This remains within the safe area at 1920x1080, 1600x900, and 1366x768.

Temporary original menu art is organized under `Art/Menu/`: `Background/TemporarySteppeSunset.png`, `Character/TemporaryBatyr.png`, and `UI/TemporaryBatyrBlackKey.mat`. Both textures use Sprite import, Point filtering, no mip maps, no compression and 1 PPU for crisp UI rendering. `Resources/UI/MainMenuVisuals.asset` holds the replaceable references. Future approved menu art only needs the same asset references changed; `MainMenuScreen` and button actions do not need to change. The generated batyr is a broad visual placeholder only and makes no claim of historical accuracy.

The menu palette is dark burgundy `#6D1D17`, secondary burgundy `#471310`, warm red `#8B2E24`, cream `#F7E6D1`, parchment `#F7F1E3`, gold `#D99A55`, and dark brown `#351713`. `MenuPixelButton` supplies the reusable border, ornament, hover, selection and press appearance. `MenuAmbientMotion` applies a small unscaled-time foreground drift only.

Run **ATTILA > Build Main Menu Visual Assets** after copying/replacing either temporary PNG so Unity re-applies the expected Sprite import settings and reconnects `MainMenuVisuals.asset`.

## Stage 1.6 menu and UI polish

The Main Menu now exposes Play, Wardrobe, Encyclopedia, Settings and Quit. Play remains the only route into Character Select and gameplay. Wardrobe, Settings and the additional encyclopedia cards are presentation-only overlays with Back buttons; they contain no inventory, audio, save or progression logic.

`MenuUiKit`, `MenuPixelButton` and `PixelWordmark` provide the shared 1920x1080 / Match .5 layout, palette, pixel borders, panels, cards, titles and back controls. Character Select now presents temporary Batyr and Khan cards; only Batyr uses the existing playable hero flow. The Khan selection does not create a new playable hero or branching content.

Temporary menu art also includes `Art/Characters/Menu/TemporaryKhan.png`. Wardrobe, encyclopedia and settings use code-drawn placeholder cards and symbols pending approved art and sourced content.

## Stage 1.6A final Main Menu polish

The Main Menu is intentionally character-free: the transparent `Art/Menu/Logo/TemporaryAttilaPixelLogo.png` is now the visual hero. It is generated by `MainMenuArtBuilder` from block pixels with cream/gold face, burgundy outline, dark-brown shadow and highlight, then imported as a Point-filtered Sprite. The centered layout is logo, subtitle, then compact vertical actions: ОЙНАУ, ГАРДЕРОБ, ЭНЦИКЛОПЕДИЯ, БАПТАУЛАР, ШЫҒУ.

All actions preserve their Stage 1.6 routes. Quit logs safely in the Unity Editor and calls `Application.Quit` only in a build. Internal screens retain their existing visible АРТҚА buttons.
