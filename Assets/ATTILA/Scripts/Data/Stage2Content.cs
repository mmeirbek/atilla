using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Data
{
    [CreateAssetMenu(menuName = "ATTILA/Stage 2 Content", fileName = "Stage2Content")]
    public sealed class Stage2Content : ScriptableObject
    {
        public List<NpcDefinition> npcs = new();
        public List<DialogueDefinition> dialogues = new();
        public List<QuestDefinition> quests = new();
        public List<EncyclopediaEntryDefinition> encyclopediaEntries = new();

        public NpcDefinition Npc(string id) => npcs.Find(value => value.id == id);
        public DialogueDefinition Dialogue(string id) => dialogues.Find(value => value.id == id);
        public QuestDefinition Quest(string id) => quests.Find(value => value.id == id);
        public EncyclopediaEntryDefinition Entry(string id) => encyclopediaEntries.Find(value => value.id == id);
    }

    [Serializable]
    public sealed class NpcDefinition
    {
        public string id;
        public string displayName;
        public float interactionRadius = 2f;
    }

    [Serializable]
    public sealed class DialogueDefinition
    {
        public string id;
        public string speakerName;
        [TextArea] public string[] lines;
        public string continueLabel = "Жалғастыру";
    }

    [Serializable]
    public sealed class QuestDefinition
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public string rewardLabel;
        public string encyclopediaEntryId;
    }

    [Serializable]
    public sealed class EncyclopediaEntryDefinition
    {
        public string id;
        public string category;
        public string title;
        [TextArea] public string lockedDescription;
        [TextArea] public string unlockedDescription;
        public Sprite imagePlaceholder;
    }
}
