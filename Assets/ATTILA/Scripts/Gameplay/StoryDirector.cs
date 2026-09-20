using System;
using ATTILA.Audio;
using ATTILA.Characters;
using ATTILA.Core;
using ATTILA.Data;
using ATTILA.NPC;
using ATTILA.Player;
using ATTILA.Quest;
using ATTILA.Save;
using ATTILA.UI;
using UnityEngine;

namespace ATTILA.Gameplay
{
    /// <summary>
    /// Runs the selected role's chapter: one beat at a time, each a walk to an NPC, a dialogue and an optional
    /// action (mini-game, campfire, map). The chapter data decides what happens; this class only executes it.
    /// </summary>
    public sealed class StoryDirector : MonoBehaviour
    {
        public static StoryDirector Active { get; private set; }
        public GameUi Ui { get; private set; }
        public QuestState Quest { get; private set; }
        public HeroDefinition Hero { get; private set; }
        public ChapterDefinition Chapter { get; private set; }
        public Stage2Content Content { get; private set; }
        public bool IsModal { get; private set; }
        public string TargetNpcId => Quest != null && !Quest.IsComplete ? Quest.Current?.npcId : null;

        private PlayerController player;
        private SokyrTekeMinigame sokyrTeke;
        private AsykMinigame asyk;
        private DuelMinigame duel;
        private int chapterXp;
        private int chapterEntries;

        private void Awake()
        {
            Active = this;
            Content = Resources.Load<Stage2Content>("Data/Stage2Content");
            if (Content == null) Debug.LogError("ATTILA: story content asset is missing.");
            Hero = HeroCatalog.Selected;
            Chapter = Content != null && Hero != null ? Content.Chapter(Hero.ChapterId) : null;
            if (Chapter == null) Debug.LogError("ATTILA: chapter for the selected role is missing.");
            Quest = new QuestState(Chapter);
        }

        public void Initialize(PlayerController value)
        {
            player = value;
            Ui = gameObject.AddComponent<GameUi>();
            Ui.Initialize(Content, this);
            Ui.SetChapter(Hero, Chapter);
            gameObject.AddComponent<PauseMenu>().Initialize(this);
            player.gameObject.AddComponent<PlayerInteraction>();
            sokyrTeke = gameObject.AddComponent<SokyrTekeMinigame>();
            asyk = gameObject.AddComponent<AsykMinigame>();
            duel = gameObject.AddComponent<DuelMinigame>();
            AudioService.PlayMusic(MusicTrack.Aul);
            RefreshUi();
            if (Quest.IsComplete) ShowFinale();
        }

        public NpcDefinition Npc(string id) => Content.Npc(id);

        // ------------------------------------------------------------ beats

        public void InteractWith(NpcInteractable npc)
        {
            if (IsModal || Ui.DialogueActive) return;
            var beat = Quest.Current;
            if (beat != null && npc.Id == beat.npcId)
            {
                var dialogue = !string.IsNullOrEmpty(beat.altFlag) && Quest.HasFlag(beat.altFlag)
                    ? Content.Dialogue(beat.altDialogueId) ?? Content.Dialogue(beat.dialogueId)
                    : Content.Dialogue(beat.dialogueId);
                Speak(dialogue, () => RunAction(beat));
                return;
            }
            var definition = Npc(npc.Id);
            if (definition == null) return;
            // The full greeting plays once; after that the NPC says a short line instead of repeating itself.
            string met = "MET_" + definition.id;
            var line = Quest.HasFlag(met) ? Content.Dialogue(definition.repeatDialogueId) ?? Content.Dialogue(definition.idleDialogueId)
                                          : Content.Dialogue(definition.idleDialogueId);
            Quest.SetFlag(met);
            SaveService.Save();
            Speak(line, null);
        }

        private void Speak(DialogueDefinition dialogue, Action complete)
        {
            if (dialogue == null) { complete?.Invoke(); return; }
            IsModal = true;
            player.SetInputEnabled(false);
            Ui.ShowDialogue(dialogue, () =>
            {
                IsModal = false;
                player.SetInputEnabled(true);
                complete?.Invoke();
            });
        }

        /// <summary>Applies a dialogue choice: flags steer later text, unity steers the chapter's ending.</summary>
        public void ApplyChoice(DialogueChoice choice)
        {
            Quest.SetFlag(choice.setFlag);
            Quest.AddUnity(choice.unity);
            if (choice.xp > 0) AddXp(choice.xp);
        }

        private void RunAction(BeatDefinition beat)
        {
            switch (beat.action)
            {
                case BeatAction.SokyrTeke:
                    // Played in the world: the player keeps walking, so only NPC interaction is blocked.
                    IsModal = true;
                    sokyrTeke.Begin(player, () => { IsModal = false; CompleteBeat(beat); }, () => Failed(beat, "Уақыт бітті. Балаларды ұстай алмадың."));
                    break;
                case BeatAction.Asyk:
                    BeginOverlay(() => asyk.Begin(() => FinishOverlay(() => CompleteBeat(beat)), () => FinishOverlay(() => Failed(beat, "Асықтар шеңберде қалды. Тағы көрейік пе?"))));
                    break;
                case BeatAction.Duel:
                    BeginOverlay(() => duel.Begin(Hero, () => FinishOverlay(() => CompleteBeat(beat)), () => FinishOverlay(() => Failed(beat, "Жекпе-жекте жеңілдің. Қайта шығасың ба?"))));
                    break;
                case BeatAction.Campfire:
                    BeginOverlay(() => CampfireScreen.Show(Ui.OverlayRoot, this, beat.campfire, () => FinishOverlay(() => CompleteBeat(beat))));
                    break;
                case BeatAction.Map:
                    BeginOverlay(() => MapScreen.Show(Ui.OverlayRoot, beat.map, () => FinishOverlay(() => CompleteBeat(beat))));
                    break;
                default:
                    CompleteBeat(beat);
                    break;
            }
        }

        private MusicTrack worldMusic = MusicTrack.Aul;

        private void BeginOverlay(Action start)
        {
            IsModal = true;
            player.SetInputEnabled(false);
            Ui.SetHudVisible(false);
            worldMusic = AudioService.CurrentTrack;
            start();
        }

        private void FinishOverlay(Action next)
        {
            Ui.SetHudVisible(true);
            AudioService.PlayMusic(worldMusic);
            IsModal = false;
            player.SetInputEnabled(true);
            next();
        }

        private void Failed(BeatDefinition beat, string message)
        {
            IsModal = true;
            Ui.ShowRetry(message, () => RunAction(beat), () =>
            {
                IsModal = false;
                player.SetInputEnabled(true);
            });
        }

        private void CompleteBeat(BeatDefinition beat)
        {
            IsModal = false;
            player.SetInputEnabled(true);
            if (beat.xp > 0) AddXp(beat.xp);
            string unlocked = null;
            if (beat.unlockEntryIds != null)
                foreach (var id in beat.unlockEntryIds)
                    if (SaveService.Unlock(id))
                    {
                        chapterEntries++;
                        unlocked = Content.Entry(id)?.title ?? id;
                    }
            Quest.Advance(beat.checkpoint);
            SaveService.Save();
            string toast = beat.xp > 0 ? $"+{beat.xp} XP" : string.Empty;
            if (unlocked != null) toast = (toast.Length > 0 ? toast + "   ·   " : string.Empty) + "ЭНЦИКЛОПЕДИЯ: " + unlocked;
            if (toast.Length > 0) Ui.ShowToast(toast);
            RefreshUi();
            if (Quest.IsComplete) Invoke(nameof(ShowFinale), 1.2f);
        }

        private void AddXp(int amount)
        {
            chapterXp += amount;
            SaveService.Current.currentXP += amount;
            SaveService.Save();
            Ui.SetXp(SaveService.Current.currentXP);
        }

        public void RefreshUi()
        {
            var beat = Quest.Current;
            Ui.SetObjective(beat != null ? beat.objective : "Тарау аяқталды", Quest.BeatIndex, Quest.BeatCount);
            Ui.SetXp(SaveService.Current.currentXP);
        }

        private void ShowFinale()
        {
            IsModal = true;
            player.SetInputEnabled(false);
            Ui.SetHudVisible(false);
            string text = Chapter.finaleAltMinUnity > 0 && Quest.Unity >= Chapter.finaleAltMinUnity && !string.IsNullOrEmpty(Chapter.finaleAltText)
                ? Chapter.finaleAltText
                : Chapter.finaleText;
            AudioService.PlayMusic(MusicTrack.Night);
            Ui.ShowFinale(Chapter, text, chapterXp, chapterEntries,
                () => SceneLoader.Load(SceneId.MainMenu),
                () => SceneLoader.Load(SceneId.CharacterSelect),
                () => EncyclopediaScreen.Show(Ui.OverlayRoot));
        }

        /// <summary>Restarts the current chapter from its first beat; XP and unlocked entries are kept.</summary>
        public void RestartChapter()
        {
            SaveService.RestartRole(Chapter.roleId);
            SceneLoader.Load(SceneId.AulPrototype);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }
    }
}
