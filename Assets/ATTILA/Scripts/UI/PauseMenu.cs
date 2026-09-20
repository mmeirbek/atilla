using ATTILA.Audio;
using ATTILA.Core;
using ATTILA.Gameplay;
using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>Esc menu. Freezes the world with timeScale, so every menu animation runs on unscaled time.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        private StoryDirector director;
        private Transform menuRoot;
        private Canvas canvas;

        public void Initialize(StoryDirector value)
        {
            director = value;
            IsPaused = false;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsPaused) Resume();
            else if (!director.Ui.DialogueActive) Open();
        }

        private void Open()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            AudioService.Play(Sfx.Click);
            canvas = GameUiKit.CreateCanvas("Pause Canvas", 40);
            canvas.transform.SetParent(transform, false);
            var veil = UiFactory.Panel(canvas.transform, new Color(.06f, .03f, .03f, .72f));
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var frame = GameUiKit.InkPanel(canvas.transform, new Vector2(.5f, .5f), new Vector2(760, 720), Vector2.zero);
            GameUiKit.Label(frame.Content, "ҮЗІЛІС", 40, MenuUiKit.Gold, TextAnchor.UpperCenter, 30, 40, 680, 56);
            GameUiKit.Pixel(frame.Content, 200, 112, 340, 3, MenuUiKit.Gold);
            menuRoot = frame.Content;
            GameUiKit.Label(frame.Content, director.Chapter != null ? director.Chapter.title : string.Empty, 22, MenuUiKit.Parchment, TextAnchor.UpperCenter, 30, 130, 680, 34);
            Button("ЖАЛҒАСТЫРУ", .70f, Resume);
            Button("ЭНЦИКЛОПЕДИЯ", .58f, () => EncyclopediaScreen.Show(canvas.transform));
            Button("БАПТАУЛАР", .46f, () => SettingsScreen.Show(canvas.transform));
            Button("ТАРАУДЫ ҚАЙТА БАСТАУ", .34f, () => { Resume(); director.RestartChapter(); });
            Button("БАСТЫ МӘЗІР", .22f, () => { Resume(); SceneLoader.Load(SceneId.MainMenu); });
        }

        private void Button(string label, float y, System.Action action)
        {
            var button = MenuPixelButton.Create(menuRoot, label, action);
            UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, y), new Vector2(600, 82), Vector2.zero);
        }

        private void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (canvas != null) Destroy(canvas.gameObject);
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
            IsPaused = false;
        }
    }
}
