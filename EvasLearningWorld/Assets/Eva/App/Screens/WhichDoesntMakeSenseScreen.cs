using EvasLearningWorld.Rules;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's fourth game (spec 4.1, build order: Which Doesn't Make Sense? is 4th). Four scenario pictures
    // are shown, one impossible (the plan's own examples: cow in ocean, fish in tree); the child taps it. Same
    // TAP-THE-TARGET shell as OddOneOutScreen (help ladder, coin payout, difficulty ladder, end panel); unlike
    // every other Playground game the round shape never changes with level (see
    // WhichDoesntMakeSenseRoundGenerator, Rules/WhichDoesntMakeSense.cs, for why).
    public sealed class WhichDoesntMakeSenseScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // Identical hand-computed grid to NumberHuntScreen's 4-tile layout (two rows of two); see there for the
        // Hud-clearance reasoning.
        private const int TileCount = 4;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const float CenterX = -200f;
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float WobbleSeconds = 0.4f;

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
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private int? _previousPoolIndex;
        private WhichDoesntMakeSenseRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.WhichDoesntMakeSense);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousPoolIndex = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = WhichDoesntMakeSenseRoundGenerator.Create(_game.Progress.WhichDoesntMakeSenseLevel, _rng, _previousPoolIndex);
            _previousPoolIndex = _round.PoolIndex;
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowRoundAnswers(_round);
            SetAnswersInteractable(true);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("whichdoesntmakesense_find");
            _eva.SetTalking(false);
        }

        // --- Answer tiles -----------------------------------------------------------------------------------

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[TileCount];
            _tileImages = new Image[TileCount];
            _tileButtons = new Button[TileCount];

            var positions = new[]
            {
                new Vector2(CenterX + ColOffsets2[0], TopY),
                new Vector2(CenterX + ColOffsets2[1], TopY),
                new Vector2(CenterX + ColOffsets2[0], BottomY),
                new Vector2(CenterX + ColOffsets2[1], BottomY),
            };

            for (var i = 0; i < TileCount; i++)
            {
                var tile = new GameObject("Tile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_answerField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);
                rect.anchoredPosition = positions[i];

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundAnswers(WhichDoesntMakeSenseRound round)
        {
            StopTilePulse();
            _tileTried = new bool[TileCount];
            for (var i = 0; i < TileCount; i++)
            {
                _tileImages[i].sprite = EvaUi.Sprite("whichdoesntmakesense/" + round.Items[i]);
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;
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
            if (i == _round.OddIndex) _runner.StartCoroutine(OnCorrectTile(i));
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

        // 2nd mistake: the impossible picture wobbles/glows on its own (per the plan's Hint note) - reuses the
        // idle pulse loop as the "glows" cue.
        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichdoesntmakesense_hint");
            var lead = _game.Voice.Duration("whichdoesntmakesense_hint") + Voice.BreathSeconds;

            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[_round.OddIndex]));
            yield return new WaitForSeconds(Mathf.Max(lead, HintRestSeconds));
            _eva.SetTalking(false);
            StopTilePulse();
            SetAllTilesInteractable(true);
        }

        // 3rd mistake: the hand taps the correct picture, then only that tile stays interactable.
        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whichdoesntmakesense_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("whichdoesntmakesense_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            yield return _hand.MoveTo(_tiles[_round.OddIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[_round.OddIndex]));
            SetOnlyTileInteractable(_round.OddIndex);
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

            _game.Progress.WhichDoesntMakeSenseLevel = DifficultyLadder.RecordRound(_game.Progress.WhichDoesntMakeSenseBuffer, _game.Progress.WhichDoesntMakeSenseLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= WhichDoesntMakeSenseRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

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

        private void AddPlaygroundBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/playground_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
