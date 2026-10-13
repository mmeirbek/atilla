using System;
using System.Collections;
using ATTILA.Audio;
using ATTILA.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>
    /// Shared feedback for the mini-games: the 3-2-1 start, the win/lose banner, rising score text, bursts of
    /// pixel sparks and shakes. Everything runs on unscaled time on a runner that dies with the scene.
    /// </summary>
    public sealed class MinigameFx : MonoBehaviour
    {
        private static MinigameFx runner;
        private static Sprite pixel;

        private static MinigameFx Runner
        {
            get
            {
                if (runner == null) runner = new GameObject("Minigame FX").AddComponent<MinigameFx>();
                return runner;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Runner.StartCoroutine(routine);

        // ------------------------------------------------------------ start and finish

        /// <summary>Big «3, 2, 1, БАСТА!» in the middle of the host; <paramref name="done"/> fires on БАСТА.</summary>
        public static void Countdown(Transform host, Action done) => Run(CountdownRoutine(host, done));

        private static IEnumerator CountdownRoutine(Transform host, Action done)
        {
            var root = Layer(host, "Countdown");
            // A dark disc behind the number keeps it readable over bright grass and the blindfold dark alike.
            var disc = UiFactory.Panel(root, new Color(.1f, .05f, .04f, .75f));
            disc.raycastTarget = false;
            disc.sprite = Environment.MvpArtSet.Instance != null ? Environment.MvpArtSet.Instance.glow : null;
            UiFactory.SetRect(disc.rectTransform, new Vector2(.5f, .5f), new Vector2(560, 400), Vector2.zero);
            var text = BigText(root, string.Empty, 200, MenuUiKit.Cream);
            string[] steps = { "3", "2", "1", "БАСТА!" };
            for (int i = 0; i < steps.Length; i++)
            {
                if (root == null) yield break;
                bool last = i == steps.Length - 1;
                text.text = steps[i];
                text.color = last ? MenuUiKit.Gold : MenuUiKit.Cream;
                AudioService.Play(last ? Sfx.Reward : Sfx.Blip, last ? .9f : .8f, last ? 1f : 1.5f - i * .15f);
                if (last) done?.Invoke();
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / (last ? .75f : .6f))
                {
                    if (root == null) yield break;
                    float pop = 1f + .9f * Mathf.Pow(1f - Mathf.Clamp01(t * 3f), 2f);
                    text.rectTransform.localScale = Vector3.one * pop;
                    float fade = t < .7f ? 1f : 1f - (t - .7f) / .3f;
                    text.color = new Color(text.color.r, text.color.g, text.color.b, fade);
                    disc.color = new Color(.1f, .05f, .04f, .75f * (last ? fade : 1f));
                    yield return null;
                }
            }
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }

        /// <summary>«ЖЕҢІС!» with sparks, or a heavier «ЖЕҢІЛДІҢ» line; <paramref name="done"/> fires once it has been seen.</summary>
        public static void Result(Transform host, bool win, string line, Action done) => Run(ResultRoutine(host, win, line, done));

        private static IEnumerator ResultRoutine(Transform host, bool win, string line, Action done)
        {
            var root = Layer(host, "Result");
            var veil = UiFactory.Panel(root, new Color(0f, 0f, 0f, 0f));
            veil.raycastTarget = false;
            UiFactory.Stretch(veil.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var band = UiFactory.Panel(root, win ? new Color(.43f, .11f, .09f, .95f) : new Color(.12f, .08f, .07f, .95f));
            band.raycastTarget = false;
            UiFactory.SetRect(band.rectTransform, new Vector2(.5f, .5f), new Vector2(4000, 300), Vector2.zero);
            foreach (float y in new[] { 150f, -150f })
            {
                var edge = UiFactory.Panel(band.transform, MenuUiKit.Gold);
                edge.raycastTarget = false;
                UiFactory.SetRect(edge.rectTransform, new Vector2(.5f, .5f), new Vector2(4000, 8), new Vector2(0, y));
            }
            var title = BigText(root, win ? "ЖЕҢІС!" : "ЖЕҢІЛДІҢ", 110, win ? MenuUiKit.Gold : MenuUiKit.Cream);
            title.rectTransform.anchoredPosition = new Vector2(0, 40);
            var sub = BigText(root, line ?? string.Empty, 30, MenuUiKit.Parchment);
            sub.rectTransform.anchoredPosition = new Vector2(0, -80);
            sub.rectTransform.sizeDelta = new Vector2(1600, 60);
            AudioService.Play(win ? Sfx.Reward : Sfx.Fail, 1f, win ? 1f : .8f);
            if (win)
            {
                Burst(root, new Vector2(-420, 40), MenuUiKit.Gold, 26, 900f);
                Burst(root, new Vector2(420, 40), MenuUiKit.Cream, 26, 900f);
                Burst(root, new Vector2(0, 60), new Color(.85f, .25f, .2f), 20, 700f);
            }
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .45f)
            {
                if (root == null) yield break;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                band.rectTransform.localScale = new Vector3(1f, ease, 1f);
                veil.color = new Color(0f, 0f, 0f, .45f * ease);
                title.rectTransform.localScale = Vector3.one * (win ? 1f + .6f * (1f - ease) + Mathf.Sin(t * Mathf.PI) * .15f : 1f);
                yield return null;
            }
            float hold = win ? 1.6f : 1.4f;
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            {
                if (root == null) yield break;
                if (win) title.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * .03f);
                yield return null;
            }
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            done?.Invoke();
        }

        // ------------------------------------------------------------ small feedback

        /// <summary>Text that jumps up from a point and fades, for hits, catches and scores.</summary>
        public static void Popup(Transform host, Vector2 position, string value, Color color, int size = 44) =>
            Run(PopupRoutine(host, position, value, color, size));

        private static IEnumerator PopupRoutine(Transform host, Vector2 position, string value, Color color, int size)
        {
            if (host == null) yield break;
            var text = UiFactory.Text(host, value, size, TextAnchor.MiddleCenter, color);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.15f, .07f, .05f, 1f);
            outline.effectDistance = new Vector2(4, -4);
            UiFactory.SetRect(text.rectTransform, new Vector2(.5f, .5f), new Vector2(800, 100), position);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.1f)
            {
                if (text == null) yield break;
                float rise = 1f - Mathf.Pow(1f - t, 3f);
                text.rectTransform.anchoredPosition = position + new Vector2(0, rise * 120f);
                text.rectTransform.localScale = Vector3.one * (t < .15f ? Mathf.Lerp(.4f, 1.25f, t / .15f) : Mathf.Lerp(1.25f, 1f, (t - .15f) * 4f));
                float alpha = t < .65f ? 1f : 1f - (t - .65f) / .35f;
                text.color = new Color(color.r, color.g, color.b, alpha);
                outline.effectColor = new Color(.15f, .07f, .05f, alpha);
                yield return null;
            }
            if (text != null) UnityEngine.Object.Destroy(text.gameObject);
        }

        /// <summary>Square pixel sparks that fly out, fall and fade.</summary>
        public static void Burst(Transform host, Vector2 position, Color color, int count = 14, float speed = 520f) =>
            Run(BurstRoutine(host, position, color, count, speed));

        private static IEnumerator BurstRoutine(Transform host, Vector2 position, Color color, int count, float speed)
        {
            if (host == null) yield break;
            var sparks = new RectTransform[count];
            var images = new Image[count];
            var velocity = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var image = UiFactory.Panel(host, color);
                image.raycastTarget = false;
                float size = UnityEngine.Random.Range(8f, 18f);
                UiFactory.SetRect(image.rectTransform, new Vector2(.5f, .5f), new Vector2(size, size), position);
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .8f + .45f) * speed * UnityEngine.Random.Range(.35f, 1f);
                sparks[i] = image.rectTransform;
                images[i] = image;
            }
            for (float t = 0f; t < .9f; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    if (sparks[i] == null) yield break;
                    velocity[i] += Vector2.down * 1400f * dt;
                    sparks[i].anchoredPosition += velocity[i] * dt;
                    images[i].color = new Color(color.r, color.g, color.b, 1f - t / .9f);
                }
                yield return null;
            }
            foreach (var spark in sparks) if (spark != null) UnityEngine.Object.Destroy(spark.gameObject);
        }

        /// <summary>Jolts a UI element around its resting place.</summary>
        public static void Shake(RectTransform target, float strength = 18f, float seconds = .3f) => Run(ShakeRoutine(target, strength, seconds));

        // Overlapping shakes share the first one's resting place, so the element never drifts.
        private static readonly System.Collections.Generic.Dictionary<RectTransform, (Vector2 home, int count)> shaking = new();

        private static IEnumerator ShakeRoutine(RectTransform target, float strength, float seconds)
        {
            if (target == null) yield break;
            var home = shaking.TryGetValue(target, out var entry) ? entry.home : target.anchoredPosition;
            shaking[target] = (home, entry.count + 1);
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (target == null) break;
                float fall = 1f - t / seconds;
                target.anchoredPosition = home + UnityEngine.Random.insideUnitCircle * strength * fall;
                yield return null;
            }
            if (shaking.TryGetValue(target, out entry))
            {
                if (entry.count <= 1) shaking.Remove(target); else shaking[target] = (entry.home, entry.count - 1);
                if (target != null && entry.count <= 1) target.anchoredPosition = home;
            }
        }

        /// <summary>Quick scale kick that settles back to one.</summary>
        public static void Punch(Transform target, float amount = .25f, float seconds = .25f) => Run(PunchRoutine(target, amount, seconds));

        private static IEnumerator PunchRoutine(Transform target, float amount, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                target.localScale = Vector3.one * (1f + amount * Mathf.Sin(t / seconds * Mathf.PI) * (1f - t / seconds));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        /// <summary>Shakes the world camera; the camera adds the offset after its pixel snap.</summary>
        public static void ShakeCamera(float strength = .25f, float seconds = .25f) => CameraController.Shake(strength, seconds);

        /// <summary>Where a world point lands on a stretched overlay rect, for popups above characters.</summary>
        public static Vector2 WorldToOverlay(RectTransform overlay, Vector3 world)
        {
            var camera = Camera.main;
            if (camera == null || overlay == null) return Vector2.zero;
            var screen = camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out var local);
            return local;
        }

        /// <summary>An expanding ring, like a sound travelling out from a clap.</summary>
        public static void Ripple(Transform host, Vector2 position, Color color, float size = 260f) => Run(RippleRoutine(host, position, color, size));

        private static IEnumerator RippleRoutine(Transform host, Vector2 position, Color color, float size)
        {
            if (host == null) yield break;
            var ring = UiFactory.Panel(host, color);
            ring.raycastTarget = false;
            ring.sprite = RingSprite;
            UiFactory.SetRect(ring.rectTransform, new Vector2(.5f, .5f), new Vector2(size, size), position);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .7f)
            {
                if (ring == null) yield break;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(.2f, 1f, 1f - Mathf.Pow(1f - t, 2f));
                ring.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
                yield return null;
            }
            if (ring != null) UnityEngine.Object.Destroy(ring.gameObject);
        }

        private static Sprite ring;

        /// <summary>A hard-edged pixel ring, drawn once.</summary>
        private static Sprite RingSprite
        {
            get
            {
                if (ring != null) return ring;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + .5f - size / 2f, dy = y + .5f - size / 2f;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        pixels[y * size + x] = distance > size / 2f - 5f && distance < size / 2f - 1f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
                return ring;
            }
        }

        // ------------------------------------------------------------ world particles

        /// <summary>Puffs of ground dust at a world point, e.g. under galloping hooves.</summary>
        public static void Dust(Vector3 position, Color color, int count = 3, float size = .35f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Dust");
                go.transform.position = position + new Vector3(UnityEngine.Random.Range(-.4f, .4f), .1f, UnityEngine.Random.Range(-.3f, .3f));
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Pixel;
                renderer.color = color;
                go.AddComponent<DustPuff>().Configure(color, size * UnityEngine.Random.Range(.7f, 1.3f));
            }
        }

        /// <summary>A white 4x4 sprite, the building block for world particles.</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                pixel = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
                return pixel;
            }
        }

        // ------------------------------------------------------------ helpers

        private static RectTransform Layer(Transform host, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rect = (RectTransform)go.transform;
            UiFactory.Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect;
        }

        private static Text BigText(Transform parent, string value, int size, Color color)
        {
            var text = UiFactory.Text(parent, value, size, TextAnchor.MiddleCenter, color);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.15f, .07f, .05f, 1f);
            outline.effectDistance = new Vector2(4, -4);
            UiFactory.SetRect(text.rectTransform, new Vector2(.5f, .5f), new Vector2(1600, 260), Vector2.zero);
            return text;
        }
    }

    /// <summary>One dust particle: drifts up, grows, faces the camera and fades out.</summary>
    public sealed class DustPuff : MonoBehaviour
    {
        private Color color;
        private float size, age;
        private Vector3 drift;
        private SpriteRenderer spriteRenderer;

        public void Configure(Color value, float scale)
        {
            color = value;
            size = scale;
            drift = new Vector3(UnityEngine.Random.Range(-.4f, .4f), UnityEngine.Random.Range(.5f, 1f), UnityEngine.Random.Range(-.2f, .2f));
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            age += Time.deltaTime;
            const float life = .7f;
            if (age >= life) { Destroy(gameObject); return; }
            transform.position += drift * Time.deltaTime;
            var camera = Camera.main;
            if (camera != null) transform.rotation = camera.transform.rotation;
            transform.localScale = Vector3.one * size * (1f + age * 1.6f);
            spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * (1f - age / life));
        }
    }
}
