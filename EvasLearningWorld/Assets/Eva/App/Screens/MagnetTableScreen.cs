using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Magnet: a real magnet on a real table. Six things lie on a wooden table, some a magnet pulls and some it does not. The
    // child holds a big red horseshoe magnet with a finger and moves it about. Near a magnetic thing the thing wiggles and leans toward the
    // magnet; held there a moment it jumps up and sticks to the tips with a "clink". A thing the magnet does not pull never moves, however
    // long the magnet sits on it. Let go of the magnet over the wooden tub and everything stuck to it drops in. The round ends when all the
    // magnetic things are in the tub; a session is two rounds (every one of the twelve things once). Nothing is a mistake. The rules are in
    // Rules/MagnetTable.cs; this draws them.
    //
    // Eva says "The nail sticks!" when something jumps up (the first time in a round she adds where to take it), and, when the magnet has sat
    // on a thing it does not pull for a while, "The pencil does not stick." After a long quiet the hand carries the magnet: to the next
    // magnetic thing, or, when something is stuck, to the tub (the only help).
    //
    // Layout: in "picture space", the 1920 x 900 frame of the background world/magnet_bg, x = px - 960, y = 450 - py, measured from the picture:
    // the table's top face runs from y -54 (its back edge) to -145 (its front edge). The things stand on it in a row, every other one a little nearer;
    // the tub stands on the right end. The magnet starts above the middle.
    public sealed class MagnetTableScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class View
        {
            public RectTransform Rect;
            public Image Image;
            public float Phase;     // the wiggle's own beat
            public float Tilt;      // lies a little crooked on the table
            public float Fly;       // 0..1: jumping from the table to the magnet
            public Vector2 FlyFrom; // where the jump started (picture space)
            public bool Landed;     // the "clink" has been played
            public float Fall = -1f; // 0..1 falling into the tub; -1 not falling
            public Vector2 FallFrom;
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public const float ThingSize = 130f;              // a thing on the table
        public const float StuckSize = 92f;               // the same thing hanging on the magnet
        public const float MagnetWidth = 240f;
        private const float TipSpreadPerWidth = 0.325f;   // from the picture: the tips' middles are this far either side of the middle
        private const float PoleInset = 8f;               // the pole point sits this far above the bottom of the picture
        public const float TubWidth = 260f;
        private static readonly Vector2 TubCentre = new Vector2(690f, -19f);
        private static readonly Vector2 TubMouth = new Vector2(690f, -4f);
        // The magnet is let go over the tub (its pole point anywhere in this box) and what hangs on it falls in.
        private static readonly Rect TubZone = Rect.MinMaxRect(540f, -90f, 850f, 330f);
        public static readonly Vector2 MagnetStart = new Vector2(0f, 270f);
        private static readonly Rect MagnetBounds = Rect.MinMaxRect(-780f, -60f, 780f, 330f); // where the magnet's middle may go
        private const float FirstX = -700f, SpacingX = 190f;
        private const float BackBaseY = -92f, FrontBaseY = -128f; // where the things' feet are (every other one stands nearer)

        private const float StartHandSeconds = 0.5f;
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintRestSeconds = 0.4f;
        private const float IdleHintSeconds = 12f;
        private const float FlySeconds = 0.17f, FallSeconds = 0.45f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _thingField, _magnetField, _tub;
        private DragItem _magnet;
        private RectTransform _magnetArt;
        private float _magnetHeight, _tipSpread;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private MagnetTable _table;
        private readonly View[] _views = new View[MagnetTable.ThingsPerRound];
        private System.Random _rng;
        private string[][] _session;
        private int _roundIndex;
        private string[] _ids;
        private bool _dragging, _magnetLive, _roundOver, _helpRunning, _hinted, _sessionEnded, _toldBucket;
        private float _idle, _time, _tubGlow;
        private int _falling;
        private bool _announcing;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private int _rightLineIndex;
        private Coroutine _hintRoutine;

        public MagnetTableScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public MagnetTable Table => _table;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public string[] CurrentIds => _ids;
        public DragItem Magnet => _magnet;
        public RectTransform Tub => _tub;
        public bool Hinting => _helpRunning;

        public static Vector2 ThingPosition(int index)
        {
            var baseY = index % 2 == 0 ? BackBaseY : FrontBaseY;
            return new Vector2(FirstX + SpacingX * index, baseY + ThingSize * 0.5f);
        }

        // The magnet's pole point (the middle of its two tips), in picture space.
        public Vector2 Pole => PoleOf(Scene(_magnet.Rect.anchoredPosition));

        // Puts the magnet so that its pole point is at `pole` (picture space).
        public void PlaceMagnetPole(Vector2 pole) => _magnet.Rect.anchoredPosition = Place(CentreOf(pole));

        private Vector2 PoleOf(Vector2 centre) => centre + new Vector2(0f, -_magnetHeight * 0.5f + PoleInset);

        private Vector2 CentreOf(Vector2 pole) => pole + new Vector2(0f, _magnetHeight * 0.5f - PoleInset);

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
            _tub.anchoredPosition = Place(TubCentre);
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            AddPictureBackground();
            BuildEva();
            _thingField = Container("Things");
            BuildThings();
            BuildTub();
            _magnetField = Container("MagnetField");
            BuildMagnet();
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
            _session = MagnetTable.CreateSession(_rng);
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
            _magnetLive = false;
            _toldBucket = false;
            _falling = 0;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _tubGlow = 0f;
            _confetti.SetActive(false);

            var placed = new List<(string Id, float X, float Y)>();
            for (var i = 0; i < _ids.Length; i++)
            {
                var home = ThingPosition(i);
                placed.Add((_ids[i], home.x, home.y));
            }
            _table = new MagnetTable(placed);

            for (var i = 0; i < _views.Length; i++)
            {
                var view = _views[i];
                view.Image.sprite = EvaUi.Sprite("sciencelab/object_" + _ids[i]);
                view.Image.color = Color.white;
                view.Fly = 0f;
                view.Landed = false;
                view.Fall = -1f;
                view.Rect.gameObject.SetActive(true);
                view.Rect.sizeDelta = new Vector2(ThingSize, ThingSize);
                view.Rect.anchoredPosition = Place(ThingPosition(i));
                view.Rect.localRotation = Quaternion.Euler(0f, 0f, view.Tilt);
                view.Rect.localScale = Vector3.one;
                _runner.StartCoroutine(PopIn(view.Rect, 0.2f, 0.06f * i));
            }
            _magnet.Rect.anchoredPosition = Place(CentreOf(PoleOf(MagnetStart)));
            _magnet.Rect.localScale = Vector3.one;
            _magnetArt.localScale = Vector3.one;
            _tub.localScale = Vector3.one;
            _runner.StartCoroutine(SayPrompt());
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("magnet_prompt");
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildTub()
        {
            var go = new GameObject("Tub", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            _tub = (RectTransform)go.transform;
            _tub.anchorMin = _tub.anchorMax = _tub.pivot = new Vector2(0.5f, 0.5f);
            _tub.sizeDelta = new Vector2(TubWidth, TubWidth);
            _tub.anchoredPosition = TubCentre;
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("sciencelab/bucket_magnetic");
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

        private void BuildMagnet()
        {
            var sprite = EvaUi.Sprite("sciencelab/horseshoe_magnet");
            _magnetHeight = MagnetWidth * sprite.rect.height / sprite.rect.width;
            _tipSpread = MagnetWidth * TipSpreadPerWidth;
            _magnet = DragItem.Create(_magnetField, "magnet", EvaUi.Sprite("icons/dot"), Place(CentreOf(PoleOf(MagnetStart))), Mathf.Max(EvaUi.MinTap, MagnetWidth + 20f));
            _magnet.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
            _magnet.BeginDrag += _ => OnBeginDrag();
            _magnet.EndDrag += _ => LetGo();
            var art = new GameObject("Art", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(_magnet.transform, false);
            _magnetArt = (RectTransform)art.transform;
            _magnetArt.anchorMin = _magnetArt.anchorMax = _magnetArt.pivot = new Vector2(0.5f, 0.5f);
            _magnetArt.anchoredPosition = Vector2.zero;
            _magnetArt.sizeDelta = new Vector2(MagnetWidth, _magnetHeight);
            var image = art.GetComponent<Image>();
            image.sprite = sprite;
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
            _tub.gameObject.SetActive(!ended);
            _magnetField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- The magnet -------------------------------------------------------------------------------------------------

        private void OnBeginDrag()
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for the magnet never waits for the hint hand
            _dragging = true;
            _magnetLive = true;
            _magnetArt.localScale = Vector3.one * 1.06f;
            _idle = 0f;
        }

        // The magnet is let go. Over the tub, everything hanging on it falls in.
        public void LetGo()
        {
            _dragging = false;
            _idle = 0f;
            _magnetArt.localScale = Vector3.one;
            if (_roundOver || _table == null) return;
            if (_table.StuckCount > 0 && TubZone.Contains(Pole)) DropIntoTub();
        }

        private void DropIntoTub()
        {
            var pole = Pole;
            for (var i = 0; i < _views.Length; i++)
            {
                if (_table.Things[i].State != MagnetThingState.Stuck) continue;
                var view = _views[i];
                view.Fall = 0f;
                view.FallFrom = pole + SlotOffset(_table.Things[i].Slot);
                _falling++;
            }
            _table.DropStuck();
            if (_game != null && _game.Sfx != null) _game.Sfx.Place();
        }

        private Vector2 SlotOffset(int slot)
        {
            switch (slot)
            {
                case 0: return new Vector2(-_tipSpread, -StuckSize * 0.5f + 6f);
                case 1: return new Vector2(_tipSpread, -StuckSize * 0.5f + 6f);
                default: return new Vector2(0f, -StuckSize * 1.45f);
            }
        }

        // The magnet may not leave the screen or sink into the table.
        private void ClampMagnet()
        {
            var centre = Scene(_magnet.Rect.anchoredPosition);
            var clamped = new Vector2(Mathf.Clamp(centre.x, MagnetBounds.xMin, MagnetBounds.xMax), Mathf.Clamp(centre.y, MagnetBounds.yMin, MagnetBounds.yMax));
            if (clamped != centre) _magnet.Rect.anchoredPosition = Place(clamped);
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

        // One frame of the table. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_table == null || _sessionEnded) return;
            _time += seconds;
            ClampMagnet();
            var pole = Pole;
            if (_magnetLive && !_roundOver)
            {
                _table.Step(seconds, pole.x, pole.y);
                foreach (var index in _table.JustStuck) OnStuck(index);
                foreach (var index in _table.JustNotPulled) Say("magnet_no_" + _table.Things[index].Id);
            }
            for (var i = 0; i < _views.Length; i++) UpdateView(i, pole, seconds);
            UpdateTubGlow(pole, seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void OnStuck(int index)
        {
            var view = _views[index];
            view.Fly = 0f;
            view.Landed = false;
            view.FlyFrom = Scene(view.Rect.anchoredPosition);
            Say("magnet_say_" + _table.Things[index].Id);
            if (!_toldBucket)
            {
                _toldBucket = true;
                Say("magnet_bucket");
            }
        }

        // Where the wiggle has moved a thing on the table, relative to its place (picture space).
        private Vector2 ThingOffset(int index, Vector2 pole)
        {
            var thing = _table.Things[index];
            var pull = thing.Pull;
            if (pull <= 0f) return Vector2.zero;
            var view = _views[index];
            var home = ThingPosition(index);
            var lean = (pole.x - home.x) * 0.18f * pull;
            var jitter = Mathf.Sin(_time * 47f + view.Phase) * 4f * pull;
            var lift = 26f * pull * (0.7f + 0.3f * Mathf.Sin(_time * 40f + view.Phase));
            return new Vector2(lean + jitter, lift);
        }

        private void UpdateView(int index, Vector2 pole, float seconds)
        {
            var thing = _table.Things[index];
            var view = _views[index];
            switch (thing.State)
            {
                case MagnetThingState.OnTable:
                {
                    var pull = thing.Pull;
                    view.Rect.anchoredPosition = Place(ThingPosition(index) + ThingOffset(index, pole));
                    var lean = Mathf.Sign(pole.x - ThingPosition(index).x);
                    view.Rect.localRotation = Quaternion.Euler(0f, 0f, view.Tilt + (Mathf.Sin(_time * 37f + view.Phase) * 9f - lean * 6f) * pull);
                    view.Rect.localScale = Vector3.one * (1f + 0.1f * pull);
                    break;
                }
                case MagnetThingState.Stuck:
                {
                    var target = pole + SlotOffset(thing.Slot);
                    if (view.Fly < 1f)
                    {
                        view.Fly = Mathf.Min(1f, view.Fly + seconds / FlySeconds);
                        var t = PointerHand.EaseInOut(view.Fly);
                        var position = Vector2.Lerp(view.FlyFrom, target, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 36f);
                        view.Rect.anchoredPosition = Place(position);
                        view.Rect.localScale = Vector3.one * Mathf.Lerp(1.1f, StuckSize / ThingSize, t);
                        view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(view.Tilt, 0f, t));
                    }
                    else
                    {
                        if (!view.Landed)
                        {
                            view.Landed = true;
                            if (_game != null && _game.Sfx != null) _game.Sfx.Clink();
                        }
                        // It hangs on the tips and swings a little as the magnet moves.
                        view.Rect.anchoredPosition = Place(target);
                        view.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 3f + view.Phase) * 3f);
                        view.Rect.localScale = Vector3.one * (StuckSize / ThingSize);
                    }
                    break;
                }
                default:
                {
                    if (view.Fall < 0f)
                    {
                        view.Fall = 0f; // dropped by a test or a hint: start falling from where it hangs
                        view.FallFrom = pole + SlotOffset(0);
                        _falling++;
                    }
                    if (view.Fall >= 1f) break;
                    view.Fall = Mathf.Min(1f, view.Fall + seconds / FallSeconds);
                    var t = view.Fall;
                    var x = Mathf.Lerp(view.FallFrom.x, TubMouth.x, t);
                    var y = Mathf.Lerp(view.FallFrom.y, TubMouth.y, t * t);
                    view.Rect.anchoredPosition = Place(new Vector2(x, y));
                    view.Rect.localScale = Vector3.one * Mathf.Lerp(StuckSize / ThingSize, 0.55f, t);
                    view.Image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - 0.7f) / 0.3f));
                    if (view.Fall >= 1f)
                    {
                        view.Rect.gameObject.SetActive(false);
                        _falling--;
                        if (_game != null && _game.Sfx != null) _game.Sfx.Drop();
                    }
                    break;
                }
            }
        }

        private void UpdateTubGlow(Vector2 pole, float seconds)
        {
            var over = _dragging && _table.StuckCount > 0 && TubZone.Contains(pole);
            _tubGlow = Mathf.MoveTowards(_tubGlow, over ? 1f : 0f, seconds * 6f);
            _tub.localScale = Vector3.one * (1f + 0.06f * _tubGlow);
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
            if (_roundOver || _helpRunning || _dragging || _falling > 0) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds)
            {
                _idle = 0f;
                _hintRoutine = _runner.StartCoroutine(RunHint());
            }
        }

        // The child touched the magnet while the hint hand was out: the hint stops, the magnet stays where it is.
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

        // After a long quiet the hand carries the magnet: to the next magnetic thing, or, with something stuck on it, to the tub.
        private IEnumerator RunHint()
        {
            var toTub = _table.StuckCount > 0;
            var target = -1;
            if (!toTub)
                for (var i = 0; i < _views.Length; i++)
                    if (_table.Things[i].Magnetic && _table.Things[i].State == MagnetThingState.OnTable) { target = i; break; }
            if (!toTub && target < 0) yield break;

            _helpRunning = true;
            _hinted = true;
            _magnetLive = true;
            _eva.SetTalking(true);
            _game.Voice.Say(toTub ? "magnet_hint_bucket" : "magnet_hint");

            var from = _magnet.Rect.anchoredPosition;
            var pole = toTub ? new Vector2(TubMouth.x, 120f) : ThingPosition(target) + new Vector2(0f, 40f);
            var to = Place(CentreOf(pole));
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                _magnet.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            _magnet.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds + MagnetTable.StickSeconds * 2f);
            _hand.Pulse(false);
            if (toTub) LetGo();

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            _idle = 0f;
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_table.Done || _falling > 0 || _announcing) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
            _confetti.SetActive(true); // the fanfare and a burst of confetti
            _eva.Cheer();
            _rightLineIndex = _rightLineIndex % 3 + 1;
            var payout = _hinted ? CoinPayout.Assisted : CoinPayout.Clean;
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            _runner.StartCoroutine(AnimateCoins(before, before + payout));
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= MagnetTable.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _tub.position);
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
