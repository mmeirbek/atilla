using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    public sealed class SettingsScreen : MonoBehaviour
    {
        public static void Show(Transform root)
        {
            var host = new GameObject("Settings Screen");
            host.transform.SetParent(root, false);
            host.AddComponent<SettingsScreen>().Build();
        }

        private void Build()
        {
            MenuUiKit.Background(transform);
            MenuUiKit.Header(transform, "БАПТАУЛАР", () => Destroy(gameObject));
            var panel = MenuUiKit.Panel(transform, new Vector2(.5f, .50f), new Vector2(760, 510));
            CreateSetting(panel.transform, "ДЫБЫС", "100%", .70f);
            CreateSetting(panel.transform, "МУЗЫКА", "100%", .49f);
            CreateSetting(panel.transform, "ТОЛЫҚ ЭКРАН", "ҚОСУЛЫ", .28f);
            var note = UiFactory.Text(panel.transform, "Уақытша presentation controls", 16, TextAnchor.MiddleCenter, MenuUiKit.Parchment);
            UiFactory.SetRect(note.rectTransform, new Vector2(.5f, .10f), new Vector2(520, 30), Vector2.zero);
        }

        private static void CreateSetting(Transform root, string title, string state, float y)
        {
            var text = UiFactory.Text(root, title, 23, TextAnchor.MiddleLeft, MenuUiKit.Cream);
            text.fontStyle = FontStyle.Bold;
            UiFactory.SetRect(text.rectTransform, new Vector2(.16f, y), new Vector2(230, 40), Vector2.zero);
            var bar = MenuUiKit.Panel(root, new Vector2(.58f, y), new Vector2(310, 28), true);
            var marker = UiFactory.Panel(bar.transform, MenuUiKit.Gold);
            UiFactory.SetRect(marker.rectTransform, new Vector2(.78f, .5f), new Vector2(18, 18), Vector2.zero);
            var value = UiFactory.Text(root, state, 18, TextAnchor.MiddleCenter, MenuUiKit.Gold);
            UiFactory.SetRect(value.rectTransform, new Vector2(.84f, y), new Vector2(135, 35), Vector2.zero);
        }
    }
}
