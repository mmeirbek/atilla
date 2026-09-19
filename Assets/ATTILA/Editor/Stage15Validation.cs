using System;
using System.IO;
using System.Linq;
using ATTILA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ATTILA.Editor
{
    // Editor-only regression check. Runs the existing scene flow; never saves/rebuilds scenes.
    [InitializeOnLoad]
    public static class Stage15Validation
    {
        private const string Key = "ATTILA.Stage15Validation";
        private static int phase;
        private static int nextFrame;
        private static double next;
        private static double deadline;
        private static Vector3 start;
        private static Vector3 cameraStart;
        private static int errors;
        static Stage15Validation()
        {
            if (SessionState.GetBool(Key, false)) Attach();
        }
        public static void Run()
        {
            SessionState.SetBool(Key, true);
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
            deadline = EditorApplication.timeSinceStartup + 120;
            next = EditorApplication.timeSinceStartup + 2;
        }
        private static void Log(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Debug.Log("STAGE15 PASS: " + message);
        }
        private static void Click(string label)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .Single(x => x.GetComponentInChildren<Text>().text == label);
            Require(button.interactable, label + " is interactable");
            button.onClick.Invoke();
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || Time.frameCount < nextFrame) return;
            next = EditorApplication.timeSinceStartup + .6;
            nextFrame = Time.frameCount + 20;
            try
            {
                switch (phase++)
                {
                    case 0:
                        Require(SceneManager.GetActiveScene().name == "MainMenu", "Boot loads MainMenu");
                        Capture("MainMenu"); Click("\u041e\u0439\u043d\u0430\u0443"); break;
                    case 1:
                        Require(SceneManager.GetActiveScene().name == "CharacterSelect", "Play loads CharacterSelect");
                        Capture("CharacterSelect"); Click("\u0410\u0440\u0442\u049b\u0430"); break;
                    case 2:
                        Require(SceneManager.GetActiveScene().name == "MainMenu", "Back returns to MainMenu");
                        Click("\u041e\u0439\u043d\u0430\u0443"); break;
                    case 3:
                        Click("\u0411\u0430\u0441\u0442\u0430\u0443"); break;
                    case 4:
                        Require(SceneManager.GetActiveScene().name == "AulPrototype", "Start loads AulPrototype");
                        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Require(player != null && player.enabled, "PlayerController remains enabled");
                        Require(player.GetComponent<MeshRenderer>() == null, "No 3D player renderer");
                        Require(player.transform.position.y > .95f && player.transform.position.y < 1.3f, "Original movement keeps player on ground");
                        Require((player.GetComponent<CharacterController>().Move(Vector3.down * .2f) & CollisionFlags.Below) != 0, "Ground collider blocks downward movement");
                        Require(Camera.main.orthographic && Mathf.Abs(Camera.main.transform.eulerAngles.x - 40f) < .01f, "Orthographic 40 degree camera");
                        Require(player.GetComponentInChildren<SpriteRenderer>().sprite != null, "Sprite loaded and displayed");
                        Capture("AulPrototype");
                        start = player.transform.position; cameraStart = Camera.main.transform.position;
                        player.GetComponent<CharacterController>().Move(Vector3.right * 2f);
                        break;
                    case 5:
                        var moved = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Require(moved.transform.position.x > start.x + 1.9f, "CharacterController permits movement");
                        Require(Camera.main.transform.position.x > cameraStart.x + 1.8f, "Camera follows displacement");
                        var viewport = Camera.main.WorldToViewportPoint(moved.transform.position);
                        Require(Mathf.Abs(viewport.x-.5f)<.02f && Mathf.Abs(viewport.y-.5f)<.02f, "Player framed near center");
                        CheckCollisions(moved);
                        moved.enabled = false;
                        break;
                    default:
                        int direction = phase - 7;
                        if (direction < 8)
                        {
                            var visual = UnityEngine.Object.FindAnyObjectByType<PlayerController>().GetComponent<PlayerVisual>();
                            float angle = direction * Mathf.PI / 4f;
                            visual.SetMovement(new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)), true);
                            Require((int)visual.Facing == direction, "Facing " + (FacingDirection)direction);
                            var set = Resources.Load<DirectionalSpriteSet>("Data/TemporaryDirections");
                            Require(set.GetSprite(visual.Facing, false, 0) != null && set.GetSprite(visual.Facing,true,0) != set.GetSprite(visual.Facing,true,1f/set.framesPerSecond), "Idle and two walk frames " + visual.Facing);
                            visual.SetMovement(Vector3.forward, false);
                            Require((int)visual.Facing == direction, "Idle preserves direction " + visual.Facing);
                        }
                        else { Require(errors == 0, "No runtime console errors"); Finish(true, "All automated checks passed. Hardware WASD input requires manual verification."); }
                        break;
                }
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }
        private static void CheckCollisions(PlayerController player)
        {
            var controller = player.GetComponent<CharacterController>();
            var original = player.transform.position;
            foreach (var sample in new[] {
                new Vector3(-12,1,9), new Vector3(-4,1,6), new Vector3(7,1,4), new Vector3(0,1,14), new Vector3(-5,1,18) })
            {
                controller.enabled = false; player.transform.position = sample + Vector3.back * 4f; controller.enabled = true;
                Physics.SyncTransforms();
                bool blocked = false;
                for (int i=0;i<50;i++) blocked |= (controller.Move(Vector3.forward * .1f) & CollisionFlags.Sides) != 0;
                Require(blocked && player.transform.position.z < sample.z, "Collision blocks passage at " + sample);
            }
            controller.enabled = false; player.transform.position = original; controller.enabled = true;
        }
        private static void Capture(string name)
        {
            var camera = Camera.main;
            bool temporary = camera == null;
            if (temporary) camera = new GameObject("Validation camera",typeof(Camera)).GetComponent<Camera>();
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f; }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1280,720,24);
            camera.targetTexture = target; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            Directory.CreateDirectory("Logs/Stage15"); File.WriteAllBytes("Logs/Stage15/"+name+".png",image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null; }
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            if(temporary) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Key,false); EditorApplication.update -= Tick;
            Directory.CreateDirectory("Logs/Stage15"); File.WriteAllText("Logs/Stage15/Validation.txt", (success ? "PASS: " : "FAIL: ") + message);
            Debug.Log("STAGE15 RESULT: " + message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
