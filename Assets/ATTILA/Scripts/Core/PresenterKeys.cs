using ATTILA.Characters;
using ATTILA.Gameplay;
using ATTILA.Save;
using UnityEngine;

namespace ATTILA.Core
{
    /// <summary>
    /// Hidden keys for live showings, so a stuck moment never stalls a demo. All of them need Shift held:
    /// 1/2/3 open a role's chapter from its start, N completes the current step, M returns to the main menu,
    /// R clears all progress and returns to the main menu. Nothing is shown on screen.
    /// </summary>
    public sealed class PresenterKeys : MonoBehaviour
    {
        private void Update()
        {
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return;
            for (int i = 0; i < HeroCatalog.Order.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) StartChapter(HeroCatalog.Order[i]);
            if (Input.GetKeyDown(KeyCode.N) && StoryDirector.Active != null) StoryDirector.Active.PresenterSkip();
            if (Input.GetKeyDown(KeyCode.M)) Leave(false);
            if (Input.GetKeyDown(KeyCode.R)) Leave(true);
        }

        private static void StartChapter(string roleId)
        {
            Time.timeScale = 1f;
            SaveService.SelectHero(roleId);
            SaveService.RestartRole(roleId);
            SceneLoader.Load(SceneId.AulPrototype);
        }

        private static void Leave(bool clearProgress)
        {
            Time.timeScale = 1f;
            if (clearProgress) SaveService.ResetForTests();
            SceneLoader.Load(SceneId.MainMenu);
        }
    }
}
