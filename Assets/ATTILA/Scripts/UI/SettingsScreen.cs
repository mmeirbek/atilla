using ATTILA.Audio;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Working volume sliders and a progress reset; every value lives in the save file.</summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const float Row = 150f;
        private Text resetNote;

        public static void Show(Transform root)
        {
            var host = new GameObject("Settings Screen", typeof(RectTransform));
            host.transform.SetParent(root, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            host.AddComponent<SettingsScreen>().Build();
        }

        private void Build()
        {
            MenuUiKit.Background(transform, .82f);
            MenuUiKit.Header(transform, "БАПТАУЛАР", () => { SaveService.Save(); Destroy(gameObject); });
            var panel = GameUiKit.InkPanel(transform, new Vector2(.5f, .5f), new Vector2(1180, 660), new Vector2(0, -20));
            GameUiKit.Label(panel.Content, "ДЫБЫС", 26, MenuUiKit.Gold, TextAnchor.UpperLeft, 60, 44, 500, 36);
            GameUiKit.Pixel(panel.Content, 60, 92, 1040, 3, MenuUiKit.Gold);
            Slider(panel.Content, "МУЗЫКА", 130f, SaveService.Current.musicVolume, AudioService.SetMusicVolume);
            Slider(panel.Content, "ДЫБЫС ЭФФЕКТІ", 130f + Row, SaveService.Current.sfxVolume, value =>
            {
                AudioService.SetSfxVolume(value);
                AudioService.Play(Sfx.Hover, 1f);
            });

            GameUiKit.Label(panel.Content, "ОЙЫН ДЕРЕКТЕРІ", 26, MenuUiKit.Gold, TextAnchor.UpperLeft, 60, 420, 500, 36);
            GameUiKit.Pixel(panel.Content, 60, 468, 1040, 3, MenuUiKit.Gold);
            var reset = MenuPixelButton.Create(panel.Content, "ПРОГРЕСТІ ТАЗАЛАУ", () =>
            {
                SaveService.ResetForTests();
                AudioService.SetMusicVolume(SaveService.Current.musicVolume);
                resetNote.text = "Прогресс тазартылды. Барлық тарау қайтадан басталады.";
            });
            var rect = reset.GetComponent<RectTransform>();
            GameUiKit.PlaceTopLeft(rect, 60, 492, 560, 104);
            resetNote = GameUiKit.Label(panel.Content, "Дыбыс деңгейі мен прогресс сақтау файлында сақталады.", 20, GameUiKit.Muted, TextAnchor.UpperLeft, 660, 512, 440, 80);
        }

        /// <summary>One row: caption on the left, track in the middle, live percentage on the right.</summary>
        private void Slider(Transform parent, string label, float y, float value, System.Action<float> changed)
        {
            GameUiKit.Label(parent, label, 26, MenuUiKit.Cream, TextAnchor.MiddleLeft, 60, y, 380, 44);
            var readout = GameUiKit.Label(parent, Percent(value), 26, MenuUiKit.Gold, TextAnchor.MiddleRight, 960, y, 140, 44);
            var track = GameUiKit.Panel(parent, new Vector2(0f, 1f), new Vector2(480, 40), Vector2.zero, MenuUiKit.Dark, MenuUiKit.Gold, 4f);
            track.Rect.pivot = new Vector2(0f, 1f);
            track.Rect.anchoredPosition = new Vector2(450, -(y + 2));
            var fillArea = UiFactory.Panel(track.Content, Color.clear);
            fillArea.raycastTarget = false;
            UiFactory.Stretch(fillArea.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            var fill = UiFactory.Panel(fillArea.transform, MenuUiKit.Gold);
            fill.raycastTarget = false;
            UiFactory.Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleArea = UiFactory.Panel(track.Content, Color.clear);
            handleArea.raycastTarget = false;
            UiFactory.Stretch(handleArea.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, 0));
            var handle = UiFactory.Panel(handleArea.transform, MenuUiKit.Cream);
            handle.rectTransform.sizeDelta = new Vector2(26, 54);
            var slider = track.GameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(next =>
            {
                changed?.Invoke(next);
                readout.text = Percent(next);
            });
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

        private void OnDestroy() => SaveService.Save();
    }
}
