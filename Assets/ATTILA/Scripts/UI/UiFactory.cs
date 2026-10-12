using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATTILA.UI
{
    public static class UiFactory
    {
        public static readonly Color Primary = new(0.545f, 0.18f, 0.141f);
        public static readonly Color Background = new(0.969f, 0.902f, 0.82f);
        public static readonly Color Accent = new(0.906f, 0.435f, 0.318f);

        // Press Start 2P is roughly twice as wide per em as the Arial the layouts were first drawn for.
        // Callers keep passing the original design sizes; they are scaled and snapped to the font's 8 px grid.
        private const float PixelFontScale = .7f;
        private const int PixelGrid = 8;
        private const int MinimumPixelSize = 16;
        private static Font pixelFont;
        private static Font PixelFont => pixelFont != null ? pixelFont : pixelFont = Resources.Load<Font>("UI/PressStart2P-Regular");

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(eventSystem);
            }
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            go.AddComponent<SafeArea>();
            return canvas;
        }
        public static Text Text(Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject("Text", typeof(Text)); go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            var font = PixelFont;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = font != null ? SnapToPixelGrid(size) : size;
            text.text = value; text.alignment = alignment; text.color = color; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            // The pixel font has no built-in leading, so wrapped lines would otherwise touch.
            text.lineSpacing = 1.3f;
            return text;
        }
        /// <summary>Steps a single-line label down the pixel grid until it fits its width; runtime text varies in length.</summary>
        public static void ShrinkToFit(Text text, int designSize)
        {
            int size = text.font != null && text.font.name.Contains("PressStart") ? SnapToPixelGrid(designSize) : designSize;
            text.fontSize = size;
            float width = text.rectTransform.rect.width;
            while (size > MinimumPixelSize && text.preferredWidth > width)
            {
                size -= PixelGrid;
                text.fontSize = size;
            }
        }

        public static Image Panel(Transform parent, Color color)
        {
            var go = new GameObject("Panel", typeof(Image)); go.transform.SetParent(parent, false); var image = go.GetComponent<Image>(); image.color = color; return image;
        }
        /// <summary>Square rotated 45 degrees; stands in for the diamond glyph the pixel font does not contain.</summary>
        public static Image Diamond(Transform parent, float size, Color color)
        {
            var image = Panel(parent, color);
            image.name = "Diamond";
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return image;
        }
        public static Button Button(Transform parent, string label, System.Action click, bool enabled = true)
        {
            var image = Panel(parent, enabled ? Primary : new Color(0.55f, 0.45f, 0.40f)); image.name = label;
            var button = image.gameObject.AddComponent<Button>(); button.interactable = enabled; button.targetGraphic = image; button.onClick.AddListener(() => click?.Invoke());
            var text = Text(image.transform, label, 25, TextAnchor.MiddleCenter, Background); Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }
        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        { rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position; }

        private static int SnapToPixelGrid(int designSize) =>
            Mathf.Max(MinimumPixelSize, Mathf.RoundToInt(designSize * PixelFontScale / PixelGrid) * PixelGrid);
    }
}
