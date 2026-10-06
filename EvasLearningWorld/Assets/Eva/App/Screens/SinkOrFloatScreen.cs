using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Sink or Float: a real tank of water. Six things stand on the shelves (three that float, three that sink, mixed); the
    // child drags them into the tank in any order and watches what the water does. Nothing is a mistake. A drop outside the tank just
    // goes back to its place. The water is alive: its surface is a WaterSurface (ripples run out from where something lands, bounce
    // off the glass and fade, over a slow swell), drawn every frame by two WaterGraphic layers - one behind the objects, one in front
    // so a sunk object is seen through the water - with foam and shafts of light. A floater (BuoyantBody) falls, dips, bobs up and rides the
    // ripples, tilting with them; a sinker falls at its own speed, swaying and trailing bubbles, lands on the floor and lies there.
    // Splashes throw drops, sinkers leave bubbles that pop at the surface, and the tank breathes a few quiet bubbles of its own.
    // Eva says what each one did ("The rock sinks!") once it has settled. When all six are in, the round ends with confetti and
    // coins; a session is two rounds (every one of the twelve things once).
    //
    // If the child leaves it alone for a while the hand carries one object toward the water and back (the only help; no mistakes).
    //
    // Layout (1440 x 900 frame, y in [-450, 450]; the picture, world/sink_float_bg, fills the whole screen): the shelves on the left,
    // two columns by three rows, each thing a 240 tap area (invisible) with its picture drawn at ShelfArt inside, because the planks are
    // only about 200 apart; held, the picture grows. The tank to their right, clear of the Back button, the coin counter and the pair
    // in the corner. The numbers are measured from the picture.
    public sealed class SinkOrFloatScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Entry
        {
            public int Cell;
            public string Id;
            public BuoyantBody Body;
            public RectTransform Rect;
            public Image Image;
            public float Appear;       // 0..1: the object shrinks from the size it was held at to its size in the water
            public float BubbleTimer;
            public float SettledTime;
            public float InWaterTime;
            public bool Announced;
        }

        private sealed class Bubble
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public float Speed, Phase, Size;
            public bool Loud;
            public bool Alive;
        }

        private sealed class Drop
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position, Velocity;
            public float Age;
            public bool Alive;
        }

        // --- Layout (canvas units) ---
        public const int Cells = SinkOrFloat.ObjectsPerRound;
        private const float CellSize = EvaUi.MinTap; // the tap area of a thing on the shelf: 240
        public const float ShelfArt = 140f;          // how big it is drawn there (smaller than the ~200 plank gap, so no picture touches another or a plank above); held, it grows
        public const float HeldArtScale = 1.3f;
        private const float PictureWidth = 1920f;
        // Measured on world/sink_float_bg (1920x900 frame): the shelf's inside runs x -523..-20, the planks' front edges (where things
        // stand) at y 154, -42 and -243; the glass's inside x 50..658, its floor y -80, the top rim y 242.
        private static readonly float[] CellX = { -365f, -155f };
        private static readonly float[] CellY = { 233f, 37f, -164f };

        public static readonly Vector2 TankCentre = new Vector2(354f, 140f);  // the middle of the water's resting surface
        public const float TankWidth = 608f;
        public const float WaterDepth = 220f;
        public const float TankHeight = WaterDepth * 2f;                      // the water graphic's rect: the surface in its middle
        public const float HeadRoom = 100f;                                   // air above the water, inside the glass
        public const float FloorY = -WaterDepth;                              // tank-local: the water's rest level is 0
        public const float BodySize = 125f;
        private static readonly Rect DropZone = Rect.MinMaxRect(30f, -110f, 680f, 300f);
        private const float SpawnTopLimit = 160f; // tank-local
        private const int SurfaceColumns = 64;

        private const float HeldScale = ShelfArt * HeldArtScale / BodySize;
        private const float HoverScale = 1.08f;
        private const float SnapBackSeconds = 0.25f;
        private const float IdleHintSeconds = 14f;
        private const float AnnounceAfterSettleSeconds = 0.5f;
        private const float AnnounceAtLatestSeconds = 3.5f; // a sinker that is still drifting, a floater that will not calm
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintRestSeconds = 0.4f;
        private const int MaxBubbles = 48, MaxDrops = 40;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _tank, _bodyField, _fxField, _itemField;
        private WaterGraphic _waterBack, _waterFront;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private WaterSurface _surface;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<BuoyantBody> _bodies = new List<BuoyantBody>();
        private readonly Bubble[] _bubbles = new Bubble[MaxBubbles];
        private readonly Drop[] _drops = new Drop[MaxDrops];
        private DragItem[] _items;
        private Image[] _itemImages;
        private RectTransform[] _itemArt;
        private readonly bool[] _onShelf = new bool[Cells];

        private System.Random _rng;
        private string[][] _session;
        private int _roundIndex;
        private string[] _ids;
        private DragItem _dragging;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded;
        private float _idle, _idleBubble, _popCooldown, _excite;
        private int _announced;
        private bool _announcing;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private int _rightLineIndex;

        public SinkOrFloatScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public int PlacedCount => _entries.Count;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public string[] CurrentIds => _ids;
        public WaterSurface Surface => _surface;
        public IReadOnlyList<BuoyantBody> Bodies => _bodies;
        public bool IsOnShelf(int cell) => _onShelf[cell];

        public static Vector2 CellPosition(int cell) => new Vector2(CellX[cell % 2], CellY[cell / 2]);

        // All the layout above is in "picture space": the 1920x900 frame of the background, x = px - 960, y = 450 - py. The picture is
        // stretched over the whole screen (full-bleed) while the screen's own space is centred on the safe area, so on a phone the two
        // differ: Place() turns a picture-space point into this screen's space (and Scene() back), from the live canvas width and cutout.
        // Tests turn it off (identity) so they can work in picture space directly.
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
            _tank.anchoredPosition = Place(TankCentre);
            _tank.localScale = new Vector3(_scaleX, 1f, 1f);
            for (var i = 0; i < Cells; i++) if (_onShelf[i]) _items[i].Rect.anchoredPosition = Place(CellPosition(i));
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            _surface = new WaterSurface(SurfaceColumns, TankWidth);

            AddPictureBackground();
            BuildEva();
            BuildTank();
            _itemField = Container("ItemField");
            BuildItems();
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
            _session = SinkOrFloat.CreateSession(_rng);
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
            _dragging = null;
            _announced = 0;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            ClearTank();
            for (var i = 0; i < Cells; i++)
            {
                _onShelf[i] = true;
                var item = _items[i];
                item.gameObject.SetActive(true);
                item.enabled = true;
                item.Rect.anchoredPosition = Place(CellPosition(i));
                item.Rect.localScale = Vector3.one;
                _itemArt[i].localScale = Vector3.one;
                _itemImages[i].sprite = EvaUi.Sprite("sciencelab/object_" + _ids[i]);
                _itemImages[i].color = Color.white;
                _runner.StartCoroutine(PopIn(item.Rect, 0.2f, 0.06f * i));
            }
            _runner.StartCoroutine(SayPrompt());
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("sinkorfloat_prompt");
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildTank()
        {
            var go = new GameObject("Tank", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _tank = (RectTransform)go.transform;
            _tank.anchorMin = _tank.anchorMax = _tank.pivot = new Vector2(0.5f, 0.5f);
            _tank.anchoredPosition = TankCentre;
            _tank.sizeDelta = new Vector2(TankWidth, TankHeight);

            _waterBack = AddWater("WaterBack", WaterGraphic.Layer.Back, 0f);
            _bodyField = StretchChild(_tank, "Bodies");
            _waterFront = AddWater("WaterFront", WaterGraphic.Layer.Front, 1.7f);
            _fxField = StretchChild(_tank, "Effects");
            for (var i = 0; i < MaxBubbles; i++) _bubbles[i] = MakeBubble();
            for (var i = 0; i < MaxDrops; i++) _drops[i] = MakeDrop();
        }

        private WaterGraphic AddWater(string name, WaterGraphic.Layer layer, float phase)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(WaterGraphic));
            go.transform.SetParent(_tank, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var water = go.GetComponent<WaterGraphic>();
            water.Kind = layer;
            water.Surface = _surface;
            water.RestY = 0f;
            water.Phase = phase;
            water.raycastTarget = false;
            return water;
        }

        private Bubble MakeBubble()
        {
            var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_fxField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = BubbleSprite();
            image.raycastTarget = false;
            go.SetActive(false);
            return new Bubble { Rect = rect, Image = image };
        }

        private Drop MakeDrop()
        {
            var go = new GameObject("Drop", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_fxField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/dot");
            image.raycastTarget = false;
            go.SetActive(false);
            return new Drop { Rect = rect, Image = image };
        }

        private void BuildItems()
        {
            _items = new DragItem[Cells];
            _itemImages = new Image[Cells];
            _itemArt = new RectTransform[Cells];
            for (var i = 0; i < Cells; i++)
            {
                var item = DragItem.Create(_itemField, "sinkfloat" + i, EvaUi.Sprite("icons/dot"), Place(CellPosition(i)), CellSize);
                var index = i;
                item.BeginDrag += _ => OnBeginDrag(index);
                item.EndDrag += _ => OnEndDrag(index);
                _items[i] = item;
                item.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var art = new GameObject("Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(item.transform, false);
                var artRect = (RectTransform)art.transform;
                artRect.anchorMin = artRect.anchorMax = artRect.pivot = new Vector2(0.5f, 0.5f);
                artRect.anchoredPosition = Vector2.zero;
                artRect.sizeDelta = new Vector2(ShelfArt, ShelfArt);
                var artImage = art.GetComponent<Image>();
                artImage.preserveAspect = true;
                artImage.raycastTarget = false;
                _itemArt[i] = artRect;
                _itemImages[i] = artImage;
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
            _tank.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Dragging ---------------------------------------------------------------------------------------------------

        private void OnBeginDrag(int cell)
        {
            if (_roundOver || !_onShelf[cell]) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a thing never waits for the hint hand
            _dragging = _items[cell];
            _itemArt[cell].localScale = Vector3.one * HeldArtScale;
            _idle = 0f;
        }

        private void OnEndDrag(int cell)
        {
            if (_dragging == _items[cell]) _dragging = null;
            _idle = 0f;
            if (_roundOver || !_onShelf[cell]) return;
            var position = Scene(_items[cell].Rect.anchoredPosition);
            if (DropZone.Contains(position)) Release(cell, position);
            else
            {
                _itemArt[cell].localScale = Vector3.one;
                _runner.StartCoroutine(SlideTo(_items[cell].Rect, Place(CellPosition(cell)), SnapBackSeconds));
            }
        }

        // The thing from `cell` is let go at `position` (canvas units): it leaves the shelf and falls into the tank from there.
        public void Release(int cell, Vector2 position)
        {
            if (!_onShelf[cell]) return;
            _onShelf[cell] = false;
            var item = SinkOrFloat.Find(_ids[cell]);
            var local = position - TankCentre;
            var halfWidth = TankWidth * 0.5f;
            var x = Mathf.Clamp(local.x, -halfWidth + BodySize * 0.5f, halfWidth - BodySize * 0.5f);
            var y = Mathf.Min(local.y, SpawnTopLimit);

            var body = new BuoyantBody(item.Floats, BodySize, item.Submerge, item.SinkSpeed, item.Sway, x, y, cell + 3 * _roundIndex);
            var go = new GameObject("Body_" + item.Id, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_bodyField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(BodySize, BodySize);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("sciencelab/object_" + item.Id);
            image.preserveAspect = true;
            image.raycastTarget = false;
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = Vector3.one * HeldScale;
            _entries.Add(new Entry { Cell = cell, Id = item.Id, Body = body, Rect = rect, Image = image });
            _bodies.Add(body);

            _items[cell].gameObject.SetActive(false);
            _items[cell].Rect.anchoredPosition = Place(CellPosition(cell));
            _items[cell].Rect.localScale = Vector3.one;
            _itemArt[cell].localScale = Vector3.one;
            _idle = 0f;
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

        // One frame of the tank: the water, every object in it, the bubbles and drops, and what follows from them. Public so tests can
        // run the physics without a player loop.
        public void Tick(float seconds)
        {
            if (_surface == null || _sessionEnded) return;
            _surface.Advance(seconds);
            _popCooldown -= seconds;
            _bodies.Clear();
            foreach (var entry in _entries) _bodies.Add(entry.Body);

            foreach (var entry in _entries) StepEntry(entry, seconds);
            BuoyantBody.Separate(_bodies, TankWidth * 0.5f);
            foreach (var entry in _entries) Place(entry, seconds);

            UpdateBubbles(seconds);
            UpdateDrops(seconds);
            UpdateHover(seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void StepEntry(Entry entry, float seconds)
        {
            var body = entry.Body;
            var result = body.Step(seconds, _surface, 0f, FloorY, TankWidth * 0.5f);
            if (result.EnteredWater) OnEnteredWater(entry, result.EntrySpeed);
            if (result.HitFloor) OnHitFloor(entry, result.FloorSpeed);

            if (body.InWater)
            {
                // The object pushes the water as it moves, but only near the surface (a sinker far below is no longer stirring it).
                var nearness = Mathf.Clamp01(1f - (_surface.HeightAt(body.X) - body.Y) / 110f);
                if (nearness > 0f && Mathf.Abs(body.VY) > 60f) _surface.Disturb(body.X, body.VY * 3f * seconds * nearness, 30f);
            }

            if (body.Sinking)
            {
                entry.BubbleTimer -= seconds;
                if (entry.BubbleTimer <= 0f)
                {
                    entry.BubbleTimer = UnityEngine.Random.Range(0.07f, 0.16f);
                    SpawnBubble(new Vector2(body.X + UnityEngine.Random.Range(-18f, 18f), body.Y + BodySize * 0.25f), UnityEngine.Random.Range(8f, 16f), true);
                }
            }

            if (body.InWater) entry.InWaterTime += seconds;
            if (entry.Announced) return;
            if (body.Settled) entry.SettledTime += seconds;
            if (entry.SettledTime >= AnnounceAfterSettleSeconds || entry.InWaterTime > AnnounceAtLatestSeconds) Announce(entry);
        }

        private void Place(Entry entry, float seconds)
        {
            var body = entry.Body;
            entry.Rect.anchoredPosition = new Vector2(body.X, body.Y);
            entry.Rect.localRotation = Quaternion.Euler(0f, 0f, body.Angle);
            if (entry.Appear < 1f)
            {
                entry.Appear = Mathf.Min(1f, entry.Appear + seconds / 0.25f);
                entry.Rect.localScale = Vector3.one * Mathf.Lerp(HeldScale, 1f, PointerHand.EaseInOut(entry.Appear));
            }
        }

        private void OnEnteredWater(Entry entry, float speed)
        {
            var body = entry.Body;
            _surface.Disturb(body.X, -Mathf.Min(speed, 1500f) * 0.28f, 38f);
            if (_game != null && _game.Sfx != null) _game.Sfx.Splash();
            var drops = Mathf.Clamp(Mathf.RoundToInt(speed / 90f), 4, 14);
            var surfaceY = _surface.HeightAt(body.X);
            for (var i = 0; i < drops; i++)
                SpawnDrop(new Vector2(body.X + UnityEngine.Random.Range(-40f, 40f), surfaceY),
                    new Vector2(UnityEngine.Random.Range(-170f, 170f), UnityEngine.Random.Range(180f, 200f + Mathf.Min(speed, 900f) * 0.55f)));
            if (!body.Floats)
                for (var i = 0; i < 7; i++)
                    SpawnBubble(new Vector2(body.X + UnityEngine.Random.Range(-45f, 45f), surfaceY - UnityEngine.Random.Range(10f, 60f)), UnityEngine.Random.Range(9f, 22f), i % 3 == 0);
        }

        private void OnHitFloor(Entry entry, float speed)
        {
            var body = entry.Body;
            for (var i = 0; i < 5; i++)
                SpawnBubble(new Vector2(body.X + UnityEngine.Random.Range(-40f, 40f), FloorY + 30f + UnityEngine.Random.Range(0f, 25f)), UnityEngine.Random.Range(8f, 18f), false);
            _surface.Disturb(body.X, -Mathf.Min(speed, 500f) * 0.02f, 60f); // the lightest nudge: something just landed far below
        }

        // --- Voice ------------------------------------------------------------------------------------------------------

        private void Announce(Entry entry)
        {
            entry.Announced = true;
            _announced++;
            _announceQueue.Enqueue("sinkorfloat_say_" + entry.Id);
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

        // --- Bubbles, drops, hover, idle --------------------------------------------------------------------------------

        private void SpawnBubble(Vector2 position, float size, bool loud)
        {
            foreach (var bubble in _bubbles)
            {
                if (bubble.Alive) continue;
                bubble.Alive = true;
                bubble.Position = position;
                bubble.Size = size;
                bubble.Speed = UnityEngine.Random.Range(90f, 170f);
                bubble.Phase = UnityEngine.Random.Range(0f, 6.28f);
                bubble.Loud = loud;
                bubble.Rect.sizeDelta = new Vector2(size, size);
                bubble.Rect.anchoredPosition = position;
                bubble.Image.color = Color.white;
                bubble.Rect.gameObject.SetActive(true);
                return;
            }
        }

        private void UpdateBubbles(float seconds)
        {
            _idleBubble -= seconds;
            if (_idleBubble <= 0f)
            {
                _idleBubble = UnityEngine.Random.Range(0.9f, 2.0f);
                SpawnBubble(new Vector2(UnityEngine.Random.Range(-TankWidth * 0.5f + 30f, TankWidth * 0.5f - 30f), FloorY + 14f), UnityEngine.Random.Range(8f, 15f), false);
            }
            foreach (var bubble in _bubbles)
            {
                if (!bubble.Alive) continue;
                bubble.Phase += seconds * 5f;
                bubble.Position.y += bubble.Speed * seconds;
                bubble.Position.x += Mathf.Cos(bubble.Phase) * 22f * seconds;
                bubble.Rect.anchoredPosition = bubble.Position;
                var surface = _surface.HeightAt(bubble.Position.x);
                if (bubble.Position.y < surface - 3f) continue;
                bubble.Alive = false;
                bubble.Rect.gameObject.SetActive(false);
                _surface.Disturb(bubble.Position.x, 14f, 18f);
                if (bubble.Loud && _popCooldown <= 0f && _game != null && _game.Sfx != null)
                {
                    _popCooldown = 0.3f;
                    _game.Sfx.Pop();
                }
            }
        }

        private void SpawnDrop(Vector2 position, Vector2 velocity)
        {
            foreach (var drop in _drops)
            {
                if (drop.Alive) continue;
                drop.Alive = true;
                drop.Age = 0f;
                drop.Position = position;
                drop.Velocity = velocity;
                var size = UnityEngine.Random.Range(9f, 18f);
                drop.Rect.sizeDelta = new Vector2(size, size);
                drop.Rect.anchoredPosition = position;
                drop.Image.color = new Color(0.82f, 0.95f, 1f, 0.95f);
                drop.Rect.gameObject.SetActive(true);
                return;
            }
        }

        private void UpdateDrops(float seconds)
        {
            foreach (var drop in _drops)
            {
                if (!drop.Alive) continue;
                drop.Age += seconds;
                drop.Velocity.y -= 1800f * seconds;
                drop.Position += drop.Velocity * seconds;
                drop.Rect.anchoredPosition = drop.Position;
                if (drop.Velocity.y < 0f && drop.Position.y <= _surface.HeightAt(drop.Position.x))
                {
                    drop.Alive = false;
                    drop.Rect.gameObject.SetActive(false);
                    _surface.Disturb(drop.Position.x, -22f, 14f); // each drop that falls back makes its own small ring
                }
            }
        }

        private void UpdateHover(float seconds)
        {
            var over = _dragging != null && DropZone.Contains(Scene(_dragging.Rect.anchoredPosition));
            _excite = Mathf.MoveTowards(_excite, over ? 1f : 0f, seconds * 5f);
            _waterBack.Excite = _waterFront.Excite = _excite;
            if (_dragging != null)
            {
                var index = Array.IndexOf(_items, _dragging);
                if (index >= 0) _itemArt[index].localScale = Vector3.one * HeldArtScale * Mathf.Lerp(1f, HoverScale, _excite);
            }
        }

        private void UpdateIdle(float seconds)
        {
            if (_roundOver || _helpRunning || _dragging != null || _entries.Count >= Cells) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds)
            {
                _idle = 0f;
                _hintRoutine = _runner.StartCoroutine(RunHint());
            }
        }

        private Coroutine _hintRoutine;
        private int _hintCell = -1;

        // The child touched a thing while the hint hand was out: the hint stops, the hinted thing goes back to its place.
        private void CancelHint()
        {
            if (_hintRoutine != null) _runner.StopCoroutine(_hintRoutine);
            _hintRoutine = null;
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _game.Voice.Stop();
            if (_hintCell >= 0 && _onShelf[_hintCell])
            {
                var item = _items[_hintCell];
                item.Rect.anchoredPosition = Place(CellPosition(_hintCell));
                item.Rect.localScale = Vector3.one;
                _itemArt[_hintCell].localScale = Vector3.one;
                item.enabled = true;
            }
            _hintCell = -1;
            _helpRunning = false;
            _idle = 0f;
        }

        // After a long quiet: the hand carries the next object on the shelf toward the water and back. Nothing is dropped.
        private IEnumerator RunHint()
        {
            var cell = -1;
            for (var i = 0; i < Cells; i++) if (_onShelf[i]) { cell = i; break; }
            if (cell < 0) yield break;
            _helpRunning = true;
            _hinted = true;
            _hintCell = cell;
            var item = _items[cell];
            item.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("sinkorfloat_hint");

            var from = Place(CellPosition(cell));
            var to = Place(TankCentre + new Vector2(0f, 120f));
            yield return Carry(item, from, to, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(item, to, from, HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            item.Rect.anchoredPosition = from;
            item.enabled = true;
            _helpRunning = false;
            _idle = 0f;
        }

        private IEnumerator Carry(DragItem item, Vector2 from, Vector2 to, float seconds)
        {
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            item.Rect.anchoredPosition = from;
            item.Rect.SetAsLastSibling();
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                item.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            item.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || _entries.Count < Cells || _announced < Cells || _announcing) return;
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
            if (_roundIndex >= SinkOrFloat.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _tank.position);
        }

        private void ClearTank()
        {
            foreach (var entry in _entries) if (entry.Rect != null) UnityEngine.Object.Destroy(entry.Rect.gameObject);
            _entries.Clear();
            _bodies.Clear();
            foreach (var bubble in _bubbles) { bubble.Alive = false; bubble.Rect.gameObject.SetActive(false); }
            foreach (var drop in _drops) { drop.Alive = false; drop.Rect.gameObject.SetActive(false); }
            _confetti.SetActive(false);
        }

        // --- Small helpers ----------------------------------------------------------------------------------------------

        private static IEnumerator SlideTo(RectTransform target, Vector2 to, float seconds)
        {
            var from = target.anchoredPosition;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                target.anchoredPosition = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                yield return null;
            }
            target.anchoredPosition = to;
            target.localScale = Vector3.one;
        }

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

        private static RectTransform StretchChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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

        // A soap bubble: a thin bright rim, a faint fill and a small highlight, made once in code.
        private static Sprite _bubbleSprite;

        private static Sprite BubbleSprite()
        {
            if (_bubbleSprite != null) return _bubbleSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var half = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f - half) / half;
                    var dy = (y + 0.5f - half) / half;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.9f, r)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1f, r)));
                    var fill = r < 0.92f ? 0.14f : 0f;
                    var hx = dx + 0.38f;
                    var hy = dy - 0.4f;
                    var highlight = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.22f) * 0.8f;
                    var alpha = Mathf.Clamp01(rim * 0.85f + fill + highlight);
                    texture.SetPixel(x, y, new Color(0.9f, 0.97f, 1f, alpha));
                }
            }
            texture.Apply();
            _bubbleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _bubbleSprite;
        }
    }
}
