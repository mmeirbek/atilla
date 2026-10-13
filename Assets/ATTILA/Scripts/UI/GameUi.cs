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
        private readonly Image[] vignetteSides = new Image[4];
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
            TouchControls.Attach(canvas.transform);
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
            // The sprite is a square; these bands carry its dark edge out to the screen border when it is shrunk.
            const float far = 6000f;
            var edges = new[]
            {
                (new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-far, -far), new Vector2(0f, far)),
                (new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, -far), new Vector2(far, far)),
                (new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, far)),
                (new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -far), Vector2.zero)
            };
            for (int i = 0; i < edges.Length; i++)
            {
                var side = UiFactory.Panel(vignette.transform, new Color(0f, 0f, 0f, .38f));
                side.name = "Vignette edge";
                side.raycastTarget = false;
                UiFactory.Stretch(side.rectTransform, edges[i].Item1, edges[i].Item2, edges[i].Item3, edges[i].Item4);
                vignetteSides[i] = side;
            }
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
            UiFactory.ShrinkToFit(roleName, 30);
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
            foreach (var side in vignetteSides) if (side != null) side.color = new Color(0f, 0f, 0f, alpha);
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
                rect.sizeDelta = new Vector2(680, 104);
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
                // Drops below the mini-game banner while one is running, instead of hiding behind it.
                ((RectTransform)toastGroup.transform).anchoredPosition = new Vector2(0f, minigamePanel.activeSelf ? -232f : -110f);
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
            if (VirtualInput.Action() || Input.GetMouseButtonDown(0)) Advance();
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

        /// <summary>The clock turns red and beats in the last ten seconds.</summary>
        public void SetMinigameTime(float seconds)
        {
            minigameTimer.text = Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString("00");
            bool hurry = seconds > 0f && seconds <= 10f;
            float beat = hurry ? Mathf.Repeat(seconds, 1f) : 0f;
            minigameTimer.color = hurry ? Color.Lerp(MenuUiKit.Gold, new Color(1f, .3f, .25f), beat) : MenuUiKit.Gold;
            minigameTimer.rectTransform.localScale = Vector3.one * (hurry ? 1f + beat * beat * .25f : 1f);
        }
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
            UiFactory.SetRect(again.GetComponent<RectTransform>(), new Vector2(.32f, .18f), new Vector2(440, 106), Vector2.zero);
            var back = MenuPixelButton.Create(frame.Content, "КЕЙІНІРЕК", () => { Destroy(host); giveUp?.Invoke(); });
            UiFactory.SetRect(back.GetComponent<RectTransform>(), new Vector2(.70f, .18f), new Vector2(440, 106), Vector2.zero);
            AudioService.Play(Sfx.Fail);
        }

        public void ShowFinale(ChapterDefinition chapter, string text, int xp, int entries, Action toMenu, Action toRoles, Action toEncyclopedia)
        {
            var host = new GameObject("Finale", typeof(RectTransform));
            host.transform.SetParent(OverlayRoot, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MenuUiKit.Background(host.transform, .72f);
            var group = host.AddComponent<CanvasGroup>();
            var frame = GameUiKit.InkPanel(host.transform, new Vector2(.5f, .5f), new Vector2(1540, 820), Vector2.zero);

            // The role's card on the left, with its name lettered into the card's empty lower field.
            var hero = director != null ? director.Hero : null;
            var card = UiFactory.Panel(frame.Content, Color.white);
            card.raycastTarget = false;
            card.sprite = hero != null ? hero.Portrait : null;
            card.preserveAspect = true;
            GameUiKit.PlaceTopLeft(card.rectTransform, 34, 34, 492, 738);
            if (hero != null)
            {
                var name = GameUiKit.Label(card.transform, hero.DisplayName, 40, MenuUiKit.Gold, TextAnchor.MiddleCenter, 40, 520, 412, 56);
                UiFactory.ShrinkToFit(name, 40);
                GameUiKit.Label(card.transform, hero.Era, 24, MenuUiKit.Parchment, TextAnchor.MiddleCenter, 40, 590, 412, 34);
            }

            const float x = 580f, width = 900f;
            GameUiKit.Label(frame.Content, chapter.finaleTitle, 48, MenuUiKit.Gold, TextAnchor.UpperCenter, x, 56, width, 64);
            GameUiKit.Label(frame.Content, chapter.title + "  ·  " + chapter.subtitle, 26, MenuUiKit.Parchment, TextAnchor.UpperCenter, x, 140, width, 40);
            GameUiKit.Pixel(frame.Content, x + 160, 200, width - 320, 3, MenuUiKit.Gold);
            GameUiKit.Label(frame.Content, text, 30, MenuUiKit.Cream, TextAnchor.UpperCenter, x + 20, 240, width - 40, 250);

            var xpLabel = GameUiKit.Label(frame.Content, "XP  0", 40, MenuUiKit.Gold, TextAnchor.MiddleCenter, x, 500, width * .5f, 60);
            GameUiKit.Label(frame.Content, "ЖИНАЛҒАН ТӘЖІРИБЕ", 20, GameUiKit.Muted, TextAnchor.MiddleCenter, x, 562, width * .5f, 30);
            GameUiKit.Label(frame.Content, "+" + entries, 40, MenuUiKit.Gold, TextAnchor.MiddleCenter, x + width * .5f, 500, width * .5f, 60);
            GameUiKit.Label(frame.Content, "ЭНЦИКЛОПЕДИЯ ЖАЗБАСЫ", 20, GameUiKit.Muted, TextAnchor.MiddleCenter, x + width * .5f, 562, width * .5f, 30);

            var actions = new (string label, Action action)[] { ("ЭНЦИКЛОПЕДИЯ", toEncyclopedia), ("БАСҚА КЕЙІПКЕР", toRoles), ("БАСТЫ МӘЗІР", toMenu) };
            // The single long word gets the wide button, so all three labels keep the same type size.
            float[] widths = { 352f, 268f, 268f };
            float left = x;
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i].action;
                var button = MenuPixelButton.Create(frame.Content, actions[i].label, () => action?.Invoke(), 30);
                GameUiKit.PlaceTopLeft(button.GetComponent<RectTransform>(), left, 650, widths[i], 104);
                left += widths[i] + 6f;
            }
            StartCoroutine(RevealFinale(group, xpLabel, xp));
            AudioService.Play(Sfx.Reward);
        }

        /// <summary>
        /// Title card that opens a chapter: the world waits behind a dark veil while the chapter name, century and a
        /// line of context fade in; then the veil lifts. Any action key skips it once the title is readable.
        /// </summary>
        public void ShowChapterIntro(HeroDefinition hero, ChapterDefinition chapter, bool showControls, Action done)
        {
            var host = new GameObject("Chapter Intro", typeof(RectTransform));
            host.transform.SetParent(OverlayRoot, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var veil = UiFactory.Panel(host.transform, new Color(.07f, .04f, .03f, 1f));
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hostGroup = host.AddComponent<CanvasGroup>();

            var words = new GameObject("Words", typeof(RectTransform));
            words.transform.SetParent(host.transform, false);
            UiFactory.Stretch((RectTransform)words.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var wordsGroup = words.AddComponent<CanvasGroup>();
            wordsGroup.alpha = 0f;
            string era = chapter != null ? chapter.subtitle : string.Empty;
            Centered(words.transform, hero != null ? hero.DisplayName.ToUpperInvariant() : string.Empty, 28, MenuUiKit.Gold, .70f, 1200, 44);
            Centered(words.transform, chapter != null ? chapter.title.ToUpperInvariant() : string.Empty, 64, MenuUiKit.Cream, .60f, 1600, 90);
            var rule = UiFactory.Panel(words.transform, MenuUiKit.Gold);
            rule.raycastTarget = false;
            UiFactory.SetRect(rule.rectTransform, new Vector2(.5f, .525f), new Vector2(520, 4), Vector2.zero);
            var diamond = UiFactory.Diamond(words.transform, 18f, MenuUiKit.Gold);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(.5f, .525f);
            diamond.rectTransform.anchoredPosition = Vector2.zero;
            Centered(words.transform, era, 30, MenuUiKit.Parchment, .47f, 1200, 48);
            if (chapter != null && !string.IsNullOrEmpty(chapter.intro))
                Centered(words.transform, chapter.intro, 26, MenuUiKit.Cream, .36f, 1240, 140);
            if (showControls)
                Centered(words.transform, "WASD — ЖҮРУ    SHIFT — ЖҮГІРУ    E — СӨЙЛЕСУ    ESC — МӘЗІР", 22, GameUiKit.Muted, .14f, 1600, 40);
            introHost = host;
            introDone = done;
            introRoutine = StartCoroutine(PlayIntro(host, hostGroup, wordsGroup));
        }

        private GameObject introHost;
        private Action introDone;
        private Coroutine introRoutine;

        /// <summary>Ends the title card at once; used by the presenter keys and the editor validation.</summary>
        public bool SkipIntro()
        {
            if (introHost == null) return false;
            if (introRoutine != null) StopCoroutine(introRoutine);
            EndIntro();
            return true;
        }

        private void EndIntro()
        {
            Destroy(introHost);
            introHost = null;
            introRoutine = null;
            ClosedAt = Time.unscaledTime;
            var done = introDone;
            introDone = null;
            done?.Invoke();
        }

        private static Text Centered(Transform parent, string value, int size, Color color, float y, float width, float height)
        {
            var text = UiFactory.Text(parent, value, size, TextAnchor.MiddleCenter, color);
            text.raycastTarget = false;
            UiFactory.SetRect(text.rectTransform, new Vector2(.5f, y), new Vector2(width, height), Vector2.zero);
            return text;
        }

        private System.Collections.IEnumerator PlayIntro(GameObject host, CanvasGroup hostGroup, CanvasGroup wordsGroup)
        {
            const float fadeIn = .9f, hold = 4.2f, fadeOut = 1.1f, skippableAfter = .8f;
            float time = 0f;
            AudioService.Play(Sfx.Ring, .55f, .8f);
            while (time < fadeIn + hold)
            {
                time += Time.unscaledDeltaTime;
                if (wordsGroup == null) break;
                wordsGroup.alpha = Mathf.Clamp01(time / fadeIn);
                if (time > skippableAfter && (VirtualInput.Action() || Input.GetMouseButtonDown(0))) break;
                yield return null;
            }
            if (wordsGroup != null) wordsGroup.alpha = 1f;
            for (float t = 0f; t < 1f && hostGroup != null; t += Time.unscaledDeltaTime / fadeOut)
            {
                hostGroup.alpha = 1f - t;
                yield return null;
            }
            EndIntro();
        }

        /// <summary>Dips the screen to dark and back, running <paramref name="middle"/> while nothing can be seen.</summary>
        public void FadeThrough(Action middle, Action done = null) => StartCoroutine(Fade(middle, done));

        private System.Collections.IEnumerator Fade(Action middle, Action done)
        {
            var host = new GameObject("Fade", typeof(RectTransform));
            host.transform.SetParent(OverlayRoot, false);
            UiFactory.Stretch((RectTransform)host.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var veil = UiFactory.Panel(host.transform, new Color(.07f, .04f, .03f, 0f));
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // The callbacks always run, even if something clears the overlays mid-fade: the ride and the
            // release of input must never be left half done.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .35f)
            {
                if (veil != null) veil.color = new Color(.07f, .04f, .03f, t);
                yield return null;
            }
            if (veil != null) veil.color = new Color(.07f, .04f, .03f, 1f);
            middle?.Invoke();
            yield return null;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .5f)
            {
                if (veil != null) veil.color = new Color(.07f, .04f, .03f, 1f - t);
                yield return null;
            }
            if (host != null) Destroy(host);
            done?.Invoke();
        }

        /// <summary>Fades the finale in, then counts the chapter's XP up, so the result lands rather than just appears.</summary>
        private System.Collections.IEnumerator RevealFinale(CanvasGroup group, Text xpLabel, int xp)
        {
            group.alpha = 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .8f)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.4f)
            {
                xpLabel.text = "XP  " + Mathf.RoundToInt(Mathf.SmoothStep(0f, xp, t));
                yield return null;
            }
            xpLabel.text = "XP  " + xp;
        }

        /// <summary>Hides the badge, tracker and XP pill while a full-screen story screen is open.</summary>
        public void SetHudVisible(bool visible)
        {
            foreach (var root in hudRoots) if (root != null) root.SetActive(visible);
            if (!visible) promptPanel.SetActive(false);
        }
    }
}
