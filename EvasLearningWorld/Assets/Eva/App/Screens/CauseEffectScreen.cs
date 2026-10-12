using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Cause and Effect: "Make it happen". One thing waits on the meadow (a sprout, an ice cream, dry ground, a blank paper) and
    // Eva asks the child to change it ("Can you make the plant grow?"). Three round tool badges lie along the bottom, one is the cause. The
    // child drags the right one onto the thing: the badge flies in and the thing changes into its after picture (a cross-fade with a little
    // pop). A wrong tool goes back to the tray and Eva says what it is for. Rules: Rules/CauseEffect.cs; this draws them.
    //
    // Two wrong tools in a row, or a long quiet: the hand carries the right tool onto the thing (the only help; the coin is then Assisted).
    //
    // Layout in "picture space" like DayNightScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py).
    public sealed class CauseEffectScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Tool
        {
            public int Index;
            public string Id;
            public DragItem Drag;
            public RectTransform Art;
            public Image ArtImage;
            public Vector2 Home;
            public bool Held, Flying;
            public float Wobble;
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);
        public const float EvaHeight = 420f;
        public static readonly Vector2 ThingPoint = new Vector2(120f, 10f);       // the middle of the thing that changes
        public const float ThingSize = 480f;
        public const float DropReach = 300f;                                       // a tool let go this close to the thing is on it
        public const float ToolTapSize = 240f;
        private const float ToolArt = 200f;
        private const float TrayY = -345f;
        private const float TrayStep = 300f;
        private const float ReturnSpeed = 2600f;
        private const float ChangeSeconds = 0.8f;
        private const float FlySeconds = 0.5f;

        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };
        private const float MusicLevel = 0.3f;

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _background;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _trayField, _thingField;
        private Image _thingBefore, _thingAfter;
        private readonly List<Tool> _tools = new List<Tool>();
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private CauseRound _round;
        private CauseRound[] _session;
        private int _roundIndex, _rightLineIndex, _flying;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing, _changed;
        private float _idle, _time, _change;      // _change 0..1: how far the thing has turned into its after picture
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public CauseEffectScreen(ScreenId selfId, ScreenId homeScreenId, string background)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _background = background;
        }

        // Read-only state for tests.
        public CauseRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public bool Changed => _changed;
        public IReadOnlyList<DragItem> ToolDrags
        {
            get { var list = new List<DragItem>(); foreach (var tool in _tools) list.Add(tool.Drag); return list; }
        }

        // The tool's middle, in picture space; PlaceTool puts it there.
        public Vector2 ToolPoint(int index) => Scene(_tools[index].Drag.Rect.anchoredPosition);
        public void PlaceTool(int index, Vector2 point) => _tools[index].Drag.Rect.anchoredPosition = Place(point);

        public static bool UseDevicePlacement = true;
        private float _scaleX = 1f;
        private Vector2 _shift;

        private Vector2 Place(Vector2 scene) => new Vector2(scene.x * _scaleX - _shift.x, scene.y);

        private Vector2 Scene(Vector2 local) => new Vector2((local.x + _shift.x) / _scaleX, local.y);

        private void UpdatePlacement()
        {
            _scaleX = 1f;
            _shift = Vector2.zero;
            var canvas = UseDevicePlacement ? Root.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var size = ((RectTransform)canvas.rootCanvas.transform).rect.size;
                if (size.x > 0f) _scaleX = size.x / PictureWidth;
                _shift = FullBleed.CentreShift(FullBleed.CurrentInsets(Root));
            }
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            AddPictureBackground();
            BuildEva();
            _thingField = Container("Thing");
            BuildThing();
            _trayField = Container("Tray");
            BuildTray();
            _hand = new PointerHand(Root, _runner);
            BuildConfetti();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            if (_game.Music != null) _game.Music.Level = MusicLevel;
            UpdatePlacement();
            StartNewSession();
        }

        private void StartNewSession()
        {
            _session = CauseEffect.CreateSession(new System.Random());
            _roundIndex = 0;
            _rightLineIndex = 0;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StopAllCoroutines();
            _runner.StartCoroutine(Loop());
            StartRound();
        }

        private void StartRound()
        {
            _round = _session[_roundIndex];
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _changed = false;
            _flying = 0;
            _change = 0f;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            _thingBefore.sprite = EvaUi.Sprite("sciencelab/causebefore_" + _round.Pair.Id);
            _thingAfter.sprite = EvaUi.Sprite("sciencelab/causeafter_" + _round.Pair.Id);
            ShowThing(0f, 0f);
            for (var i = 0; i < _tools.Count; i++)
            {
                var tool = _tools[i];
                tool.Id = _round.Tools[i];
                tool.ArtImage.sprite = EvaUi.Sprite("sciencelab/causetool_" + tool.Id);
                tool.Drag.gameObject.SetActive(true);
                tool.Held = false;
                tool.Flying = false;
                tool.Wobble = 0f;
                tool.Drag.Rect.anchoredPosition = Place(tool.Home);
                tool.Art.localScale = Vector3.one;
                tool.Art.localRotation = Quaternion.identity;
            }
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("ce_ask_" + _round.Pair.Id);
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            var go = new GameObject("EvaAnchor", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _evaAnchor = (RectTransform)go.transform;
            _evaAnchor.anchorMin = _evaAnchor.anchorMax = _evaAnchor.pivot = new Vector2(0.5f, 0.5f);
            _evaAnchor.sizeDelta = Vector2.zero;
            _eva = RigFactory.CreateEva(_evaAnchor, EvaHeight);
            _evaBaseScale = _eva.Root.localScale;
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        // The before and the after picture lie on top of each other; the change fades the after one in.
        private void BuildThing()
        {
            _thingBefore = ThingImage("Before");
            _thingAfter = ThingImage("After");
        }

        private Image ThingImage(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_thingField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ThingSize, ThingSize);
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void BuildTray()
        {
            var first = -TrayStep * (CauseEffect.ToolsPerRound - 1) * 0.5f;
            for (var i = 0; i < CauseEffect.ToolsPerRound; i++)
            {
                var tool = new Tool { Index = i, Home = new Vector2(first + i * TrayStep, TrayY) };
                tool.Drag = DragItem.Create(_trayField, "Tool" + i, EvaUi.Sprite("icons/dot"), Place(tool.Home), ToolTapSize);
                tool.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = tool;
                tool.Drag.BeginDrag += _ => OnBeginDrag(captured);
                tool.Drag.EndDrag += _ => OnEndDrag(captured);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(tool.Drag.transform, false);
                tool.Art = (RectTransform)go.transform;
                tool.Art.anchorMin = tool.Art.anchorMax = tool.Art.pivot = new Vector2(0.5f, 0.5f);
                tool.Art.anchoredPosition = Vector2.zero;
                tool.Art.sizeDelta = new Vector2(ToolArt, ToolArt);
                tool.ArtImage = go.GetComponent<Image>();
                tool.ArtImage.preserveAspect = true;
                tool.ArtImage.raycastTarget = false;
                _tools.Add(tool);
            }
        }

        private void BuildConfetti()
        {
            // An inactive WinCelebration: switching it on plays the fanfare and bursts the confetti.
            _confetti = new GameObject("RoundConfetti", typeof(RectTransform), typeof(WinCelebration));
            _confetti.transform.SetParent(Root, false);
            var rect = (RectTransform)_confetti.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _confetti.SetActive(false);
        }

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(WinCelebration));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));
            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _thingField.gameObject.SetActive(!ended);
            _trayField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The thing --------------------------------------------------------------------------------------------------

        // change 0..1: the after picture's opacity; pop: how much the thing is swollen right now.
        private void ShowThing(float change, float pop)
        {
            var place = Place(ThingPoint);
            foreach (var image in new[] { _thingBefore, _thingAfter })
            {
                image.rectTransform.anchoredPosition = place;
                image.rectTransform.localScale = Vector3.one * (1f + pop);
            }
            _thingBefore.color = new Color(1f, 1f, 1f, 1f);
            _thingAfter.color = new Color(1f, 1f, 1f, change);
        }

        // --- The tools --------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Tool tool)
        {
            if (_roundOver || tool.Flying) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a tool never waits for the hint hand
            tool.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(Tool tool)
        {
            tool.Held = false;
            _idle = 0f;
            if (_roundOver || tool.Flying) return;
            if (Vector2.Distance(Scene(tool.Drag.Rect.anchoredPosition), ThingPoint) <= DropReach) DropTool(tool);
        }

        // The tool was let go on the thing (also what the hint does).
        private void DropTool(Tool tool)
        {
            var result = _round.Drop(tool.Index);
            Haptics.Tap();
            if (result == CauseDrop.Right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                _runner.StartCoroutine(MakeItHappen(tool));
                return;
            }
            if (result == CauseDrop.Wrong)
            {
                tool.Wobble = 0.4f;
                _runner.StartCoroutine(AfterWrong(tool.Id));
            }
        }

        // A tool that does not make it happen: Eva says what it is for, then asks again (or the hand helps).
        private IEnumerator AfterWrong(string toolId)
        {
            Say("ce_bad_" + toolId);
            while (_announcing) yield return null;
            if (_round.Done) yield break;
            if (_round.NeedsHint) StartHint();
            else Say("ce_ask_" + _round.Pair.Id);
        }

        // The right tool flies to the thing, shrinks away, and the thing changes.
        private IEnumerator MakeItHappen(Tool tool)
        {
            tool.Flying = true;
            _flying++;
            var from = tool.Drag.Rect.anchoredPosition;
            var to = Place(ThingPoint);
            for (var t = 0f; t < FlySeconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / FlySeconds);
                tool.Drag.Rect.anchoredPosition = Vector2.Lerp(from, to, k);
                tool.Art.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, k);
                yield return null;
            }
            tool.Drag.gameObject.SetActive(false);
            _changed = true;
            Say("ce_say_" + _round.Pair.Id);
            for (var t = 0f; t < ChangeSeconds; t += Time.deltaTime)
            {
                _change = t / ChangeSeconds;
                yield return null;
            }
            _change = 1f;
            _flying--;
        }

        // --- The loop ---------------------------------------------------------------------------------------------------

        private IEnumerator Loop()
        {
            while (true)
            {
                Tick(Time.deltaTime);
                yield return null;
            }
        }

        // One frame. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_round == null || _sessionEnded) return;
            _time += seconds;
            foreach (var tool in _tools) UpdateTool(tool, seconds);
            UpdateThing();
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdateTool(Tool tool, float seconds)
        {
            if (tool.Flying) return;
            if (tool.Wobble > 0f)
            {
                tool.Wobble = Mathf.Max(0f, tool.Wobble - seconds);
                tool.Art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tool.Wobble * 50f) * 14f * tool.Wobble * 2f);
            }
            else
            {
                tool.Art.localRotation = Quaternion.Euler(0f, 0f, tool.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
            }
            tool.Art.localScale = Vector3.one * (tool.Held ? 1.12f : 1f);
            if (!tool.Held && !_helpRunning)
            {
                var target = Place(tool.Home);
                var position = tool.Drag.Rect.anchoredPosition;
                if (position != target) tool.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
            }
        }

        // The thing waits with a gentle breath; when it changes it pops (swells and settles) while the after picture fades in.
        private void UpdateThing()
        {
            var pop = _change > 0f && _change < 1f ? Mathf.Sin(_change * Mathf.PI) * 0.18f : 0f;
            var breath = _change <= 0f ? Mathf.Sin(_time * 2f) * 0.015f : 0f;
            ShowThing(_change, pop + breath);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
        }

        // --- Voice ------------------------------------------------------------------------------------------------------

        private void Say(string key)
        {
            if (_announceQueue.Count >= 3) return; // never a pile of lines behind the child
            _announceQueue.Enqueue(key);
            if (!_announcing) _runner.StartCoroutine(AnnounceLoop());
        }

        private IEnumerator AnnounceLoop()
        {
            _announcing = true;
            _eva.SetTalking(true);
            while (_announceQueue.Count > 0)
            {
                var key = _announceQueue.Dequeue();
                yield return _game.Voice.SayAndWait(key);
            }
            _eva.SetTalking(false);
            _announcing = false;
        }

        // --- Hint -------------------------------------------------------------------------------------------------------

        private void UpdateIdle(float seconds)
        {
            if (_roundOver || _helpRunning) { _idle = 0f; return; }
            foreach (var tool in _tools) if (tool.Held) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        private void StartHint()
        {
            if (_helpRunning || _roundOver || _round.Done) return;
            _idle = 0f;
            _round.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint());
        }

        // The child took hold of a tool while the hint hand was out: the hint stops and the tool goes home.
        private void CancelHint()
        {
            if (_hintRoutine != null) _runner.StopCoroutine(_hintRoutine);
            _hintRoutine = null;
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _game.Voice.Stop();
            _announceQueue.Clear();
            _announcing = false;
            _helpRunning = false;
            _idle = 0f;
        }

        // The hand carries the right tool onto the thing.
        private IEnumerator RunHint()
        {
            var tool = _tools[_round.RightTool];
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("dressforweather_hint");

            var from = tool.Drag.Rect.anchoredPosition;
            var to = Place(ThingPoint);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                tool.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            tool.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            DropTool(tool);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_round.Done || _announcing || _flying > 0) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
            yield return new WaitForSeconds(0.6f); // a moment to look at the changed thing
            _confetti.SetActive(true); // the fanfare and a burst of confetti
            Haptics.Win();
            _eva.Cheer();
            _rightLineIndex = _rightLineIndex % 3 + 1;
            var payout = _hinted ? CoinPayout.Assisted : CoinPayout.Clean;
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            _runner.StartCoroutine(AnimateCoins(before, before + payout));
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= CauseEffect.RoundsPerSession)
            {
                yield return _game.Voice.SayAndWait("count_done");
                _eva.Cheer();
                SetSessionEnded(true);
                yield return _game.Voice.SayAndWait("count_again");
            }
            else StartRound();
        }

        private IEnumerator AnimateCoins(int before, int after)
        {
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _evaAnchor.position);
        }

        // --- Small helpers ----------------------------------------------------------------------------------------------

        private RectTransform Container(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.sprite = EvaUi.Sprite(_background);
        }
    }
}
