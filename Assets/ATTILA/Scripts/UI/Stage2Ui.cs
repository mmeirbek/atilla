using System;
using ATTILA.Data;
using ATTILA.Gameplay;
using ATTILA.Quest;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    public sealed class Stage2Ui : MonoBehaviour
    {
        private Canvas canvas;
        private Text prompt;
        private Text quest;
        private Text xp;
        private GameObject dialoguePanel;
        private Text dialogueSpeaker;
        private Text dialogueText;
        private Button dialogueContinue;
        private DialogueDefinition currentDialogue;
        private int dialogueLine;
        private Action dialogueComplete;
        private GameObject rewardPanel;
        private GameObject minigamePanel;
        private Text minigameTimer;
        private Stage2Content content;
        private Stage2Game game;
        public void Initialize(Stage2Content stage2Content, Stage2Game stage2Game)
        {
            content = stage2Content; game = stage2Game;
            canvas = UiFactory.CreateCanvas("Stage 2 UI");
            BuildHud(); BuildDialogue(); BuildReward(); BuildMinigame();
        }
        private void BuildHud()
        {
            var questPanel = UiFactory.Panel(canvas.transform, new Color(.97f, .90f, .82f, .94f));
            UiFactory.SetRect(questPanel.rectTransform, new Vector2(.02f, .96f), new Vector2(330, 100), new Vector2(165, -55));
            quest = UiFactory.Text(questPanel.transform, string.Empty, 18, TextAnchor.UpperLeft, UiFactory.Primary);
            UiFactory.Stretch(quest.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 10), new Vector2(-14, -10));
            xp = UiFactory.Text(canvas.transform, "XP: 0", 18, TextAnchor.MiddleRight, UiFactory.Background);
            var xpPanel = UiFactory.Panel(canvas.transform, new Color(0f, 0f, 0f, .32f)); UiFactory.SetRect(xpPanel.rectTransform, new Vector2(.98f, .96f), new Vector2(130, 36), new Vector2(-65, -25));
            xp.transform.SetParent(xpPanel.transform, false); UiFactory.Stretch(xp.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
            prompt = UiFactory.Text(canvas.transform, string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.Background);
            var promptPanel = UiFactory.Panel(canvas.transform, new Color(0f, 0f, 0f, .38f)); UiFactory.SetRect(promptPanel.rectTransform, new Vector2(.5f, .12f), new Vector2(180, 42), Vector2.zero);
            prompt.transform.SetParent(promptPanel.transform, false); UiFactory.Stretch(prompt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
        private void BuildDialogue()
        {
            dialoguePanel = UiFactory.Panel(canvas.transform, UiFactory.Background).gameObject;
            UiFactory.SetRect(dialoguePanel.GetComponent<RectTransform>(), new Vector2(.5f, .18f), new Vector2(760, 190), Vector2.zero);
            dialogueSpeaker = UiFactory.Text(dialoguePanel.transform, string.Empty, 25, TextAnchor.UpperLeft, UiFactory.Primary); UiFactory.SetRect(dialogueSpeaker.rectTransform, new Vector2(.5f, .84f), new Vector2(670, 35), Vector2.zero);
            dialogueText = UiFactory.Text(dialoguePanel.transform, string.Empty, 22, TextAnchor.UpperLeft, Color.black); UiFactory.SetRect(dialogueText.rectTransform, new Vector2(.5f, .50f), new Vector2(670, 75), Vector2.zero);
            dialogueContinue = UiFactory.Button(dialoguePanel.transform, "Жалғастыру", AdvanceDialogue); UiFactory.SetRect(dialogueContinue.GetComponent<RectTransform>(), new Vector2(.85f, .14f), new Vector2(180, 42), Vector2.zero);
            dialoguePanel.SetActive(false);
        }
        private void BuildReward()
        {
            rewardPanel = UiFactory.Panel(canvas.transform, UiFactory.Background).gameObject;
            UiFactory.SetRect(rewardPanel.GetComponent<RectTransform>(), new Vector2(.5f, .57f), new Vector2(390, 150), Vector2.zero);
            var title = UiFactory.Text(rewardPanel.transform, "Тапсырма жаңартылды", 25, TextAnchor.MiddleCenter, UiFactory.Primary); UiFactory.SetRect(title.rectTransform, new Vector2(.5f, .76f), new Vector2(350, 40), Vector2.zero);
            var value = UiFactory.Text(rewardPanel.transform, string.Empty, 22, TextAnchor.MiddleCenter, UiFactory.Accent); value.name = "RewardText"; UiFactory.SetRect(value.rectTransform, new Vector2(.5f, .42f), new Vector2(350, 55), Vector2.zero);
            rewardPanel.SetActive(false);
        }
        private void BuildMinigame()
        {
            minigamePanel = UiFactory.Panel(canvas.transform, new Color(.55f, .18f, .14f, .91f)).gameObject;
            UiFactory.SetRect(minigamePanel.GetComponent<RectTransform>(), new Vector2(.5f, .92f), new Vector2(450, 82), Vector2.zero);
            var title = UiFactory.Text(minigamePanel.transform, string.Empty, 21, TextAnchor.UpperLeft, UiFactory.Background); title.name = "Title"; UiFactory.SetRect(title.rectTransform, new Vector2(.44f, .68f), new Vector2(350, 32), Vector2.zero);
            var rule = UiFactory.Text(minigamePanel.transform, string.Empty, 16, TextAnchor.UpperLeft, UiFactory.Background); rule.name = "Rule"; UiFactory.SetRect(rule.rectTransform, new Vector2(.5f, .26f), new Vector2(400, 28), Vector2.zero);
            minigameTimer = UiFactory.Text(minigamePanel.transform, string.Empty, 24, TextAnchor.MiddleRight, UiFactory.Background); UiFactory.SetRect(minigameTimer.rectTransform, new Vector2(.90f, .70f), new Vector2(45, 32), Vector2.zero);
            minigamePanel.SetActive(false);
        }
        public void SetInteractionPrompt(string value) => prompt.text = value;
        public void SetQuest(QuestState state, QuestDefinition definition, int currentXp)
        {
            string objective = state.Objective switch
            {
                FatherQuestObjective.TalkToFather => "Әкемен сөйлесіңіз",
                FatherQuestObjective.FindChildren => "Балаларды табыңыз",
                FatherQuestObjective.TalkToChildren => "Балалармен сөйлесіңіз",
                FatherQuestObjective.PlaySokyrTeke => "Соқыр теке ойынын аяқтаңыз",
                FatherQuestObjective.ReturnToFather => "Әкеге оралыңыз",
                FatherQuestObjective.Complete => "Тапсырма орындалды",
                _ => string.Empty
            };
            quest.text = "Тапсырма:\n" + objective;
            xp.text = "XP: " + currentXp;
        }
        public void ShowDialogue(DialogueDefinition definition, Action complete)
        {
            currentDialogue = definition; dialogueLine = 0; dialogueComplete = complete;
            dialoguePanel.SetActive(true); SetInteractionPrompt(string.Empty); RenderDialogue();
        }
        private void Update()
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) AdvanceDialogue();
        }
        private void RenderDialogue()
        {
            dialogueSpeaker.text = currentDialogue.speakerName;
            dialogueText.text = currentDialogue.lines[dialogueLine];
            dialogueContinue.GetComponentInChildren<Text>().text = dialogueLine + 1 < currentDialogue.lines.Length ? currentDialogue.continueLabel : "Жарайды";
        }
        private void AdvanceDialogue()
        {
            if (currentDialogue == null) return;
            dialogueLine++;
            if (dialogueLine < currentDialogue.lines.Length) { RenderDialogue(); return; }
            dialoguePanel.SetActive(false); currentDialogue = null;
            var callback = dialogueComplete; dialogueComplete = null; callback?.Invoke();
        }
        public void ShowReward(string value, float duration)
        {
            rewardPanel.transform.Find("RewardText").GetComponent<Text>().text = value;
            rewardPanel.SetActive(true); CancelInvoke(nameof(HideReward)); Invoke(nameof(HideReward), duration);
        }
        private void HideReward() => rewardPanel.SetActive(false);
        public void ShowRetry(Action retry)
        {
            rewardPanel.transform.Find("RewardText").GetComponent<Text>().text = "Уақыт аяқталды.\nҚайталап көріңіз.";
            var button = UiFactory.Button(rewardPanel.transform, "Қайталау", retry); UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, .12f), new Vector2(160, 38), Vector2.zero);
            rewardPanel.SetActive(true);
        }
        public void ShowMinigame(string title, string rule, float seconds)
        {
            minigamePanel.transform.Find("Title").GetComponent<Text>().text = title;
            minigamePanel.transform.Find("Rule").GetComponent<Text>().text = rule;
            minigamePanel.SetActive(true); SetMinigameTime(seconds);
        }
        public void SetMinigameTime(float seconds) => minigameTimer.text = Mathf.CeilToInt(seconds).ToString("00");
        public void HideMinigame() => minigamePanel.SetActive(false);
    }
}
