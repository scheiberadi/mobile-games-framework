using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's tenth game (spec 4.1: Avoid Obstacles). Wired into Activities.cs.
    //
    // Reuses Finger Maze's own drag mechanic and corridor exactly (PathDragger, MazeCorridorRenderer,
    // AvoidObstaclesRoundGenerator.Create just wraps FingerMazeRoundGenerator.Create), but scatters a few hazard
    // tiles beside the corridor (Rules/AvoidObstacles.cs). Straying the finger onto one is this game's "mistake" -
    // same soft Retry -> Hint -> Demonstrate ladder as every other Playground game, never a hard fail state (per
    // the plan's own note: hazards end the round softly "instead of just walls"). Detecting a stray finger needs
    // the RAW drag point rather than PathDragger's snapped Fraction (which always sits on the centreline, by
    // design) - see PathDragger.RawMoved, added for exactly this game.
    public sealed class AvoidObstaclesScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float CharacterSize = EvaUi.MinTap;
        private const int MaxHazards = 3;
        private const float HazardTileSize = 110f;

        // A raw finger position within this many units of a hazard's centre counts as straying onto it. Kept
        // comfortably smaller than the one-grid-step (130) distance every hazard sits from the corridor, so
        // dragging cleanly along the centreline never trips one by accident - a placeholder value pending a real
        // art/feel pass, same as every other hand-picked constant this session (TileLayout, Places, etc.).
        private const float HazardRadius = 70f;

        private const float HintFlashSeconds = 1.2f;
        private const float HintRestSeconds = 0.4f;
        private const float DemoUnitsPerSecond = 260f;
        private const float MinDemoSeconds = 1.2f;
        private const float MaxDemoSeconds = 4.5f;

        // A drag counts as reaching the finish once it snaps this close to it (see FingerMazeScreen for why).
        private const float FinishFraction = 0.97f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _mazeField, _corridorField, _hazardField;
        private PathDragger _dragger;
        private Image[] _hazardImages;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private bool _hazardTripped;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private AvoidObstaclesRound _round;
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
            _hazardField = CreateFullRectContainer("HazardField", _mazeField);
            BuildHazardTiles();
            _dragger = PathDragger.Create(_mazeField, EvaUi.Sprite("fingermaze/character"), CharacterSize);
            _dragger.Progressed += OnDraggerProgressed;
            _dragger.Released += OnDraggerReleased;
            _dragger.RawMoved += OnDraggerRawMoved;
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.AvoidObstacles);
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
            _round = AvoidObstaclesRoundGenerator.Create(_game.Progress.AvoidObstaclesLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _hazardTripped = false;

            MazeCorridorRenderer.Draw(_corridorField, _round.Path);
            ShowRoundHazards(_round);
            _dragger.SetPath(_round.Path);
            _dragger.SnapTo(0f);
            _dragger.Enabled = false;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("avoidobstacles_find");
            _eva.SetTalking(false);
            _dragger.Enabled = true;
        }

        // --- Hazards ----------------------------------------------------------------------------------------

        private void BuildHazardTiles()
        {
            _hazardImages = new Image[MaxHazards];
            for (var i = 0; i < MaxHazards; i++)
            {
                var tile = new GameObject("Hazard" + i, typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(_hazardField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(HazardTileSize, HazardTileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("avoidobstacles/hazard");
                image.preserveAspect = true;
                image.raycastTarget = false;

                _hazardImages[i] = image;
            }
        }

        private void ShowRoundHazards(AvoidObstaclesRound round)
        {
            var count = round.Hazards.Length;
            for (var i = 0; i < MaxHazards; i++)
            {
                var active = i < count;
                _hazardImages[i].gameObject.SetActive(active);
                if (!active) continue;
                _hazardImages[i].color = Color.white;
                ((RectTransform)_hazardImages[i].transform).anchoredPosition = new Vector2(round.Hazards[i].X, round.Hazards[i].Y);
            }
        }

        private void OnDraggerRawMoved(WorldPoint point)
        {
            if (_round == null || _roundOver || _hazardTripped) return;
            foreach (var hazard in _round.Hazards)
            {
                var dx = point.X - hazard.X;
                var dy = point.Y - hazard.Y;
                if (dx * dx + dy * dy <= HazardRadius * HazardRadius)
                {
                    _hazardTripped = true;
                    OnHitHazard();
                    return;
                }
            }
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnDraggerProgressed(float fraction)
        {
            if (_round == null || _roundOver) return;
            if (fraction >= FinishFraction) _runner.StartCoroutine(OnReachedFinish());
        }

        private void OnDraggerReleased()
        {
            if (_round == null || _roundOver) return;
            HandleMistake();
        }

        // Straying onto a hazard is a harder reset than just letting go (FingerMazeScreen's own "mistake"): the
        // character snaps straight back to the start, so every attempt after a hazard begins the corridor fresh.
        private void OnHitHazard()
        {
            _dragger.Enabled = false;
            _dragger.SnapTo(0f);
            _eva.Angry();
            HandleMistake();
        }

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _hazardTripped = false;
                    _dragger.Enabled = true;
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

        // 2nd mistake: every hazard tile flashes briefly (per the plan's Hint note).
        private IEnumerator RunHint()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("avoidobstacles_hint");
            var lead = _game.Voice.Duration("avoidobstacles_hint") + Voice.BreathSeconds;

            _runner.StartCoroutine(FlashHazards(Mathf.Max(lead, HintFlashSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintFlashSeconds);
            _hazardTripped = false;
            _dragger.Enabled = true;
        }

        private IEnumerator FlashHazards(float duration)
        {
            var flashColor = new Color(1f, 0.35f, 0.35f, 1f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var k = Mathf.PingPong(t * 3f, 1f);
                foreach (var image in _hazardImages)
                    if (image != null && image.gameObject.activeSelf) image.color = Color.Lerp(Color.white, flashColor, k);
                yield return null;
            }
            foreach (var image in _hazardImages) if (image != null) image.color = Color.white;
        }

        // 3rd mistake: the hand drags the character safely through the whole maze from the start, then resets
        // the character to start so the child repeats the now-demonstrated route themselves.
        private IEnumerator RunDemonstrate()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("avoidobstacles_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("avoidobstacles_demo") + Voice.BreathSeconds);
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
            _hazardTripped = false;
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

            _game.Progress.AvoidObstaclesLevel = DifficultyLadder.RecordRound(_game.Progress.AvoidObstaclesBuffer, _game.Progress.AvoidObstaclesLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _dragger.Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= AvoidObstaclesRoundGenerator.RoundsPerSession) yield return EndSession();
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
