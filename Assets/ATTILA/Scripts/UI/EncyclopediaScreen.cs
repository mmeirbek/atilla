using System.Collections.Generic;
using System.Linq;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Only entries the player can read are listed: common knowledge is open from the start and the rest
    /// appears as the chapters give it. Each entry says whether its text has been checked against a source.
    /// </summary>
    public sealed class EncyclopediaScreen : MonoBehaviour
    {
        private static readonly string[] Categories = { "ТҰЛҒАЛАР", "ОҚИҒАЛАР", "МӘДЕНИЕТ" };

        private Stage2Content content;
        private Transform listRoot;
        private Image illustration;
        private Text title, category, body, status, emptyNote;
        private readonly List<GameObject> rows = new();
        private readonly List<Button> tabs = new();
        private int tab;

        public static void Show(Transform root)
        {
            var host = new GameObject("Encyclopedia Screen", typeof(RectTransform));
            host.transform.SetParent(root, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            host.AddComponent<EncyclopediaScreen>().Build();
        }

        private static bool IsOpen(EncyclopediaEntryDefinition entry) => entry.openFromStart || SaveService.IsUnlocked(entry.id);

        private void Build()
        {
            content = Resources.Load<Stage2Content>("Data/Stage2Content");
            MenuUiKit.Background(transform, .82f);
            MenuUiKit.Header(transform, "ЭНЦИКЛОПЕДИЯ", () => Destroy(gameObject));
            var all = content != null ? content.encyclopediaEntries : new List<EncyclopediaEntryDefinition>();
            int open = all.Count(IsOpen);
            var counter = GameUiKit.Label(transform, $"АШЫЛДЫ: {open}/{all.Count}", 24, MenuUiKit.Gold, TextAnchor.MiddleRight, 0, 0, 460, 36);
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(.86f, .795f);
            counter.rectTransform.anchoredPosition = Vector2.zero;

            for (int i = 0; i < Categories.Length; i++)
            {
                int index = i;
                var button = MenuPixelButton.Create(transform, Categories[i], () => SetTab(index));
                UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.245f + i * .17f, .795f), new Vector2(310, 76), Vector2.zero);
                tabs.Add(button);
            }

            var list = GameUiKit.InkPanel(transform, new Vector2(0f, .5f), new Vector2(620, 620), new Vector2(360, -96));
            listRoot = list.Content;
            emptyNote = GameUiKit.Label(list.Content, string.Empty, 22, GameUiKit.Muted, TextAnchor.UpperCenter, 40, 60, 540, 90);

            var detail = GameUiKit.InkPanel(transform, new Vector2(1f, .5f), new Vector2(1060, 620), new Vector2(-590, -96));
            illustration = UiFactory.Panel(detail.Content, Color.white);
            illustration.raycastTarget = false;
            illustration.preserveAspect = true;
            GameUiKit.PlaceTopLeft(illustration.rectTransform, 44, 44, 190, 190);
            title = GameUiKit.Label(detail.Content, string.Empty, 40, MenuUiKit.Gold, TextAnchor.UpperLeft, 266, 52, 720, 56);
            category = GameUiKit.Label(detail.Content, string.Empty, 22, MenuUiKit.Parchment, TextAnchor.UpperLeft, 266, 116, 720, 32);
            status = GameUiKit.Label(detail.Content, string.Empty, 20, GameUiKit.Ember, TextAnchor.UpperLeft, 266, 160, 720, 32);
            GameUiKit.Pixel(detail.Content, 44, 258, 950, 3, MenuUiKit.Gold);
            body = GameUiKit.Label(detail.Content, string.Empty, 26, MenuUiKit.Cream, TextAnchor.UpperLeft, 44, 290, 950, 330);
            SetTab(0);
        }

        private List<EncyclopediaEntryDefinition> Visible(int index) =>
            content == null ? new List<EncyclopediaEntryDefinition>()
                : content.encyclopediaEntries.Where(entry => entry.category == Categories[index] && IsOpen(entry)).ToList();

        private void SetTab(int index)
        {
            tab = index;
            foreach (var row in rows) Destroy(row);
            rows.Clear();
            var entries = Visible(index);
            for (int i = 0; i < entries.Count; i++) Row(entries[i], i);
            emptyNote.text = entries.Count == 0 ? "Бұл бөлімдегі жазбалар ойын барысында ашылады." : string.Empty;
            if (entries.Count > 0) Select(entries[0]);
            else
            {
                illustration.enabled = false;
                title.text = "ЖАЗБА ЖОҚ";
                category.text = Categories[index];
                body.text = "Тарауларды ойнаған сайын жаңа жазбалар қосылады.";
                status.text = string.Empty;
            }
        }

        private void Row(EncyclopediaEntryDefinition entry, int index)
        {
            var frame = GameUiKit.Panel(listRoot, new Vector2(.5f, 1f), new Vector2(560, 92), new Vector2(0, -(66 + index * 104)), MenuUiKit.Burgundy, MenuUiKit.Gold, 4f);
            var button = frame.GameObject.AddComponent<Button>();
            button.targetGraphic = frame.Border;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => Select(entry));
            var icon = UiFactory.Panel(frame.Content, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = Illustration(entry);
            GameUiKit.PlaceTopLeft(icon.rectTransform, 12, 10, 64, 64);
            GameUiKit.Label(frame.Content, entry.title, 26, MenuUiKit.Cream, TextAnchor.MiddleLeft, 92, 28, 430, 36);
            rows.Add(frame.GameObject);
        }

        private Sprite Illustration(EncyclopediaEntryDefinition entry)
        {
            if (!string.IsNullOrEmpty(entry.look)) return LookLibrary.Portrait(entry.look);
            var art = MvpArtSet.Instance;
            return art != null ? art.Icon(entry.icon) : null;
        }

        private void Select(EncyclopediaEntryDefinition entry)
        {
            illustration.sprite = Illustration(entry);
            illustration.enabled = illustration.sprite != null;
            title.text = entry.title;
            category.text = entry.category;
            body.text = entry.unlockedDescription;
            status.text = entry.status == ContentStatus.Draft ? "ДЕРЕККӨЗІ ТЕКСЕРІЛУДЕ" : string.Empty;
        }
    }
}
