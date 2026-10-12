using System.IO;
using System.Linq;
using ATTILA.Environment;
using ATTILA.Player;
using UnityEditor;
using UnityEngine;

namespace ATTILA.Editor
{
    /// <summary>
    /// Applies import settings to Art/Generated (written by Tools/ArtGen) and rebuilds the look and art catalogue assets.
    /// Safe to rerun after replacing any PNG with final art of the same name.
    /// </summary>
    public static class MvpArtImporter
    {
        private const string Root = "Assets/ATTILA/Art/Generated/";
        private const string LooksFolder = "Assets/ATTILA/Resources/Data/Looks/";
        private const string CatalogPath = "Assets/ATTILA/Resources/Art/MvpArt.asset";
        private const float WorldPixelsPerUnit = 30f;
        private static readonly string[] Directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        public static readonly (string id, string name)[] Looks =
        {
            ("kerey", "Керей"), ("abylai", "Абылай"), ("kabanbay", "Қабанбай"), ("bi", "Би"), ("elder", "Ақсақал"),
            ("woman", "Айша"), ("child", "Бала"), ("child_girl", "Қыз бала"), ("warrior", "Сарбаз"), ("envoy", "Елші"),
            ("envoy2", "Елші"), ("resident", "Тұрғын"), ("raider", "Жау батыры")
        };

        [MenuItem("ATTILA/Art/Import Generated Art")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
                Configure(AssetDatabase.GUIDToAssetPath(guid));
            BuildLooks();
            BuildCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log("ATTILA ART IMPORT PASS: generated art configured, looks and catalogue rebuilt.");
        }

        private static void Configure(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            string folder = path.Substring(Root.Length).Split('/')[0];
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            if (folder == "Environment")
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool world = folder == "Characters" || folder == "Props";
            string file = Path.GetFileNameWithoutExtension(path);
            bool centred = file is "glow" or "shadow" or "asyk" or "saka" or "arrow";
            // Pixels per unit has to go through the settings object: SetTextureSettings overwrites the importer field.
            settings.spritePixelsPerUnit = world ? WorldPixelsPerUnit : 100f;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = world && !centred ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite SpriteAt(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + relative);

        private static void BuildLooks()
        {
            Directory.CreateDirectory(LooksFolder);
            foreach (var (id, name) in Looks)
            {
                string path = LooksFolder + id + ".asset";
                var set = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(path);
                if (set == null) { set = ScriptableObject.CreateInstance<DirectionalSpriteSet>(); AssetDatabase.CreateAsset(set, path); }
                set.directions = new DirectionalSpriteSet.DirectionFrames[8];
                for (int i = 0; i < 8; i++)
                {
                    string d = Directions[i];
                    set.directions[i] = new DirectionalSpriteSet.DirectionFrames
                    {
                        idle = SpriteAt($"Characters/{id}/{id}_idle_{d}_00.png"),
                        walk = new[] { SpriteAt($"Characters/{id}/{id}_walk_{d}_00.png"), SpriteAt($"Characters/{id}/{id}_walk_{d}_01.png") }
                    };
                }
                set.framesPerSecond = 6f;
                set.visualScale = 1f;
                set.portrait = SpriteAt($"Portraits/{id}.png");
                set.displayName = name;
                EditorUtility.SetDirty(set);
            }
        }

        private static void BuildCatalog()
        {
            var set = AssetDatabase.LoadAssetAtPath<MvpArtSet>(CatalogPath);
            if (set == null) { set = ScriptableObject.CreateInstance<MvpArtSet>(); AssetDatabase.CreateAsset(set, CatalogPath); }
            set.ground = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/ground.png");
            set.path = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/path.png");
            set.yurtWall = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/yurt_wall.png");
            set.yurtRoof = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/yurt_roof.png");
            set.yurtDoor = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/yurt_door.png");
            set.bannerRed = SpriteAt("Props/banner_red.png");
            set.bannerBlue = SpriteAt("Props/banner_blue.png");
            set.bannerDark = SpriteAt("Props/banner_dark.png");
            set.horseBay = new[] { SpriteAt("Props/horse_bay_0.png"), SpriteAt("Props/horse_bay_1.png") };
            set.horseGrey = new[] { SpriteAt("Props/horse_grey_0.png"), SpriteAt("Props/horse_grey_1.png") };
            set.horseBayGallop = Enumerable.Range(0, 4).Select(i => SpriteAt($"Props/horse_bay_gallop_{i}.png")).ToArray();
            set.horseGreyGallop = Enumerable.Range(0, 4).Select(i => SpriteAt($"Props/horse_grey_gallop_{i}.png")).ToArray();
            set.campfire = Enumerable.Range(0, 4).Select(i => SpriteAt($"Props/campfire_{i}.png")).ToArray();
            set.glow = SpriteAt("Props/glow.png");
            set.shadow = SpriteAt("Props/shadow.png");
            set.marker = SpriteAt("Props/marker.png");
            set.kazan = SpriteAt("Props/kazan.png");
            set.asyk = SpriteAt("Props/asyk.png");
            set.saka = SpriteAt("Props/saka.png");
            set.camel = new[] { SpriteAt("Props/camel_0.png"), SpriteAt("Props/camel_1.png") };
            set.sheep = SpriteAt("Props/sheep.png");
            set.cart = SpriteAt("Props/cart.png");
            set.well = SpriteAt("Props/well.png");
            set.post = SpriteAt("Props/post.png");
            set.arrow = SpriteAt("Props/arrow.png");
            set.sandyq = SpriteAt("Props/sandyq.png");
            set.tandyr = SpriteAt("Props/tandyr.png");
            set.hay = SpriteAt("Props/hay.png");
            set.tuft = SpriteAt("Props/tuft.png");
            set.flowers = new[] { SpriteAt("Props/flowers_0.png"), SpriteAt("Props/flowers_1.png"), SpriteAt("Props/flowers_2.png") };
            set.yurtFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/yurt_frame.png");
            set.yurtPoles = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/yurt_poles.png");
            set.clay = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/clay.png");
            set.cityWall = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/city_wall.png");
            set.awning = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/awning.png");
            set.tiles = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/tiles.png");
            set.water = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Environment/water.png");
            set.props = new[] { "firewood", "torsyk", "kerege", "uyk", "shanyrak", "felt", "post" }
                .Select(id => new MvpArtSet.NamedSprite { id = id, sprite = SpriteAt($"Props/{id}.png") })
                .ToList();
            set.campfireScene = SpriteAt("Scenes/campfire.png");
            set.mapScene = SpriteAt("Scenes/map.png");
            set.asykScene = SpriteAt("Scenes/asyk_ring.png");
            set.icons = AssetDatabase.FindAssets("t:Sprite", new[] { Root + "Icons" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => new MvpArtSet.NamedSprite { id = Path.GetFileNameWithoutExtension(p), sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p) })
                .ToList();
            EditorUtility.SetDirty(set);
        }
    }
}
