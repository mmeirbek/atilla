using System;
using System.Linq;
using ATTILA.Gameplay;
using ATTILA.NPC;
using ATTILA.Quest;
using ATTILA.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ATTILA.Editor
{
    public static class Stage2Validation
    {
        private static int phase;
        private static double next;
        private static double deadline;
        private static int errors;
        [MenuItem("ATTILA/Validate Stage 2 Loop")]
        public static void Run()
        {
            SaveService.ResetForTests();
            EditorSceneManager.OpenScene("Assets/ATTILA/Scenes/Boot.unity");
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
            phase = 0; errors = 0; next = EditorApplication.timeSinceStartup + 1.5; deadline = EditorApplication.timeSinceStartup + 80;
            EditorApplication.isPlaying = true;
        }
        private static void Log(string condition, string trace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert) errors++;
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .45;
            try
            {
                switch (phase++)
                {
                    case 0: Require(SceneManager.GetActiveScene().name == "MainMenu", "Boot opens MainMenu"); Click("Ойнау"); break;
                    case 1: Require(SceneManager.GetActiveScene().name == "CharacterSelect", "Character Select opens"); Click("Бастау"); break;
                    case 2:
                        Require(SceneManager.GetActiveScene().name == "AulPrototype", "AulPrototype opens");
                        Require(Camera.main.orthographic && Mathf.Abs(Camera.main.transform.eulerAngles.x - 40f) < .01f, "Stage 1.5 camera preserved");
                        Require(UnityEngine.Object.FindAnyObjectByType<ATTILA.Player.PlayerVisual>() != null, "Sprite visual preserved");
                        Require(UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>() != null, "Reusable player interaction added");
                        Stage2Game.Active.InteractWith(Npc("NPC_Father_01")); break;
                    case 3: ContinueDialogue(); break;
                    case 4: ContinueDialogue(); break;
                    case 5:
                        Require(Stage2Game.Active.Quest.Objective == FatherQuestObjective.FindChildren, "Father dialogue grants FindChildren objective");
                        Require(SaveService.Current.lastCheckpoint == "FATHER_QUEST_ACCEPTED", "Quest acceptance autosaved");
                        Stage2Game.Active.InteractWith(Npc("NPC_Children_01")); break;
                    case 6: ContinueDialogue(); break;
                    case 7: ContinueDialogue(); break;
                    case 8:
                        Require(Stage2Game.Active.Quest.Objective == FatherQuestObjective.PlaySokyrTeke, "Children dialogue starts mini-game");
                        var target = GameObject.Find("Sokyr Teke Target - Placeholder");
                        Require(target != null, "Mini-game target spawned");
                        UnityEngine.Object.FindAnyObjectByType<ATTILA.Player.PlayerController>().transform.position = target.transform.position;
                        break;
                    case 9:
                        Require(Stage2Game.Active.Quest.Objective == FatherQuestObjective.ReturnToFather, "Mini-game win advances return objective");
                        Require(SaveService.Current.currentXP == 100, "Reward grants 100 XP once");
                        Require(SaveService.Current.unlockedEncyclopediaEntryIds.Contains("game_sokyr_teke"), "Encyclopedia entry unlocks and saves");
                        Require(SaveService.Current.lastCheckpoint == "SOKYR_TEKE_COMPLETE", "Mini-game checkpoint autosaved");
                        Stage2Game.Active.InteractWith(Npc("NPC_Father_01")); break;
                    case 10: ContinueDialogue(); break;
                    case 11:
                        Require(Stage2Game.Active.Quest.IsComplete, "Final Father dialogue completes quest");
                        Require(SaveService.Current.lastCheckpoint == "FATHER_QUEST_COMPLETE", "Completion checkpoint autosaved");
                        Require(SaveService.Current.questCompleted, "Completion persists in save data");
                        SceneManager.LoadScene("CharacterSelect"); break;
                    case 12:
                        Require(SceneManager.GetActiveScene().name == "CharacterSelect", "Character Select reloads after save");
                        Require(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Any(button => button.GetComponentInChildren<Text>().text == "Продолжить"), "Continue label reflects saved hero");
                        Require(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude).Any(text => text.text == "100%"), "Progress reflects quest completion");
                        Require(errors == 0, "No runtime console errors"); Finish(true, "Stage 2 full loop passed"); break;
                }
            }
            catch (Exception exception) { Finish(false, exception.ToString()); }
        }
        private static NpcInteractable Npc(string id) => UnityEngine.Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude).Single(npc => npc.Id == id);
        private static void Click(string text) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Single(button => button.GetComponentInChildren<Text>().text == text).onClick.Invoke();
        private static void ContinueDialogue() => Click("Жалғастыру");
        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
            Debug.Log("STAGE2 PASS: " + message);
        }
        private static void Finish(bool success, string message)
        {
            EditorApplication.update -= Tick;
            Debug.Log("STAGE2 RESULT: " + message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
