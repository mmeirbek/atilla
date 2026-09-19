using ATTILA.Characters;
using ATTILA.Core;
using ATTILA.Environment;
using ATTILA.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ATTILA.Editor
{
    public static class MvpSceneBuilder
    {
        [MenuItem("ATTILA/Build Stage 1 Scenes")]
        public static void Build()
        {
            ConfigureUrp();
            EnsureHero();
            MakeScene("Boot", typeof(GameBootstrap));
            MakeScene("MainMenu", typeof(MainMenuScreen));
            MakeScene("CharacterSelect", typeof(CharacterSelectScreen));
            MakeScene("AulPrototype", typeof(AulPrototypeBuilder));
            EditorBuildSettings.scenes = new[] { "Boot", "MainMenu", "CharacterSelect", "AulPrototype" }.Select(path => new EditorBuildSettingsScene("Assets/ATTILA/Scenes/" + path + ".unity", true)).ToArray();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        private static void ConfigureUrp()
        {
            const string path = "Assets/ATTILA/Settings/ATTILA_URP.asset";
            const string rendererPath = "Assets/ATTILA/Settings/ATTILA_Renderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (pipeline == null || pipeline.rendererDataList.Length == 0 || pipeline.rendererDataList[0] == null)
            {
                if (pipeline != null) AssetDatabase.DeleteAsset(path);
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, path);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }
        private static void MakeScene(string sceneName, System.Type component)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(sceneName + " Root"); root.AddComponent(component);
            EditorSceneManager.SaveScene(scene, "Assets/ATTILA/Scenes/" + sceneName + ".unity");
        }
        private static void EnsureHero()
        {
            const string assetPath = "Assets/ATTILA/Resources/Data/TEMP_HERO.asset";
            if (AssetDatabase.LoadAssetAtPath<HeroDefinition>(assetPath) != null) return;
            var hero = ScriptableObject.CreateInstance<HeroDefinition>(); AssetDatabase.CreateAsset(hero, assetPath);
        }
    }
}
