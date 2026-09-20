using System;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Environment;
using ATTILA.Player;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// «Соқыр теке»: the player is blindfolded, so the view shrinks to a small circle and the children give
    /// themselves away by clapping. Catch two of them inside the ring before the time runs out.
    /// </summary>
    public sealed class SokyrTekeMinigame : MonoBehaviour
    {
        private const float Duration = 45f;
        private const int Needed = 2;
        private const float Radius = 8.5f;
        private const float CatchDistance = 1.5f;
        private static readonly Vector3 Centre = new(0f, 1f, -12f);

        private sealed class Runner
        {
            public Transform Transform;
            public PlayerVisual Visual;
            public Vector3 Direction;
            public float Retarget;
        }

        private readonly List<Runner> runners = new();
        private PlayerController player;
        private Transform ring;
        private GameObject clapMarker;
        private Action won, lost;
        private float startedAt, nextClap, clapHide;
        private int caught;
        private bool active;
        private Vector3 returnPosition;

        public void Begin(PlayerController playerController, Action onWon, Action onLost)
        {
            Cleanup();
            player = playerController;
            won = onWon;
            lost = onLost;
            caught = 0;
            active = true;
            startedAt = Time.time;
            nextClap = Time.time + .6f;
            returnPosition = player.transform.position;
            Teleport(player.transform, Centre);

            var art = MvpArtSet.Instance;
            ring = new GameObject("Sokyr Teke ring").transform;
            ring.SetParent(transform, false);
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2f / 40f;
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "Ring stone";
                stone.transform.SetParent(ring, false);
                stone.transform.position = Centre + new Vector3(Mathf.Cos(angle) * Radius, -.94f, Mathf.Sin(angle) * Radius);
                stone.transform.localScale = new Vector3(.5f, .12f, .5f);
                stone.transform.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                Destroy(stone.GetComponent<Collider>());
                stone.GetComponent<Renderer>().sharedMaterial = WorldArt.SharedFlat(new Color(.86f, .78f, .58f));
            }
            string[] looks = { "child", "child_girl", "child" };
            for (int i = 0; i < looks.Length; i++)
            {
                var go = new GameObject("Sokyr Teke runner " + i);
                go.transform.SetParent(transform, false);
                float angle = i * Mathf.PI * 2f / looks.Length;
                go.transform.position = Centre + new Vector3(Mathf.Cos(angle) * 4.5f, 0f, Mathf.Sin(angle) * 4.5f);
                var visual = go.AddComponent<PlayerVisual>();
                visual.SetSpriteSet(LookLibrary.Load(looks[i]));
                WorldArt.AddShadow(go.transform);
                runners.Add(new Runner { Transform = go.transform, Visual = visual, Direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) });
            }
            clapMarker = new GameObject("Clap marker");
            clapMarker.transform.SetParent(transform, false);
            var renderer = clapMarker.AddComponent<SpriteRenderer>();
            renderer.sprite = art != null ? art.marker : null;
            renderer.enabled = false;
            clapMarker.transform.localScale = Vector3.one * .8f;

            var ui = StoryDirector.Active.Ui;
            ui.ShowMinigame("СОҚЫР ТЕКЕ", "Көзің байлаулы. Шапалақ дыбысына жүріп, екі баланы ұста.");
            ui.SetMinigameStatus($"Ұсталды: 0/{Needed}");
            ui.SetVignette(.97f, 900f);
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
            float remaining = Mathf.Max(0f, Duration - (Time.time - startedAt));
            ui.SetMinigameTime(remaining);

            var playerPosition = player.transform.position;
            foreach (var runner in runners)
            {
                var position = runner.Transform.position;
                var away = position - playerPosition;
                away.y = 0f;
                float distance = away.magnitude;
                bool fleeing = distance < 4.2f;
                if (fleeing) runner.Direction = Vector3.Slerp(runner.Direction, away.normalized, Time.deltaTime * 6f);
                else if (Time.time > runner.Retarget)
                {
                    runner.Retarget = Time.time + UnityEngine.Random.Range(.8f, 2f);
                    runner.Direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;
                }
                var offset = position - Centre;
                offset.y = 0f;
                if (offset.magnitude > Radius - .8f) runner.Direction = Vector3.Slerp(runner.Direction, -offset.normalized, .5f);
                float speed = fleeing ? 3.1f : 1.5f;
                position += runner.Direction * speed * Time.deltaTime;
                position.y = 1f;
                runner.Transform.position = position;
                runner.Visual.SetMovement(runner.Direction, true);
                if (distance < CatchDistance) Catch(runner);
            }

            if (Time.time > nextClap && runners.Count > 0) Clap(playerPosition);
            if (clapMarker.activeSelf && Time.time > clapHide) clapMarker.GetComponent<SpriteRenderer>().enabled = false;
            if (caught >= Needed) Finish(true);
            else if (remaining <= 0f) Finish(false);
        }

        private void Clap(Vector3 playerPosition)
        {
            Runner closest = null;
            float best = float.MaxValue;
            foreach (var runner in runners)
            {
                float distance = Vector3.Distance(runner.Transform.position, playerPosition);
                if (distance < best) { best = distance; closest = runner; }
            }
            if (closest == null) return;
            nextClap = Time.time + Mathf.Lerp(.5f, 1.4f, Mathf.InverseLerp(1f, 10f, best));
            AudioService.Play(Sfx.Ring, Mathf.Clamp01(1.3f - best / 12f), UnityEngine.Random.Range(.9f, 1.15f));
            var renderer = clapMarker.GetComponent<SpriteRenderer>();
            clapMarker.transform.position = closest.Transform.position + Vector3.up * 2.6f;
            renderer.enabled = true;
            clapHide = Time.time + .45f;
        }

        private void Catch(Runner runner)
        {
            caught++;
            AudioService.Play(Sfx.Reward, .7f, 1.2f);
            runners.Remove(runner);
            Destroy(runner.Transform.gameObject);
            StoryDirector.Active.Ui.SetMinigameStatus($"Ұсталды: {caught}/{Needed}");
        }

        private void Finish(bool success)
        {
            active = false;
            var ui = StoryDirector.Active.Ui;
            ui.HideMinigame();
            ui.SetVignette(.38f, 3000f);
            Cleanup();
            Teleport(player.transform, returnPosition);
            if (success) won?.Invoke(); else lost?.Invoke();
        }

        private void Cleanup()
        {
            foreach (var runner in runners) if (runner.Transform != null) Destroy(runner.Transform.gameObject);
            runners.Clear();
            if (ring != null) Destroy(ring.gameObject);
            if (clapMarker != null) Destroy(clapMarker);
            ring = null;
            clapMarker = null;
        }
    }
}
