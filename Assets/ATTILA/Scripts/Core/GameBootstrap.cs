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
        }

        private void Start() => SceneLoader.Load(SceneId.MainMenu);
    }
}
