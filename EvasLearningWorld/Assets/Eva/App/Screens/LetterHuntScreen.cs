using System;
using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Letter Hunt (M4 School, Literacy): reuses Number Hunt's own screen shape almost exactly
    // (NumberHuntScreen is this screen's closest sibling; read that one first) - Eva speaks a letter, the child
    // taps the matching tile among several. Two differences from Number Hunt: the target is spoken, never shown
    // as text, and every tile shows its letter as a sprite ("letters/<lowercase>") rather than a TMP_Text
    // numeral - letters carry no order a preliterate child can infer by sight the way digits do, the same
    // reasoning Follow Letters in Order's checkpoints already established (Playground). Same two-step help
    // ladder as Number Hunt: 1st mistake a gentle Retry, 2nd a Hint (hand points at, does not tap, the correct
    // tile), 3rd a full Demonstrate (hand taps it, only that tile stays interactable).
    public sealed class LetterHuntScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // Every tile is a real tap target (no ObjectField-style exemption here), so size stays flat at MinTap.
        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const float LetterSize = 150f;
        private const float WobbleSeconds = 0.4f;

        // Same hand-computed grid as NumberHuntScreen (see there for the reasoning).
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

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
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
        private char? _previousTarget;
        private LetterHuntRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.LetterHunt);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousTarget = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = LetterHuntRoundGenerator.Create(_game.Progress.LetterHuntLevel, _rng, _previousTarget);
            _previousTarget = _round.Target;
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowRoundAnswers(_round);
            SetAnswersInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("letterhunt_find");
            yield return _game.Voice.SayAndWait("letter_" + _round.Target);
            _eva.SetTalking(false);
            SetAnswersInteractable(true);
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
                letterRect.sizeDelta = new Vector2(LetterSize, LetterSize);
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

        private void ShowRoundAnswers(LetterHuntRound round)
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
            if (_round.Choices[i] == _round.Target) _runner.StartCoroutine(OnCorrectTile(i));
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
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: tiles are disabled, the pointer hand moves to the correct tile and pulses (points, does
        // not tap it), then every tile (tried ones stay dimmed but become tappable again) is enabled once more.
        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("letterhunt_hint");
            var leadRemaining = _game.Voice.Duration("letterhunt_hint") + Voice.BreathSeconds;

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        // 3rd mistake: the hand moves to the correct tile and taps it, the tile gets an idle pulse, and only
        // that tile becomes interactable - the child still has to tap it themselves.
        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("letterhunt_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("letterhunt_demo") + Voice.BreathSeconds);
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
                if (_round.Choices[i] == _round.Target) return i;
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

            _game.Progress.LetterHuntLevel = DifficultyLadder.RecordRound(_game.Progress.LetterHuntBuffer, _game.Progress.LetterHuntLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("letter_" + _round.Target);
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= LetterHuntRoundGenerator.RoundsPerSession) yield return EndSession();
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
            _endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(WinCelebration));
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
