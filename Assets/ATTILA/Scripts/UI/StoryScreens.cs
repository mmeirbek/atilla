using System;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Gameplay;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Night at the fire: the elder answers the topics the player picks, and each answer can open an entry.</summary>
    public sealed class CampfireScreen : MonoBehaviour
    {
        private static readonly Vector2 FireAnchor = new(.5f, .444f);
        private StoryDirector director;
        private CampfireDefinition definition;
        private Action done;
        private Image flame;
        private readonly List<Button> topicButtons = new();
        private readonly List<Image> embers = new();
        private readonly List<float> emberSeeds = new();
        private CanvasGroup buttonGroup;
        private Button leaveButton;
        private int heard;
        private float time;

        public static void Show(Transform host, StoryDirector director, CampfireDefinition definition, Action done)
        {
            var go = new GameObject("Campfire Screen", typeof(RectTransform));
            go.transform.SetParent(host, false);
            UiFactory.Stretch((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = go.AddComponent<CampfireScreen>();
            screen.director = director;
            screen.definition = definition;
            screen.done = done;
            screen.Build();
        }

        private void Build()
        {
            var art = MvpArtSet.Instance;
            var background = UiFactory.Panel(transform, Color.white);
            background.sprite = art != null ? art.campfireScene : null;
            background.type = Image.Type.Simple;
            UiFactory.Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            flame = UiFactory.Panel(transform, Color.white);
            flame.raycastTarget = false;
            flame.sprite = art != null && art.campfire.Length > 0 ? art.campfire[0] : null;
            UiFactory.SetRect(flame.rectTransform, FireAnchor, new Vector2(128, 144), new Vector2(0, 40));
            var glow = UiFactory.Panel(transform, new Color(1f, .72f, .34f, .5f));
            glow.raycastTarget = false;
            glow.sprite = art != null ? art.glow : null;
            UiFactory.SetRect(glow.rectTransform, FireAnchor, new Vector2(760, 380), Vector2.zero);
            glow.transform.SetSiblingIndex(flame.transform.GetSiblingIndex());
            for (int i = 0; i < 8; i++)
            {
                var ember = UiFactory.Panel(transform, new Color(1f, .74f, .38f, .9f));
                ember.raycastTarget = false;
                UiFactory.SetRect(ember.rectTransform, FireAnchor, new Vector2(6, 6), Vector2.zero);
                embers.Add(ember);
                emberSeeds.Add(UnityEngine.Random.value * 10f);
            }

            var title = GameUiKit.Panel(transform, new Vector2(.5f, 1f), new Vector2(900, 96), new Vector2(0, -80),
                new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .92f), MenuUiKit.Gold);
            title.Border.raycastTarget = false;
            GameUiKit.Label(title.Content, definition.title, 34, MenuUiKit.Gold, TextAnchor.MiddleCenter, 20, 20, 840, 48);

            // Rows are laid out in pixels from the top, so any number of topics stacks above the leave button.
            const float row = 100f, top = 86f;
            float height = top + definition.topics.Count * row + 150f;
            var panel = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(640, height), new Vector2(-360, 20));
            buttonGroup = panel.GameObject.AddComponent<CanvasGroup>();
            GameUiKit.Label(panel.Content, "НЕ ТУРАЛЫ СҰРАЙСЫҢ?", 24, MenuUiKit.Gold, TextAnchor.UpperCenter, 30, 28, 560, 34);
            for (int i = 0; i < definition.topics.Count; i++)
            {
                var topic = definition.topics[i];
                var button = MenuPixelButton.Create(panel.Content, topic.label, () => Ask(topic), 34);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
                rect.sizeDelta = new Vector2(580, 88);
                rect.anchoredPosition = new Vector2(0, -(top + i * row));
                topicButtons.Add(button);
            }
            leaveButton = MenuPixelButton.Create(panel.Content, "ҰЙЫҚТАУҒА", Leave);
            var leave = leaveButton.GetComponent<RectTransform>();
            leave.anchorMin = leave.anchorMax = leave.pivot = new Vector2(.5f, 0f);
            leave.sizeDelta = new Vector2(500, 100);
            leave.anchoredPosition = new Vector2(0, 26);
            leaveButton.interactable = false;
            AudioService.PlayMusic(MusicTrack.Night);
        }

        private void Ask(CampfireTopic topic)
        {
            if (director.Ui.DialogueActive) return;
            heard++;
            leaveButton.interactable = true;
            director.Ui.ShowDialogue(director.Content.Dialogue(topic.dialogueId), () =>
            {
                if (SaveService.Unlock(topic.unlockEntryId))
                    director.Ui.ShowToast("ЭНЦИКЛОПЕДИЯ: " + (director.Content.Entry(topic.unlockEntryId)?.title ?? topic.unlockEntryId));
            });
        }

        private void Leave()
        {
            if (director.Ui.DialogueActive) return;
            AudioService.Play(Sfx.Click);
            var callback = done;
            done = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            var art = MvpArtSet.Instance;
            if (art != null && art.campfire.Length > 0)
                flame.sprite = art.campfire[Mathf.FloorToInt(time * 9f) % art.campfire.Length];
            for (int i = 0; i < embers.Count; i++)
            {
                float t = Mathf.Repeat(time * .45f + emberSeeds[i], 1f);
                var rect = embers[i].rectTransform;
                rect.anchoredPosition = new Vector2(Mathf.Sin((t + emberSeeds[i]) * 7f) * 60f, 40f + t * 420f);
                embers[i].color = new Color(1f, .74f, .38f, Mathf.Clamp01(1f - t) * .85f);
            }
            bool talking = director.Ui.DialogueActive;
            buttonGroup.alpha = talking ? .25f : 1f;
            buttonGroup.interactable = !talking;
            buttonGroup.blocksRaycasts = !talking;
        }
    }

    /// <summary>Parchment map: the chapter's route draws itself, then the player rides on.</summary>
    public sealed class MapScreen : MonoBehaviour
    {
        private const float DrawSeconds = 2.4f;
        /// <summary>Centre of the empty legend frame drawn in the map's bottom-left corner, in 0..1 map space.</summary>
        private static readonly Vector2 LegendBox = new(.172f, .105f);
        /// <summary>Names already lettered on the map; a marker for one of these needs no extra plate.</summary>
        private static readonly string[] DrawnNames = { "Сарыарқа", "Түркістан", "Аңырақай", "Сырдария", "Қозыбасы", "Жетісу", "Шу", "Ұлытау", "Балқаш", "Қаратау", "Тараз", "Отырар", "Сауран", "Сығанақ" };
        private MapDefinition definition;
        private Action done;
        private RectTransform mapRect;
        private readonly List<Image> dots = new();
        private Image rider;
        private float time;

        public static void Show(Transform host, MapDefinition definition, Action done)
        {
            var go = new GameObject("Map Screen", typeof(RectTransform));
            go.transform.SetParent(host, false);
            UiFactory.Stretch((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = go.AddComponent<MapScreen>();
            screen.definition = definition;
            screen.done = done;
            screen.Build();
        }

        private void Build()
        {
            var art = MvpArtSet.Instance;
            var veil = UiFactory.Panel(transform, new Color(.08f, .05f, .04f, .96f));
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var map = UiFactory.Panel(transform, Color.white);
            map.sprite = art != null ? art.mapScene : null;
            map.raycastTarget = false;
            UiFactory.SetRect(map.rectTransform, new Vector2(.5f, .5f), new Vector2(1760, 990), new Vector2(0, 20));
            mapRect = map.rectTransform;

            // The title sits in the map's own empty legend box (bottom left), so no drawn place name is covered.
            var title = GameUiKit.Panel(mapRect, new Vector2(.5f, .5f), new Vector2(488, 116), Vector2.zero,
                new Color(MenuUiKit.Burgundy.r, MenuUiKit.Burgundy.g, MenuUiKit.Burgundy.b, .96f), MenuUiKit.Gold, 4f);
            title.Border.raycastTarget = false;
            Place(title.Rect, LegendBox);
            GameUiKit.Label(title.Content, definition.title, 34, MenuUiKit.Cream, TextAnchor.MiddleCenter, 10, 14, 460, 40);
            GameUiKit.Label(title.Content, definition.goal, 22, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 10, 64, 460, 32);

            var points = definition.route;
            if (points != null && points.Length > 1)
            {
                for (int i = 0; i < points.Length - 1; i++)
                {
                    int steps = Mathf.Max(3, Mathf.RoundToInt(Vector2.Distance(points[i], points[i + 1]) * 40f));
                    for (int s = 0; s < steps; s++)
                        Dot(Vector2.Lerp(points[i], points[i + 1], s / (float)steps), 10f, MenuUiKit.Burgundy);
                }
                Marker(points[0], definition.startLabel, MenuUiKit.Dark);
                Marker(points[points.Length - 1], definition.endLabel, MenuUiKit.Burgundy);
                rider = UiFactory.Panel(mapRect, Color.white);
                rider.raycastTarget = false;
                rider.sprite = art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null;
                rider.rectTransform.sizeDelta = new Vector2(96, 72);
                Place(rider.rectTransform, points[0]);
            }

            AudioService.PlayMusic(MusicTrack.Map);
            var go = MenuPixelButton.Create(transform, "ЖОЛҒА ШЫҒУ", Continue);
            UiFactory.SetRect(go.GetComponent<RectTransform>(), new Vector2(.5f, .06f), new Vector2(600, 112), Vector2.zero);
        }

        private void Place(RectTransform rect, Vector2 normalized)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((normalized.x - .5f) * mapRect.sizeDelta.x, (normalized.y - .5f) * mapRect.sizeDelta.y);
        }

        private void Dot(Vector2 normalized, float size, Color color)
        {
            var dot = UiFactory.Panel(mapRect, color);
            dot.raycastTarget = false;
            dot.rectTransform.sizeDelta = new Vector2(size, size);
            Place(dot.rectTransform, normalized);
            dot.enabled = false;
            dots.Add(dot);
        }

        private void Marker(Vector2 normalized, string label, Color color)
        {
            var diamond = UiFactory.Diamond(mapRect, 26f, color);
            Place(diamond.rectTransform, normalized);
            foreach (var name in DrawnNames)
                if (label.Contains(name)) return;
            var plate = GameUiKit.Panel(mapRect, new Vector2(.5f, .5f), new Vector2(Mathf.Max(220, label.Length * 20 + 40), 52), Vector2.zero,
                new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .9f), MenuUiKit.Gold, 3f);
            plate.Border.raycastTarget = false;
            Place(plate.Rect, normalized);
            plate.Rect.anchoredPosition += new Vector2(0, -46);
            GameUiKit.Label(plate.Content, label, 20, MenuUiKit.Cream, TextAnchor.MiddleCenter, 8, 8, plate.Rect.sizeDelta.x - 16, 30);
        }

        private void Continue()
        {
            AudioService.Play(Sfx.Click);
            var callback = done;
            done = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / DrawSeconds);
            int visible = Mathf.RoundToInt(progress * dots.Count);
            for (int i = 0; i < dots.Count; i++)
            {
                bool show = i < visible;
                if (dots[i].enabled != show)
                {
                    dots[i].enabled = show;
                    if (show && i % 6 == 0) AudioService.Play(Sfx.Blip, .25f, 1.6f);
                }
            }
            if (rider != null && dots.Count > 0)
            {
                int index = Mathf.Clamp(visible - 1, 0, dots.Count - 1);
                rider.rectTransform.anchoredPosition = dots[index].rectTransform.anchoredPosition + new Vector2(0, 34);
                var art = MvpArtSet.Instance;
                if (art != null && art.horseBay.Length > 1) rider.sprite = art.horseBay[Mathf.FloorToInt(time * 6f) % 2];
            }
        }
    }
}
