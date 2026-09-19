using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ATTILA.Editor
{
    // Optional in-editor check. Preserves the open scene and player save; never rebuilds scenes.
    [InitializeOnLoad]
    public static class ArtPolishValidation
    {
        private const string Key = "ATTILA.ArtPolishValidation";
        private const string Request = "Logs/ArtPolish.request";
        private static int phase, errors, nextFrame;
        private static double next, deadline;
        private static Vector3 origin, cameraOrigin;
        static ArtPolishValidation()
        {
            if (SessionState.GetBool(Key, false)) Attach();
            else EditorApplication.delayCall += ConsumeRequest;
        }
        private static void ConsumeRequest()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += ConsumeRequest; return; }
            File.Delete(Request);
            try { EnvironmentArtBuilder.Build(); Run(); }
            catch (Exception ex) { Directory.CreateDirectory("Logs/ArtPolish"); File.WriteAllText("Logs/ArtPolish/Result.txt", "FAIL: " + ex); Debug.LogException(ex); }
        }
        [MenuItem("ATTILA/Art/Validate Presentation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before running presentation validation.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits before running presentation validation.");
            SessionState.SetString(Key + ".save", SaveFingerprint());
            SessionState.SetBool(Key,true);
            phase = 0; errors = 0; nextFrame = 0;
            Attach(); EditorApplication.isPlaying = true;
        }
        private static string SaveFingerprint()
        {
            if (!File.Exists(SaveService.FilePath)) return "no save";
            using var hash = SHA256.Create();
            return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(SaveService.FilePath)));
        }
        private static void Attach()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
            deadline = EditorApplication.timeSinceStartup + 120; next = EditorApplication.timeSinceStartup + 2;
        }
        private static void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private static void Check(bool value, string message)
        { if (!value) throw new Exception(message); Debug.Log("ART PASS: " + message); }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false,"Timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next || Time.frameCount < nextFrame) return;
            next = EditorApplication.timeSinceStartup + .7; nextFrame = Time.frameCount + 20;
            try
            {
                switch (phase++)
                {
                    case 0: SceneManager.LoadScene("MainMenu"); break;
                    case 1:
                        Check(SceneManager.GetActiveScene().name == "MainMenu", "Main menu loads"); Capture("Menu");
                        UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Single(b=>b.GetComponentInChildren<Text>().text == "Ойнау").onClick.Invoke(); break;
                    case 2:
                        Check(SceneManager.GetActiveScene().name == "CharacterSelect", "Character Select loads"); Capture("CharacterSelect");
                        // Loading directly avoids changing the selected hero or writing a save during an art test.
                        SceneManager.LoadScene("AulPrototype"); break;
                    case 3:
                        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Check(player != null && player.enabled, "Aul and existing PlayerController load");
                        Check(Camera.main.orthographic && Mathf.Abs(Camera.main.orthographicSize-10f)<.001f && Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(40,0,0))<.01f, "Camera remains orthographic, 40 degrees, size 10");
                        Check(player.GetComponentInChildren<SpriteRenderer>().sprite != null && player.GetComponent<MeshRenderer>() == null, "Sprite player preserved");
                        CheckMaterials(); CheckPrefabs();
                        Teleport(player, new Vector3(0,1.1f,0)); break;
                    case 4:
                        Capture("Aul-1280"); Capture("Aul-1920",1920,1080);
                        var moving = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        origin = moving.transform.position; cameraOrigin = Camera.main.transform.position;
                        moving.GetComponent<CharacterController>().Move(Vector3.right * 2f); break;
                    case 5:
                        var moved = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Check(moved.transform.position.x > origin.x+1.9f, "Player collider permits movement");
                        Check(Camera.main.transform.position.x > cameraOrigin.x+1.8f, "Camera follows movement");
                        CheckCollision(moved, new Vector3(-12,1.1f,9));
                        CheckCollision(moved, new Vector3(7,1.1f,4));
                        CheckCollision(moved, new Vector3(-6,1.1f,14));
                        Teleport(moved,new Vector3(0,1.1f,-12)); break;
                    case 6:
                        Capture("Minigame-clearing");
                        var checkPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                        Check((checkPlayer.GetComponent<CharacterController>().Move(Vector3.down*.3f)&CollisionFlags.Below)!=0,"Ground collision works");
                        Check(Physics.OverlapBox(new Vector3(0,1.1f,-12),new Vector3(4.8f,.3f,4.8f)).All(c=>c.gameObject==checkPlayer.gameObject),"Mini-game clearing stays free of obstacles");
                        checkPlayer.enabled = false;
                        Teleport(checkPlayer,new Vector3(-3,1.1f,4));
                        break;
                    case 7:
                        var visual = UnityEngine.Object.FindAnyObjectByType<PlayerController>().GetComponent<PlayerVisual>();
                        for(int i=0;i<8;i++)
                        {
                            float a = i*Mathf.PI/4;
                            visual.SetMovement(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),true);
                            Check((int)visual.Facing==i,"Facing "+(FacingDirection)i);
                        }
                        var ui = UnityEngine.Object.FindAnyObjectByType<ATTILA.UI.Stage2Ui>();
                        var content = Resources.Load<ATTILA.Data.Stage2Content>("Data/Stage2Content");
                        ui.ShowDialogue(content.Dialogue("DIA_FATHER_START"),null);
                        Capture("Dialogue-layout");
                        Check(errors==0,"No runtime console errors");
                        Check(SaveFingerprint()==SessionState.GetString(Key+".save",""),"Existing save unchanged");
                        Finish(true,"Import, references, materials, scene loads, collider movement, collisions, eight directions and unchanged camera passed. Keyboard input not injected."); break;
                }
            }
            catch(Exception ex) { Finish(false,ex.ToString()); }
        }
        private static void Teleport(PlayerController player,Vector3 position)
        { var cc=player.GetComponent<CharacterController>(); cc.enabled=false; player.transform.position=position; cc.enabled=true; Physics.SyncTransforms(); }
        private static void CheckCollision(PlayerController player,Vector3 target)
        {
            Teleport(player,target+Vector3.back*5);
            bool blocked=false;
            for(int i=0;i<60;i++) blocked |= (player.GetComponent<CharacterController>().Move(Vector3.forward*.1f)&CollisionFlags.Sides)!=0;
            Check(blocked && player.transform.position.z < target.z,"Obstacle collision at "+target);
        }
        private static void CheckMaterials()
        {
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                foreach(var material in renderer.sharedMaterials)
                    Check(material!=null && material.shader!=null && material.shader.isSupported && material.shader.name!="Hidden/InternalErrorShader","Valid material on "+renderer.name);
                if(renderer.TryGetComponent<MeshFilter>(out var filter)) Check(filter.sharedMesh!=null,"Mesh reference on "+renderer.name);
            }
        }
        private static void CheckPrefabs()
        {
            var art=Resources.Load<EnvironmentArtSet>("Art/AulEnvironment");
            Check(art!=null,"Environment art catalog loaded");
            foreach(var prefab in new[]{art.tree,art.smallTree,art.bush,art.smallBush,art.grass,art.rock,art.smallRock,art.fence,art.logs})
                Check(prefab!=null && prefab.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh!=null),"Prefab and FBX references resolved");
        }
        private static void Capture(string name,int width=1280,int height=720)
        {
            var camera=Camera.main; bool temporary=camera==null;
            if(temporary) camera=new GameObject("Art validation camera",typeof(Camera)).GetComponent<Camera>();
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            foreach(var canvas in canvases) {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            var target=new RenderTexture(width,height,24);
            camera.targetTexture=target; Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            Directory.CreateDirectory("Logs/ArtPolish");File.WriteAllBytes("Logs/ArtPolish/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
            UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
            if(temporary)UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
        private static void Finish(bool success,string message)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            Directory.CreateDirectory("Logs/ArtPolish");File.WriteAllText("Logs/ArtPolish/Result.txt",(success?"PASS: ":"FAIL: ")+message);
            Debug.Log("ART RESULT: "+message);
            if(Application.isBatchMode)EditorApplication.Exit(success?0:1);
            else EditorApplication.isPlaying=false;
        }
    }
}
