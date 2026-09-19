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
        private Vector3 targetScale = Vector3.one;

        public static Button Create(Transform parent, string labelText, System.Action action)
        {
            var border = UiFactory.Panel(parent, Gold);
            border.name = labelText;
            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => action?.Invoke());

            var fill = UiFactory.Panel(border.transform, Burgundy);
            fill.name = "Fill";
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));

            var text = UiFactory.Text(fill.transform, labelText, 29, TextAnchor.MiddleCenter, Cream);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            UiFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-28, 0));

            var left = CreateOrnament(fill.transform, "Left Ornament", TextAnchor.MiddleLeft);
            var right = CreateOrnament(fill.transform, "Right Ornament", TextAnchor.MiddleRight);

            var visual = border.gameObject.AddComponent<MenuPixelButton>();
            visual.border = border;
            visual.fill = fill;
            visual.label = text;
            visual.ornamentLeft = left;
            visual.ornamentRight = right;
            visual.ApplyState();
            return button;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; ApplyState(); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressed = false; ApplyState(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; ApplyState(); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; ApplyState(); }
        public void OnSelect(BaseEventData eventData) { selected = true; ApplyState(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; ApplyState(); }

        private static Graphic CreateOrnament(Transform parent, string name, TextAnchor anchor)
        {
            var ornament = UiFactory.Text(parent, "◆·◆", 13, anchor, Gold);
            ornament.name = name;
            ornament.raycastTarget = false;
            UiFactory.Stretch(ornament.rectTransform, Vector2.zero, Vector2.one, new Vector2(13, 0), new Vector2(-13, 0));
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

        private void Update() => transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 16f);

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
