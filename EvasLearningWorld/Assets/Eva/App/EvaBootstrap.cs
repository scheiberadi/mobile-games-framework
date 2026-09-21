using MobileGamesFramework.UI;
using UnityEngine;

namespace EvasLearningWorld.App
{
    public static class EvaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            Application.targetFrameRate = 60;
            var canvas = UiFactory.CreateCanvas(new Vector2(1600, 900), 1f);
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.91f, 1f), new Color(0.91f, 0.97f, 0.88f));
            var text = UiFactory.CreateText(canvas.transform, "Info", 48, TextAnchor.MiddleCenter);
            UiFactory.SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = "Eva's Learning World\n" + Screen.width + "x" + Screen.height + "  " + Screen.orientation
                + "\nsafe area: " + Screen.safeArea;
        }
    }
}
