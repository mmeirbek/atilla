using ATTILA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.CameraSystem
{
    /// <summary>
    /// Renders the world into a low-resolution buffer and shows it point-filtered, so the 3D props, the
    /// ground and the 30 px-per-unit character sprites end up on one pixel grid. The chapter tint is applied here.
    /// </summary>
    public sealed class PixelCamera : MonoBehaviour
    {
        /// <summary>Vertical resolution: the camera shows 18 world units, which is 30 pixels per unit.</summary>
        public const int TargetHeight = 540;
        public static PixelCamera Active { get; private set; }

        private Camera source;
        private Camera display;
        private RawImage view;
        private RenderTexture texture;
        private int width, height;

        public static PixelCamera Attach(GameObject cameraObject, Color tint)
        {
            var pixel = cameraObject.AddComponent<PixelCamera>();
            pixel.source = cameraObject.GetComponent<Camera>();
            pixel.Build(tint);
            return pixel;
        }

        private void Build(Color tint)
        {
            Active = this;
            var displayObject = new GameObject("Pixel Display Camera", typeof(Camera));
            display = displayObject.GetComponent<Camera>();
            display.cullingMask = 0;
            display.clearFlags = CameraClearFlags.SolidColor;
            display.backgroundColor = Color.black;
            display.depth = -10;
            display.orthographic = true;

            var canvas = new GameObject("Pixel View", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            var raw = new GameObject("World", typeof(RawImage));
            raw.transform.SetParent(canvas.transform, false);
            view = raw.GetComponent<RawImage>();
            view.color = tint;
            view.raycastTarget = false;
            UiFactory.Stretch(view.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Resize(Screen.width, Screen.height);
        }

        /// <summary>Forces a buffer aspect, used when rendering validation screenshots at a fixed size.</summary>
        public void Resize(int screenWidth, int screenHeight)
        {
            int newHeight = TargetHeight;
            int newWidth = Mathf.Max(64, Mathf.RoundToInt(TargetHeight * screenWidth / (float)Mathf.Max(1, screenHeight)));
            if (texture != null && newWidth == width && newHeight == height) return;
            width = newWidth;
            height = newHeight;
            if (texture != null) { source.targetTexture = null; texture.Release(); Destroy(texture); }
            texture = new RenderTexture(width, height, 24) { filterMode = FilterMode.Point, name = "ATTILA Pixel Buffer" };
            texture.Create();
            source.targetTexture = texture;
            view.texture = texture;
        }

        /// <summary>Renders the world into the buffer immediately; the validation screenshots need it up to date.</summary>
        public void RenderNow()
        {
            if (source != null && texture != null) source.Render();
        }

        private void LateUpdate() => Resize(Screen.width, Screen.height);

        private void OnDestroy()
        {
            if (source != null) source.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (display != null) Destroy(display.gameObject);
            if (view != null) Destroy(view.canvas.gameObject);
            if (Active == this) Active = null;
        }
    }
}
