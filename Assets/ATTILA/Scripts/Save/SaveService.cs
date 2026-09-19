using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Save
{
    [Serializable]
    public sealed class Stage2SaveData
    {
        public string selectedHeroId = "TEMP_HERO";
        public int currentXP;
        public string questId = "CH01_FATHER_01";
        public string questObjective = "TalkToFather";
        public bool questCompleted;
        public List<string> unlockedEncyclopediaEntryIds = new();
        public string lastCheckpoint = "AUL_START";
    }

    public static class SaveService
    {
        private const string SaveFileName = "attila_stage2_save.json";
        private static Stage2SaveData current;
        public static Stage2SaveData Current => current ??= LoadFromDisk();
        public static string FilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static bool HasSaveFor(string heroId) => File.Exists(FilePath) && Current.selectedHeroId == heroId;
        public static void SelectHero(string heroId)
        {
            Current.selectedHeroId = heroId;
            Save();
        }
        public static void Save()
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
        }
        public static void ResetForTests()
        {
            current = new Stage2SaveData();
            Save();
        }
        private static Stage2SaveData LoadFromDisk()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<Stage2SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.unlockedEncyclopediaEntryIds ??= new List<string>();
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ATTILA Stage 2 save could not be read: " + exception.Message);
            }
            return new Stage2SaveData();
        }
    }
}
