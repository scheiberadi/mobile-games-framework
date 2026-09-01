using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuMenuController : MonoBehaviour
    {
        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private GameObject _difficultyPopup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "SudokuMenu") return;
            new GameObject("SudokuMenuController").AddComponent<SudokuMenuController>();
        }

        private void Start()
        {
            var store = new PlayerPrefsStore();
            var saveService = new SudokuSaveService(store);
            var statsStore = new SudokuStatsStore(store);

            BuildUi(saveService, saveService.HasSave(), statsStore);
        }

        private void BuildUi(SudokuSaveService saveService, bool hasSave, SudokuStatsStore statsStore)
        {
            var canvas = UiFactory.CreateCanvas();

            var title = UiFactory.CreateText(canvas.transform, "Title", 48, TextAnchor.MiddleCenter);
            title.text = "Sudoku";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(400, 60));

            var statsText = UiFactory.CreateText(canvas.transform, "StatsText", 20, TextAnchor.MiddleCenter);
            statsText.text = BuildStatsText(statsStore);
            UiFactory.SetRect(statsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(440, 100));

            UiFactory.CreateButton(canvas.transform, "New Game", new Vector2(0, 20), new Vector2(220, 50), true, () =>
            {
                _difficultyPopup.SetActive(true);
            });

            UiFactory.CreateButton(canvas.transform, "Continue", new Vector2(0, -50), new Vector2(220, 50), hasSave, () =>
            {
                SudokuSessionIntent.ResumeFromSave = true;
                SudokuSessionIntent.EnterCustom = false;
                SceneManager.LoadScene("Sudoku");
            });

            UiFactory.CreateButton(canvas.transform, "Settings", new Vector2(0, -120), new Vector2(220, 50), true, () =>
            {
                SceneManager.LoadScene("SudokuSettings");
            });

            BuildDifficultyPopup(canvas.transform, saveService);
        }

        private void BuildDifficultyPopup(Transform parent, SudokuSaveService saveService)
        {
            _difficultyPopup = new GameObject("DifficultyPopup", typeof(Image));
            _difficultyPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_difficultyPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _difficultyPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_difficultyPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 340));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            var label = UiFactory.CreateText(panel.transform, "Label", 24, TextAnchor.MiddleCenter);
            label.text = "Choose Difficulty";
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(320, 40));

            for (var i = 0; i < Difficulties.Length; i++)
            {
                var difficulty = Difficulties[i];
                var row = i / 2;
                var col = i % 2;
                var x = col == 0 ? -85 : 85;
                var y = 30 - row * 65;
                UiFactory.CreateButton(panel.transform, difficulty.ToString(), new Vector2(x, y), new Vector2(150, 50), true, () =>
                {
                    saveService.ClearSave();
                    SudokuSessionIntent.Difficulty = difficulty;
                    SudokuSessionIntent.ResumeFromSave = false;
                    SudokuSessionIntent.EnterCustom = false;
                    SceneManager.LoadScene("Sudoku");
                });
            }

            UiFactory.CreateButton(panel.transform, "Custom", new Vector2(0, -100), new Vector2(150, 50), true, () =>
            {
                SudokuSessionIntent.ResumeFromSave = false;
                SudokuSessionIntent.EnterCustom = true;
                SceneManager.LoadScene("Sudoku");
            });

            UiFactory.CreateButton(panel.transform, "Cancel", new Vector2(0, -160), new Vector2(150, 40), true, () =>
            {
                _difficultyPopup.SetActive(false);
            });

            _difficultyPopup.SetActive(false);
        }

        private static string BuildStatsText(SudokuStatsStore statsStore)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Completed: {statsStore.GetTotalCompleted()}");
            foreach (var difficulty in Difficulties)
            {
                var best = statsStore.GetBestTimeSeconds(difficulty);
                lines.AppendLine($"{difficulty} best: {(best.HasValue ? FormatTime(best.Value) : "--:--")}");
            }
            return lines.ToString();
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
