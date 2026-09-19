using ATTILA.Data;
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
        public string Id => npcId;
        public string DisplayName => definition != null ? definition.displayName : npcId;
        public float InteractionRadius => definition != null ? definition.interactionRadius : 2f;
        public void Configure(NpcDefinition value) { definition = value; npcId = value.id; }
        public bool IsInRange(Transform player) => Vector3.Distance(new Vector3(player.position.x, 0f, player.position.z), new Vector3(transform.position.x, 0f, transform.position.z)) <= InteractionRadius;
        public void Interact() => Stage2Game.Active?.InteractWith(this);
    }

    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        private NpcInteractable nearest;
        private Stage2Ui ui;
        private void Start() => ui = Stage2Game.Active?.Ui;
        private void Update()
        {
            if (Stage2Game.Active == null || Stage2Game.Active.IsModal) return;
            nearest = FindNearest();
            ui ??= Stage2Game.Active.Ui;
            ui?.SetInteractionPrompt(nearest == null ? string.Empty : "[E] Сөйлесу");
            if (nearest != null && Input.GetKeyDown(KeyCode.E)) nearest.Interact();
        }
        private NpcInteractable FindNearest()
        {
            NpcInteractable best = null;
            float bestDistance = float.MaxValue;
            foreach (var npc in FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude))
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                if (distance <= npc.InteractionRadius && distance < bestDistance) { best = npc; bestDistance = distance; }
            }
            return best;
        }
    }
}
