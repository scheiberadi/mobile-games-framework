using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuHighScoresController : MonoBehaviour
    {
        private static readonly Difficulty[] Difficulties =
        {
            Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Expert
        };

        private SudokuLeaderboardStore _leaderboardStore;
        private Difficulty _selectedDifficulty = Difficulty.Easy;
        private readonly Button[] _difficultyButtons = new Button[Difficulties.Length];
        private Text _listText;
        private Text _completedText;

        private void Start()
        {
            _leaderboardStore = new SudokuLeaderboardStore(new PlayerPrefsStore());
            BuildUi();
            RefreshList();
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            RefreshList();
        }

        private void ClearLeaderboard()
        {
            _leaderboardStore.ClearTimes(_selectedDifficulty);
            RefreshList();
        }

        private void RefreshList()
        {
            for (var i = 0; i < Difficulties.Length; i++)
            {
                var pressed = Difficulties[i] == _selectedDifficulty;
                var image = _difficultyButtons[i].GetComponent<Image>();
                image.sprite = pressed
                    ? RoundedRectSprite.GetGradient(new Color(0.98f, 0.85f, 0.35f), new Color(0.90f, 0.66f, 0.10f))
                    : RoundedRectSprite.GetGradient(new Color(0.80f, 0.80f, 0.80f), new Color(0.65f, 0.65f, 0.65f));
            }

            var times = _leaderboardStore.GetTimes(_selectedDifficulty);
            if (times.Count == 0)
            {
                _listText.text = "No times recorded yet.";
            }
            else
            {
                var sb = new StringBuilder();
                for (var i = 0; i < times.Count; i++)
                    sb.AppendLine($"{i + 1}.  {FormatTime(times[i])}");
                _listText.text = sb.ToString();
            }

            _completedText.text = $"Completed: {_leaderboardStore.GetCompletedCount(_selectedDifficulty)}";
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            UiFactory.CreateButton(canvas.transform, "Back", new Vector2(330, 400), new Vector2(110, 50), true, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            });

            var title = UiFactory.CreateText(canvas.transform, "Title", 36, TextAnchor.MiddleCenter);
            title.text = "High Scores";
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 400), new Vector2(400, 50));

            for (var i = 0; i < Difficulties.Length; i++)
            {
                var difficulty = Difficulties[i];
                var x = -165 + i * 110;
                _difficultyButtons[i] = UiFactory.CreateButton(canvas.transform, difficulty.ToString(), new Vector2(x, 330), new Vector2(100, 46), true, () => SelectDifficulty(difficulty));
            }

            _completedText = UiFactory.CreateText(canvas.transform, "CompletedText", 18, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_completedText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 270), new Vector2(320, 30));

            var listPanel = new GameObject("ListPanel", typeof(Image));
            listPanel.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(listPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(360, 500));
            var listPanelImage = listPanel.GetComponent<Image>();
            listPanelImage.sprite = RoundedRectSprite.Get();
            listPanelImage.type = Image.Type.Sliced;
            listPanelImage.color = new Color(0.98f, 0.97f, 0.94f);

            _listText = UiFactory.CreateText(listPanel.transform, "ListText", 18, TextAnchor.UpperCenter);
            UiFactory.SetRect(_listText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(320, 460));

            UiFactory.CreateButton(canvas.transform, "Clear Leaderboard", new Vector2(0, -350), new Vector2(280, 46), true, ClearLeaderboard);
        }
    }
}
