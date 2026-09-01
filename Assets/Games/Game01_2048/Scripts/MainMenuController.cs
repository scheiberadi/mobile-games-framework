using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
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
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            CreateLabel(canvasObject.transform, "2048", 48, new Vector2(0, 140));
            CreateLabel(canvasObject.transform, $"High Score: {highScore}", 28, new Vector2(0, 80));

            CreateButton(canvasObject.transform, "New Game", new Vector2(0, 0), true, () =>
            {
                saveService.ClearSave();
                GameSessionIntent.ResumeFromSave = false;
                SceneManager.LoadScene("Game");
            });

            CreateButton(canvasObject.transform, "Continue", new Vector2(0, -70), hasSave, () =>
            {
                GameSessionIntent.ResumeFromSave = true;
                SceneManager.LoadScene("Game");
            });
        }

        private static Text CreateLabel(Transform parent, string content, int fontSize, Vector2 position)
        {
            var textObject = new GameObject("Label", typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.text = content;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 60);
            return text;
        }

        private static void CreateButton(Transform parent, string label, Vector2 position, bool interactable, UnityAction onClick)
        {
            var buttonObject = new GameObject(label + "Button", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220, 50);

            var image = buttonObject.GetComponent<Image>();
            image.color = interactable ? new Color(0.93f, 0.76f, 0.18f) : new Color(0.7f, 0.7f, 0.7f);

            var button = buttonObject.GetComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(onClick);

            CreateLabel(buttonObject.transform, label, 24, Vector2.zero);
        }
    }
}
