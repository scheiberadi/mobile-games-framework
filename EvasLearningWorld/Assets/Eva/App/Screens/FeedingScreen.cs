using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // FEEDING presenter (Zoo & Farm Food, Rules/Feeding.cs): three hungry animals sit in a row, each with a thought bubble that shows
    // the foods it wishes for (one picture per portion). A conveyor belt below carries foods past, right to left, taken from the foods
    // of every animal the level can bring, not only the three on screen. The child drags a food from the belt to the animal that wishes
    // for it: the animal chomps, the picture in its bubble is ticked off, and when all its wishes are fed it is full, calls out with its
    // own sound and leaves, and the next animal takes its seat. A food the animal does not wish for is refused (it shakes its head, the
    // food rides on). The game runs until every animal is fed, then pays 1 coin like the other Zoo pairing games. The level ladder (six
    // levels: portions per animal, different foods per animal, animals per game, belt speed) is recorded once per animal.
    //
    // Help ladder as in PairingScreen: 2nd wrong food in a row a hint (the hand carries a wished-for food from the belt to its animal and
    // back), 3rd a demonstration (the hand feeds it). Dropping away from every animal is not an attempt: the food just rides on.
    //
    // The belt art is a still picture; the movement is the slat strip (belt_slats, one period of the pattern) scrolled as a repeating
    // texture over the belt's flat top. Foods ride at the same speed as the strip.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): animals (200) at y 20, x -310 / -10 / 290, their bubbles above so that the leftmost
    // clears the Home and Back buttons; foods (210, DragItem) on the belt at y -198; Eva and the player stand bottom-right, clear of the belt.
    public sealed class FeedingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Seat
        {
            public RectTransform Root, Animal, Bubble;
            public Image AnimalImage, Ring;
            public Image[] Icons, Ticks;
            public FeedingGuest Guest;
            public WorldPoint Centre;
            public bool Demonstrated;
        }

        private sealed class BeltItem
        {
            public DragItem Drag;
            public Image Image;
            public string Food;
            public float HomeX;      // where it would be on the belt right now (kept moving while the child holds it)
            public bool Alive, Held, Returning;
        }

        private static readonly float[] SeatX = { -310f, -10f, 290f };
        private const float SeatY = 20f;
        private const float AnimalSize = 200f;
        private const float BubbleWidth = 260f, BubbleHeight = 100f;
        private static readonly Vector2 BubbleOffset = new Vector2(-13f, 160f); // the tail of the picture is a little right of its middle

        private const float BeltWidth = 1075f, BeltHeight = 124f;
        private static readonly Vector2 BeltCentre = new Vector2(-180f, -345f);
        // The flat top of the belt picture where the slats scroll, in the belt picture's pixels from its top-left corner (tools/art-import/make-feeding-art.js).
        private const float SurfaceX = 109f, SurfaceY = 4f, SurfaceWidth = 858f, SurfaceHeight = 41f, SlatTile = 88f;
        private static readonly float[] BeltSpeedByLevel = { 85f, 95f, 105f, 115f, 125f, 135f };

        private const float ItemSize = 210f;
        private const float ItemY = -214f;
        private const float SpawnX = 290f, ExitX = -690f, Pitch = 230f; // SpawnX keeps a new food clear of the pair standing bottom-right
        private const int MaxItems = 7;

        private const float SnapRadius = 150f;
        private const float HoverScale = 1.07f;
        private const float ReturnSeconds = 0.25f;
        private const float ArriveSeconds = 0.22f;
        private const float ChompSeconds = 0.55f;
        private const float HopSeconds = 0.3f;
        private const float MaxSoundWait = 1.8f;
        private const float VanishSeconds = 0.25f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly string _promptKey, _hintKey, _demoKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _seatField, _beltField, _itemField;
        private RawImage _surface;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly Seat[] _seats = new Seat[FeedingGame.Seats];
        private readonly List<BeltItem> _items = new List<BeltItem>();

        private System.Random _rng;
        private FeedingGame _feeding;
        private int _level;
        private float _scroll;
        private int _mistakes;
        private bool _busy, _over, _beltRunning;
        private BeltItem _dragging;
        private int _hoverSeat = -1;
        private Coroutine _hoverRoutine;

        public FeedingScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            string promptKey, string hintKey, string demoKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _promptKey = promptKey;
            _hintKey = hintKey;
            _demoKey = demoKey;
        }

        // Read-only state, exposed so tests can drive the screen.
        public FeedingGame Game => _feeding;
        public int Level => _level;
        public IEnumerable<(string Food, Vector2 Position)> FoodsOnBelt =>
            _items.Where(i => i.Alive).Select(i => (i.Food, i.Drag.Rect.anchoredPosition));

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _beltField = CreateFullRectContainer("BeltField");
            _seatField = CreateFullRectContainer("SeatField");
            _itemField = CreateFullRectContainer("ItemField"); // after the animals, so a dragged food draws above them
            BuildBelt();
            BuildSeats();
            BuildItems();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewGame();
        }

        private void StartNewGame()
        {
            _runner.StopAllCoroutines();
            _rng = new System.Random();
            _level = _getLevel(_game.Progress);
            _feeding = new FeedingGame(_level, _rng);
            _mistakes = 0;
            _busy = false;
            _over = false;
            _beltRunning = false;
            _dragging = null;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetGameEnded(false);

            foreach (var item in _items) Kill(item);
            var onBelt = new List<string>();
            for (var x = SpawnX; x > ExitX + Pitch * 0.5f && onBelt.Count < MaxItems; x -= Pitch)
            {
                var food = _feeding.NextBeltFood(onBelt);
                onBelt.Add(food);
                Spawn(food, x, false);
            }
            for (var i = 0; i < _seats.Length; i++) { _seats[i].Guest = null; _seats[i].Demonstrated = false; SyncSeat(i); }

            _runner.StartCoroutine(BeltLoop());
            _runner.StartCoroutine(Intro());
        }

        private IEnumerator Intro()
        {
            _beltRunning = true;
            SetDragsEnabled(true);
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_promptKey);
            _eva.SetTalking(false);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildBelt()
        {
            var go = new GameObject("Belt", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_beltField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = BeltCentre;
            rect.sizeDelta = new Vector2(BeltWidth, BeltHeight);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("zoofarm/belt");
            image.raycastTarget = false;

            // The moving top: a repeating texture of one slat period, scrolled by shifting its uv window.
            var surfaceGo = new GameObject("BeltSurface", typeof(RectTransform), typeof(RawImage));
            surfaceGo.transform.SetParent(go.transform, false);
            var surfaceRect = (RectTransform)surfaceGo.transform;
            surfaceRect.anchorMin = surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            surfaceRect.pivot = new Vector2(0f, 1f);
            surfaceRect.anchoredPosition = new Vector2(SurfaceX - BeltWidth * 0.5f, BeltHeight * 0.5f - SurfaceY);
            surfaceRect.sizeDelta = new Vector2(SurfaceWidth, SurfaceHeight);
            _surface = surfaceGo.GetComponent<RawImage>();
            var texture = EvaUi.Sprite("zoofarm/belt_slats").texture;
            texture.wrapMode = TextureWrapMode.Repeat;
            _surface.texture = texture;
            _surface.uvRect = new Rect(0f, 0f, SurfaceWidth / SlatTile, 1f);
            _surface.raycastTarget = false;
        }

        private void BuildSeats()
        {
            for (var i = 0; i < _seats.Length; i++)
            {
                var seat = new Seat { Centre = new WorldPoint(SeatX[i], SeatY) };

                var rootGo = new GameObject("Seat" + i, typeof(RectTransform));
                rootGo.transform.SetParent(_seatField, false);
                seat.Root = (RectTransform)rootGo.transform;
                seat.Root.anchorMin = seat.Root.anchorMax = seat.Root.pivot = new Vector2(0.5f, 0.5f);
                seat.Root.anchoredPosition = new Vector2(SeatX[i], SeatY);
                seat.Root.sizeDelta = Vector2.zero;

                seat.Ring = NewImage("HoverRing", seat.Root, "icons/ring_thin", Vector2.zero, new Vector2(AnimalSize * 1.3f, AnimalSize * 1.3f));
                seat.Ring.gameObject.SetActive(false);
                seat.AnimalImage = NewImage("Animal", seat.Root, null, Vector2.zero, new Vector2(AnimalSize, AnimalSize));
                seat.Animal = seat.AnimalImage.rectTransform;

                var bubble = NewImage("Bubble", seat.Root, "zoofarm/bubble", BubbleOffset, new Vector2(BubbleWidth, BubbleHeight));
                seat.Bubble = bubble.rectTransform;
                seat.Icons = new Image[3];
                seat.Ticks = new Image[3];
                for (var k = 0; k < 3; k++)
                {
                    seat.Icons[k] = NewImage("Wish" + k, seat.Bubble, null, Vector2.zero, new Vector2(60f, 60f));
                    seat.Ticks[k] = NewImage("Tick" + k, seat.Icons[k].rectTransform, "icons/check", Vector2.zero, new Vector2(30f, 30f));
                }
                rootGo.SetActive(false);
                _seats[i] = seat;
            }
        }

        private static Image NewImage(string name, Transform parent, string sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            if (sprite != null) image.sprite = EvaUi.Sprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
            return image;
        }

        private void BuildItems()
        {
            for (var i = 0; i < MaxItems; i++)
            {
                var item = new BeltItem();
                item.Drag = DragItem.Create(_itemField, "beltitem" + i, EvaUi.Sprite("icons/dot"), new Vector2(SpawnX, ItemY), ItemSize);
                item.Image = item.Drag.GetComponent<Image>();
                item.Drag.BeginDrag += _ => OnBeginDrag(item);
                item.Drag.EndDrag += _ => OnEndDrag(item);
                item.Drag.gameObject.SetActive(false);
                _items.Add(item);
            }
        }

        // Shows the animal in a seat (or empties it) and its bubble; a newly arrived animal pops in.
        private void SyncSeat(int index)
        {
            var seat = _seats[index];
            var guest = _feeding.Guests[index];
            if (guest == null) { seat.Guest = null; seat.Root.gameObject.SetActive(false); return; }
            if (guest != seat.Guest)
            {
                seat.Guest = guest;
                seat.Demonstrated = false;
                seat.Root.gameObject.SetActive(true);
                seat.Root.localScale = Vector3.one;
                seat.Animal.localRotation = Quaternion.identity;
                seat.Animal.localScale = Vector3.one;
                seat.Animal.anchoredPosition = Vector2.zero;
                seat.AnimalImage.sprite = EvaUi.Sprite("zoofarm/animal_" + guest.AnimalId);
                seat.Ring.gameObject.SetActive(false);
                _runner.StartCoroutine(PopIn(seat.Root, 0.3f));
            }
            RefreshBubble(seat);
        }

        // One picture per portion in a row inside the cloud; a fed one is dimmed and ticked.
        private static void RefreshBubble(Seat seat)
        {
            var guest = seat.Guest;
            var count = guest.Wishes.Length;
            var size = count >= 3 ? 56f : (count == 2 ? 64f : 72f);
            var pitch = size + 8f;
            for (var k = 0; k < seat.Icons.Length; k++)
            {
                var active = k < count;
                seat.Icons[k].gameObject.SetActive(active);
                if (!active) continue;
                var rect = seat.Icons[k].rectTransform;
                rect.anchoredPosition = new Vector2((k - (count - 1) / 2f) * pitch, 10f);
                rect.sizeDelta = new Vector2(size, size);
                seat.Icons[k].sprite = EvaUi.Sprite("zoofarm/food_" + guest.Wishes[k]);
                seat.Icons[k].color = guest.Fed[k] ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
                seat.Ticks[k].gameObject.SetActive(guest.Fed[k]);
                seat.Ticks[k].rectTransform.sizeDelta = new Vector2(size * 0.55f, size * 0.55f);
            }
        }

        // --- The belt ----------------------------------------------------------------------------------------

        private float Speed => BeltSpeedByLevel[Mathf.Clamp(_level - DifficultyLadder.MinLevel, 0, BeltSpeedByLevel.Length - 1)];

        private IEnumerator BeltLoop()
        {
            while (true)
            {
                yield return null;
                if (!_beltRunning || _over) continue;
                var step = Speed * Time.deltaTime;
                _scroll = Mathf.Repeat(_scroll + step / SlatTile, 1f);
                _surface.uvRect = new Rect(_scroll, 0f, SurfaceWidth / SlatTile, 1f);
                foreach (var item in _items)
                {
                    if (!item.Alive) continue;
                    item.HomeX -= step;
                    if (item.Held || item.Returning) continue;
                    if (item.HomeX < ExitX) { Kill(item); continue; }
                    item.Drag.Rect.anchoredPosition = new Vector2(item.HomeX, ItemY);
                }
                TrySpawn();
            }
        }

        // A new food comes onto the belt at its right end once the last one has moved a pitch away.
        private void TrySpawn()
        {
            var alive = _items.Where(i => i.Alive).ToList();
            if (alive.Count >= MaxItems) return;
            if (alive.Count > 0 && alive.Max(i => i.HomeX) > SpawnX - Pitch) return;
            Spawn(_feeding.NextBeltFood(alive.Select(i => i.Food).ToList()), SpawnX, true);
        }

        private void Spawn(string food, float x, bool popIn)
        {
            var item = _items.First(i => !i.Alive);
            item.Alive = true;
            item.Held = false;
            item.Returning = false;
            item.Food = food;
            item.HomeX = x;
            item.Drag.gameObject.SetActive(true);
            item.Drag.enabled = !_busy && !_over && _beltRunning;
            item.Drag.Rect.anchoredPosition = new Vector2(x, ItemY);
            item.Drag.Rect.sizeDelta = new Vector2(ItemSize, ItemSize);
            item.Drag.Rect.localScale = Vector3.one;
            item.Drag.Rect.localRotation = Quaternion.identity;
            item.Image.sprite = EvaUi.Sprite("zoofarm/food_" + food);
            item.Image.color = Color.white;
            item.Image.raycastTarget = true;
            if (popIn) _runner.StartCoroutine(PopIn(item.Drag.Rect, 0.25f));
        }

        private static void Kill(BeltItem item)
        {
            item.Alive = false;
            item.Held = false;
            item.Returning = false;
            item.Drag.gameObject.SetActive(false);
        }

        private void SetDragsEnabled(bool enabled)
        {
            foreach (var item in _items) item.Drag.enabled = enabled && item.Alive;
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void OnBeginDrag(BeltItem item)
        {
            if (_busy || _over) return;
            _dragging = item;
            item.Held = true;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop(item));
        }

        // While a food is held, the nearest animal in snap range grows; every other animal rests.
        private IEnumerator HoverLoop(BeltItem item)
        {
            while (true)
            {
                var nearest = NearestSeat(item.Drag.Rect.anchoredPosition);
                if (nearest != _hoverSeat) SetHover(nearest);
                yield return null;
            }
        }

        private int NearestSeat(Vector2 position)
        {
            // Only animals that are showing count: an empty seat sits far away from every drop.
            var centres = new WorldPoint[_seats.Length];
            for (var i = 0; i < _seats.Length; i++)
                centres[i] = _seats[i].Guest != null && !_seats[i].Guest.IsFull && _seats[i].Root.gameObject.activeSelf ? _seats[i].Centre : new WorldPoint(100000f, 100000f);
            return DropGeometry.NearestWithinRadius(position.x, position.y, centres, SnapRadius);
        }

        private void SetHover(int seat)
        {
            _hoverSeat = seat;
            for (var i = 0; i < _seats.Length; i++)
            {
                if (_seats[i].Guest == null) continue;
                var hovered = i == seat;
                _seats[i].Root.localScale = Vector3.one * (hovered ? HoverScale : 1f);
                _seats[i].Ring.gameObject.SetActive(hovered);
            }
        }

        private void StopHover()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hoverSeat = -1;
            foreach (var seat in _seats)
            {
                if (seat == null || seat.Root == null) continue;
                seat.Root.localScale = Vector3.one;
                seat.Ring.gameObject.SetActive(false);
            }
        }

        private void OnEndDrag(BeltItem item)
        {
            StopHover();
            _dragging = null;
            if (_feeding == null || !item.Alive) return;
            if (_busy || _over) { Release(item); return; }

            var seat = NearestSeat(item.Drag.Rect.anchoredPosition);
            if (seat < 0) { Release(item); return; } // empty space: not an attempt, the food rides on
            var result = _feeding.Feed(seat, item.Food, out var wish);
            item.Held = true; // out of the belt's hands while the animal deals with it
            _runner.StartCoroutine(result == FeedResult.Refused ? Refuse(item, seat) : Eat(item, seat, result, wish));
        }

        // The food goes back to riding the belt: it slides to where it would be by now.
        private void Release(BeltItem item)
        {
            item.Held = false;
            _runner.StartCoroutine(ReturnToBelt(item));
        }

        private IEnumerator ReturnToBelt(BeltItem item)
        {
            item.Returning = true;
            var from = item.Drag.Rect.anchoredPosition;
            var fromScale = item.Drag.Rect.localScale.x;
            for (var t = 0f; t < ReturnSeconds && item.Alive; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / ReturnSeconds);
                item.Drag.Rect.anchoredPosition = Vector2.Lerp(from, new Vector2(item.HomeX, ItemY), k);
                item.Drag.Rect.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, k);
                yield return null;
            }
            item.Returning = false;
            if (item.Alive) item.Drag.Rect.anchoredPosition = new Vector2(item.HomeX, ItemY);
        }

        // --- Eating ------------------------------------------------------------------------------------------

        private static Vector2 CentreOf(Seat seat) => new Vector2(seat.Centre.X, seat.Centre.Y);

        private Vector2 MouthOf(Seat seat) => new Vector2(seat.Centre.X, seat.Centre.Y - 25f);

        // The food slides to the animal, is chomped up, its picture in the bubble is ticked; a full animal calls out and leaves.
        // Nothing else waits for the chomp or the leaving animal: the next food can be dragged at once.
        private IEnumerator Eat(BeltItem item, int index, FeedResult result, int wish)
        {
            var seat = _seats[index];
            _mistakes = 0;
            yield return SlideScale(item.Drag.Rect, MouthOf(seat), 0.55f, ArriveSeconds);

            _game.Sfx.Chomp();
            Haptics.Tap();
            _runner.StartCoroutine(Chomp(seat.Animal));
            _runner.StartCoroutine(ShrinkAway(item.Drag.Rect, ChompSeconds * 0.6f));
            RefreshBubble(seat);
            yield return new WaitForSeconds(ChompSeconds);
            Kill(item);

            if (result == FeedResult.Full) _runner.StartCoroutine(LeaveThenFinish(index));
        }

        private IEnumerator LeaveThenFinish(int index)
        {
            yield return Leave(index);
            if (_feeding.Finished && !_over) yield return Finish();
        }

        private IEnumerator Leave(int index)
        {
            var seat = _seats[index];
            var clean = !seat.Demonstrated;
            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));

            _game.Sfx.Coin();
            Haptics.Tap();
            var heard = _game.Sfx.PlayAnimal(seat.Guest.AnimalId);
            if (heard <= 0f && ZooFarmAnimals.All.Any(a => a.Id == seat.Guest.AnimalId && a.RealmOf == Realm.Sea)) { _game.Sfx.Splash(); heard = 0.8f; } // a swimmer with no call of its own
            var wait = Mathf.Clamp(heard, HopSeconds * 2f, MaxSoundWait);
            var hops = Mathf.Clamp(Mathf.RoundToInt(wait / HopSeconds), 2, 6);
            yield return Hops(seat.Animal, Vector2.zero, hops);

            for (var t = 0f; t < VanishSeconds; t += Time.deltaTime)
            {
                seat.Root.localScale = Vector3.one * Mathf.Lerp(1f, 0f, t / VanishSeconds);
                yield return null;
            }
            seat.Root.localScale = Vector3.zero;
            _feeding.Dismiss(index);
            SyncSeat(index);
        }

        private IEnumerator Chomp(RectTransform animal)
        {
            for (var t = 0f; t < ChompSeconds; t += Time.deltaTime)
            {
                var k = Mathf.Sin(t / ChompSeconds * Mathf.PI * 3f);
                animal.localScale = new Vector3(1f + 0.1f * k, 1f - 0.12f * k, 1f);
                yield return null;
            }
            animal.localScale = Vector3.one;
        }

        private static IEnumerator ShrinkAway(RectTransform rect, float seconds)
        {
            var from = rect.localScale.x;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(from, 0f, t / seconds);
                yield return null;
            }
            rect.localScale = Vector3.zero;
        }

        private IEnumerator Hops(RectTransform rect, Vector2 home, int hops)
        {
            for (var h = 0; h < hops; h++)
                for (var t = 0f; t < HopSeconds; t += Time.deltaTime)
                {
                    rect.anchoredPosition = home + new Vector2(0f, Mathf.Sin(t / HopSeconds * Mathf.PI) * 45f);
                    yield return null;
                }
            rect.anchoredPosition = home;
        }

        // --- A wrong food ------------------------------------------------------------------------------------

        // The animal shakes its head at the food, which then rides on; the mistake climbs the help ladder.
        private IEnumerator Refuse(BeltItem item, int index)
        {
            _busy = true;
            SetDragsEnabled(false);
            var seat = _seats[index];
            yield return SlideScale(item.Drag.Rect, MouthOf(seat), 0.7f, ArriveSeconds);
            _game.Sfx.Retry();
            yield return Wobble(seat.Animal, 0.5f);
            item.Held = false;
            yield return ReturnToBelt(item);
            _busy = false;
            SetDragsEnabled(true);
            HandleMistake();
        }

        // --- Help ladder -------------------------------------------------------------------------------------

        // Mistakes in a row since the last meal: the 1st is only the reaction, the 2nd a hint, the 3rd on a demonstration.
        private void HandleMistake()
        {
            _mistakes++;
            if (_mistakes == 1) return;
            if (!TryFindFeed(out var item, out var seat)) return;
            if (_mistakes == 2)
            {
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                _runner.StartCoroutine(RunHint(item, seat));
            }
            else
            {
                _mistakes = 0;
                _runner.StartCoroutine(RunDemonstrate(item, seat));
            }
        }

        // A food the hand can reach (well on the belt) and the animal that wishes for it.
        private bool TryFindFeed(out BeltItem item, out int seat)
        {
            var candidates = _items.Where(i => i.Alive && !i.Held && !i.Returning && i.HomeX > ExitX + 100f && i.HomeX < SpawnX - 40f).ToList();
            if (_feeding.TryFindFeed(candidates.Select(i => i.Food).ToList(), out var index, out seat)) { item = candidates[index]; return true; }
            item = null;
            return false;
        }

        // The belt stops; the hand grabs the food, carries it to the animal that wishes for it, then back.
        private IEnumerator RunHint(BeltItem item, int index)
        {
            _busy = true;
            _beltRunning = false;
            SetDragsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintKey);

            var from = item.Drag.Rect.anchoredPosition;
            var to = CentreOf(_seats[index]);
            item.Held = true;
            yield return Carry(item, from, to, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(item, to, from, HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            item.Held = false;
            _beltRunning = true;
            _busy = false;
            if (!_over) SetDragsEnabled(true);
        }

        // The belt stops; the hand carries the food to the animal and it is fed for the child.
        private IEnumerator RunDemonstrate(BeltItem item, int index)
        {
            _busy = true;
            _beltRunning = false;
            SetDragsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoKey) * 0.3f);

            item.Held = true;
            yield return Carry(item, item.Drag.Rect.anchoredPosition, CentreOf(_seats[index]), HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _seats[index].Demonstrated = true;
            _beltRunning = true;
            _busy = false;
            var result = _feeding.Feed(index, item.Food, out var wish);
            yield return Eat(item, index, result, wish);
        }

        // The hand glides to the food, "grabs" it, then moves with it to `to`, fingertip on its centre.
        private IEnumerator Carry(BeltItem item, Vector2 from, Vector2 to, float seconds)
        {
            var rect = item.Drag.Rect;
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from;
            rect.SetAsLastSibling();
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
        }

        // --- The end -----------------------------------------------------------------------------------------

        private IEnumerator Finish()
        {
            _over = true;
            _beltRunning = false;
            _hand.Hide();
            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));

            // One coin for the whole game, like the other Zoo pairing games.
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(1);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + 1, _game.Sfx, _seats[0].Root.position);

            yield return _game.Voice.SayAndWait("count_done");
            SetGameEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
        }

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
                EndButtonPositions[0], EndButtonSize, StartNewGame);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));

            _endPanel.SetActive(false);
        }

        private void SetGameEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _seatField.gameObject.SetActive(!ended);
            _beltField.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ---------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens ------------------------------------------------------------------------------------

        private static IEnumerator SlideScale(RectTransform target, Vector2 to, float scale, float seconds)
        {
            var from = target.anchoredPosition;
            var fromScale = target.localScale.x;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / seconds);
                target.anchoredPosition = Vector2.Lerp(from, to, k);
                target.localScale = Vector3.one * Mathf.Lerp(fromScale, scale, k);
                yield return null;
            }
            target.anchoredPosition = to;
            target.localScale = Vector3.one * scale;
        }

        private static IEnumerator PopIn(RectTransform target, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 10f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay);
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

        // --- Helpers -----------------------------------------------------------------------------------------

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
