using System.Collections.Generic;
using System.Linq;
using ATTILA.Characters;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Clothing catalogue. The character on the left turns on the spot, and the list shows only what that
    /// character actually wears, so the batyr's armour and the woman's kimeshek never mix.
    /// </summary>
    public sealed class WardrobeScreen : MonoBehaviour
    {
        private static readonly string[] Looks = { "kerey", "abylai", "kabanbay", "woman", "bi", "elder", "warrior", "child" };
        private static readonly FacingDirection[] Turn = { FacingDirection.S, FacingDirection.SE, FacingDirection.E, FacingDirection.NE, FacingDirection.N, FacingDirection.NW, FacingDirection.W, FacingDirection.SW };

        private Stage2Content content;
        private Transform listRoot;
        private Image preview, itemIcon;
        private Text previewName, itemTitle, itemSlot, itemText, itemStatus, emptyNote;
        private readonly List<GameObject> rows = new();
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
            var selected = HeroCatalog.Selected;
            look = Mathf.Max(0, System.Array.IndexOf(Looks, selected != null ? selected.VisualSetId : "kerey"));
            MenuUiKit.Background(transform, .82f);
            MenuUiKit.Header(transform, "ҚАЗАҚ КИІМІ", () => Destroy(gameObject));

            var stand = GameUiKit.InkPanel(transform, new Vector2(0f, .5f), new Vector2(600, 740), new Vector2(340, -20));
            preview = UiFactory.Panel(stand.Content, Color.white);
            preview.raycastTarget = false;
            preview.preserveAspect = true;
            UiFactory.SetRect(preview.rectTransform, new Vector2(.5f, .6f), new Vector2(360, 600), Vector2.zero);
            previewName = GameUiKit.Label(stand.Content, string.Empty, 30, MenuUiKit.Gold, TextAnchor.MiddleCenter, 40, 596, 510, 46);
            var left = MenuPixelButton.Create(stand.Content, "‹", () => Shift(-1));
            UiFactory.SetRect(left.GetComponent<RectTransform>(), new Vector2(.22f, .075f), new Vector2(120, 88), Vector2.zero);
            var right = MenuPixelButton.Create(stand.Content, "›", () => Shift(1));
            UiFactory.SetRect(right.GetComponent<RectTransform>(), new Vector2(.78f, .075f), new Vector2(120, 88), Vector2.zero);

            var list = GameUiKit.InkPanel(transform, new Vector2(.5f, .5f), new Vector2(520, 740), new Vector2(-40, -20));
            GameUiKit.Label(list.Content, "КИІМДЕРІ", 24, MenuUiKit.Gold, TextAnchor.UpperCenter, 20, 22, 440, 34);
            listRoot = list.Content;
            emptyNote = GameUiKit.Label(list.Content, string.Empty, 22, GameUiKit.Muted, TextAnchor.UpperCenter, 40, 120, 400, 80);

            var detail = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(660, 740), new Vector2(-370, -20));
            itemIcon = UiFactory.Panel(detail.Content, Color.white);
            itemIcon.raycastTarget = false;
            itemIcon.preserveAspect = true;
            GameUiKit.PlaceTopLeft(itemIcon.rectTransform, 44, 44, 150, 150);
            itemTitle = GameUiKit.Label(detail.Content, string.Empty, 34, MenuUiKit.Gold, TextAnchor.UpperLeft, 220, 56, 380, 48);
            itemSlot = GameUiKit.Label(detail.Content, string.Empty, 20, MenuUiKit.Parchment, TextAnchor.UpperLeft, 220, 112, 380, 30);
            GameUiKit.Pixel(detail.Content, 44, 224, 570, 3, MenuUiKit.Gold);
            itemText = GameUiKit.Label(detail.Content, string.Empty, 24, MenuUiKit.Cream, TextAnchor.UpperLeft, 44, 256, 570, 380);
            itemStatus = GameUiKit.Label(detail.Content, string.Empty, 20, GameUiKit.Ember, TextAnchor.UpperLeft, 44, 664, 570, 30);
            Refresh();
        }

        private List<OutfitItemDefinition> ItemsFor(string lookId) =>
            content == null ? new List<OutfitItemDefinition>()
                : content.outfitItems.Where(item => item.looks != null && item.looks.Contains(lookId)).ToList();

        private void Row(OutfitItemDefinition item, int index)
        {
            var frame = GameUiKit.Panel(listRoot, new Vector2(.5f, 1f), new Vector2(460, 92), new Vector2(0, -(96 + index * 104)), MenuUiKit.Burgundy, MenuUiKit.Gold, 4f);
            var button = frame.GameObject.AddComponent<Button>();
            button.targetGraphic = frame.Border;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => SelectItem(item));
            var icon = UiFactory.Panel(frame.Content, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.Icon(item.icon) : null;
            GameUiKit.PlaceTopLeft(icon.rectTransform, 12, 10, 64, 64);
            GameUiKit.Label(frame.Content, item.title, 26, MenuUiKit.Cream, TextAnchor.MiddleLeft, 92, 28, 330, 36);
            rows.Add(frame.GameObject);
        }

        private void SelectItem(OutfitItemDefinition item)
        {
            itemIcon.enabled = true;
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
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            var set = LookLibrary.Load(Looks[look]);
            if (set == null) return;
            previewName.text = string.IsNullOrEmpty(set.displayName) ? Looks[look] : set.displayName;
            preview.sprite = set.directions[(int)Turn[facing]]?.idle;
            var items = ItemsFor(Looks[look]);
            for (int i = 0; i < items.Count; i++) Row(items[i], i);
            emptyNote.text = items.Count == 0 ? "Бұл кейіпкердің киімі әлі сипатталмаған." : string.Empty;
            if (items.Count > 0) SelectItem(items[0]);
            else
            {
                itemIcon.enabled = false;
                itemTitle.text = string.Empty;
                itemSlot.text = string.Empty;
                itemText.text = string.Empty;
                itemStatus.text = string.Empty;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTurn) return;
            nextTurn = Time.unscaledTime + 1.1f;
            facing = (facing + 1) % Turn.Length;
            var set = LookLibrary.Load(Looks[look]);
            if (set != null) preview.sprite = set.directions[(int)Turn[facing]]?.idle;
        }
    }
}
