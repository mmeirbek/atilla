using ATTILA.Save;

namespace ATTILA.Quest
{
    public enum FatherQuestObjective { TalkToFather, FindChildren, TalkToChildren, PlaySokyrTeke, ReturnToFather, Complete }

    public sealed class QuestState
    {
        public const string QuestId = "CH01_FATHER_01";
        public FatherQuestObjective Objective { get; private set; }
        public bool IsComplete => Objective == FatherQuestObjective.Complete;
        public QuestState()
        {
            if (!System.Enum.TryParse(SaveService.Current.questObjective, out FatherQuestObjective loaded))
                loaded = FatherQuestObjective.TalkToFather;
            Objective = SaveService.Current.questCompleted ? FatherQuestObjective.Complete : loaded;
        }
        public void Set(FatherQuestObjective objective, string checkpoint)
        {
            Objective = objective;
            SaveService.Current.questId = QuestId;
            SaveService.Current.questObjective = objective.ToString();
            SaveService.Current.questCompleted = IsComplete;
            SaveService.Current.lastCheckpoint = checkpoint;
            SaveService.Save();
        }
        public int ProgressPercent => Objective switch
        {
            FatherQuestObjective.TalkToFather => 0,
            FatherQuestObjective.FindChildren => 25,
            FatherQuestObjective.TalkToChildren or FatherQuestObjective.PlaySokyrTeke => 50,
            FatherQuestObjective.ReturnToFather => 75,
            FatherQuestObjective.Complete => 100,
            _ => 0
        };
    }
}
