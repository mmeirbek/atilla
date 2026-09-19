using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Shared smooth hover and press feedback for art-backed menu buttons.</summary>
    public sealed class MenuImageButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image image;
        private bool hovering;
        private bool pressed;
        private Vector3 targetScale = Vector3.one;

        private void Awake() => image = GetComponent<Image>();
        public void OnPointerEnter(PointerEventData eventData) { hovering = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; Refresh(); }
        private void Refresh()
        {
            targetScale = pressed ? new Vector3(.95f, .95f, 1f) : hovering ? new Vector3(1.045f, 1.045f, 1f) : Vector3.one;
            if (image != null) image.color = pressed ? new Color(.82f,.82f,.82f,1f) : hovering ? new Color(1f,.95f,.82f,1f) : Color.white;
        }
        private void Update() => transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 16f);
    }
}
