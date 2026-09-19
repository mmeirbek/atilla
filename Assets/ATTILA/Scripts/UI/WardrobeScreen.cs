using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    public sealed class WardrobeScreen : MonoBehaviour
    {
        public static void Show(Transform root)
        {
            var host = new GameObject("Wardrobe Screen");
            host.transform.SetParent(root, false);
            host.AddComponent<WardrobeScreen>().Build();
        }

        private void Build()
        {
            MenuUiKit.Background(transform);
            MenuUiKit.Header(transform, "ГАРДЕРОБ", () => Destroy(gameObject));
            var preview = MenuUiKit.Panel(transform, new Vector2(.32f, .49f), new Vector2(520, 650), true);
            var visuals = Resources.Load<MenuVisualDefinition>("UI/MainMenuVisuals");
            MenuUiKit.Portrait(preview.transform, visuals != null ? visuals.character : null, visuals != null ? visuals.characterMaterial : null, new Vector2(.5f, .53f), new Vector2(350, 520));
            var label = UiFactory.Text(preview.transform, "УАҚЫТША КЕЙІП", 20, TextAnchor.MiddleCenter, MenuUiKit.Gold);
            UiFactory.SetRect(label.rectTransform, new Vector2(.5f, .08f), new Vector2(400, 36), Vector2.zero);

            var grid = MenuUiKit.Panel(transform, new Vector2(.72f, .49f), new Vector2(600, 650));
            CreateSlot(grid.transform, "БАС КИІМ", "Тақия", new Vector2(.29f, .73f));
            CreateSlot(grid.transform, "КИІМ", "Шапан", new Vector2(.71f, .73f));
            CreateSlot(grid.transform, "САУЫТ", "Уақытша сауыт", new Vector2(.29f, .38f));
            CreateSlot(grid.transform, "АЯҚ КИІМ", "Етік", new Vector2(.71f, .38f));
        }

        private static void CreateSlot(Transform root, string category, string item, Vector2 anchor)
        {
            var card = MenuUiKit.Card(root, category, item, anchor, new Vector2(230, 190), () => { });
            var icon = UiFactory.Text(card.transform, "◆", 42, TextAnchor.MiddleCenter, MenuUiKit.Gold);
            icon.raycastTarget = false;
            UiFactory.SetRect(icon.rectTransform, new Vector2(.5f, .67f), new Vector2(80, 70), Vector2.zero);
        }
    }
}
