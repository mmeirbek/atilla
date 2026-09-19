using UnityEngine.SceneManagement;

namespace ATTILA.Core
{
    public enum SceneId { Boot, MainMenu, CharacterSelect, AulPrototype }

    public static class SceneLoader
    {
        public static void Load(SceneId scene)
        {
            SceneManager.LoadScene(scene switch
            {
                SceneId.Boot => "Boot",
                SceneId.MainMenu => "MainMenu",
                SceneId.CharacterSelect => "CharacterSelect",
                SceneId.AulPrototype => "AulPrototype",
                _ => "MainMenu"
            });
        }
    }
}
