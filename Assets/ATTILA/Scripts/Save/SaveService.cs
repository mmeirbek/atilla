using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Save
{
    /// <summary>Progress of one playable role. Kept per role so each storyline continues independently.</summary>
    [Serializable]
    public sealed class RoleProgress
    {
        public string roleId;
        public int beatIndex;
        public bool completed;
        public int unity;
        public List<string> flags = new();
    }

    [Serializable]
    public sealed class SaveData
    {
        public string selectedHeroId = SaveService.DefaultRoleId;
        public int currentXP;
        public List<RoleProgress> roles = new();
        public List<string> unlockedEncyclopediaEntryIds = new();
        public List<string> equippedOutfitIds = new();
        public string lastCheckpoint = "AUL_START";
        public float musicVolume = .7f;
        public float sfxVolume = .85f;
    }

    public static class SaveService
    {
        public const string DefaultRoleId = "ROLE_KEREY";
        private const string SaveFileName = "attila_save.json";
        private static SaveData current;
        public static SaveData Current => current ??= LoadFromDisk();
        public static string FilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static RoleProgress Progress(string roleId)
        {
            var found = Current.roles.Find(role => role.roleId == roleId);
            if (found != null) return found;
            found = new RoleProgress { roleId = roleId };
            Current.roles.Add(found);
            return found;
        }

        public static void SelectHero(string heroId)
        {
            Current.selectedHeroId = heroId;
            Save();
        }

        /// <summary>Restarts one storyline. XP and unlocked encyclopedia entries are kept.</summary>
        public static void RestartRole(string roleId)
        {
            var progress = Progress(roleId);
            progress.beatIndex = 0;
            progress.completed = false;
            progress.unity = 0;
            progress.flags.Clear();
            Save();
        }

        /// <summary>Returns true only when the entry was not already unlocked.</summary>
        public static bool Unlock(string entryId)
        {
            if (string.IsNullOrEmpty(entryId) || Current.unlockedEncyclopediaEntryIds.Contains(entryId)) return false;
            Current.unlockedEncyclopediaEntryIds.Add(entryId);
            return true;
        }

        public static bool IsUnlocked(string entryId) => Current.unlockedEncyclopediaEntryIds.Contains(entryId);

        public static void Save()
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
        }

        public static void ResetForTests()
        {
            current = new SaveData();
            Save();
        }

        private static SaveData LoadFromDisk()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                    {
                        loaded.roles ??= new List<RoleProgress>();
                        loaded.unlockedEncyclopediaEntryIds ??= new List<string>();
                        loaded.equippedOutfitIds ??= new List<string>();
                        return loaded;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("ATTILA save could not be read: " + exception.Message);
            }
            return new SaveData();
        }
    }
}
