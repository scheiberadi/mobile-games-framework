using System;
using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Build a Word (M4 School, Literacy): Missing Letter's own answer-tile mechanic (read that one first) with
    // the hidden position moved from the middle letter to the last one - "CA_" shows the word's first two
    // letters beside a mystery box, and Eva speaks the whole target word aloud (word_<Word>, Rhyming's per-word
    // voice-line convention) before the tiles go interactive, so the child's ear confirms what they're building
    // as well as their eye. Same 3-letter catalogue and letter-tile shape as Missing Letter.
    public sealed class BuildAWordScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const float TileLetterSize = 150f;
        private const float WobbleSeconds = 0.4f;

        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float SingleY = -140f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        // The problem display, above the answer tiles: "CA_" - two known letters, then a mystery box.
        private const float ProblemY = 260f;
        private const float ProblemSlotSize = 150f;
        private const float ProblemSlotOffset = 170f;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _problemField;
        private RectTransform _answerField;
        private GameObject _endPanel;

        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private Image[] _tileLetterImages;
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousWord;
        private BuildAWordRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _problemField = CreateFullRectContainer("ProblemField");
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.BuildAWord);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousWord = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = BuildAWordRoundGenerator.Create(_game.Progress.BuildAWordLevel, _rng, _previousWord);
            _previousWord = _round.Word;
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowProblem(_round);
            ShowRoundAnswers(_round);
            SetAnswersInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("buildaword_find");
            yield return _game.Voice.SayAndWait("word_" + _round.Word);
            _eva.SetTalking(false);
            SetAnswersInteractable(true);
        }

        // --- Problem display ---------------------------------------------------------------------------------

        private void ShowProblem(BuildAWordRound round)
        {
            ClearChildren(_problemField);
            BuildLetterSlot(round.Word[0], -ProblemSlotOffset);
            BuildLetterSlot(round.Word[1], 0f);
            BuildMysteryBox(ProblemSlotOffset);
        }

        private void BuildLetterSlot(char letter, float centerX)
        {
            var go = new GameObject("Letter", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_problemField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, ProblemY);
            rect.sizeDelta = new Vector2(ProblemSlotSize, ProblemSlotSize);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("letters/" + letter);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        // The hidden letter's placeholder: a plain question-mark box, deliberately never a letter sprite - that
        // would hand the child the answer before they solve for it.
        private void BuildMysteryBox(float centerX)
        {
            var go = new GameObject("Mystery", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_problemField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, ProblemY);
            rect.sizeDelta = new Vector2(ProblemSlotSize, ProblemSlotSize);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/question");
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        // --- Answer tiles -----------------------------------------------------------------------------------

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[MaxTiles];
            _tileImages = new Image[MaxTiles];
            _tileLetterImages = new Image[MaxTiles];
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

                var letterGo = new GameObject("Letter", typeof(RectTransform), typeof(Image));
                letterGo.transform.SetParent(tile.transform, false);
                var letterRect = (RectTransform)letterGo.transform;
                letterRect.anchorMin = letterRect.anchorMax = letterRect.pivot = new Vector2(0.5f, 0.5f);
                letterRect.anchoredPosition = Vector2.zero;
                letterRect.sizeDelta = new Vector2(TileLetterSize, TileLetterSize);
                var letterImage = letterGo.GetComponent<Image>();
                letterImage.preserveAspect = true;
                letterImage.raycastTarget = false;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileLetterImages[i] = letterImage;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundAnswers(BuildAWordRound round)
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
                _tileLetterImages[i].sprite = EvaUi.Sprite("letters/" + round.Choices[i]);
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;
            }
        }

        // Same hand-computed grid shapes as NumberHuntScreen's PositionsFor (see there for the reasoning).
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
            if (_round == null || _roundOver || !_tileButtons[i].interactable) return;
            if (_round.Choices[i] == _round.Missing) _runner.StartCoroutine(OnCorrectTile(i));
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
            _game.Voice.Say("buildaword_hint");
            var leadRemaining = _game.Voice.Duration("buildaword_hint") + Voice.BreathSeconds;

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
            _game.Voice.Say("buildaword_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("buildaword_demo") + Voice.BreathSeconds);
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
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == _round.Missing) return i;
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

            _game.Progress.BuildAWordLevel = DifficultyLadder.RecordRound(_game.Progress.BuildAWordBuffer, _game.Progress.BuildAWordLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= BuildAWordRoundGenerator.RoundsPerSession) yield return EndSession();
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
            _problemField.gameObject.SetActive(!ended);
            _answerField.gameObject.SetActive(!ended);
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
