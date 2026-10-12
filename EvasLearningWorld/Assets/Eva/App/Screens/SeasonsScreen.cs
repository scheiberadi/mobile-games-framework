using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Seasons: "Year tree". A big tree stands in a meadow and Eva asks for a season ("Make it spring!"). Four round season
    // cards lie along the bottom; the child drags one onto the meadow and the whole picture changes into that season (blossom and falling
    // petals, green leaves with the sun's flowers and critters, orange leaves falling, a bare snowy tree with snow). Any card shows its
    // season; the round is done when it is the asked one. Rules: Rules/Seasons.cs; this draws them.
    //
    // Two wrong seasons in a row, or a long quiet: the hand carries the right card onto the meadow (the only help; the coin is then Assisted).
    //
    // Layout in "picture space" like MakeWeatherScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py).
    public sealed class SeasonsScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private enum Particles { None, Petals, Leaves, Snow }

        private sealed class Card
        {
            public Season Kind;
            public DragItem Drag;
            public RectTransform Art;
            public Vector2 Home;
            public bool Held;
            public float Wobble;
        }

        private sealed class Speck
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Pos;
            public float Speed, Phase;
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);      // her feet, left of the tree
        public const float EvaHeight = 420f;
        public static readonly Vector2 MeadowCentre = new Vector2(-100f, 40f);    // where the hint hand carries the card
        public const float MeadowBottom = -190f;                                  // a card let go above this line is on the meadow
        public const float CardTapSize = 240f;
        private const float CardArt = 200f;
        private const float TrayY = -345f;
        private const float TrayStep = 300f;
        private const float ReturnSpeed = 2600f;
        private const float SeasonFadeSeconds = 1.4f;

        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };
        private const int SpeckCount = 36;
        private const float MusicLevel = 0.3f;
        private const float AmbientNormal = 0.3f, AmbientUnderVoice = 0.1f;
        private const float SunnyFlowersFromX = -80f;  // the sun's flowers stay right of the tree

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundPrefix;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _speckField, _trayField;
        private Image _backFade, _frontFade;
        private float _fade = 1f;
        private readonly Dictionary<Season, Sprite> _backgrounds = new Dictionary<Season, Sprite>();
        private readonly List<Card> _cards = new List<Card>();
        private readonly List<Speck> _specks = new List<Speck>();
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;
        private Sprite _petalSprite, _flakeSprite;
        private Sprite[] _leafSprites;

        private SeasonRound _round;
        private Season[] _session;
        private Season _shown = Seasons.Start;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        private Particles _mode;
        private float _speckAlpha;
        private SunnyLife _sunny;
        private float _life;                    // 0..1 how much of the sunny life shows (summer only)

        public SeasonsScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundPrefix)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundPrefix = backgroundPrefix;
        }

        // Read-only state for tests.
        public SeasonRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public Season ShownSeason => _shown;
        public bool PetalsFalling => _mode == Particles.Petals;
        public bool LeavesFalling => _mode == Particles.Leaves;
        public bool SnowFalling => _mode == Particles.Snow;
        public IReadOnlyList<DragItem> CardDrags
        {
            get { var list = new List<DragItem>(); foreach (var card in _cards) list.Add(card.Drag); return list; }
        }

        // The card's middle, in picture space; PlaceCard puts it there.
        public Vector2 CardPoint(int index) => Scene(_cards[index].Drag.Rect.anchoredPosition);
        public void PlaceCard(int index, Vector2 point) => _cards[index].Drag.Rect.anchoredPosition = Place(point);

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
            _petalSprite = EvaUi.Sprite("sciencelab/seasonbit_petal");
            _flakeSprite = EvaUi.Sprite("sciencelab/weathersky_snowflake");
            _leafSprites = new[]
            {
                EvaUi.Sprite("sciencelab/seasonbit_maple_red"), EvaUi.Sprite("sciencelab/seasonbit_maple_orange"), EvaUi.Sprite("sciencelab/seasonbit_oak"),
            };
            foreach (Season season in System.Enum.GetValues(typeof(Season))) _backgrounds[season] = EvaUi.Sprite(_backgroundPrefix + Seasons.IdOf(season));
            AddPictureBackground();
            BuildEva();
            _sunny = new SunnyLife(Root, Place, SunnyFlowersFromX);
            _speckField = Container("Specks");
            BuildSpecks();
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
            _session = Seasons.CreateSession(new System.Random());
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
            if (_roundIndex == 0) ShowSeason(Seasons.Start, true);
            _round = new SeasonRound(_session[_roundIndex], _shown);
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
            foreach (var card in _cards)
            {
                card.Held = false;
                card.Wobble = 0f;
                card.Drag.Rect.anchoredPosition = Place(card.Home);
                card.Art.localScale = Vector3.one;
                card.Art.localRotation = Quaternion.identity;
            }
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("seasons_ask_" + Seasons.IdOf(_round.Asked));
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

        private void BuildSpecks()
        {
            for (var i = 0; i < SpeckCount; i++)
            {
                var go = new GameObject("Speck" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_speckField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                go.SetActive(false);
                _specks.Add(new Speck { Rect = rect, Image = image });
            }
        }

        private void BuildTray()
        {
            var count = System.Enum.GetValues(typeof(Season)).Length;
            var first = -TrayStep * (count - 1) * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var kind = (Season)i;
                var card = new Card { Kind = kind, Home = new Vector2(first + i * TrayStep, TrayY) };
                var art = EvaUi.Sprite("sciencelab/seasoncard_" + Seasons.IdOf(kind));
                card.Drag = DragItem.Create(_trayField, kind.ToString(), EvaUi.Sprite("icons/dot"), Place(card.Home), CardTapSize);
                card.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = card;
                card.Drag.BeginDrag += _ => OnBeginDrag(captured);
                card.Drag.EndDrag += _ => OnEndDrag(captured);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(card.Drag.transform, false);
                card.Art = (RectTransform)go.transform;
                card.Art.anchorMin = card.Art.anchorMax = card.Art.pivot = new Vector2(0.5f, 0.5f);
                card.Art.anchoredPosition = Vector2.zero;
                var aspect = art.rect.width / art.rect.height;
                card.Art.sizeDelta = aspect >= 1f ? new Vector2(CardArt, CardArt / aspect) : new Vector2(CardArt * aspect, CardArt);
                var image = go.GetComponent<Image>();
                image.sprite = art;
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

        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _speckField.gameObject.SetActive(!ended);
            _sunny.SetActive(!ended);
            if (ended && _game.Sfx != null) _game.Sfx.StopAmbient();
            _trayField.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // --- The season -------------------------------------------------------------------------------------------------

        // The meadow changes into this season: at once when the game starts, cross-faded afterwards.
        private void ShowSeason(Season season, bool snap)
        {
            if (snap)
            {
                _backFade.sprite = _backgrounds[season];
                _frontFade.sprite = _backgrounds[season];
                _frontFade.color = new Color(1f, 1f, 1f, 0f);
                _fade = 1f;
            }
            else if (season != _shown)
            {
                if (_fade >= 0.5f) _backFade.sprite = _backgrounds[_shown];
                _frontFade.sprite = _backgrounds[season];
                _frontFade.color = new Color(1f, 1f, 1f, 0f);
                _fade = 0f;
            }
            _shown = season;
            switch (season)
            {
                case Season.Spring: SetMode(Particles.Petals, snap); break;
                case Season.Autumn: SetMode(Particles.Leaves, snap); break;
                case Season.Winter: SetMode(Particles.Snow, snap); break;
                default: SetMode(Particles.None, snap); break;
            }
            if (_game != null && _game.Sfx != null)
            {
                // Autumn blows, winter is a quiet snowfall; spring and summer have no loop of their own.
                var kind = season == Season.Autumn ? (int)WeatherKind.Windy : season == Season.Winter ? (int)WeatherKind.Snowy : -1;
                _game.Sfx.StartAmbient(kind);
                if (kind >= 0 && !snap) _game.Sfx.Weather(kind);
            }
        }

        private void SetMode(Particles mode, bool snap)
        {
            _mode = mode;
            if (mode == Particles.None)
            {
                if (snap) { _speckAlpha = 0f; foreach (var speck in _specks) speck.Rect.gameObject.SetActive(false); }
                return;
            }
            _speckAlpha = snap ? 1f : 0f;
            var count = mode == Particles.Petals ? 18 : mode == Particles.Leaves ? 16 : 34;
            var width = mode == Particles.Snow ? 38f : mode == Particles.Leaves ? 62f : 44f;
            for (var i = 0; i < _specks.Count; i++)
            {
                var speck = _specks[i];
                speck.Rect.gameObject.SetActive(i < count);
                if (i >= count) continue;
                var sprite = mode == Particles.Snow ? _flakeSprite : mode == Particles.Petals ? _petalSprite : _leafSprites[i % _leafSprites.Length];
                speck.Image.color = new Color(1f, 1f, 1f, _speckAlpha);
                speck.Image.sprite = sprite;
                var aspect = sprite.rect.width / sprite.rect.height;
                speck.Rect.sizeDelta = new Vector2(width, width / aspect);
                speck.Pos = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-250f, 450f));
                speck.Speed = Random.Range(0.8f, 1.2f);
                speck.Phase = Random.Range(0f, 6.28f);
            }
        }

        // --- The cards --------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Card card)
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a card never waits for the hint hand
            card.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(Card card)
        {
            card.Held = false;
            _idle = 0f;
            if (_roundOver) return;
            if (Scene(card.Drag.Rect.anchoredPosition).y >= MeadowBottom) DropCard(card);
        }

        // The card was let go on the meadow (also what the hint does): the meadow becomes that season.
        private void DropCard(Card card)
        {
            var before = _shown;
            var right = _round.Drop(card.Kind);
            if (card.Kind == before) return; // the meadow already shows it
            ShowSeason(card.Kind, false);
            Haptics.Tap();
            if (right)
            {
                if (_game != null && _game.Sfx != null) _game.Sfx.Right();
                Say("seasons_say_" + Seasons.IdOf(card.Kind));
                return;
            }
            card.Wobble = 0.4f;
            _runner.StartCoroutine(AfterWrong(card.Kind));
        }

        // A season that is not the asked one: Eva says what it is, then asks again (or the hand helps).
        private IEnumerator AfterWrong(Season shown)
        {
            Say("seasons_say_" + Seasons.IdOf(shown));
            while (_announcing) yield return null;
            if (_round.Done || _round.Shown != shown) yield break; // the child already moved on
            if (_round.NeedsHint) StartHint();
            else Say("seasons_ask_" + Seasons.IdOf(_round.Asked));
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
            foreach (var card in _cards) UpdateCard(card, seconds);
            UpdateMeadow(seconds);
            UpdateSpecks(seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
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
            card.Art.localScale = Vector3.one * (card.Held ? 1.12f : 1f);
            if (!card.Held && !_helpRunning)
            {
                var target = Place(card.Home);
                var position = card.Drag.Rect.anchoredPosition;
                if (position != target) card.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
            }
        }

        private void UpdateMeadow(float seconds)
        {
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + seconds / SeasonFadeSeconds);
                _frontFade.color = new Color(1f, 1f, 1f, _fade);
            }
            _speckAlpha = Mathf.MoveTowards(_speckAlpha, _mode == Particles.None ? 0f : 1f, seconds);
            _life = Mathf.MoveTowards(_life, _shown == Season.Summer ? 1f : 0f, seconds / 1.5f);
            _sunny.Show(_life, _time);
            if (_game.Sfx != null) _game.Sfx.AmbientVolume = _game.Voice != null && _game.Voice.IsSpeaking ? AmbientUnderVoice : AmbientNormal;
            _evaAnchor.anchoredPosition = Place(EvaPoint);
        }

        private void UpdateSpecks(float seconds)
        {
            foreach (var speck in _specks)
            {
                if (!speck.Rect.gameObject.activeSelf) continue;
                if (_mode == Particles.None && _speckAlpha <= 0f) { speck.Rect.gameObject.SetActive(false); continue; }
                speck.Image.color = new Color(1f, 1f, 1f, _speckAlpha);
                var angle = 0f;
                switch (_mode)
                {
                    case Particles.Petals:
                        speck.Pos += new Vector2(Mathf.Sin(_time * 0.9f + speck.Phase) * 70f + 25f, -95f * speck.Speed) * seconds;
                        angle = Mathf.Sin(_time * 1.5f + speck.Phase) * 50f;
                        if (speck.Pos.y < -260f) speck.Pos = new Vector2(Random.Range(-1000f, 1000f), 460f);
                        break;
                    case Particles.Leaves:
                        speck.Pos += new Vector2(Mathf.Sin(_time * 0.8f + speck.Phase) * 110f + 60f, -125f * speck.Speed) * seconds;
                        angle = Mathf.Sin(_time * 1.2f + speck.Phase) * 70f;
                        if (speck.Pos.y < -260f) speck.Pos = new Vector2(Random.Range(-1000f, 1000f), 460f);
                        break;
                    case Particles.Snow:
                        speck.Pos += new Vector2(Mathf.Sin(_time * 1.3f + speck.Phase) * 30f, -150f * speck.Speed) * seconds;
                        angle = Mathf.Sin(_time + speck.Phase) * 25f;
                        if (speck.Pos.y < -260f) speck.Pos = new Vector2(Random.Range(-1000f, 1000f), 460f);
                        break;
                }
                speck.Rect.anchoredPosition = Place(speck.Pos);
                speck.Rect.localRotation = Quaternion.Euler(0f, 0f, angle);
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
            if (_roundOver || _helpRunning) { _idle = 0f; return; }
            foreach (var card in _cards) if (card.Held) { _idle = 0f; return; }
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

        // The hand carries the right card onto the meadow.
        private IEnumerator RunHint()
        {
            var card = _cards[(int)_round.Asked];
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("dressforweather_hint");

            var from = card.Drag.Rect.anchoredPosition;
            var to = Place(MeadowCentre);
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
            DropCard(card);
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
            yield return new WaitForSeconds(0.6f); // a moment to look at the tree
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
            if (_roundIndex >= Seasons.RoundsPerSession)
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
            _frontFade = BackgroundLayer("BackgroundFade");
            _backFade = BackgroundLayer("Background");
            _backFade.sprite = _backgrounds[Seasons.Start];
            _frontFade.sprite = _backgrounds[Seasons.Start];
            _frontFade.color = new Color(1f, 1f, 1f, 0f);
        }

        // Each new layer goes to the very back, so the one made last is the bottom one.
        private Image BackgroundLayer(string name)
        {
            var background = new GameObject(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }
    }
}
