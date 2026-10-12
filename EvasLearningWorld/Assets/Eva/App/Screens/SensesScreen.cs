using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Human Senses: "What do I use?". Three things sit on a table (a rainbow, a bell, a lemon...) and the five organs (eye,
    // ear, nose, tongue, hand) lie on the floor. The child drags an organ onto a thing. The right one is used: the thing is looked at (it
    // glitters), listened to (it rings and sound waves leave it), smelled (scent wisps rise), tasted (it is bitten) or felt (it wiggles);
    // then it pops away and Eva says what we did. A wrong organ is only "not that one": it shakes its head and goes back. The round ends
    // when all three things are used; a session is three rounds. Rules: Rules/Senses.cs; this draws them.
    //
    // Two wrong tries in a row, or a long quiet: the hand carries the right organ to the next thing (the only help; the coin is then
    // Assisted).
    //
    // Layout in "picture space", the 1920 x 900 frame of the background world/senses_bg, x = px - 960, y = 450 - py: the tabletop runs
    // from y -25 (back) to -110 (front), x -550..550; the things stand on it, the organs lie on the floor below.
    public sealed class SensesScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class ThingView
        {
            public RectTransform Rect;
            public Image Image;
            public float Hover;          // 0..1 an organ is over it
            public bool Busy;            // being used right now
            public Vector2 Size;
        }

        private sealed class OrganView
        {
            public Sense Sense;
            public DragItem Drag;
            public RectTransform Art;
            public Vector2 Home;
            public bool Held;
            public bool Busy;            // at a thing, being used (cannot be picked up)
            public float Wobble;         // seconds of "not that one" shaking left
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public const float ThingMax = 270f;                 // a thing's picture is at most this wide / tall
        public const float ThingFeetY = -70f;
        public static readonly float[] ThingXs = { -340f, 0f, 340f };
        public const float OrganWidth = 190f;
        private const float OrganTapSize = 240f;
        public const float OverRange = 210f;                // an organ let go this close to a thing's middle is tried on it
        public static readonly float[] OrganXs = { -690f, -445f, -200f, 45f, 290f };
        public const float OrganY = -335f;
        private const float ReturnSpeed = 2600f;
        private static readonly string[] OrganIds = { "eye", "ear", "nose", "tongue", "hand" };

        private const float UseSeconds = 1.7f, PopAwaySeconds = 0.35f, HintGapSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _thingField, _organField, _fxField;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private SensesRound _round;
        private readonly ThingView[] _things = new ThingView[Senses.ThingsPerRound];
        private readonly List<OrganView> _organs = new List<OrganView>();
        private string[][] _session;
        private int _roundIndex, _rightLineIndex, _using;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public SensesScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public SensesRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public IReadOnlyList<DragItem> OrganDrags
        {
            get { var list = new List<DragItem>(); foreach (var organ in _organs) list.Add(organ.Drag); return list; }
        }

        // The middle of a thing, in picture space.
        public Vector2 ThingPoint(int index) => new Vector2(ThingXs[index], ThingFeetY + _things[index].Size.y * 0.5f);

        // The organ's middle, in picture space; PlaceOrgan puts it there.
        public Vector2 OrganPoint(int index) => Scene(_organs[index].Drag.Rect.anchoredPosition);
        public void PlaceOrgan(int index, Vector2 point) => _organs[index].Drag.Rect.anchoredPosition = Place(point);

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
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();
            AddPictureBackground();
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
            _thingField = Container("Things");
            BuildThings();
            _fxField = Container("Effects");
            _organField = Container("Organs");
            BuildOrgans();
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
            _session = Senses.CreateSession(new System.Random());
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
            _round = new SensesRound(_session[_roundIndex]);
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _using = 0;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            for (var i = 0; i < _things.Length; i++)
            {
                var view = _things[i];
                var sprite = EvaUi.Sprite("sciencelab/sensething_" + _round.IdOf(i));
                view.Image.sprite = sprite;
                var aspect = sprite.rect.width / sprite.rect.height;
                view.Size = aspect >= 1f ? new Vector2(ThingMax, ThingMax / aspect) : new Vector2(ThingMax * aspect, ThingMax);
                view.Rect.sizeDelta = view.Size;
                view.Image.color = Color.white;
                view.Hover = 0f;
                view.Busy = false;
                view.Rect.gameObject.SetActive(true);
                view.Rect.anchoredPosition = Place(new Vector2(ThingXs[i], ThingFeetY));
                view.Rect.localRotation = Quaternion.identity;
                view.Rect.localScale = Vector3.one;
                _runner.StartCoroutine(PopIn(view.Rect, 0.2f, 0.1f * i));
            }
            foreach (var organ in _organs)
            {
                organ.Held = false;
                organ.Busy = false;
                organ.Wobble = 0f;
                organ.Drag.GetComponent<Image>().raycastTarget = true;
                organ.Drag.Rect.anchoredPosition = Place(organ.Home);
                organ.Art.localScale = Vector3.one;
                organ.Art.localRotation = Quaternion.identity;
            }
            _runner.StartCoroutine(SayPrompt());
        }

        private IEnumerator SayPrompt()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("senses_prompt");
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        private void BuildThings()
        {
            for (var i = 0; i < _things.Length; i++)
            {
                var go = new GameObject("Thing" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_thingField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f); // stands on its feet
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                _things[i] = new ThingView { Rect = rect, Image = image };
            }
        }

        private void BuildOrgans()
        {
            for (var i = 0; i < OrganIds.Length; i++)
            {
                var organ = new OrganView { Sense = (Sense)i, Home = new Vector2(OrganXs[i], OrganY) };
                var art = EvaUi.Sprite("sciencelab/organ_" + OrganIds[i]);
                organ.Drag = DragItem.Create(_organField, OrganIds[i], EvaUi.Sprite("icons/dot"), Place(organ.Home), OrganTapSize);
                organ.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = organ;
                organ.Drag.BeginDrag += _ => OnBeginDrag(captured);
                organ.Drag.EndDrag += _ => OnEndDrag(captured);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(organ.Drag.transform, false);
                organ.Art = (RectTransform)go.transform;
                organ.Art.anchorMin = organ.Art.anchorMax = organ.Art.pivot = new Vector2(0.5f, 0.5f);
                organ.Art.anchoredPosition = Vector2.zero;
                organ.Art.sizeDelta = new Vector2(OrganWidth, OrganWidth * art.rect.height / art.rect.width);
                var image = go.GetComponent<Image>();
                image.sprite = art;
                image.preserveAspect = true;
                image.raycastTarget = false;
                _organs.Add(organ);
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
            _fxField.gameObject.SetActive(!ended);
            _organField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The organs -------------------------------------------------------------------------------------------------

        private void OnBeginDrag(OrganView organ)
        {
            if (_roundOver || organ.Busy) return;
            if (_helpRunning) CancelHint(); // a child who reaches for an organ never waits for the hint hand
            organ.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(OrganView organ)
        {
            organ.Held = false;
            _idle = 0f;
            if (_roundOver || organ.Busy) return;
            var thing = ThingUnder(organ);
            if (thing >= 0) TryOrgan(organ, thing);
        }

        // The unused thing nearest the organ's middle, within OverRange, or -1.
        private int ThingUnder(OrganView organ)
        {
            var point = Scene(organ.Drag.Rect.anchoredPosition);
            var best = -1;
            var bestDistance = OverRange;
            for (var i = 0; i < _things.Length; i++)
            {
                if (_round.IsUsed(i) || _things[i].Busy) continue;
                var distance = Vector2.Distance(point, ThingPoint(i));
                if (distance < bestDistance) { best = i; bestDistance = distance; }
            }
            return best;
        }

        // The organ was let go over thing `index` (also what the hint does).
        private void TryOrgan(OrganView organ, int index)
        {
            switch (_round.Try(index, organ.Sense))
            {
                case SenseTry.Right:
                    _runner.StartCoroutine(Use(organ, index));
                    break;
                case SenseTry.Wrong:
                    organ.Wobble = 0.5f;
                    Haptics.Wrong();
                    if (_game != null && _game.Sfx != null) _game.Sfx.Retry();
                    if (_round.NeedsHint) StartHint();
                    break;
            }
        }

        // --- Using a thing ----------------------------------------------------------------------------------------------

        private IEnumerator Use(OrganView organ, int index)
        {
            var view = _things[index];
            var id = _round.IdOf(index);
            var sense = organ.Sense;
            view.Busy = true;
            organ.Busy = true;
            organ.Drag.GetComponent<Image>().raycastTarget = false;
            _using++;
            Haptics.Tap();
            if (_game != null && _game.Sfx != null) _game.Sfx.Pop();
            Say("senses_say_" + id);

            // The organ hops up beside the thing and is used there.
            var middle = ThingPoint(index);
            var spot = new Vector2(middle.x - 130f, ThingFeetY + view.Size.y + 30f);
            var from = organ.Drag.Rect.anchoredPosition;
            var to = Place(spot);
            for (var t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                organ.Drag.Rect.anchoredPosition = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / 0.25f));
                yield return null;
            }
            organ.Drag.Rect.anchoredPosition = to;

            var top = new Vector2(middle.x, ThingFeetY + view.Size.y);
            var fx = MakeEffects(sense);
            for (var t = 0f; t < UseSeconds; t += Time.deltaTime)
            {
                var k = t / UseSeconds;
                OrganGesture(organ, sense, t);
                ThingGesture(view, sense, t, k);
                EffectFrame(sense, fx, view, middle, top, t);
                yield return null;
            }
            foreach (var picture in fx) if (picture != null) Object.Destroy(picture.gameObject);

            // The thing pops away; the organ goes home.
            organ.Art.localScale = Vector3.one;
            organ.Art.localRotation = Quaternion.identity;
            organ.Busy = false;
            organ.Drag.GetComponent<Image>().raycastTarget = true;
            if (_game != null && _game.Sfx != null) _game.Sfx.Drop();
            for (var t = 0f; t < PopAwaySeconds; t += Time.deltaTime)
            {
                var k = t / PopAwaySeconds;
                view.Rect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0f, k * k);
                view.Image.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            view.Rect.gameObject.SetActive(false);
            view.Busy = false;
            _using--;
        }

        // What the organ does while it is used: the eye blinks, the ear tilts, the nose sniffs, the tongue licks, the hand pats.
        private void OrganGesture(OrganView organ, Sense sense, float t)
        {
            var scale = 1f;
            var angle = 0f;
            switch (sense)
            {
                case Sense.Sight: scale = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 5f)); break;
                case Sense.Hearing: angle = Mathf.Sin(t * 9f) * 14f; scale = 1.1f; break;
                case Sense.Smell: scale = 1f + 0.18f * Mathf.Abs(Mathf.Sin(t * 7f)); break;
                case Sense.Taste: angle = Mathf.Sin(t * 12f) * 18f; scale = 1.1f; break;
                case Sense.Touch: scale = 1.1f; angle = Mathf.Sin(t * 8f) * 8f; break;
            }
            organ.Art.localScale = Vector3.one * scale;
            organ.Art.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // What the thing does: sight glitters (grows), hearing rings (shakes), smell sways, taste is bitten, touch wiggles.
        private void ThingGesture(ThingView view, Sense sense, float t, float k)
        {
            var scale = Vector3.one;
            var angle = 0f;
            switch (sense)
            {
                case Sense.Sight: scale = Vector3.one * (1f + 0.16f * Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI)); break;
                case Sense.Hearing: angle = Mathf.Sin(t * 38f) * 9f * (1f - k); break;
                case Sense.Smell: angle = Mathf.Sin(t * 5f) * 6f; break;
                case Sense.Taste:
                {
                    var bite = Mathf.Abs(Mathf.Sin(t * 5.5f));
                    scale = new Vector3(1f - 0.12f * bite, 1f - 0.18f * bite, 1f) * Mathf.Lerp(1f, 0.85f, k);
                    break;
                }
                case Sense.Touch:
                    angle = Mathf.Sin(t * 14f) * 5f;
                    scale = new Vector3(1f + 0.06f * Mathf.Sin(t * 10f), 1f - 0.06f * Mathf.Sin(t * 10f), 1f);
                    break;
            }
            view.Rect.localScale = scale * (view.Busy ? 1.1f : 1f);
            view.Rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // --- Effects (little pictures that come and go while a thing is used) -------------------------------------------------

        private RectTransform Fx(string sprite, float width)
        {
            var art = EvaUi.Sprite(sprite);
            var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_fxField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, width * art.rect.height / art.rect.width);
            rect.localScale = Vector3.zero;
            var image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        // Three pictures for one use: scent wisps, sound waves or sparkles.
        private RectTransform[] MakeEffects(Sense sense)
        {
            var key = sense == Sense.Smell ? "scent" : sense == Sense.Hearing ? "sound" : "sparkle";
            var width = key == "sparkle" ? 70f : 120f;
            return new[] { Fx("sciencelab/senseeffect_" + key, width), Fx("sciencelab/senseeffect_" + key, width * 0.8f), Fx("sciencelab/senseeffect_" + key, width * 0.9f) };
        }

        private void EffectFrame(Sense sense, RectTransform[] set, ThingView view, Vector2 middle, Vector2 top, float t)
        {
            switch (sense)
            {
                case Sense.Smell: // three wisps rise from the top of the thing, one after another, and fade
                    for (var i = 0; i < 3; i++)
                    {
                        var running = t * 0.7f - i * 0.33f;
                        var local = Mathf.Repeat(running, 1f);
                        var position = top + new Vector2((i - 1) * 55f + Mathf.Sin(t * 4f + i) * 12f, 20f + local * 190f);
                        PlaceFx(set[i], position, running >= 0f ? 1f : 0f, Mathf.Sin(local * Mathf.PI), 0f);
                    }
                    break;
                case Sense.Hearing: // waves leave the thing to the right, getting bigger, three times
                    for (var i = 0; i < 3; i++)
                    {
                        var running = t * 1.4f - i * 0.33f;
                        var local = Mathf.Repeat(running, 1f);
                        PlaceFx(set[i], middle + new Vector2(view.Size.x * 0.5f + 40f + local * 90f, 40f), running >= 0f ? 0.6f + local * 0.7f : 0f, 1f - local, 0f);
                    }
                    break;
                default: // sparkles pop around the thing
                    var offsets = new[] { new Vector2(-110f, 90f), new Vector2(120f, 60f), new Vector2(10f, 150f) };
                    for (var i = 0; i < 3; i++)
                    {
                        var local = Mathf.Clamp01((t - 0.2f - i * 0.25f) / 0.6f);
                        PlaceFx(set[i], middle + offsets[i], local > 0f ? Mathf.Sin(local * Mathf.PI) * 1.2f : 0f, 1f, local * 90f);
                    }
                    break;
            }
        }

        private void PlaceFx(RectTransform rect, Vector2 point, float scale, float alpha, float angle)
        {
            if (rect == null) return;
            rect.anchoredPosition = Place(point);
            rect.localScale = Vector3.one * scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            var image = rect.GetComponent<Image>();
            if (image != null) image.color = new Color(1f, 1f, 1f, alpha);
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
            if (_round == null || _sessionEnded) return;
            _time += seconds;
            // A thing an organ is held over lifts a little, so the child sees where it will land.
            for (var i = 0; i < _things.Length; i++)
            {
                var view = _things[i];
                if (view.Busy || _round.IsUsed(i)) continue;
                var over = false;
                foreach (var organ in _organs) if (organ.Held && ThingUnder(organ) == i) over = true;
                if (!over && view.Hover <= 0f) continue;
                view.Hover = Mathf.MoveTowards(view.Hover, over ? 1f : 0f, seconds * 8f);
                view.Rect.localScale = Vector3.one * (1f + 0.12f * view.Hover);
            }
            foreach (var organ in _organs) UpdateOrgan(organ, seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdateOrgan(OrganView organ, float seconds)
        {
            if (organ.Busy) return;
            if (organ.Wobble > 0f)
            {
                organ.Wobble = Mathf.Max(0f, organ.Wobble - seconds);
                organ.Art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(organ.Wobble * 50f) * 14f * organ.Wobble * 2f);
            }
            else
            {
                organ.Art.localRotation = Quaternion.Euler(0f, 0f, organ.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
            }
            organ.Art.localScale = Vector3.one * (organ.Held ? 1.12f : 1f);
            if (!organ.Held && !_helpRunning)
            {
                var target = Place(organ.Home);
                var position = organ.Drag.Rect.anchoredPosition;
                if (position != target) organ.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
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
            if (_roundOver || _helpRunning || _using > 0) { _idle = 0f; return; }
            foreach (var organ in _organs) if (organ.Held) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        private void StartHint()
        {
            if (_helpRunning || _roundOver) return;
            _idle = 0f;
            _round.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint());
        }

        // The child took hold of an organ while the hint hand was out: the hint stops and the organ goes home.
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

        // The hand carries the right organ to the first thing not used yet.
        private IEnumerator RunHint()
        {
            var target = -1;
            for (var i = 0; i < _things.Length && target < 0; i++) if (!_round.IsUsed(i) && !_things[i].Busy) target = i;
            if (target < 0) yield break;
            var organ = _organs[(int)_round.SenseOf(target)];
            if (organ.Busy) yield break;

            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("senses_hint");

            var from = organ.Drag.Rect.anchoredPosition;
            var to = Place(ThingPoint(target));
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                organ.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            organ.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            TryOrgan(organ, target);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_round.Done || _using > 0 || _announcing) return;
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
            if (_roundIndex >= Senses.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _things[1].Rect.position);
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
