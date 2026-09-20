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

        protected static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        protected void Finish()
        {
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
                if (item != null) items.Add(item);
            }
        }

        protected override string Status() => $"{Task.itemLabel}: {taken}/{Mathf.Max(1, Task.required)}";

        protected override void Tick(Vector3 playerPosition)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] == null) { items.RemoveAt(i); continue; }
                if (Flat(items[i].transform.position, playerPosition) > 1.5f) continue;
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
            float previous = distance;
            distance = Flat(target.transform.position, playerPosition);
            if (targetRenderer != null) targetRenderer.enabled = distance < 6f;
            if (Mathf.Abs(previous - distance) > .4f) Report();
            if (distance < 1.8f)
            {
                StoryDirector.Active.Ui.ShowToast("ТАБЫЛДЫ: " + Task.itemLabel);
                Finish();
            }
        }
    }

    /// <summary>Lead a horse to the hitching post: it follows when you are close and stops when you stop.</summary>
    public sealed class EscortTask : WorldTask
    {
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
            if (horse == null) return string.Empty;
            float toGoal = Flat(horse.transform.position, new Vector3(Task.goal.x, 0f, Task.goal.y));
            return $"Бағанаға дейін: {Mathf.CeilToInt(toGoal)} қадам";
        }

        protected override void Tick(Vector3 playerPosition)
        {
            if (horse == null) { Finish(); return; }
            var position = horse.transform.position;
            float toPlayer = Flat(position, playerPosition);
            if (toPlayer < 9f && toPlayer > 2.4f)
            {
                var step = (playerPosition - position);
                step.y = 0f;
                position += step.normalized * 3.4f * Time.deltaTime;
                horse.transform.position = position;
                var renderer = horse.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) renderer.flipX = step.x < 0f;
            }
            Report();
            if (Flat(position, new Vector3(Task.goal.x, 0f, Task.goal.y)) < 2.4f)
            {
                StoryDirector.Active.Ui.ShowToast("АТ БАЙЛАНДЫ");
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
        private readonly List<GameObject> parts = new();
        private YurtSite site;
        private GameObject carried;
        private int carriedIndex = -1;
        private int placed;

        protected override void Setup()
        {
            site = YurtSite.Create(new Vector3(Task.goal.x, 0f, Task.goal.y), "red", 0);
            for (int i = 0; i < Task.points.Length; i++)
            {
                var part = Spawn(PropSprite(i), Task.points[i], .8f);
                parts.Add(part);
            }
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
            if (carried == null)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i] == null || i != placed) continue;
                    if (Flat(parts[i].transform.position, playerPosition) > 1.6f) continue;
                    carried = parts[i];
                    carriedIndex = i;
                    AudioService.Play(Sfx.Blip, .7f, .9f);
                    Report();
                    break;
                }
            }
            else
            {
                carried.transform.position = playerPosition + new Vector3(0f, 1.6f, 0f);
                if (Flat(playerPosition, new Vector3(Task.goal.x, 0f, Task.goal.y)) < 3.2f)
                {
                    Destroy(carried);
                    parts[carriedIndex] = null;
                    carried = null;
                    carriedIndex = -1;
                    placed++;
                    site.SetStage(placed);
                    AudioService.Play(Sfx.Hit, .6f, 1.2f);
                    StoryDirector.Active.Ui.ShowToast(placed >= parts.Count ? "КИІЗ ҮЙ ТІГІЛДІ" : "БӨЛІК ОРНЫНА ҚОЙЫЛДЫ", 2f);
                    Report();
                    if (placed >= parts.Count) Finish();
                }
            }
        }
    }
}
