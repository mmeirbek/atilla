using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>
    /// Keeps the interface inside the usable part of the screen. Phones with a notch and browsers with odd
    /// aspects report a smaller safe area than the full canvas, and without this the HUD would sit under the cut.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform frame;
        private Rect applied;

        private void Start()
        {
            var canvas = GetComponent<Canvas>();
            frame = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(transform, false);
            // Everything already built moves inside the frame, so callers never have to know about it.
            // The order has to be kept: the menu background is drawn first and the buttons on top of it.
            var children = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child != frame) children.Add(child);
            }
            foreach (var child in children) child.SetParent(frame, false);
            frame.SetAsFirstSibling();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != applied) Apply();
        }

        private void Apply()
        {
            if (frame == null) return;
            applied = Screen.safeArea;
            var min = applied.position;
            var max = applied.position + applied.size;
            min.x /= Mathf.Max(1, Screen.width);
            min.y /= Mathf.Max(1, Screen.height);
            max.x /= Mathf.Max(1, Screen.width);
            max.y /= Mathf.Max(1, Screen.height);
            frame.anchorMin = min;
            frame.anchorMax = max;
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
        }
    }
}
