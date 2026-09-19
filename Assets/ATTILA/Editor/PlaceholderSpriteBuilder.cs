using System.IO;
using ATTILA.Player;
using UnityEditor;
using UnityEngine;

namespace ATTILA.Editor
{
    // Generates only neutral temporary art. Does not rebuild or modify any scene.
    public static class PlaceholderSpriteBuilder
    {
        private const string Folder = "Assets/ATTILA/Art/Characters/RenderedSprites/Placeholder";
        [MenuItem("ATTILA/Generate Stage 1.5 Placeholder Sprites")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/ATTILA/Resources/Art");
            const string dataPath = "Assets/ATTILA/Resources/Data/TemporaryDirections.asset";
            var set = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(dataPath);
            if (set == null) { set = ScriptableObject.CreateInstance<DirectionalSpriteSet>(); AssetDatabase.CreateAsset(set, dataPath); }
            set.directions = new DirectionalSpriteSet.DirectionFrames[8];
            for (int direction = 0; direction < 8; direction++)
            {
                var frames = new DirectionalSpriteSet.DirectionFrames { walk = new Sprite[2] };
                frames.idle = MakeSprite(direction, 0);
                frames.walk[0] = MakeSprite(direction, 1);
                frames.walk[1] = MakeSprite(direction, -1);
                set.directions[direction] = frames;
            }
            EditorUtility.SetDirty(set);
            const string materialPath = "Assets/ATTILA/Resources/Art/PlaceholderFlat.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")), materialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 1.5 placeholder sprites and flat material generated; existing scenes preserved.");
        }
        private static Sprite MakeSprite(int direction, int step)
        {
            const int width = 48, height = 80;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            Color32 ink = new Color32(58, 64, 66, 255);
            Color32 pants = new Color32(67, 82, 98, 255);
            Color32 shirt = new Color32(69, 139, 164, 255);
            Color32 sleeve = new Color32(57, 115, 140, 255);
            Color32 skin = new Color32(214, 159, 116, 255);
            Color32 hair = new Color32(77, 61, 51, 255);
            bool profile = direction == 2 || direction == 6;
            bool back = direction == 0 || direction == 1 || direction == 7;
            int look = direction >= 1 && direction <= 3 ? 1 : direction >= 5 ? -1 : 0;
            int half = profile ? 5 : look == 0 ? 8 : 7;
            void Rect(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (int y = Mathf.Max(0, y0); y <= Mathf.Min(height - 1, y1); y++)
                    for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++) pixels[y * width + x] = color;
            }
            void Oval(int cx, int cy, int rx, int ry, Color32 color)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                    for (int x = cx - rx; x <= cx + rx; x++)
                        if (x >= 0 && x < width && y >= 0 && y < height &&
                            (x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry) <= 1f)
                            pixels[y * width + x] = color;
            }
            int leg = profile ? 2 : 5;
            Rect(24-leg-3, 4+step*2, 24-leg+2, 29, pants);
            Rect(24+leg-2, 4-step*2, 24+leg+3, 29, pants);
            Rect(24-leg-4+look, 2+step*2, 24-leg+3+look, 5+step*2, ink);
            Rect(24+leg-3+look, 2-step*2, 24+leg+4+look, 5-step*2, ink);
            // Broad plain shirt, no historic costume, accessories, or weapons.
            Rect(24-half, 28, 24+half, 49, shirt);
            Rect(24-half-3, 34-step, 24-half, 48, sleeve);
            Rect(24+half, 34+step, 24+half+3, 48, sleeve);
            Rect(24-half-3, 29-step, 24-half, 34-step, skin);
            Rect(24+half, 29+step, 24+half+3, 34+step, skin);
            Rect(22+look, 49, 26+look, 54, skin);
            Oval(24, 61, 7, 10, skin);
            Oval(24-look, 67, 7, 5, hair);
            if (back)
            {
                Oval(24-look, 60, 7, 9, hair);
                if (look != 0) Rect(24+look*6, 56, 24+look*6+1, 62, skin);
            }
            else if (profile)
            {
                Rect(24+look*6, 58, 24+look*6+look*2, 60, skin);
                if (look < 0) Rect(15, 58, 18, 60, skin);
                Rect(24+look*5, 62, 24+look*5+1, 63, ink);
                Rect(24-look*4, 58, 24-look*4+1, 62, hair);
            }
            else
            {
                int shift = look * 2;
                Rect(21+shift, 60, 22+shift, 61, ink);
                Rect(26+shift, 60, 27+shift, 61, ink);
                Rect(21, 47, 27, 48, new Color32(139, 189, 198, 255));
            }
            texture.SetPixels32(pixels); texture.Apply();
            string action = step == 0 ? "idle" : "walk";
            string path = $"{Folder}/TEMP_HERO_{action}_{(FacingDirection)direction}_{(step < 0 ? 1 : 0):00}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 30f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, 0f);
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
