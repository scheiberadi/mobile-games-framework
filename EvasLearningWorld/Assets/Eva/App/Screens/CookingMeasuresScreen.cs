using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Cooking Measures: "Fill the cup". On a kitchen counter stand a measuring cup and a pitcher (water, flour or milk). Eva asks
    // for a cup that is full, half full or empty and a recipe card shows the cup to match. The child drags the pitcher over the cup: it tilts
    // and pours, and the level rises (the real empty / half / full cup pictures cross-fade). Dragging the cup away tips it out and the level
    // falls. When the child lets go, Eva looks at the cup: right ends the round, otherwise she says a little more or too much. Rules:
    // Rules/CookingMeasure.cs; this draws them.
    //
    // Two misses in a row, or a long quiet: the hand pours (or tips out) to the recipe for the child (the only help; the coin is then Assisted).
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
        public static readonly Vector2 TipPoint = new Vector2(450f, 200f);        // where the cup hangs over the jug to tip out into it
        public static readonly Vector2 RecipePoint = new Vector2(-230f, 235f);
        public const float PourReach = 170f;                                       // the pitcher this close to the pour point pours
        public const float TipReach = 150f;                                        // the cup this close to the tip point is tipped into the jug
        public const float TapSize = 240f;
        private const float CupSize = 300f, PitcherSize = 270f, RecipeSize = 290f, RecipePanel = 420f;
        private const float PourSeconds = 3.2f, TipSeconds = 2.4f;
        private const float PourAngle = 48f, TipAngle = 50f, TiltSpeed = 260f, FlipOffset = 80f;
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
        private RectTransform _evaAnchor, _items, _recipeField;
        private Piece _cup, _pitcher;
        private Image _cupEmpty, _cupHalf, _cupFull, _stream, _recipePicture;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private CookRound _round;
        private CookRound[] _session;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing, _dirty;
        private float _idle, _time, _pitcherAngle, _cupAngle;
        private bool _pouring, _tipping, _cupFlipped;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public CookingMeasuresScreen(ScreenId selfId, ScreenId homeScreenId, string background)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _background = background;
        }

        // Read-only state for tests.
        public CookRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public bool Pouring => _pouring;
        public bool Tipping => _tipping;
        public DragItem CupDrag => _cup.Drag;
        public DragItem PitcherDrag => _pitcher.Drag;

        // The pieces' middles, in picture space; the Place methods put them there.
        public Vector2 CupPoint => Scene(_cup.Drag.Rect.anchoredPosition);
        public Vector2 PitcherPoint => Scene(_pitcher.Drag.Rect.anchoredPosition);
        public void PlaceCup(Vector2 point) => _cup.Drag.Rect.anchoredPosition = Place(point);
        public void PlacePitcher(Vector2 point) => _pitcher.Drag.Rect.anchoredPosition = Place(point);

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
            _items = Container("Items");
            BuildCup();
            BuildStream();
            BuildPitcher();
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
            _session = CookingMeasure.CreateSession(new System.Random());
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
            _dirty = false;
            _pouring = _tipping = _cupFlipped = false;
            _pitcherAngle = _cupAngle = 0f;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            var ingredient = _round.Ingredient;
            _cupEmpty.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(ingredient, CupLevel.Empty));
            _cupHalf.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(ingredient, CupLevel.Half));
            _cupFull.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(ingredient, CupLevel.Full));
            _recipePicture.sprite = EvaUi.Sprite("sciencelab/" + CookingMeasure.CupPicture(ingredient, _round.Target));
            _pitcher.ArtImage.sprite = EvaUi.Sprite("sciencelab/cookpour_" + ingredient);
            _stream.sprite = EvaUi.Sprite("sciencelab/cookstream_" + ingredient);
            foreach (var piece in new[] { _cup, _pitcher })
            {
                piece.Held = false;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.localRotation = Quaternion.identity;
                piece.Art.localScale = Vector3.one;
            }
            ShowLevel();
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("ck_ask_" + _round.Ingredient + "_" + CookingMeasure.IdOf(_round.Target));
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

        // The recipe card: a round white panel with the cup picture to match.
        private void BuildRecipe()
        {
            var panel = Child(_recipeField, "Panel", new Vector2(RecipePanel, RecipePanel));
            var image = panel.gameObject.AddComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/dot");
            image.color = new Color(1f, 1f, 1f, 0.9f);
            image.raycastTarget = false;
            _recipePicture = ChildImage(panel, "Cup", new Vector2(RecipeSize, RecipeSize));
            _recipePanel = panel;
        }

        private RectTransform _recipePanel;

        private void BuildCup()
        {
            _cup = MakePiece("Cup", CupHome, CupSize);
            _cupEmpty = ChildImage(_cup.Art, "Empty", new Vector2(CupSize, CupSize));
            _cupHalf = ChildImage(_cup.Art, "Half", new Vector2(CupSize, CupSize));
            _cupFull = ChildImage(_cup.Art, "Full", new Vector2(CupSize, CupSize));
            _cup.Drag.BeginDrag += _ => OnBeginDrag(_cup);
            _cup.Drag.EndDrag += _ => OnEndDrag();
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
            _pitcher.Drag.EndDrag += _ => OnEndDrag();
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

        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _items.gameObject.SetActive(!ended);
            _recipeField.gameObject.SetActive(!ended);
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

        // Let go: the pieces go home and, if the level changed, Eva looks at the cup.
        private void OnEndDrag()
        {
            _cup.Held = false;
            _pitcher.Held = false;
            _idle = 0f;
            if (_roundOver) return;
            Judge();
        }

        private void Judge()
        {
            if (!_dirty) return;
            _dirty = false;
            var result = _round.Judge();
            Haptics.Tap();
            if (result == CookJudge.Right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                Say("ck_say_" + CookingMeasure.IdOf(_round.Target));
                return;
            }
            _runner.StartCoroutine(AfterMiss(result));
        }

        // The level is not right yet: Eva says what is missing (or the hand helps).
        private IEnumerator AfterMiss(CookJudge result)
        {
            Say(result == CookJudge.TooLittle ? "ck_more" : "ck_less");
            while (_announcing) yield return null;
            if (_round.Done) yield break;
            if (_round.NeedsHint) StartHint();
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
            UpdateHome(_cup, seconds);
            UpdateHome(_pitcher, seconds);
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

        private float LiquidY() => CupHome.y + CupBottom + _round.Level * CupInside;

        // Where the cup's spout is (picture space), given the tilt; the cup turns over to the right, towards the jug, so it is mirrored.
        private Vector2 CupSpout()
        {
            var middle = Scene(_cup.Drag.Rect.anchoredPosition);
            var sign = _cupFlipped ? 1f : -1f;
            var turned = Quaternion.Euler(0f, 0f, _cupFlipped ? -_cupAngle : _cupAngle) * new Vector3(sign * CupSize * 0.42f, CupSize * 0.1f, 0f);
            return middle + new Vector2(turned.x, turned.y);
        }

        private float JugMouthY() => PitcherHome.y + PitcherSize * 0.4f;

        // The pitcher over the cup pours (the level rises); the cup over the jug is tipped into it (the level falls). The piece that is held lights
        // up the place it goes: the cup while the pitcher is held, the jug while the cup is held.
        private void UpdatePour(float seconds)
        {
            if (_roundOver) { _pouring = _tipping = false; }
            else
            {
                _pouring = Vector2.Distance(Scene(_pitcher.Drag.Rect.anchoredPosition), PourPoint) <= PourReach;
                _tipping = !_pouring && Vector2.Distance(Scene(_cup.Drag.Rect.anchoredPosition), TipPoint) <= TipReach;
                if (_pouring && _round.Level < 1f) { _round.Add(seconds / PourSeconds); _dirty = true; _idle = 0f; }
                else if (_tipping && _round.Level > 0f) { _round.Add(-seconds / TipSeconds); _dirty = true; _idle = 0f; }
            }
            _cupFlipped = Scene(_cup.Drag.Rect.anchoredPosition).x > CupHome.x + FlipOffset;
            _pitcherAngle = Mathf.MoveTowards(_pitcherAngle, _pouring ? PourAngle : 0f, TiltSpeed * seconds);
            _cupAngle = Mathf.MoveTowards(_cupAngle, _tipping ? TipAngle : 0f, TiltSpeed * seconds);
            _pitcher.Art.localRotation = Quaternion.Euler(0f, 0f, _pitcherAngle);
            _cup.Art.localRotation = Quaternion.Euler(0f, 0f, _cupFlipped ? -_cupAngle : _cupAngle);
            _cup.Art.localScale = new Vector3(_cupFlipped ? -1f : 1f, 1f, 1f);
            ShowGlows();
            ShowStream();
        }

        private void ShowGlows()
        {
            var wantCup = _pitcher.Held || (_helpRunning && _round.NeedsMore);
            var wantJug = _cup.Held || (_helpRunning && !_round.NeedsMore);
            if (_roundOver) wantCup = wantJug = false;
            if (wantCup) SoftGlow.Pulse(_cup.Glow, _time); else _cup.Glow.gameObject.SetActive(false);
            if (wantJug) SoftGlow.Pulse(_pitcher.Glow, _time); else _pitcher.Glow.gameObject.SetActive(false);
        }

        // The stream falls from the spout of whatever pours, into the cup or into the jug.
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
                bottom = JugMouthY();
                on = top.y > bottom + 10f && Mathf.Abs(top.x - PitcherHome.x) < 200f;
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
            var place = _recipePanel;
            place.anchoredPosition = Place(RecipePoint);
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
            if (_cup.Held || _pitcher.Held) { _idle = 0f; return; }
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

        // The hand pours (or tips out) until the cup matches the recipe, then lets go.
        private IEnumerator RunHint()
        {
            var fill = _round.NeedsMore;
            var piece = fill ? _pitcher : _cup;
            var stop = fill ? PourPoint : TipPoint;
            var goal = CookingMeasure.LevelValue(_round.Target);
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
            // The level moves by itself while the piece hangs there (Tick); wait until it reaches the recipe.
            while (fill ? _round.Level < goal : _round.Level > goal) yield return null;
            _hand.Pulse(false);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            Judge();
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
            yield return new WaitForSeconds(0.6f); // a moment to look at the cup
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
            if (_roundIndex >= CookingMeasure.RoundsPerSession)
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
