using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Cooking Measures: "Let's make pancakes!". Eva tells the child what they are making, then walks through the recipe one step
    // at a time on a kitchen counter with a bowl at the left. A cup step ("Pour half a cup of milk in the bowl"): the child drags the pitcher
    // over the cup, which tilts and pours until it holds what the recipe asks (the real empty / half / full cup pictures cross-fade), then drags
    // the cup over the bowl to tip it all in (twice for two cups). An add step ("Add two eggs"): the child drags the egg (or the spoon of sugar)
    // onto the bowl. A tiny picture of everything that went in lines up in front of the bowl. After the last step the dish appears.
    // Rules: Rules/CookingMeasure.cs; this draws them.
    //
    // The recipe card shows what the step asks (the half cup, two full cups, three eggs). The piece that is held lights up the place it goes.
    // Two misses in a row, or a long quiet: the hand does the step for the child (the only help; the coin is then Assisted).
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
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);
        public const float EvaHeight = 420f;
        public static readonly Vector2 CupHome = new Vector2(120f, -55f);          // the base stands on the counter (picture y -205)
        public static readonly Vector2 PitcherHome = new Vector2(600f, -70f);
        public static readonly Vector2 PourPoint = new Vector2(250f, 190f);       // where the pitcher hangs over the cup to pour
        public static readonly Vector2 BowlPoint = new Vector2(-300f, -140f);
        public static readonly Vector2 BowlTipPoint = new Vector2(-196f, 140f);   // where the cup hangs over the bowl to tip into it
        public static readonly Vector2 RecipePoint = new Vector2(-285f, 275f);
        public static readonly Vector2 DishPoint = new Vector2(120f, -10f);
        public static readonly Vector2 DishCorner = new Vector2(-10f, 345f);      // the small dish that says what is being made
        public const float PourReach = 170f;                                       // the pitcher this close to the pour point pours
        public const float TipReach = 150f;                                        // the cup this close to the tip point is tipped into the bowl
        public const float BowlReach = 190f;                                       // an egg or sugar this close to the bowl is in it
        public const float TapSize = 240f;
        private const float CupSize = 300f, PitcherSize = 270f, ItemSize = 220f, RecipeSize = 240f, RecipePanel = 340f;
        private const float BowlSize = 300f, TokenSize = 64f, TokenStep = 68f, TokenY = -258f, DishSize = 380f, DishSmall = 150f;
        private const int MaxTokens = 10;
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
        private RectTransform _evaAnchor, _items, _recipeField, _bowlField, _dishField, _recipePanel, _bowlHolder;
        private Piece _cup, _pitcher, _item;
        private Image _cupEmpty, _cupHalf, _cupFull, _stream, _recipePicture, _bowlGlow, _dish, _dishSmall;
        private readonly List<Image> _recipeMinis = new List<Image>();
        private readonly List<Image> _tally = new List<Image>();
        private readonly List<float> _tallyBorn = new List<float>();
        private int _tallyCount;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private CookRecipe _recipe;
        private CookRound[] _rounds;
        private CookRound _round;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing, _dirty;
        private float _idle, _time, _pitcherAngle, _cupAngle;
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
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public bool Pouring => _pouring;
        public bool Tipping => _tipping;
        public int InTheBowl => _tallyCount;
        public DragItem CupDrag => _cup.Drag;
        public DragItem PitcherDrag => _pitcher.Drag;
        public DragItem ItemDrag => _item.Drag;

        // The pieces' middles, in picture space; the Place methods put them there.
        public Vector2 CupPoint => Scene(_cup.Drag.Rect.anchoredPosition);
        public Vector2 PitcherPoint => Scene(_pitcher.Drag.Rect.anchoredPosition);
        public void PlaceCup(Vector2 point) => _cup.Drag.Rect.anchoredPosition = Place(point);
        public void PlacePitcher(Vector2 point) => _pitcher.Drag.Rect.anchoredPosition = Place(point);
        public void PlaceItem(Vector2 point) => _item.Drag.Rect.anchoredPosition = Place(point);

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
            _recipeField = Container("Recipe");
            BuildRecipe();
            _bowlField = Container("Bowl");
            BuildBowl();
            _items = Container("Items");
            BuildCup();
            BuildStream();
            BuildPitcher();
            BuildItem();
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
            _roundIndex = 0;
            _rightLineIndex = 0;
            _tallyCount = 0;
            foreach (var token in _tally) token.gameObject.SetActive(false);
            _dishSmall.sprite = EvaUi.Sprite("sciencelab/cookdish_" + _recipe.Dish);
            _dish.sprite = _dishSmall.sprite;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StopAllCoroutines();
            _runner.StartCoroutine(Loop());
            StartRound();
        }

        // Starts this step of the recipe (what the screen does by itself after each round; tests use it to reach a later step).
        public void ShowStep(int index)
        {
            _roundIndex = index;
            StartRound();
        }

        private void StartRound()
        {
            _round = _rounds[_roundIndex];
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _dirty = false;
            _pouring = _tipping = false;
            _pitcherAngle = _cupAngle = 0f;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            var what = _round.What;
            _cup.Drag.gameObject.SetActive(!_round.IsAdd);
            _pitcher.Drag.gameObject.SetActive(!_round.IsAdd);
            _item.Drag.gameObject.SetActive(_round.IsAdd);
            if (_round.IsAdd) _item.ArtImage.sprite = EvaUi.Sprite(ItemArt(what));
            else
            {
                _cupEmpty.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(what, CupLevel.Empty));
                _cupHalf.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(what, CupLevel.Half));
                _cupFull.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(what, CupLevel.Full));
                _pitcher.ArtImage.sprite = EvaUi.Sprite("sciencelab/cookpour_" + what);
                _stream.sprite = EvaUi.Sprite("sciencelab/cookstream_" + what);
            }
            ShowRecipeStep();
            foreach (var piece in new[] { _cup, _pitcher, _item })
            {
                piece.Held = false;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.localRotation = Quaternion.identity;
                piece.Art.localScale = Vector3.one;
            }
            ShowLevel();
            _runner.StartCoroutine(SayAsk());
        }

        // The recipe card shows the step: one half cup, the full cups, or the eggs / spoons to add.
        private void ShowRecipeStep()
        {
            var step = _round.Step;
            var sprite = _round.IsAdd
                ? EvaUi.Sprite(ItemArt(step.What))
                : EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(step.What, step.Level == CupLevel.Half ? CupLevel.Half : CupLevel.Full));
            for (var i = 0; i < _recipeMinis.Count; i++)
            {
                var mini = _recipeMinis[i];
                mini.gameObject.SetActive(i < step.Count);
                mini.sprite = sprite;
                var width = step.Count >= 3 ? 100f : step.Count == 2 ? 125f : RecipeSize * 0.8f;
                mini.rectTransform.sizeDelta = new Vector2(width, width);
                mini.rectTransform.anchoredPosition = new Vector2((i - (step.Count - 1) * 0.5f) * (width + 6f), 0f);
            }
        }

        private static string ItemArt(string what) => "sciencelab/cookitem_" + (what == "eggs" ? "egg" : "sugar");

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            if (_roundIndex == 0) yield return _game.Voice.SayAndWait("ck_make_" + _recipe.Dish);
            yield return _game.Voice.SayAndWait("ck_step_" + _round.Step.Id);
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

        // The recipe card: a round white panel with what the step asks, and the small dish that says what is being made.
        private void BuildRecipe()
        {
            var panel = Child(_recipeField, "Panel", new Vector2(RecipePanel, RecipePanel));
            var image = panel.gameObject.AddComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/dot");
            image.color = new Color(1f, 1f, 1f, 0.9f);
            image.raycastTarget = false;
            _recipePanel = panel;
            for (var i = 0; i < 3; i++) _recipeMinis.Add(ChildImage(panel, "Step" + i, new Vector2(100f, 100f)));
            _dishSmall = ChildImage(_recipeField, "DishSmall", new Vector2(DishSmall, DishSmall));
        }

        // The bowl on the counter and the tally of what went into it (a tiny picture for every cup, egg and spoon).
        private void BuildBowl()
        {
            _bowlHolder = Child(_bowlField, "BowlHolder", new Vector2(BowlSize, BowlSize));
            _bowlGlow = SoftGlow.Create(_bowlHolder, BowlSize * 1.5f);
            ChildImage(_bowlHolder, "Bowl", new Vector2(BowlSize, BowlSize)).sprite = EvaUi.Sprite("sciencelab/cookbowl");
            for (var i = 0; i < MaxTokens; i++)
            {
                var token = ChildImage(_bowlField, "Tally" + i, new Vector2(TokenSize, TokenSize));
                token.gameObject.SetActive(false);
                _tally.Add(token);
                _tallyBorn.Add(0f);
            }
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

        private void BuildPitcher()
        {
            _pitcher = MakePiece("Pitcher", PitcherHome, PitcherSize);
            _pitcher.ArtImage = ChildImage(_pitcher.Art, "Picture", new Vector2(PitcherSize, PitcherSize));
            _pitcher.Drag.BeginDrag += _ => OnBeginDrag(_pitcher);
            _pitcher.Drag.EndDrag += _ => OnEndDrag(_pitcher);
        }

        // The egg or the spoon of sugar, in the middle of the counter where the cup stands in a cup step.
        private void BuildItem()
        {
            _item = MakePiece("Item", CupHome, ItemSize);
            _item.ArtImage = ChildImage(_item.Art, "Picture", new Vector2(ItemSize, ItemSize));
            _item.Drag.BeginDrag += _ => OnBeginDrag(_item);
            _item.Drag.EndDrag += _ => OnEndDrag(_item);
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
            var piece = new Piece { Home = home };
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
            _recipeField.gameObject.SetActive(!ended);
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
            _idle = 0f;
        }

        // Let go: the pieces go home. A cup is judged if its level changed; an egg or sugar over the bowl is in it.
        private void OnEndDrag(Piece piece)
        {
            _cup.Held = _pitcher.Held = _item.Held = false;
            _idle = 0f;
            if (_roundOver) return;
            if (piece == _item)
            {
                if (Vector2.Distance(Scene(_item.Drag.Rect.anchoredPosition), BowlPoint) <= BowlReach) DropItem();
                return;
            }
            Judge();
        }

        // An egg or a spoon of sugar went into the bowl (also what the hint does).
        private void DropItem()
        {
            var result = _round.AddOne();
            Haptics.Tap();
            AddToTally(ItemArt(_round.What));
            _item.Art.localScale = Vector3.one;
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
            if (!_dirty) return;
            _dirty = false;
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
            var key = _round.CupReady ? "ck_tipall" : result == CookJudge.TooLittle ? "ck_more" : "ck_less";
            Say(key);
            while (_announcing) yield return null;
            if (_round.Done) yield break;
            if (_round.NeedsHint) StartHint();
        }

        // A tiny picture of what went into the bowl joins the row in front of it and pops in.
        private void AddToTally(string art)
        {
            if (_tallyCount >= _tally.Count) return;
            var token = _tally[_tallyCount];
            token.sprite = EvaUi.Sprite(art);
            token.gameObject.SetActive(true);
            _tallyBorn[_tallyCount] = _time;
            token.rectTransform.localScale = Vector3.zero;
            _tallyCount++;
        }

        private void UpdateTally()
        {
            for (var i = 0; i < _tallyCount; i++)
            {
                var token = _tally[i];
                var k = Mathf.Clamp01((_time - _tallyBorn[i]) / 0.35f);
                var back = k - 1f;
                token.rectTransform.localScale = Vector3.one * (1f + 3.2f * back * back * back + 2.2f * back * back); // pops past full size, then settles
                token.rectTransform.anchoredPosition = Place(new Vector2(BowlPoint.x + (i - (MaxTokens - 1) * 0.25f) * TokenStep, TokenY));
            }
            _bowlHolder.anchoredPosition = Place(BowlPoint);
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
            UpdateTally();
            UpdateHome(_cup, seconds);
            UpdateHome(_pitcher, seconds);
            UpdateHome(_item, seconds);
            ShowLevel();
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        // Where the pitcher hangs and where its spout is (picture space), given the tilt.
        private Vector2 SpoutPoint()
        {
            var middle = Scene(_pitcher.Drag.Rect.anchoredPosition);
            var turned = Quaternion.Euler(0f, 0f, _pitcherAngle) * new Vector3(-PitcherSize * 0.42f, PitcherSize * 0.18f, 0f);
            return middle + new Vector2(turned.x, turned.y);
        }

        // Where the cup spout is (picture space), given the tilt: it turns to the left, towards the bowl.
        private Vector2 CupSpout()
        {
            var middle = Scene(_cup.Drag.Rect.anchoredPosition);
            var turned = Quaternion.Euler(0f, 0f, _cupAngle) * new Vector3(-CupSize * 0.42f, CupSize * 0.1f, 0f);
            return middle + new Vector2(turned.x, turned.y);
        }

        private float LiquidY() => CupHome.y + CupBottom + _round.Level * CupInside;

        private float BowlMouthY() => BowlPoint.y + BowlSize * 0.2f;

        // The pitcher over the cup pours (the level rises); the cup over the bowl is tipped into it (the level falls).
        private void UpdatePour(float seconds)
        {
            if (_roundOver || _round.IsAdd) { _pouring = _tipping = false; }
            else
            {
                _pouring = Vector2.Distance(Scene(_pitcher.Drag.Rect.anchoredPosition), PourPoint) <= PourReach;
                _tipping = !_pouring && Vector2.Distance(Scene(_cup.Drag.Rect.anchoredPosition), BowlTipPoint) <= TipReach;
                if (_pouring && _round.Level < 1f) { _round.Add(seconds / PourSeconds); _dirty = true; _idle = 0f; }
                else if (_tipping && _round.Level > 0f) { _round.Add(-seconds / TipSeconds); _dirty = true; _idle = 0f; }
            }
            _pitcherAngle = Mathf.MoveTowards(_pitcherAngle, _pouring ? PourAngle : 0f, TiltSpeed * seconds);
            _cupAngle = Mathf.MoveTowards(_cupAngle, _tipping ? TipAngle : 0f, TiltSpeed * seconds);
            _pitcher.Art.localRotation = Quaternion.Euler(0f, 0f, _pitcherAngle);
            _cup.Art.localRotation = Quaternion.Euler(0f, 0f, _cupAngle);
            ShowGlows();
            ShowStream();
        }

        // The piece that is held lights up the place it goes: the cup while the pitcher is held, the bowl while the cup or the egg is held.
        private void ShowGlows()
        {
            var tipping = _round.IsAdd || _round.CupReady || !_round.NeedsMore;
            var wantCup = !_round.IsAdd && (_pitcher.Held || (_helpRunning && _round.NeedsMore));
            var wantBowl = _cup.Held || _item.Held || (_helpRunning && tipping);
            if (_roundOver) wantCup = wantBowl = false;
            if (wantCup) SoftGlow.Pulse(_cup.Glow, _time); else _cup.Glow.gameObject.SetActive(false);
            if (wantBowl) SoftGlow.Pulse(_bowlGlow, _time); else _bowlGlow.gameObject.SetActive(false);
            _pitcher.Glow.gameObject.SetActive(false);
            _item.Glow.gameObject.SetActive(false);
        }

        // The stream falls from the spout of whatever pours, into the cup or into the bowl.
        private void ShowStream()
        {
            var on = false;
            var top = Vector2.zero;
            var bottom = 0f;
            if (_pouring && _round.Level < 1f)
            {
                top = SpoutPoint();
                bottom = LiquidY();
                on = top.y > bottom + 10f && Mathf.Abs(top.x - CupHome.x) < 170f;
            }
            else if (_tipping && _round.Level > 0f)
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
            var level = _round.Level;
            _cupEmpty.color = Color.white;
            _cupHalf.color = new Color(1f, 1f, 1f, Mathf.Clamp01(level / 0.5f));
            _cupFull.color = new Color(1f, 1f, 1f, Mathf.Clamp01((level - 0.5f) / 0.5f));
            _recipePanel.anchoredPosition = Place(RecipePoint);
            _dishSmall.rectTransform.anchoredPosition = Place(DishCorner);
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
            if (_cup.Held || _pitcher.Held || _item.Held) { _idle = 0f; return; }
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
        private IEnumerator RunHint()
        {
            var fill = _round.NeedsMore;
            var piece = _round.IsAdd ? _item : fill ? _pitcher : _cup;
            var stop = _round.IsAdd ? BowlPoint : fill ? PourPoint : BowlTipPoint;
            var goal = fill ? CookingMeasure.LevelValue(_round.Target) : 0f;
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
            if (!_round.IsAdd)
            {
                // The level moves by itself while the piece hangs there (Tick); wait until it reaches the recipe.
                while (fill ? _round.Level < goal : _round.Level > goal) yield return null;
            }
            _hand.Pulse(false);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            if (_round.IsAdd) DropItem(); else Judge();
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_round.Done || _announcing) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
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

            _roundIndex++;
            if (_roundIndex >= _rounds.Length)
            {
                _bowlField.gameObject.SetActive(false);
                yield return ShowDish();
                yield return _game.Voice.SayAndWait("count_done");
                _eva.Cheer();
                SetSessionEnded(true);
                _dishField.gameObject.SetActive(true); // the dish stays under the two buttons
                yield return _game.Voice.SayAndWait("count_again");
            }
            else StartRound();
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
                var back = t / 0.5f - 1f;
                _dish.rectTransform.localScale = Vector3.one * (1f + 3.2f * back * back * back + 2.2f * back * back);
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
