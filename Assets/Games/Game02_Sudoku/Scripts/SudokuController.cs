using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MobileGamesFramework.Grid;
using MobileGamesFramework.Monetization;
using MobileGamesFramework.Persistence;
using MobileGamesFramework.UI;

namespace Game02_Sudoku
{
    public class SudokuController : MonoBehaviour
    {
        private const int BoardSize = 9;
        private const string GameId = "sudoku";
        private const int InterstitialCadence = 3;
        private const string RemoveAdsProductId = "remove_ads";

        private static readonly Color ActiveGradientTop = new Color(0.98f, 0.85f, 0.35f);
        private static readonly Color ActiveGradientBottom = new Color(0.90f, 0.66f, 0.10f);
        private static readonly Color InactiveGradientTop = new Color(0.80f, 0.80f, 0.80f);
        private static readonly Color InactiveGradientBottom = new Color(0.65f, 0.65f, 0.65f);

        private enum Mode { Play, Editor }

        private Mode _mode = Mode.Play;
        private SudokuGame _game;
        private SudokuSaveService _saveService;
        private SudokuStatsStore _statsStore;
        private IAdProvider _adProvider;
        private IIapProvider _iapProvider;
        private InterstitialCadenceTracker _cadenceTracker;
        private AdsTestSettings _adsTestSettings;
        private Difficulty _difficulty;
        private float _elapsedSeconds;
        private bool _wasComplete;
        private GridCore<SudokuCell> _editBoard;
        private string _editError;
        private Image[,] _cellImages;
        private Text[,] _cellTexts;
        private GridPosition? _selected;
        private bool _notesMode;
        private int? _activeNumber;
        private bool _activeErase;
        private readonly HashSet<GridPosition> _verifyMistakes = new HashSet<GridPosition>();

        private Button _undoButton;
        private Button _hintButton;
        private Button _notesToggleButton;
        private Button _eraseButton;
        private readonly Button[] _numberButtons = new Button[10];
        private Button _startButton;
        private Button _clearEditorButton;
        private Button _watchAdButton;
        private Button _clearEntriesButton;
        private Button _verifyButton;
        private Text _statusText;
        private Text _timeText;

        private void Start()
        {
            var store = new PlayerPrefsStore();
            _saveService = new SudokuSaveService(store);
            _statsStore = new SudokuStatsStore(store);
            _cadenceTracker = new InterstitialCadenceTracker(store);
            _adsTestSettings = new AdsTestSettings(store);

            BuildUi();

            if (SudokuSessionIntent.EnterCustom)
            {
                EnterEditor();
            }
            else
            {
                if (SudokuSessionIntent.ResumeFromSave && _saveService.TryLoad(out var loaded, out var difficulty, out var elapsed))
                {
                    _game = loaded;
                    _difficulty = difficulty;
                    _elapsedSeconds = elapsed;
                }
                else
                {
                    _difficulty = SudokuSessionIntent.Difficulty;
                    _game = new SudokuGame(SudokuGenerator.Generate(_difficulty, new System.Random()));
                    _elapsedSeconds = 0f;
                }

                Refresh();
            }

            // Ad/IAP SDK init can briefly stall the render thread on real devices (native
            // Play Services/Billing bootstrap); deferring it a frame ensures the built UI
            // is already on screen before that happens, instead of gating the first frame.
            StartCoroutine(InitializeMonetization());
        }

        private System.Collections.IEnumerator InitializeMonetization()
        {
            yield return null;
            _adProvider = new AdMobAdProvider();
            _iapProvider = new UnityIapProvider(new[] { RemoveAdsProductId });
            if (_mode == Mode.Play) Refresh();
        }

        private void Update()
        {
            if (_mode == Mode.Play && !_game.IsComplete)
            {
                _elapsedSeconds += Time.deltaTime;
                UpdateTimeText();
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ReturnToMenu();
        }

        private void SelectCell(GridPosition pos)
        {
            _selected = pos;

            if (_mode == Mode.Editor)
            {
                ApplyActiveToolToEditorCell(pos);
                return;
            }

            var cell = _game.Board.Get(pos).Value;
            if (cell.IsGiven)
            {
                Refresh();
                return;
            }

            if (_activeErase)
            {
                _game.Erase(pos);
                _verifyMistakes.Clear();
            }
            else if (_activeNumber.HasValue)
            {
                if (_notesMode) _game.ToggleNote(pos, _activeNumber.Value);
                else _game.SetValue(pos, _activeNumber.Value);
                _verifyMistakes.Clear();
            }

            Refresh();
        }

        private void ApplyActiveToolToEditorCell(GridPosition pos)
        {
            var cell = _editBoard.Get(pos).Value;
            if (_activeErase)
            {
                cell.Value = 0;
                _editBoard.Set(pos, cell);
                _editError = null;
            }
            else if (_activeNumber.HasValue)
            {
                cell.Value = _activeNumber.Value;
                _editBoard.Set(pos, cell);
                _editError = null;
            }

            Refresh();
        }

        private void SelectNumber(int number)
        {
            _activeNumber = _activeNumber == number ? (int?)null : number;
            _activeErase = false;
            Refresh();
        }

        private void SelectErase()
        {
            _activeErase = !_activeErase;
            _activeNumber = null;
            Refresh();
        }

        private void UndoMove()
        {
            if (_mode != Mode.Play) return;
            if (_game.Undo())
            {
                _verifyMistakes.Clear();
                Refresh();
            }
        }

        private void ClearEntriesAction()
        {
            if (_mode != Mode.Play) return;
            _game.ClearEntries();
            _verifyMistakes.Clear();
            Refresh();
        }

        private void ReturnToMenu()
        {
            SceneManager.LoadScene("SudokuMenu");
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
            if (_mode != Mode.Play) return;
            _verifyMistakes.Clear();
            foreach (var pos in _game.FindIncorrectEntries()) _verifyMistakes.Add(pos);
            Refresh();
        }

        private void WatchAdForHint()
        {
            if (_adProvider == null) return;
            _adProvider.ShowRewarded(granted =>
            {
                if (!granted) return;
                _game.GrantExtraHint();
                Refresh();
            });
        }

        private void OnGameCompleted()
        {
            if (_adProvider == null || _iapProvider == null) return;
            if (_adsTestSettings.AdsDisabledForTesting) return;
            if (_iapProvider.IsPurchased(RemoveAdsProductId)) return;
            if (_cadenceTracker.ShouldShowInterstitial(GameId, InterstitialCadence))
                _adProvider.ShowInterstitial();
        }

        private void EnterEditor()
        {
            _mode = Mode.Editor;
            _editBoard = SudokuBoardFactory.CreateEmpty();
            _editError = null;
            _selected = null;
            _activeNumber = null;
            _activeErase = false;
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
            _elapsedSeconds = 0f;
            _wasComplete = false;
            _selected = null;
            _activeNumber = null;
            _activeErase = false;
            _editError = null;
            Refresh();
        }

        private void Refresh()
        {
            if (_mode == Mode.Editor) RefreshEditor();
            else RefreshPlay();
        }

        private void UpdateTimeText()
        {
            var best = _statsStore.GetBestTimeSeconds(_difficulty);
            _timeText.text = $"Time: {FormatTime(_elapsedSeconds)}" + (best.HasValue ? $"   Best: {FormatTime(best.Value)}" : "");
        }

        private void RefreshPlay()
        {
            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var pos = new GridPosition(row, col);
                var cell = _game.Board.Get(pos).Value;

                _cellTexts[row, col].text = cell.Value != 0 ? cell.Value.ToString() : NotesGridText(cell.NotesMask);
                _cellTexts[row, col].fontSize = cell.Value != 0 ? 26 : 9;
                _cellTexts[row, col].fontStyle = cell.Value != 0 && !cell.IsGiven ? FontStyle.Bold : FontStyle.Normal;

                Color color;
                if (_verifyMistakes.Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else if (cell.IsGiven) color = new Color(0.85f, 0.85f, 0.85f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            UiFactory.SetInteractable(_undoButton, _game.CanUndo);
            UiFactory.SetInteractable(_hintButton, _game.HintsRemaining > 0);
            RefreshToolButtonVisuals();

            _startButton.gameObject.SetActive(false);
            _clearEditorButton.gameObject.SetActive(false);
            _watchAdButton.gameObject.SetActive(true);

            if (_game.IsComplete)
            {
                if (!_wasComplete)
                {
                    if (!_game.IsCustom) _statsStore.ReportCompletion(_difficulty, _elapsedSeconds);
                    OnGameCompleted();
                }
                _saveService.ClearSave();
            }
            else
            {
                _saveService.Save(_game, _difficulty, _elapsedSeconds);
            }
            _wasComplete = _game.IsComplete;

            UiFactory.SetInteractable(_watchAdButton, _game.HintsRemaining == 0 && _adProvider != null && _adProvider.IsRewardedReady && !_adsTestSettings.AdsDisabledForTesting);

            UpdateTimeText();
            _statusText.text = _game.IsComplete ? "Solved!" : $"Hints left: {_game.HintsRemaining}";
        }

        private void RefreshEditor()
        {
            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var pos = new GridPosition(row, col);
                var cell = _editBoard.Get(pos).Value;

                _cellTexts[row, col].text = cell.Value != 0 ? cell.Value.ToString() : "";
                _cellTexts[row, col].fontSize = 26;
                _cellTexts[row, col].fontStyle = FontStyle.Normal;

                Color color;
                if (SudokuSolver.FindConflicts(_editBoard).Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            UiFactory.SetInteractable(_undoButton, false);
            UiFactory.SetInteractable(_hintButton, false);
            RefreshToolButtonVisuals();

            _startButton.gameObject.SetActive(true);
            _clearEditorButton.gameObject.SetActive(true);
            _watchAdButton.gameObject.SetActive(false);
            UiFactory.SetInteractable(_startButton, true);
            UiFactory.SetInteractable(_clearEditorButton, true);

            _timeText.text = "";
            _statusText.text = _editError ?? "Building custom puzzle — pick a number, then tap cells to fill.";
        }

        private void RefreshToolButtonVisuals()
        {
            for (var n = 1; n <= 9; n++)
                SetToolButtonPressed(_numberButtons[n], _activeNumber == n);
            SetToolButtonPressed(_eraseButton, _activeErase);
            SetToolButtonPressed(_notesToggleButton, _notesMode);
        }

        private static void SetToolButtonPressed(Button button, bool pressed)
        {
            var image = button.GetComponent<Image>();
            image.sprite = pressed
                ? RoundedRectSprite.GetGradient(ActiveGradientTop, ActiveGradientBottom)
                : RoundedRectSprite.GetGradient(InactiveGradientTop, InactiveGradientBottom);
        }

        private static string NotesGridText(int mask)
        {
            var sb = new StringBuilder();
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var n = row * 3 + col + 1;
                    sb.Append((mask & (1 << (n - 1))) != 0 ? n.ToString() : " ");
                    if (col < 2) sb.Append(' ');
                }
                if (row < 2) sb.Append('\n');
            }
            return sb.ToString();
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

            _statusText = UiFactory.CreateText(canvas.transform, "Status", 24, TextAnchor.UpperCenter);
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(440, 40));

            _timeText = UiFactory.CreateText(canvas.transform, "TimeText", 16, TextAnchor.UpperCenter);
            UiFactory.SetRect(_timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(440, 26));

            _clearEntriesButton = UiFactory.CreateButton(canvas.transform, "Clear", new Vector2(-110, 340), new Vector2(190, 44), true, ClearEntriesAction);
            _verifyButton = UiFactory.CreateButton(canvas.transform, "Verify", new Vector2(110, 340), new Vector2(190, 44), true, Verify);

            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(gridObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 115), new Vector2(380, 380));
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(40, 40);
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

            AddBoxDividers(gridObject.transform);

            // Number pad: two rows of six/five so nothing falls outside the reference
            // canvas (a single nine-wide row plus every control below it used to run
            // off the bottom of the screen).
            for (var n = 1; n <= 5; n++)
            {
                var number = n;
                var x = -300 + (n - 1) * 120;
                _numberButtons[n] = UiFactory.CreateButton(canvas.transform, n.ToString(), new Vector2(x, -130), new Vector2(105, 50), true, () => SelectNumber(number));
            }
            _eraseButton = BuildEraseButton(canvas.transform, new Vector2(300, -130), new Vector2(105, 50));

            for (var n = 6; n <= 9; n++)
            {
                var number = n;
                var x = -240 + (n - 6) * 120;
                _numberButtons[n] = UiFactory.CreateButton(canvas.transform, n.ToString(), new Vector2(x, -195), new Vector2(105, 50), true, () => SelectNumber(number));
            }
            _notesToggleButton = BuildPencilButton(canvas.transform, new Vector2(240, -195), new Vector2(105, 50));

            _undoButton = UiFactory.CreateButton(canvas.transform, "Undo", new Vector2(-180, -260), new Vector2(150, 44), false, UndoMove);
            _hintButton = UiFactory.CreateButton(canvas.transform, "Hint", new Vector2(0, -260), new Vector2(150, 44), true, UseHint);
            UiFactory.CreateButton(canvas.transform, "Autofill", new Vector2(180, -260), new Vector2(150, 44), true, Autofill);

            _startButton = UiFactory.CreateButton(canvas.transform, "Start", new Vector2(-90, -325), new Vector2(150, 44), false, StartCustomGame);
            _clearEditorButton = UiFactory.CreateButton(canvas.transform, "Clear Grid", new Vector2(90, -325), new Vector2(150, 44), false, ClearEditor);
            _watchAdButton = UiFactory.CreateButton(canvas.transform, "Watch Ad +1 Hint", new Vector2(0, -325), new Vector2(220, 44), false, WatchAdForHint);

            UiFactory.CreateButton(canvas.transform, "Menu", new Vector2(0, -390), new Vector2(220, 44), true, ReturnToMenu);
        }

        private Button BuildEraseButton(Transform parent, Vector2 position, Vector2 size)
        {
            var button = UiFactory.CreateButton(parent, "", position, size, true, SelectErase);
            button.GetComponentInChildren<Text>().text = "";

            AddIconRect(button.transform, Vector2.zero, new Vector2(size.x * 0.55f, size.y * 0.55f), new Color(0.95f, 0.55f, 0.65f));
            AddIconRect(button.transform, new Vector2(0, -size.y * 0.16f), new Vector2(size.x * 0.55f, size.y * 0.18f), Color.white);

            return button;
        }

        private Button BuildPencilButton(Transform parent, Vector2 position, Vector2 size)
        {
            var button = UiFactory.CreateButton(parent, "", position, size, true, ToggleNotesMode);
            button.GetComponentInChildren<Text>().text = "";

            var body = AddIconRect(button.transform, Vector2.zero, new Vector2(size.x * 0.16f, size.y * 0.62f), new Color(0.95f, 0.76f, 0.25f));
            body.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            var tip = AddIconRect(button.transform, new Vector2(size.x * 0.13f, -size.y * 0.13f), new Vector2(size.x * 0.16f, size.y * 0.14f), new Color(0.3f, 0.3f, 0.3f));
            tip.transform.localRotation = Quaternion.Euler(0, 0, 45f);

            return button;
        }

        private static Image AddIconRect(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject("Icon", typeof(Image));
            obj.transform.SetParent(parent, false);
            UiFactory.SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var image = obj.GetComponent<Image>();
            image.sprite = RoundedRectSprite.Get();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddBoxDividers(Transform gridParent)
        {
            AddDividerLine(gridParent, new Vector2(-62, 0), new Vector2(3, 380));
            AddDividerLine(gridParent, new Vector2(64, 0), new Vector2(3, 380));
            AddDividerLine(gridParent, new Vector2(0, 64), new Vector2(380, 3));
            AddDividerLine(gridParent, new Vector2(0, -62), new Vector2(380, 3));
        }

        private static void AddDividerLine(Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject("BoxDivider", typeof(Image));
            obj.transform.SetParent(parent, false);
            UiFactory.SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var image = obj.GetComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.32f, 0.85f);
            image.raycastTarget = false;
        }
    }
}
