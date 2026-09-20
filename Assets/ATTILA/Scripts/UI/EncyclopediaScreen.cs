using System.Collections.Generic;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Reads the entries from the story content. Locked entries stay visible so the player sees what is still
    /// out there, and every entry shows whether its text has been checked against a source yet.
    /// </summary>
    public sealed class EncyclopediaScreen : MonoBehaviour
    {
        private Stage2Content content;
        private Image illustration;
        private Text title, category, body, status;
        private readonly List<Button> rows = new();
        private int selected;

        public static void Show(Transform root)
        {
            var host = new GameObject("Encyclopedia Screen", typeof(RectTransform));
            host.transform.SetParent(root, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            host.AddComponent<EncyclopediaScreen>().Build();
        }

        private void Build()
        {
            content = Resources.Load<Stage2Content>("Data/Stage2Content");
            MenuUiKit.Background(transform, .8f);
            MenuUiKit.Header(transform, "ЭНЦИКЛОПЕДИЯ", () => Destroy(gameObject));
            var entries = content != null ? content.encyclopediaEntries : new List<EncyclopediaEntryDefinition>();
            int unlocked = 0;
            foreach (var entry in entries) if (SaveService.IsUnlocked(entry.id)) unlocked++;
            var counter = GameUiKit.Label(transform, $"АШЫЛДЫ: {unlocked}/{entries.Count}", 24, MenuUiKit.Gold, TextAnchor.MiddleRight, 0, 0, 420, 36);
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(.88f, .88f);
            counter.rectTransform.anchoredPosition = Vector2.zero;

            var list = GameUiKit.InkPanel(transform, new Vector2(0f, .5f), new Vector2(620, 720), new Vector2(360, -20));
            for (int i = 0; i < entries.Count; i++) rows.Add(Row(list.Content, entries[i], i));

            var detail = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(1060, 720), new Vector2(-590, -20));
            illustration = UiFactory.Panel(detail.Content, Color.white);
            illustration.raycastTarget = false;
            illustration.preserveAspect = true;
            GameUiKit.PlaceTopLeft(illustration.rectTransform, 40, 40, 200, 200);
            title = GameUiKit.Label(detail.Content, string.Empty, 40, MenuUiKit.Gold, TextAnchor.UpperLeft, 270, 48, 720, 56);
            category = GameUiKit.Label(detail.Content, string.Empty, 22, MenuUiKit.Parchment, TextAnchor.UpperLeft, 270, 112, 720, 32);
            status = GameUiKit.Label(detail.Content, string.Empty, 20, GameUiKit.Ember, TextAnchor.UpperLeft, 270, 156, 720, 32);
            GameUiKit.Pixel(detail.Content, 40, 270, 960, 3, MenuUiKit.Gold);
            body = GameUiKit.Label(detail.Content, string.Empty, 26, MenuUiKit.Cream, TextAnchor.UpperLeft, 40, 300, 960, 340);
            // Open the first entry the player has actually unlocked, so the page is not empty after a win.
            int first = entries.FindIndex(entry => SaveService.IsUnlocked(entry.id));
            if (entries.Count > 0) Select(first >= 0 ? first : 0);
        }

        private Button Row(Transform parent, EncyclopediaEntryDefinition entry, int index)
        {
            bool open = SaveService.IsUnlocked(entry.id);
            var frame = GameUiKit.Panel(parent, new Vector2(.5f, 1f), new Vector2(560, 76), new Vector2(0, -(58 + index * 84)),
                open ? MenuUiKit.Burgundy : new Color(.24f, .17f, .14f), open ? MenuUiKit.Gold : new Color(.42f, .33f, .26f), 4f);
            var button = frame.GameObject.AddComponent<Button>();
            button.targetGraphic = frame.Border;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => Select(index));
            var icon = UiFactory.Panel(frame.Content, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = Illustration(entry, open);
            GameUiKit.PlaceTopLeft(icon.rectTransform, 10, 8, 52, 52);
            GameUiKit.Label(frame.Content, open ? entry.title : "ЖАБЫҚ", 24, open ? MenuUiKit.Cream : GameUiKit.Muted, TextAnchor.MiddleLeft, 78, 20, 450, 32);
            return button;
        }

        private Sprite Illustration(EncyclopediaEntryDefinition entry, bool open)
        {
            var art = MvpArtSet.Instance;
            if (!open) return art != null ? art.Icon("lock") : null;
            if (!string.IsNullOrEmpty(entry.look)) return LookLibrary.Portrait(entry.look);
            return art != null ? art.Icon(entry.icon) : null;
        }

        private void Select(int index)
        {
            selected = index;
            var entry = content.encyclopediaEntries[index];
            bool open = SaveService.IsUnlocked(entry.id);
            illustration.sprite = Illustration(entry, open);
            illustration.enabled = illustration.sprite != null;
            title.text = open ? entry.title : "ЖАБЫҚ ЖАЗБА";
            category.text = entry.category;
            body.text = open ? entry.unlockedDescription : entry.lockedDescription;
            status.text = open && entry.status == ContentStatus.Draft ? "ДЕРЕККӨЗІ ТЕКСЕРІЛУДЕ" : string.Empty;
        }
    }
}
