using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Space: "Build the solar system". The sun glows at the left edge with eight orbit rings curving away from it. Four
    // planets lie along the bottom (the four inner ones first, then the four outer ones) and Eva (an astronaut) asks where one goes ("Where does Earth
    // go?"), and the planet she asks for glows in the tray. The child drags a planet onto its ring: the right ring keeps it (it then travels slowly up and down along its ring, nearer
    // planets faster), a wrong ring sends it back and Eva says which planet from the sun it really is. The child always places all eight, in
    // two pages: the inner four stay on their rings while the outer four are asked. Rules: Rules/SolarSystem.cs.
    //
    // Two wrong rings in a row, or a long quiet: the hand carries the asked planet to its ring (the only help; the coin is then Assisted).
    //
    // Layout in "picture space" like MakeWeatherScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py). The slots are
    // where each ring crosses the sun's horizontal line in world/space_bg, measured on the picture.
    public sealed class SpaceScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Card
        {
            public Planet Planet;
            public DragItem Drag;
            public RectTransform Art;
            public Vector2 Home;
            public bool Held;
            public float Wobble;
            public Image Glow;                  // the soft light behind the planet Eva asks for
        }

        // A planet on its ring: it travels up and down along it at its own pace once it has settled into it.
        private sealed class Orbiter
        {
            public Planet Planet;
            public RectTransform Rect;
            public Vector2 From;
            public float Phase, Blend, StartWidth;
        }

        // Eva as an astronaut: one picture, and an open mouth laid over it while she talks.
        private sealed class Astronaut
        {
            public RectTransform Root;
            public Image Mouth;
            public bool Talking;
            public float Hop;
            public void SetTalking(bool talking) { Talking = talking; if (!talking) Mouth.enabled = false; }
            public void Cheer() { Hop = 1f; }
            public void Tick(float time, float seconds)
            {
                Mouth.enabled = Talking && Mathf.Sin(time * 15f) > -0.3f;
                Hop = Mathf.Max(0f, Hop - seconds * 1.6f);
                Root.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(Hop * Mathf.PI * 3f)) * 60f * Hop);
            }
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(840f, -435f);       // her feet, bottom right
        public const float EvaHeight = 330f;
        private const float EvaAspect = 790f / 949f;
        // Where each ring crosses the sun's horizontal line (ring 0 is nearest the sun); the rings are arcs, bending towards the sun.
        public static readonly float[] RingX = { -656f, -533f, -371f, -200f, -5f, 218f, 449f, 692f };
        public const float RingY = -67f;
        private const float RingBend = 0.00125f;         // an arc is RingX - RingBend * dy^2
        private const float RingReach = 62f;             // a planet let go within this of an arc is on that ring
        private const float RingSpan = 420f;             // ... and this close to the sun's line, up or down
        private const float OrbitSpan = 200f;            // a placed planet travels this far up and down its ring
        public const float CardTapSize = 240f;
        private const float TrayY = -345f;
        private const float TrayStep = 300f;
        private const float ReturnSpeed = 2600f;
        private const float SettleSeconds = 0.6f;

        // How wide each planet is drawn on its ring, and in the tray (small ones larger, big ones capped, so every card is easy to grab).
        private static readonly float[] RingWidth = { 70f, 98f, 102f, 82f, 170f, 230f, 140f, 140f };
        private const float TrayMin = 112f, TrayMax = 190f;

        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };
        private const float MusicLevel = 0.3f;

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private Astronaut _eva;
        private RectTransform _evaAnchor, _trayField, _orbitField;
        private Image _ringGlow;
        private static Sprite _glowSprite;
        private readonly List<Card> _cards = new List<Card>();
        private readonly List<Orbiter> _orbiters = new List<Orbiter>();
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;

        private SolarRound _round;
        private SolarRound[] _session;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        public SpaceScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public SolarRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public int PlacedPlanets => _orbiters.Count;
        public bool AskedGlows => _round != null && _round.Asked != null && _cards.Exists(c => c.Planet == _round.Asked.Value && c.Glow.gameObject.activeSelf);
        public IReadOnlyList<DragItem> CardDrags
        {
            get { var list = new List<DragItem>(); foreach (var card in _cards) list.Add(card.Drag); return list; }
        }
        public Planet CardPlanet(int index) => _cards[index].Planet;
        public int CardOf(Planet planet) => _cards.FindIndex(c => c.Planet == planet);

        // The card's middle, in picture space; PlaceCard puts it there.
        public Vector2 CardPoint(int index) => Scene(_cards[index].Drag.Rect.anchoredPosition);
        public void PlaceCard(int index, Vector2 point) => _cards[index].Drag.Rect.anchoredPosition = Place(point);

        // The point on a ring (0 = nearest the sun) at this height, in picture space.
        public static Vector2 RingPoint(int ring, float y) => new Vector2(RingX[ring] - RingBend * (y - RingY) * (y - RingY), y);

        // The ring a point is on, or -1.
        public static int RingAt(Vector2 point)
        {
            if (Mathf.Abs(point.y - RingY) > RingSpan) return -1;
            var best = -1;
            var bestDistance = RingReach;
            for (var ring = 0; ring < RingX.Length; ring++)
            {
                var distance = Mathf.Abs(point.x - RingPoint(ring, point.y).x);
                if (distance <= bestDistance) { best = ring; bestDistance = distance; }
            }
            return best;
        }

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
            _orbitField = Container("Orbits");
            BuildRingGlow();
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
            _session = SolarSystem.CreateSession(new System.Random());
            _roundIndex = 0;
            _rightLineIndex = 0;
            if (_hand != null) _hand.Hide();
            foreach (var orbiter in _orbiters) Object.Destroy(orbiter.Rect.gameObject);
            _orbiters.Clear();
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
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            // The four planets of the round in a random order along the tray.
            var order = _round.Planets.OrderBy(_ => Random.value).ToArray();
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                card.Planet = order[i];
                var art = EvaUi.Sprite("sciencelab/planet_" + SolarSystem.IdOf(card.Planet));
                var image = card.Art.GetComponent<Image>();
                image.sprite = art;
                var width = Mathf.Clamp(RingWidth[SolarSystem.RingOf(card.Planet)] * 1.4f, TrayMin, TrayMax);
                card.Art.sizeDelta = new Vector2(width, width * art.rect.height / art.rect.width);
                card.Drag.gameObject.SetActive(true);
                card.Art.anchoredPosition = Vector2.zero;
                card.Held = false;
                card.Wobble = 0f;
                card.Drag.Rect.anchoredPosition = Place(card.Home);
                card.Art.localScale = Vector3.one;
                card.Art.localRotation = Quaternion.identity;
            }
            _ringGlow.gameObject.SetActive(false);
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            var asked = _round.Asked;
            if (asked == null) yield break;
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("sp_ask_" + SolarSystem.IdOf(asked.Value));
            _eva.SetTalking(false);
        }

        // --- Building ---------------------------------------------------------------------------------------------------

        // Eva in her space suit (the picture the Arcade space shooter also uses) with an open mouth over it for talking.
        private void BuildEva()
        {
            var go = new GameObject("EvaAnchor", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _evaAnchor = (RectTransform)go.transform;
            _evaAnchor.anchorMin = _evaAnchor.anchorMax = _evaAnchor.pivot = new Vector2(0.5f, 0.5f);
            _evaAnchor.sizeDelta = Vector2.zero;
            var size = new Vector2(EvaHeight * EvaAspect, EvaHeight);
            var body = new GameObject("Astronaut", typeof(RectTransform), typeof(Image));
            body.transform.SetParent(_evaAnchor, false);
            var rect = (RectTransform)body.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            var image = body.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("arcade/astronaut_eva");
            image.preserveAspect = true;
            image.raycastTarget = false;
            var mouth = new GameObject("Mouth", typeof(RectTransform), typeof(Image));
            mouth.transform.SetParent(body.transform, false);
            var mouthRect = (RectTransform)mouth.transform;
            mouthRect.anchorMin = Vector2.zero;
            mouthRect.anchorMax = Vector2.one;
            mouthRect.offsetMin = mouthRect.offsetMax = Vector2.zero;
            var mouthImage = mouth.GetComponent<Image>();
            mouthImage.sprite = EvaUi.Sprite("sciencelab/astronaut_eva_mouth");
            mouthImage.raycastTarget = false;
            mouthImage.enabled = false;
            _eva = new Astronaut { Root = rect, Mouth = mouthImage };
        }

        // A soft round light (made in code: a white disc fading to nothing at its edge).
        private static Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.15f - d) * Mathf.Clamp01(1.6f - d * 1.2f)));
                }
            texture.Apply();
            _glowSprite = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _glowSprite;
        }

        private void BuildRingGlow()
        {
            var go = new GameObject("RingGlow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_orbitField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(190f, 190f);
            _ringGlow = go.GetComponent<Image>();
            _ringGlow.sprite = GlowSprite();
            _ringGlow.color = new Color(1f, 0.95f, 0.5f, 0f);
            _ringGlow.raycastTarget = false;
            go.SetActive(false);
        }

        private void BuildTray()
        {
            var first = -TrayStep * (SolarSystem.PlanetsPerRound - 1) * 0.5f;
            for (var i = 0; i < SolarSystem.PlanetsPerRound; i++)
            {
                var card = new Card { Home = new Vector2(first + i * TrayStep, TrayY) };
                card.Drag = DragItem.Create(_trayField, "Planet" + i, EvaUi.Sprite("icons/dot"), Place(card.Home), CardTapSize);
                card.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = card;
                card.Drag.BeginDrag += _ => OnBeginDrag(captured);
                card.Drag.EndDrag += _ => OnEndDrag(captured);
                var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                glow.transform.SetParent(card.Drag.transform, false);
                var glowRect = (RectTransform)glow.transform;
                glowRect.anchorMin = glowRect.anchorMax = glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.sizeDelta = new Vector2(TrayMax * 1.9f, TrayMax * 1.9f);
                card.Glow = glow.GetComponent<Image>();
                card.Glow.sprite = GlowSprite();
                card.Glow.color = new Color(1f, 0.95f, 0.5f, 0f);
                card.Glow.raycastTarget = false;
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(card.Drag.transform, false);
                card.Art = (RectTransform)go.transform;
                card.Art.anchorMin = card.Art.anchorMax = card.Art.pivot = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                _cards.Add(card);
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

        // At the end of the session the planets keep travelling and the two buttons show.
        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _trayField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The cards --------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Card card)
        {
            if (_roundOver || !card.Drag.gameObject.activeSelf) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a card never waits for the hint hand
            card.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(Card card)
        {
            card.Held = false;
            _idle = 0f;
            if (_roundOver || !card.Drag.gameObject.activeSelf) return;
            var ring = RingAt(Scene(card.Drag.Rect.anchoredPosition));
            if (ring >= 0) DropCard(card, ring);
        }

        // The planet was let go on this ring (also what the hint does).
        private void DropCard(Card card, int ring)
        {
            var result = _round.Drop(card.Planet, ring);
            Haptics.Tap();
            if (result == SolarDrop.Right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                SettleOnRing(card);
                Say("sp_say_" + SolarSystem.IdOf(card.Planet));
                Say("sp_fact_" + SolarSystem.IdOf(card.Planet)); // a little something about the planet
                return;
            }
            if (result == SolarDrop.Wrong)
            {
                card.Wobble = 0.4f;
                _runner.StartCoroutine(AfterWrong(card.Planet));
            }
        }

        // The planet stays and starts travelling along its ring, from where it was let go. It becomes a planet of its own on the orbit field (the
        // tray is refilled for the next page and goes away at the end); the card goes.
        private void SettleOnRing(Card card)
        {
            var go = new GameObject("Orbit" + card.Planet, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_orbitField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = card.Art.sizeDelta;
            rect.localScale = card.Art.localScale;
            var image = go.GetComponent<Image>();
            image.sprite = card.Art.GetComponent<Image>().sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var orbiter = new Orbiter { Planet = card.Planet, Rect = rect, From = Scene(card.Drag.Rect.anchoredPosition), StartWidth = card.Art.sizeDelta.x };
            var ring = SolarSystem.RingOf(card.Planet);
            orbiter.Phase = Mathf.Asin(Mathf.Clamp((orbiter.From.y - RingY) / OrbitSpan, -1f, 1f)) - _time * OrbitSpeed(ring);
            rect.anchoredPosition = card.Drag.Rect.anchoredPosition;
            _orbiters.Add(orbiter);
            card.Drag.gameObject.SetActive(false); // nothing left to grab in the tray
        }

        private static float OrbitSpeed(int ring) => 0.95f - 0.085f * ring;

        // A planet on the wrong ring: Eva says where it really goes, then asks again (or the hand helps).
        private IEnumerator AfterWrong(Planet planet)
        {
            Say("sp_no_" + SolarSystem.IdOf(planet));
            while (_announcing) yield return null;
            if (_round.Done) yield break;
            if (_round.NeedsHint) StartHint();
            else if (_round.Asked != null) Say("sp_ask_" + SolarSystem.IdOf(_round.Asked.Value));
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
            if (_round == null) return;
            _time += seconds;
            foreach (var card in _cards) UpdateCard(card, seconds);
            foreach (var orbiter in _orbiters) UpdateOrbiter(orbiter, seconds);
            UpdateGlow();
            _eva.Tick(_time, seconds);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            if (_sessionEnded) return;
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdateOrbiter(Orbiter orbiter, float seconds)
        {
            var ring = SolarSystem.RingOf(orbiter.Planet);
            var y = RingY + OrbitSpan * Mathf.Sin(_time * OrbitSpeed(ring) + orbiter.Phase);
            var target = RingPoint(ring, y);
            orbiter.Blend = Mathf.Min(1f, orbiter.Blend + seconds / SettleSeconds);
            var k = orbiter.Blend * orbiter.Blend * (3f - 2f * orbiter.Blend);
            orbiter.Rect.anchoredPosition = Place(Vector2.Lerp(orbiter.From, target, k));
            var width = Mathf.Lerp(orbiter.StartWidth, RingWidth[ring], k);
            orbiter.Rect.localScale = Vector3.one * (width / orbiter.StartWidth);
        }

        // The planet Eva asks for glows and swells a little in the tray; after a wrong ring its ring lights up too.
        private void UpdateGlow()
        {
            var asked = _round.Asked;
            var pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
            foreach (var card in _cards)
            {
                var on = asked.HasValue && card.Planet == asked.Value && card.Drag.gameObject.activeSelf && !_roundOver;
                card.Glow.gameObject.SetActive(on);
                if (!on) continue;
                card.Glow.color = new Color(1f, 0.95f, 0.5f, 0.55f + 0.35f * pulse);
                card.Glow.rectTransform.localScale = Vector3.one * (0.9f + 0.18f * pulse);
            }
            var showRing = asked.HasValue && _round.Wrongs > 0 && !_roundOver;
            _ringGlow.gameObject.SetActive(showRing);
            if (showRing)
            {
                _ringGlow.rectTransform.anchoredPosition = Place(RingPoint(SolarSystem.RingOf(asked.Value), RingY));
                _ringGlow.color = new Color(1f, 0.95f, 0.5f, 0.35f + 0.3f * pulse);
                _ringGlow.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse);
            }
        }

        private void UpdateCard(Card card, float seconds)
        {
            if (card.Wobble > 0f)
            {
                card.Wobble = Mathf.Max(0f, card.Wobble - seconds);
                card.Art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(card.Wobble * 50f) * 14f * card.Wobble * 2f);
            }
            else
            {
                card.Art.localRotation = Quaternion.Euler(0f, 0f, card.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
            }
            var asked = _round.Asked.HasValue && card.Planet == _round.Asked.Value;
            card.Art.localScale = Vector3.one * (card.Held ? 1.12f : asked ? 1.1f : 1f);
            if (!card.Held && !_helpRunning)
            {
                var target = Place(card.Home);
                var position = card.Drag.Rect.anchoredPosition;
                if (position != target) card.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
            }
        }

        // --- Voice ------------------------------------------------------------------------------------------------------

        private void Say(string key)
        {
            if (_announceQueue.Count >= 4) return; // never a pile of lines behind the child
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
            foreach (var card in _cards) if (card.Held) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle >= IdleHintSeconds) StartHint();
        }

        private void StartHint()
        {
            if (_helpRunning || _roundOver || _round.Asked == null) return;
            _idle = 0f;
            _round.HintGiven();
            _hintRoutine = _runner.StartCoroutine(RunHint());
        }

        // The child took hold of a card while the hint hand was out: the hint stops and the card goes home.
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

        // The hand carries the asked planet to its ring.
        private IEnumerator RunHint()
        {
            var card = _cards[CardOf(_round.Asked.Value)];
            var ring = SolarSystem.RingOf(card.Planet);
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("dressforweather_hint");

            var from = card.Drag.Rect.anchoredPosition;
            var to = Place(RingPoint(ring, RingY));
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            for (var t = 0f; t < HandCarrySeconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / HandCarrySeconds));
                card.Drag.Rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            card.Drag.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            DropCard(card, ring);
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
            yield return new WaitForSeconds(0.6f); // a moment to look at the planets travelling
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
            yield return new WaitForSeconds(1.5f); // the whole family travels for a moment

            _roundIndex++;
            if (_roundIndex >= SolarSystem.RoundsPerSession)
            {
                yield return _game.Voice.SayAndWait("count_done");
                _eva.Cheer();
                SetSessionEnded(true);
                yield return _game.Voice.SayAndWait("count_again");
            }
            else
            {
                StartRound();
            }
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
            image.sprite = EvaUi.Sprite(_backgroundSprite);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
