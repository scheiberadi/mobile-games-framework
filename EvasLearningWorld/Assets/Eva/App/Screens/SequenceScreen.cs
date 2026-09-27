using System;
using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shared SEQUENCE presenter (M4.5 Science Lab's Plant Growth, docs/kids-games/full-catalogue-plan.md
    // "7. Science Lab"): NumberOrderingScreen's own tile-grid-tap-in-order shape (read that one first), generalized
    // to show a sprite per tile instead of a numeral - the same relationship MatchScreen has to WordToImageScreen.
    // One instance is built per game, configured with its own round generator, ladder, sprite prefix and voice
    // lines; only which stage/value each tile shows varies. A wrong tap never eliminates a tile, since it may
    // still be due later in the sequence - identical mistake handling to Number Ordering and Follow Numbers/Letters
    // in Order.
    public sealed class SequenceScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float EvaHeight = 430f;
        private static readonly Vector2 EvaPosition = new Vector2(627f, -140f);

        private const int MaxTiles = 5;
        private const float TileSize = 220f;
        private const float WobbleSeconds = 0.4f;

        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 60f;
        private const float BottomY = -220f;
        private const float SingleY = -80f;

        private const float HandMoveSeconds = 0.4f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 1.2f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly string _tileSpritePrefix;
        private readonly Func<int, System.Random, SequenceRound> _generateRound;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, System.Collections.Generic.List<bool>> _getBuffer;
        private readonly int _roundsPerSession;
        private readonly string _promptVoiceKey, _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _answerField;
        private GameObject _endPanel;

        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private Button[] _tileButtons;
        private bool[] _tileDone;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private SequenceRound _round;
        private HelpLadder _ladder;
        private int _nextIndex;
        private int _rightLineIndex;

        public SequenceScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite, string tileSpritePrefix,
            Func<int, System.Random, SequenceRound> generateRound,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, System.Collections.Generic.List<bool>> getBuffer,
            int roundsPerSession, string promptVoiceKey, string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _tileSpritePrefix = tileSpritePrefix;
            _generateRound = generateRound;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _roundsPerSession = roundsPerSession;
            _promptVoiceKey = promptVoiceKey;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _answerField = CreateFullRectContainer("AnswerField");
            BuildTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = _generateRound(_getLevel(_game.Progress), _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _nextIndex = 0;

            ShowRoundTiles(_round);
            SetAllTilesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_promptVoiceKey);
            _eva.SetTalking(false);
            SetAllTilesInteractable(true);
        }

        // --- Tiles ---------------------------------------------------------------------------------------------

        private void BuildTiles()
        {
            _tiles = new RectTransform[MaxTiles];
            _tileImages = new Image[MaxTiles];
            _tileButtons = new Button[MaxTiles];

            for (var i = 0; i < MaxTiles; i++)
            {
                var tile = new GameObject("Tile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_answerField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var tileIndex = i;
                button.onClick.AddListener(() => OnTileTapped(tileIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundTiles(SequenceRound round)
        {
            StopTilePulse();
            _tileDone = new bool[MaxTiles];
            var count = round.Choices.Length;
            var positions = PositionsFor(count);
            for (var i = 0; i < MaxTiles; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _tiles[i].anchoredPosition = positions[i];
                _tileImages[i].sprite = EvaUi.Sprite(_tileSpritePrefix + round.Choices[i]);
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private void SetAllTilesInteractable(bool interactable)
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (!_tileDone[i]) _tileButtons[i].interactable = interactable;
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

        // Which tile currently shows `value` (Choices' positions are shuffled independently of the target order).
        private int IndexOfTileValue(string value)
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == value) return i;
            return -1;
        }

        // --- Taps ------------------------------------------------------------------------------------------

        private void OnTileTapped(int i)
        {
            if (_round == null || _roundOver || _tileDone[i] || !_tileButtons[i].interactable) return;
            if (_round.Choices[i] == _round.TargetOrder[_nextIndex]) _runner.StartCoroutine(OnCorrectTap(i));
            else OnWrongTap(i);
        }

        private void OnWrongTap(int i)
        {
            _eva.Angry();
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

        // 2nd mistake: the next-correct tile pulses.
        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);
            var lead = _game.Voice.Duration(_hintVoiceKey) + Voice.BreathSeconds;

            var targetIndex = IndexOfTileValue(_round.TargetOrder[_nextIndex]);
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[targetIndex]));
            yield return new WaitForSeconds(Mathf.Max(lead, HintRestSeconds));
            _eva.SetTalking(false);
            StopTilePulse();
            SetAllTilesInteractable(true);
        }

        // 3rd mistake: the hand taps through the remaining correct order once (nothing is locked in by this),
        // then the child repeats the whole remainder themselves.
        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);

            for (var n = _nextIndex; n < _round.TargetOrder.Length; n++)
            {
                var index = IndexOfTileValue(_round.TargetOrder[n]);
                yield return _hand.MoveTo(_tiles[index].anchoredPosition, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
            }
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        private IEnumerator OnCorrectTap(int i)
        {
            _tileDone[i] = true;
            _tileButtons[i].interactable = false;
            _game.Sfx.Tap();
            _runner.StartCoroutine(PopPulse(_tiles[i], _tileImages[i], 1.15f, 0.25f));
            _nextIndex++;

            if (_nextIndex < _round.TargetOrder.Length) yield break;

            _roundOver = true;
            SetAllTilesInteractable(false);
            StopTilePulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            var progress = _game.Progress;
            _setLevel(progress, DifficultyLadder.RecordRound(_getBuffer(progress), _getLevel(progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        private IEnumerator AdvanceRound()
        {
            _roundIndex++;
            if (_roundIndex >= _roundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(_homeScreenId);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _answerField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            var anchor = new GameObject("EvaAnchor", typeof(RectTransform));
            anchor.transform.SetParent(Root, false);
            var anchorRect = (RectTransform)anchor.transform;
            anchorRect.anchorMin = anchorRect.anchorMax = anchorRect.pivot = new Vector2(0.5f, 0.5f);
            anchorRect.anchoredPosition = EvaPosition;
            anchorRect.sizeDelta = Vector2.zero;

            _eva = RigFactory.CreateEva(anchorRect, EvaHeight);
            var scale = _eva.Root.localScale;
            _eva.Root.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
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

        private void AddPictureBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(_backgroundSprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
