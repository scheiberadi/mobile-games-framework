using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Cooking Measures: "Let's make pancakes!". Eva says what is being made. A recipe book at the top shows every step with its
    // picture (half a cup of milk, two eggs, ...); everything the recipe needs stands on the kitchen counter and the child adds it to the bowl
    // in any order. A pitcher dragged over the cup pours (the real empty / half / full cup pictures cross-fade) until the cup holds what the
    // book shows, then the cup is dragged over the bowl and tipped in (twice for two cups). An egg or a spoon of sugar is dragged onto the
    // bowl. What went in piles up inside the bowl, and the book ticks the step off. When every step is ticked the dish appears.
    // Rules: Rules/CookingMeasure.cs; this draws them.
    //
    // The piece that is held lights up the place it goes. A cup that holds one ingredient cannot take another (Eva says to tip it in first).
    // Two misses in a row, or a long quiet: the hand does the next step for the child (the only help; the coin is then Assisted).
    //
    // Layout in "picture space" like DayNightScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py).
    public sealed class CookingMeasuresScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Piece
        {
            public DragItem Drag;
            public RectTransform Art;
            public Image ArtImage;
            public Vector2 Home;
            public bool Held;
            public Image Glow;                  // the light that shows where this piece goes
            public CookRound Round;             // the step this ingredient belongs to (null for the cup)
            public bool IsPitcher;
            public float Angle;                 // how far a pitcher is tipped
            public float Size;
        }

        private sealed class BookRow
        {
            public RectTransform Root;
            public Image[] Minis;
            public Image Ring, Check;
            public float CheckBorn = -10f;
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);
        public const float EvaHeight = 420f;
        public static readonly Vector2 CupHome = new Vector2(-20f, -55f);         // the base stands on the counter (picture y -205)
        public static readonly Vector2 PourPoint = new Vector2(110f, 190f);       // where a pitcher hangs over the cup to pour
        public static readonly Vector2 BowlPoint = new Vector2(-300f, -87f);
        public static readonly Vector2 BowlTipPoint = new Vector2(-196f, 193f);   // where the cup hangs over the bowl to tip into it
        public static readonly Vector2 BookPoint = new Vector2(430f, 250f);
        public static readonly Vector2 DishPoint = new Vector2(120f, -10f);
        // Where the steps' ingredients stand on the counter, in the order of the recipe's steps.
        public static readonly Vector2[] SourceHomes =
        {
            new Vector2(210f, -70f), new Vector2(440f, -70f), new Vector2(650f, -100f), new Vector2(830f, -100f)
        };
        public const float PourReach = 170f;                                       // a pitcher this close to the pour point pours
        public const float TipReach = 150f;                                        // the cup this close to the tip point is tipped into the bowl
        public const float BowlReach = 190f;                                       // an egg or sugar this close to the bowl is in it
        public const float TapSize = 240f;
        private const float CupSize = 300f, PitcherSize = 220f, ItemSize = 160f;
        private const float BookWidth = 400f, BookHeight = 380f, RowHeight = 68f, MiniSize = 62f, DishSmall = 84f;
        private const float BowlSize = 300f, DishSize = 380f;
        private const int MaxRows = 4, MaxMinis = 3, BowlStages = 3;
        private const float PourSeconds = 3.2f, TipSeconds = 2.4f;
        private const float PourAngle = 48f, TipAngle = 50f, TiltSpeed = 260f;
        private const float CupBottom = -115f, CupInside = 190f;                  // the liquid runs from CupHome.y + CupBottom up by CupInside
        private const float ReturnSpeed = 2600f;

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
        private RectTransform _evaAnchor, _items, _bookField, _bowlField, _dishField, _bookPanel, _bowlHolder;
        private Piece _cup;
        private readonly List<Piece> _sources = new List<Piece>();
        private Image _cupEmpty, _cupHalf, _cupFull, _stream, _bowlGlow, _dish, _dishSmall, _bowlImage;
        private readonly List<BookRow> _rows = new List<BookRow>();
        private int _tallyCount;       // cups, eggs and spoons in the bowl
        private int _bowlStage;
        private float _bowlBorn = -10f;
        private readonly Sprite[] _bowlPictures = new Sprite[BowlStages + 1];
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private CookRecipe _recipe;
        private CookRound[] _rounds;
        private bool[] _celebrated;
        private CookRound _round;        // the step the child is working on now (the cup's, or the last one touched)
        private CookRound _cupRound;     // the step whose ingredient is in the cup
        private Piece _pouringPiece;
        private int _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing, _dirty, _busySaid;
        private float _idle, _time, _cupAngle;
        private bool _pouring, _tipping;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public CookingMeasuresScreen(ScreenId selfId, ScreenId homeScreenId, string background)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _background = background;
        }

        // Read-only state for tests.
        public CookRecipe Recipe => _recipe;
        public CookRound Round => _round;
        public CookRound CupRound => _cupRound;
        public CookRound[] Rounds => _rounds;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public bool Pouring => _pouring;
        public bool Tipping => _tipping;
        public int InTheBowl => _tallyCount;
        public DragItem CupDrag => _cup.Drag;

        // The drag piece of this step of the recipe, and whether the book shows it ticked off.
        public DragItem SourceDrag(int step) => _sources[step].Drag;
        public bool SourceShown(int step) => _sources[step].Drag.gameObject.activeSelf;
        public bool Ticked(int step) => _rows[step].Check.gameObject.activeSelf;
        public void PlaceSource(int step, Vector2 point) => _sources[step].Drag.Rect.anchoredPosition = Place(point);
        public Vector2 SourcePoint(int step) => Scene(_sources[step].Drag.Rect.anchoredPosition);

        // The cup's middle, in picture space; PlaceCup puts it there.
        public Vector2 CupPoint => Scene(_cup.Drag.Rect.anchoredPosition);
        public void PlaceCup(Vector2 point) => _cup.Drag.Rect.anchoredPosition = Place(point);

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
            _bookField = Container("Book");
            BuildBook();
            _bowlField = Container("Bowl");
            BuildBowl();
            _items = Container("Items");
            BuildCup();
            BuildStream();
            for (var i = 0; i < SourceHomes.Length; i++) BuildSource(i);
            _dishField = Container("Dish");
            BuildDish();
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
            _recipe = CookingMeasure.CreateRecipe(new System.Random());
            _rounds = CookingMeasure.CreateRounds(_recipe);
            _celebrated = new bool[_rounds.Length];
            _rightLineIndex = 0;
            _tallyCount = 0;
            ShowBowlStage(0);
            _dishSmall.sprite = EvaUi.Sprite("sciencelab/cookdish_" + _recipe.Dish);
            _dish.sprite = _dishSmall.sprite;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StopAllCoroutines();
            _runner.StartCoroutine(Loop());

            _round = _rounds[0];
            _cupRound = null;
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _dirty = false;
            _busySaid = false;
            _pouring = _tipping = false;
            _pouringPiece = null;
            _cupAngle = 0f;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            SetUpBook();
            SetUpSources();
            ResetCup();
            ShowLevel();
            _runner.StartCoroutine(SayAsk());
        }

        // The book shows every step: the pictures to add and an empty ring to tick.
        private void SetUpBook()
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.Root.gameObject.SetActive(i < _rounds.Length);
                row.Check.gameObject.SetActive(false);
                row.Ring.gameObject.SetActive(true);
                row.CheckBorn = -10f;
                if (i >= _rounds.Length) continue;
                var step = _rounds[i].Step;
                // An add step shows the eggs or spoons; a cup step shows what it is (the bag, the pitcher, the jug) and then its cups.
                var cups = step.Kind == CookStepKind.Cups;
                var sprite = cups
                    ? EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(step.What, step.Level == CupLevel.Half ? CupLevel.Half : CupLevel.Full))
                    : EvaUi.Sprite(ItemArt(step.What));
                for (var m = 0; m < MaxMinis; m++)
                {
                    var mini = row.Minis[m];
                    mini.gameObject.SetActive(cups ? m <= step.Count : m < step.Count);
                    mini.sprite = cups && m == 0 ? EvaUi.Sprite("sciencelab/cookpour_" + step.What) : sprite;
                }
            }
        }

        // Every step's ingredient stands on the counter: a pitcher for milk, flour or water, the egg, the spoon of sugar.
        private void SetUpSources()
        {
            for (var i = 0; i < _sources.Count; i++)
            {
                var piece = _sources[i];
                piece.Drag.gameObject.SetActive(i < _rounds.Length);
                if (i >= _rounds.Length) continue;
                var round = _rounds[i];
                piece.Round = round;
                piece.IsPitcher = !round.IsAdd;
                piece.Size = piece.IsPitcher ? PitcherSize : ItemSize;
                piece.Art.sizeDelta = new Vector2(piece.Size, piece.Size);
                piece.ArtImage.rectTransform.sizeDelta = new Vector2(piece.Size, piece.Size);
                piece.ArtImage.sprite = EvaUi.Sprite(piece.IsPitcher ? "sciencelab/cookpour_" + round.What : ItemArt(round.What));
                piece.Home = SourceHomes[i];
                piece.Held = false;
                piece.Angle = 0f;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.localRotation = Quaternion.identity;
            }
            _cup.Drag.gameObject.SetActive(true); // the end of the last session cleared it away
            _cup.Held = false;
            _cup.Drag.Rect.anchoredPosition = Place(_cup.Home);
            _cup.Art.localRotation = Quaternion.identity;
        }

        private void ResetCup()
        {
            _cupRound = null;
            _cupEmpty.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture("water", CupLevel.Empty));
            _cupHalf.sprite = null;
            _cupFull.sprite = null;
        }

        // The cup now holds this step's ingredient: its pictures (empty, half, full) are the ones that cross-fade.
        private void SetCupRound(CookRound round)
        {
            if (_cupRound == round) return;
            _cupRound = round;
            _round = round;
            _cupEmpty.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(round.What, CupLevel.Empty));
            _cupHalf.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(round.What, CupLevel.Half));
            _cupFull.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(round.What, CupLevel.Full));
            _stream.sprite = EvaUi.Sprite("sciencelab/cookstream_" + round.What);
        }

        private bool CupFree => _cupRound == null || _cupRound.Level <= CookingMeasure.EmptyTo;

        private CookRound FirstOpen()
        {
            foreach (var round in _rounds) if (!round.Done) return round;
            return null;
        }

        private Piece SourceOf(CookRound round) => _sources.Find(s => s.Round == round);

        private static string ItemArt(string what) => "sciencelab/cookitem_" + (what == "eggs" ? "egg" : "sugar");

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("ck_make_" + _recipe.Dish);
            yield return _game.Voice.SayAndWait("ck_book");
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

        // The recipe book: a cream page with the dish on top and a row for every step (its pictures and a ring that turns into a tick).
        private void BuildBook()
        {
            _bookPanel = Child(_bookField, "Page", new Vector2(BookWidth, BookHeight));
            var page = _bookPanel.gameObject.AddComponent<Image>();
            page.sprite = RoundedRectSprite.Get();
            page.type = Image.Type.Sliced;
            page.color = new Color(1f, 0.97f, 0.88f, 0.96f);
            page.raycastTarget = false;
            _dishSmall = ChildImage(_bookPanel, "Dish", new Vector2(DishSmall, DishSmall));
            _dishSmall.rectTransform.anchoredPosition = new Vector2(0f, BookHeight * 0.5f - DishSmall * 0.5f - 12f);
            for (var i = 0; i < MaxRows; i++)
            {
                var root = Child(_bookPanel, "Row" + i, new Vector2(BookWidth - 40f, RowHeight));
                root.anchoredPosition = new Vector2(0f, BookHeight * 0.5f - DishSmall - 34f - i * RowHeight);
                var row = new BookRow { Root = root, Minis = new Image[MaxMinis] };
                var line = ChildImage(root, "Line", new Vector2(BookWidth - 56f, 3f));
                line.sprite = EvaUi.Sprite("icons/dot");
                line.color = new Color(0.55f, 0.4f, 0.25f, 0.35f);
                line.rectTransform.anchoredPosition = new Vector2(0f, -RowHeight * 0.5f);
                for (var m = 0; m < MaxMinis; m++)
                {
                    row.Minis[m] = ChildImage(root, "Mini" + m, new Vector2(MiniSize, MiniSize));
                    row.Minis[m].rectTransform.anchoredPosition = new Vector2(-BookWidth * 0.5f + 62f + m * (MiniSize + 6f), 0f);
                }
                row.Ring = ChildImage(root, "Ring", new Vector2(52f, 52f));
                row.Ring.sprite = EvaUi.Sprite("icons/ring_thin");
                row.Ring.rectTransform.anchoredPosition = new Vector2(BookWidth * 0.5f - 62f, 0f);
                row.Check = ChildImage(root, "Check", new Vector2(56f, 56f));
                row.Check.sprite = EvaUi.Sprite("icons/check");
                row.Check.rectTransform.anchoredPosition = row.Ring.rectTransform.anchoredPosition;
                row.Check.gameObject.SetActive(false);
                _rows.Add(row);
            }
        }

        // The bowl on the counter; what went into it piles up inside it (a tiny picture for every cup, egg and spoon).
        private void BuildBowl()
        {
            _bowlHolder = Child(_bowlField, "BowlHolder", new Vector2(BowlSize, BowlSize));
            _bowlGlow = SoftGlow.Create(_bowlHolder, BowlSize * 1.5f);
            _bowlImage = ChildImage(_bowlHolder, "Bowl", new Vector2(BowlSize, BowlSize));
            // The bowl pictures fill up with batter: cookbowl (empty), cookbowl_1 .. cookbowl_3 (full). A missing one keeps the picture before it.
            _bowlPictures[0] = EvaUi.Sprite("sciencelab/cookbowl");
            for (var i = 1; i <= BowlStages; i++) _bowlPictures[i] = Resources.Load<Sprite>("Art/sciencelab/cookbowl_" + i);
            _bowlImage.sprite = _bowlPictures[0];
        }

        private void BuildCup()
        {
            _cup = MakePiece("Cup", CupHome, CupSize);
            _cupEmpty = ChildImage(_cup.Art, "Empty", new Vector2(CupSize, CupSize));
            _cupHalf = ChildImage(_cup.Art, "Half", new Vector2(CupSize, CupSize));
            _cupFull = ChildImage(_cup.Art, "Full", new Vector2(CupSize, CupSize));
            _cup.Drag.BeginDrag += _ => OnBeginDrag(_cup);
            _cup.Drag.EndDrag += _ => OnEndDrag(_cup);
        }

        private void BuildStream()
        {
            _stream = ChildImage(_items, "Stream", new Vector2(40f, 200f));
            _stream.preserveAspect = false;
            _stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            _stream.gameObject.SetActive(false);
        }

        // A place on the counter for one step's ingredient: a pitcher or an egg or a spoon (set up for each recipe).
        private void BuildSource(int index)
        {
            var piece = MakePiece("Source" + index, SourceHomes[index], PitcherSize);
            piece.ArtImage = ChildImage(piece.Art, "Picture", new Vector2(PitcherSize, PitcherSize));
            piece.Drag.BeginDrag += _ => OnBeginDrag(piece);
            piece.Drag.EndDrag += _ => OnEndDrag(piece);
            _sources.Add(piece);
        }

        // The dish that appears at the end of the session.
        private void BuildDish()
        {
            _dish = ChildImage(_dishField, "Dish", new Vector2(DishSize, DishSize));
            _dishField.gameObject.SetActive(false);
        }

        // A drag piece: an invisible 240-unit tap area with an art holder (the picture turns inside it).
        private Piece MakePiece(string name, Vector2 home, float size)
        {
            var piece = new Piece { Home = home, Size = size };
            piece.Drag = DragItem.Create(_items, name, EvaUi.Sprite("icons/dot"), Place(home), TapSize);
            piece.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            piece.Glow = SoftGlow.Create(piece.Drag.transform, size * 1.7f);
            piece.Art = Child(piece.Drag.transform, "Art", new Vector2(size, size));
            return piece;
        }

        private static RectTransform Child(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image ChildImage(Transform parent, string name, Vector2 size)
        {
            var rect = Child(parent, name, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
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

        // At the end the bench is cleared and the dish stands in the middle with the two buttons.
        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _items.gameObject.SetActive(!ended);
            _bookField.gameObject.SetActive(!ended);
            _bowlField.gameObject.SetActive(!ended);
            if (!ended) _dishField.gameObject.SetActive(false);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- Dragging ---------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Piece piece)
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for something never waits for the hint hand
            piece.Held = true;
            _busySaid = false;
            _idle = 0f;
        }

        // Let go: the pieces go home. A cup is judged if its level changed; an egg or sugar over the bowl is in it.
        private void OnEndDrag(Piece piece)
        {
            _cup.Held = false;
            foreach (var source in _sources) source.Held = false;
            _idle = 0f;
            if (_roundOver) return;
            if (piece != _cup && !piece.IsPitcher)
            {
                if (Vector2.Distance(Scene(piece.Drag.Rect.anchoredPosition), BowlPoint) <= BowlReach) DropItem(piece);
                return;
            }
            Judge();
        }

        // An egg or a spoon of sugar went into the bowl (also what the hint does).
        private void DropItem(Piece piece)
        {
            _round = piece.Round;
            if (_round.Done) return;
            var result = _round.AddOne();
            Haptics.Tap();
            AddToTally(ItemArt(_round.What));
            piece.Art.localScale = Vector3.one;
            if (result == CookJudge.Right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                return;
            }
            if (_game != null && _game.Sfx != null) _game.Sfx.Pop();
            Say("ck_" + _round.What + "_" + _round.InBowl);
        }

        private void Judge()
        {
            if (!_dirty || _cupRound == null) return;
            _dirty = false;
            _round = _cupRound;
            var result = _round.Judge();
            Haptics.Tap();
            switch (result)
            {
                case CookJudge.CupReady:
                    if (_game != null && _game.Sfx != null) _game.Sfx.Pop();
                    Say(_round.Target == CupLevel.Half ? "ck_half" : "ck_full");
                    return;
                case CookJudge.CupInBowl:
                    AddToTally(CupTokenArt());
                    if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                    Say("ck_cups_" + _round.InBowl);
                    return;
                case CookJudge.Right:
                    AddToTally(CupTokenArt());
                    if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                    return;
                default:
                    _runner.StartCoroutine(AfterMiss(result));
                    return;
            }
        }

        private string CupTokenArt() => "sciencelab/" + CookingMeasure.CupPicture(_round.What, _round.Target == CupLevel.Half ? CupLevel.Half : CupLevel.Full);

        // The cup is not right yet: Eva says what is missing (or the hand helps).
        private IEnumerator AfterMiss(CookJudge result)
        {
            var round = _round;
            var key = round.CupReady ? "ck_tipall" : result == CookJudge.TooLittle ? "ck_more" : "ck_less";
            Say(key);
            while (_announcing) yield return null;
            if (round.Done) yield break;
            if (round.NeedsHint) StartHint();
        }

        // Something went into the bowl: it fills up one more step (the bowl picture changes and gives a little bounce).
        private void AddToTally(string art)
        {
            _tallyCount++;
            var total = 0;
            foreach (var round in _rounds) total += round.Count;
            ShowBowlStage(_tallyCount >= total ? BowlStages : Mathf.Min(BowlStages - 1, 1 + (int)(2f * _tallyCount / total)));
        }

        private void ShowBowlStage(int stage)
        {
            if (stage != _bowlStage) _bowlBorn = _time;
            _bowlStage = stage;
            var picture = _bowlPictures[0];
            for (var i = 1; i <= stage; i++) if (_bowlPictures[i] != null) picture = _bowlPictures[i];
            _bowlImage.sprite = picture;
        }

        private void UpdateBowl()
        {
            _bowlHolder.anchoredPosition = Place(BowlPoint);
            var age = _time - _bowlBorn;
            _bowlHolder.localScale = Vector3.one * (age < 0.3f ? 1f + 0.08f * Mathf.Sin(age / 0.3f * Mathf.PI) : 1f);
        }

        // Pops past full size, then settles.
        private static float PopScale(float age)
        {
            var back = Mathf.Clamp01(age / 0.35f) - 1f;
            return 1f + 3.2f * back * back * back + 2.2f * back * back;
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
            UpdatePour(seconds);
            UpdateBowl();
            UpdateBook();
            UpdateHome(_cup, seconds);
            foreach (var source in _sources) UpdateHome(source, seconds);
            ShowLevel();
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdateBook()
        {
            _bookPanel.anchoredPosition = Place(BookPoint);
            foreach (var row in _rows) if (row.Check.gameObject.activeSelf) row.Check.rectTransform.localScale = Vector3.one * PopScale(_time - row.CheckBorn);
        }

        // Where the pitcher hangs and where its spout is (picture space), given the tilt.
        private Vector2 SpoutPoint(Piece pitcher)
        {
            var middle = Scene(pitcher.Drag.Rect.anchoredPosition);
            var turned = Quaternion.Euler(0f, 0f, pitcher.Angle) * new Vector3(-pitcher.Size * 0.42f, pitcher.Size * 0.18f, 0f);
            return middle + new Vector2(turned.x, turned.y);
        }

        // Where the cup spout is (picture space), given the tilt: it turns to the left, towards the bowl.
        private Vector2 CupSpout()
        {
            var middle = Scene(_cup.Drag.Rect.anchoredPosition);
            var turned = Quaternion.Euler(0f, 0f, _cupAngle) * new Vector3(-CupSize * 0.42f, CupSize * 0.1f, 0f);
            return middle + new Vector2(turned.x, turned.y);
        }

        private float LiquidY() => CupHome.y + CupBottom + (_cupRound == null ? 0f : _cupRound.Level) * CupInside;

        private float BowlMouthY() => BowlPoint.y + BowlSize * 0.2f;

        // A pitcher over the cup pours (the level rises); the cup over the bowl is tipped into it (the level falls).
        private void UpdatePour(float seconds)
        {
            _pouringPiece = null;
            _pouring = _tipping = false;
            if (!_roundOver)
            {
                foreach (var source in _sources)
                {
                    if (!source.IsPitcher || !source.Drag.gameObject.activeSelf || source.Round.Done) continue;
                    if (Vector2.Distance(Scene(source.Drag.Rect.anchoredPosition), PourPoint) > PourReach) continue;
                    if (CupFree || _cupRound == source.Round)
                    {
                        SetCupRound(source.Round);
                        _pouringPiece = source;
                        _pouring = true;
                    }
                    else if (!_busySaid)
                    {
                        _busySaid = true;
                        Say("ck_busy"); // the cup holds something else: it goes into the bowl first
                    }
                    break;
                }
                var near = Vector2.Distance(Scene(_cup.Drag.Rect.anchoredPosition), BowlTipPoint) <= TipReach;
                _tipping = !_pouring && near && _cupRound != null && _cupRound.Level > 0f;
                if (_pouring && _cupRound.Level < 1f) { _cupRound.Add(seconds / PourSeconds); _dirty = true; _idle = 0f; }
                else if (_tipping) { _cupRound.Add(-seconds / TipSeconds); _dirty = true; _idle = 0f; }
            }
            foreach (var source in _sources)
            {
                if (!source.IsPitcher) continue;
                source.Angle = Mathf.MoveTowards(source.Angle, source == _pouringPiece ? PourAngle : 0f, TiltSpeed * seconds);
                source.Art.localRotation = Quaternion.Euler(0f, 0f, source.Angle);
            }
            _cupAngle = Mathf.MoveTowards(_cupAngle, _tipping ? TipAngle : 0f, TiltSpeed * seconds);
            _cup.Art.localRotation = Quaternion.Euler(0f, 0f, _cupAngle);
            ShowGlows();
            ShowStream();
        }

        // The piece that is held lights up the place it goes: the cup while a pitcher is held, the bowl while the cup or an egg is held.
        private void ShowGlows()
        {
            var pitcherHeld = false;
            var itemHeld = false;
            foreach (var source in _sources)
            {
                if (!source.Held) continue;
                if (source.IsPitcher) pitcherHeld = true; else itemHeld = true;
            }
            var hintRound = _helpRunning ? _round : null;
            var hintPours = hintRound != null && !hintRound.IsAdd && hintRound.NeedsMore;
            var wantCup = pitcherHeld || hintPours;
            var wantBowl = (_cup.Held && _cupRound != null && !CupFree) || itemHeld || (_helpRunning && !hintPours);
            if (_roundOver) wantCup = wantBowl = false;
            if (wantCup) SoftGlow.Pulse(_cup.Glow, _time); else _cup.Glow.gameObject.SetActive(false);
            if (wantBowl) SoftGlow.Pulse(_bowlGlow, _time); else _bowlGlow.gameObject.SetActive(false);
            foreach (var source in _sources) source.Glow.gameObject.SetActive(false);
        }

        // The stream falls from the spout of whatever pours, into the cup or into the bowl.
        private void ShowStream()
        {
            var on = false;
            var top = Vector2.zero;
            var bottom = 0f;
            if (_pouring && _cupRound.Level < 1f)
            {
                top = SpoutPoint(_pouringPiece);
                bottom = LiquidY();
                on = top.y > bottom + 10f && Mathf.Abs(top.x - CupHome.x) < 170f;
            }
            else if (_tipping)
            {
                top = CupSpout();
                bottom = BowlMouthY();
                on = top.y > bottom + 10f && Mathf.Abs(top.x - BowlPoint.x) < 200f;
            }
            _stream.gameObject.SetActive(on);
            if (!on) return;
            var height = top.y - bottom;
            var aspect = _stream.sprite != null ? _stream.sprite.rect.width / _stream.sprite.rect.height : 0.3f;
            _stream.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(height * aspect * 1.4f, 60f, 140f), height);
            _stream.rectTransform.anchoredPosition = Place(top);
        }

        private void UpdateHome(Piece piece, float seconds)
        {
            if (piece.Held || _helpRunning) return;
            var target = Place(piece.Home);
            var position = piece.Drag.Rect.anchoredPosition;
            if (position != target) piece.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
        }

        // The cup picture shows the level: empty, then the half picture fades in, then the full one.
        private void ShowLevel()
        {
            var level = _cupRound == null ? 0f : _cupRound.Level;
            _cupEmpty.color = Color.white;
            _cupHalf.color = new Color(1f, 1f, 1f, Mathf.Clamp01(level / 0.5f));
            _cupFull.color = new Color(1f, 1f, 1f, Mathf.Clamp01((level - 0.5f) / 0.5f));
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

        private bool AnyHeld()
        {
            if (_cup.Held) return true;
            foreach (var source in _sources) if (source.Held) return true;
            return false;
        }

        private void UpdateIdle(float seconds)
        {
            if (_roundOver || _helpRunning) { _idle = 0f; return; }
            if (AnyHeld()) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        // The hint carries on with what is in the cup, or else starts the first step that is not ticked yet.
        private void StartHint()
        {
            if (_helpRunning || _roundOver) return;
            var round = _cupRound != null && !CupFree && !_cupRound.Done ? _cupRound : FirstOpen();
            if (round == null) return;
            _idle = 0f;
            _round = round;
            round.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint(round));
        }

        // The child took hold of something while the hint hand was out: the hint stops and the pieces go home.
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

        // The hand does the next thing of the step: pours, tips the cup into the bowl, or carries the egg or sugar to the bowl.
        private IEnumerator RunHint(CookRound round)
        {
            var fill = round.NeedsMore;
            var piece = round.IsAdd || fill ? SourceOf(round) : _cup;
            var stop = round.IsAdd ? BowlPoint : fill ? PourPoint : BowlTipPoint;
            var goal = fill ? CookingMeasure.LevelValue(round.Target) : 0f;
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("dressforweather_hint");

            var from = piece.Drag.Rect.anchoredPosition;
            var to = Place(stop);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                piece.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            piece.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            if (!round.IsAdd)
            {
                // The level moves by itself while the piece hangs there (Tick); wait until it reaches the recipe.
                while (fill ? round.Level < goal : round.Level > goal) yield return null;
            }
            _hand.Pulse(false);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            if (round.IsAdd) DropItem(piece); else Judge();
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        // A step whose last cup, egg or spoon is in the bowl gets ticked off (one at a time).
        private void CheckRoundEnd()
        {
            if (_roundOver) return;
            for (var i = 0; i < _rounds.Length; i++)
            {
                if (!_rounds[i].Done || _celebrated[i]) continue;
                _celebrated[i] = true;
                _roundOver = true;
                _runner.StartCoroutine(OnRoundComplete(i));
                return;
            }
        }

        private IEnumerator OnRoundComplete(int index)
        {
            var source = _sources[index];
            source.Drag.gameObject.SetActive(false); // there is nothing more of it to add
            var row = _rows[index];
            row.Ring.gameObject.SetActive(false);
            row.Check.gameObject.SetActive(true);
            row.CheckBorn = _time;
            while (_announcing) yield return null; // let the line about the last egg or cup finish
            yield return new WaitForSeconds(0.6f); // a moment to look at the bowl
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

            _hinted = false;
            var next = FirstOpen();
            if (next == null)
            {
                _bowlField.gameObject.SetActive(false);
                foreach (var piece in _sources) piece.Drag.gameObject.SetActive(false);
                _cup.Drag.gameObject.SetActive(false);
                _stream.gameObject.SetActive(false);
                yield return ShowDish();
                yield return _game.Voice.SayAndWait("count_done");
                _eva.Cheer();
                SetSessionEnded(true);
                _dishField.gameObject.SetActive(true); // the dish stays under the two buttons
                yield return _game.Voice.SayAndWait("count_again");
            }
            else
            {
                _confetti.SetActive(false);
                _round = next;
                _roundOver = false;
            }
        }

        // The ingredients became a dish: it pops in on the counter and Eva says what it is.
        private IEnumerator ShowDish()
        {
            _dishField.gameObject.SetActive(true);
            _dish.rectTransform.anchoredPosition = Place(DishPoint);
            _confetti.SetActive(true);
            if (_game.Sfx != null) _game.Sfx.Right();
            _runner.StartCoroutine(PopDish());
            yield return _game.Voice.SayAndWait("ck_dish_" + _recipe.Dish);
        }

        private IEnumerator PopDish()
        {
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                _dish.rectTransform.localScale = Vector3.one * PopScale(t / 0.5f * 0.35f);
                yield return null;
            }
            _dish.rectTransform.localScale = Vector3.one;
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
