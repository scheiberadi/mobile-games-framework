using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game01_2048
{
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "MainMenu":
                    new GameObject("MainMenuController").AddComponent<MainMenuController>();
                    break;
                case "Game":
                    new GameObject("Game2048Controller").AddComponent<Game2048Controller>();
                    break;
            }
        }
    }
}
