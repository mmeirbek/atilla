using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>Very small presentation-only drift for the menu foreground.</summary>
    public sealed class MenuAmbientMotion : MonoBehaviour
    {
        [SerializeField] private Vector2 amplitude = new(2f, 1f);
        [SerializeField] private float frequency = .18f;
        private RectTransform target;
        private Vector2 startPosition;

        private void Awake()
        {
            target = GetComponent<RectTransform>();
            startPosition = target.anchoredPosition;
        }

        private void Update()
        {
            var time = Time.unscaledTime * frequency;
            target.anchoredPosition = startPosition + new Vector2(Mathf.Sin(time) * amplitude.x, Mathf.Cos(time * .71f) * amplitude.y);
        }
    }
}
