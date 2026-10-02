using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's eleventh game (spec 4.1: Collect Everything). Wired into Activities.cs.
    //
    // Reuses Finger Maze's own drag mechanic and corridor exactly (PathDragger, MazeCorridorRenderer,
    // CollectEverythingRoundGenerator.Create just wraps FingerMazeRoundGenerator.Create), then marks a few of the
    // path's own interior waypoints as pickups (Rules/CollectEverything.cs). Dragging the character past a
    // pickup's own fraction collects it automatically - no PathDragger.RawMoved needed here (unlike Avoid
    // Obstacles), since pickups sit ON the corridor rather than beside it. Reaching the finish before every
    // pickup is collected is this game's "mistake" (soft Retry -> Hint -> Demonstrate, same ladder as everything
    // else): Hint pulses the nearest uncollected pickup, Demo has the hand collect just that one pickup itself
    // (not the whole route, since collecting is cumulative) before handing control back for the child to finish
    // the rest, exactly per the plan's own note for this game.
    public sealed class CollectEverythingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float CharacterSize = EvaUi.MinTap;
        private const int MaxPickups = 4;
        private const float PickupTileSize = 110f;
        private const float PickupPopSeconds = 0.25f;

        private const float HintPulseSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;

        // A drag counts as reaching the finish once it snaps this close to it (see FingerMazeScreen for why).
        private const float FinishFraction = 0.97f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _mazeField, _corridorField, _pickupField;
        private PathDragger _dragger;
        private Image[] _pickupImages;
        private bool[] _collected;
        private Coroutine _pickupPulseRoutine;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private bool _finishTripped;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private CollectEverythingRound _round;
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
            _pickupField = CreateFullRectContainer("PickupField", _mazeField);
            BuildPickupTiles();
            _dragger = PathDragger.Create(_mazeField, EvaUi.Sprite("fingermaze/character"), CharacterSize);
            _dragger.Progressed += OnDraggerProgressed;
            _dragger.Released += OnDraggerReleased;
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.CollectEverything);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopPickupPulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = CollectEverythingRoundGenerator.Create(_game.Progress.CollectEverythingLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _finishTripped = false;
            _collected = new bool[MaxPickups];

            MazeCorridorRenderer.Draw(_corridorField, _round.Path);
            ShowRoundPickups(_round);
            _dragger.SetPath(_round.Path);
            _dragger.SnapTo(0f);
            _dragger.Enabled = false;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("collecteverything_find");
            _eva.SetTalking(false);
            _dragger.Enabled = true;
        }

        // --- Pickups ------------------------------------------------------------------------------------------

        private void BuildPickupTiles()
        {
            _pickupImages = new Image[MaxPickups];
            for (var i = 0; i < MaxPickups; i++)
            {
                var tile = new GameObject("Pickup" + i, typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(_pickupField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(PickupTileSize, PickupTileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("collecteverything/pickup");
                image.preserveAspect = true;
                image.raycastTarget = false;

                _pickupImages[i] = image;
            }
        }

        private void ShowRoundPickups(CollectEverythingRound round)
        {
            StopPickupPulse();
            var count = round.Pickups.Length;
            for (var i = 0; i < MaxPickups; i++)
            {
                var active = i < count;
                _pickupImages[i].gameObject.SetActive(active);
                if (!active) continue;
                _pickupImages[i].color = Color.white;
                var rect = (RectTransform)_pickupImages[i].transform;
                rect.anchoredPosition = new Vector2(round.Pickups[i].X, round.Pickups[i].Y);
                rect.localScale = Vector3.one;
            }
        }

        private void CollectPickup(int i)
        {
            if (_collected[i]) return;
            _collected[i] = true;
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopAndHide(_pickupImages[i]));
        }

        private static IEnumerator PopAndHide(Image image)
        {
            var rect = (RectTransform)image.transform;
            for (var t = 0f; t < PickupPopSeconds; t += Time.deltaTime)
            {
                var k = t / PickupPopSeconds;
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, k);
                var color = image.color;
                color.a = Mathf.Lerp(1f, 0f, k);
                image.color = color;
                yield return null;
            }
            image.gameObject.SetActive(false);
            rect.localScale = Vector3.one;
            var reset = image.color;
            reset.a = 1f;
            image.color = reset;
        }

        private bool AllCollected()
        {
            for (var i = 0; i < _round.Pickups.Length; i++) if (!_collected[i]) return false;
            return true;
        }

        // Pickups are reported in path order (ascending fraction), so the first uncollected one is always the
        // nearest one still ahead.
        private int NearestUncollectedIndex()
        {
            for (var i = 0; i < _round.Pickups.Length; i++)
                if (!_collected[i]) return i;
            return -1;
        }

        private void StopPickupPulse()
        {
            if (_pickupPulseRoutine != null)
            {
                _runner.StopCoroutine(_pickupPulseRoutine);
                _pickupPulseRoutine = null;
            }
            if (_pickupImages != null)
                foreach (var image in _pickupImages) if (image != null) image.transform.localScale = Vector3.one;
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnDraggerProgressed(float fraction)
        {
            if (_round == null || _roundOver) return;
            for (var i = 0; i < _round.Pickups.Length; i++)
                if (!_collected[i] && fraction >= _round.PickupFractions[i] - 0.001f) CollectPickup(i);

            if (fraction >= FinishFraction)
            {
                if (AllCollected()) _runner.StartCoroutine(OnReachedFinish());
                else if (!_finishTripped)
                {
                    _finishTripped = true;
                    HandleMistake();
                }
            }
            else
            {
                _finishTripped = false;
            }
        }

        private void OnDraggerReleased()
        {
            if (_round == null || _roundOver) return;
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

        // 2nd mistake: the nearest uncollected pickup pulses (per the plan's Hint note).
        private IEnumerator RunHint()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("collecteverything_hint");
            var lead = _game.Voice.Duration("collecteverything_hint") + Voice.BreathSeconds;

            var index = NearestUncollectedIndex();
            if (index >= 0) _pickupPulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_pickupImages[index]));
            yield return new WaitForSeconds(Mathf.Max(lead, HintPulseSeconds));
            _eva.SetTalking(false);
            StopPickupPulse();
            _finishTripped = false;
            _dragger.Enabled = true;
        }

        // 3rd mistake: the hand collects just the nearest uncollected pickup to show the motion, then hands
        // control back for the child to finish the rest (per the plan's Demo note) - it never retraces the
        // whole route, since collecting is cumulative and every already-collected pickup must stay collected.
        private IEnumerator RunDemonstrate()
        {
            _dragger.Enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("collecteverything_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("collecteverything_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var index = NearestUncollectedIndex();
            if (index >= 0)
            {
                var pickup = _round.Pickups[index];
                yield return _hand.MoveTo(new Vector2(pickup.X, pickup.Y), HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                CollectPickup(index);
            }
            _hand.Hide();
            _finishTripped = false;
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

            _game.Progress.CollectEverythingLevel = DifficultyLadder.RecordRound(_game.Progress.CollectEverythingBuffer, _game.Progress.CollectEverythingLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _dragger.Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= CollectEverythingRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private static IEnumerator IdlePulseLoop(Image image) => IdlePulseLoop((RectTransform)image.transform);

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
