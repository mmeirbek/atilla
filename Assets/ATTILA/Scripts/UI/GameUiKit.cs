using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>In-game panels built from the menu palette and pixel frames so gameplay and menus read as one design.</summary>
    public static class GameUiKit
    {
        public static readonly Color Ink = MenuUiKit.FromHex("2B1A14");
        public static readonly Color InkSoft = MenuUiKit.FromHex("3A2419");
        public static readonly Color Ember = MenuUiKit.FromHex("E7A55B");
        public static readonly Color Muted = MenuUiKit.FromHex("B89B7E");

        private static Sprite vignette;

        public sealed class Frame
        {
            public Image Border;
            public Image Fill;
            public GameObject GameObject => Border.gameObject;
            public RectTransform Rect => Border.rectTransform;
            public Transform Content => Fill.transform;
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvas = MenuUiKit.Canvas(name);
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }

        public static Frame Panel(Transform root, Vector2 anchor, Vector2 size, Vector2 position, Color fill, Color border, float edge = 6f)
        {
            var borderImage = UiFactory.Panel(root, border);
            UiFactory.SetRect(borderImage.rectTransform, anchor, size, position);
            var fillImage = UiFactory.Panel(borderImage.transform, fill);
            fillImage.name = "Fill";
            fillImage.raycastTarget = false;
            UiFactory.Stretch(fillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(edge, edge), new Vector2(-edge, -edge));
            return new Frame { Border = borderImage, Fill = fillImage };
        }

        public static Frame InkPanel(Transform root, Vector2 anchor, Vector2 size, Vector2 position) =>
            Panel(root, anchor, size, position, new Color(Ink.r, Ink.g, Ink.b, .95f), MenuUiKit.Gold);

        /// <summary>Text laid out from the top-left corner of its parent, which makes inner panel layouts easy to reason about.</summary>
        public static Text Label(Transform parent, string value, int size, Color color, TextAnchor alignment, float x, float y, float width, float height)
        {
            var text = UiFactory.Text(parent, value, size, alignment, color);
            text.raycastTarget = false;
            PlaceTopLeft(text.rectTransform, x, y, width, height);
            return text;
        }

        public static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static Image Pixel(Transform parent, float x, float y, float width, float height, Color color)
        {
            var image = UiFactory.Panel(parent, color);
            image.name = "Pixel";
            image.raycastTarget = false;
            PlaceTopLeft(image.rectTransform, x, y, width, height);
            return image;
        }

        /// <summary>Name plate that sits on a frame's top edge, like the speaker tab in the dialogue reference.</summary>
        public static Text NameTab(Transform parent, string value, float x, float y, float width)
        {
            var tab = Panel(parent, new Vector2(0f, 1f), new Vector2(width, 60f), Vector2.zero, MenuUiKit.Burgundy, MenuUiKit.Gold, 4f);
            tab.Rect.pivot = new Vector2(0f, 0f);
            tab.Rect.anchoredPosition = new Vector2(x, y);
            tab.Border.raycastTarget = false;
            var text = UiFactory.Text(tab.Content, value, 34, TextAnchor.MiddleCenter, MenuUiKit.Cream);
            text.raycastTarget = false;
            UiFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
            return text;
        }

        public static Slider Slider(Transform parent, Vector2 anchor, Vector2 size, float value, System.Action<float> onChanged)
        {
            var track = MenuUiKit.Panel(parent, anchor, size, true);
            track.name = "Slider";
            var fillArea = UiFactory.Panel(track.transform, Color.clear);
            fillArea.raycastTarget = false;
            UiFactory.Stretch(fillArea.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10));
            var fill = UiFactory.Panel(fillArea.transform, MenuUiKit.Gold);
            fill.raycastTarget = false;
            var handleArea = UiFactory.Panel(track.transform, Color.clear);
            handleArea.raycastTarget = false;
            UiFactory.Stretch(handleArea.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0));
            var handle = UiFactory.Panel(handleArea.transform, MenuUiKit.Cream);
            handle.rectTransform.sizeDelta = new Vector2(20f, size.y + 16f);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(changed => onChanged?.Invoke(changed));
            return slider;
        }

        /// <summary>Radial darkening with a hard pixel-stepped falloff. Scale the Image to move the clear centre's radius.</summary>
        public static Image Vignette(Transform parent, float size, float alpha)
        {
            var image = UiFactory.Panel(parent, new Color(1f, 1f, 1f, alpha));
            image.name = "Vignette";
            image.sprite = VignetteSprite();
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        private static Sprite VignetteSprite()
        {
            if (vignette != null) return vignette;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Vignette", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2f - 1f, dy = (y + .5f) / size * 2f - 1f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((distance - .18f) / .38f);
                    alpha = Mathf.Round(alpha * 6f) / 6f;
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            vignette = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            return vignette;
        }
    }
}
