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
        // Ads are a product decision to switch off for now, not remove - flip this back
        // to re-enable the rewarded hint and completion interstitial without touching
        // anything else below.
        private const bool AdsEnabled = false;

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
        private SudokuLeaderboardStore _leaderboardStore;
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
        private GameObject _successPopup;
        private Text _successTimeText;
        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            var store = new PlayerPrefsStore();
            _saveService = new SudokuSaveService(store);
            _leaderboardStore = new SudokuLeaderboardStore(store);
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
            if (AdsEnabled) StartCoroutine(InitializeMonetization());
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
                _audioSource.PlayOneShot(SudokuAudio.Tap);
            }
            else if (_activeNumber.HasValue)
            {
                if (_notesMode) _game.ToggleNote(pos, _activeNumber.Value);
                else _game.SetValue(pos, _activeNumber.Value);
                _verifyMistakes.Clear();
                _audioSource.PlayOneShot(SudokuAudio.Tap);
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
                _audioSource.PlayOneShot(SudokuAudio.Tap);
            }
            else if (_activeNumber.HasValue)
            {
                cell.Value = _activeNumber.Value;
                _editBoard.Set(pos, cell);
                _editError = null;
                _audioSource.PlayOneShot(SudokuAudio.Tap);
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
            if (_verifyMistakes.Count > 0) _audioSource.PlayOneShot(SudokuAudio.Error);
            Refresh();
        }

        private void WatchAdForHint()
        {
            if (!AdsEnabled || _adProvider == null) return;
            _adProvider.ShowRewarded(granted =>
            {
                if (!granted) return;
                _game.GrantExtraHint();
                Refresh();
            });
        }

        private void OnGameCompleted()
        {
            if (!AdsEnabled || _adProvider == null || _iapProvider == null) return;
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
            var times = _leaderboardStore.GetTimes(_difficulty);
            _timeText.text = $"Time: {FormatTime(_elapsedSeconds)}" + (times.Count > 0 ? $"   Best: {FormatTime(times[0])}" : "");
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
            _watchAdButton.gameObject.SetActive(AdsEnabled);

            if (_game.IsComplete)
            {
                if (!_wasComplete)
                {
                    // Autofill hands the player the answer - that's not a solve worth
                    // recording, and custom puzzles were never eligible either.
                    if (!_game.IsCustom && !_game.HasUsedAutofill)
                        _leaderboardStore.ReportCompletion(_difficulty, _elapsedSeconds);
                    OnGameCompleted();
                    ShowSuccessPopup();
                }
                _saveService.ClearSave();
            }
            else
            {
                _saveService.Save(_game, _difficulty, _elapsedSeconds);
            }
            _wasComplete = _game.IsComplete;

            if (AdsEnabled)
                UiFactory.SetInteractable(_watchAdButton, _game.HintsRemaining == 0 && _adProvider != null && _adProvider.IsRewardedReady && !_adsTestSettings.AdsDisabledForTesting);

            UpdateTimeText();
            _statusText.text = _game.IsComplete ? "Solved!" : $"Hints left: {_game.HintsRemaining}";
        }

        private void ShowSuccessPopup()
        {
            _successTimeText.text = _game.HasUsedAutofill
                ? $"Time: {FormatTime(_elapsedSeconds)} (autofilled - not recorded)"
                : $"Time: {FormatTime(_elapsedSeconds)}";
            _successPopup.SetActive(true);
            SudokuAudio.PlaySuccess(this, _audioSource);
        }

        private void PlayAgain()
        {
            _successPopup.SetActive(false);
            _mode = Mode.Play;
            _game = new SudokuGame(SudokuGenerator.Generate(_difficulty, new System.Random()));
            _elapsedSeconds = 0f;
            _wasComplete = false;
            _selected = null;
            _activeNumber = null;
            _activeErase = false;
            _verifyMistakes.Clear();
            Refresh();
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

            UiFactory.CreateBackButton(canvas.transform, ReturnToMenu);

            _statusText = UiFactory.CreateText(canvas.transform, "Status", 24, TextAnchor.UpperCenter);
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(440, 40));

            _timeText = UiFactory.CreateText(canvas.transform, "TimeText", 16, TextAnchor.UpperCenter);
            UiFactory.SetRect(_timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(440, 26));

            _clearEntriesButton = UiFactory.CreateButton(canvas.transform, "Clear", new Vector2(-110, 340), new Vector2(190, 44), true, ClearEntriesAction);
            _verifyButton = UiFactory.CreateButton(canvas.transform, "Verify", new Vector2(110, 340), new Vector2(190, 44), true, Verify);

            // Sized to run edge to edge with the number pad below it - from where
            // button "1" starts to where the Erase button ends (x = -352.5..+352.5).
            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(gridObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(705, 705));
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(76, 76);
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

            // A GridLayoutGroup treats every child as another cell to lay out - divider
            // lines can't live inside gridObject or they get absorbed as extra cells
            // (a visible 10th partial row). They get their own overlay, positioned
            // identically and rendered after the grid so it draws on top.
            var gridOverlay = new GameObject("GridOverlay", typeof(RectTransform));
            gridOverlay.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(gridOverlay.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -55), new Vector2(705, 705));
            AddBoxDividers(gridOverlay.transform);

            // Number pad: two rows of six/five so nothing falls outside the reference
            // canvas (a single nine-wide row plus every control below it used to run
            // off the bottom of the screen).
            for (var n = 1; n <= 5; n++)
            {
                var number = n;
                var x = -300 + (n - 1) * 120;
                _numberButtons[n] = UiFactory.CreateButton(canvas.transform, n.ToString(), new Vector2(x, -460), new Vector2(105, 50), true, () => SelectNumber(number));
            }
            _eraseButton = BuildEraseButton(canvas.transform, new Vector2(300, -460), new Vector2(105, 50));

            for (var n = 6; n <= 9; n++)
            {
                var number = n;
                var x = -240 + (n - 6) * 120;
                _numberButtons[n] = UiFactory.CreateButton(canvas.transform, n.ToString(), new Vector2(x, -522), new Vector2(105, 50), true, () => SelectNumber(number));
            }
            _notesToggleButton = BuildPencilButton(canvas.transform, new Vector2(240, -522), new Vector2(105, 50));

            _undoButton = UiFactory.CreateButton(canvas.transform, "Undo", new Vector2(-180, -584), new Vector2(150, 44), false, UndoMove);
            _hintButton = UiFactory.CreateButton(canvas.transform, "Hint", new Vector2(0, -584), new Vector2(150, 44), true, UseHint);
            UiFactory.CreateButton(canvas.transform, "Autofill", new Vector2(180, -584), new Vector2(150, 44), true, Autofill);

            _startButton = UiFactory.CreateButton(canvas.transform, "Start", new Vector2(-90, -646), new Vector2(150, 44), false, StartCustomGame);
            _clearEditorButton = UiFactory.CreateButton(canvas.transform, "Clear Grid", new Vector2(90, -646), new Vector2(150, 44), false, ClearEditor);
            _watchAdButton = UiFactory.CreateButton(canvas.transform, "Watch Ad +1 Hint", new Vector2(0, -646), new Vector2(220, 44), false, WatchAdForHint);

            BuildSuccessPopup(canvas.transform);
        }

        private Button BuildEraseButton(Transform parent, Vector2 position, Vector2 size)
        {
            var button = UiFactory.CreateButton(parent, "", position, size, true, SelectErase);
            button.GetComponentInChildren<Text>().text = "";

            // Plain (unrounded) rects: RoundedRectSprite bakes a fixed pixel corner
            // radius into a 32x32 texture, which looks blobby once 9-sliced onto a
            // shape this small/thin. A plain rect silhouette reads cleanly at icon size.
            AddPlainRect(button.transform, new Vector2(0, size.y * 0.06f), new Vector2(size.x * 0.3f, size.y * 0.4f), new Color(0.95f, 0.55f, 0.65f));
            AddPlainRect(button.transform, new Vector2(0, -size.y * 0.2f), new Vector2(size.x * 0.3f, size.y * 0.16f), Color.white);

            return button;
        }

        private Button BuildPencilButton(Transform parent, Vector2 position, Vector2 size)
        {
            var button = UiFactory.CreateButton(parent, "", position, size, true, ToggleNotesMode);
            button.GetComponentInChildren<Text>().text = "";

            // Distinct from both the active-tool gold and inactive grey button fills -
            // a straight yellow body was invisible against the gold "pressed" state.
            var body = AddPlainRect(button.transform, Vector2.zero, new Vector2(size.x * 0.12f, size.y * 0.5f), new Color(0.80f, 0.35f, 0.08f));
            body.transform.localRotation = Quaternion.Euler(0, 0, 40f);
            var tip = AddPlainRect(button.transform, new Vector2(size.x * 0.1f, -size.y * 0.19f), new Vector2(size.x * 0.12f, size.y * 0.09f), new Color(0.3f, 0.3f, 0.3f));
            tip.transform.localRotation = Quaternion.Euler(0, 0, 40f);

            return button;
        }

        private static Image AddPlainRect(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject("Icon", typeof(Image));
            obj.transform.SetParent(parent, false);
            UiFactory.SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddBoxDividers(Transform gridParent)
        {
            AddDividerLine(gridParent, new Vector2(-117, 0), new Vector2(4, 705));
            AddDividerLine(gridParent, new Vector2(117, 0), new Vector2(4, 705));
            AddDividerLine(gridParent, new Vector2(0, 117), new Vector2(705, 4));
            AddDividerLine(gridParent, new Vector2(0, -117), new Vector2(705, 4));
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

        private void BuildSuccessPopup(Transform parent)
        {
            _successPopup = new GameObject("SuccessPopup", typeof(Image));
            _successPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_successPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _successPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_successPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 300));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.96f, 0.94f, 0.90f);

            var label = UiFactory.CreateText(panel.transform, "Label", 30, TextAnchor.MiddleCenter);
            label.text = "Solved!";
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(320, 44));

            _successTimeText = UiFactory.CreateText(panel.transform, "SuccessTimeText", 18, TextAnchor.MiddleCenter);
            UiFactory.SetRect(_successTimeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 55), new Vector2(320, 60));

            UiFactory.CreateButton(panel.transform, "New Puzzle", new Vector2(0, -20), new Vector2(260, 50), true, PlayAgain);
            UiFactory.CreateButton(panel.transform, "Menu", new Vector2(0, -90), new Vector2(260, 50), true, ReturnToMenu);

            _successPopup.SetActive(false);
        }
    }
}
