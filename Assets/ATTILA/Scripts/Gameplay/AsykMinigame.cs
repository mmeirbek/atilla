using System;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Environment;
using ATTILA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// «Асық ату»: two presses per throw, first the line, then the strength. A dotted arc shows where the сақа
    /// will go, it flies in a spinning arc, and a hit knocks the asyk tumbling out of the ring.
    /// Five throws, three asyks out of the ring to win. Full-screen overlay, the ring seen from above.
    /// </summary>
    public sealed class AsykMinigame : MonoBehaviour
    {
        private const int Throws = 5;
        private const int Needed = 3;
        private const float ZoneHalfWidth = .1f;
        private const float MarkerSpeed = .95f;
        private const float SpeedPerThrow = .15f;
        private const float PowerSpeed = 1.35f;
        private const float PowerBand = .22f;
        private const float LineY = -20f;          // the line drawn across the ring in the background art
        private const float PieceSpread = 300f;    // how far the aim marker's ends reach along the line
        private const int ArcDots = 14;

        private enum Phase { Ready, Aiming, Power, Throwing, Resolving, Done }

        private Action won, lost;
        private GameObject root;
        private RectTransform rootRect, markerRect, sakaRect, sakaShadow, powerFill;
        private Image powerFillImage;
        private readonly List<Image> pieces = new();
        private readonly List<Vector2> homes = new();
        private readonly List<Image> arc = new();
        private readonly List<Image> throwIcons = new();
        private readonly List<Image> hitIcons = new();
        private Text stageText;
        private float power;
        private int powerDirection = 1;
        private float aim;
        private Phase phase;
        private float marker;
        private int direction = 1;
        private int thrown, hits;
        private float animation, time;
        private Image targetPiece, highlighted;
        private Vector2 targetHome, targetExit, sakaHome, sakaTarget;
        private float targetSpin;
        private string missLine;

        public void Begin(Action onWon, Action onLost)
        {
            won = onWon;
            lost = onLost;
            thrown = 0;
            hits = 0;
            marker = 0f;
            phase = Phase.Ready;
            Build();
            MinigameFx.Countdown(rootRect, () => { if (phase == Phase.Ready) { phase = Phase.Aiming; UpdateStatus(); } });
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            var art = MvpArtSet.Instance;
            var ui = StoryDirector.Active.Ui;
            root = new GameObject("Asyk Minigame", typeof(RectTransform));
            root.transform.SetParent(ui.OverlayRoot, false);
            rootRect = (RectTransform)root.transform;
            UiFactory.Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var background = UiFactory.Panel(root.transform, Color.white);
            background.sprite = art != null ? art.asykScene : null;
            UiFactory.Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = GameUiKit.Panel(root.transform, new Vector2(.5f, 1f), new Vector2(880, 110), new Vector2(0, -70),
                new Color(MenuUiKit.Burgundy.r, MenuUiKit.Burgundy.g, MenuUiKit.Burgundy.b, .96f), MenuUiKit.Gold);
            title.Border.raycastTarget = false;
            GameUiKit.Label(title.Content, "АСЫҚ АТУ", 34, MenuUiKit.Cream, TextAnchor.MiddleCenter, 20, 10, 820, 44);
            GameUiKit.Label(title.Content, "Екі рет бас: алдымен бағытты, сосын күшті дәл тап.", 20, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 20, 58, 820, 32);

            // Score: one сақа per throw left, one asyk per hit still needed.
            var tally = GameUiKit.InkPanel(root.transform, new Vector2(0f, 1f), new Vector2(380, 170), new Vector2(220, -110));
            tally.Border.raycastTarget = false;
            GameUiKit.Label(tally.Content, "АТЫС", 20, MenuUiKit.Gold, TextAnchor.UpperLeft, 18, 16, 120, 30);
            GameUiKit.Label(tally.Content, "ҰТЫС", 20, MenuUiKit.Gold, TextAnchor.UpperLeft, 18, 92, 120, 30);
            for (int i = 0; i < Throws; i++) throwIcons.Add(TallyIcon(tally.Content, art != null ? art.saka : null, 120 + i * 46, 6));
            for (int i = 0; i < Needed; i++) hitIcons.Add(TallyIcon(tally.Content, art != null ? art.asyk : null, 120 + i * 46, 82));

            for (int i = 0; i < 5; i++)
            {
                var home = new Vector2(-240 + i * 120, LineY);
                Shadow(home + new Vector2(0, -30), 70);
                var piece = UiFactory.Panel(root.transform, Color.white);
                piece.raycastTarget = false;
                piece.sprite = art != null ? art.asyk : null;
                UiFactory.SetRect(piece.rectTransform, new Vector2(.5f, .5f), new Vector2(96, 84), home);
                pieces.Add(piece);
                homes.Add(home);
            }

            for (int i = 0; i < ArcDots; i++)
            {
                var dot = UiFactory.Panel(root.transform, MenuUiKit.Cream);
                dot.raycastTarget = false;
                dot.rectTransform.sizeDelta = new Vector2(10, 10);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(.5f, .5f);
                arc.Add(dot);
            }

            sakaHome = new Vector2(0, -250);
            sakaShadow = Shadow(sakaHome + new Vector2(0, -34), 80).rectTransform;
            var saka = UiFactory.Panel(root.transform, Color.white);
            saka.raycastTarget = false;
            saka.sprite = art != null ? art.saka : null;
            UiFactory.SetRect(saka.rectTransform, new Vector2(.5f, .5f), new Vector2(112, 98), sakaHome);
            sakaRect = saka.rectTransform;

            var powerTrack = GameUiKit.Panel(root.transform, new Vector2(.5f, 0f), new Vector2(1000, 44), new Vector2(0, 208), MenuUiKit.Dark, MenuUiKit.Gold);
            var powerZone = UiFactory.Panel(powerTrack.Content, new Color(.42f, .68f, .34f, .95f));
            powerZone.raycastTarget = false;
            UiFactory.SetRect(powerZone.rectTransform, new Vector2(1f - PowerBand / 2f, .5f), new Vector2(1000 * PowerBand, 30), Vector2.zero);
            powerFillImage = UiFactory.Panel(powerTrack.Content, MenuUiKit.Gold);
            powerFillImage.raycastTarget = false;
            powerFill = powerFillImage.rectTransform;
            powerFill.anchorMin = powerFill.anchorMax = new Vector2(0f, .5f);
            powerFill.pivot = new Vector2(0f, .5f);
            powerFill.anchoredPosition = new Vector2(6, 0);
            powerFill.sizeDelta = new Vector2(0, 30);
            GameUiKit.Label(powerTrack.Rect, "КҮШ", 20, MenuUiKit.Gold, TextAnchor.MiddleRight, -120, 6, 100, 32);

            var bar = GameUiKit.Panel(root.transform, new Vector2(.5f, 0f), new Vector2(1000, 66), new Vector2(0, 130), MenuUiKit.Dark, MenuUiKit.Gold);
            var zone = UiFactory.Panel(bar.Content, new Color(.42f, .68f, .34f, .95f));
            zone.raycastTarget = false;
            UiFactory.SetRect(zone.rectTransform, new Vector2(.5f, .5f), new Vector2(1000 * ZoneHalfWidth * 2f, 46), Vector2.zero);
            var centre = UiFactory.Panel(bar.Content, MenuUiKit.Cream);
            centre.raycastTarget = false;
            UiFactory.SetRect(centre.rectTransform, new Vector2(.5f, .5f), new Vector2(4, 54), Vector2.zero);
            var markerImage = UiFactory.Panel(bar.Content, MenuUiKit.Cream);
            markerImage.raycastTarget = false;
            UiFactory.SetRect(markerImage.rectTransform, new Vector2(.5f, .5f), new Vector2(14, 66), Vector2.zero);
            markerRect = markerImage.rectTransform;
            GameUiKit.Label(bar.Rect, "БАҒЫТ", 20, MenuUiKit.Gold, TextAnchor.MiddleRight, -140, 16, 120, 32);

            stageText = GameUiKit.Label(root.transform, string.Empty, 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 0, 0, 1000, 40);
            stageText.rectTransform.anchorMin = stageText.rectTransform.anchorMax = new Vector2(.5f, 0f);
            stageText.rectTransform.pivot = new Vector2(.5f, .5f);
            stageText.rectTransform.anchoredPosition = new Vector2(0, 66);
            stageText.gameObject.AddComponent<Outline>().effectColor = new Color(.15f, .07f, .05f, 1f);
            UpdateStatus();
        }

        private static Image TallyIcon(Transform parent, Sprite sprite, float x, float y)
        {
            var icon = UiFactory.Panel(parent, Color.white);
            icon.raycastTarget = false;
            icon.sprite = sprite;
            GameUiKit.PlaceTopLeft(icon.rectTransform, x, y, 40, 36);
            return icon;
        }

        private Image Shadow(Vector2 position, float width)
        {
            var shadow = UiFactory.Panel(root.transform, new Color(0f, 0f, 0f, .28f));
            shadow.raycastTarget = false;
            shadow.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.shadow : null;
            UiFactory.SetRect(shadow.rectTransform, new Vector2(.5f, .5f), new Vector2(width, width * .32f), position);
            return shadow;
        }

        private void UpdateStatus()
        {
            for (int i = 0; i < throwIcons.Count; i++) throwIcons[i].color = i < Throws - thrown ? Color.white : new Color(1f, 1f, 1f, .18f);
            for (int i = 0; i < hitIcons.Count; i++) hitIcons[i].color = i < hits ? Color.white : new Color(.2f, .14f, .12f, .55f);
            stageText.text = phase switch
            {
                Phase.Ready => string.Empty,
                Phase.Aiming => "1 · БАҒЫТТЫ ТАҢДА — E / БОС ОРЫН",
                Phase.Power => "2 · КҮШТІ ТАҢДА — E / БОС ОРЫН",
                _ => string.Empty
            };
        }

        // ------------------------------------------------------------ play

        private static bool Pressed() => VirtualInput.Action() || Input.GetMouseButtonDown(0);

        private void Update()
        {
            if (root == null) return;
            float delta = Time.unscaledDeltaTime;
            time += delta;
            IdlePieces();
            switch (phase)
            {
                case Phase.Aiming:
                    marker += direction * (MarkerSpeed + thrown * SpeedPerThrow) * delta;
                    if (marker > 1f) { marker = 1f; direction = -1; }
                    else if (marker < -1f) { marker = -1f; direction = 1; }
                    markerRect.anchoredPosition = new Vector2(marker * 480f, 0f);
                    markerRect.GetComponent<Image>().color = InZone(marker) ? new Color(.62f, .9f, .5f) : MenuUiKit.Cream;
                    DrawArc(marker, 1f, InZone(marker));
                    if (Pressed())
                    {
                        aim = marker;
                        power = 0f;
                        powerDirection = 1;
                        phase = Phase.Power;
                        AudioService.Play(Sfx.Blip, .6f);
                        MinigameFx.Punch(markerRect, .5f);
                        UpdateStatus();
                    }
                    break;
                case Phase.Power:
                    power += powerDirection * PowerSpeed * delta;
                    if (power > 1f) { power = 1f; powerDirection = -1; }
                    else if (power < 0f) { power = 0f; powerDirection = 1; }
                    powerFill.sizeDelta = new Vector2(power * 988f, 30);
                    powerFillImage.color = Strong(power) ? new Color(.62f, .9f, .5f) : Color.Lerp(MenuUiKit.Gold, new Color(.9f, .4f, .25f), power);
                    DrawArc(aim, Mathf.Lerp(.35f, 1f, power), InZone(aim) && Strong(power));
                    if (Pressed()) Throw();
                    break;
                case Phase.Throwing:
                    animation += delta * 1.9f;
                    FlySaka(Mathf.Min(1f, animation));
                    if (animation >= 1f) Resolve();
                    break;
                case Phase.Resolving:
                    animation += delta * 1.5f;
                    if (targetPiece != null) Tumble(Mathf.Min(1f, animation));
                    else sakaRect.anchoredPosition = sakaTarget + new Vector2(0, Mathf.Abs(Mathf.Sin(animation * 9f)) * 26f * (1f - Mathf.Min(1f, animation)));
                    if (animation >= 1f) NextThrow();
                    break;
            }
        }

        private static bool InZone(float value) => Mathf.Abs(value) <= ZoneHalfWidth * 2f;
        private static bool Strong(float value) => value >= 1f - PowerBand;

        /// <summary>The asyks rock gently, and the one the line points at lifts and glows.</summary>
        private void IdlePieces()
        {
            Image wanted = phase is Phase.Aiming or Phase.Power ? NearestPiece((phase == Phase.Aiming ? marker : aim) * PieceSpread) : null;
            if (wanted != null && !InZone(phase == Phase.Aiming ? marker : aim)) wanted = null;
            highlighted = wanted;
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (!piece.enabled || piece == targetPiece) continue;
                bool lit = piece == highlighted;
                piece.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2.2f + i) * 4f);
                piece.rectTransform.localScale = Vector3.one * (lit ? 1.18f + Mathf.Sin(time * 8f) * .05f : 1f);
                piece.color = lit ? new Color(1f, .95f, .75f) : Color.white;
            }
        }

        /// <summary>Dotted flight path from the сақа toward the line; short when the throw is weak.</summary>
        private void DrawArc(float along, float reach, bool good)
        {
            var end = new Vector2(along * PieceSpread, LineY - 50f);
            var color = good ? new Color(.7f, .95f, .55f, .95f) : new Color(1f, .93f, .8f, .8f);
            for (int i = 0; i < arc.Count; i++)
            {
                float t = (i + 1f) / arc.Count * reach;
                var point = Vector2.Lerp(sakaHome, end, t) + new Vector2(0, Mathf.Sin(t * Mathf.PI) * 90f);
                arc[i].enabled = true;
                arc[i].rectTransform.anchoredPosition = point;
                float pulse = Mathf.Repeat(time * 2.5f - i / (float)arc.Count, 1f);
                arc[i].color = new Color(color.r, color.g, color.b, color.a * (.45f + .55f * pulse));
            }
        }

        private void HideArc()
        {
            foreach (var dot in arc) dot.enabled = false;
        }

        private void Throw()
        {
            phase = Phase.Throwing;
            animation = 0f;
            thrown++;
            HideArc();
            AudioService.Play(Sfx.Toss, .9f);
            // Both halves have to land: the line has to be right and the throw strong enough to reach it.
            bool aimed = InZone(aim);
            bool strong = Strong(power);
            targetPiece = aimed && strong ? NearestPiece(aim * PieceSpread) : null;
            sakaTarget = targetPiece != null
                ? targetPiece.rectTransform.anchoredPosition - new Vector2(0, 40)
                : new Vector2(aim * 420f, Mathf.Lerp(-210f, LineY - 10f, power));
            missLine = !aimed ? "БАҒЫТ ҚАТЕ" : !strong ? (power < 1f - PowerBand ? "КҮШ ЖЕТПЕДІ" : "ТЫМ ҚАТТЫ") : null;
            UpdateStatus();
        }

        private void FlySaka(float t)
        {
            var ground = Vector2.Lerp(sakaHome, sakaTarget, t);
            float height = Mathf.Sin(t * Mathf.PI) * 220f;
            sakaRect.anchoredPosition = ground + new Vector2(0, height);
            sakaRect.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * .45f);
            sakaRect.localRotation = Quaternion.Euler(0f, 0f, -t * 720f);
            sakaShadow.anchoredPosition = ground + new Vector2(0, -34);
            sakaShadow.localScale = Vector3.one * (1f - Mathf.Sin(t * Mathf.PI) * .45f);
        }

        private Image NearestPiece(float x)
        {
            Image best = null;
            float bestDistance = float.MaxValue;
            foreach (var piece in pieces)
            {
                if (!piece.enabled) continue;
                float distance = Mathf.Abs(piece.rectTransform.anchoredPosition.x - x);
                if (distance < bestDistance) { bestDistance = distance; best = piece; }
            }
            return best;
        }

        private void Resolve()
        {
            phase = Phase.Resolving;
            animation = 0f;
            sakaRect.localRotation = Quaternion.identity;
            sakaRect.localScale = Vector3.one;
            if (targetPiece != null)
            {
                hits++;
                AudioService.Play(Sfx.Hit, 1f, 1.3f);
                targetHome = targetPiece.rectTransform.anchoredPosition;
                float side = Mathf.Sign(targetHome.x + (UnityEngine.Random.value - .5f) * 40f + .01f);
                targetExit = targetHome + new Vector2(side * 640f, 300f);
                targetSpin = side * -900f;
                MinigameFx.Burst(rootRect, targetHome, MenuUiKit.Gold, 18, 620f);
                MinigameFx.Burst(rootRect, targetHome, new Color(.72f, .58f, .4f), 12, 380f);
                MinigameFx.Popup(rootRect, targetHome + new Vector2(0, 70), hits >= Needed ? "ЖЕҢІС!" : "ДӘЛ ТИДІ!", MenuUiKit.Gold, 56);
                MinigameFx.Shake(rootRect, 22f, .3f);
            }
            else
            {
                AudioService.Play(Sfx.Fail, .5f);
                MinigameFx.Burst(rootRect, sakaTarget, new Color(.72f, .58f, .4f), 10, 260f);
                MinigameFx.Popup(rootRect, sakaTarget + new Vector2(0, 80), missLine ?? "ҚАТЕ", MenuUiKit.Cream, 44);
                MinigameFx.Shake(rootRect, 8f, .2f);
            }
            UpdateStatus();
            if (hits > 0 && hits <= hitIcons.Count) MinigameFx.Punch(hitIcons[hits - 1].transform, .8f, .35f);
        }

        /// <summary>The struck asyk flies out of the ring end over end, rising and dropping like a real knock.</summary>
        private void Tumble(float t)
        {
            var path = Vector2.Lerp(targetHome, targetExit, t) + new Vector2(0, Mathf.Sin(t * Mathf.PI) * 160f);
            targetPiece.rectTransform.anchoredPosition = path;
            targetPiece.rectTransform.localRotation = Quaternion.Euler(0f, 0f, targetSpin * t);
            targetPiece.rectTransform.localScale = Vector3.one * (1.2f + Mathf.Sin(t * Mathf.PI) * .3f);
            targetPiece.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - .7f) / .3f));
        }

        private void NextThrow()
        {
            if (targetPiece != null)
            {
                targetPiece.enabled = false;
                targetPiece.rectTransform.anchoredPosition = new Vector2(4000, 0);
                targetPiece = null;
            }
            sakaRect.anchoredPosition = sakaHome;
            sakaShadow.anchoredPosition = sakaHome + new Vector2(0, -34);
            sakaShadow.localScale = Vector3.one;
            powerFill.sizeDelta = new Vector2(0, 30);
            if (hits >= Needed) { Finish(true); return; }
            if (Throws - thrown < Needed - hits) { Finish(false); return; }   // no way back: end early
            phase = Phase.Aiming;
            UpdateStatus();
        }

        private void Finish(bool success)
        {
            phase = Phase.Done;
            HideArc();
            UpdateStatus();
            MinigameFx.Result(rootRect, success, success ? "Үш асық шеңберден шықты!" : $"{hits} асық қана шықты", () =>
            {
                Destroy(root);
                root = null;
                pieces.Clear();
                homes.Clear();
                arc.Clear();
                throwIcons.Clear();
                hitIcons.Clear();
                if (success) won?.Invoke(); else lost?.Invoke();
            });
        }
    }
}
