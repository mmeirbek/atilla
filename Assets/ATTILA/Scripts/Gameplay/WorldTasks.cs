using System;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.UI;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// Jobs the player does by walking around the aul instead of talking: gather firewood, search by a hint,
    /// lead a horse to the hitching post and raise a yurt part by part. Every task drives the mini-game banner.
    /// </summary>
    public abstract class WorldTask : MonoBehaviour
    {
        protected TaskDefinition Task;
        protected PlayerController Player;
        private Action done;
        private readonly List<GameObject> spawned = new();

        public void Begin(TaskDefinition task, PlayerController player, Action complete)
        {
            Task = task;
            Player = player;
            done = complete;
            var ui = StoryDirector.Active.Ui;
            ui.ShowMinigame(task.title, task.hint);
            Setup();
            Report();
        }

        protected abstract void Setup();
        protected abstract void Tick(Vector3 playerPosition);
        protected abstract string Status();

        protected Sprite PropSprite(int index)
        {
            var art = MvpArtSet.Instance;
            if (art == null || Task.props == null || Task.props.Length == 0) return null;
            return art.Prop(Task.props[Mathf.Min(index, Task.props.Length - 1)]);
        }

        protected GameObject Spawn(Sprite sprite, Vector2 point, float shadow = .7f)
        {
            var prop = WorldArt.Prop(sprite, new Vector3(point.x, 0f, point.y), shadow);
            if (prop != null) spawned.Add(prop);
            return prop;
        }

        protected void Report() => StoryDirector.Active.Ui.SetMinigameStatus(Status());

        /// <summary>A soft light on the ground under something the player should walk to; it breathes so it reads in grass.</summary>
        protected GameObject Beacon(Vector3 position, float size = 1.6f)
        {
            var art = MvpArtSet.Instance;
            var beacon = WorldArt.Decal(art != null ? art.glow : null, new Vector3(position.x, .08f, position.z), new Color(1f, .86f, .45f, .6f), size);
            if (beacon != null) { beacon.AddComponent<Breathe>(); spawned.Add(beacon); }
            return beacon;
        }

        private float nextNudge;

        /// <summary>A short line over the player, at most once a second, for "not yet" moments.</summary>
        protected void Nudge(string line)
        {
            if (Time.time < nextNudge) return;
            nextNudge = Time.time + 1.2f;
            AudioService.Play(Sfx.Fail, .4f, 1.2f);
            var overlay = (RectTransform)StoryDirector.Active.Ui.OverlayRoot;
            UI.MinigameFx.Popup(overlay, UI.MinigameFx.WorldToOverlay(overlay, Player.transform.position + Vector3.up * 2.4f), line, UI.MenuUiKit.Cream, 36);
        }

        /// <summary>Sparks, a rising line and a small jolt where something was picked up or set down.</summary>
        protected static void Celebrate(Vector3 at, string line, bool big = false)
        {
            var overlay = (RectTransform)StoryDirector.Active.Ui.OverlayRoot;
            var screen = UI.MinigameFx.WorldToOverlay(overlay, at + Vector3.up * 1.2f);
            UI.MinigameFx.Burst(overlay, screen, UI.MenuUiKit.Gold, big ? 26 : 12, big ? 700f : 420f);
            UI.MinigameFx.Popup(overlay, screen + new Vector2(0, 60), line, UI.MenuUiKit.Gold, big ? 56 : 44);
            UI.MinigameFx.ShakeCamera(big ? .25f : .1f, big ? .35f : .15f);
        }

        protected static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>Points the ground arrows at the next thing this task wants.</summary>
        protected void Aim(Vector3? target) => UI.GuideArrows.TaskTarget = target;

        protected void Finish()
        {
            UI.GuideArrows.TaskTarget = null;
            var ui = StoryDirector.Active.Ui;
            ui.HideMinigame();
            AudioService.Play(Sfx.Reward, .8f);
            foreach (var item in spawned) if (item != null) Destroy(item);
            spawned.Clear();
            var callback = done;
            done = null;
            Cleanup();
            callback?.Invoke();
            Destroy(this);
        }

        protected virtual void Cleanup() { }

        private void Update()
        {
            if (done == null || Player == null) return;
            Tick(Player.transform.position);
        }
    }

    /// <summary>Gather scattered items: walk over each one.</summary>
    public sealed class CollectTask : WorldTask
    {
        private readonly List<GameObject> items = new();
        private int taken;

        protected override void Setup()
        {
            for (int i = 0; i < Task.points.Length; i++)
            {
                var item = Spawn(PropSprite(i), Task.points[i]);
                if (item == null) continue;
                items.Add(item);
                beacons.Add(item, Beacon(item.transform.position));
            }
        }

        private readonly Dictionary<GameObject, GameObject> beacons = new();

        protected override string Status() => $"{Task.itemLabel}: {taken}/{Mathf.Max(1, Task.required)}";

        protected override void Tick(Vector3 playerPosition)
        {
            Vector3? closest = null;
            float best = float.MaxValue;
            foreach (var item in items)
            {
                if (item == null) continue;
                float distance = Flat(item.transform.position, playerPosition);
                if (distance < best) { best = distance; closest = item.transform.position; }
            }
            Aim(closest);
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null) { items.RemoveAt(i); continue; }
                if (Flat(items[i].transform.position, playerPosition) > 1.9f) continue;
                Celebrate(items[i].transform.position, $"+1  {Mathf.Min(taken + 1, Task.required)}/{Task.required}");
                if (beacons.TryGetValue(items[i], out var beacon) && beacon != null) Destroy(beacon);
                Destroy(items[i]);
                items.RemoveAt(i);
                taken++;
                AudioService.Play(Sfx.Blip, .7f, 1.1f);
                Report();
                if (taken >= Task.required) { Finish(); return; }
            }
        }
    }

    /// <summary>Search by a hint: the banner says warm or cold until the thing is in sight.</summary>
    public sealed class FindTask : WorldTask
    {
        private GameObject target;
        private SpriteRenderer targetRenderer;
        private float distance = 99f;

        protected override void Setup()
        {
            target = Spawn(PropSprite(0), Task.points.Length > 0 ? Task.points[0] : Task.goal);
            if (target == null) return;
            targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
            targetRenderer.enabled = false;
        }

        protected override string Status() =>
            distance < 6f ? "КӨРІНДІ!" : distance < 14f ? "ЖАҚЫНДАДЫҢ" : "АЛЫС";

        protected override void Tick(Vector3 playerPosition)
        {
            if (target == null) { Finish(); return; }
            // The arrows always lead; the banner still says how warm the search is.
            Aim(target.transform.position);
            float previous = distance;
            distance = Flat(target.transform.position, playerPosition);
            if (targetRenderer != null) targetRenderer.enabled = distance < 6f;
            if (Mathf.Abs(previous - distance) > .4f) Report();
            if (distance < 2.2f)
            {
                StoryDirector.Active.Ui.ShowToast("ТАБЫЛДЫ: " + Task.itemLabel);
                Celebrate(target.transform.position, "ТАБЫЛДЫ!", true);
                Finish();
            }
        }
    }

    /// <summary>Lead a horse to the hitching post: it follows when you are close and stops when you stop.</summary>
    public sealed class EscortTask : WorldTask
    {
        private const float LeadReach = 10f;
        private GameObject horse;
        private GameObject postProp;
        private GameObject ring;

        protected override void Setup()
        {
            var art = MvpArtSet.Instance;
            var start = Task.points.Length > 0 ? Task.points[0] : Vector2.zero;
            horse = WorldArt.Prop(art != null && art.horseBay.Length > 0 ? art.horseBay[0] : null, new Vector3(start.x, 0f, start.y), 2.2f);
            if (horse != null) horse.AddComponent<SpriteFlipbook>().Configure(art != null ? art.horseBay : null, .8f);
            postProp = Spawn(art != null ? art.post : null, Task.goal, .6f);
            ring = WorldArt.Decal(art != null ? art.glow : null, new Vector3(Task.goal.x, .05f, Task.goal.y), new Color(.95f, .82f, .45f, .5f), 1.4f);
        }

        protected override string Status()
        {
            if (horse == null || Player == null) return string.Empty;
            if (Flat(horse.transform.position, Player.transform.position) >= LeadReach) return "Атқа қайта бар!";
            float toGoal = Flat(horse.transform.position, new Vector3(Task.goal.x, 0f, Task.goal.y));
            return $"Бағанаға дейін: {Mathf.CeilToInt(toGoal)} қадам";
        }

        protected override void Tick(Vector3 playerPosition)
        {
            if (horse == null) { Finish(); return; }
            var position = horse.transform.position;
            float toPlayer = Flat(position, playerPosition);
            // Left behind, the horse stops; the arrows then lead back to it instead of to the post.
            bool lost = toPlayer >= LeadReach;
            Aim(lost ? position : new Vector3(Task.goal.x, 0f, Task.goal.y));
            if (lost && Flat(playerPosition, new Vector3(Task.goal.x, 0f, Task.goal.y)) < 6f) Nudge("АТ АРТТА ҚАЛДЫ");
            if (toPlayer < LeadReach && toPlayer > 2.4f)
            {
                var step = (playerPosition - position);
                step.y = 0f;
                position += step.normalized * 3.4f * Time.deltaTime;
                horse.transform.position = position;
                var renderer = horse.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) renderer.flipX = step.x < 0f;
            }
            Report();
            // The horse halts 2.4 from the player, so standing at the post must still count: 3.6 leaves room.
            if (Flat(position, new Vector3(Task.goal.x, 0f, Task.goal.y)) < 3.6f)
            {
                StoryDirector.Active.Ui.ShowToast("АТ БАЙЛАНДЫ");
                Celebrate(new Vector3(Task.goal.x, 1f, Task.goal.y), "АТ БАЙЛАНДЫ!", true);
                Finish();
            }
        }

        protected override void Cleanup()
        {
            if (ring != null) Destroy(ring);
            if (horse != null) Destroy(horse);
        }
    }

    /// <summary>Raise a yurt: carry кереге, уық, шаңырақ and киіз to the site, one at a time, in that order.</summary>
    public sealed class BuildTask : WorldTask
    {
        // Once the lattice stands its collider keeps the player about 3.2 from the centre, so a part counts
        // as delivered anywhere within this ring around the site.
        private const float DeliverRadius = 4.8f;
        private const float PickupRadius = 1.9f;

        private readonly List<GameObject> parts = new();
        private YurtSite site;
        private GameObject carried;
        private int carriedIndex = -1;
        private int placed;
        private GameObject partBeacon, siteBeacon;
        private float time;

        protected override void Setup()
        {
            site = YurtSite.Create(new Vector3(Task.goal.x, 0f, Task.goal.y), "red", 0);
            for (int i = 0; i < Task.points.Length; i++)
            {
                var part = Spawn(PropSprite(i), Task.points[i], .8f);
                parts.Add(part);
            }
            siteBeacon = Beacon(new Vector3(Task.goal.x, 0f, Task.goal.y), 4.2f);
            MarkNext();
        }

        /// <summary>Light under the part that goes on next; the site glows while a part is being carried.</summary>
        private void MarkNext()
        {
            if (partBeacon != null) Destroy(partBeacon);
            partBeacon = carried == null && placed < parts.Count && parts[placed] != null ? Beacon(parts[placed].transform.position, 1.8f) : null;
            if (siteBeacon != null) siteBeacon.SetActive(carried != null);
        }

        protected override string Status()
        {
            string part = carriedIndex >= 0 && Task.props != null && carriedIndex < Task.props.Length ? PartName(carriedIndex) : "—";
            return $"Қолыңда: {part}      Тұрғызылды: {placed}/{parts.Count}";
        }

        private string PartName(int index) => index switch
        {
            0 => "Кереге",
            1 => "Уық",
            2 => "Шаңырақ",
            _ => "Киіз"
        };

        protected override void Tick(Vector3 playerPosition)
        {
            time += Time.deltaTime;
            Aim(carried != null ? new Vector3(Task.goal.x, 0f, Task.goal.y)
                : placed < parts.Count && parts[placed] != null ? parts[placed].transform.position : (Vector3?)null);
            if (carried == null)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i] == null) continue;
                    if (Flat(parts[i].transform.position, playerPosition) > PickupRadius) continue;
                    if (i != placed)
                    {
                        // The order is the lesson: say which part goes up first instead of silently ignoring this one.
                        Nudge("АЛДЫМЕН: " + PartName(placed).ToUpperInvariant());
                        continue;
                    }
                    carried = parts[i];
                    carriedIndex = i;
                    AudioService.Play(Sfx.Blip, .7f, .9f);
                    Celebrate(playerPosition, PartName(i).ToUpperInvariant());
                    MarkNext();
                    Report();
                    break;
                }
            }
            else
            {
                carried.transform.position = playerPosition + new Vector3(0f, 1.7f + Mathf.Abs(Mathf.Sin(time * 8f)) * .12f, 0f);
                if (Flat(playerPosition, new Vector3(Task.goal.x, 0f, Task.goal.y)) < DeliverRadius)
                {
                    Destroy(carried);
                    parts[carriedIndex] = null;
                    carried = null;
                    carriedIndex = -1;
                    placed++;
                    site.SetStage(placed);
                    AudioService.Play(Sfx.Hit, .6f, 1.2f);
                    Celebrate(new Vector3(Task.goal.x, 1f, Task.goal.y), placed >= parts.Count ? "КИІЗ ҮЙ ДАЙЫН!" : $"{placed}/{parts.Count}", placed >= parts.Count);
                    StoryDirector.Active.Ui.ShowToast(placed >= parts.Count ? "КИІЗ ҮЙ ТІГІЛДІ" : "БӨЛІК ОРНЫНА ҚОЙЫЛДЫ", 2f);
                    Report();
                    if (placed >= parts.Count) { Finish(); return; }
                    MarkNext();
                }
            }
        }
    }

    /// <summary>Slow pulse of a ground light, so a marker in the grass catches the eye without flashing.</summary>
    public sealed class Breathe : MonoBehaviour
    {
        private Vector3 baseScale;
        private float seed;

        private void Start()
        {
            baseScale = transform.localScale;
            seed = UnityEngine.Random.value * 6f;
        }

        private void Update() => transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * 3f + seed) * .12f);
    }
}
