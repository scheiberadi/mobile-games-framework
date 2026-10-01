using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's ninth game (spec 4.1: Shortest Path). Wired into Activities.cs; the building menu scrolls
    // (TileLayout/BuildingScreen), so there's no ceiling on how many games a building can list.
    //
    // 2-3 routes (drawn with the shared MazeCorridorRenderer, same as Finger Maze/Follow Numbers/Follow Letters)
    // are shown side by side; the child taps the start of whichever looks shortest. Same TAP-THE-TARGET shell as
    // OddOneOutScreen (wrong taps are eliminated/dimmed, not just rejected), with route "start" tiles swapping in
    // for OddOneOut's item tiles. See ShortestPathRoundGenerator (Rules/ShortestPath.cs) for the route/length math.
    public sealed class ShortestPathScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxRoutes = 3;
        private const float TileSize = EvaUi.MinTap;
        private const float WobbleSeconds = 0.4f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _routesField;
        private RectTransform[] _routeFields;
        private RectTransform _startField;
        private GameObject _endPanel;

        private RectTransform[] _startTiles;
        private Image[] _startImages;
        private Button[] _startButtons;
        private bool[] _routeTried;
        private Coroutine _startPulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private ShortestPathRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _routesField = CreateFullRectContainer("RoutesField");
            BuildRoutes();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.ShortestPath);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopStartPulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = ShortestPathRoundGenerator.Create(_game.Progress.ShortestPathLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowRoundRoutes(_round);
            SetRoutesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("shortestpath_find");
            _eva.SetTalking(false);
            SetRoutesInteractable(true);
        }

        // --- Routes and start tiles -------------------------------------------------------------------------

        private void BuildRoutes()
        {
            _routeFields = new RectTransform[MaxRoutes];
            for (var i = 0; i < MaxRoutes; i++)
                _routeFields[i] = CreateFullRectContainer("RouteField" + i, _routesField);

            // Created after the route fields so its tap targets draw on top of the corridor's waypoint tiles.
            _startField = CreateFullRectContainer("StartField", _routesField);

            _startTiles = new RectTransform[MaxRoutes];
            _startImages = new Image[MaxRoutes];
            _startButtons = new Button[MaxRoutes];

            for (var i = 0; i < MaxRoutes; i++)
            {
                var tile = new GameObject("StartTile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_startField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("shortestpath/start");
                image.preserveAspect = true;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnRouteTapped(choiceIndex));

                _startTiles[i] = rect;
                _startImages[i] = image;
                _startButtons[i] = button;
            }
        }

        private void ShowRoundRoutes(ShortestPathRound round)
        {
            StopStartPulse();
            _routeTried = new bool[MaxRoutes];
            var count = round.Routes.Length;
            for (var i = 0; i < MaxRoutes; i++)
            {
                var active = i < count;
                _routeFields[i].gameObject.SetActive(active);
                _startTiles[i].gameObject.SetActive(active);
                if (!active) continue;

                MazeCorridorRenderer.Draw(_routeFields[i], round.Routes[i]);
                var start = round.Routes[i][0];
                _startTiles[i].anchoredPosition = new Vector2(start.X, start.Y);
                _startImages[i].color = Color.white;
                _startTiles[i].localScale = Vector3.one;
                _startButtons[i].interactable = false;
            }
        }

        private void SetRoutesInteractable(bool interactable)
        {
            for (var i = 0; i < _startButtons.Length; i++)
                if (!_routeTried[i]) _startButtons[i].interactable = interactable;
        }

        private void SetAllRoutesInteractable(bool interactable)
        {
            for (var i = 0; i < _startButtons.Length; i++)
                _startButtons[i].interactable = interactable;
        }

        private void SetOnlyRouteInteractable(int index)
        {
            for (var i = 0; i < _startButtons.Length; i++)
                _startButtons[i].interactable = i == index;
        }

        private void StopStartPulse()
        {
            if (_startPulseRoutine != null)
            {
                _runner.StopCoroutine(_startPulseRoutine);
                _startPulseRoutine = null;
            }
            if (_startTiles != null)
                foreach (var tile in _startTiles) if (tile != null) tile.localScale = Vector3.one;
        }

        private void OnRouteTapped(int i)
        {
            if (_round == null || _roundOver || !_startButtons[i].interactable) return;
            if (i == _round.ShortestIndex) _runner.StartCoroutine(OnCorrectRoute(i));
            else OnWrongRoute(i);
        }

        private void OnWrongRoute(int i)
        {
            _routeTried[i] = true;
            _eva.Angry();
            _startButtons[i].interactable = false;
            _startImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_startTiles[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the shortest route's start tile pulses in place (per the plan's Hint note).
        private IEnumerator RunHint()
        {
            SetAllRoutesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shortestpath_hint");
            var lead = _game.Voice.Duration("shortestpath_hint") + Voice.BreathSeconds;

            _startPulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_startTiles[_round.ShortestIndex]));
            yield return new WaitForSeconds(Mathf.Max(lead, HintRestSeconds));
            _eva.SetTalking(false);
            StopStartPulse();
            SetAllRoutesInteractable(true);
        }

        // 3rd mistake: the hand traces the shortest route from its start to its finish, then comes back and taps
        // that route's start tile, then only that tile stays interactable (per the plan's Demo note).
        private IEnumerator RunDemonstrate()
        {
            SetAllRoutesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shortestpath_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("shortestpath_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var route = _round.Routes[_round.ShortestIndex];
            yield return _hand.MoveTo(new Vector2(route[0].X, route[0].Y), HandMoveSeconds);
            for (var i = 1; i < route.Length; i++)
                yield return _hand.MoveTo(new Vector2(route[i].X, route[i].Y), HandMoveSeconds);

            yield return _hand.MoveTo(_startTiles[_round.ShortestIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();

            _startPulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_startTiles[_round.ShortestIndex]));
            SetOnlyRouteInteractable(_round.ShortestIndex);
        }

        private IEnumerator OnCorrectRoute(int i)
        {
            _roundOver = true;
            SetAllRoutesInteractable(false);
            StopStartPulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_startTiles[i], _startImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.ShortestPathLevel = DifficultyLadder.RecordRound(_game.Progress.ShortestPathBuffer, _game.Progress.ShortestPathLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _startTiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= ShortestPathRoundGenerator.RoundsPerSession) yield return EndSession();
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
            _routesField.gameObject.SetActive(!ended);
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
