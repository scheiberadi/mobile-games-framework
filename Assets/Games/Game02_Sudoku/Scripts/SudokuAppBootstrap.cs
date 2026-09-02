using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game02_Sudoku
{
    public static class SudokuAppBootstrap
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
                case "SudokuMenu":
                    new GameObject("SudokuMenuController").AddComponent<SudokuMenuController>();
                    break;
                case "Sudoku":
                    new GameObject("SudokuController").AddComponent<SudokuController>();
                    break;
                case "SudokuSettings":
                    new GameObject("SudokuSettingsController").AddComponent<SudokuSettingsController>();
                    break;
                case "SudokuHighScores":
                    new GameObject("SudokuHighScoresController").AddComponent<SudokuHighScoresController>();
                    break;
            }
        }
    }
}
