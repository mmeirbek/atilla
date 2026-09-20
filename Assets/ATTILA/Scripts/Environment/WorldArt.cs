using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ATTILA.Environment
{
    /// <summary>Shared builders for world geometry and flat props, used by the aul, the yurt sites and the tasks.</summary>
    public static class WorldArt
    {
        private static readonly Dictionary<Color, Material> FlatMaterials = new();
        private static readonly Dictionary<Texture2D, Material> TexturedMaterials = new();
        private static readonly List<Mesh> Meshes = new();
        private static Mesh roofMesh;

        /// <summary>Parent for everything a location builds; clearing it removes the whole place.</summary>
        public static Transform Root { get; private set; }

        public static Transform NewRoot(string name)
        {
            if (Root != null) Object.Destroy(Root.gameObject);
            Root = new GameObject(name).transform;
            return Root;
        }

        private static GameObject Attach(GameObject go)
        {
            if (Root != null && go != null && go.transform.parent == null) go.transform.SetParent(Root, true);
            return go;
        }

        public static Material SharedFlat(Color color)
        {
            if (FlatMaterials.TryGetValue(color, out var cached) && cached != null) return cached;
            var material = new Material(Resources.Load<Material>("Art/PlaceholderFlat")) { name = "Flat " + color };
            material.SetColor("_BaseColor", color);
            FlatMaterials[color] = material;
            return material;
        }

        public static Material Textured(Texture2D texture, Vector2 tiling, Color color)
        {
            if (texture == null) return SharedFlat(color);
            if (TexturedMaterials.TryGetValue(texture, out var cached) && cached != null && cached.GetTextureScale("_BaseMap") == tiling && cached.GetColor("_BaseColor") == color)
                return cached;
            var material = new Material(Resources.Load<Material>("Art/PlaceholderFlat")) { name = "Textured " + texture.name };
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            TexturedMaterials[texture] = material;
            return material;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return Attach(go);
        }

        /// <summary>Shallow twelve-sided yurt roof; one mesh is shared by every yurt in the scene.</summary>
        public static Mesh RoofMesh(float radius)
        {
            if (roofMesh != null) return roofMesh;
            const int sides = 12;
            var vertices = new Vector3[sides + 1];
            var uv = new Vector2[sides + 1];
            var triangles = new int[sides * 3];
            vertices[0] = new Vector3(0f, 3.4f, 0f);
            uv[0] = new Vector2(.5f, .5f);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 1.85f, Mathf.Sin(angle) * radius);
                uv[i + 1] = new Vector2(Mathf.Cos(angle) * .5f + .5f, Mathf.Sin(angle) * .5f + .5f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % sides + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            roofMesh = new Mesh { name = "Yurt roof", vertices = vertices, uv = uv, triangles = triangles };
            roofMesh.RecalculateNormals();
            roofMesh.RecalculateBounds();
            return roofMesh;
        }

        /// <summary>Soft blob under a character or prop; grounds the billboarded sprites on the tilted ground.</summary>
        public static void AddShadow(Transform parent, float scale = 1f)
        {
            var art = MvpArtSet.Instance;
            if (art == null || art.shadow == null) return;
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -.91f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = Vector3.one * scale;
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = art.shadow;
            renderer.sortingOrder = -1;
        }

        /// <summary>Billboarded flat prop with a shadow: banners, carts, firewood, yurt parts.</summary>
        public static GameObject Prop(Sprite sprite, Vector3 position, float shadow = 1f, bool mirrored = false)
        {
            if (sprite == null) return null;
            var prop = new GameObject(sprite.name);
            prop.transform.position = position;
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = mirrored;
            prop.AddComponent<Billboard>();
            AddShadow(prop.transform, shadow);
            return Attach(prop);
        }

        /// <summary>Flat decal lying on the ground: fire glow, target rings.</summary>
        public static GameObject Decal(Sprite sprite, Vector3 position, Color color, float scale)
        {
            if (sprite == null) return null;
            var go = new GameObject(sprite.name + " decal");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -1;
            return Attach(go);
        }

        public static GameObject GroundPatch(Transform parent, Vector3 position, Vector2 size, Texture2D texture)
        {
            const int count = 16;
            var vertices = new Vector3[count + 1];
            var uv = new Vector2[count + 1];
            var triangles = new int[count * 3];
            vertices[0] = position + Vector3.up * .014f;
            uv[0] = new Vector2(position.x, position.z) * .25f;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                float radius = 1f + .06f * Mathf.Sin(i * 2.7f);
                vertices[i + 1] = position + new Vector3(Mathf.Cos(angle) * size.x * radius, .014f, Mathf.Sin(angle) * size.y * radius);
                uv[i + 1] = new Vector2(vertices[i + 1].x, vertices[i + 1].z) * .25f;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % count + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            return Mesh("Earth clearing", parent, vertices, uv, triangles, Color.white, texture);
        }

        public static GameObject Mesh(string name, Transform parent, Vector3[] vertices, Vector2[] uv, int[] triangles, Color color, Texture2D texture)
        {
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Meshes.Add(mesh);
            var patch = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            if (parent != null) patch.transform.SetParent(parent, true);
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = patch.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = texture != null ? Textured(texture, Vector2.one, color) : SharedFlat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return Attach(patch);
        }
    }

    /// <summary>Keeps a flat prop facing the tilted camera, the same way the character sprites do.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        private Camera view;

        private void LateUpdate()
        {
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
        }
    }

    /// <summary>Cycles a sprite list on a SpriteRenderer: campfire flames, grazing horses.</summary>
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        private Sprite[] frames;
        private float interval;
        private float next;
        private int index;
        private SpriteRenderer target;

        public void Configure(Sprite[] value, float framesPerSecond, float delay = 0f)
        {
            frames = value;
            interval = 1f / Mathf.Max(.05f, framesPerSecond);
            target = GetComponent<SpriteRenderer>();
            next = Time.time + delay;
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2 || Time.time < next) return;
            next = Time.time + interval;
            index = (index + 1) % frames.Length;
            target.sprite = frames[index];
        }
    }
}
