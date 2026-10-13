using ATTILA.UI;
using UnityEngine;

namespace ATTILA.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 14f;
        [Tooltip("Shift held: the aul is wide, and a run saves time when the next person is across it.")]
        [SerializeField] private float runMultiplier = 1.65f;
        private bool inputEnabled = true;
        private float speedMultiplier = 1f;
        private CharacterController controller;
        private PlayerVisual visual;
        private Vector3 velocity;
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            visual = GetComponent<PlayerVisual>();
            if (visual != null) visual.PlaySteps = true;
        }
        private void Update()
        {
            if (!inputEnabled) return;
            Vector2 input = VirtualInput.Movement();
            Vector3 move = new(input.x, 0f, input.y);
            bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            // On horseback the race sets the pace; Shift only speeds up walking.
            float run = running && Mathf.Approximately(speedMultiplier, 1f) ? runMultiplier : 1f;
            if (visual != null) visual.Pace = run;
            if (move.sqrMagnitude > 0.001f)
            {
                transform.forward = Vector3.Slerp(transform.forward, move, turnSpeed * Time.deltaTime);
                visual.SetMovement(move, true);
            }
            else visual.SetMovement(transform.forward, false);
            velocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move((move * (moveSpeed * speedMultiplier * run) + velocity) * Time.deltaTime);
            if (controller.isGrounded && velocity.y < 0f) velocity.y = -1f;
        }
        public void SetInputEnabled(bool value) => inputEnabled = value;
        /// <summary>Riding a horse during the бәйге is the same controller, only faster.</summary>
        public void SetSpeedMultiplier(float value) => speedMultiplier = Mathf.Max(.1f, value);
    }
}
