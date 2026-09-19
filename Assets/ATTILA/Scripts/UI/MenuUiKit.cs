using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Shared presentation construction for all Stage 1.6 menu screens.</summary>
    public static class MenuUiKit
    {
        public static readonly Color Dark = FromHex("471310");
        public static readonly Color Burgundy = FromHex("6D1D17");
        public static readonly Color Warm = FromHex("8B2E24");
        public static readonly Color Cream = FromHex("F7E6D1");
        public static readonly Color Parchment = FromHex("F7F1E3");
        public static readonly Color Gold = FromHex("D99A55");

        public static Canvas Canvas(string name)
        {
            var canvas = UiFactory.CreateCanvas(name);
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        public static void Background(Transform root, float veilAlpha = .58f)
        {
            var visuals = Resources.Load<MenuVisualDefinition>("UI/MainMenuVisuals");
            var image = UiFactory.Panel(root, Color.white);
            image.name = "Shared Steppe Background";
            image.sprite = visuals != null ? visuals.background : null;
            image.preserveAspect = true;
            UiFactory.Stretch(image.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var veil = UiFactory.Panel(root, new Color(Dark.r, Dark.g, Dark.b, veilAlpha));
            veil.name = "Parchment Burgundy Veil";
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public static void Header(Transform root, string title, System.Action back)
        {
            var heading = UiFactory.Text(root, title, 48, TextAnchor.MiddleCenter, Gold);
            heading.fontStyle = FontStyle.Bold;
            UiFactory.SetRect(heading.rectTransform, new Vector2(.5f, .88f), new Vector2(980, 78), Vector2.zero);
            if (back == null) return;
            var button = MenuPixelButton.Create(root, "АРТҚА", back);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.12f, .10f), new Vector2(190, 58), Vector2.zero);
        }

        public static Image Panel(Transform root, Vector2 anchor, Vector2 size, bool selected = false)
        {
            var border = UiFactory.Panel(root, selected ? Cream : Gold);
            border.name = selected ? "Selected Pixel Panel" : "Pixel Panel";
            UiFactory.SetRect(border.rectTransform, anchor, size, Vector2.zero);
            var fill = UiFactory.Panel(border.transform, selected ? Warm : Burgundy);
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            return border;
        }

        public static Button Card(Transform root, string title, string subtitle, Vector2 anchor, Vector2 size, System.Action click, bool selected = false)
        {
            var panel = Panel(root, anchor, size, selected);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => click?.Invoke());
            var titleText = UiFactory.Text(panel.transform, title, 28, TextAnchor.MiddleCenter, Cream);
            titleText.fontStyle = FontStyle.Bold;
            UiFactory.SetRect(titleText.rectTransform, new Vector2(.5f, .20f), new Vector2(size.x - 26, 42), Vector2.zero);
            var sub = UiFactory.Text(panel.transform, subtitle, 18, TextAnchor.MiddleCenter, Parchment);
            UiFactory.SetRect(sub.rectTransform, new Vector2(.5f, .11f), new Vector2(size.x - 30, 34), Vector2.zero);
            return button;
        }

        public static void Portrait(Transform root, Sprite sprite, Material material, Vector2 anchor, Vector2 size)
        {
            var portrait = UiFactory.Panel(root, Color.white);
            portrait.sprite = sprite;
            portrait.material = material;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            UiFactory.SetRect(portrait.rectTransform, anchor, size, Vector2.zero);
        }

        public static Color FromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
