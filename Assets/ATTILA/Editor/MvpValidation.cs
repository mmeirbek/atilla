using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATTILA.CameraSystem;
using ATTILA.Characters;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Gameplay;
using ATTILA.NPC;
using ATTILA.Player;
using ATTILA.Save;
using ATTILA.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ATTILA.Editor
{
    /// <summary>
    /// Editor-only MVP check. First it validates the content graph without entering play mode, then it walks the
    /// three roles in play mode and writes screenshots of every screen to the ignored Logs/Mvp folder.
    /// Batch: -executeMethod ATTILA.Editor.MvpValidation.Run (no -quit, no -nographics).
    /// </summary>
    [InitializeOnLoad]
    public static class MvpValidation
    {
        private const string Key = "ATTILA.MvpValidation";
        private const string Folder = "Logs/Mvp";
        private static readonly List<string> Report = new();
        private static int phase, errors, nextFrame, role;
        private static double next, deadline;

        static MvpValidation()
        {
            if (SessionState.GetBool(Key, false)) Attach();
        }

        // ------------------------------------------------------------ content graph

        [MenuItem("ATTILA/Validate Content")]
        public static void ValidateContent()
        {
            var content = AssetDatabase.LoadAssetAtPath<Stage2Content>("Assets/ATTILA/Resources/Data/Stage2Content.asset");
            Require(content != null, "Story content asset exists");
            var art = AssetDatabase.LoadAssetAtPath<MvpArtSet>("Assets/ATTILA/Resources/Art/MvpArt.asset");
            Require(art != null, "Generated art catalogue exists");
            Require(content.chapters.Count == 3, "Three chapters");
            foreach (var id in HeroCatalog.Order)
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>($"Assets/ATTILA/Resources/Data/Roles/{id}.asset");
                Require(hero != null, "Role asset " + id);
                Require(hero.Portrait != null, "Card art for " + id);
                Require(LookSet(hero.VisualSetId) != null, "Look for " + id);
                Require(content.Chapter(hero.ChapterId) != null, "Chapter for " + id);
            }
            foreach (var npc in content.npcs)
            {
                Require(LookSet(npc.look) != null, "Look for NPC " + npc.id);
                Require(content.Dialogue(npc.idleDialogueId) != null, "Idle dialogue for " + npc.id);
            }
            foreach (var chapter in content.chapters)
            {
                Require(chapter.beats.Count >= 4, chapter.id + " has beats");
                var placed = chapter.npcs.Where(p => !string.IsNullOrEmpty(p.npcId)).Select(p => p.npcId).ToHashSet();
                foreach (var placement in chapter.npcs)
                {
                    string look = !string.IsNullOrEmpty(placement.npcId) ? content.Npc(placement.npcId)?.look : placement.look;
                    Require(LookSet(look) != null, $"{chapter.id}: look for placement {placement.npcId}{placement.look}");
                }
                foreach (var beat in chapter.beats)
                {
                    Require(placed.Contains(beat.npcId), $"{chapter.id}: beat NPC {beat.npcId} is placed in the aul");
                    Require(content.Dialogue(beat.dialogueId) != null, $"{chapter.id}: dialogue {beat.dialogueId}");
                    Require(!string.IsNullOrEmpty(beat.objective), $"{chapter.id}: objective text for {beat.npcId}");
                    foreach (var entry in beat.unlockEntryIds ?? Array.Empty<string>())
                        Require(content.Entry(entry) != null, $"{chapter.id}: entry {entry}");
                    if (beat.action == BeatAction.Campfire)
                    {
                        Require(beat.campfire != null && beat.campfire.topics.Count > 0, $"{chapter.id}: campfire topics");
                        foreach (var topic in beat.campfire.topics)
                        {
                            Require(content.Dialogue(topic.dialogueId) != null, $"{chapter.id}: topic dialogue {topic.dialogueId}");
                            Require(string.IsNullOrEmpty(topic.unlockEntryId) || content.Entry(topic.unlockEntryId) != null, $"{chapter.id}: topic entry {topic.unlockEntryId}");
                        }
                    }
                    if (beat.action is BeatAction.Collect or BeatAction.Find or BeatAction.Escort or BeatAction.Build)
                    {
                        Require(beat.task != null, $"{chapter.id}: task data for {beat.action}");
                        Require(beat.task.points != null && beat.task.points.Length > 0, $"{chapter.id}: task points for {beat.action}");
                        Require(!string.IsNullOrEmpty(beat.task.title) && !string.IsNullOrEmpty(beat.task.hint), $"{chapter.id}: task title and hint for {beat.action}");
                        foreach (var prop in beat.task.props ?? Array.Empty<string>())
                            Require(art.Prop(prop) != null, $"{chapter.id}: task prop {prop}");
                        if (beat.action == BeatAction.Collect)
                            Require(beat.task.points.Length >= beat.task.required, $"{chapter.id}: enough pickups for {beat.task.title}");
                    }
                    if (beat.action == BeatAction.Map)
                        Require(beat.map != null && beat.map.route != null && beat.map.route.Length >= 2, $"{chapter.id}: map route");
                }
            }
            foreach (var dialogue in content.dialogues)
            {
                Require(dialogue.lines != null && dialogue.lines.Length > 0, "Lines in " + dialogue.id);
                foreach (var choice in dialogue.choices)
                    Require(string.IsNullOrEmpty(choice.nextDialogueId) || content.Dialogue(choice.nextDialogueId) != null, $"{dialogue.id}: choice target {choice.nextDialogueId}");
                Require(dialogue.status == ContentStatus.Verified || string.IsNullOrEmpty(dialogue.sourceNote) || dialogue.sourceNote.Contains("ПРОВЕРИТЬ"), dialogue.id + " marks its source note");
            }
            foreach (var entry in content.encyclopediaEntries)
            {
                Require(!string.IsNullOrEmpty(entry.unlockedDescription), "Text for entry " + entry.id);
                Require(!string.IsNullOrEmpty(entry.sourceNote), "Source note for entry " + entry.id);
                Require(string.IsNullOrEmpty(entry.icon) || art.Icon(entry.icon) != null, "Icon for entry " + entry.id);
                Require(string.IsNullOrEmpty(entry.look) || LookSet(entry.look) != null, "Portrait look for entry " + entry.id);
            }
            foreach (var item in content.outfitItems)
            {
                Require(art.Icon(item.icon) != null, "Icon for outfit " + item.id);
                Require(item.looks != null && item.looks.Length > 0, "Outfit " + item.id + " belongs to a character");
                foreach (var look in item.looks) Require(LookSet(look) != null, $"Outfit {item.id}: look {look}");
            }
            foreach (var npc in content.npcs)
                Require(content.Dialogue(npc.repeatDialogueId) != null, "Repeat line for " + npc.id);
            var reachable = new HashSet<string>(content.encyclopediaEntries.Where(entry => entry.openFromStart).Select(entry => entry.id));
            foreach (var chapter in content.chapters)
                foreach (var beat in chapter.beats)
                {
                    foreach (var id in beat.unlockEntryIds ?? Array.Empty<string>()) reachable.Add(id);
                    if (beat.campfire != null)
                        foreach (var topic in beat.campfire.topics)
                            if (!string.IsNullOrEmpty(topic.unlockEntryId)) reachable.Add(topic.unlockEntryId);
                }
            foreach (var entry in content.encyclopediaEntries)
                Require(reachable.Contains(entry.id), "Entry " + entry.id + " can actually be opened");
            Debug.Log($"ATTILA CONTENT VALIDATION PASS: {Report.Count} checks.");
        }

        private static DirectionalSpriteSet LookSet(string id) =>
            string.IsNullOrEmpty(id) ? null : AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>($"Assets/ATTILA/Resources/Data/Looks/{id}.asset");

        // ------------------------------------------------------------ play mode tour

        public static void Run()
        {
            Report.Clear();
            Directory.CreateDirectory(Folder);
            ValidateContent();
            SaveService.ResetForTests();
            SessionState.SetBool(Key, true);
            phase = 0;
            errors = 0;
            role = 0;
            EditorSceneManager.OpenScene("Assets/ATTILA/Scenes/Boot.unity");
            Attach();
            EditorApplication.isPlaying = true;
        }

        private static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= Log;
            Application.logMessageReceived += Log;
            deadline = EditorApplication.timeSinceStartup + 600;
            next = EditorApplication.timeSinceStartup + 2;
        }

        private static void Log(string message, string stack, LogType type)
        {
            // Unity's own QuickSearch indexer throws on startup in batch mode; that is not the project's code.
            if (stack != null && stack.Contains("UnityEditor.Search")) return;
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
            {
                errors++;
                Report.Add("CONSOLE ERROR: " + message);
            }
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || Time.frameCount < nextFrame) return;
            next = EditorApplication.timeSinceStartup + .5;
            nextFrame = Time.frameCount + 12;
            try
            {
                Step();
            }
            catch (Exception exception)
            {
                Finish(false, exception.ToString());
            }
        }

        private static void Step()
        {
            switch (phase++)
            {
                case 0:
                    Require(SceneManager.GetActiveScene().name == "MainMenu", "Boot opens the main menu");
                    Capture("01-main-menu");
                    SceneManager.LoadScene("CharacterSelect");
                    break;
                case 1:
                    Require(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length >= 3, "Character select shows the roles");
                    Capture("02-character-select");
                    StartRole();
                    break;
                case 2:
                    CheckWorld();
                    Capture($"03-{role}-aul");
                    Talk();
                    break;
                case 3:
                    Capture($"04-{role}-dialogue");
                    Skip();
                    break;
                case 4:
                    Capture($"05-{role}-dialogue-choice");
                    Skip();
                    break;
                case 5:
                    Require(Director.Quest.BeatIndex > 0 || Director.Ui.DialogueActive, "First beat progresses after the dialogue");
                    StartTask();
                    break;
                case 6:
                    Capture($"05b-{role}-task");
                    Close();
                    Screens();
                    break;
                case 7:
                    Capture($"06-{role}-campfire");
                    Close();
                    MapScreen.Show(Director.Ui.OverlayRoot, FindMap(), () => { });
                    break;
                case 8:
                    Capture($"07-{role}-map");
                    Close();
                    Minigame();
                    break;
                case 9:
                    Capture($"08-{role}-minigame");
                    Close();
                    // Open a couple of entries so the screenshot shows both states.
                    SaveService.Unlock("kazakh_khanate");
                    SaveService.Unlock("sokyr_teke");
                    EncyclopediaScreen.Show(Director.Ui.OverlayRoot);
                    break;
                case 10:
                    Capture($"09-{role}-encyclopedia");
                    Close();
                    WardrobeScreen.Show(Director.Ui.OverlayRoot);
                    break;
                case 11:
                    Capture($"10-{role}-wardrobe");
                    Close();
                    SettingsScreen.Show(Director.Ui.OverlayRoot);
                    break;
                case 12:
                    Capture($"11-{role}-settings");
                    Close();
                    Director.Ui.ShowFinale(Director.Chapter, Director.Chapter.finaleText, 320, 2, () => { }, () => { }, () => { });
                    break;
                case 13:
                    Capture($"12-{role}-finale");
                    Close();
                    role++;
                    if (role < HeroCatalog.Order.Length) { phase = 2; StartRole(); }
                    else
                    {
                        Require(errors == 0, "No runtime console errors");
                        Finish(true, "Content graph, three roles, world build and every screen rendered.");
                    }
                    break;
            }
        }

        private static StoryDirector Director => StoryDirector.Active;

        private static void StartRole()
        {
            SaveService.SelectHero(HeroCatalog.Order[role]);
            SceneManager.LoadScene("AulPrototype");
        }

        private static void CheckWorld()
        {
            Require(Director != null, "Story director runs in the aul");
            Require(Director.Hero.HeroId == HeroCatalog.Order[role], "Selected role is " + HeroCatalog.Order[role]);
            Require(Director.Chapter != null && Director.Chapter.roleId == Director.Hero.HeroId, "Chapter matches the role");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Require(player != null && player.enabled, "Player controller is active");
            Require(player.GetComponentInChildren<SpriteRenderer>().sprite != null, "Role sprite is shown");
            Require(Camera.main.orthographic && Mathf.Abs(Camera.main.transform.eulerAngles.x - 40f) < .01f, "Orthographic 40 degree camera");
            Require(PixelCamera.Active != null, "Pixel buffer is attached");
            var npcs = UnityEngine.Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Require(npcs.Length >= 4, "Chapter NPCs are placed");
            Require(UnityEngine.Object.FindObjectsByType<SpriteFlipbook>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0, "Animated props exist");
            Require((player.GetComponent<CharacterController>().Move(Vector3.down * .2f) & CollisionFlags.Below) != 0, "Ground blocks the player");
        }

        private static void Talk()
        {
            var beat = Director.Quest.Current;
            var npc = UnityEngine.Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(n => n.Id == beat.npcId);
            Require(npc != null, "Beat NPC is in the scene: " + beat.npcId);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = npc.transform.position + new Vector3(0f, 0f, -1.4f);
            controller.enabled = true;
            Physics.SyncTransforms();
            Director.InteractWith(npc);
            Require(Director.Ui.DialogueActive, "Dialogue opens");
        }

        private static void Skip() => Director.Ui.FastForward();

        /// <summary>Starts the chapter's world job so the screenshot shows the pickups, the horse or the yurt site.</summary>
        private static void StartTask()
        {
            while (Director.Ui.DialogueActive) Director.Ui.FastForward();
            var beat = Director.Chapter.beats.FirstOrDefault(b => b.action is BeatAction.Collect or BeatAction.Find or BeatAction.Escort or BeatAction.Build);
            Require(beat != null, "Chapter has a world task");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            WorldTask task = beat.action switch
            {
                BeatAction.Collect => Director.gameObject.AddComponent<CollectTask>(),
                BeatAction.Find => Director.gameObject.AddComponent<FindTask>(),
                BeatAction.Escort => Director.gameObject.AddComponent<EscortTask>(),
                _ => Director.gameObject.AddComponent<BuildTask>()
            };
            task.Begin(beat.task, player, () => { });
            var first = beat.task.points[0];
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = new Vector3(first.x, 1f, first.y - 3f);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private static void Screens()
        {
            while (Director.Ui.DialogueActive) Director.Ui.FastForward();
            var campfire = Director.Chapter.beats.FirstOrDefault(b => b.action == BeatAction.Campfire)?.campfire;
            Require(campfire != null, "Chapter has a campfire beat");
            CampfireScreen.Show(Director.Ui.OverlayRoot, Director, campfire, () => { });
        }

        private static MapDefinition FindMap()
        {
            var map = Director.Chapter.beats.FirstOrDefault(b => b.action == BeatAction.Map)?.map;
            Require(map != null, "Chapter has a map beat");
            return map;
        }

        private static void Minigame()
        {
            var action = Director.Chapter.beats.FirstOrDefault(b => b.action is BeatAction.Asyk or BeatAction.Duel or BeatAction.SokyrTeke)?.action ?? BeatAction.None;
            Require(action != BeatAction.None, "Chapter has a mini-game");
            var host = Director.gameObject;
            if (action == BeatAction.Asyk) host.GetComponent<AsykMinigame>().Begin(() => { }, () => { });
            else if (action == BeatAction.Duel) host.GetComponent<DuelMinigame>().Begin(Director.Hero, () => { }, () => { });
            else host.GetComponent<SokyrTekeMinigame>().Begin(UnityEngine.Object.FindAnyObjectByType<PlayerController>(), () => { }, () => { });
        }

        private static void Close()
        {
            foreach (var task in Director.GetComponents<WorldTask>()) UnityEngine.Object.DestroyImmediate(task);
            foreach (Transform child in Director.Ui.OverlayRoot) UnityEngine.Object.DestroyImmediate(child.gameObject);
            Director.Ui.SetVignette(.38f, 3000f);
            Director.Ui.HideMinigame();
            Director.Ui.SetHudVisible(true);
        }

        // ------------------------------------------------------------ helpers

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception("FAILED: " + message);
            Report.Add("PASS: " + message);
        }

        private static void Capture(string name, int width = 1920, int height = 1080)
        {
            if (PixelCamera.Active != null)
            {
                PixelCamera.Active.Resize(width, height);
                PixelCamera.Active.RenderNow();
            }
            var camera = new GameObject("Validation camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // The world is already inside the pixel buffer; hiding the scene renderers keeps this camera from
            // drawing the 3D scene a second time on top of it.
            var hidden = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(renderer => renderer.enabled).ToArray();
            foreach (var renderer in hidden) renderer.enabled = false;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes($"{Folder}/{name}.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            foreach (var canvas in canvases)
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            Directory.CreateDirectory(Folder);
            Report.Add((success ? "RESULT PASS: " : "RESULT FAIL: ") + message);
            File.WriteAllLines($"{Folder}/Validation.txt", Report);
            Debug.Log("ATTILA MVP RESULT: " + message);
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
