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
    /// Builds the place the story is currently in: the aul, the open steppe or the city. The player, the camera
    /// and the story director survive a move; everything else hangs off one root that is thrown away and rebuilt.
    /// </summary>
    public sealed class AulPrototypeBuilder : MonoBehaviour
    {
        private static readonly Vector3[] YurtSpots = { new(-12, 0, 9), new(12, 0, 10), new(-13, 0, -9), new(13, 0, -10) };

        public static AulPrototypeBuilder Active { get; private set; }
        public LocationId Current { get; private set; }

        private EnvironmentArtSet environmentArt;
        private MvpArtSet art;
        private ChapterDefinition chapter;
        private Stage2Content content;
        private GameObject player;
        private PixelCamera pixelCamera;

        public static Vector3 Spawn(LocationId location) => location switch
        {
            LocationId.Steppe => new Vector3(0f, 1f, 16f),
            LocationId.City => new Vector3(0f, 1f, 1f),
            _ => new Vector3(0f, 1f, 0f)
        };

        /// <summary>Where each place sits on the parchment map, used when drawing a travel route.</summary>
        public static Vector2 MapPoint(LocationId location) => location switch
        {
            LocationId.Steppe => new Vector2(.52f, .58f),
            LocationId.City => new Vector2(.45f, .25f),
            _ => new Vector2(.56f, .76f)
        };

        public static string LocationName(LocationId location) => location switch
        {
            LocationId.Steppe => "Дала",
            LocationId.City => "Түркістан",
            _ => "Ауыл"
        };

        private void Start()
        {
            Active = this;
            var director = gameObject.AddComponent<StoryDirector>();
            chapter = director.Chapter;
            content = Resources.Load<Stage2Content>("Data/Stage2Content");
            art = MvpArtSet.Instance;
            environmentArt = Resources.Load<EnvironmentArtSet>("Art/AulEnvironment");
            var start = director.Quest != null && director.Quest.Current != null ? director.Quest.Current.location : LocationId.Aul;
            player = BuildPlayer(director.Hero, start);
            var camera = BuildCamera(player.transform);
            pixelCamera = PixelCamera.Attach(camera, Tint(start));
            gameObject.AddComponent<QuestMarker>();
            gameObject.AddComponent<AllyWatch>().Initialize(chapter, content);
            BuildLocation(start);
            director.Initialize(player.GetComponent<PlayerController>());
        }

        /// <summary>Tears the old place down and raises the new one; the player keeps everything they carry.</summary>
        public void Travel(LocationId location)
        {
            BuildLocation(location);
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = Spawn(location);
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        private void BuildLocation(LocationId location)
        {
            Current = location;
            WorldArt.NewRoot("World " + location);
            Atmosphere(location);
            switch (location)
            {
                case LocationId.Steppe: BuildSteppe(); break;
                case LocationId.City: BuildCity(); break;
                default: BuildAul(); break;
            }
            BuildPeople(location);
            BuildTravelPoint(location);
            if (pixelCamera != null) pixelCamera.SetTint(Tint(location));
        }

        private Color Tint(LocationId location)
        {
            var baseTint = chapter != null ? chapter.worldTint : Color.white;
            var shift = location switch
            {
                LocationId.Steppe => new Color(1.02f, .98f, .88f),
                LocationId.City => new Color(1.04f, .96f, .86f),
                _ => Color.white
            };
            return new Color(baseTint.r * shift.r, baseTint.g * shift.g, baseTint.b * shift.b);
        }

        private string Banner => chapter != null ? chapter.banner : "red";

        private void Atmosphere(LocationId location)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = chapter != null ? chapter.ambientColor : new Color(.90f, .93f, .86f);
            RenderSettings.fog = false;
            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = location == LocationId.City ? new Color(.78f, .72f, .62f)
                    : chapter != null ? chapter.skyColor : new Color(.65f, .82f, .91f);
        }

        // ------------------------------------------------------------ the aul

        private void BuildAul()
        {
            Ground(26f);
            Vector3[] route = { new(0, 0, -22), new(-.2f, 0, -16), new(.15f, 0, -9), new(0, 0, -2), new(-.15f, 0, 5), new(.1f, 0, 12), new(0, 0, 22) };
            PathRibbon("Soft path verge", route, 2.02f, .052f, new Color(.78f, .76f, .52f), null);
            PathRibbon("Main earth path", route, 1.78f, .056f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach west", new[] { new Vector3(-12, 0, 5.9f), new Vector3(-7, 0, 5.2f), new Vector3(0, 0, 5.5f) }, .85f, .058f, Color.white, art != null ? art.path : null);
            PathRibbon("Yurt approach east", new[] { new Vector3(0, 0, 5.5f), new Vector3(6, 0, 6f), new Vector3(12, 0, 6.9f) }, .85f, .058f, Color.white, art != null ? art.path : null);
            foreach (var spot in YurtSpots) YurtSite.Create(spot, Banner, YurtSite.Stages);
            Groves(new[] { new Vector3(-17, 0, 13), new Vector3(17, 0, 14), new Vector3(-18, 0, -14), new Vector3(18, 0, -15), new Vector3(-5, 0, 18), new Vector3(7, 0, -19), new Vector3(-15.5f, 0, 1.5f), new Vector3(15.5f, 0, -.5f) });
            Rocks(new[] { new Vector3(-7, 0, -5), new Vector3(7, 0, 4), new Vector3(-18, 0, 2), new Vector3(17, 0, 2) });
            Place(environmentArt.logs, new Vector3(-8.5f, 0, 10f), 15f);
            Place(environmentArt.logs, new Vector3(16.2f, 0, -9.5f), -20f);
            foreach (float x in new[] { -9f, -6f, -3.5f, 3.5f, 6f, 9f }) Place(environmentArt.fence, new Vector3(x, 0, 14));
            Campfire(new Vector3(-3.6f, 0f, -1.6f));
            Horse(new Vector3(10.6f, 0f, -3.4f), 0f, art != null ? art.horseBay : null);
            Horse(new Vector3(12.4f, 0f, -4.8f), 12f, art != null ? art.horseGrey : null);
            Horse(new Vector3(-10.8f, 0f, 6.4f), 0f, art != null ? art.horseGrey : null, true);
            Camel(new Vector3(-10.4f, 0f, -5.2f), 0f);
            Camel(new Vector3(-12.6f, 0f, -6.4f), 1.4f, true);
            foreach (var spot in new[] { new Vector3(14.5f, 0f, 4.4f), new Vector3(15.8f, 0f, 3.2f), new Vector3(13.4f, 0f, 2.6f), new Vector3(16.4f, 0f, 5.4f) })
                WorldArt.Prop(art != null ? art.sheep : null, spot, 1.2f, spot.x > 15f);
            Solid(WorldArt.Prop(art != null ? art.cart : null, new Vector3(-7.4f, 0f, 8.6f), 2.2f), new Vector3(2.2f, 1.4f, 1f));
            Solid(WorldArt.Prop(art != null ? art.well : null, new Vector3(4.6f, 0f, -6.4f), 1.6f), new Vector3(1.6f, 1.6f, 1.2f));
            var banner = art != null ? art.Banner(Banner) : null;
            foreach (var position in new[] { new Vector3(-2.9f, 0, -6f), new Vector3(2.9f, 0, 2f), new Vector3(-2.9f, 0, 10f), new Vector3(2.9f, 0, 13.6f) })
                WorldArt.Prop(banner, position, .5f);
        }

        // ------------------------------------------------------------ the open steppe

        private void BuildSteppe()
        {
            Ground(34f);
            Vector3[] trail = { new(0, 0, 24), new(1.5f, 0, 14), new(-1f, 0, 4), new(1f, 0, -8), new(0, 0, -22) };
            PathRibbon("Steppe trail", trail, 1.5f, .055f, Color.white, art != null ? art.path : null);
            Groves(new[] { new Vector3(-20, 0, 12), new Vector3(19, 0, 6), new Vector3(-16, 0, -12), new Vector3(15, 0, -16), new Vector3(-8, 0, 20) });
            Rocks(new[] { new Vector3(-11, 0, 4), new Vector3(9, 0, -3), new Vector3(-6, 0, -14), new Vector3(13, 0, 12), new Vector3(-17, 0, -3) });
            for (int i = 0; i < 26; i++)
            {
                float angle = i * 2.4f;
                Place(environmentArt.grass, new Vector3(Mathf.Cos(angle) * (5 + i * .7f), 0, Mathf.Sin(angle) * (5 + i * .8f)), i * 37f, 1.1f);
            }
            foreach (var spot in new[] { new Vector3(-13.5f, 0f, -6.5f), new Vector3(-12f, 0f, -8f), new Vector3(-15f, 0f, -8.6f), new Vector3(-11f, 0f, -5f) })
                WorldArt.Prop(art != null ? art.sheep : null, spot, 1.2f);
            Horse(new Vector3(8.5f, 0f, 9.5f), 0f, art != null ? art.horseGrey : null);
            Horse(new Vector3(10.2f, 0f, 8.2f), .8f, art != null ? art.horseBay : null, true);
            Camel(new Vector3(14f, 0f, -6f), .4f);
            Campfire(new Vector3(-4.2f, 0f, -4.4f));
            YurtSite.Create(new Vector3(-6.5f, 0f, 10.5f), Banner, YurtSite.Stages);
            var banner = art != null ? art.Banner(Banner) : null;
            foreach (var position in new[] { new Vector3(-2.4f, 0, 18f), new Vector3(2.4f, 0, -18f) })
                WorldArt.Prop(banner, position, .5f);
        }

        // ------------------------------------------------------------ the city

        private void BuildCity()
        {
            Ground(30f, art != null ? art.path : null);
            PathRibbon("City street", new[] { new Vector3(0, 0, -16), new Vector3(0, 0, -4), new Vector3(0, 0, 6), new Vector3(0, 0, 14) }, 2.6f, .06f, new Color(.88f, .82f, .7f), art != null ? art.path : null);
            PathRibbon("Bazaar row", new[] { new Vector3(-11, 0, 1.5f), new Vector3(0, 0, 1.5f), new Vector3(11, 0, 1.5f) }, 2.2f, .062f, new Color(.88f, .82f, .7f), art != null ? art.path : null);
            Wall(new Vector3(0, 0, 16), new Vector3(34, 3.6f, 1.2f));
            Wall(new Vector3(-16, 0, 0), new Vector3(1.2f, 3.6f, 32));
            Wall(new Vector3(16, 0, 0), new Vector3(1.2f, 3.6f, 32));
            Wall(new Vector3(-9.6f, 0, -16), new Vector3(13, 3.6f, 1.2f));
            Wall(new Vector3(9.6f, 0, -16), new Vector3(13, 3.6f, 1.2f));
            Tower(new Vector3(-3.2f, 0, -16));
            Tower(new Vector3(3.2f, 0, -16));
            Tower(new Vector3(-16, 0, 16));
            Tower(new Vector3(16, 0, 16));
            // A darker walkway on top keeps the wall from reading as more ground.
            foreach (var (centre, size) in new[] { (new Vector3(0, 0, 16), new Vector3(34, 0, 1.2f)), (new Vector3(-16, 0, 0), new Vector3(1.2f, 0, 32)),
                                                   (new Vector3(16, 0, 0), new Vector3(1.2f, 0, 32)), (new Vector3(-9.6f, 0, -16), new Vector3(13, 0, 1.2f)),
                                                   (new Vector3(9.6f, 0, -16), new Vector3(13, 0, 1.2f)) })
                WorldArt.Primitive(PrimitiveType.Cube, "Wall walk", centre + Vector3.up * 3.7f, new Vector3(size.x - .1f, .25f, size.z - .1f))
                    .GetComponent<Renderer>().sharedMaterial = WorldArt.SharedFlat(new Color(.62f, .5f, .38f));
            Building(new Vector3(-9.5f, 0, 9.5f), new Vector3(7, 3.6f, 6));
            Building(new Vector3(9.5f, 0, 10f), new Vector3(8, 4f, 5.5f));
            Building(new Vector3(-10.5f, 0, -6.5f), new Vector3(6, 3.4f, 7));
            Building(new Vector3(10.5f, 0, -7f), new Vector3(7, 3.8f, 6));
            Mausoleum(new Vector3(0, 0, 12f));
            foreach (float x in new[] { -5.4f, 0f, 5.4f }) Stall(new Vector3(x, 0f, x == 0f ? 4.4f : -1.6f));
            foreach (var green in new[] { new Vector3(-13.5f, 0, 2f), new Vector3(13.5f, 0, 3f), new Vector3(-12.5f, 0, -12f), new Vector3(12.5f, 0, -13f) })
            {
                Place(environmentArt.smallTree, green, green.x * 13f);
                Place(environmentArt.bush, green + new Vector3(1.2f, 0, -1f), green.z * 17f);
            }
            Solid(WorldArt.Prop(art != null ? art.well : null, new Vector3(-5.2f, 0f, -4.8f), 1.6f), new Vector3(1.6f, 1.6f, 1.2f));
            Solid(WorldArt.Prop(art != null ? art.cart : null, new Vector3(6.2f, 0f, -5.6f), 2.2f), new Vector3(2.2f, 1.4f, 1f));
            Camel(new Vector3(8.4f, 0f, -10.6f), .2f);
            Camel(new Vector3(10.4f, 0f, -11.8f), 1.1f, true);
            var banner = art != null ? art.Banner(Banner) : null;
            foreach (var position in new[] { new Vector3(-4.4f, 0, -14.6f), new Vector3(4.4f, 0, -14.6f), new Vector3(-3.2f, 0, 10f), new Vector3(3.2f, 0, 10f) })
                WorldArt.Prop(banner, position, .5f);
        }

        /// <summary>The city's landmark: a clay mass with a glazed turquoise dome and a tall portal.</summary>
        private void Mausoleum(Vector3 position)
        {
            var body = WorldArt.Primitive(PrimitiveType.Cube, "Mausoleum", position + Vector3.up * 3f, new Vector3(11, 6, 8));
            body.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, new Vector2(8f, 4f), new Color(.94f, .88f, .78f));
            var portal = WorldArt.Primitive(PrimitiveType.Cube, "Portal", position + new Vector3(0f, 4.2f, -4.2f), new Vector3(7.4f, 8.4f, 1.4f));
            portal.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, new Vector2(5f, 6f), new Color(.98f, .92f, .82f));
            var arch = WorldArt.Primitive(PrimitiveType.Cube, "Arch", position + new Vector3(0f, 2.2f, -4.9f), new Vector3(3.4f, 4.4f, .5f));
            arch.GetComponent<Renderer>().sharedMaterial = WorldArt.SharedFlat(new Color(.26f, .18f, .16f));
            var band = WorldArt.Primitive(PrimitiveType.Cube, "Tiled band", position + new Vector3(0f, 7.6f, -4.2f), new Vector3(7.6f, 1.2f, 1.5f));
            band.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.tiles : null, new Vector2(4f, 1f), Color.white);
            var dome = WorldArt.Primitive(PrimitiveType.Sphere, "Dome", position + new Vector3(0f, 7.4f, 1.2f), new Vector3(7.6f, 6.4f, 7.6f));
            dome.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.tiles : null, new Vector2(3f, 2f), Color.white);
            var drum = WorldArt.Primitive(PrimitiveType.Cylinder, "Drum", position + new Vector3(0f, 6.2f, 1.2f), new Vector3(6.4f, 1.2f, 6.4f));
            drum.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.tiles : null, new Vector2(6f, 1f), new Color(.86f, .92f, .92f));
            var small = WorldArt.Primitive(PrimitiveType.Sphere, "Small dome", position + new Vector3(-4.2f, 6.6f, 2.6f), new Vector3(3.4f, 2.8f, 3.4f));
            small.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.tiles : null, new Vector2(2f, 1f), Color.white);
        }

        private void Wall(Vector3 centre, Vector3 size)
        {
            var wall = WorldArt.Primitive(PrimitiveType.Cube, "City wall", centre + Vector3.up * size.y * .5f, size);
            // One brick course is about a third of a unit, so the wall reads at the character's scale.
            wall.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.cityWall : null, new Vector2(Mathf.Max(size.x, size.z) * .7f, size.y * .7f), Color.white);
        }

        private void Tower(Vector3 position)
        {
            var tower = WorldArt.Primitive(PrimitiveType.Cylinder, "Tower", position + Vector3.up * 2.4f, new Vector3(2.6f, 2.4f, 2.6f));
            tower.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.cityWall : null, new Vector2(5f, 3f), Color.white);
            var cap = WorldArt.Primitive(PrimitiveType.Cylinder, "Tower cap", position + Vector3.up * 4.9f, new Vector3(3.1f, .25f, 3.1f));
            cap.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, Vector2.one, new Color(.82f, .7f, .56f));
        }

        private void Building(Vector3 position, Vector3 size)
        {
            var body = WorldArt.Primitive(PrimitiveType.Cube, "Clay house", position + Vector3.up * size.y * .5f, size);
            body.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, new Vector2(size.x * .7f, size.y * .7f), Color.white);
            var roof = WorldArt.Primitive(PrimitiveType.Cube, "Flat roof", position + Vector3.up * (size.y + .2f), new Vector3(size.x + .8f, .4f, size.z + .8f));
            roof.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, Vector2.one, new Color(.74f, .62f, .48f));
            var door = WorldArt.Primitive(PrimitiveType.Cube, "Door", position + new Vector3(0f, 1.1f, -size.z * .5f - .1f), new Vector3(1.3f, 2.2f, .2f));
            door.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.yurtDoor : null, Vector2.one, Color.white);
        }

        private void Stall(Vector3 position)
        {
            var table = WorldArt.Primitive(PrimitiveType.Cube, "Stall", position + Vector3.up * .5f, new Vector3(3.4f, 1f, 1.8f));
            table.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.clay : null, Vector2.one, new Color(.72f, .56f, .4f));
            var awning = WorldArt.Primitive(PrimitiveType.Cube, "Awning", position + Vector3.up * 2.3f, new Vector3(4f, .25f, 2.6f));
            awning.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art != null ? art.awning : null, new Vector2(2f, 1f), Color.white);
            foreach (float side in new[] { -1.7f, 1.7f })
                WorldArt.Primitive(PrimitiveType.Cube, "Post", position + new Vector3(side, 1.15f, 0f), new Vector3(.2f, 2.3f, .2f))
                    .GetComponent<Renderer>().sharedMaterial = WorldArt.SharedFlat(new Color(.5f, .36f, .24f));
        }

        // ------------------------------------------------------------ shared pieces

        private void Ground(float tiling, Texture2D texture = null)
        {
            var ground = WorldArt.Primitive(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(5.5f, 1, 5.5f));
            ground.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(texture != null ? texture : art != null ? art.ground : null, new Vector2(tiling, tiling), Color.white);
        }

        private void Groves(Vector3[] groves)
        {
            for (int i = 0; i < groves.Length; i++)
            {
                var p = groves[i];
                Place(i % 3 == 0 ? environmentArt.smallTree : environmentArt.tree, p, i * 51f);
                Place(environmentArt.bush, p + new Vector3(1.4f, 0, .6f), i * 27f);
                Place(environmentArt.smallBush, p + new Vector3(-1.1f, 0, -.3f), i * 37f);
                Place(environmentArt.grass, p + new Vector3(.8f, 0, -1.2f), i * 43f, 1.2f);
            }
        }

        private void Rocks(Vector3[] spots)
        {
            foreach (var p in spots)
            {
                Place(environmentArt.rock, p, 25f);
                Place(environmentArt.smallRock, p + new Vector3(.65f, 0, .4f), 140f);
                Place(environmentArt.grass, p + new Vector3(-.6f, 0, .3f), 70f);
            }
        }

        private void Campfire(Vector3 position)
        {
            WorldArt.GroundPatch(null, position, new Vector2(2.6f, 2.4f), art != null ? art.path : null);
            var fire = WorldArt.Prop(art != null && art.campfire.Length > 0 ? art.campfire[0] : null, position, .9f);
            if (fire != null) fire.AddComponent<SpriteFlipbook>().Configure(art.campfire, 9f);
            WorldArt.Decal(art != null ? art.glow : null, position + Vector3.up * .04f, new Color(1f, .68f, .32f, .5f), 1.3f);
            WorldArt.Prop(art != null ? art.kazan : null, position + new Vector3(1.9f, 0f, .4f), .8f);
        }

        private void Horse(Vector3 position, float delay, Sprite[] frames, bool mirrored = false)
        {
            var horse = WorldArt.Prop(frames != null && frames.Length > 0 ? frames[0] : null, position, 2.2f, mirrored);
            if (horse == null) return;
            horse.AddComponent<SpriteFlipbook>().Configure(frames, .6f, delay);
            Solid(horse, new Vector3(2f, 1.6f, .9f));
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

        /// <summary>The cart at the edge of the road: talking to it opens the travel map.</summary>
        private void BuildTravelPoint(LocationId location)
        {
            var position = location switch
            {
                LocationId.Steppe => new Vector3(2.8f, 0f, 20.5f),
                LocationId.City => new Vector3(4.2f, 0f, -12.4f),
                _ => new Vector3(2.8f, 0f, -18.5f)
            };
            var point = WorldArt.Prop(art != null ? art.cart : null, position, 2.2f);
            if (point == null) return;
            point.name = "Travel point";
            point.AddComponent<TravelPoint>();
            WorldArt.Prop(art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null, position + new Vector3(-2.6f, 0f, .4f), 2.2f);
        }

        private GameObject BuildPlayer(HeroDefinition hero, LocationId location)
        {
            var created = new GameObject((hero != null ? hero.HeroId : "HERO") + " Player");
            created.transform.position = Spawn(location);
            created.AddComponent<CharacterController>();
            var visual = created.AddComponent<PlayerVisual>();
            visual.SetSpriteSet(LookLibrary.Load(hero != null ? hero.VisualSetId : "kerey"));
            created.AddComponent<PlayerController>();
            WorldArt.AddShadow(created.transform, 1f);
            return created;
        }

        private void BuildPeople(LocationId location)
        {
            if (chapter == null) return;
            foreach (var placement in chapter.npcs)
                if (placement.location == location) Person(content, placement);
        }

        /// <summary>Spawns one villager; interactive when the placement names an NPC, a background figure otherwise.</summary>
        public static GameObject Person(Stage2Content content, NpcPlacement placement)
        {
            var definition = !string.IsNullOrEmpty(placement.npcId) && content != null ? content.Npc(placement.npcId) : null;
            string look = definition != null ? definition.look : placement.look;
            var person = new GameObject(definition != null ? definition.id : "Figure " + look);
            if (WorldArt.Root != null) person.transform.SetParent(WorldArt.Root, true);
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

        private void Place(GameObject prefab, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0));
            instance.transform.localScale *= scale;
            if (WorldArt.Root != null) instance.transform.SetParent(WorldArt.Root, true);
        }
    }

    /// <summary>Marks the spot where the player can set out for another place.</summary>
    public sealed class TravelPoint : MonoBehaviour
    {
        public const float Radius = 3.2f;
    }
}
