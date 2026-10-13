using UnityEngine;

namespace ATTILA.Core
{
    /// <summary>
    /// Keeps the screen clean for an audience, whichever scene play starts from:
    /// a backdrop camera clears the screen in the menu scenes, which have no camera of their own (no stale pixels in
    /// a build, no "No cameras rendering" in the editor), and the mouse cursor hides after a moment without use.
    /// </summary>
    public sealed class PresentationGuard : MonoBehaviour
    {
        private const float CursorIdleSeconds = 2f;

        private Camera backdrop;
        private Vector3 lastMouse;
        private float lastUse;
        private float nextCameraCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("Presentation Guard");
            DontDestroyOnLoad(go);
            go.AddComponent<PresentationGuard>();
        }

        private void Awake()
        {
            backdrop = gameObject.AddComponent<Camera>();
            backdrop.cullingMask = 0;
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = Color.black;
            backdrop.depth = -100;
            backdrop.orthographic = true;
            lastMouse = Input.mousePosition;
            lastUse = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime > nextCameraCheck)
            {
                nextCameraCheck = Time.unscaledTime + .25f;
                backdrop.enabled = !AnotherCameraDrawsToScreen();
            }

            // Any mouse movement or click brings the cursor back; keyboard play lets it fade from the shot.
            var mouse = Input.mousePosition;
            if ((mouse - lastMouse).sqrMagnitude > 1f || Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.touchCount > 0)
            {
                lastMouse = mouse;
                lastUse = Time.unscaledTime;
            }
            bool show = Time.unscaledTime - lastUse < CursorIdleSeconds;
            if (Cursor.visible != show) Cursor.visible = show;
        }

        private bool AnotherCameraDrawsToScreen()
        {
            foreach (var camera in Camera.allCameras)
                if (camera != backdrop && camera.targetTexture == null) return true;
            return false;
        }

        private void OnDestroy() => Cursor.visible = true;
    }
}
