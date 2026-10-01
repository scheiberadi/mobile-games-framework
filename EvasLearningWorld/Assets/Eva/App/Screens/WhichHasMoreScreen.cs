using System;
using System.Collections;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Which Has More? (M4 School, Mathematics): two object groups (Count's own CountObject/objects sprites).
    // Levels 1-3 are a genuinely new shape for this session - not a numeral-tile pick, but a direct TAP-THE-
    // TARGET on the bigger group itself: two large buttons (each a real tap target, not the small per-icon
    // grids inside them) hold the two groups side by side, and the child taps the one with more. From level 4
    // (the plan's own "later, 'how many more' asks for the numeric difference" extension) the two groups are
    // shown only as a static comparison and the question becomes numeric: the child taps the difference among
    // answer tiles, Addition/Subtraction's own answer-tile shape (reusing NumberHuntScreen's mechanic, read that
    // one first for those tiles). Same two-step help ladder throughout: 1st mistake a gentle Retry, 2nd a Hint
    // (hand points at, does not tap, the correct target), 3rd a full Demonstrate (hand taps it, only that
    // target stays interactable).
    public sealed class WhichHasMoreScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }


        // --- Levels 1-3: tap the bigger group ----------------------------------------------------------------
        private const float GroupButtonWidth = 380f;
        private const float GroupButtonHeight = 380f;
        private const float GroupButtonOffsetX = 260f;
        private const float GroupButtonY = -40f;
        private const float GroupIconSize = 60f;
        private const int GroupIconsPerRow = 3;
        private const float GroupIconPitch = 90f;

        // --- Levels 4-6: tap the numeral difference (Addition/Subtraction's own answer-tile shape) ------------
        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const int TileNumeralFontSize = 70;
        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float SingleY = -140f;
        private const float ProblemY = 260f;
        private const float ProblemIconSize = 55f;
        private const int ProblemIconsPerRow = 4;
        private const float ProblemIconPitch = 70f;
        private const float ProblemGroupOffsetX = 220f;

        private const float WobbleSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;

        private RectTransform _compareField;
        private RectTransform[] _groupButtonRects;
        private Image[] _groupButtonImages;
        private Button[] _groupButtons;

        private RectTransform _problemField;
        private RectTransform _answerField;
        private GameObject _endPanel;

        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private TextMeshProUGUI[] _tileNumerals;
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private int? _previousDifference;
        private WhichHasMoreRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _compareField = CreateFullRectContainer("CompareField");
            BuildGroupButtons();
            _problemField = CreateFullRectContainer("ProblemField");
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.WhichHasMore);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousDifference = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = WhichHasMoreRoundGenerator.Create(_game.Progress.WhichHasMoreLevel, _rng, _previousDifference);
            _previousDifference = Math.Abs(_round.A - _round.B);
            _ladder = new HelpLadder();
            _roundOver = false;

            _compareField.gameObject.SetActive(!_round.AskDifference);
            _problemField.gameObject.SetActive(_round.AskDifference);
            _answerField.gameObject.SetActive(_round.AskDifference);

            if (_round.AskDifference)
            {
                ShowProblem(_round);
                ShowRoundAnswers(_round);
                SetAnswersInteractable(false);
            }
            else
            {
                ShowGroups(_round);
                SetGroupsInteractable(false);
            }

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.AskDifference ? "whichhasmore_difference" : "whichhasmore_find");
            _eva.SetTalking(false);

            if (_round.AskDifference) SetAnswersInteractable(true);
            else SetGroupsInteractable(true);
        }

        // --- Levels 1-3: tap the bigger group -----------------------------------------------------------------

        private void BuildGroupButtons()
        {
            _groupButtonRects = new RectTransform[2];
            _groupButtonImages = new Image[2];
            _groupButtons = new Button[2];
            var offsets = new[] { -GroupButtonOffsetX, GroupButtonOffsetX };

            for (var i = 0; i < 2; i++)
            {
                var go = new GameObject("GroupButton" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                go.transform.SetParent(_compareField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(offsets[i], GroupButtonY);
                rect.sizeDelta = new Vector2(GroupButtonWidth, GroupButtonHeight);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.type = Image.Type.Simple;

                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var groupIndex = i;
                button.onClick.AddListener(() => OnGroupTapped(groupIndex));

                _groupButtonRects[i] = rect;
                _groupButtonImages[i] = image;
                _groupButtons[i] = button;
            }
        }

        private void ShowGroups(WhichHasMoreRound round)
        {
            foreach (var rect in _groupButtonRects) ClearChildren(rect);
            var counts = new[] { round.A, round.B };
            for (var i = 0; i < 2; i++)
            {
                PopulateObjectGrid(_groupButtonRects[i], round.Object, counts[i], GroupIconSize, GroupIconsPerRow, GroupIconPitch);
                _groupButtonImages[i].color = Color.white;
                _groupButtonRects[i].localScale = Vector3.one;
            }
        }

        private void SetGroupsInteractable(bool interactable)
        {
            foreach (var button in _groupButtons) button.interactable = interactable;
        }

        private int CorrectGroupIndex() => _round.A > _round.B ? 0 : 1;

        private void OnGroupTapped(int i)
        {
            if (_round == null || _roundOver || _round.AskDifference || !_groupButtons[i].interactable) return;
            if (i == CorrectGroupIndex()) _runner.StartCoroutine(OnCorrectGroup(i));
            else OnWrongGroup(i);
        }

        private void OnWrongGroup(int i)
        {
            _eva.Angry();
            _groupButtons[i].interactable = false;
            _groupButtonImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_groupButtonRects[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunGroupHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunGroupDemonstrate());
                    break;
            }
        }

        private IEnumerator RunGroupHint()
        {
            SetGroupsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichhasmore_hint");
            var leadRemaining = _game.Voice.Duration("whichhasmore_hint") + Voice.BreathSeconds;

            var correctIndex = CorrectGroupIndex();
            yield return _hand.MoveTo(_groupButtonRects[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetGroupsInteractable(true);
        }

        private IEnumerator RunGroupDemonstrate()
        {
            SetGroupsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichhasmore_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("whichhasmore_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var correctIndex = CorrectGroupIndex();
            yield return _hand.MoveTo(_groupButtonRects[correctIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _groupButtons[correctIndex].interactable = true;
            _groupButtons[1 - correctIndex].interactable = false;
        }

        private IEnumerator OnCorrectGroup(int i)
        {
            _roundOver = true;
            SetGroupsInteractable(false);
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_groupButtonRects[i], _groupButtonImages[i], 1.1f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.WhichHasMoreLevel = DifficultyLadder.RecordRound(_game.Progress.WhichHasMoreBuffer, _game.Progress.WhichHasMoreLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _groupButtonRects[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        // --- Levels 4-6: tap the numeral difference ----------------------------------------------------------

        private void ShowProblem(WhichHasMoreRound round)
        {
            ClearChildren(_problemField);
            var leftGo = new GameObject("GroupA", typeof(RectTransform));
            leftGo.transform.SetParent(_problemField, false);
            var leftRect = (RectTransform)leftGo.transform;
            leftRect.anchorMin = leftRect.anchorMax = leftRect.pivot = new Vector2(0.5f, 0.5f);
            leftRect.anchoredPosition = new Vector2(-ProblemGroupOffsetX, ProblemY);
            PopulateObjectGrid(leftRect, round.Object, round.A, ProblemIconSize, ProblemIconsPerRow, ProblemIconPitch);

            var rightGo = new GameObject("GroupB", typeof(RectTransform));
            rightGo.transform.SetParent(_problemField, false);
            var rightRect = (RectTransform)rightGo.transform;
            rightRect.anchorMin = rightRect.anchorMax = rightRect.pivot = new Vector2(0.5f, 0.5f);
            rightRect.anchoredPosition = new Vector2(ProblemGroupOffsetX, ProblemY);
            PopulateObjectGrid(rightRect, round.Object, round.B, ProblemIconSize, ProblemIconsPerRow, ProblemIconPitch);
        }

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[MaxTiles];
            _tileImages = new Image[MaxTiles];
            _tileNumerals = new TextMeshProUGUI[MaxTiles];
            _tileButtons = new Button[MaxTiles];

            for (var i = 0; i < MaxTiles; i++)
            {
                var tile = new GameObject("Tile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_answerField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.type = Image.Type.Simple;

                var numeral = EvaUi.Numeral(tile.transform, "Numeral", TileNumeralFontSize);
                var numeralRect = (RectTransform)numeral.transform;
                numeralRect.anchorMin = numeralRect.anchorMax = numeralRect.pivot = new Vector2(0.5f, 0.5f);
                numeralRect.anchoredPosition = Vector2.zero;
                numeralRect.sizeDelta = new Vector2(TileSize, 150f);

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileNumerals[i] = numeral;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundAnswers(WhichHasMoreRound round)
        {
            StopTilePulse();
            _tileTried = new bool[MaxTiles];
            var count = round.Choices.Length;
            var positions = PositionsFor(count);
            for (var i = 0; i < MaxTiles; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _tiles[i].anchoredPosition = positions[i];
                _tileNumerals[i].text = round.Choices[i].ToString();
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;
            }
        }

        private static Vector2[] PositionsFor(int count)
        {
            switch (count)
            {
                case 3:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], SingleY),
                        new Vector2(CenterX + ColOffsets3[1], SingleY),
                        new Vector2(CenterX + ColOffsets3[2], SingleY),
                    };
                case 4:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets2[0], TopY),
                        new Vector2(CenterX + ColOffsets2[1], TopY),
                        new Vector2(CenterX + ColOffsets2[0], BottomY),
                        new Vector2(CenterX + ColOffsets2[1], BottomY),
                    };
                case 5:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], TopY),
                        new Vector2(CenterX + ColOffsets3[1], TopY),
                        new Vector2(CenterX + ColOffsets3[2], TopY),
                        new Vector2(CenterX + ColOffsets2[0], BottomY),
                        new Vector2(CenterX + ColOffsets2[1], BottomY),
                    };
                case 6:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], TopY),
                        new Vector2(CenterX + ColOffsets3[1], TopY),
                        new Vector2(CenterX + ColOffsets3[2], TopY),
                        new Vector2(CenterX + ColOffsets3[0], BottomY),
                        new Vector2(CenterX + ColOffsets3[1], BottomY),
                        new Vector2(CenterX + ColOffsets3[2], BottomY),
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private void SetAnswersInteractable(bool interactable)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                if (!_tileTried[i]) _tileButtons[i].interactable = interactable;
        }

        private void SetAllTilesInteractable(bool interactable)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                _tileButtons[i].interactable = interactable;
        }

        private void SetOnlyTileInteractable(int index)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                _tileButtons[i].interactable = i == index;
        }

        private void StopTilePulse()
        {
            if (_tilePulseRoutine != null)
            {
                _runner.StopCoroutine(_tilePulseRoutine);
                _tilePulseRoutine = null;
            }
            if (_tiles != null)
                foreach (var tile in _tiles) tile.localScale = Vector3.one;
        }

        private void OnTileTapped(int i)
        {
            if (_round == null || _roundOver || !_round.AskDifference || !_tileButtons[i].interactable) return;
            var difference = Math.Abs(_round.A - _round.B);
            if (_round.Choices[i] == difference) _runner.StartCoroutine(OnCorrectTile(i));
            else OnWrongTile(i);
        }

        private void OnWrongTile(int i)
        {
            _tileTried[i] = true;
            _eva.Angry();
            _tileButtons[i].interactable = false;
            _tileImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_tiles[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichhasmore_hint");
            var leadRemaining = _game.Voice.Duration("whichhasmore_hint") + Voice.BreathSeconds;

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichhasmore_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("whichhasmore_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[correctIndex]));
            SetOnlyTileInteractable(correctIndex);
        }

        private int CorrectTileIndex()
        {
            var difference = Math.Abs(_round.A - _round.B);
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == difference) return i;
            return -1;
        }

        private IEnumerator OnCorrectTile(int i)
        {
            _roundOver = true;
            SetAllTilesInteractable(false);
            StopTilePulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_tiles[i], _tileImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.WhichHasMoreLevel = DifficultyLadder.RecordRound(_game.Progress.WhichHasMoreBuffer, _game.Progress.WhichHasMoreLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        private IEnumerator AdvanceRound()
        {
            _roundIndex++;
            if (_roundIndex >= WhichHasMoreRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        private IEnumerator EndSession()
        {
            yield return _game.Voice.SayAndWait("count_done");
            _eva.Cheer();
            SetSessionEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
        }

        // --- End of session -----------------------------------------------------------------------------

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, GoHomeAfterSession);

            _endPanel.SetActive(false);
        }

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.School);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _compareField.gameObject.SetActive(false);
            _problemField.gameObject.SetActive(false);
            _answerField.gameObject.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens (identical shapes to NumberHuntScreen's - see there for the reasoning behind each) ---

        private static IEnumerator PopPulse(RectTransform target, Image tint, float peakScale, float duration)
        {
            var original = tint != null ? tint.color : Color.white;
            var glow = Color.Lerp(original, Color.white, 0.6f);
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, k);
                if (tint != null) tint.color = Color.Lerp(original, glow, k);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, k);
                if (tint != null) tint.color = Color.Lerp(glow, original, k);
                yield return null;
            }
            target.localScale = Vector3.one;
            if (tint != null) tint.color = original;
        }

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                var angle = Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay;
                target.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            target.localRotation = Quaternion.identity;
        }

        private static IEnumerator BigCheer(RectTransform target, Vector3 baseScale)
        {
            const float peak = 1.18f, duration = 0.4f, half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = baseScale;
        }

        private static IEnumerator IdlePulseLoop(RectTransform target)
        {
            const float period = 0.7f;
            const float peakScale = 1.12f;
            while (true)
            {
                for (var t = 0f; t < period; t += Time.deltaTime)
                {
                    var k = t / period;
                    var scale = k < 0.5f ? Mathf.Lerp(1f, peakScale, k * 2f) : Mathf.Lerp(peakScale, 1f, (k - 0.5f) * 2f);
                    target.localScale = Vector3.one * scale;
                    yield return null;
                }
            }
        }

        // --- Helpers ----------------------------------------------------------------------------------------

        // Lays out `count` object icons in a grid centred on `parent`'s own local origin - used both for the
        // levels 1-3 compare buttons (parent is the button's own rect, so the grid sits inside it) and the
        // levels 4-6 static problem groups (parent is a plain positioned container).
        private static void PopulateObjectGrid(Transform parent, CountObject obj, int count, float iconSize, int perRow, float pitch)
        {
            var rows = (count + perRow - 1) / perRow;
            for (var i = 0; i < count; i++)
            {
                var row = i / perRow;
                var inRow = row == rows - 1 ? count - row * perRow : perRow;
                var col = i % perRow;
                var x = (col - (inRow - 1) / 2f) * pitch;
                var y = ((rows - 1) / 2f - row) * pitch;

                var go = new GameObject("Object" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(iconSize, iconSize);
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("objects/" + obj.ToString().ToLowerInvariant());
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        private RectTransform CreateFullRectContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private void AddSchoolBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/school_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
