using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Environment
{
    /// <summary>
    /// Catalogue of the generated MVP art (Art/Generated). Filled by ATTILA > Art > Import Generated Art.
    /// Replacing a PNG at the same path keeps every reference; code only reads this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "ATTILA/MVP Art Set")]
    public sealed class MvpArtSet : ScriptableObject
    {
        [Serializable]
        public sealed class NamedSprite
        {
            public string id;
            public Sprite sprite;
        }

        [Header("Tiling textures")]
        public Texture2D ground;
        public Texture2D path;
        public Texture2D yurtWall;
        public Texture2D yurtRoof;
        public Texture2D yurtDoor;

        [Header("World props")]
        public Sprite bannerRed;
        public Sprite bannerBlue;
        public Sprite bannerDark;
        public Sprite[] horseBay;
        public Sprite[] horseGrey;
        public Sprite[] campfire;
        public Sprite glow;
        public Sprite shadow;
        public Sprite marker;
        public Sprite kazan;
        public Sprite asyk;
        public Sprite saka;
        public Sprite[] camel;
        public Sprite sheep;
        public Sprite cart;
        public Sprite well;

        [Header("Scenes")]
        public Sprite campfireScene;
        public Sprite mapScene;
        public Sprite asykScene;

        [Header("Icons")]
        public List<NamedSprite> icons = new();

        private static MvpArtSet cached;
        public static MvpArtSet Instance => cached != null ? cached : cached = Resources.Load<MvpArtSet>("Art/MvpArt");

        public Sprite Icon(string id) => icons.Find(icon => icon.id == id)?.sprite;

        public Sprite Banner(string id) => id switch
        {
            "blue" => bannerBlue,
            "dark" => bannerDark,
            _ => bannerRed
        };
    }
}
