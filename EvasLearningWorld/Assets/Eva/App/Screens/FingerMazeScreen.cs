using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's sixth game (spec 4.1, build order: Finger Maze is 6th) - the first of the plan's NAVIGATION
    // games. Drag the character along a corridor from start to finish; no timer, no way to fail mid-drag, only
    // whether the child keeps dragging until the finish or lets go early. See FingerMazeRoundGenerator and
    // FingerMazePath (Rules/FingerMaze.cs) for the maze/drag math, and PathDragger (App/Ui) for the reusable
    // drag-along-a-path component every later NAVIGATION game (Follow Numbers/Letters in Order, Shortest Path,
    // Avoid Obstacles, Collect Everything) is expected to share.
    public sealed class FingerMazeScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // Must satisfy the no-reading audit's 240-unit TapTarget floor even though it visually overhangs a
        // single corridor cell (CellSize=130) - a later art pass tunes the placeholder art, not this size.
        private const float CharacterSize = EvaUi.MinTap;

        private const float HintGlowSeconds = 1.2f;
        private const float HintRestSeconds = 0.4f;
        private const float DemoUnitsPerSecond = 260f;
        private const float MinDemoSeconds = 1.2f;
        private const float MaxDemoSeconds = 4.5f;

        // A drag counts as reaching the finish once it snaps this close to it; a finger rarely lands on the
        // exact last point, and there is nothing past the finish to keep dragging toward.
        private const float FinishFraction = 0.97f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _mazeField, _corridorField;
        private PathDragger _dragger;
        private GameObject _endPanel;

        private readonly List<Image> _segmentImages = new List<Image>();
        private float[] _waypointFractions;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private FingerMazeRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _mazeField = CreateFullRectContainer("MazeField");
            _corridorField = CreateFullRectContainer("CorridorField", _mazeField);
            _dragger = PathDragger.Create(_mazeField, EvaUi.Sprite("fingermaze/character"), CharacterSize);
            _dragger.Progressed += OnDraggerProgressed;
            _dragger.Released += OnDraggerReleased;
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.FingerMaze);
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
            _round = FingerMazeRoundGenerator.Create(_game.Progress.FingerMazeLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;

            DrawCorridor(_round.Path);
            _dragger.SetPath(_round.Path);
            _dragger.SnapTo(0f);
            _dragger.Enabled = true;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("fingermaze_find");
            _eva.SetTalking(false);
        }

        // --- Corridor -------------------------------------------------------------------------------------------

        private void DrawCorridor(WorldPoint[] path)
        {
            _segmentImages.Clear();
            _segmentImages.AddRange(MazeCorridorRenderer.Draw(_corridorField, path));
            _waypointFractions = MazeCorridorRenderer.WaypointFractions(path);
        }

        private int NextSegmentIndex()
        {
            var fraction = _dragger.Fraction;
            for (var i = 0; i < _waypointFractions.Length - 1; i++)
                if (fraction < _waypointFractions[i + 1] - 0.001f) return i;
            return Mathf.Max(0, _waypointFractions.Length - 2);
        }

        // --- Drag events ------------------------------------------------------------------------------------------

        private void OnDraggerProgressed(float fraction)
        {
            if (_round == null || _roundOver) return;
            if (fraction >= FinishFraction) _runner.StartCoroutine(OnReachedFinish());
        }

        private void OnDraggerReleased()
        {
            if (_round == null || _roundOver) return;
            OnGaveUpBeforeFinish();
        }

        // Letting go before the finish is this game's only "mistake": there is nothing to tap wrong, only
        // whether the child keeps going. Same Retry -> Hint -> Demonstrate ladder as every other Playground game.
        private void OnGaveUpBeforeFinish()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
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

        // 2nd mistake: the next stretch of corridor ahead of where the child let go glows.
        private IEnumerator RunHint()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("fingermaze_hint");
            var lead = _game.Voice.Duration("fingermaze_hint") + Voice.BreathSeconds;

            var segmentIndex = NextSegmentIndex();
            _runner.StartCoroutine(GlowSegment(segmentIndex, Mathf.Max(lead, HintGlowSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            _dragger.Enabled = true;
        }

        private IEnumerator GlowSegment(int index, float duration)
        {
            if (index < 0 || index >= _segmentImages.Count) yield break;
            var image = _segmentImages[index];
            var original = image.color;
            var glow = Color.Lerp(original, Color.white, 0.5f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        // 3rd mistake: the hand drags the character through the whole maze from the start, then resets the
        // character to start so the child repeats the now-highlighted path themselves.
        private IEnumerator RunDemonstrate()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("fingermaze_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("fingermaze_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            _dragger.SnapTo(0f);
            _hand.Rect.gameObject.SetActive(true);
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);

            var duration = Mathf.Clamp(MapPath.Length(_round.Path) / DemoUnitsPerSecond, MinDemoSeconds, MaxDemoSeconds);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                _dragger.SnapTo(Mathf.Clamp01(t / duration));
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);
                yield return null;
            }
            _dragger.SnapTo(1f);
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(_dragger.Rect.anchoredPosition);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Hide();

            _dragger.SnapTo(0f);
            _dragger.Enabled = true;
        }

        private IEnumerator OnReachedFinish()
        {
            _roundOver = true;
            _dragger.Enabled = false;
            _dragger.SnapTo(1f);

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_dragger.Rect, _dragger.Image, 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.FingerMazeLevel = DifficultyLadder.RecordRound(_game.Progress.FingerMazeBuffer, _game.Progress.FingerMazeLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _dragger.Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= FingerMazeRoundGenerator.RoundsPerSession) yield return EndSession();
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
