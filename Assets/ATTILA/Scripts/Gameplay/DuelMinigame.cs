using System;
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
    /// </summary>
    public sealed class DuelMinigame : MonoBehaviour
    {
        private const int MaxHealth = 120;
        private const int EnemyHealth = 130;

        private enum Phase { Player, Resolving, Enemy, Done }

        private Action won, lost;
        private GameObject root;
        private Image heroImage, enemyImage, heroBar, enemyBar;
        private Text logText, heroHealthText, enemyHealthText, feintText, remedyText;
        private Button[] actions;
        private int health = MaxHealth, enemy = EnemyHealth;
        private int feintCooldown, remedies = 1;
        private bool guarding, enemyWindingUp;
        private Phase phase = Phase.Player;
        private float timer;
        private Vector2 heroHome, enemyHome;
        private float heroShake, enemyShake;

        public void Begin(HeroDefinition hero, Action onWon, Action onLost)
        {
            won = onWon;
            lost = onLost;
            health = MaxHealth;
            enemy = EnemyHealth;
            feintCooldown = 0;
            remedies = 1;
            guarding = false;
            enemyWindingUp = false;
            phase = Phase.Player;
            Build(hero);
        }

        private void Build(HeroDefinition hero)
        {
            var ui = StoryDirector.Active.Ui;
            root = new GameObject("Duel Minigame", typeof(RectTransform));
            root.transform.SetParent(ui.OverlayRoot, false);
            UiFactory.Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MenuUiKit.Background(root.transform, .62f);
            AudioService.PlayMusic(MusicTrack.Battle);

            heroHome = new Vector2(-460, -40);
            enemyHome = new Vector2(460, -40);
            heroImage = Fighter(hero != null ? hero.VisualSetId : "kabanbay", FacingDirection.E, heroHome);
            enemyImage = Fighter("raider", FacingDirection.W, enemyHome);

            var heroPanel = GameUiKit.InkPanel(root.transform, new Vector2(0f, 1f), new Vector2(620, 150), new Vector2(340, -110));
            heroHealthText = HealthPanel(heroPanel, hero != null ? hero.DisplayName : "Батыр", out heroBar);
            var enemyPanel = GameUiKit.InkPanel(root.transform, new Vector2(1f, 1f), new Vector2(620, 150), new Vector2(-340, -110));
            enemyHealthText = HealthPanel(enemyPanel, "Жау батыры", out enemyBar);

            var logPanel = GameUiKit.Panel(root.transform, new Vector2(.5f, 1f), new Vector2(1100, 92), new Vector2(0, -300),
                new Color(GameUiKit.Ink.r, GameUiKit.Ink.g, GameUiKit.Ink.b, .9f), MenuUiKit.Gold);
            logPanel.Border.raycastTarget = false;
            logText = GameUiKit.Label(logPanel.Content, "Жекпе-жек басталды.", 26, MenuUiKit.Cream, TextAnchor.MiddleCenter, 20, 20, 1040, 44);

            var actionPanel = GameUiKit.InkPanel(root.transform, new Vector2(.5f, 0f), new Vector2(1240, 200), new Vector2(0, 130));
            actions = new Button[4];
            actions[0] = Action("ШАБУЫЛ", "18–28 зақым", -450, () => PlayerTurn(0));
            actions[1] = Action("ҚОРҒАНУ", "зақым екі есе аз", -150, () => PlayerTurn(1));
            actions[2] = Action("АЙЛА", "35 зақым, 60%", 150, () => PlayerTurn(2));
            actions[3] = Action("ШИПА", "+34 күш", 450, () => PlayerTurn(3));
            feintText = actions[2].GetComponentInChildren<Text>();
            remedyText = actions[3].GetComponentInChildren<Text>();

            Button Action(string label, string hint, float x, Action click)
            {
                var button = MenuPixelButton.Create(actionPanel.Content, label, click);
                UiFactory.SetRect(button.GetComponent<RectTransform>(), new Vector2(.5f, .62f), new Vector2(300, 98), new Vector2(x, 0));
                var note = GameUiKit.Label(actionPanel.Content, hint, 20, GameUiKit.Muted, TextAnchor.MiddleCenter, 0, 0, 280, 30);
                note.rectTransform.anchorMin = note.rectTransform.anchorMax = new Vector2(.5f, .2f);
                note.rectTransform.anchoredPosition = new Vector2(x, 0);
                return button;
            }

            Refresh();
        }

        private Image Fighter(string look, FacingDirection facing, Vector2 position)
        {
            var set = LookLibrary.Load(look);
            var sprite = set != null && set.directions != null ? set.directions[(int)facing]?.idle : null;
            var image = UiFactory.Panel(root.transform, Color.white);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.preserveAspect = true;
            UiFactory.SetRect(image.rectTransform, new Vector2(.5f, .5f), new Vector2(288, 480), position);
            return image;
        }

        private Text HealthPanel(GameUiKit.Frame frame, string name, out Image bar)
        {
            frame.Border.raycastTarget = false;
            GameUiKit.Label(frame.Content, name, 28, MenuUiKit.Cream, TextAnchor.UpperLeft, 22, 16, 420, 38);
            var track = UiFactory.Panel(frame.Content, MenuUiKit.Dark);
            track.raycastTarget = false;
            GameUiKit.PlaceTopLeft(track.rectTransform, 22, 66, 560, 30);
            bar = UiFactory.Panel(track.transform, new Color(.72f, .22f, .2f));
            bar.raycastTarget = false;
            UiFactory.Stretch(bar.rectTransform, Vector2.zero, new Vector2(1f, 1f), new Vector2(3, 3), new Vector2(-3, -3));
            var text = GameUiKit.Label(frame.Content, string.Empty, 22, MenuUiKit.Gold, TextAnchor.UpperRight, 160, 18, 420, 34);
            return text;
        }

        private void Refresh()
        {
            heroBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)health / MaxHealth), 1f);
            enemyBar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)enemy / EnemyHealth), 1f);
            heroHealthText.text = $"{Mathf.Max(0, health)}/{MaxHealth}";
            enemyHealthText.text = $"{Mathf.Max(0, enemy)}/{EnemyHealth}";
            if (feintText != null) feintText.text = feintCooldown > 0 ? $"АЙЛА ({feintCooldown})" : "АЙЛА";
            if (remedyText != null) remedyText.text = remedies > 0 ? "ШИПА" : "ШИПА (0)";
            foreach (var button in actions) button.interactable = phase == Phase.Player;
            if (phase == Phase.Player)
            {
                actions[2].interactable = feintCooldown <= 0;
                actions[3].interactable = remedies > 0;
            }
        }

        private void PlayerTurn(int action)
        {
            if (phase != Phase.Player) return;
            guarding = false;
            switch (action)
            {
                case 0:
                    int damage = UnityEngine.Random.Range(18, 29);
                    enemy -= damage;
                    enemyShake = .35f;
                    logText.text = $"Соққың дәл тиді: {damage} зақым.";
                    AudioService.Play(Sfx.Hit, .9f);
                    break;
                case 1:
                    guarding = true;
                    logText.text = "Қалқанды көтердің. Келесі соққы әлсіз тиеді.";
                    AudioService.Play(Sfx.Click);
                    break;
                case 2:
                    feintCooldown = 2;
                    if (UnityEngine.Random.value < .6f)
                    {
                        enemy -= 35;
                        enemyShake = .5f;
                        logText.text = "Айла-шарғың өтті! 35 зақым.";
                        AudioService.Play(Sfx.Hit, 1f, .8f);
                    }
                    else
                    {
                        logText.text = "Жау айлаңды аңғарып қалды.";
                        AudioService.Play(Sfx.Fail, .6f);
                    }
                    break;
                default:
                    remedies--;
                    health = Mathf.Min(MaxHealth, health + 34);
                    logText.text = "Жарақатыңды таңып, күш жинадың: +34.";
                    AudioService.Play(Sfx.Reward, .6f);
                    break;
            }
            phase = Phase.Resolving;
            enemyIsNext = true;
            timer = .75f;
            Refresh();
        }

        private void EnemyTurn()
        {
            if (enemyWindingUp)
            {
                enemyWindingUp = false;
                int damage = guarding ? 16 : 34;
                health -= damage;
                heroShake = .5f;
                logText.text = guarding ? $"Ауыр соққыны қалқанмен тостың: {damage} зақым." : $"Ауыр соққы тиді: {damage} зақым!";
                AudioService.Play(Sfx.Hit, 1f, .75f);
            }
            else if (UnityEngine.Random.value < .3f)
            {
                enemyWindingUp = true;
                logText.text = "Жау найзасын көтерді — ауыр соққыға дайындалуда!";
                AudioService.Play(Sfx.Toss, .7f);
            }
            else
            {
                int damage = UnityEngine.Random.Range(12, 21);
                if (guarding) damage /= 2;
                health -= damage;
                heroShake = .3f;
                logText.text = $"Жаудың соққысы: {damage} зақым.";
                AudioService.Play(Sfx.Hit, .7f, 1.1f);
            }
            if (feintCooldown > 0) feintCooldown--;
            phase = Phase.Resolving;
            enemyIsNext = false;
            timer = .8f;
            Refresh();
        }

        private void Update()
        {
            if (root == null) return;
            float delta = Time.unscaledDeltaTime;
            heroShake = Mathf.Max(0f, heroShake - delta);
            enemyShake = Mathf.Max(0f, enemyShake - delta);
            heroImage.rectTransform.anchoredPosition = heroHome + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * heroShake * 18f, 0f);
            enemyImage.rectTransform.anchoredPosition = enemyHome + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * enemyShake * 18f, 0f);
            heroImage.color = heroShake > .2f ? new Color(1f, .6f, .6f) : Color.white;
            enemyImage.color = enemyShake > .2f ? new Color(1f, .6f, .6f) : Color.white;
            if (phase != Phase.Resolving) return;
            timer -= delta;
            if (timer > 0f) return;
            if (enemy <= 0) { Finish(true); return; }
            if (health <= 0) { Finish(false); return; }
            if (enemyIsNext) { phase = Phase.Enemy; EnemyTurn(); }
            else { phase = Phase.Player; Refresh(); }
        }

        private bool enemyIsNext;

        private void Finish(bool success)
        {
            phase = Phase.Done;
            logText.text = success ? "Жау батыры жеңілді!" : "Жекпе-жекте жеңілдің.";
            Destroy(root);
            root = null;
            if (success) won?.Invoke(); else lost?.Invoke();
        }
    }
}
