using UnityEngine;

namespace EvasLearningWorld.App
{
    public static class EvaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            // Ask for 120 fps with vsync off; the panel picks the highest rate it can actually deliver.
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            new GameObject("EvaGame", typeof(EvaGame)).GetComponent<EvaGame>().Build();
        }
    }
}
