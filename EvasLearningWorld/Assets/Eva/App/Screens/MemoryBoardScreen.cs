using System;
using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Classic Memory's own mechanic (M4.8 Brain Gym, docs/kids-games/full-catalogue-plan.md "10. Brain Gym"):
    // a grid of face-down cards, tap two at a time, matches stay face up. Board size (2-6 pairs) grows with
    // level via MemoryBoardRoundGenerator. Mistakes (mismatched pairs) drive the same HelpLadder/CoinPayout shape
    // every other game uses: 1st mismatch is a gentle retry, 2nd briefly peeks a random still-hidden pair (hint),
    // 3rd auto-reveals and matches one pair for the child (demonstrate) - "clean" (for the level ladder and coin
    // payout) means the whole board finished before any demonstrate step fired.
    public sealed class MemoryBoardScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float EvaHeight = 380f;
        private static readonly Vector2 EvaPosition = new Vector2(627f, -300f);

        private const int MaxCards = 12;
        private const float CardSize = 170f;
        private const float ColSpacing = 190f;
        private const float RowSpacing = 190f;
        private const int MaxColumns = 4;
        private const float TopY = 220f;
        private const float CenterX = -200f;
        private const float FlipSeconds = 0.2f;
        private const float MismatchPauseSeconds = 0.7f;
        private const float PeekSeconds = 1.0f;

        private const float HandMoveSeconds = 0.4f;
        private const float HandTapSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<int, System.Random, MemoryRound> _generateRound;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, System.Collections.Generic.List<bool>> _getBuffer;
        private readonly int _roundsPerSession;
        private readonly string _promptVoiceKey, _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _boardField;
        private GameObject _endPanel;

        private RectTransform[] _cards;
        private Image[] _cardImages;
        private Button[] _cardButtons;
        private bool[] _cardMatched;

        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private MemoryRound _round;
        private HelpLadder _ladder;
        private int _matchedPairs;
        private int _firstFlipped = -1;
        private bool _boardOver;
        private bool _inputLocked;
        private int _rightLineIndex;

        public MemoryBoardScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<int, System.Random, MemoryRound> generateRound,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, System.Collections.Generic.List<bool>> getBuffer,
            int roundsPerSession, string promptVoiceKey, string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
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
            _boardField = CreateFullRectContainer("BoardField");
            BuildCards();
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
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = _generateRound(_getLevel(_game.Progress), _rng);
            _ladder = new HelpLadder();
            _matchedPairs = 0;
            _firstFlipped = -1;
            _boardOver = false;
            _inputLocked = true;

            ShowBoard(_round);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_promptVoiceKey);
            _eva.SetTalking(false);
            _inputLocked = false;
        }

        // --- Board ------------------------------------------------------------------------------------------

        private void BuildCards()
        {
            _cards = new RectTransform[MaxCards];
            _cardImages = new Image[MaxCards];
            _cardButtons = new Button[MaxCards];

            for (var i = 0; i < MaxCards; i++)
            {
                var card = new GameObject("Card" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                card.transform.SetParent(_boardField, false);
                var rect = (RectTransform)card.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(CardSize, CardSize);

                var image = card.GetComponent<Image>();
                image.preserveAspect = true;

                var button = card.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var cardIndex = i;
                button.onClick.AddListener(() => OnCardTapped(cardIndex));

                _cards[i] = rect;
                _cardImages[i] = image;
                _cardButtons[i] = button;
            }
        }

        private void ShowBoard(MemoryRound round)
        {
            _cardMatched = new bool[MaxCards];
            var count = round.Board.Length;
            var columns = Math.Min(MaxColumns, count);
            var rows = (count + columns - 1) / columns;
            var rowWidth = (columns - 1) * ColSpacing;

            for (var i = 0; i < MaxCards; i++)
            {
                _cards[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                var col = i % columns;
                var row = i / columns;
                var x = CenterX - rowWidth / 2f + col * ColSpacing;
                var y = TopY - row * RowSpacing;
                _cards[i].anchoredPosition = new Vector2(x, y);
                _cardImages[i].sprite = EvaUi.Sprite(MemoryBoardRoundGenerator.CardBackSprite);
                _cardImages[i].color = Color.white;
                _cards[i].localScale = Vector3.one;
                _cardButtons[i].interactable = true;
            }
        }

        private void SetAllCardsInteractable(bool interactable)
        {
            for (var i = 0; i < _round.Board.Length; i++)
                if (!_cardMatched[i]) _cardButtons[i].interactable = interactable;
        }

        // --- Taps ------------------------------------------------------------------------------------------

        private void OnCardTapped(int i)
        {
            if (_round == null || _boardOver || _inputLocked || _cardMatched[i] || !_cardButtons[i].interactable) return;
            if (i == _firstFlipped) return;
            _runner.StartCoroutine(FlipUp(i));

            if (_firstFlipped < 0)
            {
                _firstFlipped = i;
                return;
            }

            var first = _firstFlipped;
            _firstFlipped = -1;
            if (_round.Board[first] == _round.Board[i]) _runner.StartCoroutine(OnPairMatched(first, i));
            else _runner.StartCoroutine(OnPairMismatched(first, i));
        }

        private IEnumerator FlipUp(int i)
        {
            for (var t = 0f; t < FlipSeconds; t += Time.deltaTime)
            {
                var k = t / FlipSeconds;
                _cards[i].localScale = new Vector3(Mathf.Abs(Mathf.Lerp(1f, -1f, k)), 1f, 1f);
                if (k >= 0.5f) _cardImages[i].sprite = EvaUi.Sprite(MemoryBoardRoundGenerator.CardSpritePrefix + _round.Board[i]);
                yield return null;
            }
            _cards[i].localScale = Vector3.one;
        }

        private IEnumerator FlipDown(int i)
        {
            for (var t = 0f; t < FlipSeconds; t += Time.deltaTime)
            {
                var k = t / FlipSeconds;
                _cards[i].localScale = new Vector3(Mathf.Abs(Mathf.Lerp(1f, -1f, k)), 1f, 1f);
                if (k >= 0.5f) _cardImages[i].sprite = EvaUi.Sprite(MemoryBoardRoundGenerator.CardBackSprite);
                yield return null;
            }
            _cards[i].localScale = Vector3.one;
        }

        private IEnumerator OnPairMatched(int a, int b)
        {
            _cardMatched[a] = true;
            _cardMatched[b] = true;
            _cardButtons[a].interactable = false;
            _cardButtons[b].interactable = false;
            _game.Sfx.Tap();
            _runner.StartCoroutine(PopPulse(_cards[a]));
            _runner.StartCoroutine(PopPulse(_cards[b]));
            _matchedPairs++;

            if (_matchedPairs * 2 < _round.Board.Length) yield break;

            _boardOver = true;
            SetAllCardsInteractable(false);
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            var progress = _game.Progress;
            _setLevel(progress, DifficultyLadder.RecordRound(_getBuffer(progress), _getLevel(progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _cards[b].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        private IEnumerator OnPairMismatched(int a, int b)
        {
            _inputLocked = true;
            _eva.Angry();
            var step = _ladder.RecordMistake();
            yield return new WaitForSeconds(MismatchPauseSeconds);
            yield return FlipDown(a);
            yield return FlipDown(b);

            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    break;
                case HelpStep.Hint:
                    yield return RunHint();
                    break;
                case HelpStep.Demonstrate:
                    yield return RunDemonstrate();
                    break;
            }
            _inputLocked = false;
        }

        // 2nd mismatch: peek a random still-hidden pair, then hide it again.
        private IEnumerator RunHint()
        {
            SetAllCardsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);

            var pairIndex = FindAnyHiddenPair();
            if (pairIndex.a >= 0)
            {
                yield return FlipUp(pairIndex.a);
                yield return FlipUp(pairIndex.b);
                yield return new WaitForSeconds(PeekSeconds);
                yield return FlipDown(pairIndex.a);
                yield return FlipDown(pairIndex.b);
            }
            _eva.SetTalking(false);
            SetAllCardsInteractable(true);
        }

        // 3rd mismatch: Eva reveals and matches one pair for the child.
        private IEnumerator RunDemonstrate()
        {
            SetAllCardsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var pair = FindAnyHiddenPair();
            if (pair.a >= 0)
            {
                yield return _hand.MoveTo(_cards[pair.a].anchoredPosition, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return FlipUp(pair.a);
                yield return _hand.MoveTo(_cards[pair.b].anchoredPosition, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return OnPairMatched(pair.a, pair.b);
            }
            _hand.Hide();
            SetAllCardsInteractable(true);
        }

        private (int a, int b) FindAnyHiddenPair()
        {
            for (var i = 0; i < _round.Board.Length; i++)
            {
                if (_cardMatched[i]) continue;
                for (var j = i + 1; j < _round.Board.Length; j++)
                {
                    if (_cardMatched[j]) continue;
                    if (_round.Board[i] == _round.Board[j]) return (i, j);
                }
            }
            return (-1, -1);
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
            _boardField.gameObject.SetActive(!ended);
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

        // --- Small tweens (identical shapes to other screens' - see MatchScreen for the reasoning) ------------

        private static IEnumerator PopPulse(RectTransform target)
        {
            const float peak = 1.2f, duration = 0.3f, half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = Vector3.one;
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
