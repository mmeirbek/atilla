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
        private const int Throws = 3;
        private const int Needed = 2;
        private const float ZoneHalfWidth = .085f;
        private const float MarkerSpeed = .85f;

        private enum Phase { Aiming, Throwing, Resolving, Done }

        private Action won, lost;
        private GameObject root;
        private RectTransform markerRect, sakaRect;
        private readonly List<Image> pieces = new();
        private readonly List<Vector2> homes = new();
        private Text statusText;
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
            GameUiKit.Label(title.Content, "Сақаны дәл ату үшін жолақ ортасында БОС ОРЫН басыңыз.", 20, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 20, 58, 820, 32);

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
            statusText.rectTransform.anchoredPosition = new Vector2(0, 214);
            UpdateStatus();
        }

        private void UpdateStatus() => statusText.text = $"АТЫС: {thrown}/{Throws}          ҰТЫС: {hits}/{Needed}";

        private void Update()
        {
            if (root == null) return;
            float delta = Time.unscaledDeltaTime;
            switch (phase)
            {
                case Phase.Aiming:
                    marker += direction * MarkerSpeed * delta;
                    if (marker > 1f) { marker = 1f; direction = -1; }
                    else if (marker < -1f) { marker = -1f; direction = 1; }
                    markerRect.anchoredPosition = new Vector2(marker * 480f, 0f);
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) Throw();
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
            bool hit = Mathf.Abs(marker) <= ZoneHalfWidth * 2f;
            targetPiece = hit ? NearestPiece(marker * 300f) : null;
            sakaTarget = targetPiece != null
                ? targetPiece.rectTransform.anchoredPosition - new Vector2(0, 70)
                : new Vector2(marker * 420f, -60f);
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
            if (hits >= Needed) { Finish(true); return; }
            if (thrown >= Throws) { Finish(false); return; }
            phase = Phase.Aiming;
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
