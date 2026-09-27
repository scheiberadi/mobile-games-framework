using UnityEngine;

namespace EvasLearningWorld.App
{
    public static class EvaBootstrap
    {
        // Runs its Launch one frame after it is created, so the objects destroyed by a restart are really gone.
        private sealed class Relauncher : MonoBehaviour
        {
            private void Start()
            {
                Launch();
                Destroy(gameObject);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            // Ask for 120 fps with vsync off; the panel picks the highest rate it can actually deliver.
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            Launch();
        }

        private static void Launch()
        {
            var game = new GameObject("EvaGame", typeof(EvaGame)).GetComponent<EvaGame>();
            game.Build();
            game.StartOverRequested += () =>
            {
                // UiFactory.CreateCanvas only creates an EventSystem when none exists, so the old one is kept and reused.
                if (game.OwnedCanvas != null) Object.Destroy(game.OwnedCanvas);
                Object.Destroy(game.gameObject);
                new GameObject("EvaRelaunch", typeof(Relauncher));
            };
        }
    }
}
