using UnityEngine;

namespace ATTILA.Player
{
    [CreateAssetMenu(menuName = "ATTILA/Directional Sprite Set")]
    public sealed class DirectionalSpriteSet : ScriptableObject
    {
        [System.Serializable]
        public sealed class DirectionFrames
        {
            public Sprite idle;
            public Sprite[] walk;
        }
        [Tooltip("N, NE, E, SE, S, SW, W, NW. Use transparent PNGs with bottom-center pivots.")]
        public DirectionFrames[] directions = new DirectionFrames[8];
        [Min(1f)] public float framesPerSecond = 6f;
        [Min(.1f)] public float visualScale = 1f;
        public Sprite GetSprite(FacingDirection facing, bool moving, float time)
        {
            int index = (int)facing;
            if (directions == null || index >= directions.Length || directions[index] == null) return null;
            var frames = directions[index];
            if (moving && frames.walk != null && frames.walk.Length > 0)
                return frames.walk[Mathf.FloorToInt(time * framesPerSecond) % frames.walk.Length] ?? frames.idle;
            return frames.idle;
        }
    }
}
