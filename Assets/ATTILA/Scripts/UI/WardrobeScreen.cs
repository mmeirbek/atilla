using System.Collections.Generic;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Clothing catalogue: a rotating character on the left, the garment list on the right.
    /// Presentation only, no inventory: the descriptions are the point.
    /// </summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private static readonly string[] Looks = { "kerey", "abylai", "kabanbay", "woman", "bi", "elder", "warrior", "child" };
        private static readonly FacingDirection[] Turn = { FacingDirection.S, FacingDirection.SE, FacingDirection.E, FacingDirection.NE, FacingDirection.N, FacingDirection.NW, FacingDirection.W, FacingDirection.SW };

        private Stage2Content content;
        private Image preview, itemIcon;
        private Text previewName, itemTitle, itemSlot, itemText, itemStatus;
        private int look;
        private int facing;
        private float nextTurn;

        public static void Show(Transform root)
        {
            var host = new GameObject("Wardrobe Screen", typeof(RectTransform));
            host.transform.SetParent(root, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            host.AddComponent<WardrobeScreen>().Build();
        }

        private void Build()
        {
            content = Resources.Load<Stage2Content>("Data/Stage2Content");
            MenuUiKit.Background(transform, .8f);
            MenuUiKit.Header(transform, "ҚАЗАҚ КИІМІ", () => Destroy(gameObject));

            var stand = GameUiKit.InkPanel(transform, new Vector2(0f, .5f), new Vector2(680, 760), new Vector2(390, -20));
            preview = UiFactory.Panel(stand.Content, Color.white);
            preview.raycastTarget = false;
            preview.preserveAspect = true;
            UiFactory.SetRect(preview.rectTransform, new Vector2(.5f, .58f), new Vector2(384, 640), Vector2.zero);
            previewName = GameUiKit.Label(stand.Content, string.Empty, 30, MenuUiKit.Gold, TextAnchor.MiddleCenter, 40, 640, 580, 44);
            var left = MenuPixelButton.Create(stand.Content, "‹", () => Shift(-1));
            UiFactory.SetRect(left.GetComponent<RectTransform>(), new Vector2(.14f, .1f), new Vector2(96, 76), Vector2.zero);
            var right = MenuPixelButton.Create(stand.Content, "›", () => Shift(1));
            UiFactory.SetRect(right.GetComponent<RectTransform>(), new Vector2(.86f, .1f), new Vector2(96, 76), Vector2.zero);

            var list = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(520, 760), new Vector2(-1030, -20));
            var items = content != null ? content.outfitItems : new List<OutfitItemDefinition>();
            for (int i = 0; i < items.Count; i++) Row(list.Content, items[i], i);

            var detail = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(680, 760), new Vector2(-390, -20));
            itemIcon = UiFactory.Panel(detail.Content, Color.white);
            itemIcon.raycastTarget = false;
            itemIcon.preserveAspect = true;
            GameUiKit.PlaceTopLeft(itemIcon.rectTransform, 40, 40, 160, 160);
            itemTitle = GameUiKit.Label(detail.Content, string.Empty, 34, MenuUiKit.Gold, TextAnchor.UpperLeft, 230, 52, 400, 48);
            itemSlot = GameUiKit.Label(detail.Content, string.Empty, 20, MenuUiKit.Parchment, TextAnchor.UpperLeft, 230, 108, 400, 30);
            GameUiKit.Pixel(detail.Content, 40, 230, 600, 3, MenuUiKit.Gold);
            itemText = GameUiKit.Label(detail.Content, string.Empty, 24, MenuUiKit.Cream, TextAnchor.UpperLeft, 40, 262, 600, 330);
            itemStatus = GameUiKit.Label(detail.Content, string.Empty, 20, GameUiKit.Ember, TextAnchor.UpperLeft, 40, 620, 600, 30);
            if (items.Count > 0) SelectItem(items[0]);
            Refresh();
        }

        private void Row(Transform parent, OutfitItemDefinition item, int index)
        {
            var frame = GameUiKit.Panel(parent, new Vector2(.5f, 1f), new Vector2(460, 86), new Vector2(0, -(62 + index * 100)), MenuUiKit.Burgundy, MenuUiKit.Gold, 4f);
            var button = frame.GameObject.AddComponent<Button>();
            button.targetGraphic = frame.Border;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => SelectItem(item));
            var icon = UiFactory.Panel(frame.Content, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.Icon(item.icon) : null;
            GameUiKit.PlaceTopLeft(icon.rectTransform, 12, 10, 58, 58);
            GameUiKit.Label(frame.Content, item.title, 26, MenuUiKit.Cream, TextAnchor.MiddleLeft, 86, 26, 340, 34);
        }

        private void SelectItem(OutfitItemDefinition item)
        {
            itemIcon.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.Icon(item.icon) : null;
            itemTitle.text = item.title;
            itemSlot.text = item.slot;
            itemText.text = item.description;
            itemStatus.text = item.status == ContentStatus.Draft ? "ДЕРЕККӨЗІ ТЕКСЕРІЛУДЕ" : string.Empty;
        }

        private void Shift(int direction)
        {
            look = (look + direction + Looks.Length) % Looks.Length;
            Refresh();
        }

        private void Refresh()
        {
            var set = LookLibrary.Load(Looks[look]);
            if (set == null) return;
            previewName.text = string.IsNullOrEmpty(set.displayName) ? Looks[look] : set.displayName;
            preview.sprite = set.directions[(int)Turn[facing]]?.idle;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTurn) return;
            nextTurn = Time.unscaledTime + 1.1f;
            facing = (facing + 1) % Turn.Length;
            Refresh();
        }
    }
}
