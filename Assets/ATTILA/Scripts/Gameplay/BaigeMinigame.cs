using System;
using ATTILA.Audio;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.UI;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// «Бәйге»: a race around the aul. The player mounts up and has to take the marks in order while a rival
    /// rider keeps a steady pace; the yurts and rocks stay solid, so the shortest line is not the fastest one.
    /// The horses gallop with a bob and kick up dust, arrows lead to the next mark, and the place is always shown.
    /// </summary>
    public sealed class BaigeMinigame : MonoBehaviour
    {
        private const float RiderSpeed = 2.3f;
        private const float RivalSpeed = 6.6f;
        private const float MarkRadius = 3.4f;
        private const float Limit = 75f;
        private static readonly Color DustColor = new(.78f, .66f, .46f, .8f);

        private PlayerController player;
        private PlayerVisual visual;
        private Vector2[] marks;
        private Action won, lost;
        private GameObject mount, rival;
        private SpriteRenderer mountRenderer, rivalRenderer;
        private Sprite standing;
        private Sprite[] gallop, rivalGallop;
        private const float GallopFps = 12f;
        private PlayerVisual rivalVisual;
        private GameObject[] flags;
        private GameObject[] rings;
        private int playerMark, rivalMark;
        private float startedAt, nextDust, time;
        private bool active, racing;
        private Vector3 returnPosition, lastPosition;

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
            racing = false;
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
            lastPosition = start;
            mount = WorldArt.Prop(art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null, start, 2.2f);
            if (mount != null) mountRenderer = mount.GetComponent<SpriteRenderer>();
            standing = art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null;
            gallop = art != null && art.horseBayGallop != null && art.horseBayGallop.Length > 0 ? art.horseBayGallop : art?.horseBay;
            rivalGallop = art != null && art.horseGreyGallop != null && art.horseGreyGallop.Length > 0 ? art.horseGreyGallop : art?.horseGrey;
            visual.SetLift(.85f);
            player.SetSpeedMultiplier(RiderSpeed);
            player.SetInputEnabled(false);

            rival = new GameObject("Rival rider");
            rival.transform.position = start + new Vector3(2.6f, 0f, 0f);
            var rivalHorse = WorldArt.Prop(art != null && art.horseGrey.Length > 0 ? art.horseGrey[0] : null, rival.transform.position, 2.2f);
            if (rivalHorse != null)
            {
                rivalHorse.transform.SetParent(rival.transform, true);
                rivalRenderer = rivalHorse.GetComponent<SpriteRenderer>();
            }
            rivalVisual = new GameObject("Rival").AddComponent<PlayerVisual>();
            rivalVisual.transform.SetParent(rival.transform, false);
            rivalVisual.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            rivalVisual.SetSpriteSet(LookLibrary.Load("warrior"));

            var ui = StoryDirector.Active.Ui;
            ui.ShowMinigame("БӘЙГЕ", "Бес белгіні ретімен ал. Қарсыласыңнан бұрын мәреге жет.");
            Status();
            ui.SetMinigameTime(Limit);
            MinigameFx.Countdown(ui.OverlayRoot, () =>
            {
                if (!active) return;
                racing = true;
                startedAt = Time.time;
                player.SetInputEnabled(true);
                MinigameFx.ShakeCamera(.15f, .3f);
            });
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
            time += Time.deltaTime;
            var ui = StoryDirector.Active.Ui;
            var position = player.transform.position;
            var moved = position - lastPosition;
            moved.y = 0f;
            bool galloping = moved.magnitude > .01f;
            lastPosition = position;

            // The horse rides under the player with a canter bob and faces where it is going.
            float bob = galloping ? Mathf.Abs(Mathf.Sin(time * 14f)) * .18f : 0f;
            if (mount != null) mount.transform.position = new Vector3(position.x, bob, position.z);
            if (mountRenderer != null && Mathf.Abs(moved.x) > .005f) mountRenderer.flipX = moved.x < 0f;
            if (mountRenderer != null && gallop != null && gallop.Length > 0)
                mountRenderer.sprite = galloping ? gallop[Mathf.FloorToInt(time * GallopFps) % gallop.Length] : standing ?? gallop[0];
            if (rivalRenderer != null && rivalGallop != null && rivalGallop.Length > 0)
                rivalRenderer.sprite = rivalGallop[racing ? Mathf.FloorToInt(time * GallopFps + 2) % rivalGallop.Length : 0];
            visual.SetLift(.85f + bob);
            AnimateRings();

            if (!racing)
            {
                GuideArrows.TaskTarget = null;
                return;
            }
            float remaining = Mathf.Max(0f, Limit - (Time.time - startedAt));
            ui.SetMinigameTime(remaining);
            if (Time.time > nextDust)
            {
                nextDust = Time.time + .08f;
                if (galloping) MinigameFx.Dust(new Vector3(position.x, 0f, position.z) - moved.normalized * .8f, DustColor, 2);
                if (rival != null) MinigameFx.Dust(rival.transform.position, DustColor, 1);
            }
            GuideArrows.TaskTarget = playerMark < marks.Length ? new Vector3(marks[playerMark].x, 0f, marks[playerMark].y) : null;

            if (playerMark < marks.Length && Flat(position, marks[playerMark]) < MarkRadius)
            {
                TakeMark(ui);
                if (playerMark >= marks.Length) { Finish(true); return; }
            }

            MoveRival();
            if (active && remaining <= 0f) Finish(false);
        }

        private void TakeMark(GameUi ui)
        {
            var at = new Vector3(marks[playerMark].x, 1.5f, marks[playerMark].y);
            AudioService.Play(Sfx.Ring, .9f, 1.1f + playerMark * .08f);
            MinigameFx.ShakeCamera(.12f, .2f);
            var overlay = (RectTransform)ui.OverlayRoot;
            var screen = MinigameFx.WorldToOverlay(overlay, at);
            MinigameFx.Burst(overlay, screen, MenuUiKit.Gold, 16, 520f);
            MinigameFx.Popup(overlay, screen + new Vector2(0, 60), $"{playerMark + 1}/{marks.Length}", MenuUiKit.Gold, 52);
            if (rings[playerMark] != null) Destroy(rings[playerMark]);
            if (flags[playerMark] != null) Destroy(flags[playerMark]);
            playerMark++;
            Status();
        }

        /// <summary>The next mark's ring breathes so it reads from across the aul; later ones stay faint.</summary>
        private void AnimateRings()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                bool next = i == playerMark;
                float pulse = next ? 2.2f + Mathf.Sin(time * 5f) * .35f : 2.2f;
                rings[i].transform.localScale = Vector3.one * pulse;
                rings[i].GetComponent<SpriteRenderer>().color = new Color(.95f, .84f, .42f, next ? .75f : .22f);
            }
        }

        private void Status()
        {
            bool ahead = playerMark > rivalMark || (playerMark == rivalMark && playerMark < marks.Length && rival != null
                         && Flat(player.transform.position, marks[playerMark]) <= Flat(rival.transform.position, marks[playerMark]));
            StoryDirector.Active.Ui.SetMinigameStatus($"{(ahead ? "1-ШІ" : "2-ШІ")} ОРЫН   ·   Белгі: {playerMark}/{marks.Length}");
        }

        private void MoveRival()
        {
            if (rival == null || rivalMark >= marks.Length) return;
            var target = new Vector3(marks[rivalMark].x, 0f, marks[rivalMark].y);
            var step = target - rival.transform.position;
            step.y = 0f;
            // A touch of rubber banding keeps the race close without letting the rival cheat past the player.
            float speed = RivalSpeed * (rivalMark > playerMark ? .92f : 1.06f);
            var flat = rival.transform.position + step.normalized * speed * Time.deltaTime;
            rival.transform.position = new Vector3(flat.x, Mathf.Abs(Mathf.Sin(time * 14f + 1f)) * .18f, flat.z);
            if (rivalRenderer != null && Mathf.Abs(step.x) > .05f) rivalRenderer.flipX = step.x < 0f;
            if (rivalVisual != null) rivalVisual.SetMovement(step, true);
            if (Mathf.FloorToInt(time * 4f) % 2 == 0) Status();
            if (step.magnitude < 2f)
            {
                rivalMark++;
                Status();
                if (rivalMark >= marks.Length) Finish(false);
            }
        }

        private static float Flat(Vector3 a, Vector2 b) => Vector2.Distance(new Vector2(a.x, a.z), b);

        private void Finish(bool success)
        {
            active = false;
            racing = false;
            GuideArrows.TaskTarget = null;
            player.SetInputEnabled(false);
            var ui = StoryDirector.Active.Ui;
            MinigameFx.ShakeCamera(.3f, .45f);
            MinigameFx.Result(ui.OverlayRoot, success, success ? "Мәреге бірінші жеттің!" : "Қарсылас озып кетті.", () =>
            {
                ui.HideMinigame();
                visual.SetLift(0f);
                player.SetSpeedMultiplier(1f);
                player.SetInputEnabled(true);
                if (mount != null) Destroy(mount);
                if (rival != null) Destroy(rival);
                foreach (var flag in flags) if (flag != null) Destroy(flag);
                foreach (var ring in rings) if (ring != null) Destroy(ring);
                Teleport(player.transform, returnPosition);
                if (success) won?.Invoke(); else lost?.Invoke();
            });
        }
    }
}
