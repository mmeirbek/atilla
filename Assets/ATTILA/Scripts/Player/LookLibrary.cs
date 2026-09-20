using System.Collections.Generic;
using UnityEngine;

namespace ATTILA.Player
{
    /// <summary>Loads character looks by id. Every look lives at Resources/Data/Looks/&lt;id&gt;.</summary>
    public static class LookLibrary
    {
        public const string Folder = "Data/Looks/";
        private const int PortraitHeight = 40;
        private static readonly Dictionary<string, Sprite> Crops = new();

        public static DirectionalSpriteSet Load(string lookId) =>
            string.IsNullOrEmpty(lookId) ? null : Resources.Load<DirectionalSpriteSet>(Folder + lookId);

        /// <summary>The look's painted bust, or a head crop of its south-facing sprite when no bust exists.</summary>
        public static Sprite Portrait(string lookId)
        {
            var set = Load(lookId);
            if (set == null) return null;
            if (set.portrait != null) return set.portrait;
            if (Crops.TryGetValue(lookId, out var cached) && cached != null) return cached;
            var idle = set.directions != null && set.directions.Length > (int)FacingDirection.S ? set.directions[(int)FacingDirection.S]?.idle : null;
            if (idle == null) return null;
            var cell = idle.textureRect;
            var crop = Sprite.Create(idle.texture, new Rect(cell.x + 2f, cell.yMax - PortraitHeight, cell.width - 4f, PortraitHeight), new Vector2(.5f, .5f), 100f);
            crop.name = lookId + " portrait";
            Crops[lookId] = crop;
            return crop;
        }
    }
}
