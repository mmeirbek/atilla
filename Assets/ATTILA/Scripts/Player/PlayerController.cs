using UnityEngine;

namespace ATTILA.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 14f;
        private bool inputEnabled = true;
        private CharacterController controller;
        private PlayerVisual visual;
        private Vector3 velocity;
        private void Awake() { controller = GetComponent<CharacterController>(); visual = GetComponent<PlayerVisual>(); }
        private void Update()
        {
            if (!inputEnabled) return;
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 move = new(input.x, 0f, input.y);
            if (move.sqrMagnitude > 0.001f)
            {
                transform.forward = Vector3.Slerp(transform.forward, move, turnSpeed * Time.deltaTime);
                visual.SetMovement(move, true);
            }
            else visual.SetMovement(transform.forward, false);
            velocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move((move * moveSpeed + velocity) * Time.deltaTime);
            if (controller.isGrounded && velocity.y < 0f) velocity.y = -1f;
        }
        public void SetInputEnabled(bool value) => inputEnabled = value;
    }
}
