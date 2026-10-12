using ATTILA.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Reusable presentation component for the Main Menu's pixel-framed buttons.</summary>
    public sealed class MenuPixelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private static readonly Color DarkBurgundy = ColorFromHex("471310");
        private static readonly Color Burgundy = ColorFromHex("6D1D17");
        private static readonly Color WarmRed = ColorFromHex("8B2E24");
        private static readonly Color Cream = ColorFromHex("F7E6D1");
        private static readonly Color Gold = ColorFromHex("D99A55");

        private Image border;
        private Image fill;
        private Graphic ornamentLeft;
        private Graphic ornamentRight;
        private Text label;
        private bool hovering;
        private bool pressed;
        private bool selected;
        private int labelSize;
        private string fittedText;
        private Vector3 targetScale = Vector3.one;

        public static Button Create(Transform parent, string labelText, System.Action action, int fontSize = 40)
        {
            var border = UiFactory.Panel(parent, Gold);
            border.name = labelText;
            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => { AudioService.Play(Sfx.Click); action?.Invoke(); });

            var fill = UiFactory.Panel(border.transform, Burgundy);
            fill.name = "Fill";
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));

            var text = UiFactory.Text(fill.transform, labelText, fontSize, TextAnchor.MiddleCenter, Cream);
            text.raycastTarget = false;
            UiFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(fontSize < 36 ? 20 : 34, 0), new Vector2(fontSize < 36 ? -20 : -34, 0));

            var left = CreateOrnament(fill.transform, "Left Ornament", true);
            var right = CreateOrnament(fill.transform, "Right Ornament", false);

            var visual = border.gameObject.AddComponent<MenuPixelButton>();
            visual.border = border;
            visual.fill = fill;
            visual.label = text;
            visual.ornamentLeft = left;
            visual.ornamentRight = right;
            visual.labelSize = text.fontSize;
            visual.ApplyState();
            return button;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; AudioService.Play(Sfx.Hover, .5f); ApplyState(); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressed = false; ApplyState(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; ApplyState(); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; ApplyState(); }
        public void OnSelect(BaseEventData eventData) { selected = true; ApplyState(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; ApplyState(); }

        private static Graphic CreateOrnament(Transform parent, string name, bool leftSide)
        {
            var ornament = UiFactory.Diamond(parent, 14f, Gold);
            ornament.name = name;
            var rect = ornament.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(leftSide ? 0f : 1f, .5f);
            rect.anchoredPosition = new Vector2(leftSide ? 22f : -22f, 0f);
            return ornament;
        }

        private void ApplyState()
        {
            var active = hovering || selected;
            border.color = active ? Cream : Gold;
            fill.color = pressed ? DarkBurgundy : active ? WarmRed : Burgundy;
            label.color = active ? Color.white : Cream;
            ornamentLeft.color = ornamentRight.color = active ? Cream : Gold;
            targetScale = pressed ? new Vector3(.96f, .96f, 1f) : active ? new Vector3(1.045f, 1.045f, 1f) : Vector3.one;
        }

        private void Start() => FitLabel();
        private void OnRectTransformDimensionsChange() => FitLabel();

        /// <summary>Steps the pixel font down a grid size at a time until no word is split and every line fits.</summary>
        public void FitLabel()
        {
            if (label == null || labelSize == 0) return;
            fittedText = label.text;
            var rect = label.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            int size = labelSize;
            label.fontSize = size;
            while (size > 16 && !LabelFits(rect))
            {
                size -= 8;
                label.fontSize = size;
            }
        }

        private bool LabelFits(Rect rect)
        {
            var generator = label.cachedTextGeneratorForLayout;
            var settings = label.GetGenerationSettings(Vector2.zero);
            foreach (var word in label.text.Split(' '))
                if (generator.GetPreferredWidth(word, settings) / label.pixelsPerUnit > rect.width) return false;
            return label.preferredHeight <= rect.height + 1f;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 16f);
            if (label != null && label.text != fittedText) FitLabel();
        }

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
