using System;
using System.Collections;
using System.Collections.Generic;
using ATTILA.Audio;
using ATTILA.Characters;
using ATTILA.Environment;
using ATTILA.Player;
using ATTILA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// «Жекпе-жек»: a short turn-based single combat before the battle. Attack, guard, a risky feint and one
    /// remedy. No grid and no army: the duel is about reading the enemy's telegraphed heavy strike.
    /// Every blow is shown: a lunge, a knock back, a flash, the damage number and a shake.
    /// </summary>
    public sealed class DuelMinigame : MonoBehaviour
    {
        // Tuned so a first-time player wins by reading the heavy strike, without a guaranteed walkover.
        private const int MaxHealth = 140;
        private const int EnemyHealth = 110;
        private static readonly Color Blood = new(.86f, .24f, .2f);
        private static readonly Color Heal = new(.48f, .86f, .42f);
        private static readonly Color Ward = new(.45f, .7f, 1f);

        private enum Phase { Intro, Player, Busy, Done }

        private sealed class Fighter
        {
            public Image Image;
            public RectTransform Rect;
            public Vector2 Home;
            public Vector2 Offset;        // lunges and knock-backs, added to the idle bob
            public float Flash;           // seconds of hit tint left
            public float Fall = -1f;      // set when defeated
            public float Seed;
            public int Facing = 1;        // +1 faces right
        }

        private Action won, lost;
        private GameObject root;
        private RectTransform rootRect;
        private Fighter hero, enemy, allyFighter;
        private Image heroBar, enemyBar, heroGhost, enemyGhost, guardGlow, threatGlow;
        private Text logText, heroHealthText, enemyHealthText, feintText, remedyText, threatMark, turnText;
        private CanvasGroup actionGroup;
        private Button[] actions;
        private Button allyButton;
        private int health = MaxHealth, foe = EnemyHealth;
        private int feintCooldown, remedies = 1;
        private bool guarding, enemyWindingUp, ally, allyStrike;
        private Phase phase = Phase.Intro;
        private float time;

        public void Begin(HeroDefinition heroDefinition, bool allySupport, Action onWon, Action onLost)
        {
            won = onWon;
            lost = onLost;
            health = MaxHealth;
            foe = EnemyHealth;
            feintCooldown = 0;
            remedies = 1;
            guarding = false;
            enemyWindingUp = false;
            phase = Phase.Intro;
            ally = allySupport;
            allyStrike = allySupport;
            Build(heroDefinition);
            MinigameFx.Countdown(rootRect, () => { phase = Phase.Player; Refresh(); });
        }

        // ------------------------------------------------------------ layout

        private void Build(HeroDefinition heroDefinition)
        {
            var ui = StoryDirector.Active.Ui;
            root = new GameObject("Duel Minigame", typeof(RectTransform));
            root.transform.SetParent(ui.OverlayRoot, false);
            rootRect = (RectTransform)root.transform;
            UiFactory.Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MenuUiKit.Background(root.transform, .55f);
            AudioService.PlayMusic(MusicTrack.Battle);

            threatGlow = Glow(new Vector2(440, -20), new Color(1f, .2f, .15f, 0f), 620);
            guardGlow = Glow(new Vector2(-440, -20), new Color(Ward.r, Ward.g, Ward.b, 0f), 560);
            if (ally) allyFighter = MakeFighter("warrior", FacingDirection.E, new Vector2(-680, -90), 1, .85f);
            hero = MakeFighter(heroDefinition != null ? heroDefinition.VisualSetId : "kabanbay", FacingDirection.E, new Vector2(-440, -40), 1, 1f);
            enemy = MakeFighter("raider", FacingDirection.W, new Vector2(440, -40), -1, 1f);
            threatMark = GameUiKit.Label(root.transform, "!", 120, Blood, TextAnchor.MiddleCenter, 0, 0, 200, 160);
            threatMark.rectTransform.anchorMin = threatMark.rectTransform.anchorMax = new Vector2(.5f, .5f);
            threatMark.rectTransform.pivot = new Vector2(.5f, .5f);
            threatMark.rectTransform.anchoredPosition = new Vector2(440, 280);
            threatMark.gameObject.AddComponent<Outline>().effectColor = new Color(.15f, .07f, .05f, 1f);
            threatMark.enabled = false;

            var heroPanel = GameUiKit.InkPanel(root.transform, new Vector2(0f, 1f), new Vector2(620, 150), new Vector2(340, -110));
            heroHealthText = HealthPanel(heroPanel, heroDefinition != null ? heroDefinition.DisplayName : "Батыр", out heroBar, out heroGhost);
            var enemyPanel = GameUiKit.InkPanel(root.transform, new Vector2(1f, 1f), new Vector2(620, 150), new Vector2(-340, -110));
            enemyHealthText = HealthPanel(enemyPanel, "Жау батыры", out enemyBar, out enemyGhost);

            var logPanel = GameUiKit.Panel(root.transform, new Vector2(.5f, 1f), new Vector2(1100, 92), new Vector2(0, -300),
                new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .9f), MenuUiKit.Gold);
            logPanel.Border.raycastTarget = false;
            logText = GameUiKit.Label(logPanel.Content, "Жекпе-жек басталады!", 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 20, 20, 1040, 44);

            var actionPanel = GameUiKit.InkPanel(root.transform, new Vector2(.5f, 0f), new Vector2(1560, 210), new Vector2(0, 130));
            actionGroup = actionPanel.GameObject.AddComponent<CanvasGroup>();
            turnText = GameUiKit.Label(root.transform, string.Empty, 22, MenuUiKit.Gold, TextAnchor.MiddleCenter, 0, 0, 800, 34);
            turnText.rectTransform.anchorMin = turnText.rectTransform.anchorMax = new Vector2(.5f, 0f);
            turnText.rectTransform.pivot = new Vector2(.5f, .5f);
            turnText.rectTransform.anchoredPosition = new Vector2(0, 262);
            turnText.gameObject.AddComponent<Outline>().effectColor = new Color(.15f, .07f, .05f, 1f);
            var labels = new List<(string label, string hint, int index)>
            {
                ("ШАБУЫЛ", "18–28 зақым", 0),
                ("ҚОРҒАНУ", "зақым екі есе аз", 1),
                ("АЙЛА", "35 зақым, 60%", 2),
                ("ШИПА", "+34 күш", 3)
            };
            if (ally) labels.Add(("КӨМЕК", "40 зақым", 4));
            actions = new Button[labels.Count];
            // One evenly spaced row, so a fifth button never lands on top of the others.
            float span = 1440f / labels.Count;
            for (int i = 0; i < labels.Count; i++)
            {
                var entry = labels[i];
                float x = -720f + span * (i + .5f);
                var button = MenuPixelButton.Create(actionPanel.Content, entry.label, () => PlayerTurn(entry.index));
                UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, .62f), new Vector2(span - 20f, 108), new Vector2(x, 0));
                var note = GameUiKit.Label(actionPanel.Content, entry.hint, 20, GameUiKit.Muted, TextAnchor.MiddleCenter, 0, 0, span - 24f, 30);
                note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(.5f, .18f);
                note.rectTransform.pivot = new Vector2(.5f, .5f);
                note.rectTransform.anchoredPosition = new Vector2(x, 0);
                actions[i] = button;
                if (entry.index == 2) feintText = button.GetComponentInChildren<Text>();
                if (entry.index == 3) remedyText = button.GetComponentInChildren<Text>();
                if (entry.index == 4) allyButton = button;
            }
            Refresh();
        }

        private Image Glow(Vector2 position, Color color, float size)
        {
            var glow = UiFactory.Panel(root.transform, color);
            glow.raycastTarget = false;
            glow.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.glow : null;
            UiFactory.SetRect(glow.rectTransform, new Vector2(.5f, .5f), new Vector2(size, size), position);
            return glow;
        }

        private Fighter MakeFighter(string look, FacingDirection facing, Vector2 centre, int side, float scale)
        {
            var feet = centre + new Vector2(0, -240 * scale);
            var shadow = UiFactory.Panel(root.transform, new Color(0f, 0f, 0f, .35f));
            shadow.raycastTarget = false;
            shadow.sprite = MvpArtSet.Instance != null ? MvpArtSet.Instance.shadow : null;
            UiFactory.SetRect(shadow.rectTransform, new Vector2(.5f, .5f), new Vector2(220 * scale, 54 * scale), feet + new Vector2(0, 6));
            var set = LookLibrary.Load(look);
            var sprite = set != null && set.directions != null ? set.directions[(int)facing]?.idle : null;
            var image = UiFactory.Panel(root.transform, Color.white);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 0f);   // feet: bobbing and falling turn around the ground point
            rect.sizeDelta = new Vector2(288 * scale, 480 * scale);
            rect.anchoredPosition = feet;
            return new Fighter { Image = image, Rect = rect, Home = feet, Seed = UnityEngine.Random.value * 6f, Facing = side };
        }

        private Text HealthPanel(GameUiKit.Frame frame, string name, out Image bar, out Image ghost)
        {
            frame.Border.raycastTarget = false;
            GameUiKit.Label(frame.Content, name, 28, MenuUiKit.Cream, TextAnchor.UpperLeft, 22, 16, 420, 38);
            var track = UiFactory.Panel(frame.Content, MenuUiKit.Dark);
            track.raycastTarget = false;
            GameUiKit.PlaceTopLeft(track.rectTransform, 22, 66, 560, 30);
            ghost = UiFactory.Panel(track.transform, new Color(1f, .93f, .8f, .85f));
            ghost.raycastTarget = false;
            UiFactory.Stretch(ghost.rectTransform, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            bar = UiFactory.Panel(track.transform, Blood);
            bar.raycastTarget = false;
            UiFactory.Stretch(bar.rectTransform, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            return GameUiKit.Label(frame.Content, string.Empty, 22, MenuUiKit.Gold, TextAnchor.UpperRight, 160, 18, 420, 34);
        }

        // ------------------------------------------------------------ turns

        private void Refresh()
        {
            heroHealthText.text = $"{Mathf.Max(0, health)}/{MaxHealth}";
            enemyHealthText.text = $"{Mathf.Max(0, foe)}/{EnemyHealth}";
            if (feintText != null) feintText.text = feintCooldown > 0 ? $"АЙЛА ({feintCooldown})" : "АЙЛА";
            if (remedyText != null) remedyText.text = remedies > 0 ? "ШИПА" : "ШИПА (0)";
            bool yours = phase == Phase.Player;
            foreach (var button in actions) button.interactable = yours;
            if (yours)
            {
                actions[2].interactable = feintCooldown <= 0;
                actions[3].interactable = remedies > 0;
                if (allyButton != null) allyButton.interactable = allyStrike;
            }
            actionGroup.alpha = yours ? 1f : .45f;
            actionGroup.blocksRaycasts = yours;
            turnText.text = yours ? (enemyWindingUp ? "САҚ БОЛ! ҚОРҒАН!" : "СЕНІҢ КЕЗЕГІҢ") : string.Empty;
            turnText.color = enemyWindingUp && yours ? Blood : MenuUiKit.Gold;
        }

        private void PlayerTurn(int action)
        {
            if (phase != Phase.Player) return;
            phase = Phase.Busy;
            guarding = false;
            Refresh();
            StartCoroutine(Round(action));
        }

        private IEnumerator Round(int action)
        {
            switch (action)
            {
                case 0:
                {
                    yield return Lunge(hero, 1f);
                    int damage = UnityEngine.Random.Range(18, 29);
                    Strike(enemy, damage, false);
                    logText.text = $"Соққың дәл тиді: {damage} зақым.";
                    yield return Return(hero);
                    break;
                }
                case 1:
                    guarding = true;
                    logText.text = "Қалқанды көтердің. Келесі соққы әлсіз тиеді.";
                    AudioService.Play(Sfx.Ring, .7f, 1.2f);
                    MinigameFx.Popup(rootRect, hero.Home + new Vector2(0, 520), "ҚОРҒАН", Ward, 44);
                    MinigameFx.Punch(hero.Rect, .08f, .3f);
                    yield return Wait(.5f);
                    break;
                case 2:
                    feintCooldown = 2;
                    yield return Lunge(hero, 1.25f);
                    if (UnityEngine.Random.value < .6f)
                    {
                        Strike(enemy, 35, true);
                        MinigameFx.Popup(rootRect, enemy.Home + new Vector2(0, 620), "АЙЛА ӨТТІ!", MenuUiKit.Gold, 50);
                        logText.text = "Айла-шарғың өтті! 35 зақым.";
                    }
                    else
                    {
                        enemy.Offset = new Vector2(70f, 0f);
                        MinigameFx.Popup(rootRect, enemy.Home + new Vector2(0, 540), "ЖАЛТАРДЫ", MenuUiKit.Cream, 44);
                        logText.text = "Жау айлаңды аңғарып қалды.";
                        AudioService.Play(Sfx.Fail, .6f);
                    }
                    StartCoroutine(Return(enemy));
                    yield return Return(hero);
                    break;
                case 3:
                    remedies--;
                    health = Mathf.Min(MaxHealth, health + 34);
                    logText.text = "Жарақатыңды таңып, күш жинадың: +34.";
                    AudioService.Play(Sfx.Reward, .7f);
                    MinigameFx.Burst(rootRect, hero.Home + new Vector2(0, 220), Heal, 22, 420f);
                    MinigameFx.Popup(rootRect, hero.Home + new Vector2(0, 520), "+34", Heal, 60);
                    yield return Wait(.6f);
                    break;
                default:
                    allyStrike = false;
                    if (allyFighter != null) yield return Lunge(allyFighter, 1.6f);
                    Strike(enemy, 40, true);
                    MinigameFx.Popup(rootRect, enemy.Home + new Vector2(0, 620), "ЖАСАҚ!", MenuUiKit.Gold, 50);
                    logText.text = "Жасақтас жауды дәл тапты: 40 зақым!";
                    if (allyFighter != null) yield return Return(allyFighter);
                    break;
            }
            if (action != 2 && action != 1 && action != 3) StartCoroutine(Return(enemy));
            Refresh();
            yield return Wait(.45f);
            if (foe <= 0) { yield return Finish(true); yield break; }
            yield return EnemyRound();
            if (feintCooldown > 0) feintCooldown--;
            if (health <= 0) { yield return Finish(false); yield break; }
            phase = Phase.Player;
            Refresh();
        }

        private IEnumerator EnemyRound()
        {
            if (enemyWindingUp)
            {
                enemyWindingUp = false;
                yield return Lunge(enemy, 1.3f);
                int damage = guarding ? 16 : 34;
                Strike(hero, damage, !guarding);
                logText.text = guarding ? $"Ауыр соққыны қалқанмен тостың: {damage} зақым." : $"Ауыр соққы тиді: {damage} зақым!";
                if (guarding) MinigameFx.Popup(rootRect, hero.Home + new Vector2(0, 620), "ТОСТЫҢ!", Ward, 48);
                StartCoroutine(Return(hero));
                yield return Return(enemy);
            }
            else if (UnityEngine.Random.value < .3f)
            {
                enemyWindingUp = true;
                logText.text = "Жау найзасын көтерді — ауыр соққыға дайындалуда!";
                AudioService.Play(Sfx.Toss, .8f, .7f);
                MinigameFx.Punch(threatMark.transform, .6f, .4f);
                yield return Wait(.6f);
            }
            else
            {
                yield return Lunge(enemy, .9f);
                int damage = UnityEngine.Random.Range(12, 21);
                if (guarding) damage /= 2;
                bool blocked = ally && UnityEngine.Random.value < .3f;
                if (blocked) damage /= 2;
                Strike(hero, damage, false);
                logText.text = blocked ? $"Жасақтас қалқанымен бөгеді: {damage} зақым." : $"Жаудың соққысы: {damage} зақым.";
                StartCoroutine(Return(hero));
                yield return Return(enemy);
            }
            Refresh();
            yield return Wait(.3f);
        }

        // ------------------------------------------------------------ motion

        /// <summary>A short wind-back, then a dart toward the other fighter; reach above one is a heavier blow.</summary>
        private IEnumerator Lunge(Fighter fighter, float reach)
        {
            AudioService.Play(Sfx.Toss, .6f, 1.3f);
            var target = new Vector2(fighter.Facing * 300f * reach, 0f);
            var back = new Vector2(-fighter.Facing * 40f, 0f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .12f)
            {
                fighter.Offset = Vector2.Lerp(Vector2.zero, back, t);
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .12f)
            {
                fighter.Offset = Vector2.Lerp(back, target, t * t);
                yield return null;
            }
            fighter.Offset = target;
        }

        private IEnumerator Return(Fighter fighter)
        {
            var from = fighter.Offset;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / .3f)
            {
                fighter.Offset = Vector2.Lerp(from, Vector2.zero, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            fighter.Offset = Vector2.zero;
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        /// <summary>Damage lands: knock back, flash, number, sparks, shake.</summary>
        private void Strike(Fighter target, int damage, bool heavy)
        {
            if (target == enemy) foe -= damage; else health -= damage;
            target.Offset = new Vector2(-target.Facing * (heavy ? 90f : 55f), 0f);
            target.Flash = .3f;
            var at = target.Home + new Vector2(0, 300);
            MinigameFx.Burst(rootRect, at, heavy ? MenuUiKit.Gold : new Color(1f, .85f, .6f), heavy ? 24 : 14, heavy ? 720f : 500f);
            MinigameFx.Popup(rootRect, at + new Vector2(-target.Facing * 60f, 160), "-" + damage, Blood, heavy ? 72 : 56);
            MinigameFx.Shake(rootRect, heavy ? 30f : 16f, heavy ? .4f : .25f);
            AudioService.Play(Sfx.Hit, 1f, heavy ? .7f : 1.05f);
        }

        private void Update()
        {
            if (root == null) return;
            float dt = Time.unscaledDeltaTime;
            time += dt;
            Animate(hero, dt);
            Animate(enemy, dt);
            if (allyFighter != null) Animate(allyFighter, dt);

            // Health bars drain smoothly, with a pale trail showing what the last blow took.
            Drain(heroBar, heroGhost, (float)Mathf.Max(0, health) / MaxHealth, dt);
            Drain(enemyBar, enemyGhost, (float)Mathf.Max(0, foe) / EnemyHealth, dt);

            float threat = enemyWindingUp ? .55f + Mathf.Sin(time * 8f) * .2f : 0f;
            threatGlow.color = new Color(1f, .2f, .15f, Mathf.MoveTowards(threatGlow.color.a, threat, dt * 3f));
            threatMark.enabled = enemyWindingUp;
            if (enemyWindingUp) threatMark.rectTransform.anchoredPosition = new Vector2(440, 300 + Mathf.Sin(time * 6f) * 12f);
            float ward = guarding ? .5f + Mathf.Sin(time * 5f) * .12f : 0f;
            guardGlow.color = new Color(Ward.r, Ward.g, Ward.b, Mathf.MoveTowards(guardGlow.color.a, ward, dt * 3f));
        }

        private void Animate(Fighter fighter, float dt)
        {
            if (fighter.Fall >= 0f)
            {
                fighter.Fall = Mathf.Min(1f, fighter.Fall + dt * 2.2f);
                float ease = fighter.Fall * fighter.Fall;
                fighter.Rect.localRotation = Quaternion.Euler(0f, 0f, fighter.Facing * 82f * ease);
                fighter.Rect.anchoredPosition = fighter.Home + fighter.Offset;
                fighter.Image.color = new Color(.75f, .65f, .62f, 1f - fighter.Fall * .35f);
                return;
            }
            // Idle breathing: a small stretch and bob, each fighter on its own rhythm.
            float breath = Mathf.Sin(time * 2.6f + fighter.Seed);
            bool raised = fighter == enemy && enemyWindingUp;
            fighter.Rect.localScale = new Vector3(1f - breath * .012f, 1f + breath * .02f + (raised ? .05f : 0f), 1f);
            fighter.Rect.anchoredPosition = fighter.Home + fighter.Offset + new Vector2(0f, raised ? 18f : 0f);
            fighter.Rect.localRotation = Quaternion.Euler(0f, 0f, raised ? fighter.Facing * -6f : 0f);
            fighter.Flash = Mathf.Max(0f, fighter.Flash - dt);
            fighter.Image.color = fighter.Flash > 0f && Mathf.Repeat(fighter.Flash * 30f, 2f) < 1f ? new Color(1f, .45f, .4f) : Color.white;
        }

        private static void Drain(Image bar, Image ghost, float target, float dt)
        {
            float shown = Mathf.MoveTowards(bar.rectTransform.anchorMax.x, target, dt * 1.6f);
            bar.rectTransform.anchorMax = new Vector2(shown, 1f);
            float trail = ghost.rectTransform.anchorMax.x;
            trail = trail > shown ? Mathf.MoveTowards(trail, shown, dt * .45f) : shown;
            ghost.rectTransform.anchorMax = new Vector2(trail, 1f);
        }

        private IEnumerator Finish(bool success)
        {
            phase = Phase.Done;
            enemyWindingUp = false;
            guarding = false;
            Refresh();
            logText.text = success ? "Жау батыры жеңілді!" : "Жекпе-жекте жеңілдің.";
            (success ? enemy : hero).Fall = 0f;
            AudioService.Play(Sfx.Hit, 1f, .55f);
            MinigameFx.Shake(rootRect, 34f, .5f);
            yield return Wait(1f);
            bool closed = false;
            MinigameFx.Result(rootRect, success, success ? "Жасақтың рухы көтерілді!" : "Тыныс алып, қайта шық.", () => closed = true);
            while (!closed) yield return null;
            Destroy(root);
            root = null;
            if (success) won?.Invoke(); else lost?.Invoke();
        }
    }
}
