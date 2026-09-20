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

        private EnvironmentArtSet environmentArt;
        private MvpArtSet art;
        private ChapterDefinition chapter;

        private void Start()
        {
            var director = gameObject.AddComponent<StoryDirector>();
            chapter = director.Chapter;
            art = MvpArtSet.Instance;
            environmentArt = Resources.Load<EnvironmentArtSet>("Art/AulEnvironment");
            ApplyAtmosphere();
            BuildGround();
            BuildPath();
            foreach (var spot in YurtSpots) YurtSite.Create(spot, Banner, YurtSite.Stages);
            BuildNature();
            BuildFence();
            BuildCampfire();
            BuildHorses();
            BuildLivestock();
            BuildBanners();
            var player = BuildPlayer(director.Hero);
            BuildPeople();
            var camera = BuildCamera(player.transform);
            PixelCamera.Attach(camera, chapter != null ? chapter.worldTint : Color.white);
            gameObject.AddComponent<QuestMarker>();
            director.Initialize(player.GetComponent<PlayerController>());
        }

        private string Banner => chapter != null ? chapter.banner : "red";

        private void ApplyAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = chapter != null ? chapter.ambientColor : new Color(.90f, .93f, .86f);
            RenderSettings.fog = false;
        }

        // ------------------------------------------------------------ ground and paths

        private void BuildGround()
        {
            var ground = WorldArt.Primitive(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(5.5f, 1, 5.5f));
            ground.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.ground : null, new Vector2(26f, 26f), Color.white);
        }

        private void BuildPath()
        {
            Vector3[] route = { new(0, 0, -22), new(-.2f, 0, -16), new(.15f, 0, -9), new(0, 0, -2), new(-.15f, 0, 5), new(.1f, 0, 12), new(0, 0, 22) };
            PathRibbon("Soft path verge", route, 2.02f, .052f, new Color(.78f, .76f, .52f), null);
            PathRibbon("Main earth path", route, 1.78f, .056f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach west", new[] { new Vector3(-12, 0, 5.9f), new Vector3(-7, 0, 5.2f), new Vector3(0, 0, 5.5f) }, .85f, .058f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach east", new[] { new Vector3(0, 0, 5.5f), new Vector3(6, 0, 6f), new Vector3(12, 0, 6.9f) }, .85f, .058f, Color.white, art != null ? art.path : null);
        }

        private static void PathRibbon(string name, Vector3[] points, float halfWidth, float height, Color color, Texture2D texture)
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
            WorldArt.Mesh(name, null, vertices, uv, triangles, color, texture);
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
            WorldArt.GroundPatch(null, position, new Vector2(2.6f, 2.4f), art != null ? art.path : null);
            var fire = WorldArt.Prop(art != null && art.campfire.Length > 0 ? art.campfire[0] : null, position, .9f);
            if (fire != null) fire.AddComponent<SpriteFlipbook>().Configure(art.campfire, 9f);
            WorldArt.Decal(art != null ? art.glow : null, position + Vector3.up * .04f, new Color(1f, .68f, .32f, .5f), 1.3f);
            WorldArt.Prop(art != null ? art.kazan : null, position + new Vector3(1.9f, 0f, .4f), .8f);
        }

        private void BuildHorses()
        {
            Horse(new Vector3(10.6f, 0f, -3.4f), 0f, art != null ? art.horseBay : null);
            Horse(new Vector3(12.4f, 0f, -4.8f), 12f, art != null ? art.horseGrey : null);
            Horse(new Vector3(-10.8f, 0f, 6.4f), 0f, art != null ? art.horseGrey : null, true);
        }

        private void Horse(Vector3 position, float delay, Sprite[] frames, bool mirrored = false)
        {
            var horse = WorldArt.Prop(frames != null && frames.Length > 0 ? frames[0] : null, position, 2.2f, mirrored);
            if (horse == null) return;
            horse.AddComponent<SpriteFlipbook>().Configure(frames, .6f, delay);
            Solid(horse, new Vector3(2f, 1.6f, .9f));
        }

        /// <summary>Camels, sheep, a cart and a well: the aul reads as lived-in rather than as a set of markers.</summary>
        private void BuildLivestock()
        {
            Camel(new Vector3(-10.4f, 0f, -5.2f), 0f);
            Camel(new Vector3(-12.6f, 0f, -6.4f), 1.4f, true);
            foreach (var spot in new[] { new Vector3(14.5f, 0f, 4.4f), new Vector3(15.8f, 0f, 3.2f), new Vector3(13.4f, 0f, 2.6f), new Vector3(16.4f, 0f, 5.4f) })
                WorldArt.Prop(art != null ? art.sheep : null, spot, 1.2f, spot.x > 15f);
            Solid(WorldArt.Prop(art != null ? art.cart : null, new Vector3(-7.4f, 0f, 8.6f), 2.2f), new Vector3(2.2f, 1.4f, 1f));
            Solid(WorldArt.Prop(art != null ? art.well : null, new Vector3(4.6f, 0f, -6.4f), 1.6f), new Vector3(1.6f, 1.6f, 1.2f));
        }

        private void Camel(Vector3 position, float delay, bool mirrored = false)
        {
            var camel = WorldArt.Prop(art != null && art.camel.Length > 0 ? art.camel[0] : null, position, 2.4f, mirrored);
            if (camel == null) return;
            camel.AddComponent<SpriteFlipbook>().Configure(art.camel, .5f, delay);
            Solid(camel, new Vector3(2f, 1.8f, 1f));
        }

        private static void Solid(GameObject prop, Vector3 size)
        {
            if (prop == null) return;
            var collider = prop.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, size.y * .5f, 0f);
        }

        private void BuildBanners()
        {
            var sprite = art != null ? art.Banner(Banner) : null;
            foreach (var position in new[] { new Vector3(-2.9f, 0, -6f), new Vector3(2.9f, 0, 2f), new Vector3(-2.9f, 0, 10f), new Vector3(2.9f, 0, 13.6f) })
                WorldArt.Prop(sprite, position, .5f);
        }

        private GameObject BuildPlayer(HeroDefinition hero)
        {
            var player = new GameObject((hero != null ? hero.HeroId : "HERO") + " Player");
            player.transform.position = Spawn;
            player.AddComponent<CharacterController>();
            var visual = player.AddComponent<PlayerVisual>();
            visual.SetSpriteSet(LookLibrary.Load(hero != null ? hero.VisualSetId : "kerey"));
            player.AddComponent<PlayerController>();
            WorldArt.AddShadow(player.transform, 1f);
            return player;
        }

        private void BuildPeople()
        {
            if (chapter == null) return;
            var content = Resources.Load<Stage2Content>("Data/Stage2Content");
            foreach (var placement in chapter.npcs) Person(content, placement);
        }

        /// <summary>Spawns one villager; interactive when the placement names an NPC, a background figure otherwise.</summary>
        public static GameObject Person(Stage2Content content, NpcPlacement placement)
        {
            var definition = !string.IsNullOrEmpty(placement.npcId) && content != null ? content.Npc(placement.npcId) : null;
            string look = definition != null ? definition.look : placement.look;
            var person = new GameObject(definition != null ? definition.id : "Aul figure " + look);
            person.transform.position = new Vector3(placement.position.x, 1f, placement.position.y);
            person.AddComponent<CharacterController>();
            var visual = person.AddComponent<PlayerVisual>();
            visual.SetSpriteSet(LookLibrary.Load(look));
            visual.Face(Direction(placement.facing));
            WorldArt.AddShadow(person.transform, 1f);
            if (definition != null) person.AddComponent<NpcInteractable>().Configure(definition);
            return person;
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

        private void Place(GameObject prefab, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0));
            instance.transform.localScale *= scale;
        }
    }
}
