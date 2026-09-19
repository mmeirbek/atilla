using ATTILA.UI;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ATTILA.Editor
{
    public static class MainMenuArtBuilder
    {
        private const string BackgroundPath = "Assets/ATTILA/Art/Menu/Background/ProvidedMainMenuBackground.png";
        private const string LogoPath = "Assets/ATTILA/Art/Menu/Logo/ProvidedAttilaLogo.png";
        private const string PlayButtonPath = "Assets/ATTILA/Art/Menu/UI/ProvidedPlayButton.png";
        private const string WardrobeButtonPath = "Assets/ATTILA/Art/Menu/UI/ProvidedWardrobeButton.png";
        private const string EncyclopediaButtonPath = "Assets/ATTILA/Art/Menu/UI/ProvidedEncyclopediaButton.png";
        private const string SettingsButtonPath = "Assets/ATTILA/Art/Menu/UI/ProvidedSettingsButton.png";
        private const string QuitButtonPath = "Assets/ATTILA/Art/Menu/UI/ProvidedQuitButton.png";
        private const string KereyKhanCardPath = "Assets/ATTILA/Art/Characters/Menu/ProvidedKereyKhanCard.png";
        private const string CharacterPath = "Assets/ATTILA/Art/Menu/Character/TemporaryBatyr.png";
        private const string KhanPath = "Assets/ATTILA/Art/Characters/Menu/TemporaryKhan.png";
        private const string DefinitionPath = "Assets/ATTILA/Resources/UI/MainMenuVisuals.asset";
        private const string MaterialPath = "Assets/ATTILA/Art/Menu/UI/TemporaryBatyrBlackKey.mat";

        [MenuItem("ATTILA/Build Main Menu Visual Assets")]
        public static void Build()
        {
            ConfigureSprite(BackgroundPath, 1);
            ConfigureSprite(LogoPath, 1);
            ConfigureSprite(PlayButtonPath, 1);
            ConfigureSprite(WardrobeButtonPath, 1);
            ConfigureSprite(EncyclopediaButtonPath, 1);
            ConfigureSprite(SettingsButtonPath, 1);
            ConfigureSprite(QuitButtonPath, 1);
            ConfigureSprite(KereyKhanCardPath, 1);
            ConfigureSprite(CharacterPath, 1);
            ConfigureSprite(KhanPath, 1);
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(LogoPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(PlayButtonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(WardrobeButtonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(EncyclopediaButtonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(SettingsButtonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(QuitButtonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(KereyKhanCardPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(CharacterPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(KhanPath, ImportAssetOptions.ForceSynchronousImport);

            var definition = AssetDatabase.LoadAssetAtPath<MenuVisualDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MenuVisualDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("ATTILA/Menu Black Key"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            definition.background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            definition.logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            definition.playButton = AssetDatabase.LoadAssetAtPath<Sprite>(PlayButtonPath);
            definition.wardrobeButton = AssetDatabase.LoadAssetAtPath<Sprite>(WardrobeButtonPath);
            definition.encyclopediaButton = AssetDatabase.LoadAssetAtPath<Sprite>(EncyclopediaButtonPath);
            definition.settingsButton = AssetDatabase.LoadAssetAtPath<Sprite>(SettingsButtonPath);
            definition.quitButton = AssetDatabase.LoadAssetAtPath<Sprite>(QuitButtonPath);
            definition.kereyKhanCard = AssetDatabase.LoadAssetAtPath<Sprite>(KereyKhanCardPath);
            definition.character = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterPath);
            definition.khan = AssetDatabase.LoadAssetAtPath<Sprite>(KhanPath);
            definition.characterMaterial = material;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log("ATTILA Main Menu visual assets are ready.");
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

    }
}
