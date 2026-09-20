using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Characters;
using ATTILA.Core;
using ATTILA.Data;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Carousel of the three playable roles. Each card carries its own chapter and its own progress.</summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        private static readonly Vector2 CardSize = new(520, 780);
        private static readonly Vector2 Centre = new(0, -20);
        private static readonly Vector2 Left = new(-640, -20);
        private static readonly Vector2 Right = new(640, -20);

        private readonly List<Card> cards = new();
        private int selected;

        private void Start()
        {
            var canvas = MenuUiKit.Canvas("Character Select Canvas");
            MenuUiKit.Background(canvas.transform, .74f);
            MenuUiKit.Header(canvas.transform, "КЕЙІПКЕРДІ ТАҢДА", () => SceneLoader.Load(SceneId.MainMenu));
            var content = Resources.Load<Stage2Content>("Data/Stage2Content");
            for (int i = 0; i < HeroCatalog.Order.Length; i++)
            {
                var hero = HeroCatalog.Get(HeroCatalog.Order[i]);
                if (hero == null) continue;
                cards.Add(new Card(canvas.transform, hero, content, i, this));
            }
            var hint = GameUiKit.Label(canvas.transform, "← →  ТАҢДАУ        ENTER  БАСТАУ", 20, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 0, 0, 900, 34);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(.5f, .045f);
            hint.rectTransform.anchoredPosition = Vector2.zero;
            AudioService.PlayMusic(MusicTrack.Menu);
            Refresh(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Shift(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Shift(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Choose(selected);
            foreach (var card in cards) card.Animate();
        }

        private void Shift(int direction)
        {
            if (cards.Count == 0) return;
            selected = (selected + direction + cards.Count) % cards.Count;
            AudioService.Play(Sfx.Hover, .7f);
            Refresh(false);
        }

        private void Select(int index)
        {
            if (index == selected) { Choose(index); return; }
            selected = index;
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int relative = (i - selected + cards.Count) % cards.Count;
                if (relative == cards.Count - 1) relative = -1;
                cards[i].SetState(relative, immediate);
            }
        }

        private void Choose(int index)
        {
            var hero = cards[index].Hero;
            if (SaveService.Progress(hero.HeroId).completed) SaveService.RestartRole(hero.HeroId);
            SaveService.SelectHero(hero.HeroId);
            AudioService.Play(Sfx.Click);
            SceneLoader.Load(SceneId.AulPrototype);
        }

        private sealed class Card
        {
            private readonly RectTransform rect;
            private readonly CanvasGroup group;
            private readonly GameObject action;
            private readonly Image border;
            private Vector2 targetPosition;
            private Vector3 targetScale = Vector3.one, baseScale = Vector3.one;
            private float targetAlpha = 1f;
            private bool hovering, pressed;

            public HeroDefinition Hero { get; }

            public Card(Transform root, HeroDefinition hero, Stage2Content content, int index, CharacterSelectScreen screen)
            {
                Hero = hero;
                var card = UiFactory.Panel(root, Color.white);
                card.name = hero.DisplayName + " card";
                card.sprite = hero.Portrait;
                card.preserveAspect = true;
                UiFactory.SetRect(card.rectTransform, new Vector2(.5f, .5f), CardSize, Centre);
                rect = card.rectTransform;
                border = card;
                group = card.gameObject.AddComponent<CanvasGroup>();

                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => screen.Select(index));

                var progress = SaveService.Progress(hero.HeroId);
                var chapter = content != null ? content.Chapter(hero.ChapterId) : null;
                int beats = chapter != null ? chapter.beats.Count : 0;
                int percent = progress.completed ? 100 : beats > 0 ? Mathf.RoundToInt(100f * progress.beatIndex / beats) : 0;

                Label(card.transform, hero.DisplayName, 40, MenuUiKit.Gold, 234, 52);
                Label(card.transform, hero.Era, 22, MenuUiKit.Cream, 196, 34);
                Label(card.transform, hero.Tagline, 20, MenuUiKit.Parchment, 166, 30);
                var track = UiFactory.Panel(card.transform, MenuUiKit.Dark);
                track.raycastTarget = false;
                UiFactory.SetRect(track.rectTransform, new Vector2(.5f, 0f), new Vector2(360, 16), new Vector2(0, 140));
                var fill = UiFactory.Panel(track.transform, MenuUiKit.Gold);
                fill.raycastTarget = false;
                UiFactory.Stretch(fill.rectTransform, Vector2.zero, new Vector2(percent / 100f, 1f), new Vector2(2, 2), new Vector2(-2, -2));
                Label(card.transform, percent > 0 ? $"ПРОГРЕСС {percent}%" : "ЖАҢА ТАРАУ", 20, MenuUiKit.Parchment, 112, 28);

                string label = progress.completed ? "ҚАЙТА ОЙНАУ" : progress.beatIndex > 0 ? "ЖАЛҒАСТЫРУ" : "БАСТАУ";
                var play = MenuPixelButton.Create(card.transform, label, () => screen.Select(index));
                UiFactory.SetRect(play.GetComponent<RectTransform>(), new Vector2(.5f, 0f), new Vector2(380, 66), new Vector2(0, 62));
                action = play.gameObject;

                var trigger = card.gameObject.AddComponent<EventTrigger>();
                trigger.triggers = new List<EventTrigger.Entry>();
                Add(trigger, EventTriggerType.PointerEnter, () => { hovering = true; Apply(); });
                Add(trigger, EventTriggerType.PointerExit, () => { hovering = false; pressed = false; Apply(); });
                Add(trigger, EventTriggerType.PointerDown, () => { pressed = true; Apply(); });
                Add(trigger, EventTriggerType.PointerUp, () => { pressed = false; Apply(); });
            }

            private static void Label(Transform parent, string value, int size, Color color, float y, float height)
            {
                var text = UiFactory.Text(parent, value, size, TextAnchor.MiddleCenter, color);
                text.raycastTarget = false;
                UiFactory.SetRect(text.rectTransform, new Vector2(.5f, 0f), new Vector2(460, height), new Vector2(0, y));
            }

            private static void Add(EventTrigger trigger, EventTriggerType type, System.Action action)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => action());
                trigger.triggers.Add(entry);
            }

            public void SetState(int relative, bool immediate)
            {
                bool centre = relative == 0;
                targetPosition = centre ? Centre : relative < 0 ? Left : Right;
                baseScale = centre ? Vector3.one : new Vector3(.74f, .74f, 1f);
                targetAlpha = centre ? 1f : .34f;
                action.SetActive(centre);
                border.color = centre ? Color.white : new Color(.78f, .74f, .72f);
                Apply();
                if (!immediate) return;
                rect.anchoredPosition = targetPosition;
                rect.localScale = targetScale;
                group.alpha = targetAlpha;
            }

            private void Apply() => targetScale = baseScale * (pressed ? .96f : hovering ? 1.04f : 1f);

            public void Animate()
            {
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.unscaledDeltaTime * 11f);
                rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 11f);
                group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.unscaledDeltaTime * 11f);
            }
        }
    }
}
