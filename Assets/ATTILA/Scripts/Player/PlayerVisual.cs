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
        private float idleTime;
        private float seed;
        private float lean;
        private int lastStep = -1;
        /// <summary>Only the player's feet are heard; a crowd of footsteps would be noise.</summary>
        public bool PlaySteps { get; set; }
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
            // Every character breathes on its own rhythm, so a crowd never moves in lockstep.
            seed = Random.value * 10f;
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
            if (spriteSet == null) return;
            if (IsMoving) animationTime += Time.deltaTime;
            idleTime += Time.deltaTime;
            spriteRenderer.sprite = spriteSet.GetSprite(Facing, IsMoving, animationTime);

            // Motion on top of the drawn frames: a bounce on every step and a lean into the direction of travel
            // while walking; a slow breath while standing. The sprite pivot is at the feet, so they stay planted.
            float scale = spriteSet.visualScale;
            float bob = 0f, stretch = 1f, squash = 1f;
            int side = Facing is FacingDirection.E or FacingDirection.NE or FacingDirection.SE ? 1
                     : Facing is FacingDirection.W or FacingDirection.NW or FacingDirection.SW ? -1 : 0;
            if (IsMoving)
            {
                // Highest in the middle of the passing frame, lowest when a foot lands.
                float phase = (animationTime * spriteSet.StepRate - .5f) * Mathf.PI * .5f;
                int step = Mathf.FloorToInt(animationTime * spriteSet.StepRate);
                if (PlaySteps && lift <= 0f && step != lastStep && step % 2 == 0)
                    ATTILA.Audio.AudioService.Play(ATTILA.Audio.Sfx.Step, .3f, Random.Range(.85f, 1.15f));
                lastStep = step;
                float beat = Mathf.Abs(Mathf.Sin(phase));
                bob = beat * .07f;
                stretch = 1f + beat * .025f;
                squash = 1f - beat * .015f;
            }
            else
            {
                lastStep = -1;
                float breath = Mathf.Sin(idleTime * 2.1f + seed);
                stretch = 1f + breath * .012f;
                squash = 1f - breath * .006f;
            }
            lean = Mathf.MoveTowards(lean, IsMoving ? -side * 4f : 0f, Time.deltaTime * 40f);
            spriteRenderer.transform.localPosition = new Vector3(0f, -.98f + lift + bob, 0f);
            spriteRenderer.transform.localScale = new Vector3(scale * squash, scale * stretch, scale);
            if (viewCamera != null) spriteRenderer.transform.rotation = viewCamera.transform.rotation * Quaternion.Euler(0f, 0f, lean);
        }
    }
}
