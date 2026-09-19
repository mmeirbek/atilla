using ATTILA.Core;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Presentation carousel that preserves the existing TEMP_HERO gameplay route.</summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        private static readonly Vector2 CardSize = new Vector2(520, 610);
        private static readonly Vector2 CenterPosition = new Vector2(0, -10);
        private static readonly Vector2 LeftPosition = new Vector2(-570, -10);
        private static readonly Vector2 RightPosition = new Vector2(570, -10);

        private readonly CharacterCard[] cards = new CharacterCard[3];
        private int selectedIndex;

        private void Start()
        {
            var canvas = MenuUiKit.Canvas("Character Select Canvas");
            MenuUiKit.Background(canvas.transform, .72f);

            var parchment = UiFactory.Panel(canvas.transform, new Color(MenuUiKit.Parchment.r, MenuUiKit.Parchment.g, MenuUiKit.Parchment.b, .92f));
            parchment.name = "Character Select Parchment";
            parchment.raycastTarget = false;
            UiFactory.Stretch(parchment.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateTitle(canvas.transform);

            var visuals = Resources.Load<MenuVisualDefinition>("UI/MainMenuVisuals");
            cards[0] = CreateCard(canvas.transform, 0, "КЕРЕЙ ХАН", "ХАН • XV Ғ.", visuals != null ? visuals.khan : null, visuals != null ? visuals.characterMaterial : null, visuals != null ? visuals.kereyKhanCard : null);
            cards[1] = CreateCard(canvas.transform, 1, "АБЫЛАЙ ХАН", "ХАН • XVIII Ғ.", visuals != null ? visuals.khan : null, visuals != null ? visuals.characterMaterial : null, null);
            cards[2] = CreateCard(canvas.transform, 2, "ҚАБАНБАЙ БАТЫР", "БАТЫР • XVIII Ғ.", visuals != null ? visuals.character : null, visuals != null ? visuals.characterMaterial : null, null);
            CreateNavigation(canvas.transform);
            RefreshCards(true);
        }

        private void Update()
        {
            foreach (var card in cards) card?.Animate();
        }

        private void CreateTitle(Transform root)
        {
            var panel = MenuUiKit.Panel(root, new Vector2(.5f, .88f), new Vector2(990, 135), true);
            panel.name = "КЕЙІПКЕРДІ ТАҢДА Title Panel";
            var title = UiFactory.Text(panel.transform, "КЕЙІПКЕРДІ ТАҢДА", 48, TextAnchor.MiddleCenter, MenuUiKit.Cream);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Stretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(35, 0), new Vector2(-35, 0));
        }

        private void CreateNavigation(Transform root)
        {
            var left = MenuPixelButton.Create(root, "‹", () => Shift(-1));
            left.name = "Previous Character Arrow";
            UiFactory.SetRect(left.GetComponent<RectTransform>(), new Vector2(.11f, .48f), new Vector2(105, 88), Vector2.zero);
            var right = MenuPixelButton.Create(root, "›", () => Shift(1));
            right.name = "Next Character Arrow";
            UiFactory.SetRect(right.GetComponent<RectTransform>(), new Vector2(.89f, .48f), new Vector2(105, 88), Vector2.zero);
            var back = MenuPixelButton.Create(root, "АРТҚА", () => SceneLoader.Load(SceneId.MainMenu));
            back.name = "АРТҚА";
            UiFactory.SetRect(back.GetComponent<RectTransform>(), new Vector2(.12f, .10f), new Vector2(225, 66), Vector2.zero);
        }

        private CharacterCard CreateCard(Transform root, int index, string title, string period, Sprite portraitSprite, Material portraitMaterial, Sprite fullCardSprite)
        {
            if (fullCardSprite != null)
            {
                var cardImage = UiFactory.Panel(root, Color.white);
                cardImage.name = title + " Provided Card";
                cardImage.sprite = fullCardSprite;
                cardImage.preserveAspect = true;
                UiFactory.SetRect(cardImage.rectTransform, new Vector2(.5f, .48f), CardSize, Vector2.zero);
                var providedCardView = new CharacterCard(cardImage.rectTransform, cardImage.gameObject.AddComponent<CanvasGroup>());
                var providedCardButton = cardImage.gameObject.AddComponent<Button>();
                providedCardButton.targetGraphic = cardImage;
                providedCardButton.transition = Selectable.Transition.None;
                providedCardButton.onClick.AddListener(() => Select(index));
                providedCardView.SetVisuals(cardImage, null);
                AddCardMotion(cardImage.gameObject, providedCardView);
                return providedCardView;
            }

            var border = MenuUiKit.Panel(root, new Vector2(.5f, .48f), CardSize, false);
            border.name = title + " Card";
            var view = new CharacterCard(border.rectTransform, border.gameObject.AddComponent<CanvasGroup>());
            var cardButton = border.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = border;
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(() => Select(index));

            var portraitBackground = UiFactory.Panel(border.transform, MenuUiKit.Dark);
            portraitBackground.raycastTarget = false;
            UiFactory.SetRect(portraitBackground.rectTransform, new Vector2(.5f, .63f), new Vector2(476, 388), Vector2.zero);
            MenuUiKit.Portrait(portraitBackground.transform, portraitSprite, portraitMaterial, new Vector2(.5f, .5f), new Vector2(430, 355));

            var name = UiFactory.Text(border.transform, title, 32, TextAnchor.MiddleCenter, MenuUiKit.Cream);
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            UiFactory.SetRect(name.rectTransform, new Vector2(.5f, .25f), new Vector2(470, 46), Vector2.zero);
            var sub = UiFactory.Text(border.transform, period, 19, TextAnchor.MiddleCenter, MenuUiKit.Gold);
            sub.fontStyle = FontStyle.Bold;
            sub.raycastTarget = false;
            UiFactory.SetRect(sub.rectTransform, new Vector2(.5f, .18f), new Vector2(470, 32), Vector2.zero);

            var choose = MenuPixelButton.Create(border.transform, "ТАҢДАУ", ChooseCurrent);
            choose.name = "ТАҢДАУ";
            UiFactory.SetRect(choose.GetComponent<RectTransform>(), new Vector2(.5f, .07f), new Vector2(300, 58), Vector2.zero);
            view.SetVisuals(border, choose.gameObject);
            AddCardMotion(border.gameObject, view);
            return view;
        }

        private static void AddCardMotion(GameObject card, CharacterCard view)
        {
            var trigger = card.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            trigger.triggers = new System.Collections.Generic.List<UnityEngine.EventSystems.EventTrigger.Entry>();
            AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerEnter, () => view.SetHover(true));
            AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerExit, () => { view.SetHover(false); view.SetPressed(false); });
            AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerDown, () => view.SetPressed(true));
            AddTrigger(trigger, UnityEngine.EventSystems.EventTriggerType.PointerUp, () => view.SetPressed(false));
        }

        private static void AddTrigger(UnityEngine.EventSystems.EventTrigger trigger, UnityEngine.EventSystems.EventTriggerType type, System.Action action)
        {
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void Shift(int direction)
        {
            selectedIndex = (selectedIndex + direction + cards.Length) % cards.Length;
            RefreshCards(false);
        }

        private void Select(int index)
        {
            if (selectedIndex == index) { ChooseCurrent(); return; }
            selectedIndex = index;
            RefreshCards(false);
        }

        private void RefreshCards(bool immediate)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                var relative = (i - selectedIndex + cards.Length) % cards.Length;
                if (relative == 2) relative = -1;
                cards[i].SetState(relative, immediate);
            }
        }

        private void ChooseCurrent()
        {
            var hero = Resources.Load<ATTILA.Characters.HeroDefinition>("Data/TEMP_HERO");
            if (hero == null) return;
            SaveService.SelectHero(hero.HeroId);
            SceneLoader.Load(SceneId.AulPrototype);
        }

        private sealed class CharacterCard
        {
            private readonly RectTransform rect;
            private readonly CanvasGroup group;
            private Image border;
            private GameObject chooseButton;
            private Vector2 targetPosition;
            private Vector3 targetScale;
            private Vector3 baseScale;
            private float targetAlpha;
            private bool hovering;
            private bool pressed;

            public CharacterCard(RectTransform rect, CanvasGroup group) { this.rect = rect; this.group = group; }
            public void SetVisuals(Image borderImage, GameObject choose) { border = borderImage; chooseButton = choose; }
            public void SetHover(bool value) { hovering = value; ApplyInteractionScale(); }
            public void SetPressed(bool value) { pressed = value; ApplyInteractionScale(); }

            public void SetState(int relative, bool immediate)
            {
                var center = relative == 0;
                targetPosition = center ? CenterPosition : relative < 0 ? LeftPosition : RightPosition;
                baseScale = center ? Vector3.one : new Vector3(.72f, .72f, 1f);
                targetAlpha = center ? 1f : .28f;
                if (chooseButton != null) chooseButton.SetActive(center);
                border.color = chooseButton == null ? Color.white : center ? MenuUiKit.Cream : MenuUiKit.Gold;
                ApplyInteractionScale();
                if (!immediate) return;
                rect.anchoredPosition = targetPosition;
                rect.localScale = targetScale;
                group.alpha = targetAlpha;
            }

            private void ApplyInteractionScale()
            {
                var multiplier = pressed ? .96f : hovering ? 1.04f : 1f;
                targetScale = baseScale * multiplier;
            }

            public void Animate()
            {
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.unscaledDeltaTime * 11f);
                rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 11f);
                group.alpha = Mathf.Lerp(group.alpha, targetAlpha, Time.unscaledDeltaTime * 11f);
            }
        }
    }
}
