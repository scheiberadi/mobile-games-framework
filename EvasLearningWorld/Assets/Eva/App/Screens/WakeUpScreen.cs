using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Living vs Non-living: "Wake up!". Six things sit asleep on the lab bench, some alive and some not. The child moves a
    // feather about with a finger. Tickling a living thing makes it giggle (it wiggles harder and harder, then bounces, and hops into the
    // basket on the right). A thing that is not alive never moves, however long the feather stays on it. The round ends when all the living
    // things are in the basket; a session is two rounds (twelve of the fourteen things). Nothing is a mistake. The rules are in
    // Rules/WakeUp.cs; this draws them.
    //
    // Eva says "The dog is alive!" when something wakes and, when the feather has sat on a thing that is not alive for a while, "The stone is
    // not alive." After a long quiet the hand carries the feather to the next living thing (the only help).
    //
    // Layout: in "picture space", the 1920 x 900 frame of the background world/sciencelab_bg, x = px - 960, y = 450 - py: the bench top
    // runs from y -130 (its back edge) to -295 (its front edge). The things stand on it in a row, every other one a little nearer; the
    // basket stands on the right end.
    public sealed class WakeUpScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class View
        {
            public RectTransform Rect;
            public Image Image;
            public float Phase;      // the wiggle's own beat
            public float Tilt;       // sits a little crooked
            public float AwakeTime;  // seconds since it woke
            public Vector2 HopFrom;  // where the hop started (picture space)
            public float Hop = -1f;  // 0..1 hopping to the basket; -1 not hopping
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public const float ThingSize = 130f;
        public const float StarSize = 190f;         // the feather's tap area and the width of its picture
        private const float FeatherWidth = 100f;        // the feather's picture is this wide (it is tall and slim)
        private const float TipUp = 0.4f;               // the feather's soft tip is this far (of its height) below its middle
        public const float BasketWidth = 260f;
        private static readonly Vector2 BasketCentre = new Vector2(700f, -120f);
        private static readonly Vector2 BasketMouth = new Vector2(700f, -70f);
        public static readonly Vector2 StarStart = new Vector2(0f, 130f);
        private static readonly Rect StarBounds = Rect.MinMaxRect(-780f, -280f, 780f, 330f); // where the star's middle may go
        private const float FirstX = -700f, SpacingX = 190f;
        private const float BackBaseY = -190f, FrontBaseY = -250f; // where the things' feet are (every other one stands nearer)

        private const float BounceSeconds = 0.7f, HopSeconds = 0.8f;
        private const float StartHandSeconds = 0.5f;
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintRestSeconds = 0.4f;
        private const float IdleHintSeconds = 12f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _thingField, _starField, _basket;
        private DragItem _star;
        private RectTransform _starArt;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private WakeUp _table;
        private readonly View[] _views = new View[WakeUp.ThingsPerRound];
        private System.Random _rng;
        private string[][] _session;
        private int _roundIndex;
        private string[] _ids;
        private bool _dragging, _starLive, _roundOver, _helpRunning, _hinted, _sessionEnded;
        private float _idle, _time, _buzzTimer;
        private int _hopping;
        private bool _announcing;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private int _rightLineIndex;
        private Coroutine _hintRoutine;

        public WakeUpScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public WakeUp Table => _table;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public string[] CurrentIds => _ids;
        public DragItem Star => _star;
        public RectTransform Basket => _basket;
        public bool Hinting => _helpRunning;

        public static Vector2 ThingPosition(int index)
        {
            var baseY = index % 2 == 0 ? BackBaseY : FrontBaseY;
            return new Vector2(FirstX + SpacingX * index, baseY + ThingSize * 0.5f);
        }

        // The feather's tip (what tickles), in picture space.
        public Vector2 StarPoint => Scene(_star.Rect.anchoredPosition) + TipOffset;

        // Puts the feather so that its tip is at `point` (picture space).
        public void PlaceStar(Vector2 point) => _star.Rect.anchoredPosition = Place(point - TipOffset);

        private Vector2 TipOffset => new Vector2(0f, -_featherHeight * TipUp);
        private float _featherHeight = StarSize;

        // All the layout above is in "picture space": the 1920x900 frame of the background. The picture is stretched over the whole screen
        // (full-bleed) while the screen's own space is centred on the safe area, so on a phone the two differ: Place() turns a picture-space
        // point into this screen's space (and Scene() back), from the live canvas width and cutout. Tests turn it off (identity).
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
            _basket.anchoredPosition = Place(BasketCentre);
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            AddPictureBackground();
            BuildEva();
            _thingField = Container("Things");
            BuildThings();
            BuildBasket();
            _starField = Container("StarField");
            BuildStar();
            _hand = new PointerHand(Root, _runner);
            BuildConfetti();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            UpdatePlacement();
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _session = WakeUp.CreateSession(_rng);
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
            _ids = _session[_roundIndex];
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _dragging = false;
            _starLive = false;
            _hopping = 0;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);

            var placed = new List<(string Id, float X, float Y)>();
            for (var i = 0; i < _ids.Length; i++)
            {
                var home = ThingPosition(i);
                placed.Add((_ids[i], home.x, home.y));
            }
            _table = new WakeUp(placed);

            for (var i = 0; i < _views.Length; i++)
            {
                var view = _views[i];
                view.Image.sprite = EvaUi.Sprite("sciencelab/object_" + _ids[i]);
                view.Image.color = Color.white;
                view.Hop = -1f;
                view.AwakeTime = 0f;
                view.Rect.gameObject.SetActive(true);
                view.Rect.sizeDelta = new Vector2(ThingSize, ThingSize);
                view.Rect.anchoredPosition = Place(ThingPosition(i));
                view.Rect.localRotation = Quaternion.Euler(0f, 0f, view.Tilt);
                view.Rect.localScale = Vector3.one;
                _runner.StartCoroutine(PopIn(view.Rect, 0.2f, 0.06f * i));
            }
            PlaceStar(StarStart);
            _star.Rect.localScale = Vector3.one;
            _starArt.localScale = Vector3.one;
            _basket.localScale = Vector3.one;
            _runner.StartCoroutine(SayPrompt());
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("wake_prompt");
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildBasket()
        {
            var go = new GameObject("Basket", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            _basket = (RectTransform)go.transform;
            _basket.anchorMin = _basket.anchorMax = _basket.pivot = new Vector2(0.5f, 0.5f);
            _basket.sizeDelta = new Vector2(BasketWidth, BasketWidth);
            _basket.anchoredPosition = BasketCentre;
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("sciencelab/basket_plain");
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildThings()
        {
            for (var i = 0; i < _views.Length; i++)
            {
                var thing = new GameObject("Thing" + i, typeof(RectTransform), typeof(Image));
                thing.transform.SetParent(_thingField, false);
                var rect = (RectTransform)thing.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var picture = thing.GetComponent<Image>();
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                _views[i] = new View { Rect = rect, Image = picture, Phase = i * 1.7f, Tilt = ((i * 37) % 11 - 5) * 1.2f };
            }
        }

        private void BuildStar()
        {
            _star = DragItem.Create(_starField, "star", EvaUi.Sprite("icons/dot"), Place(StarStart), Mathf.Max(EvaUi.MinTap, StarSize + 20f));
            _star.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
            _star.BeginDrag += _ => OnBeginDrag();
            _star.EndDrag += _ => LetGo();
            var art = new GameObject("Art", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(_star.transform, false);
            _starArt = (RectTransform)art.transform;
            _starArt.anchorMin = _starArt.anchorMax = _starArt.pivot = new Vector2(0.5f, 0.5f);
            _starArt.anchoredPosition = Vector2.zero;
            var image = art.GetComponent<Image>();
            var feather = Resources.Load<Sprite>("Art/sciencelab/feather");
            if (feather != null) _featherHeight = FeatherWidth * feather.rect.height / feather.rect.width;
            _starArt.sizeDelta = new Vector2(feather != null ? FeatherWidth : StarSize, _featherHeight);
            image.sprite = feather != null ? feather : EvaUi.Sprite("sciencelab/space_star");
            image.preserveAspect = true;
            image.raycastTarget = false;
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
            _basket.gameObject.SetActive(!ended);
            _starField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- The star ---------------------------------------------------------------------------------------------------

        private void OnBeginDrag()
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for the star never waits for the hint hand
            _dragging = true;
            _starLive = true;
            _starArt.localScale = Vector3.one * 1.1f;
            _idle = 0f;
        }

        public void LetGo()
        {
            _dragging = false;
            _idle = 0f;
            _starArt.localScale = Vector3.one;
        }

        // The star may not leave the screen or sink into the bench.
        private void ClampStar()
        {
            var centre = StarPoint;
            var clamped = new Vector2(Mathf.Clamp(centre.x, StarBounds.xMin, StarBounds.xMax), Mathf.Clamp(centre.y, StarBounds.yMin, StarBounds.yMax));
            if (clamped != centre) PlaceStar(clamped);
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

        // One frame of the bench. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_table == null || _sessionEnded) return;
            _time += seconds;
            ClampStar();
            var point = StarPoint;
            if (_starLive && !_roundOver)
            {
                _table.Step(seconds, point.x, point.y);
                foreach (var index in _table.JustWoke) OnWoke(index);
                BuzzWhileWaking(seconds);
                foreach (var index in _table.JustNotAlive) Say("wake_no_" + _table.Things[index].Id);
            }
            for (var i = 0; i < _views.Length; i++) UpdateView(i, point, seconds);
            // The feather sways; moved it is busier, and over something sleeping it wiggles fast and wide, tickling.
            var tickling = _dragging && IsOverSleeper(point);
            _starArt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * (tickling ? 26f : _dragging ? 9f : 2.5f)) * (tickling ? 15f : _dragging ? 8f : 5f));
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private bool IsOverSleeper(Vector2 point)
        {
            foreach (var thing in _table.Things)
                if (thing.State == WakeState.Asleep && Vector2.Distance(point, new Vector2(thing.HomeX, thing.HomeY)) < WakeUp.WakeRange) return true;
            return false;
        }

        // While the star is waking something, a light pulse every so often, stronger the closer it is to waking.
        private void BuzzWhileWaking(float seconds)
        {
            var strongest = 0f;
            foreach (var thing in _table.Things) if (thing.State == WakeState.Asleep) strongest = Mathf.Max(strongest, thing.Progress);
            _buzzTimer -= seconds;
            if (strongest < 0.05f || _buzzTimer > 0f) return;
            _buzzTimer = 0.12f;
            Haptics.Buzz(strongest);
        }

        private void OnWoke(int index)
        {
            Haptics.Tap();
            var view = _views[index];
            view.AwakeTime = 0f;
            view.Hop = -1f;
            view.HopFrom = ThingPosition(index);
            _hopping++;
            if (_game != null && _game.Sfx != null) _game.Sfx.Pop();
            Say("wake_say_" + _table.Things[index].Id);
        }

        private void UpdateView(int index, Vector2 star, float seconds)
        {
            var thing = _table.Things[index];
            var view = _views[index];
            var home = ThingPosition(index);
            switch (thing.State)
            {
                case WakeState.Asleep:
                {
                    var progress = thing.Progress;
                    var shake = Mathf.Sin(_time * 33f + view.Phase) * 8f * progress; // a thing that is not alive never moves at all
                    view.Rect.anchoredPosition = Place(home + new Vector2(0f, 14f * progress));
                    view.Rect.localRotation = Quaternion.Euler(0f, 0f, view.Tilt + shake);
                    view.Rect.localScale = Vector3.one * (1f + 0.12f * progress);
                    break;
                }
                case WakeState.Awake:
                {
                    view.AwakeTime += seconds;
                    if (view.AwakeTime < BounceSeconds)
                    {
                        // It bounces on the spot, squashing and stretching.
                        var beat = Mathf.Abs(Mathf.Sin(view.AwakeTime * 9f));
                        view.Rect.anchoredPosition = Place(home + new Vector2(0f, beat * 50f));
                        view.Rect.localScale = new Vector3(1.1f - 0.12f * beat, 1.05f + 0.15f * beat, 1f);
                        view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(view.AwakeTime * 12f) * 6f);
                        break;
                    }
                    if (view.Hop < 0f) view.Hop = 0f;
                    if (view.Hop >= 1f) break;
                    view.Hop = Mathf.Min(1f, view.Hop + seconds / HopSeconds);
                    var t = view.Hop;
                    var x = Mathf.Lerp(view.HopFrom.x, BasketMouth.x, t);
                    var y = Mathf.Lerp(view.HopFrom.y, BasketMouth.y, t) + Mathf.Sin(t * Mathf.PI) * 170f;
                    view.Rect.anchoredPosition = Place(new Vector2(x, y));
                    view.Rect.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.55f, t);
                    view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 6.28f) * 12f);
                    view.Image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - 0.75f) / 0.25f));
                    if (view.Hop >= 1f)
                    {
                        _table.Collect(index);
                        view.Rect.gameObject.SetActive(false);
                        _hopping--;
                        Haptics.Thud();
                        if (_game != null && _game.Sfx != null) _game.Sfx.Drop();
                    }
                    break;
                }
            }
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
            if (_roundOver || _helpRunning || _dragging || _hopping > 0) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds)
            {
                _idle = 0f;
                _hintRoutine = _runner.StartCoroutine(RunHint());
            }
        }

        // The child touched the star while the hint hand was out: the hint stops, the star stays where it is.
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

        // After a long quiet the hand carries the star to the next living thing that still sleeps.
        private IEnumerator RunHint()
        {
            var target = -1;
            for (var i = 0; i < _views.Length; i++)
                if (_table.Things[i].Alive && _table.Things[i].State == WakeState.Asleep) { target = i; break; }
            if (target < 0) yield break;

            _helpRunning = true;
            _hinted = true;
            _starLive = true;
            _eva.SetTalking(true);
            _game.Voice.Say("wake_hint");

            var from = _star.Rect.anchoredPosition;
            var to = Place(ThingPosition(target) - TipOffset);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                _star.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            _star.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds + WakeUp.WakeSeconds);
            _hand.Pulse(false);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            _idle = 0f;
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_table.Done || _hopping > 0 || _announcing) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
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
            if (_roundIndex >= WakeUp.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _basket.position);
        }

        // --- Small helpers ----------------------------------------------------------------------------------------------

        private static IEnumerator PopIn(RectTransform target, float duration, float delay)
        {
            target.localScale = Vector3.one * 0.3f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

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
            image.sprite = EvaUi.Sprite(_backgroundSprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
