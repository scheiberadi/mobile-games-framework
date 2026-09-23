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
        // Ads are a product decision to switch off for now. AdMobAdProvider itself was
        // removed (not just this flag) because Sudoku's own reference to GoogleMobileAds.Api
        // - even dead behind this flag - still linked the GoogleMobileAds.* managed
        // assemblies into Sudoku's compiled binary; Google Play's automated data-safety scan
        // flagged that presence directly ("Device Or Other IDs") across three consecutive
        // releases, independent of the AD_ID/BILLING permissions and AdMob app-id manifest
        // metadata already being excluded (see SudokuNoAdsMainTemplate.gradle.txt and
        // SudokuStripAdsManifest.cs, which handle the Java-level and manifest-level parts of
        // the same problem - none of those reached this C#-level reference). To re-enable
        // ads: restore Assets/Games/Game02_Sudoku/Scripts/AdMobAdProvider.cs and the
        // InitializeMonetization() coroutine from git history (both removed in the same
        // commit as this comment) and flip this back to true.
        private const bool AdsEnabled = false;

        private const int BoardSize = 9;
        private const string GameId = "sudoku";
        private const int InterstitialCadence = 3;

        private static readonly Color InactiveGradientTop = new Color(0.80f, 0.80f, 0.80f);
        private static readonly Color InactiveGradientBottom = new Color(0.65f, 0.65f, 0.65f);

        private enum Mode { Play, Editor }

        private Mode _mode = Mode.Play;
        private SudokuGame _game;
        private SudokuSaveService _saveService;
        private SudokuLeaderboardStore _leaderboardStore;
        private IAdProvider _adProvider;
        private InterstitialCadenceTracker _cadenceTracker;
        private AdsTestSettings _adsTestSettings;
        private SudokuAudioSettings _audioSettings;
        private Difficulty _difficulty;
        private float _elapsedSeconds;
        private bool _wasComplete;
        private GridCore<SudokuCell> _editBoard;
        private SudokuCustomPuzzleError? _editError;
        private Image[,] _cellImages;
        private Text[,] _cellTexts;
        private GridPosition? _selected;
        private bool _notesMode;
        private int? _activeNumber;
        private bool _activeErase;
        private readonly HashSet<GridPosition> _verifyMistakes = new HashSet<GridPosition>();

        private Button _undoButton;
        private Button _hintButton;
        private Button _autofillButton;
        private Button _notesToggleButton;
        private Button _eraseButton;
        private readonly Button[] _numberButtons = new Button[10];
        private Button _startButton;
        private Button _clearEditorButton;
        private Button _generateButton;
        private GameObject _generateDifficultyPopup;
        private Button _watchAdButton;
        private Button _clearEntriesButton;
        private Button _verifyButton;
        private Text _difficultyText;
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
            _audioSettings = new SudokuAudioSettings(store);

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

            // AdsEnabled is always false (see the comment on its declaration) - the
            // provider-construction coroutine that used to run here was removed along with
            // AdMobAdProvider.cs itself.
        }

        private void Update()
        {
            if (_mode == Mode.Play && !_game.IsComplete)
            {
                _elapsedSeconds += Time.deltaTime;
                UpdateTimeText();
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Close whichever popup is on top first - a bare back-navigation
                // shouldn't jump straight to the menu out from under an open dialog.
                if (_successPopup.activeSelf) _successPopup.SetActive(false);
                else if (_generateDifficultyPopup.activeSelf) _generateDifficultyPopup.SetActive(false);
                else ReturnToMenu();
            }
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
                PlaySfx(SudokuAudio.Tap);
            }
            else if (_activeNumber.HasValue)
            {
                if (_notesMode) _game.ToggleNote(pos, _activeNumber.Value);
                else _game.SetValue(pos, _activeNumber.Value);
                _verifyMistakes.Clear();
                PlaySfx(SudokuAudio.Tap);
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
                PlaySfx(SudokuAudio.Tap);
            }
            else if (_activeNumber.HasValue)
            {
                cell.Value = _activeNumber.Value;
                _editBoard.Set(pos, cell);
                _editError = null;
                PlaySfx(SudokuAudio.Tap);
            }

            Refresh();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (_audioSettings.SfxEnabled) _audioSource.PlayOneShot(clip);
        }

        private void SelectNumber(int number)
        {
            _activeNumber = _activeNumber == number ? (int?)null : number;
            _activeErase = false;
            Refresh();
        }

        private bool ShouldHighlightSameNumber(SudokuCell cell) =>
            _activeNumber.HasValue && cell.Value == _activeNumber.Value;

        private bool ShouldHighlightNote(SudokuCell cell) =>
            _activeNumber.HasValue && cell.Value == 0 && (cell.NotesMask & (1 << (_activeNumber.Value - 1))) != 0;

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
            if (_verifyMistakes.Count > 0) PlaySfx(SudokuAudio.Error);
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
            if (!AdsEnabled || _adProvider == null) return;
            if (_adsTestSettings.AdsDisabledForTesting) return;
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
            _timeText.text = times.Count > 0
                ? Loc.Get("play.timeWithBest", FormatTime(_elapsedSeconds), FormatTime(times[0]))
                : Loc.Get("play.time", FormatTime(_elapsedSeconds));
        }

        private void RefreshPlay()
        {
            for (var row = 0; row < BoardSize; row++)
            for (var col = 0; col < BoardSize; col++)
            {
                var pos = new GridPosition(row, col);
                var cell = _game.Board.Get(pos).Value;

                _cellTexts[row, col].text = cell.Value != 0 ? cell.Value.ToString() : NotesGridText(cell.NotesMask);
                _cellTexts[row, col].fontSize = cell.Value != 0 ? 33 : 20;
                _cellTexts[row, col].fontStyle = cell.Value != 0 && !cell.IsGiven ? FontStyle.Bold : FontStyle.Normal;

                Color color;
                if (_verifyMistakes.Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else if (ShouldHighlightSameNumber(cell)) color = new Color(1f, 0.95f, 0.70f);
                else if (ShouldHighlightNote(cell)) color = new Color(1f, 0.98f, 0.84f);
                else if (cell.IsGiven) color = new Color(0.85f, 0.85f, 0.85f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            UiFactory.SetButtonActive(_undoButton, true);
            UiFactory.SetButtonActive(_hintButton, true);
            UiFactory.SetButtonActive(_autofillButton, true);
            UiFactory.SetButtonActive(_notesToggleButton, true);
            UiFactory.SetButtonActive(_clearEntriesButton, true);
            UiFactory.SetButtonActive(_verifyButton, true);
            SudokuUi.SetInteractable(_undoButton, _game.CanUndo);
            SudokuUi.SetInteractable(_hintButton, _game.HintsRemaining > 0);

            var doneDigits = new bool[10];
            for (var n = 1; n <= 9; n++) doneDigits[n] = _game.CountPlaced(n) >= BoardSize;
            RefreshToolButtonVisuals(doneDigits);

            UiFactory.SetButtonActive(_startButton, false);
            UiFactory.SetButtonActive(_clearEditorButton, false);
            UiFactory.SetButtonActive(_generateButton, false);
            UiFactory.SetButtonActive(_watchAdButton, AdsEnabled);

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
                SudokuUi.SetInteractable(_watchAdButton, _game.HintsRemaining == 0 && _adProvider != null && _adProvider.IsRewardedReady && !_adsTestSettings.AdsDisabledForTesting);

            UpdateTimeText();
            _difficultyText.text = Loc.Difficulty(_difficulty);
            _statusText.text = _game.IsComplete ? Loc.Get("play.solved") : Loc.Get("play.hintsLeft", _game.HintsRemaining);
        }

        private void ShowSuccessPopup()
        {
            _successTimeText.text = _game.HasUsedAutofill
                ? Loc.Get("popup.successTimeAutofilled", FormatTime(_elapsedSeconds))
                : Loc.Get("popup.successTime", FormatTime(_elapsedSeconds));
            _successPopup.SetActive(true);
            if (_audioSettings.SfxEnabled) SudokuAudio.PlaySuccess(this, _audioSource);
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
                _cellTexts[row, col].fontSize = 33;
                _cellTexts[row, col].fontStyle = FontStyle.Normal;

                Color color;
                if (SudokuSolver.FindConflicts(_editBoard).Contains(pos)) color = new Color(0.95f, 0.45f, 0.45f);
                else if (_selected.HasValue && _selected.Value.Equals(pos)) color = new Color(0.78f, 0.85f, 1f);
                else color = Color.white;
                _cellImages[row, col].color = color;
            }

            RefreshToolButtonVisuals();

            UiFactory.SetButtonActive(_undoButton, false);
            UiFactory.SetButtonActive(_hintButton, false);
            UiFactory.SetButtonActive(_autofillButton, false);
            UiFactory.SetButtonActive(_notesToggleButton, false);
            UiFactory.SetButtonActive(_clearEntriesButton, false);
            UiFactory.SetButtonActive(_verifyButton, false);
            UiFactory.SetButtonActive(_watchAdButton, false);
            SudokuUi.SetInteractable(_startButton, true);
            SudokuUi.SetInteractable(_clearEditorButton, true);
            SudokuUi.SetInteractable(_generateButton, true);
            UiFactory.SetButtonActive(_startButton, true);
            UiFactory.SetButtonActive(_clearEditorButton, true);
            UiFactory.SetButtonActive(_generateButton, true);

            _difficultyText.text = "";
            _timeText.text = "";
            _statusText.text = _editError.HasValue ? EditorErrorMessage(_editError.Value) : Loc.Get("editor.buildingHint");
        }

        private static string EditorErrorMessage(SudokuCustomPuzzleError error)
        {
            switch (error)
            {
                case SudokuCustomPuzzleError.ConflictingNumbers: return Loc.Get("editor.errorConflict");
                case SudokuCustomPuzzleError.NoSolution: return Loc.Get("editor.errorNoSolution");
                case SudokuCustomPuzzleError.MultipleSolutions: return Loc.Get("editor.errorMultipleSolutions");
                default: return "";
            }
        }

        // doneDigits (Play mode only - Editor mode has no game to count against) marks
        // which digits already have all 9 instances placed on the board. The button
        // still needs to work if the player wants to erase/replace one of them, so this
        // only fades its look (CanvasGroup.alpha) rather than touching interactable -
        // UiFactory.SetInteractable/SetButtonActive both gate real interactivity, which
        // is the opposite of what's wanted here.
        private void RefreshToolButtonVisuals(bool[] doneDigits = null)
        {
            for (var n = 1; n <= 9; n++)
            {
                SetToolButtonPressed(_numberButtons[n], _activeNumber == n);
                SetNumberButtonDone(_numberButtons[n], doneDigits != null && doneDigits[n]);
            }
            SetToolButtonPressed(_eraseButton, _activeErase);
            SetToolButtonPressed(_notesToggleButton, _notesMode);
        }

        private static void SetToolButtonPressed(Button button, bool pressed)
        {
            var image = button.GetComponent<Image>();
            image.sprite = pressed
                ? RoundedRectSprite.GetGradient(SudokuUi.ActiveTop, SudokuUi.ActiveBottom)
                : RoundedRectSprite.GetGradient(InactiveGradientTop, InactiveGradientBottom);
        }

        private static void SetNumberButtonDone(Button button, bool done)
        {
            if (!button.TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = done ? 0.35f : 1f;
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
            UiFactory.CreateBackground(canvas.transform, SudokuTheme.Palette.BackgroundTop, SudokuTheme.Palette.BackgroundBottom);

            SudokuUi.CreateBackButton(canvas.transform, ReturnToMenu, Loc.Get("common.back"));

            _difficultyText = UiFactory.CreateText(canvas.transform, "DifficultyText", 28, TextAnchor.UpperCenter);
            _difficultyText.fontStyle = FontStyle.Bold;
            _difficultyText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_difficultyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(440, 36));

            _statusText = UiFactory.CreateText(canvas.transform, "Status", 24, TextAnchor.UpperCenter);
            _statusText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -84), new Vector2(440, 40));

            _timeText = UiFactory.CreateText(canvas.transform, "TimeText", 16, TextAnchor.UpperCenter);
            _timeText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -114), new Vector2(440, 26));

            // Bottom-anchored (not the old center-anchor + fixed offset) for the same
            // reason as the grid/number pad/control rows below: a fixed offset from
            // canvas center only clears the grid's top edge on the 800x900 reference
            // aspect. On a taller phone the canvas is proportionally taller in canvas
            // units, so that same offset lands almost exactly on the grid's top edge
            // instead of above it - these two teal (interactable) buttons are created
            // before the grid, so their shadow shows through the 2px gaps between
            // row-1 cells instead of being safely hidden behind a clearly-higher row.
            var bottomAnchor = new Vector2(0.5f, 0f);
            _clearEntriesButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.clear"), new Vector2(-110, 1267), new Vector2(190, 44), true, ClearEntriesAction, bottomAnchor);
            _verifyButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.verify"), new Vector2(110, 1267), new Vector2(190, 44), true, Verify, bottomAnchor);

            // The grid, the number pad, the notes toggle, and both bottom control rows
            // all anchor to the canvas's bottom edge (fixed distance up from y=0 in that
            // frame) instead of a fixed offset from center. With matchWidthOrHeight=0 a
            // wider-aspect device (tablet in portrait) gets a shorter canvas in UI units,
            // so a center-fixed offset that fits on phones can land past the bottom edge
            // - anchoring to the edge keeps the same physical margin regardless of canvas
            // height. Every one of these has to share this same anchor: mixing a
            // center-anchored element with a bottom-anchored one lets their gap drift
            // with canvas height, and on a tall-aspect phone that drift previously closed
            // to zero and two rows rendered on top of each other. (bottomAnchor itself
            // is declared above, alongside Clear/Verify - the first bottom-anchored
            // elements built on this screen.)

            // Sized to run edge to edge with the number pad below it - from where
            // button "1" starts to where the Erase button ends (x = -352.5..+352.5).
            var gridObject = new GameObject("Grid", typeof(GridLayoutGroup));
            gridObject.transform.SetParent(canvas.transform, false);
            UiFactory.SetRect(gridObject.GetComponent<RectTransform>(), bottomAnchor, bottomAnchor, new Vector2(0, 502), new Vector2(705, 705));
            gridObject.GetComponent<RectTransform>().pivot = bottomAnchor;
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
            UiFactory.SetRect(gridOverlay.GetComponent<RectTransform>(), bottomAnchor, bottomAnchor, new Vector2(0, 502), new Vector2(705, 705));
            gridOverlay.GetComponent<RectTransform>().pivot = bottomAnchor;
            AddBoxDividers(gridOverlay.transform);

            // Number pad: two rows of six/five so nothing falls outside the reference
            // canvas (a single nine-wide row plus every control below it used to run
            // off the bottom of the screen).
            for (var n = 1; n <= 5; n++)
            {
                var number = n;
                var x = -300 + (n - 1) * 120;
                _numberButtons[n] = SudokuUi.CreateButton(canvas.transform, n.ToString(), new Vector2(x, 424), new Vector2(105, 50), true, () => SelectNumber(number), bottomAnchor);
            }
            _eraseButton = BuildEraseButton(canvas.transform, new Vector2(300, 424), new Vector2(105, 50), bottomAnchor);

            for (var n = 6; n <= 9; n++)
            {
                var number = n;
                var x = -240 + (n - 6) * 120;
                _numberButtons[n] = SudokuUi.CreateButton(canvas.transform, n.ToString(), new Vector2(x, 350), new Vector2(105, 50), true, () => SelectNumber(number), bottomAnchor);
            }
            _notesToggleButton = BuildPencilButton(canvas.transform, new Vector2(240, 350), new Vector2(105, 50), bottomAnchor);

            _undoButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.undo"), new Vector2(-180, 282), new Vector2(150, 44), false, UndoMove, bottomAnchor);
            _hintButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.hint"), new Vector2(0, 282), new Vector2(150, 44), true, UseHint, bottomAnchor);
            _autofillButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.autofill"), new Vector2(180, 282), new Vector2(150, 44), true, Autofill, bottomAnchor);

            _generateButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.generate"), new Vector2(-240, 220), new Vector2(220, 44), false, () =>
            {
                _generateDifficultyPopup.SetActive(true);
            }, bottomAnchor);
            _startButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.start"), new Vector2(0, 220), new Vector2(220, 44), false, StartCustomGame, bottomAnchor);
            _clearEditorButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.clearGrid"), new Vector2(240, 220), new Vector2(220, 44), false, ClearEditor, bottomAnchor);
            _watchAdButton = SudokuUi.CreateButton(canvas.transform, Loc.Get("play.watchAdHint"), new Vector2(0, 220), new Vector2(220, 44), false, WatchAdForHint, bottomAnchor);

            BuildSuccessPopup(canvas.transform);
            BuildGenerateDifficultyPopup(canvas.transform);
        }

        private void GenerateForEditor(Difficulty difficulty)
        {
            if (_mode != Mode.Editor) return;
            _editBoard = SudokuGenerator.Generate(difficulty, new System.Random()).Board;
            _editError = null;
            _generateDifficultyPopup.SetActive(false);
            Refresh();
        }

        private void BuildGenerateDifficultyPopup(Transform parent)
        {
            _generateDifficultyPopup = new GameObject("GenerateDifficultyPopup", typeof(Image));
            _generateDifficultyPopup.transform.SetParent(parent, false);
            UiFactory.SetRect(_generateDifficultyPopup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _generateDifficultyPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(_generateDifficultyPopup.transform, false);
            UiFactory.SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 440));
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = RoundedRectSprite.Get();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = SudokuTheme.Palette.PanelColor;

            var label = UiFactory.CreateText(panel.transform, "Label", 22, TextAnchor.MiddleCenter);
            label.color = SudokuTheme.Palette.TextColor;
            label.text = Loc.Get("popup.generateBody");
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(320, 70));

            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Easy), new Vector2(0, 70), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Easy));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Medium), new Vector2(0, 15), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Medium));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Hard), new Vector2(0, -40), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Hard));
            SudokuUi.CreateButton(panel.transform, Loc.Difficulty(Difficulty.Expert), new Vector2(0, -95), new Vector2(220, 46), true, () => GenerateForEditor(Difficulty.Expert));

            SudokuUi.CreateButton(panel.transform, Loc.Get("common.cancel"), new Vector2(0, -165), new Vector2(220, 40), true, () =>
            {
                _generateDifficultyPopup.SetActive(false);
            });

            _generateDifficultyPopup.SetActive(false);
        }

        private Button BuildEraseButton(Transform parent, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            // Named "Erase" (not "") so its GameObject/shadow don't collide with the
            // pencil button's under UiFactory.SetButtonActive's name-based shadow lookup -
            // two buttons both named "Button"/"ButtonShadow" made Transform.Find grab the
            // wrong sibling and orphan a shadow behind a hidden button.
            var button = SudokuUi.CreateButton(parent, "Erase", position, size, true, SelectErase, anchor);
            button.GetComponentInChildren<Text>().text = "";
            AddIconSprite(button.transform, ToolIconSprite.GetEraser(), size.y * 0.78f);
            return button;
        }

        private Button BuildPencilButton(Transform parent, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            var button = SudokuUi.CreateButton(parent, "Pencil", position, size, true, ToggleNotesMode, anchor);
            button.GetComponentInChildren<Text>().text = "";
            AddIconSprite(button.transform, ToolIconSprite.GetPencil(), size.y * 0.78f);
            return button;
        }

        // Icon textures are square (ToolIconSprite.Get*), so a single square size keeps
        // them undistorted regardless of the button's own (wider-than-tall) aspect ratio.
        private static void AddIconSprite(Transform parent, Sprite sprite, float squareSize)
        {
            var obj = new GameObject("Icon", typeof(Image));
            obj.transform.SetParent(parent, false);
            UiFactory.SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(squareSize, squareSize));
            var image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
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
            panelImage.color = SudokuTheme.Palette.PanelColor;

            var label = UiFactory.CreateText(panel.transform, "Label", 30, TextAnchor.MiddleCenter);
            label.color = SudokuTheme.Palette.TextColor;
            label.text = Loc.Get("play.solved");
            UiFactory.SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(320, 44));

            _successTimeText = UiFactory.CreateText(panel.transform, "SuccessTimeText", 18, TextAnchor.MiddleCenter);
            _successTimeText.color = SudokuTheme.Palette.TextColor;
            UiFactory.SetRect(_successTimeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 55), new Vector2(320, 60));

            SudokuUi.CreateButton(panel.transform, Loc.Get("popup.newPuzzle"), new Vector2(0, -20), new Vector2(260, 50), true, PlayAgain);
            SudokuUi.CreateButton(panel.transform, Loc.Get("popup.menu"), new Vector2(0, -90), new Vector2(260, 50), true, ReturnToMenu);

            _successPopup.SetActive(false);
        }
    }
}
