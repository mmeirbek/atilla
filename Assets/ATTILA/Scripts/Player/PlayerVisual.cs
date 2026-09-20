using UnityEngine;

namespace ATTILA.Player
{
    public enum FacingDirection { N, NE, E, SE, S, SW, W, NW }
    public sealed class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private DirectionalSpriteSet spriteSet;
        private SpriteRenderer spriteRenderer;
        private Camera viewCamera;
        private float animationTime;
        private float lift;
        public FacingDirection Facing { get; private set; } = FacingDirection.S;
        public bool IsMoving { get; private set; }
        private void Awake()
        {
            if (spriteSet == null) spriteSet = Resources.Load<DirectionalSpriteSet>("Data/TemporaryDirections");
            var root = new GameObject("PlayerVisual - placeholder sprite");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, -.98f, 0f);
            spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }
        /// <summary>Raises the sprite so the character sits on a mount.</summary>
        public void SetLift(float value)
        {
            lift = value;
            if (spriteRenderer != null) spriteRenderer.transform.localPosition = new Vector3(0f, -.98f + lift, 0f);
        }

        /// <summary>Swaps the directional sprites, e.g. to give each role or NPC its own look.</summary>
        public void SetSpriteSet(DirectionalSpriteSet value)
        {
            if (value != null) spriteSet = value;
        }
        public void SetMovement(Vector3 direction, bool isMoving)
        {
            if (!isMoving) { IsMoving = false; animationTime = 0f; return; }
            IsMoving = true;
            Face(direction);
        }

        /// <summary>Turns to face a direction without starting the walk cycle.</summary>
        public void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < .001f) return;
            float angle = Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 22.5f, 360f);
            var next = (FacingDirection)Mathf.FloorToInt(angle / 45f);
            if (next != Facing) animationTime = 0f;
            Facing = next;
        }
        private void LateUpdate()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) spriteRenderer.transform.rotation = viewCamera.transform.rotation;
            if (spriteSet == null) return;
            if (IsMoving) animationTime += Time.deltaTime;
            spriteRenderer.transform.localScale = Vector3.one * spriteSet.visualScale;
            spriteRenderer.sprite = spriteSet.GetSprite(Facing, IsMoving, animationTime);
        }
    }
}
