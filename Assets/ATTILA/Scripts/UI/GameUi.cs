using System;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Characters;
using ATTILA.Data;
using ATTILA.Gameplay;
using ATTILA.Player;
using ATTILA.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Everything the player reads while playing a chapter: role badge, objective tracker, dialogue with
    /// portrait and choices, reward toasts, mini-game banner and the chapter finale. Built at runtime on one canvas.
    /// </summary>
    public sealed class GameUi : MonoBehaviour
    {
        private const float TypeSpeed = 48f;
        private static readonly Vector2 DialogueSize = new(1500, 300);

        private Canvas canvas;
        private Text objectiveText, objectiveStep, xpText, promptText, roleName, chapterName;
        private Image rolePortrait, progressFill;
        private GameObject promptPanel;
        private readonly List<GameObject> hudRoots = new();
        private CanvasGroup toastGroup;
        private Text toastText;
        private float toastUntil;

        private GameObject dialoguePanel;
        private Image dialoguePortrait;
        private Text dialogueSpeaker, dialogueText, dialogueHint;
        private Transform choiceRoot;
        private readonly List<Button> choiceButtons = new();
        private DialogueDefinition dialogue;
        private int line;
        private float revealed;
        private Action dialogueComplete;
        private bool waitingForChoice;

        private GameObject minigamePanel;
        private Text minigameTitle, minigameRule, minigameTimer, minigameStatus;
        private Image vignette;
        private GameObject overlayRoot;

        private Stage2Content content;
        private StoryDirector director;

        public bool DialogueActive => dialogue != null || waitingForChoice;
        /// <summary>Time the last dialogue closed; interaction ignores the same key press that closed it.</summary>
        public float ClosedAt { get; private set; } = -1f;
        public Transform OverlayRoot => overlayRoot.transform;
        public Canvas Canvas => canvas;

        public void Initialize(Stage2Content stage2Content, StoryDirector storyDirector)
        {
            content = stage2Content;
            director = storyDirector;
            canvas = GameUiKit.CreateCanvas("Game UI", 10);
            canvas.transform.SetParent(transform, false);
            BuildVignette();
            BuildHud();
            BuildPrompt();
            BuildToast();
            BuildMinigame();
            BuildDialogue();
            overlayRoot = new GameObject("Overlays", typeof(RectTransform));
            overlayRoot.transform.SetParent(canvas.transform, false);
            UiFactory.Stretch((RectTransform)overlayRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------ construction

        private void BuildVignette()
        {
            vignette = GameUiKit.Vignette(canvas.transform, 3000f, .38f);
            vignette.rectTransform.anchorMin = vignette.rectTransform.anchorMax = new Vector2(.5f, .5f);
        }

        private void BuildHud()
        {
            var badge = GameUiKit.InkPanel(canvas.transform, new Vector2(0f, 1f), new Vector2(470, 132), new Vector2(255, -86));
            badge.Border.raycastTarget = false;
            rolePortrait = UiFactory.Panel(badge.Content, Color.white);
            rolePortrait.raycastTarget = false;
            rolePortrait.preserveAspect = true;
            GameUiKit.PlaceTopLeft(rolePortrait.rectTransform, 8, 8, 104, 104);
            roleName = GameUiKit.Label(badge.Content, string.Empty, 30, MenuUiKit.Cream, TextAnchor.LowerLeft, 128, 14, 320, 40);
            chapterName = GameUiKit.Label(badge.Content, string.Empty, 20, MenuUiKit.Gold, TextAnchor.UpperLeft, 128, 58, 320, 30);
            var track = UiFactory.Panel(badge.Content, MenuUiKit.Dark);
            track.raycastTarget = false;
            GameUiKit.PlaceTopLeft(track.rectTransform, 128, 92, 310, 14);
            progressFill = UiFactory.Panel(track.transform, MenuUiKit.Gold);
            progressFill.raycastTarget = false;
            UiFactory.Stretch(progressFill.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(2, 2), new Vector2(-2, -2));

            hudRoots.Add(badge.GameObject);
            var objective = GameUiKit.InkPanel(canvas.transform, new Vector2(0f, 1f), new Vector2(470, 150), new Vector2(255, -240));
            objective.Border.raycastTarget = false;
            GameUiKit.Label(objective.Content, "ТАПСЫРМА", 20, MenuUiKit.Gold, TextAnchor.UpperLeft, 18, 14, 260, 28);
            objectiveStep = GameUiKit.Label(objective.Content, string.Empty, 20, GameUiKit.Muted, TextAnchor.UpperRight, 180, 14, 240, 28);
            objectiveText = GameUiKit.Label(objective.Content, string.Empty, 26, MenuUiKit.Cream, TextAnchor.UpperLeft, 18, 50, 420, 84);

            hudRoots.Add(objective.GameObject);
            var xpPanel = GameUiKit.InkPanel(canvas.transform, new Vector2(1f, 1f), new Vector2(270, 74), new Vector2(-155, -57));
            xpPanel.Border.raycastTarget = false;
            UiFactory.Diamond(xpPanel.Content, 18f, MenuUiKit.Gold).rectTransform.anchoredPosition = new Vector2(-88, 0);
            xpText = GameUiKit.Label(xpPanel.Content, "0 XP", 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 30, 16, 200, 34);
            hudRoots.Add(xpPanel.GameObject);
            var hint = GameUiKit.Label(canvas.transform, "ESC · МӘЗІР", 20, GameUiKit.Muted, TextAnchor.UpperRight, 0, 0, 300, 30);
            hint.rectTransform.anchoredPosition = new Vector2(-290, -108);
            hudRoots.Add(hint.gameObject);
        }

        private void BuildPrompt()
        {
            var frame = GameUiKit.Panel(canvas.transform, new Vector2(.5f, 0f), new Vector2(620, 72), new Vector2(0, 150), new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .92f), MenuUiKit.Gold);
            promptPanel = frame.GameObject;
            frame.Border.raycastTarget = false;
            promptText = GameUiKit.Label(frame.Content, string.Empty, 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 10, 16, 580, 40);
            promptPanel.SetActive(false);
        }

        private void BuildToast()
        {
            var frame = GameUiKit.Panel(canvas.transform, new Vector2(.5f, 1f), new Vector2(760, 110), new Vector2(0, -110), new Color(MenuUiKit.Burgundy.r, MenuUiKit.Burgundy.g, MenuUiKit.Burgundy.b, .96f), MenuUiKit.Gold);
            frame.Border.raycastTarget = false;
            toastText = GameUiKit.Label(frame.Content, string.Empty, 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 16, 12, 716, 74);
            toastGroup = frame.GameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
        }

        private void BuildMinigame()
        {
            var frame = GameUiKit.Panel(canvas.transform, new Vector2(.5f, 1f), new Vector2(880, 116), new Vector2(0, -78), new Color(MenuUiKit.Burgundy.r, MenuUiKit.Burgundy.g, MenuUiKit.Burgundy.b, .94f), MenuUiKit.Gold);
            minigamePanel = frame.GameObject;
            frame.Border.raycastTarget = false;
            minigameTitle = GameUiKit.Label(frame.Content, string.Empty, 30, MenuUiKit.Cream, TextAnchor.UpperLeft, 24, 14, 540, 36);
            minigameRule = GameUiKit.Label(frame.Content, string.Empty, 20, MenuUiKit.Parchment, TextAnchor.UpperLeft, 24, 58, 600, 44);
            minigameTimer = GameUiKit.Label(frame.Content, string.Empty, 40, MenuUiKit.Gold, TextAnchor.MiddleRight, 620, 20, 220, 48);
            minigameStatus = GameUiKit.Label(frame.Content, string.Empty, 20, MenuUiKit.Parchment, TextAnchor.MiddleRight, 620, 66, 220, 30);
            minigamePanel.SetActive(false);
        }

        private void BuildDialogue()
        {
            var frame = GameUiKit.InkPanel(canvas.transform, new Vector2(.5f, 0f), DialogueSize, new Vector2(0, 190));
            dialoguePanel = frame.GameObject;
            var portraitFrame = GameUiKit.Panel(frame.Content, new Vector2(0f, 1f), new Vector2(204, 204), Vector2.zero, MenuUiKit.Dark, MenuUiKit.Gold, 4f);
            portraitFrame.Rect.pivot = new Vector2(0f, 1f);
            portraitFrame.Rect.anchoredPosition = new Vector2(20, -20);
            dialoguePortrait = UiFactory.Panel(portraitFrame.Content, Color.white);
            dialoguePortrait.raycastTarget = false;
            dialoguePortrait.preserveAspect = true;
            UiFactory.Stretch(dialoguePortrait.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            dialogueSpeaker = GameUiKit.NameTab(frame.Content, string.Empty, 240, -6, 420);
            dialogueText = GameUiKit.Label(frame.Content, string.Empty, 30, MenuUiKit.Cream, TextAnchor.UpperLeft, 250, 34, 1200, 150);
            dialogueHint = GameUiKit.Label(frame.Content, "E · ЖАЛҒАСТЫРУ", 20, GameUiKit.Muted, TextAnchor.LowerRight, 1150, 230, 320, 32);
            choiceRoot = new GameObject("Choices", typeof(RectTransform)).transform;
            choiceRoot.SetParent(frame.Content, false);
            UiFactory.Stretch((RectTransform)choiceRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dialoguePanel.SetActive(false);
        }

        // ------------------------------------------------------------ hud api

        public void SetChapter(HeroDefinition hero, ChapterDefinition chapter)
        {
            roleName.text = hero != null ? hero.DisplayName : string.Empty;
            chapterName.text = chapter != null ? chapter.title : string.Empty;
            rolePortrait.sprite = hero != null ? LookLibrary.Portrait(hero.VisualSetId) : null;
        }

        public void SetObjective(string text, int index, int count)
        {
            objectiveText.text = text;
            objectiveStep.text = count > 0 ? $"{Mathf.Min(index + 1, count)}/{count}" : string.Empty;
            float fill = count > 0 ? Mathf.Clamp01((float)index / count) : 0f;
            progressFill.rectTransform.anchorMax = new Vector2(fill, 1f);
        }

        public void SetXp(int value) => xpText.text = value + " XP";

        public void SetPrompt(string value)
        {
            bool show = !string.IsNullOrEmpty(value) && !DialogueActive;
            promptPanel.SetActive(show);
            if (show) promptText.text = value;
        }

        public void ShowToast(string message, float seconds = 3.2f)
        {
            toastText.text = message;
            toastUntil = Time.unscaledTime + seconds;
            AudioService.Play(Sfx.Reward, .8f);
        }

        public void SetVignette(float alpha, float size)
        {
            vignette.color = new Color(1f, 1f, 1f, alpha);
            vignette.rectTransform.sizeDelta = new Vector2(size, size);
        }

        // ------------------------------------------------------------ dialogue

        public void ShowDialogue(DialogueDefinition definition, Action complete)
        {
            if (definition == null) { complete?.Invoke(); return; }
            dialogue = definition;
            dialogueComplete = complete;
            line = 0;
            waitingForChoice = false;
            SetPrompt(string.Empty);
            dialoguePanel.SetActive(true);
            RenderLine();
        }

        private void RenderLine()
        {
            revealed = 0f;
            ClearChoices();
            dialogueSpeaker.text = dialogue.speakerName;
            var portrait = LookLibrary.Portrait(dialogue.speakerLook);
            dialoguePortrait.enabled = portrait != null;
            dialoguePortrait.sprite = portrait;
            dialogueText.text = string.Empty;
            bool last = line + 1 >= dialogue.lines.Length;
            dialogueHint.text = last && dialogue.choices.Count > 0 ? "1 · 2 · ТАҢДА" : last ? "E · ЖАБУ" : "E · ЖАЛҒАСТЫРУ";
        }

        private void ClearChoices()
        {
            foreach (var button in choiceButtons) if (button != null) Destroy(button.gameObject);
            choiceButtons.Clear();
        }

        private void ShowChoices()
        {
            waitingForChoice = true;
            dialogueText.rectTransform.sizeDelta = new Vector2(1200, 90);
            for (int i = 0; i < dialogue.choices.Count; i++)
            {
                var choice = dialogue.choices[i];
                int index = i;
                var button = MenuPixelButton.Create(choiceRoot, $"{i + 1}. {choice.label}", () => Choose(index));
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(640, 88);
                rect.anchoredPosition = new Vector2(250 + i * 660, 22);
                choiceButtons.Add(button);
            }
        }

        private void Choose(int index)
        {
            if (!waitingForChoice) return;
            var choice = dialogue.choices[index];
            waitingForChoice = false;
            ClearChoices();
            AudioService.Play(Sfx.Click);
            director.ApplyChoice(choice);
            var next = content.Dialogue(choice.nextDialogueId);
            var complete = dialogueComplete;
            if (next != null)
            {
                dialogue = next;
                line = 0;
                dialogueText.rectTransform.sizeDelta = new Vector2(1200, 150);
                RenderLine();
                return;
            }
            Close(complete);
        }

        private void Advance()
        {
            if (dialogue == null) return;
            if (revealed < dialogue.lines[line].Length) { revealed = dialogue.lines[line].Length; return; }
            bool last = line + 1 >= dialogue.lines.Length;
            if (!last)
            {
                line++;
                AudioService.Play(Sfx.Blip, .5f);
                RenderLine();
                return;
            }
            if (dialogue.choices.Count > 0) { ShowChoices(); return; }
            Close(dialogueComplete);
        }

        private void Close(Action complete)
        {
            dialogue = null;
            dialogueComplete = null;
            waitingForChoice = false;
            ClearChoices();
            dialogueText.rectTransform.sizeDelta = new Vector2(1200, 150);
            dialoguePanel.SetActive(false);
            ClosedAt = Time.unscaledTime;
            complete?.Invoke();
        }

        private void Update()
        {
            if (toastGroup != null)
            {
                float target = Time.unscaledTime < toastUntil ? 1f : 0f;
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, target, Time.unscaledDeltaTime * 4f);
            }
            if (dialogue == null || PauseMenu.IsPaused) return;
            string text = dialogue.lines[line];
            if (revealed < text.Length)
            {
                float before = revealed;
                revealed = Mathf.Min(text.Length, revealed + Time.unscaledDeltaTime * TypeSpeed);
                if (Mathf.FloorToInt(revealed) / 3 != Mathf.FloorToInt(before) / 3) AudioService.Play(Sfx.Blip, .18f, 1.4f);
            }
            dialogueText.text = text.Substring(0, Mathf.FloorToInt(revealed));
            if (waitingForChoice)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && choiceButtons.Count > 0) Choose(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2) && choiceButtons.Count > 1) Choose(1);
                return;
            }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) Advance();
        }

        /// <summary>Editor validation hook: reveals the line and takes one step, choosing the first option.</summary>
        public void FastForward()
        {
            if (waitingForChoice) { Choose(0); return; }
            if (dialogue == null) return;
            revealed = dialogue.lines[line].Length;
            Advance();
        }

        // ------------------------------------------------------------ mini-game banner

        public void ShowMinigame(string title, string rule)
        {
            minigamePanel.SetActive(true);
            minigameTitle.text = title;
            minigameRule.text = rule;
            minigameTimer.text = string.Empty;
            minigameStatus.text = string.Empty;
        }

        public void SetMinigameTime(float seconds) => minigameTimer.text = Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString("00");
        public void SetMinigameStatus(string value) => minigameStatus.text = value;
        public void HideMinigame() => minigamePanel.SetActive(false);

        // ------------------------------------------------------------ result panels

        public void ShowRetry(string message, Action retry, Action giveUp)
        {
            var host = new GameObject("Retry", typeof(RectTransform));
            host.transform.SetParent(OverlayRoot, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var veil = UiFactory.Panel(host.transform, new Color(0f, 0f, 0f, .55f));
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var frame = GameUiKit.InkPanel(host.transform, new Vector2(.5f, .5f), new Vector2(880, 380), Vector2.zero);
            GameUiKit.Label(frame.Content, "ТАҒЫ БІР МҮМКІНДІК", 34, MenuUiKit.Gold, TextAnchor.UpperCenter, 40, 34, 780, 44);
            GameUiKit.Label(frame.Content, message, 26, MenuUiKit.Cream, TextAnchor.UpperCenter, 60, 110, 740, 110);
            var again = MenuPixelButton.Create(frame.Content, "ҚАЙТАЛАУ", () => { Destroy(host); retry?.Invoke(); });
            UiFactory.SetRect(again.GetComponent<RectTransform>(), new Vector2(.32f, .18f), new Vector2(400, 92), Vector2.zero);
            var back = MenuPixelButton.Create(frame.Content, "КЕЙІНІРЕК", () => { Destroy(host); giveUp?.Invoke(); });
            UiFactory.SetRect(back.GetComponent<RectTransform>(), new Vector2(.70f, .18f), new Vector2(400, 92), Vector2.zero);
            AudioService.Play(Sfx.Fail);
        }

        public void ShowFinale(ChapterDefinition chapter, string text, int xp, int entries, Action toMenu, Action toRoles, Action toEncyclopedia)
        {
            var host = new GameObject("Finale", typeof(RectTransform));
            host.transform.SetParent(OverlayRoot, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MenuUiKit.Background(host.transform, .78f);
            var frame = GameUiKit.InkPanel(host.transform, new Vector2(.5f, .5f), new Vector2(1240, 720), Vector2.zero);
            GameUiKit.Label(frame.Content, chapter.finaleTitle, 40, MenuUiKit.Gold, TextAnchor.UpperCenter, 40, 46, 1140, 60);
            GameUiKit.Label(frame.Content, chapter.title, 26, MenuUiKit.Parchment, TextAnchor.UpperCenter, 40, 116, 1140, 40);
            GameUiKit.Pixel(frame.Content, 320, 172, 580, 3, MenuUiKit.Gold);
            GameUiKit.Label(frame.Content, text, 30, MenuUiKit.Cream, TextAnchor.UpperCenter, 90, 210, 1040, 210);
            GameUiKit.Label(frame.Content, $"ЖИНАЛҒАН XP: {xp}          ЭНЦИКЛОПЕДИЯ: +{entries}", 24, MenuUiKit.Gold, TextAnchor.UpperCenter, 90, 430, 1040, 40);
            var encyclopedia = MenuPixelButton.Create(frame.Content, "ЭНЦИКЛОПЕДИЯ", () => toEncyclopedia?.Invoke());
            UiFactory.SetRect(encyclopedia.GetComponent<RectTransform>(), new Vector2(.25f, .115f), new Vector2(400, 94), Vector2.zero);
            var roles = MenuPixelButton.Create(frame.Content, "БАСҚА КЕЙІПКЕР", () => toRoles?.Invoke());
            UiFactory.SetRect(roles.GetComponent<RectTransform>(), new Vector2(.5f, .115f), new Vector2(420, 94), Vector2.zero);
            var menu = MenuPixelButton.Create(frame.Content, "БАСТЫ МӘЗІР", () => toMenu?.Invoke());
            UiFactory.SetRect(menu.GetComponent<RectTransform>(), new Vector2(.75f, .115f), new Vector2(400, 94), Vector2.zero);
            AudioService.Play(Sfx.Reward);
        }

        /// <summary>Hides the badge, tracker and XP pill while a full-screen story screen is open.</summary>
        public void SetHudVisible(bool visible)
        {
            foreach (var root in hudRoots) if (root != null) root.SetActive(visible);
            if (!visible) promptPanel.SetActive(false);
        }
    }
}
