using ATTILA.Data;
using UnityEditor;
using UnityEngine;

namespace ATTILA.Editor
{
    public static class Stage2ContentBuilder
    {
        private const string AssetPath = "Assets/ATTILA/Resources/Data/Stage2Content.asset";
        [MenuItem("ATTILA/Generate Stage 2 Content")]
        public static void Build()
        {
            var content = AssetDatabase.LoadAssetAtPath<Stage2Content>(AssetPath);
            if (content == null) { content = ScriptableObject.CreateInstance<Stage2Content>(); AssetDatabase.CreateAsset(content, AssetPath); }
            content.npcs = new()
            {
                new NpcDefinition { id = "NPC_Father_01", displayName = "Әкесі", interactionRadius = 2.2f },
                new NpcDefinition { id = "NPC_Children_01", displayName = "Балалар", interactionRadius = 2.4f },
                new NpcDefinition { id = "NPC_Neutral_01", displayName = "Тұрғын", interactionRadius = 1.8f }
            };
            content.dialogues = new()
            {
                new DialogueDefinition { id = "DIA_FATHER_START", speakerName = "Әкесі", lines = new[] { "Балаларға барып кел.", "Олар сені күтіп отыр." } },
                new DialogueDefinition { id = "DIA_FATHER_WAIT", speakerName = "Әкесі", lines = new[] { "Алдымен балаларды тап." } },
                new DialogueDefinition { id = "DIA_FATHER_COMPLETE", speakerName = "Әкесі", lines = new[] { "Жарайсың. Тапсырма аяқталды." } },
                new DialogueDefinition { id = "DIA_FATHER_DONE", speakerName = "Әкесі", lines = new[] { "Бүгінгі тапсырма аяқталды." } },
                new DialogueDefinition { id = "DIA_CHILDREN_START", speakerName = "Балалар", lines = new[] { "Ойын басталады.", "Уақытша ереже: нысанаға жақындаңыз." } },
                new DialogueDefinition { id = "DIA_CHILDREN_COMPLETE", speakerName = "Балалар", lines = new[] { "Ойынды аяқтадың." } }
            };
            content.quests = new()
            {
                new QuestDefinition { id = "CH01_FATHER_01", title = "Әкенің тапсырмасы", description = "Уақытша тапсырма мәтіні.", rewardLabel = "+100 XP", encyclopediaEntryId = "game_sokyr_teke" }
            };
            content.encyclopediaEntries = new()
            {
                new EncyclopediaEntryDefinition { id = "game_sokyr_teke", category = "Ойындар", title = "Соқыр теке", lockedDescription = "Ойын барысында ашылады.", unlockedDescription = "Тарихи мәтін кейін тексерілген дереккөздерден қосылады." }
            };
            EditorUtility.SetDirty(content); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
    }
}
