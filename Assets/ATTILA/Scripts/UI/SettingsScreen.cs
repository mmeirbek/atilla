using ATTILA.Audio;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Working volume sliders and a progress reset; every value lives in the save file.</summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private Text musicValue, sfxValue, resetNote;

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
            var panel = GameUiKit.InkPanel(transform, new Vector2(.5f, .5f), new Vector2(1000, 620), new Vector2(0, -10));
            musicValue = Slider(panel.Content, "МУЗЫКА", .74f, SaveService.Current.musicVolume, value =>
            {
                AudioService.SetMusicVolume(value);
                musicValue.text = Percent(value);
            });
            sfxValue = Slider(panel.Content, "ДЫБЫС", .54f, SaveService.Current.sfxVolume, value =>
            {
                AudioService.SetSfxVolume(value);
                sfxValue.text = Percent(value);
            });
            var reset = MenuPixelButton.Create(panel.Content, "ПРОГРЕСТІ ТАЗАЛАУ", () =>
            {
                SaveService.ResetForTests();
                AudioService.SetMusicVolume(SaveService.Current.musicVolume);
                resetNote.text = "Прогресс тазартылды. Барлық тарау қайта басталады.";
            });
            UiFactory.SetRect(reset.GetComponent<RectTransform>(), new Vector2(.5f, .27f), new Vector2(520, 80), Vector2.zero);
            resetNote = GameUiKit.Label(panel.Content, "Дыбыс деңгейі сақтау файлында сақталады.", 20, GameUiKit.Muted, TextAnchor.MiddleCenter, 60, 520, 880, 34);
        }

        private Text Slider(Transform parent, string label, float y, float value, System.Action<float> changed)
        {
            var title = GameUiKit.Label(parent, label, 28, MenuUiKit.Cream, TextAnchor.MiddleLeft, 0, 0, 300, 40);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(.18f, y);
            title.rectTransform.anchoredPosition = Vector2.zero;
            var readout = GameUiKit.Label(parent, Percent(value), 26, MenuUiKit.Gold, TextAnchor.MiddleRight, 0, 0, 160, 40);
            readout.rectTransform.anchorMin = readout.rectTransform.anchorMax = new Vector2(.9f, y);
            readout.rectTransform.anchoredPosition = Vector2.zero;
            GameUiKit.Slider(parent, new Vector2(.58f, y), new Vector2(500, 34), value, changed);
            return readout;
        }

        private static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

        private void OnDestroy() => SaveService.Save();
    }
}
