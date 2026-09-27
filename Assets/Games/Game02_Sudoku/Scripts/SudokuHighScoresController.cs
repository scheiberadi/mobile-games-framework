using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
        private GameObject _columnsRoot;
        private Text _rankText;
        private Text _timeText;
        private Text _dateText;
        private Text _completedText;

        private void Start()
        {
            _leaderboardStore = new SudokuLeaderboardStore(new PlayerPrefsStore());
            BuildUi();
            RefreshList();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SceneManager.LoadScene("SudokuMenu");
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
                    ? RoundedRectSprite.GetGradient(SudokuUi.ActiveTop, SudokuUi.ActiveBottom)
                    : RoundedRectSprite.GetGradient(new Color(0.80f, 0.80f, 0.80f), new Color(0.65f, 0.65f, 0.65f));
            }

            var entries = _leaderboardStore.GetEntries(_selectedDifficulty);
            var hasEntries = entries.Count > 0;
            _listText.text = hasEntries ? "" : Loc.Get("highscores.noTimes");
            _columnsRoot.SetActive(hasEntries);
            if (hasEntries)
            {
                var ranks = new StringBuilder();
                var times = new StringBuilder();
                var dates = new StringBuilder();
                for (var i = 0; i < entries.Count; i++)
                {
                    ranks.Append(i + 1).Append('.').Append('\n');
                    times.Append(FormatTime(entries[i].Seconds)).Append('\n');
                    dates.Append(FormatDate(entries[i].CompletedAt)).Append('\n');
                }
                _rankText.text = ranks.ToString();
                _timeText.text = times.ToString();
                _dateText.text = dates.ToString();
            }

            _completedText.text = Loc.Get("highscores.completed", _leaderboardStore.GetCompletedCount(_selectedDifficulty));
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        // Times recorded before completion dates existed have no real date (see
        // SudokuLeaderboardStore.GetEntries) - omit the suffix entirely for those
        // rather than printing a meaningless "01/01/01" date.
        private static string FormatDate(System.DateTime completedAt) =>
            completedAt == System.DateTime.MinValue ? "" : $"{completedAt:MM/dd/yy HH:mm}";

        private Text CreateColumnHeader(string name, string label, float x, float width)
        {
            var header = UiFactory.CreateText(_columnsRoot.transform, name, 20, TextAnchor.UpperCenter);
            header.color = SudokuTheme.Palette.TextColor;
            header.fontStyle = FontStyle.Bold;
            header.text = label;
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -25), new Vector2(width, 30));
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            return header;
        }

        private Text CreateColumn(string name, float x, float width)
        {
            var column = UiFactory.CreateText(_columnsRoot.transform, name, 20, TextAnchor.UpperCenter);
            column.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(column.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -65), new Vector2(width, 610));
            column.rectTransform.pivot = new Vector2(0.5f, 1f);
            return column;
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();
            UiFactory.CreateBackground(canvas.transform, SudokuTheme.Palette.BackgroundTop, SudokuTheme.Palette.BackgroundBottom);

            SudokuUi.CreateBackButton(canvas.transform, () =>
            {
                SceneManager.LoadScene("SudokuMenu");
            }, Loc.Get("common.back"));

            var title = UiFactory.CreateText(canvas.transform, "Title", 36, TextAnchor.MiddleCenter);
            title.color = SudokuTheme.Palette.TextColor;
            title.text = Loc.Get("highscores.title");
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 400), new Vector2(400, 50));

            for (var i = 0; i < Difficulties.Length; i++)
            {
                var difficulty = Difficulties[i];
                var x = -165 + i * 110;
                _difficultyButtons[i] = SudokuUi.CreateButton(canvas.transform, Loc.Difficulty(difficulty), new Vector2(x, 330), new Vector2(100, 46), true, () => SelectDifficulty(difficulty));
            }

            _completedText = UiFactory.CreateText(canvas.transform, "CompletedText", 18, TextAnchor.MiddleCenter);
            _completedText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_completedText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 275), new Vector2(320, 26));

            // Near-full-screen list, matching the Sudoku grid's own 705-unit width, so the
            // leaderboard reads as the main content of this screen instead of a small box.
            var listPanel = new GameObject("ListPanel", typeof(Image));
            listPanel.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(listPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(705, 690));
            var listPanelImage = listPanel.GetComponent<Image>();
            listPanelImage.sprite = RoundedRectSprite.Get();
            listPanelImage.type = Image.Type.Sliced;
            listPanelImage.color = SudokuTheme.Palette.PanelColor;

            _listText = UiFactory.CreateText(listPanel.transform, "ListText", 20, TextAnchor.UpperCenter);
            _listText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_listText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(665, 650));
            // Pivot defaults to center, so without this the anchored position places the
            // BOX'S CENTER (not its top) 25 units below the panel's top edge - with a
            // 650-tall box that pushes its top edge up past the panel and into the header.
            // Pivoting to the box's own top edge makes the offset measure from there instead.
            _listText.rectTransform.pivot = new Vector2(0.5f, 1f);

            // Rank / Time / Date as separate columns under a header row, so the completion
            // date can't be mistaken for the solve time. Only shown when there are entries.
            _columnsRoot = new GameObject("Columns", typeof(RectTransform));
            _columnsRoot.transform.SetParent(listPanel.transform, false);
            UiFactory.SetRect(_columnsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var columnsRect = _columnsRoot.GetComponent<RectTransform>();
            columnsRect.offsetMin = Vector2.zero;
            columnsRect.offsetMax = Vector2.zero;

            const float rankX = -250f, timeX = -90f, dateX = 170f;
            CreateColumnHeader("HeaderRank", "#", rankX, 70);
            CreateColumnHeader("HeaderTime", Loc.Get("highscores.time"), timeX, 150);
            CreateColumnHeader("HeaderDate", Loc.Get("highscores.date"), dateX, 300);
            _rankText = CreateColumn("RankColumn", rankX, 70);
            _timeText = CreateColumn("TimeColumn", timeX, 150);
            _dateText = CreateColumn("DateColumn", dateX, 300);

            SudokuUi.CreateButton(canvas.transform, Loc.Get("highscores.clearLeaderboard"), new Vector2(0, -480), new Vector2(280, 46), true, ClearLeaderboard);
        }
    }
}
