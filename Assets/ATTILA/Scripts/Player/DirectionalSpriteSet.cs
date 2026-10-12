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
        [Tooltip("Head-and-shoulders portrait for dialogue and HUD. Optional.")] public Sprite portrait;
        public string displayName;
        /// <summary>Steps per second of the walk cycle; the visual bob is timed to the same beat.</summary>
        public float StepRate => framesPerSecond * 1.4f;

        public Sprite GetSprite(FacingDirection facing, bool moving, float time)
        {
            int index = (int)facing;
            if (directions == null || index >= directions.Length || directions[index] == null) return null;
            var frames = directions[index];
            if (!moving || frames.walk == null || frames.walk.Length == 0) return frames.idle;
            int step = Mathf.FloorToInt(time * StepRate);
            // Two drawn strides become a four-beat cycle with the standing pose as the passing frame between them.
            if (frames.walk.Length == 2 && frames.idle != null)
                return (step % 4) switch { 0 => frames.walk[0], 2 => frames.walk[1], _ => frames.idle } ?? frames.idle;
            return frames.walk[step % frames.walk.Length] ?? frames.idle;
        }
    }
}
