using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Movement and the action key for devices without a keyboard: a thumb stick on the left, an action button
    /// on the right. Built only when the device reports touch, so desktop play is untouched.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float Reach = 110f;

        private RectTransform knob;
        private RectTransform ring;
        private Canvas canvas;
        private int pointerId = -99;

        public static bool Supported => Input.touchSupported || Application.isMobilePlatform;

        public static TouchControls Attach(Transform host)
        {
            if (!Supported) return null;
            var go = new GameObject("Touch Controls", typeof(RectTransform));
            go.transform.SetParent(host, false);
            UiFactory.Stretch((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var controls = go.AddComponent<TouchControls>();
            controls.Build();
            return controls;
        }

        private void Build()
        {
            canvas = GetComponentInParent<Canvas>();
            var pad = UiFactory.Panel(transform, new Color(0f, 0f, 0f, .001f));
            pad.name = "Move pad";
            UiFactory.Stretch(pad.rectTransform, Vector2.zero, new Vector2(.45f, .6f), Vector2.zero, Vector2.zero);
            pad.gameObject.AddComponent<TouchPadRelay>().Owner = this;

            var ringFrame = GameUiKit.Panel(transform, new Vector2(.12f, .22f), new Vector2(240, 240), Vector2.zero,
                new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .35f), new Color(MenuUiKit.Gold.r, MenuUiKit.Gold.g, MenuUiKit.Gold.b, .5f));
            ringFrame.Border.raycastTarget = false;
            ring = ringFrame.Rect;
            var knobImage = UiFactory.Panel(ringFrame.Content, new Color(MenuUiKit.Cream.r, MenuUiKit.Cream.g, MenuUiKit.Cream.b, .75f));
            knobImage.raycastTarget = false;
            UiFactory.SetRect(knobImage.rectTransform, new Vector2(.5f, .5f), new Vector2(88, 88), Vector2.zero);
            knob = knobImage.rectTransform;

            Action("ІС-ӘРЕКЕТ", new Vector2(.88f, .2f), new Vector2(260, 130), () => VirtualInput.PressAction());
            Action("МӘЗІР", new Vector2(.93f, .93f), new Vector2(190, 92), () => VirtualInput.PressMenu());
        }

        private void Action(string label, Vector2 anchor, Vector2 size, System.Action press)
        {
            var button = MenuPixelButton.Create(transform, label, press);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), anchor, size, Vector2.zero);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerId = eventData.pointerId;
            ring.position = eventData.position;
            Drag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) Drag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            pointerId = -99;
            knob.anchoredPosition = Vector2.zero;
            VirtualInput.Move = Vector2.zero;
        }

        private void Drag(PointerEventData eventData)
        {
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            var offset = (eventData.position - (Vector2)ring.position) / Mathf.Max(.01f, scale);
            offset = Vector2.ClampMagnitude(offset, Reach);
            knob.anchoredPosition = offset;
            VirtualInput.Move = offset / Reach;
        }
    }

    /// <summary>Forwards the pad's pointer events to the stick, so the whole left half of the screen drives it.</summary>
    public sealed class TouchPadRelay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public TouchControls Owner;
        public void OnPointerDown(PointerEventData eventData) => Owner.OnPointerDown(eventData);
        public void OnDrag(PointerEventData eventData) => Owner.OnDrag(eventData);
        public void OnPointerUp(PointerEventData eventData) => Owner.OnPointerUp(eventData);
    }

    /// <summary>Keyboard and touch meet here, so gameplay code never asks which device it is running on.</summary>
    public static class VirtualInput
    {
        private static int actionFrame = -10;
        private static int menuFrame = -10;

        public static Vector2 Move { get; set; }

        public static void PressAction() => actionFrame = Time.frameCount;
        public static void PressMenu() => menuFrame = Time.frameCount;

        /// <summary>WASD plus the thumb stick, clamped to one unit.</summary>
        public static Vector2 Movement()
        {
            var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(keyboard + Move, 1f);
        }

        /// <summary>True on the frame E, Space, Enter, a click or the touch action button fires.</summary>
        public static bool Action() =>
            Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || ActionTouched();

        public static bool ActionTouched() => Time.frameCount - actionFrame <= 1;

        public static bool Menu() => Input.GetKeyDown(KeyCode.Escape) || Time.frameCount - menuFrame <= 1;
    }
}
