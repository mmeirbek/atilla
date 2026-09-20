using System.Collections.Generic;
using ATTILA.CameraSystem;
using ATTILA.Characters;
using ATTILA.Data;
using ATTILA.Gameplay;
using ATTILA.NPC;
using ATTILA.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace ATTILA.Environment
{
    /// <summary>
    /// Builds the aul for the selected role's chapter: the same layout every time, but its light, banners,
    /// people and props come from the chapter data, so each storyline has its own time of day and crowd.
    /// </summary>
    public sealed class AulPrototypeBuilder : MonoBehaviour
    {
        private static readonly Vector3 Spawn = new(0f, 1f, 0f);
        private static readonly Vector3[] YurtSpots = { new(-12, 0, 9), new(12, 0, 10), new(-13, 0, -9), new(13, 0, -10) };
        private static readonly Dictionary<Color, Material> SharedMaterials = new();

        private EnvironmentArtSet environmentArt;
        private MvpArtSet art;
        private ChapterDefinition chapter;
        private readonly List<Mesh> meshes = new();
        private Mesh roofMesh;

        private void Start()
        {
            var director = gameObject.AddComponent<StoryDirector>();
            chapter = director.Chapter;
            art = MvpArtSet.Instance;
            environmentArt = Resources.Load<EnvironmentArtSet>("Art/AulEnvironment");
            ApplyAtmosphere();
            BuildGround();
            BuildPath();
            BuildYurts();
            BuildNature();
            BuildFence();
            BuildCampfire();
            BuildHorses();
            BuildBanners();
            var player = BuildPlayer(director.Hero);
            BuildPeople();
            var camera = BuildCamera(player.transform);
            PixelCamera.Attach(camera, chapter != null ? chapter.worldTint : Color.white);
            gameObject.AddComponent<QuestMarker>();
            director.Initialize(player.GetComponent<PlayerController>());
        }

        private void ApplyAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = chapter != null ? chapter.ambientColor : new Color(.90f, .93f, .86f);
            RenderSettings.fog = false;
        }

        // ------------------------------------------------------------ ground and paths

        private void BuildGround()
        {
            var ground = Primitive(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(5.5f, 1, 5.5f), Color.white);
            ground.GetComponent<Renderer>().sharedMaterial = Textured(art != null ? art.ground : null, new Vector2(26f, 26f), Color.white);
        }

        private void BuildPath()
        {
            Vector3[] route = { new(0, 0, -22), new(-.2f, 0, -16), new(.15f, 0, -9), new(0, 0, -2), new(-.15f, 0, 5), new(.1f, 0, 12), new(0, 0, 22) };
            PathRibbon("Soft path verge", route, 2.02f, .052f, new Color(.78f, .76f, .52f), null);
            PathRibbon("Main earth path", route, 1.78f, .056f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach west", new[] { new Vector3(-12, 0, 5.9f), new Vector3(-7, 0, 5.2f), new Vector3(0, 0, 5.5f) }, .85f, .058f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach east", new[] { new Vector3(0, 0, 5.5f), new Vector3(6, 0, 6f), new Vector3(12, 0, 6.9f) }, .85f, .058f, Color.white, art != null ? art.path : null);
        }

        // ------------------------------------------------------------ yurts

        private void BuildYurts()
        {
            foreach (var spot in YurtSpots) CreateYurt(spot);
        }

        private void CreateYurt(Vector3 position)
        {
            var root = new GameObject("Yurt");
            root.transform.position = position;
            GroundPatch("Yurt earth clearing", position, new Vector2(3.4f, 3.5f), Color.white, art != null ? art.path : null);
            var wall = Primitive(PrimitiveType.Cylinder, "Yurt wall", position + Vector3.up * .95f, new Vector3(5.4f, .95f, 5.4f), Color.white);
            wall.GetComponent<Renderer>().sharedMaterial = Textured(art != null ? art.yurtWall : null, new Vector2(3f, 1f), Color.white);
            wall.transform.SetParent(root.transform);
            CreateRoof(root.transform);
            var door = Primitive(PrimitiveType.Cube, "Yurt door", position + new Vector3(0, .8f, -2.71f), new Vector3(.95f, 1.6f, .12f), Color.white);
            door.GetComponent<Renderer>().sharedMaterial = Textured(art != null ? art.yurtDoor : null, Vector2.one, Color.white);
            door.transform.SetParent(root.transform);
        }

        private void CreateRoof(Transform parent)
        {
            if (roofMesh == null)
            {
                const int sides = 12;
                const float radius = 2.85f;
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
                meshes.Add(roofMesh);
            }
            var roof = new GameObject("Yurt roof", typeof(MeshFilter), typeof(MeshRenderer));
            roof.transform.SetParent(parent, false);
            roof.GetComponent<MeshFilter>().sharedMesh = roofMesh;
            var renderer = roof.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Textured(art != null ? art.yurtRoof : null, Vector2.one, Color.white);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------ nature, props and people

        private void BuildNature()
        {
            Vector3[] groves = { new(-17, 0, 13), new(17, 0, 14), new(-18, 0, -14), new(18, 0, -15), new(-5, 0, 18), new(7, 0, -19), new(-15.5f, 0, 1.5f), new(15.5f, 0, -.5f) };
            for (int i = 0; i < groves.Length; i++)
            {
                var p = groves[i];
                Place(i % 3 == 0 ? environmentArt.smallTree : environmentArt.tree, p, i * 51f);
                Place(environmentArt.bush, p + new Vector3(1.4f, 0, .6f), i * 27f);
                Place(environmentArt.smallBush, p + new Vector3(-1.1f, 0, -.3f), i * 37f);
                Place(environmentArt.grass, p + new Vector3(.8f, 0, -1.2f), i * 43f, 1.2f);
            }
            foreach (var p in new[] { new Vector3(-7, 0, -5), new Vector3(7, 0, 4), new Vector3(-18, 0, 2), new Vector3(17, 0, 2) })
            {
                Place(environmentArt.rock, p, 25f);
                Place(environmentArt.smallRock, p + new Vector3(.65f, 0, .4f), 140f);
                Place(environmentArt.grass, p + new Vector3(-.6f, 0, .3f), 70f);
            }
            Place(environmentArt.logs, new Vector3(-8.5f, 0, 10f), 15f);
            Place(environmentArt.logs, new Vector3(16.2f, 0, -9.5f), -20f);
            foreach (var p in new[] { new Vector3(-2.9f, 0, 10), new Vector3(3.1f, 0, 1), new Vector3(-3.3f, 0, -5), new Vector3(4.6f, 0, 11) })
                Place(environmentArt.grass, p, p.z * 18f);
        }

        private void BuildFence()
        {
            foreach (float x in new[] { -9f, -6f, -3.5f, 3.5f, 6f, 9f })
                Place(environmentArt.fence, new Vector3(x, 0, 14));
        }

        private void BuildCampfire()
        {
            var position = new Vector3(-3.6f, 0f, -1.6f);
            GroundPatch("Fire clearing", position, new Vector2(2.6f, 2.4f), Color.white, art != null ? art.path : null);
            var fire = new GameObject("Campfire");
            fire.transform.position = position;
            var renderer = fire.AddComponent<SpriteRenderer>();
            renderer.sprite = art != null && art.campfire.Length > 0 ? art.campfire[0] : null;
            fire.AddComponent<SpriteFlipbook>().Configure(art != null ? art.campfire : null, 9f);
            fire.AddComponent<Billboard>();
            Flat(art != null ? art.glow : null, position + Vector3.up * .04f, new Color(1f, .68f, .32f, .5f), 1.3f);
            var pot = new GameObject("Kazan");
            pot.transform.position = position + new Vector3(1.9f, 0f, .4f);
            pot.AddComponent<SpriteRenderer>().sprite = art != null ? art.kazan : null;
            pot.AddComponent<Billboard>();
            AddShadow(pot.transform, .8f);
        }

        private void BuildHorses()
        {
            var sprites = art != null ? art.horseBay : null;
            Horse(new Vector3(10.6f, 0f, -3.4f), 0f, sprites);
            Horse(new Vector3(12.4f, 0f, -4.8f), 12f, art != null ? art.horseGrey : null);
            Horse(new Vector3(-10.8f, 0f, 6.4f), 0f, art != null ? art.horseGrey : null, true);
        }

        private void Horse(Vector3 position, float delay, Sprite[] frames, bool mirrored = false)
        {
            var horse = new GameObject("Horse");
            horse.transform.position = position;
            var renderer = horse.AddComponent<SpriteRenderer>();
            renderer.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            renderer.flipX = mirrored;
            horse.AddComponent<SpriteFlipbook>().Configure(frames, .6f, delay);
            horse.AddComponent<Billboard>();
            AddShadow(horse.transform, 2.2f);
            var collider = horse.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 1.6f, .9f);
            collider.center = new Vector3(0f, .8f, 0f);
        }

        private void BuildBanners()
        {
            var sprite = art != null ? art.Banner(chapter != null ? chapter.banner : "red") : null;
            foreach (var position in new[] { new Vector3(-2.9f, 0, -6f), new Vector3(2.9f, 0, 2f), new Vector3(-2.9f, 0, 10f), new Vector3(2.9f, 0, 13.6f) })
                Banner(sprite, position);
            foreach (var spot in YurtSpots) Banner(sprite, spot + new Vector3(2.2f, 0, -2.4f));
        }

        private void Banner(Sprite sprite, Vector3 position)
        {
            var banner = new GameObject("Banner");
            banner.transform.position = position;
            banner.AddComponent<SpriteRenderer>().sprite = sprite;
            banner.AddComponent<Billboard>();
            AddShadow(banner.transform, .5f);
        }

        private GameObject BuildPlayer(HeroDefinition hero)
        {
            var player = new GameObject((hero != null ? hero.HeroId : "HERO") + " Player");
            player.transform.position = Spawn;
            player.AddComponent<CharacterController>();
            var visual = player.AddComponent<PlayerVisual>();
            visual.SetSpriteSet(LookLibrary.Load(hero != null ? hero.VisualSetId : "kerey"));
            player.AddComponent<PlayerController>();
            AddShadow(player.transform, 1f);
            return player;
        }

        private void BuildPeople()
        {
            if (chapter == null) return;
            foreach (var placement in chapter.npcs)
            {
                var definition = !string.IsNullOrEmpty(placement.npcId) ? Resources.Load<Stage2Content>("Data/Stage2Content").Npc(placement.npcId) : null;
                string look = definition != null ? definition.look : placement.look;
                var person = new GameObject(definition != null ? definition.id : "Aul figure " + look);
                person.transform.position = new Vector3(placement.position.x, 1f, placement.position.y);
                person.AddComponent<CharacterController>();
                var visual = person.AddComponent<PlayerVisual>();
                visual.SetSpriteSet(LookLibrary.Load(look));
                visual.Face(Direction(placement.facing));
                AddShadow(person.transform, 1f);
                if (definition != null) person.AddComponent<NpcInteractable>().Configure(definition);
            }
        }

        private static Vector3 Direction(FacingHint hint) => hint switch
        {
            FacingHint.East => Vector3.right,
            FacingHint.West => Vector3.left,
            FacingHint.North => Vector3.forward,
            _ => Vector3.back
        };

        private GameObject BuildCamera(Transform target)
        {
            var cameraObject = new GameObject("Gameplay Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var controller = cameraObject.AddComponent<CameraController>();
            controller.SetTarget(target);
            cameraObject.GetComponent<Camera>().backgroundColor = chapter != null ? chapter.skyColor : new Color(.65f, .82f, .91f);
            var light = new GameObject("Sun", typeof(Light));
            var sun = light.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = chapter != null ? chapter.sunIntensity : .55f;
            sun.color = chapter != null ? chapter.sunColor : Color.white;
            sun.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(chapter != null ? chapter.sunEuler : new Vector3(50, -30, 0));
            return cameraObject;
        }

        // ------------------------------------------------------------ shared helpers

        /// <summary>Flat unlit material for a colour, shared between every caller in the scene.</summary>
        public static Material SharedFlat(Color color)
        {
            if (SharedMaterials.TryGetValue(color, out var cached) && cached != null) return cached;
            var material = new Material(Resources.Load<Material>("Art/PlaceholderFlat")) { name = "Flat " + color };
            material.SetColor("_BaseColor", color);
            SharedMaterials[color] = material;
            return material;
        }

        /// <summary>Soft blob under a character or prop; grounds the billboarded sprites on the tilted ground.</summary>
        public static void AddShadow(Transform parent, float scale = 1f)
        {
            var art = MvpArtSet.Instance;
            if (art == null || art.shadow == null) return;
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0f, -.96f + .05f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = Vector3.one * scale;
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = art.shadow;
            renderer.sortingOrder = -1;
        }

        private static void Flat(Sprite sprite, Vector3 position, Color color, float scale)
        {
            if (sprite == null) return;
            var go = new GameObject("Glow");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = -1;
        }

        private Material Textured(Texture2D texture, Vector2 tiling, Color color)
        {
            var material = new Material(Resources.Load<Material>("Art/PlaceholderFlat")) { name = "Textured " + (texture != null ? texture.name : "flat") };
            material.SetColor("_BaseColor", color);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            return material;
        }

        private void Place(GameObject prefab, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0));
            instance.transform.localScale *= scale;
        }

        private void GroundPatch(string name, Vector3 position, Vector2 size, Color color, Texture2D texture)
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
            GroundMesh(name, vertices, uv, triangles, color, texture);
        }

        private void PathRibbon(string name, Vector3[] points, float halfWidth, float height, Color color, Texture2D texture)
        {
            var vertices = new Vector3[points.Length * 2];
            var uv = new Vector2[points.Length * 2];
            var triangles = new int[(points.Length - 1) * 6];
            float travelled = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 tangent = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(0, i - 1)];
                Vector3 side = Vector3.Cross(Vector3.up, tangent.normalized) * halfWidth;
                if (i > 0) travelled += Vector3.Distance(points[i], points[i - 1]);
                vertices[i * 2] = points[i] - side + Vector3.up * height;
                vertices[i * 2 + 1] = points[i] + side + Vector3.up * height;
                uv[i * 2] = new Vector2(0f, travelled * .35f);
                uv[i * 2 + 1] = new Vector2(1f, travelled * .35f);
                if (i == points.Length - 1) continue;
                int a = i * 2, t = i * 6;
                triangles[t] = a; triangles[t + 1] = a + 2; triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1; triangles[t + 4] = a + 2; triangles[t + 5] = a + 3;
            }
            GroundMesh(name, vertices, uv, triangles, color, texture);
        }

        private void GroundMesh(string name, Vector3[] vertices, Vector2[] uv, int[] triangles, Color color, Texture2D texture)
        {
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshes.Add(mesh);
            var patch = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = patch.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = texture != null ? Textured(texture, Vector2.one, color) : SharedFlat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = SharedFlat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
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
            interval = framesPerSecond > 1f ? 1f / framesPerSecond : 1f / Mathf.Max(.05f, framesPerSecond);
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
