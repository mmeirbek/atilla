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
    /// «Асық ату»: a timing bar stands in for aiming the сақа. Three throws, two asyks out of the ring to win.
    /// Full-screen overlay so the ring is seen from above, the way the players squat around it.
    /// </summary>
    public sealed class AsykMinigame : MonoBehaviour
    {
        private const int Throws = 4;
        private const int Needed = 3;
        private const float ZoneHalfWidth = .085f;
        private const float MarkerSpeed = .95f;
        private const float SpeedPerThrow = .22f;
        private const float PowerSpeed = 1.35f;
        private const float PowerBand = .22f;

        private enum Phase { Aiming, Power, Throwing, Resolving, Done }

        private Action won, lost;
        private GameObject root;
        private RectTransform markerRect, sakaRect;
        private readonly List<Image> pieces = new();
        private readonly List<Vector2> homes = new();
        private Text statusText, stageText;
        private RectTransform powerFill;
        private float power;
        private int powerDirection = 1;
        private float aim;
        private Phase phase;
        private float marker;
        private int direction = 1;
        private int thrown, hits;
        private float animation;
        private Image targetPiece;
        private Vector2 targetHome, targetExit, sakaHome, sakaTarget;

        public void Begin(Action onWon, Action onLost)
        {
            won = onWon;
            lost = onLost;
            thrown = 0;
            hits = 0;
            marker = 0f;
            phase = Phase.Aiming;
            Build();
        }

        private void Build()
        {
            var art = MvpArtSet.Instance;
            var ui = StoryDirector.Active.Ui;
            root = new GameObject("Asyk Minigame", typeof(RectTransform));
            root.transform.SetParent(ui.OverlayRoot, false);
            UiFactory.Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var background = UiFactory.Panel(root.transform, Color.white);
            background.sprite = art != null ? art.asykScene : null;
            UiFactory.Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = GameUiKit.Panel(root.transform, new Vector2(.5f, 1f), new Vector2(880, 110), new Vector2(0, -70),
                new Color(MenuUiKit.Burgundy.r, MenuUiKit.Burgundy.g, MenuUiKit.Burgundy.b, .96f), MenuUiKit.Gold);
            title.Border.raycastTarget = false;
            GameUiKit.Label(title.Content, "АСЫҚ АТУ", 34, MenuUiKit.Cream, TextAnchor.MiddleCenter, 20, 10, 820, 44);
            GameUiKit.Label(title.Content, "Екі рет бас: алдымен бағытты, сосын күшті дәл тап.", 20, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 20, 58, 820, 32);

            for (int i = 0; i < 5; i++)
            {
                var piece = UiFactory.Panel(root.transform, Color.white);
                piece.raycastTarget = false;
                piece.sprite = art != null ? art.asyk : null;
                var home = new Vector2(-260 + i * 130, 60);
                UiFactory.SetRect(piece.rectTransform, new Vector2(.5f, .5f), new Vector2(96, 84), home);
                pieces.Add(piece);
                homes.Add(home);
            }
            var saka = UiFactory.Panel(root.transform, Color.white);
            saka.raycastTarget = false;
            saka.sprite = art != null ? art.saka : null;
            sakaHome = new Vector2(0, -250);
            UiFactory.SetRect(saka.rectTransform, new Vector2(.5f, .5f), new Vector2(112, 98), sakaHome);
            sakaRect = saka.rectTransform;

            var powerTrack = GameUiKit.Panel(root.transform, new Vector2(.5f, 0f), new Vector2(1000, 44), new Vector2(0, 208), MenuUiKit.Dark, MenuUiKit.Gold);
            var powerZone = UiFactory.Panel(powerTrack.Content, new Color(.42f, .68f, .34f, .95f));
            powerZone.raycastTarget = false;
            UiFactory.SetRect(powerZone.rectTransform, new Vector2(1f - PowerBand / 2f, .5f), new Vector2(1000 * PowerBand, 30), Vector2.zero);
            var fill = UiFactory.Panel(powerTrack.Content, MenuUiKit.Gold);
            fill.raycastTarget = false;
            fill.rectTransform.anchorMin = new Vector2(0f, .5f);
            fill.rectTransform.anchorMax = new Vector2(0f, .5f);
            fill.rectTransform.pivot = new Vector2(0f, .5f);
            fill.rectTransform.anchoredPosition = new Vector2(-494, 0);
            fill.rectTransform.sizeDelta = new Vector2(0, 30);
            powerFill = fill.rectTransform;

            var bar = GameUiKit.Panel(root.transform, new Vector2(.5f, 0f), new Vector2(1000, 66), new Vector2(0, 130), MenuUiKit.Dark, MenuUiKit.Gold);
            var zone = UiFactory.Panel(bar.Content, new Color(.42f, .68f, .34f, .95f));
            zone.raycastTarget = false;
            UiFactory.SetRect(zone.rectTransform, new Vector2(.5f, .5f), new Vector2(1000 * ZoneHalfWidth * 2f, 46), Vector2.zero);
            var centre = UiFactory.Panel(bar.Content, MenuUiKit.Cream);
            centre.raycastTarget = false;
            UiFactory.SetRect(centre.rectTransform, new Vector2(.5f, .5f), new Vector2(4, 54), Vector2.zero);
            var markerImage = UiFactory.Panel(bar.Content, MenuUiKit.Gold);
            markerImage.raycastTarget = false;
            UiFactory.SetRect(markerImage.rectTransform, new Vector2(.5f, .5f), new Vector2(12, 62), Vector2.zero);
            markerRect = markerImage.rectTransform;

            statusText = GameUiKit.Label(root.transform, string.Empty, 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 0, 0, 900, 40);
            statusText.rectTransform.anchorMin = statusText.rectTransform.anchorMax = new Vector2(.5f, 0f);
            statusText.rectTransform.anchoredPosition = new Vector2(0, 268);
            stageText = GameUiKit.Label(root.transform, string.Empty, 22, MenuUiKit.Gold, TextAnchor.MiddleCenter, 0, 0, 900, 34);
            stageText.rectTransform.anchorMin = stageText.rectTransform.anchorMax = new Vector2(.5f, 0f);
            stageText.rectTransform.anchoredPosition = new Vector2(0, 90);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            statusText.text = $"АТЫС: {thrown}/{Throws}          ҰТЫС: {hits}/{Needed}";
            if (stageText != null) stageText.text = phase == Phase.Aiming ? "1. БАҒЫТ — БОС ОРЫН" : phase == Phase.Power ? "2. КҮШ — БОС ОРЫН" : string.Empty;
        }

        private static bool Pressed() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);

        private void Update()
        {
            if (root == null) return;
            float delta = Time.unscaledDeltaTime;
            switch (phase)
            {
                case Phase.Aiming:
                    marker += direction * (MarkerSpeed + thrown * SpeedPerThrow) * delta;
                    if (marker > 1f) { marker = 1f; direction = -1; }
                    else if (marker < -1f) { marker = -1f; direction = 1; }
                    markerRect.anchoredPosition = new Vector2(marker * 480f, 0f);
                    if (Pressed())
                    {
                        aim = marker;
                        power = 0f;
                        powerDirection = 1;
                        phase = Phase.Power;
                        AudioService.Play(Sfx.Blip, .6f);
                        UpdateStatus();
                    }
                    break;
                case Phase.Power:
                    power += powerDirection * PowerSpeed * delta;
                    if (power > 1f) { power = 1f; powerDirection = -1; }
                    else if (power < 0f) { power = 0f; powerDirection = 1; }
                    powerFill.sizeDelta = new Vector2(power * 988f, 30);
                    if (Pressed()) Throw();
                    break;
                case Phase.Throwing:
                    animation += delta * 2.6f;
                    sakaRect.anchoredPosition = Vector2.Lerp(sakaHome, sakaTarget, Mathf.Min(1f, animation));
                    if (animation >= 1f) Resolve();
                    break;
                case Phase.Resolving:
                    animation += delta * 1.8f;
                    if (targetPiece != null)
                        targetPiece.rectTransform.anchoredPosition = Vector2.Lerp(targetHome, targetExit, Mathf.Min(1f, animation));
                    if (animation >= 1f) NextThrow();
                    break;
            }
        }

        private void Throw()
        {
            phase = Phase.Throwing;
            animation = 0f;
            thrown++;
            AudioService.Play(Sfx.Toss, .9f);
            // Both halves have to land: the line has to be right and the throw strong enough to reach it.
            bool aimed = Mathf.Abs(aim) <= ZoneHalfWidth * 2f;
            bool strong = power >= 1f - PowerBand;
            targetPiece = aimed && strong ? NearestPiece(aim * 300f) : null;
            sakaTarget = targetPiece != null
                ? targetPiece.rectTransform.anchoredPosition - new Vector2(0, 70)
                : new Vector2(aim * 420f, Mathf.Lerp(-210f, -40f, power));
            if (aimed && !strong) statusText.text = "КҮШ ЖЕТПЕДІ";
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
            if (targetPiece != null)
            {
                hits++;
                AudioService.Play(Sfx.Hit, .8f, 1.3f);
                targetHome = targetPiece.rectTransform.anchoredPosition;
                targetExit = targetHome + new Vector2(Mathf.Sign(targetHome.x + .01f) * 520f, 240f);
            }
            else AudioService.Play(Sfx.Fail, .5f);
            UpdateStatus();
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
            powerFill.sizeDelta = new Vector2(0, 30);
            if (hits >= Needed) { Finish(true); return; }
            if (thrown >= Throws) { Finish(false); return; }
            phase = Phase.Aiming;
            UpdateStatus();
        }

        private void Finish(bool success)
        {
            phase = Phase.Done;
            Destroy(root);
            root = null;
            pieces.Clear();
            homes.Clear();
            if (success) won?.Invoke(); else lost?.Invoke();
        }
    }
}
