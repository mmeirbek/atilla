using ATTILA.Data;
using ATTILA.NPC;
using ATTILA.Player;
using ATTILA.Quest;
using ATTILA.Save;
using ATTILA.UI;
using UnityEngine;

namespace ATTILA.Gameplay
{
    public sealed class Stage2Game : MonoBehaviour
    {
        public static Stage2Game Active { get; private set; }
        public Stage2Ui Ui { get; private set; }
        public QuestState Quest { get; private set; }
        public bool IsModal { get; private set; }
        private Stage2Content content;
        private PlayerController player;
        private SokyrTekeMinigame miniGame;
        private const string FatherId = "NPC_Father_01";
        private const string ChildrenId = "NPC_Children_01";
        private void Awake()
        {
            Active = this;
            content = Resources.Load<Stage2Content>("Data/Stage2Content");
            if (content == null) Debug.LogError("Stage 2 content asset is missing.");
            Quest = new QuestState();
        }
        public void Initialize(PlayerController value)
        {
            player = value;
            Ui = gameObject.AddComponent<Stage2Ui>();
            Ui.Initialize(content, this);
            player.gameObject.AddComponent<PlayerInteraction>();
            miniGame = gameObject.AddComponent<SokyrTekeMinigame>();
            RestoreCheckpointPosition();
            RefreshUi();
        }
        public NpcDefinition Npc(string id) => content.Npc(id);
        public void InteractWith(NpcInteractable npc)
        {
            if (IsModal) return;
            if (npc.Id == FatherId) InteractFather();
            else if (npc.Id == ChildrenId) InteractChildren();
        }
        private void InteractFather()
        {
            if (Quest.Objective == FatherQuestObjective.TalkToFather)
                ShowDialogue("DIA_FATHER_START", () => SetObjective(FatherQuestObjective.FindChildren, "FATHER_QUEST_ACCEPTED"));
            else if (Quest.Objective == FatherQuestObjective.ReturnToFather)
                ShowDialogue("DIA_FATHER_COMPLETE", () => SetObjective(FatherQuestObjective.Complete, "FATHER_QUEST_COMPLETE"));
            else if (Quest.IsComplete) ShowDialogue("DIA_FATHER_DONE", null);
            else ShowDialogue("DIA_FATHER_WAIT", null);
        }
        private void InteractChildren()
        {
            if (Quest.Objective == FatherQuestObjective.FindChildren)
            {
                SetObjective(FatherQuestObjective.TalkToChildren, "CHILDREN_FOUND");
                ShowDialogue("DIA_CHILDREN_START", StartSokyrTeke);
            }
            else if (Quest.Objective == FatherQuestObjective.TalkToChildren || Quest.Objective == FatherQuestObjective.PlaySokyrTeke)
                ShowDialogue("DIA_CHILDREN_START", StartSokyrTeke);
            else if (Quest.Objective == FatherQuestObjective.ReturnToFather) ShowDialogue("DIA_CHILDREN_COMPLETE", null);
        }
        private void StartSokyrTeke()
        {
            SetObjective(FatherQuestObjective.PlaySokyrTeke, "SOKYR_TEKE_ACTIVE");
            IsModal = true;
            miniGame.Begin(player, OnSokyrTekeWon, OnSokyrTekeLost);
        }
        private void OnSokyrTekeWon()
        {
            SaveService.Current.currentXP += 100;
            const string entryId = "game_sokyr_teke";
            if (!SaveService.Current.unlockedEncyclopediaEntryIds.Contains(entryId)) SaveService.Current.unlockedEncyclopediaEntryIds.Add(entryId);
            SetObjective(FatherQuestObjective.ReturnToFather, "SOKYR_TEKE_COMPLETE");
            player.transform.position = new Vector3(5f, 1f, 5f);
            IsModal = false;
            Ui.ShowReward("+100 XP\nСоқыр теке: жазба ашылды", 3f);
            RefreshUi();
        }
        private void OnSokyrTekeLost()
        {
            IsModal = false;
            Ui.ShowRetry(StartSokyrTeke);
        }
        private void SetObjective(FatherQuestObjective objective, string checkpoint)
        {
            Quest.Set(objective, checkpoint);
            RefreshUi();
        }
        private void ShowDialogue(string id, System.Action complete)
        {
            IsModal = true;
            player.SetInputEnabled(false);
            Ui.ShowDialogue(content.Dialogue(id), () => { IsModal = false; player.SetInputEnabled(true); complete?.Invoke(); });
        }
        private void RefreshUi() => Ui.SetQuest(Quest, content.Quest(QuestState.QuestId), SaveService.Current.currentXP);
        private void RestoreCheckpointPosition()
        {
            if (Quest.Objective == FatherQuestObjective.FindChildren || Quest.Objective == FatherQuestObjective.TalkToChildren || Quest.Objective == FatherQuestObjective.PlaySokyrTeke)
                player.transform.position = new Vector3(3.5f, 1f, 5f);
            else if (Quest.Objective == FatherQuestObjective.ReturnToFather || Quest.IsComplete)
                player.transform.position = new Vector3(-2.5f, 1f, 5.5f);
            else player.transform.position = new Vector3(0f, 1f, 0f);
        }
        private void OnDestroy() { if (Active == this) Active = null; }
    }
}
