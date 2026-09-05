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

            var entries = _leaderboardStore.GetEntries(_selectedDifficulty);
            if (entries.Count == 0)
            {
                _listText.text = "No times recorded yet.";
            }
            else
            {
                var sb = new StringBuilder();
                for (var i = 0; i < entries.Count; i++)
                    sb.AppendLine($"{i + 1}.  {FormatTime(entries[i].Seconds)}{FormatDateSuffix(entries[i].CompletedAt)}");
                _listText.text = sb.ToString();
            }

            _completedText.text = $"Completed: {_leaderboardStore.GetCompletedCount(_selectedDifficulty)}";
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        // Times recorded before completion dates existed have no real date (see
        // SudokuLeaderboardStore.GetEntries) - omit the suffix entirely for those
        // rather than printing a meaningless "01/01/01" date.
        private static string FormatDateSuffix(System.DateTime completedAt) =>
            completedAt == System.DateTime.MinValue ? "" : $"   {completedAt:MM/dd/yy HH:mm}";

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, new Color(0.75f, 0.85f, 0.97f), new Color(0.98f, 0.98f, 1f));

            UiFactory.CreateBackButton(canvas.transform, () =>
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
            UiFactory.SetRect(_completedText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 275), new Vector2(320, 26));

            // Near-full-screen list, matching the Sudoku grid's own 705-unit width, so the
            // leaderboard reads as the main content of this screen instead of a small box.
            var listPanel = new GameObject("ListPanel", typeof(Image));
            listPanel.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(listPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(705, 690));
            var listPanelImage = listPanel.GetComponent<Image>();
            listPanelImage.sprite = RoundedRectSprite.Get();
            listPanelImage.type = Image.Type.Sliced;
            listPanelImage.color = new Color(0.98f, 0.97f, 0.94f);

            _listText = UiFactory.CreateText(listPanel.transform, "ListText", 20, TextAnchor.UpperCenter);
            UiFactory.SetRect(_listText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(665, 650));
            // Pivot defaults to center, so without this the anchored position places the
            // BOX'S CENTER (not its top) 25 units below the panel's top edge - with a
            // 650-tall box that pushes its top edge up past the panel and into the header.
            // Pivoting to the box's own top edge makes the offset measure from there instead.
            _listText.rectTransform.pivot = new Vector2(0.5f, 1f);

            UiFactory.CreateButton(canvas.transform, "Clear Leaderboard", new Vector2(0, -480), new Vector2(280, 46), true, ClearLeaderboard);
        }
    }
}
