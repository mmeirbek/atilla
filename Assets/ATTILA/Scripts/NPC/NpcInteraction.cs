using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Gameplay;
using ATTILA.Player;
using ATTILA.UI;
using UnityEngine;

namespace ATTILA.NPC
{
    public sealed class NpcInteractable : MonoBehaviour
    {
        [SerializeField] private string npcId;
        private NpcDefinition definition;
        private PlayerVisual visual;
        public string Id => npcId;
        public string DisplayName => definition != null ? definition.displayName : npcId;
        public float InteractionRadius => definition != null ? definition.interactionRadius : 2f;

        public void Configure(NpcDefinition value)
        {
            definition = value;
            npcId = value.id;
            visual = GetComponent<PlayerVisual>();
        }

        public bool IsInRange(Transform player) =>
            Vector3.Distance(new Vector3(player.position.x, 0f, player.position.z), new Vector3(transform.position.x, 0f, transform.position.z)) <= InteractionRadius;

        /// <summary>Turns towards the player when they come close, so conversations are face to face.</summary>
        public void LookAt(Transform player)
        {
            if (visual == null) return;
            var direction = player.position - transform.position;
            direction.y = 0f;
            visual.Face(direction);
        }

        public void Interact() => StoryDirector.Active?.InteractWith(this);
    }

    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        private const float LookRadius = 5f;
        private NpcInteractable nearest;
        private GameUi ui;

        private void Update()
        {
            var director = StoryDirector.Active;
            if (director == null) return;
            ui ??= director.Ui;
            nearest = null;
            float bestDistance = float.MaxValue;
            foreach (var npc in FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                if (distance <= LookRadius) npc.LookAt(transform);
                if (distance <= npc.InteractionRadius && distance < bestDistance) { nearest = npc; bestDistance = distance; }
            }
            if (director.IsModal) { ui?.SetPrompt(string.Empty); return; }
            // The key press that closed a dialogue must not open the next one in the same breath.
            if (ui != null && Time.unscaledTime - ui.ClosedAt < .25f) { ui.SetPrompt(string.Empty); return; }
            var travel = director.NeedsTravel ? NearestTravelPoint() : null;
            if (travel != null)
            {
                ui?.SetPrompt("E  ·  ЖОЛҒА ШЫҒУ");
                if (Input.GetKeyDown(KeyCode.E)) director.OpenTravel();
                return;
            }
            ui?.SetPrompt(nearest == null ? string.Empty : $"E  ·  {nearest.DisplayName}");
            if (nearest != null && Input.GetKeyDown(KeyCode.E)) nearest.Interact();
        }

        private TravelPoint NearestTravelPoint()
        {
            foreach (var point in FindObjectsByType<TravelPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (Vector3.Distance(transform.position, point.transform.position) <= TravelPoint.Radius) return point;
            return null;
        }
    }

    /// <summary>Floating marker over the NPC the current beat is waiting for.</summary>
    public sealed class QuestMarker : MonoBehaviour
    {
        private SpriteRenderer marker;
        private Transform target;
        private string targetId;

        private void Start()
        {
            var art = MvpArtSet.Instance;
            var go = new GameObject("Quest marker");
            go.transform.SetParent(transform, false);
            marker = go.AddComponent<SpriteRenderer>();
            marker.sprite = art != null ? art.marker : null;
            marker.sortingOrder = 5;
            marker.enabled = false;
        }

        private void LateUpdate()
        {
            var director = StoryDirector.Active;
            if (director != null && director.NeedsTravel)
            {
                var point = FindAnyObjectByType<TravelPoint>();
                targetId = null;
                marker.enabled = point != null && !director.IsModal;
                if (point != null) Hover(point.transform);
                return;
            }
            string wanted = director != null ? director.TargetNpcId : null;
            if (string.IsNullOrEmpty(wanted)) { marker.enabled = false; targetId = null; return; }
            if (wanted != targetId || target == null)
            {
                targetId = wanted;
                target = null;
                foreach (var npc in FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (npc.Id == wanted) target = npc.transform;
            }
            if (target == null) { marker.enabled = false; return; }
            marker.enabled = !director.IsModal;
            Hover(target);
        }

        private void Hover(Transform above)
        {
            marker.transform.position = above.position + new Vector3(0f, 2.5f + Mathf.Sin(Time.time * 3f) * .12f, 0f);
            var camera = Camera.main;
            if (camera != null) marker.transform.rotation = camera.transform.rotation;
        }
    }
}
