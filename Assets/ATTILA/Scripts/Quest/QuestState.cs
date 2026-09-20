using ATTILA.Data;
using ATTILA.Save;
using UnityEngine;

namespace ATTILA.Quest
{
    /// <summary>Runtime view over one role's chapter progress. All persistent state lives in <see cref="RoleProgress"/>.</summary>
    public sealed class QuestState
    {
        private readonly RoleProgress progress;
        public ChapterDefinition Chapter { get; }
        public int BeatCount => Chapter.beats.Count;
        public int BeatIndex => progress.beatIndex;
        public bool IsComplete => progress.completed || progress.beatIndex >= BeatCount;
        public BeatDefinition Current => IsComplete ? null : Chapter.beats[progress.beatIndex];
        public int Unity => progress.unity;
        public int ProgressPercent => IsComplete ? 100 : Mathf.RoundToInt(100f * progress.beatIndex / Mathf.Max(1, BeatCount));

        public QuestState(ChapterDefinition chapter)
        {
            Chapter = chapter;
            progress = SaveService.Progress(chapter.roleId);
        }

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && progress.flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !progress.flags.Contains(flag)) progress.flags.Add(flag);
        }

        public void AddUnity(int amount) => progress.unity = Mathf.Clamp(progress.unity + amount, 0, 100);

        public void Advance(string checkpoint)
        {
            progress.beatIndex++;
            if (progress.beatIndex >= BeatCount) progress.completed = true;
            if (!string.IsNullOrEmpty(checkpoint)) SaveService.Current.lastCheckpoint = checkpoint;
            SaveService.Save();
        }
    }
}
