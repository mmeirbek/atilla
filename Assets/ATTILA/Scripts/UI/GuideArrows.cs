using ATTILA.Environment;
using ATTILA.Gameplay;
using ATTILA.NPC;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>
    /// Lays a short trail of arrows on the ground in front of the player, pointing at whatever the current beat
    /// wants: the NPC, the travel cart or the next task point. They fade out once the target is close.
    /// </summary>
    public sealed class GuideArrows : MonoBehaviour
    {
        private const int Count = 4;
        private const float Spacing = 2.1f;
        private const float HideDistance = 4.5f;

        private readonly SpriteRenderer[] arrows = new SpriteRenderer[Count];
        private Transform player;
        private float pulse;

        /// <summary>Set by a running world task so the arrows point at the next pickup or the site.</summary>
        public static Vector3? TaskTarget { get; set; }

        private void Start()
        {
            var art = MvpArtSet.Instance;
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Guide arrow " + i);
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = art != null ? art.arrow : null;
                renderer.sortingOrder = -1;
                renderer.enabled = false;
                arrows[i] = renderer;
            }
        }

        private Vector3? Target()
        {
            if (TaskTarget.HasValue) return TaskTarget;
            var director = StoryDirector.Active;
            if (director == null || director.IsModal) return null;
            if (director.NeedsTravel)
            {
                var point = FindAnyObjectByType<TravelPoint>();
                return point != null ? point.transform.position : null;
            }
            string wanted = director.TargetNpcId;
            if (string.IsNullOrEmpty(wanted)) return null;
            foreach (var npc in FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (npc.Id == wanted) return npc.transform.position;
            return null;
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                if (controller == null) return;
                player = controller.transform;
            }
            var target = Target();
            if (!target.HasValue)
            {
                foreach (var arrow in arrows) arrow.enabled = false;
                return;
            }
            var from = player.position;
            var direction = target.Value - from;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < HideDistance)
            {
                foreach (var arrow in arrows) arrow.enabled = false;
                return;
            }
            pulse += Time.deltaTime * 1.6f;
            direction.Normalize();
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            for (int i = 0; i < arrows.Length; i++)
            {
                float travel = Mathf.Repeat(pulse + i * (1f / arrows.Length), 1f);
                float reach = Mathf.Min(distance - 1.5f, 2f + travel * Spacing * arrows.Length);
                arrows[i].enabled = true;
                arrows[i].transform.position = from + direction * reach + Vector3.up * .06f;
                arrows[i].transform.rotation = Quaternion.Euler(90f, angle, 0f);
                arrows[i].color = new Color(1f, .86f, .48f, Mathf.Sin(travel * Mathf.PI) * .85f);
            }
        }
    }
}
