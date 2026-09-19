using System;
using System.IO;
using System.Linq;
using ATTILA.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ATTILA.Editor
{
    public static class EnvironmentArtBuilder
    {
        private const string Source = "Assets/ThirdParty/KenneyNatureKit/Models/FBX format/";
        private const string Prefabs = "Assets/ATTILA/Prefabs/Environment/KenneyNature/";
        private const string Materials = "Assets/ATTILA/Art/Environment/Materials/";

        [MenuItem("ATTILA/Art/Build Nature Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Materials);
            AssetDatabase.Refresh();
            const string path = "Assets/ATTILA/Resources/Art/AulEnvironment.asset";
            var set = AssetDatabase.LoadAssetAtPath<EnvironmentArtSet>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<EnvironmentArtSet>(); AssetDatabase.CreateAsset(set, path); }
            set.tree = Create("tree_oak", 4.4f, "tree");
            set.smallTree = Create("tree_small", 3.4f, "tree");
            set.bush = Create("plant_bush", .85f);
            set.smallBush = Create("plant_bushSmall", .55f);
            set.grass = Create("grass", .30f);
            set.rock = Create("rock_largeA", .62f, "rock");
            set.smallRock = Create("rock_smallA", .36f);
            set.fence = Create("fence_simple", 1.3f, "fence");
            set.logs = Create("log_stack", .6f);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Debug.Log("ART BUILD PASS: 9 ATTILA prefabs, shared matte URP materials and environment catalog saved. No scene or third-party source edited.");
        }

        private static GameObject Create(string name, float height, string colliderKind = "")
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source + name + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing imported model: " + name);
            var root = new GameObject("ATTILA " + name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.transform.SetParent(root.transform, false);
                var renderers = model.GetComponentsInChildren<MeshRenderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = height / bounds.size.y;
                // Normalize in the wrapper only. FBX units, pivots and original materials stay intact.
                model.transform.localScale *= scale;
                model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
                if (colliderKind == "fence") root.transform.localScale = new Vector3(3f / (bounds.size.x * scale), 1f, 1f);
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => AdaptMaterial(material, name)).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                if (colliderKind == "tree")
                {
                    var collider = root.AddComponent<CapsuleCollider>();
                    collider.radius = .24f; collider.height = 2.6f; collider.center = Vector3.up * 1.3f;
                }
                else if (colliderKind == "rock")
                {
                    var collider = root.AddComponent<BoxCollider>();
                    collider.size = new Vector3(bounds.size.x * scale * .8f, height * .85f, bounds.size.z * scale * .8f);
                    collider.center = Vector3.up * height * .425f;
                }
                else if (colliderKind == "fence")
                {
                    var collider = root.AddComponent<BoxCollider>();
                    collider.size = new Vector3(bounds.size.x * scale, height, .16f);
                    collider.center = Vector3.up * height * .5f;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Material AdaptMaterial(Material source, string model)
        {
            if (source == null) throw new InvalidOperationException("Missing source material for " + model);
            string key = source.name;
            if (model.StartsWith("rock") && key == "dirt") key = "stone";
            if (model.StartsWith("tree") && key == "grass") key = "canopy";
            if (model == "grass") key = "grassTuft";
            string path = Materials + key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            Color color = key switch
            {
                "canopy" => new Color(.38f, .64f, .39f),
                "grass" => new Color(.47f, .66f, .36f),
                "grassTuft" => new Color(.55f, .69f, .37f),
                "wood" => new Color(.62f, .44f, .28f),
                "woodDark" => new Color(.43f, .30f, .20f),
                "stone" => new Color(.64f, .65f, .56f),
                "dirt" => new Color(.63f, .48f, .32f),
                _ => source.HasProperty("_Color") ? source.color : new Color(.64f, .65f, .56f)
            };
            material.SetColor("_BaseColor", color);
            material.SetColor("_SpecColor", Color.black);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
