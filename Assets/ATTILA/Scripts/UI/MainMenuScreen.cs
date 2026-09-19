using ATTILA.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private static readonly Vector2 MenuButtonSize = new Vector2(560, 120);

        private void Start()
        {
            var canvas = MenuUiKit.Canvas("Main Menu Canvas");
            MenuUiKit.Background(canvas.transform, .10f);
            var visuals = Resources.Load<MenuVisualDefinition>("UI/MainMenuVisuals");
            CreateLogo(canvas.transform, visuals);
            PlayButton(canvas.transform, visuals);
            ArtButton(canvas.transform, "ГАРДЕРОБ Provided Pixel Button", visuals != null ? visuals.wardrobeButton : null, .465f, MenuButtonSize, () => WardrobeScreen.Show(canvas.transform));
            ArtButton(canvas.transform, "ЭНЦИКЛОПЕДИЯ Provided Pixel Button", visuals != null ? visuals.encyclopediaButton : null, .35f, MenuButtonSize, () => EncyclopediaScreen.Show(canvas.transform));
            ArtButton(canvas.transform, "БАПТАУЛАР Provided Pixel Button", visuals != null ? visuals.settingsButton : null, .235f, MenuButtonSize, () => SettingsScreen.Show(canvas.transform));
            ArtButton(canvas.transform, "ШЫҒУ Provided Pixel Button", visuals != null ? visuals.quitButton : null, .12f, MenuButtonSize, QuitSafely);
        }

        private static void PlayButton(Transform root, MenuVisualDefinition visuals)
        {
            if (visuals != null && visuals.playButton != null)
            {
                var go = new GameObject("ОЙНАУ Provided Pixel Button", typeof(Image), typeof(Button));
                go.transform.SetParent(root, false);
                var image = go.GetComponent<Image>();
                image.sprite = visuals.playButton;
                image.preserveAspect = true;
                go.AddComponent<MenuImageButtonMotion>();
                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SceneLoader.Load(SceneId.CharacterSelect));
                UiFactory.SetRect(image.rectTransform, new Vector2(.5f, .58f), MenuButtonSize, Vector2.zero);
                return;
            }
            CreateButton(root, "ОЙНАУ", .525f, new Vector2(560, 82), () => SceneLoader.Load(SceneId.CharacterSelect));
        }

        private static void CreateLogo(Transform root, MenuVisualDefinition visuals)
        {
            if (visuals != null && visuals.logo != null)
            {
                var go = new GameObject("ATTILA Pixel Logo", typeof(Image));
                go.transform.SetParent(root, false);
                var image = go.GetComponent<Image>();
                image.sprite = visuals.logo;
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                UiFactory.SetRect(image.rectTransform, new Vector2(.5f, .82f), new Vector2(1750, 400), Vector2.zero);
                return;
            }
            var fallback = new GameObject("ATTILA Pixel Logo Fallback", typeof(PixelWordmark)).GetComponent<PixelWordmark>();
            fallback.transform.SetParent(root, false);
            fallback.color = MenuUiKit.Gold;
            fallback.raycastTarget = false;
            fallback.SetValue("ATTILA");
            UiFactory.SetRect(fallback.rectTransform, new Vector2(.5f, .775f), new Vector2(1600, 450), Vector2.zero);
        }

        private static void CreateButton(Transform root, string label, float y, Vector2 size, System.Action action)
        { var button = MenuPixelButton.Create(root, label, action); UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, y), size, Vector2.zero); }

        private static void ArtButton(Transform root, string name, Sprite sprite, float y, Vector2 size, System.Action action)
        {
            if (sprite == null)
            {
                CreateButton(root, name.Replace(" Provided Pixel Button", string.Empty), y, size, action);
                return;
            }

            var go = new GameObject(name, typeof(Image), typeof(Button), typeof(MenuImageButtonMotion));
            go.transform.SetParent(root, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            UiFactory.SetRect(image.rectTransform, new Vector2(.5f, y), size, Vector2.zero);
        }

        private static void QuitSafely()
        {
#if UNITY_EDITOR
            Debug.Log("ATTILA: Quit requested (ignored safely in the Unity Editor).");
#else
            Application.Quit();
#endif
        }
    }
}
