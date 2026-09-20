using System;
using ATTILA.Audio;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// «Бәйге»: a race around the aul. The player mounts up and has to take the marks in order while a rival
    /// rider keeps a steady pace; the yurts and rocks stay solid, so the shortest line is not the fastest one.
    /// </summary>
    public sealed class BaigeMinigame : MonoBehaviour
    {
        private const float RiderSpeed = 2.3f;
        private const float RivalSpeed = 6.6f;
        private const float MarkRadius = 3.4f;
        private const float Limit = 75f;

        private PlayerController player;
        private PlayerVisual visual;
        private Vector2[] marks;
        private Action won, lost;
        private GameObject mount, rival;
        private GameObject[] flags;
        private GameObject[] rings;
        private int playerMark, rivalMark;
        private float startedAt;
        private bool active;
        private Vector3 returnPosition;

        public void Begin(TaskDefinition course, PlayerController playerController, Action onWon, Action onLost)
        {
            player = playerController;
            visual = player.GetComponent<PlayerVisual>();
            marks = course.points;
            won = onWon;
            lost = onLost;
            playerMark = 0;
            rivalMark = 0;
            active = true;
            startedAt = Time.time;
            returnPosition = player.transform.position;

            var art = MvpArtSet.Instance;
            flags = new GameObject[marks.Length];
            rings = new GameObject[marks.Length];
            for (int i = 0; i < marks.Length; i++)
            {
                var position = new Vector3(marks[i].x, 0f, marks[i].y);
                flags[i] = WorldArt.Prop(art != null ? art.Banner("dark") : null, position, .5f);
                rings[i] = WorldArt.Decal(art != null ? art.glow : null, position + Vector3.up * .05f,
                    new Color(.95f, .84f, .42f, i == 0 ? .6f : .25f), 2.2f);
            }

            var start = new Vector3(course.goal.x, 1f, course.goal.y);
            Teleport(player.transform, start);
            mount = WorldArt.Prop(art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null, start, 2.2f);
            if (mount != null) mount.AddComponent<SpriteFlipbook>().Configure(art.horseBay, 6f);
            visual.SetLift(.85f);
            player.SetSpeedMultiplier(RiderSpeed);

            rival = new GameObject("Rival rider");
            rival.transform.position = start + new Vector3(2.6f, 0f, 0f);
            var rivalHorse = WorldArt.Prop(art != null && art.horseGrey.Length > 0 ? art.horseGrey[0] : null, rival.transform.position, 2.2f);
            if (rivalHorse != null)
            {
                rivalHorse.transform.SetParent(rival.transform, true);
                rivalHorse.AddComponent<SpriteFlipbook>().Configure(art.horseGrey, 6f);
            }
            var rivalVisual = new GameObject("Rival").AddComponent<PlayerVisual>();
            rivalVisual.transform.SetParent(rival.transform, false);
            rivalVisual.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            rivalVisual.SetSpriteSet(LookLibrary.Load("warrior"));

            var ui = StoryDirector.Active.Ui;
            ui.ShowMinigame("БӘЙГЕ", "Бес белгіні ретімен ал. Қарсыласыңнан бұрын мәреге жет.");
            ui.SetMinigameStatus($"Белгі: 0/{marks.Length}      Қарсылас: 0/{marks.Length}");
        }

        private static void Teleport(Transform target, Vector3 position)
        {
            var controller = target.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            target.position = position;
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
        }

        private void Update()
        {
            if (!active) return;
            var ui = StoryDirector.Active.Ui;
            float remaining = Mathf.Max(0f, Limit - (Time.time - startedAt));
            ui.SetMinigameTime(remaining);

            var position = player.transform.position;
            if (mount != null) mount.transform.position = new Vector3(position.x, 0f, position.z);

            if (playerMark < marks.Length && Flat(position, marks[playerMark]) < MarkRadius)
            {
                AudioService.Play(Sfx.Ring, .8f, 1.2f);
                if (rings[playerMark] != null) Destroy(rings[playerMark]);
                if (flags[playerMark] != null) Destroy(flags[playerMark]);
                playerMark++;
                if (playerMark < marks.Length && rings[playerMark] != null)
                    rings[playerMark].GetComponent<SpriteRenderer>().color = new Color(.95f, .84f, .42f, .6f);
                ui.SetMinigameStatus($"Белгі: {playerMark}/{marks.Length}      Қарсылас: {rivalMark}/{marks.Length}");
                if (playerMark >= marks.Length) { Finish(true); return; }
            }

            MoveRival();
            if (remaining <= 0f) Finish(false);
        }

        private void MoveRival()
        {
            if (rival == null || rivalMark >= marks.Length) return;
            var target = new Vector3(marks[rivalMark].x, 0f, marks[rivalMark].y);
            var step = target - rival.transform.position;
            step.y = 0f;
            // A touch of rubber banding keeps the race close without letting the rival cheat past the player.
            float speed = RivalSpeed * (rivalMark > playerMark ? .92f : 1.06f);
            rival.transform.position += step.normalized * speed * Time.deltaTime;
            var rivalVisual = rival.GetComponentInChildren<PlayerVisual>();
            if (rivalVisual != null) rivalVisual.SetMovement(step, true);
            if (step.magnitude < 2f)
            {
                rivalMark++;
                StoryDirector.Active.Ui.SetMinigameStatus($"Белгі: {playerMark}/{marks.Length}      Қарсылас: {rivalMark}/{marks.Length}");
                if (rivalMark >= marks.Length) Finish(false);
            }
        }

        private static float Flat(Vector3 a, Vector2 b) => Vector2.Distance(new Vector2(a.x, a.z), b);

        private void Finish(bool success)
        {
            active = false;
            var ui = StoryDirector.Active.Ui;
            ui.HideMinigame();
            visual.SetLift(0f);
            player.SetSpeedMultiplier(1f);
            if (mount != null) Destroy(mount);
            if (rival != null) Destroy(rival);
            foreach (var flag in flags) if (flag != null) Destroy(flag);
            foreach (var ring in rings) if (ring != null) Destroy(ring);
            Teleport(player.transform, returnPosition);
            if (success) won?.Invoke(); else lost?.Invoke();
        }
    }
}
