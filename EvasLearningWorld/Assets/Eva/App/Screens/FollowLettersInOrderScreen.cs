using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's eighth game (spec 4.1, build order: Follow Letters in Order is 8th - the last that fits
    // inside TileLayout's 8-activity ceiling, see full-catalogue-plan.md) - the third NAVIGATION game, and the
    // first with a Literacy angle. Same shape as FollowNumbersInOrderScreen (reuses MazeCorridorRenderer for the
    // backdrop, tap-in-order over a shuffled checkpoint layout), but letters carry no self-evident order the way
    // numbers do, so Eva speaks the target sequence aloud before every round instead of leaving it to be read
    // off the tiles - the letters themselves are drawn as sprites (never TMP_Text), so nothing here is "reading"
    // in the sense the no-reading audit cares about; the point is matching a shown shape to a heard name.
    public sealed class FollowLettersInOrderScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }


        private const int MaxCheckpoints = 8;
        private const float CheckpointSize = EvaUi.MinTap; // overhangs a single corridor cell; see FingerMazeScreen's CharacterSize note

        private const float HandMoveSeconds = 0.4f;
        private const float HandTapSeconds = 0.3f;
        private const float WobbleSeconds = 0.4f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _mazeField, _corridorField, _checkpointField;
        private GameObject _endPanel;

        private RectTransform[] _checkpointTiles;
        private Image[] _checkpointImages;
        private Button[] _checkpointButtons;
        private bool[] _checkpointDone;
        private int _activeCount;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private FollowLettersInOrderRound _round;
        private HelpLadder _ladder;
        private int _nextIndex;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _mazeField = CreateFullRectContainer("MazeField");
            _corridorField = CreateFullRectContainer("CorridorField", _mazeField);
            _checkpointField = CreateFullRectContainer("CheckpointField", _mazeField);
            BuildCheckpointTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.FollowLettersInOrder);
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
            _round = FollowLettersInOrderRoundGenerator.Create(_game.Progress.FollowLettersLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _nextIndex = 0;

            MazeCorridorRenderer.Draw(_corridorField, _round.Path, withFinishFlag: false);
            ShowRoundCheckpoints(_round);
            SetAllCheckpointsInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("followletters_find");
            foreach (var letter in _round.SortedLetters) yield return _game.Voice.SayAndWait(LetterVoiceKey(letter));
            _eva.SetTalking(false);
            SetAllCheckpointsInteractable(true);
        }

        private static string LetterVoiceKey(char letter) => "letter_" + char.ToLowerInvariant(letter);

        // --- Checkpoint tiles ---------------------------------------------------------------------------------

        private void BuildCheckpointTiles()
        {
            _checkpointTiles = new RectTransform[MaxCheckpoints];
            _checkpointImages = new Image[MaxCheckpoints];
            _checkpointButtons = new Button[MaxCheckpoints];

            for (var i = 0; i < MaxCheckpoints; i++)
            {
                var tile = new GameObject("Checkpoint" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_checkpointField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(CheckpointSize, CheckpointSize);

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var checkpointIndex = i;
                button.onClick.AddListener(() => OnCheckpointTapped(checkpointIndex));

                _checkpointTiles[i] = rect;
                _checkpointImages[i] = image;
                _checkpointButtons[i] = button;
            }
        }

        private void ShowRoundCheckpoints(FollowLettersInOrderRound round)
        {
            StopTilePulse();
            _activeCount = round.CheckpointPositions.Length;
            _checkpointDone = new bool[MaxCheckpoints];
            for (var i = 0; i < MaxCheckpoints; i++)
            {
                _checkpointTiles[i].gameObject.SetActive(i < _activeCount);
                if (i >= _activeCount) continue;
                var position = round.CheckpointPositions[i];
                _checkpointTiles[i].anchoredPosition = new Vector2(position.X, position.Y);
                _checkpointTiles[i].localScale = Vector3.one;
                _checkpointImages[i].color = Color.white;
                _checkpointImages[i].sprite = EvaUi.Sprite("letters/" + char.ToLowerInvariant(round.CheckpointLetters[i]));
                _checkpointButtons[i].interactable = false;
            }
        }

        private void SetAllCheckpointsInteractable(bool interactable)
        {
            for (var i = 0; i < _activeCount; i++)
                if (!_checkpointDone[i]) _checkpointButtons[i].interactable = interactable;
        }

        private void StopTilePulse()
        {
            if (_tilePulseRoutine != null)
            {
                _runner.StopCoroutine(_tilePulseRoutine);
                _tilePulseRoutine = null;
            }
            if (_checkpointTiles != null)
                foreach (var tile in _checkpointTiles) tile.localScale = Vector3.one;
        }

        private int IndexOfCheckpointLetter(char letter)
        {
            for (var i = 0; i < _activeCount; i++) if (_round.CheckpointLetters[i] == letter) return i;
            return 0;
        }

        // --- Taps ----------------------------------------------------------------------------------------------

        private void OnCheckpointTapped(int i)
        {
            if (_round == null || _roundOver || _checkpointDone[i] || !_checkpointButtons[i].interactable) return;
            if (_round.CheckpointLetters[i] == _round.SortedLetters[_nextIndex]) _runner.StartCoroutine(OnCorrectTap(i));
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
                    _runner.StartCoroutine(Wobble(_checkpointTiles[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the next correct checkpoint pulses while Eva names it.
        private IEnumerator RunHint()
        {
            SetAllCheckpointsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("followletters_hint");
            yield return new WaitForSeconds(_game.Voice.Duration("followletters_hint") + Voice.BreathSeconds);

            var nextLetter = _round.SortedLetters[_nextIndex];
            var targetIndex = IndexOfCheckpointLetter(nextLetter);
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_checkpointTiles[targetIndex]));
            yield return _game.Voice.SayAndWait(LetterVoiceKey(nextLetter));
            _eva.SetTalking(false);
            StopTilePulse();
            SetAllCheckpointsInteractable(true);
        }

        // 3rd mistake: the hand moves to each remaining checkpoint in order, Eva names it, then it taps -
        // already-tapped checkpoints stay done - then the child repeats what is left.
        private IEnumerator RunDemonstrate()
        {
            SetAllCheckpointsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("followletters_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("followletters_demo") + Voice.BreathSeconds);

            for (var n = _nextIndex; n < _round.SortedLetters.Length; n++)
            {
                var letter = _round.SortedLetters[n];
                var index = IndexOfCheckpointLetter(letter);
                yield return _hand.MoveTo(_checkpointTiles[index].anchoredPosition, HandMoveSeconds);
                yield return _game.Voice.SayAndWait(LetterVoiceKey(letter));
                yield return _hand.Tap(HandTapSeconds);
            }
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllCheckpointsInteractable(true);
        }

        private IEnumerator OnCorrectTap(int i)
        {
            _checkpointDone[i] = true;
            _checkpointButtons[i].interactable = false;
            _game.Sfx.Tap();
            _runner.StartCoroutine(PopPulse(_checkpointTiles[i], _checkpointImages[i], 1.15f, 0.25f));
            _nextIndex++;

            if (_nextIndex < _round.SortedLetters.Length) yield break;

            _roundOver = true;
            SetAllCheckpointsInteractable(false);
            StopTilePulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.FollowLettersLevel = DifficultyLadder.RecordRound(_game.Progress.FollowLettersBuffer, _game.Progress.FollowLettersLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _checkpointTiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= FollowLettersInOrderRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _mazeField.gameObject.SetActive(!ended);
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

        private RectTransform CreateFullRectContainer(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : Root, false);
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
