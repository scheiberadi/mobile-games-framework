using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MobileGamesFramework.Grid;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuController : MonoBehaviour
    {
        private const int BoardSize = 9;

        private enum Mode { Play, Editor }

        private Mode _mode = Mode.Play;
        private SudokuGame _game;
        private GridCore<SudokuCell> _editBoard;
        private string _editError;
        private Image[,] _cellImages;
        private Text[,] _cellTexts;
        private GridPosition? _selected;
        private bool _notesMode;

        private Button _undoButton;
        private Button _hintButton;
        private Button _notesToggleButton;
        private Button _customButton;
        private Button _startButton;
        private Button _clearButton;
        private Text _statusText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Sudoku") return;
            new GameObject("SudokuController").AddComponent<SudokuController>();
        }

        private void Start()
        {
            _game = new SudokuGame(SudokuGenerator.Generate(Difficulty.Medium, new System.Random()));
            BuildUi();
            Refresh();
        }

        private void SelectCell(GridPosition pos)
        {
            if (_mode == Mode.Editor)
            {
                _selected = pos;
                Refresh();
                return;
            }

            var cell = _game.Board.Get(pos).Value;
            if (cell.IsGiven) return;
            _selected = pos;
            Refresh();
        }

        private void EnterNumber(int number)
        {
            if (_selected == null) return;

            if (_mode == Mode.Editor)
            {
                var cell = _editBoard.Get(_selected.Value).Value;
                cell.Value = number;
                _editBoard.Set(_selected.Value, cell);
                _editError = null;
                Refresh();
                return;
            }

            if (_notesMode) _game.ToggleNote(_selected.Value, number);
            else _game.SetValue(_selected.Value, number);
            Refresh();
        }

        private void Erase()
        {
            if (_selected == null) return;

            if (_mode == Mode.Editor)
            {
                var cell = _editBoard.Get(_selected.Value).Value;
                cell.Value = 0;
                _editBoard.Set(_selected.Value, cell);
                _editError = null;
                Refresh();
                return;
            }

            if (_game.Erase(_selected.Value))
                Refresh();
        }

        private void UndoMove()
        {
            if (_mode != Mode.Play) return;
            if (_game.Undo()) Refresh();
        }

        private void Restart()
        {
            _mode = Mode.Play;
            _game = new SudokuGame(SudokuGenerator.Generate(Difficulty.Medium, new System.Random()));
            _selected = null;
            Refresh();
        }

        private void UseHint()
        {
            if (_mode != Mode.Play) return;
            if (_game.FillHint(new System.Random())) Refresh();
        }

        private void Autofill()
        {
            if (_mode != Mode.Play) return;
            _game.AutofillRemaining();
            Refresh();
        }

        private void ToggleNotesMode()
        {
            if (_mode != Mode.Play) return;
            _notesMode = !_notesMode;
            Refresh();
        }

        private void Verify()
        {
            Refresh();
        }

        private void EnterEditor()
        {
            _mode = Mode.Editor;
            _editBoard = SudokuBoardFactory.CreateEmpty();
            _editError = null;
            _selected = null;
            Refresh();
        }

        private void ClearEditor()
        {
            if (_mode != Mode.Editor) return;
            _editBoard = SudokuBoardFactory.CreateEmpty();
            _editError = null;
            Refresh();
        }

        private void StartCustomGame()
        {
            if (_mode != Mode.Editor) return;

            if (!SudokuCustomPuzzle.TryBuild(_editBoard, out var puzzle, out var error))
            {
                _editError = error;
                Refresh();
                return;
            }

            _game = new SudokuGame(puzzle) { IsCustom = true };
            _mode = Mode.Play;
            _selected = null;
            _editError = null;
            Refresh();
        }

        private void Refresh()
        {
            if (_mode == Mode.Editor) RefreshEditor();
            else RefreshPlay();
        }

        private void RefreshPlay()
        {
            var conflicts = _game.Conflicts;

            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var pos = new GridPosition(row, col);
                var cell = _game.Board.Get(pos).Value;

                _cellTexts[row, col].text = cell.Value != 0 ? cell.Value.ToString() : NotesText(cell.NotesMask);
                _cellTexts[row, col].fontSize = cell.Value != 0 ? 26 : 11;

                Color color;
                if (conflicts.Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else if (cell.IsGiven) color = new Color(0.85f, 0.85f, 0.85f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            _undoButton.interactable = _game.CanUndo;
            _hintButton.interactable = _game.HintsRemaining > 0;
            _notesToggleButton.GetComponentInChildren<Text>().text = _notesMode ? "Notes: On" : "Notes: Off";
            _customButton.interactable = true;
            _startButton.interactable = false;
            _clearButton.interactable = false;
            _statusText.text = _game.IsComplete ? "Solved!" : $"Hints left: {_game.HintsRemaining}";
        }

        private void RefreshEditor()
        {
            var conflicts = SudokuSolver.FindConflicts(_editBoard);

            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var pos = new GridPosition(row, col);
                var cell = _editBoard.Get(pos).Value;

                _cellTexts[row, col].text = cell.Value != 0 ? cell.Value.ToString() : "";
                _cellTexts[row, col].fontSize = 26;

                Color color;
                if (conflicts.Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            _undoButton.interactable = false;
            _hintButton.interactable = false;
            _customButton.interactable = false;
            _startButton.interactable = true;
            _clearButton.interactable = true;
            _statusText.text = _editError ?? "Building custom puzzle — enter numbers, then Start.";
        }

        private static string NotesText(int mask)
        {
            if (mask == 0) return "";
            return string.Concat(Enumerable.Range(1, 9).Select(n => (mask & (1 << (n - 1))) != 0 ? n.ToString() : " "));
        }

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas();

            _statusText = UiFactory.CreateText(canvas.transform, "Status", 26, TextAnchor.UpperCenter);
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(400, 40));

            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(gridObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(450, 450));
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(48, 48);
            layout.spacing = new Vector2(2, 2);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = BoardSize;

            _cellImages = new Image[BoardSize, BoardSize];
            _cellTexts = new Text[BoardSize, BoardSize];

            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var r = row;
                var c = col;
                var cellObject = new GameObject($"Cell_{row}_{col}", typeof(Image), typeof(Button));
                cellObject.transform.SetParent(gridObject.transform, false);

                _cellImages[row, col] = cellObject.GetComponent<Image>();
                cellObject.GetComponent<Button>().onClick.AddListener(() => SelectCell(new GridPosition(r, c)));

                var text = UiFactory.CreateText(cellObject.transform, "Label", 26, TextAnchor.MiddleCenter);
                UiFactory.SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _cellTexts[row, col] = text;
            }

            for (var n = 1; n <= 9; n++)
            {
                var number = n;
                var x = -320 + (n - 1) * 80;
                UiFactory.CreateButton(canvas.transform, n.ToString(), new Vector2(x, -430), new Vector2(70, 60), true, () => EnterNumber(number));
            }

            _undoButton = UiFactory.CreateButton(canvas.transform, "Undo", new Vector2(-260, -500), new Vector2(140, 50), false, UndoMove);
            UiFactory.CreateButton(canvas.transform, "Erase", new Vector2(-100, -500), new Vector2(140, 50), true, Erase);
            _notesToggleButton = UiFactory.CreateButton(canvas.transform, "Notes: Off", new Vector2(60, -500), new Vector2(140, 50), true, ToggleNotesMode);
            UiFactory.CreateButton(canvas.transform, "Restart", new Vector2(220, -500), new Vector2(140, 50), true, Restart);

            _hintButton = UiFactory.CreateButton(canvas.transform, "Hint", new Vector2(-160, -560), new Vector2(140, 50), true, UseHint);
            UiFactory.CreateButton(canvas.transform, "Verify", new Vector2(0, -560), new Vector2(140, 50), true, Verify);
            UiFactory.CreateButton(canvas.transform, "Autofill", new Vector2(160, -560), new Vector2(140, 50), true, Autofill);

            _customButton = UiFactory.CreateButton(canvas.transform, "Custom", new Vector2(-160, -620), new Vector2(140, 50), true, EnterEditor);
            _startButton = UiFactory.CreateButton(canvas.transform, "Start", new Vector2(0, -620), new Vector2(140, 50), false, StartCustomGame);
            _clearButton = UiFactory.CreateButton(canvas.transform, "Clear", new Vector2(160, -620), new Vector2(140, 50), false, ClearEditor);
        }
    }
}
