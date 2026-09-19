using System;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.Gameplay
{
    public sealed class SokyrTekeMinigame : MonoBehaviour
    {
        private const float Duration = 40f;
        private PlayerController player;
        private GameObject target;
        private float startedAt;
        private bool active;
        private Action won;
        private Action lost;
        public void Begin(PlayerController playerController, Action onWon, Action onLost)
        {
            Cleanup();
            player = playerController; won = onWon; lost = onLost; active = true; startedAt = Time.time;
            player.transform.position = new Vector3(0f, 1f, -12f);
            target = new GameObject("Sokyr Teke Target - Placeholder");
            target.transform.position = new Vector3(0f, 1f, -7f);
            target.AddComponent<PlayerVisual>();
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Sokyr Teke area marker"; marker.transform.position = new Vector3(0f, .03f, -12f); marker.transform.localScale = new Vector3(9f, .05f, 9f);
            marker.GetComponent<Renderer>().material.color = new Color(.91f, .44f, .32f, .28f);
            Destroy(marker.GetComponent<Collider>()); marker.transform.SetParent(transform);
            Stage2Game.Active.Ui.ShowMinigame("Соқыр теке", "Уақытша ереже: нысанаға жақындаңыз.", Duration);
        }
        private void Update()
        {
            if (!active) return;
            float remaining = Mathf.Max(0f, Duration - (Time.time - startedAt));
            Stage2Game.Active.Ui.SetMinigameTime(remaining);
            float t = Time.time - startedAt;
            target.transform.position = new Vector3(Mathf.Sin(t * 1.7f) * 3.2f, 1f, -12f + Mathf.Cos(t * 1.1f) * 3.2f);
            if (Vector3.Distance(player.transform.position, target.transform.position) < 1.25f) Finish(true);
            else if (remaining <= 0f) Finish(false);
        }
        private void Finish(bool success)
        {
            active = false; Stage2Game.Active.Ui.HideMinigame(); Cleanup();
            if (success) won?.Invoke(); else lost?.Invoke();
        }
        private void Cleanup()
        {
            if (target != null) Destroy(target);
            foreach (Transform child in transform) Destroy(child.gameObject);
            target = null;
        }
    }
}
