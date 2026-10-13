using ATTILA.Environment;
using ATTILA.Gameplay;
using ATTILA.NPC;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.UI
{
    /// <summary>
    /// Lays a short trail of arrows on the ground in front of the player, pointing at whatever the current beat
    /// wants: the NPC, the travel cart or the next task point. The arrows stand still on the ground and a soft
    /// wave of light runs along them toward the goal; they turn smoothly and fade out once the target is close.
    /// </summary>
    public sealed class GuideArrows : MonoBehaviour
    {
        private const int Count = 4;
        private const float FirstGap = 1.6f;      // from the player's feet to the first arrow
        private const float Spacing = 1.3f;
        private const float HideDistance = 3.5f;
        private const float WaveSeconds = 1.4f;   // one sweep of light along the whole trail
        private const float GroundHeight = .1f;     // just above the path ribbons, so roads never hide the arrows

        private readonly SpriteRenderer[] arrows = new SpriteRenderer[Count];
        private Transform player;
        private float pulse;
        private Vector3 heading;
        private float visibility;

        /// <summary>Set by a running world task so the arrows point at the next pickup or the site.</summary>
        public static Vector3? TaskTarget { get; set; }

        private void Start()
        {
            // A target left by a job in a scene that has since been reloaded must not steer the new one.
            TaskTarget = null;
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
            var from = new Vector3(player.position.x, GroundHeight, player.position.z);
            float distance = 0f;
            if (target.HasValue)
            {
                var direction = new Vector3(target.Value.x, GroundHeight, target.Value.z) - from;
                distance = direction.magnitude;
                if (distance > .01f)
                {
                    direction /= distance;
                    // Turn toward a new direction over a moment instead of snapping, so the trail never flickers.
                    heading = heading.sqrMagnitude < .01f ? direction : Vector3.Slerp(heading, direction, Time.deltaTime * 6f).normalized;
                }
            }
            bool show = target.HasValue && distance > HideDistance;
            visibility = Mathf.MoveTowards(visibility, show ? 1f : 0f, Time.deltaTime * 3f);
            if (visibility <= 0f || heading.sqrMagnitude < .01f)
            {
                foreach (var arrow in arrows) arrow.enabled = false;
                return;
            }
            pulse = Mathf.Repeat(pulse + Time.deltaTime / WaveSeconds, 1f);
            float angle = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            for (int i = 0; i < arrows.Length; i++)
            {
                float reach = FirstGap + i * Spacing;
                // Arrows past the goal are not drawn; the trail shortens as the player closes in.
                bool inside = !target.HasValue || reach < distance - 1.2f;
                arrows[i].enabled = inside;
                if (!inside) continue;
                float place = (i + .5f) / arrows.Length;
                float wave = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(pulse - place + .5f, 1f) - .5f) * 3f);
                arrows[i].transform.position = from + heading * reach;
                arrows[i].transform.rotation = Quaternion.Euler(90f, angle, 0f);
                arrows[i].transform.localScale = Vector3.one * (1f + wave * .18f);
                arrows[i].color = new Color(1f, .86f, .48f, (.35f + wave * .6f) * visibility);
            }
        }
    }
}
