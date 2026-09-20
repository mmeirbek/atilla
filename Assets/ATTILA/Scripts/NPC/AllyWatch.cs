using System.Collections.Generic;
using ATTILA.Data;
using ATTILA.Environment;
using ATTILA.Gameplay;
using UnityEngine;

namespace ATTILA.NPC
{
    /// <summary>
    /// Watches the chapter's unity value. Once the player has earned enough of it, the promised people ride in
    /// and stay in the aul, so a fair judgement or a call for unity is visible on the ground, not only in the ending.
    /// </summary>
    public sealed class AllyWatch : MonoBehaviour
    {
        private ChapterDefinition chapter;
        private Stage2Content content;
        private readonly List<GameObject> arrived = new();
        private float next;

        public void Initialize(ChapterDefinition value, Stage2Content storyContent)
        {
            chapter = value;
            content = storyContent;
        }

        private void Update()
        {
            if (chapter == null || chapter.allyMinUnity <= 0 || arrived.Count > 0 || Time.time < next) return;
            next = Time.time + .5f;
            var director = StoryDirector.Active;
            if (director == null || director.Quest == null || director.Quest.Unity < chapter.allyMinUnity) return;
            foreach (var placement in chapter.allies) arrived.Add(AulPrototypeBuilder.Person(content, placement));
            if (!string.IsNullOrEmpty(chapter.allyMessage)) director.Ui.ShowToast(chapter.allyMessage, 4f);
        }
    }
}
