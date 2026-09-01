using UnityEngine;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Persistence;

namespace Game01_2048
{
    public class MainMenuController : MonoBehaviour
    {
        private const string GameId = "2048";

        private void Start()
        {
            var store = new PlayerPrefsStore();
            var saveService = new Game2048SaveService(store);
            var highScoreStore = new HighScoreStore(store);

            BuildUi(saveService, saveService.HasSave(), highScoreStore.GetHighScore(GameId));
        }

        private void BuildUi(Game2048SaveService saveService, bool hasSave, int highScore)
        {
            var canvas = UiFactory.CreateCanvas();

            var title = UiFactory.CreateText(canvas.transform, "Title", 48, TextAnchor.MiddleCenter);
            title.text = "2048";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(400, 60));

            var highScoreText = UiFactory.CreateText(canvas.transform, "HighScoreText", 28, TextAnchor.MiddleCenter);
            highScoreText.text = $"High Score: {highScore}";
            UiFactory.SetRect(highScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(400, 40));

            UiFactory.CreateButton(canvas.transform, "New Game", new Vector2(0, 0), new Vector2(220, 50), true, () =>
            {
                saveService.ClearSave();
                GameSessionIntent.ResumeFromSave = false;
                SceneManager.LoadScene("Game");
            });

            UiFactory.CreateButton(canvas.transform, "Continue", new Vector2(0, -70), new Vector2(220, 50), hasSave, () =>
            {
                GameSessionIntent.ResumeFromSave = true;
                SceneManager.LoadScene("Game");
            });
        }
    }
}
