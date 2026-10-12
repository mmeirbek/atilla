using ATTILA.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ATTILA.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap instance;

        private void Awake()
        {
            if (instance != null) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<PresenterKeys>();
        }

        private void Start()
        {
            AudioService.PlayMusic(MusicTrack.Menu);
            SceneLoader.Load(SceneId.MainMenu);
        }
    }
}
