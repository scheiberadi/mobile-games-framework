using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Healthy vs Unhealthy: "Feed Eva". Four foods lie on the kitchen table and Eva sits on the rug, hungry, with five empty
    // stars above her. The child drags a food to her mouth. A healthy one is eaten: she chomps and bounces, hearts float up and a star lights.
    // An unhealthy one makes her lean back with her ears flat and a tail lash, a grey "yuck" cloud puffs and her tummy rumbles; a lit star
    // goes out and the food slides back to the table. All five stars lit ends the round; a session is two rounds. The rules are in
    // Rules/FoodTable.cs; this draws them.
    //
    // Two unhealthy foods in a row, or a long quiet: the hand carries a healthy food to Eva (the only help; the coin is then Assisted).
    //
    // Layout in "picture space", the 1920 x 900 frame of the background world/food_bg, x = px - 960, y = 450 - py: the tabletop runs from
    // y -25 (back) to -105 (front), x -930..170; the rug is on the right, x 260..940, y -180..-375.
    public sealed class FeedEvaScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class FoodView
        {
            public DragItem Drag;
            public RectTransform Art;
            public Image Image;
            public Vector2 Home;
            public bool Held;
            public bool Busy;            // being eaten / refused, cannot be picked up
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly float[] FoodXs = { -780f, -530f, -280f, -30f };
        public const float FoodY = 0f;
        private const float FoodWidth = 190f;
        private const float FoodTapSize = 240f;
        public const float EvaX = 600f, EvaFeetY = -330f, EvaHeight = 520f;
        public static readonly Vector2 EvaMouth = new Vector2(EvaX, 10f);
        public const float MouthRange = 250f;       // a food let go this close to Eva's mouth is fed to her
        private const float StarY = 235f, StarSpacing = 100f, StarSize = 90f;
        private const float ReturnSpeed = 2600f;

        private const float EatSeconds = 0.35f, RefuseSeconds = 1.3f, BackSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _foodField, _fxField;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private readonly Image[] _starsOn = new Image[FoodTable.StarsNeeded];
        private readonly Image[] _starsOff = new Image[FoodTable.StarsNeeded];

        private FoodTable _table;
        private readonly List<FoodView> _foods = new List<FoodView>();
        private int _roundIndex, _rightLineIndex, _busyCount, _healthyEaten, _shownStars;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public FeedEvaScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public FoodTable Table => _table;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public IReadOnlyList<DragItem> FoodDrags
        {
            get { var list = new List<DragItem>(); foreach (var food in _foods) list.Add(food.Drag); return list; }
        }
        public RectTransform EvaRoot => _eva.Root;

        public void PlaceFood(int slot, Vector2 point) => _foods[slot].Drag.Rect.anchoredPosition = Place(point);

        // All the layout above is in "picture space": the 1920x900 frame of the background. The picture is stretched over the whole screen
        // (full-bleed) while the screen's own space is centred on the safe area, so on a phone the two differ: Place() turns a picture-space
        // point into this screen's space (and Scene() back). Tests turn it off (identity).
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
            _evaAnchor.anchoredPosition = Place(new Vector2(EvaX, EvaFeetY));
            for (var i = 0; i < FoodTable.StarsNeeded; i++)
            {
                var point = Place(StarPoint(i));
                _starsOn[i].rectTransform.anchoredPosition = point;
                _starsOff[i].rectTransform.anchoredPosition = point;
            }
        }

        private static Vector2 StarPoint(int index) => new Vector2(EvaX + (index - (FoodTable.StarsNeeded - 1) * 0.5f) * StarSpacing, StarY);

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            AddPictureBackground();
            BuildEva();
            BuildStars();
            _foodField = Container("Foods");
            BuildFoods();
            _fxField = Container("Effects");
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
            _roundIndex = 0;
            _rightLineIndex = 0;
            if (_hand != null) _hand.Hide();
            SetSessionEnded(false);
            _runner.StopAllCoroutines();
            _runner.StartCoroutine(Loop());
            StartRound();
        }

        private void StartRound()
        {
            _table = new FoodTable(new System.Random());
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _busyCount = 0;
            _healthyEaten = 0;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _shownStars = 0;
            RefreshStars(false);
            for (var i = 0; i < _foods.Count; i++)
            {
                var food = _foods[i];
                food.Held = false;
                food.Busy = false;
                food.Drag.GetComponent<Image>().raycastTarget = true;
                food.Drag.Rect.anchoredPosition = Place(food.Home);
                ShowFood(i);
                _runner.StartCoroutine(PopIn(food.Art, 0.2f, 0.08f * i));
            }
            _runner.StartCoroutine(SayPrompt());
        }

        private void ShowFood(int slot)
        {
            var food = _foods[slot];
            food.Image.sprite = EvaUi.Sprite("sciencelab/food_" + _table.FoodAt(slot));
            food.Image.color = Color.white;
            food.Art.localScale = Vector3.one;
            food.Art.localRotation = Quaternion.identity;
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("food_prompt");
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
            _evaAnchor.anchoredPosition = new Vector2(EvaX, EvaFeetY);
            _eva = RigFactory.CreateEva(_evaAnchor, EvaHeight);
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private void BuildStars()
        {
            for (var i = 0; i < FoodTable.StarsNeeded; i++)
            {
                _starsOff[i] = StarImage("StarOff" + i, "sciencelab/foodfx_star_off");
                _starsOn[i] = StarImage("StarOn" + i, "sciencelab/foodfx_star");
            }
        }

        private Image StarImage(string name, string sprite)
        {
            var art = EvaUi.Sprite(sprite);
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(StarSize, StarSize * art.rect.height / art.rect.width);
            var image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void BuildFoods()
        {
            for (var i = 0; i < FoodTable.Slots; i++)
            {
                var food = new FoodView { Home = new Vector2(FoodXs[i], FoodY) };
                food.Drag = DragItem.Create(_foodField, "food" + i, EvaUi.Sprite("icons/dot"), Place(food.Home), FoodTapSize);
                food.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = food;
                var slot = i;
                food.Drag.BeginDrag += _ => OnBeginDrag(captured);
                food.Drag.EndDrag += _ => OnEndDrag(captured, slot);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(food.Drag.transform, false);
                food.Art = (RectTransform)go.transform;
                food.Art.anchorMin = food.Art.anchorMax = food.Art.pivot = new Vector2(0.5f, 0.5f);
                food.Art.anchoredPosition = Vector2.zero;
                food.Art.sizeDelta = new Vector2(FoodWidth, FoodWidth);
                food.Image = go.GetComponent<Image>();
                food.Image.preserveAspect = true;
                food.Image.raycastTarget = false;
                _foods.Add(food);
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
            _foodField.gameObject.SetActive(!ended);
            _fxField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The foods --------------------------------------------------------------------------------------------------

        private void OnBeginDrag(FoodView food)
        {
            if (_roundOver || food.Busy) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a food never waits for the hint hand
            food.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(FoodView food, int slot)
        {
            food.Held = false;
            _idle = 0f;
            if (_roundOver || food.Busy) return;
            if (Vector2.Distance(Scene(food.Drag.Rect.anchoredPosition), EvaMouth) < MouthRange) Feed(slot);
        }

        // The food in `slot` is fed to Eva (also what the hint does).
        private void Feed(int slot)
        {
            var food = _foods[slot];
            var id = _table.FoodAt(slot);
            var result = _table.Feed(slot);
            food.Busy = true;
            food.Drag.GetComponent<Image>().raycastTarget = false;
            _busyCount++;
            if (result == FoodResult.Healthy) _healthyEaten++;
            if (result == FoodResult.Unhealthy && _table.NeedsHint) StartHint();
            _runner.StartCoroutine(result == FoodResult.Healthy ? Eat(slot, id) : Refuse(slot, id));
        }

        private IEnumerator MoveFood(FoodView food, Vector2 to, float seconds, float endScale)
        {
            var from = food.Drag.Rect.anchoredPosition;
            var target = Place(to);
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / seconds);
                food.Drag.Rect.anchoredPosition = Vector2.Lerp(from, target, k);
                food.Art.localScale = Vector3.one * Mathf.Lerp(1f, endScale, k);
                yield return null;
            }
            food.Drag.Rect.anchoredPosition = target;
            food.Art.localScale = Vector3.one * endScale;
        }

        private IEnumerator Eat(int slot, string id)
        {
            var food = _foods[slot];
            yield return MoveFood(food, EvaMouth, EatSeconds, 0.45f);
            Haptics.Tap();
            if (_game != null && _game.Sfx != null) { _game.Sfx.Chomp(); _game.Sfx.Right(); }
            _eva.Cheer();
            food.Art.localScale = Vector3.zero;
            RefreshStars(true);
            if (_healthyEaten % 3 == 1) Say("food_say_" + id); // not every time: Eva does not talk all the time
            for (var i = 0; i < 3; i++) _runner.StartCoroutine(FloatUp("heart", EvaMouth + new Vector2((i - 1) * 90f, 150f), 1.1f, 0.12f * i));

            // The next food is already on the table's slot: it pops in at home.
            food.Drag.Rect.anchoredPosition = Place(food.Home);
            ShowFood(slot);
            yield return PopIn(food.Art, 0.25f, 0.4f);
            food.Busy = false;
            food.Drag.GetComponent<Image>().raycastTarget = true;
            _busyCount--;
        }

        private IEnumerator Refuse(int slot, string id)
        {
            var food = _foods[slot];
            yield return MoveFood(food, EvaMouth, EatSeconds, 0.8f);
            Haptics.Wrong();
            if (_game != null && _game.Sfx != null) _game.Sfx.Retry();
            _eva.Angry();
            RefreshStars(true);
            Say("food_say_" + id);
            _runner.StartCoroutine(FloatUp("yuck", EvaMouth + new Vector2(-30f, 90f), RefuseSeconds, 0f));
            _runner.StartCoroutine(Rumble(RefuseSeconds));
            yield return new WaitForSeconds(0.45f);
            yield return MoveFood(food, food.Home, BackSeconds, 1f);
            food.Busy = false;
            food.Drag.GetComponent<Image>().raycastTarget = true;
            _busyCount--;
        }

        // --- Stars and effects ------------------------------------------------------------------------------------------

        private void RefreshStars(bool animate)
        {
            var stars = _table.Stars;
            for (var i = 0; i < FoodTable.StarsNeeded; i++)
            {
                var lit = i < stars;
                _starsOn[i].gameObject.SetActive(lit);
                _starsOff[i].gameObject.SetActive(!lit);
                if (animate && lit && i >= _shownStars) _runner.StartCoroutine(PopIn(_starsOn[i].rectTransform, 0.3f, 0f, 1.5f));
            }
            _shownStars = stars;
        }

        private RectTransform Fx(string sprite, float width)
        {
            var art = EvaUi.Sprite(sprite);
            var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_fxField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, width * art.rect.height / art.rect.width);
            var image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        // A heart or a yuck cloud pops up at `from`, drifts upward and fades.
        private IEnumerator FloatUp(string kind, Vector2 from, float seconds, float delay)
        {
            var rect = Fx("sciencelab/foodfx_" + kind, kind == "heart" ? 80f : 170f);
            var image = rect.GetComponent<Image>();
            rect.localScale = Vector3.zero;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                var position = from + new Vector2(Mathf.Sin(t * 5f) * 14f, k * 130f);
                rect.anchoredPosition = Place(position);
                rect.localScale = Vector3.one * Mathf.Min(1f, k * 5f) * (kind == "yuck" ? 1f + 0.08f * Mathf.Sin(t * 14f) : 1f);
                image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((k - 0.6f) / 0.4f));
                yield return null;
            }
            Object.Destroy(rect.gameObject);
        }

        // Her tummy rumbles: three wavy lines at her belly that shake.
        private IEnumerator Rumble(float seconds)
        {
            var rect = Fx("sciencelab/foodfx_rumble", 110f);
            var image = rect.GetComponent<Image>();
            var home = new Vector2(EvaX, EvaFeetY + 170f);
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                rect.anchoredPosition = Place(home + new Vector2(Mathf.Sin(t * 45f) * 8f, 0f));
                image.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t / seconds - 0.7f) / 0.3f));
                yield return null;
            }
            Object.Destroy(rect.gameObject);
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

        // One frame of the kitchen. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_table == null || _sessionEnded) return;
            _time += seconds;
            foreach (var food in _foods)
            {
                if (food.Busy) continue;
                food.Art.localRotation = Quaternion.Euler(0f, 0f, food.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
                if (food.Held) food.Art.localScale = Vector3.one * 1.1f;
                else if (food.Art.localScale.x > 1f) food.Art.localScale = Vector3.one;
                if (!food.Held && !_helpRunning)
                {
                    var target = Place(food.Home);
                    var position = food.Drag.Rect.anchoredPosition;
                    if (position != target) food.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
                }
            }
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        // --- Voice ------------------------------------------------------------------------------------------------------

        private void Say(string key)
        {
            if (_announceQueue.Count >= 2) return; // never a pile of lines behind the child
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
            if (_roundOver || _helpRunning || _busyCount > 0) { _idle = 0f; return; }
            foreach (var food in _foods) if (food.Held) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        private void StartHint()
        {
            if (_helpRunning || _roundOver) return;
            _idle = 0f;
            _table.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint());
        }

        // The child took hold of a food while the hint hand was out: the hint stops and the food goes home.
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

        // The hand carries a healthy food to Eva's mouth.
        private IEnumerator RunHint()
        {
            // Wait until nothing is being eaten or refused, so the food the hand takes is at home.
            while (_busyCount > 0) yield return null;
            var slot = _table.HealthySlot();
            if (slot < 0 || _roundOver) yield break;
            var food = _foods[slot];

            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("food_hint");

            var from = food.Drag.Rect.anchoredPosition;
            var to = Place(EvaMouth);
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                food.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            food.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            Feed(slot);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_table.Done || _busyCount > 0 || _announcing) return;
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
            if (_roundIndex >= FoodTable.RoundsPerSession)
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

        private static IEnumerator PopIn(RectTransform target, float duration, float delay, float overshoot = 1f)
        {
            target.localScale = Vector3.one * 0.3f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var k = t / duration;
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k) * (overshoot > 1f ? 1f + (overshoot - 1f) * Mathf.Sin(k * Mathf.PI) : 1f);
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
