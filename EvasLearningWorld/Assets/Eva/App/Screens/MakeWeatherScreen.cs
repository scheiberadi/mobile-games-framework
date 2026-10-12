using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Weather: "Make the weather". A village meadow; Eva asks for a weather ("Make it rainy!") and a tray of six sky pieces
    // lies along the bottom: the sun, a white cloud, a rain cloud, a storm cloud, a snowflake and the wind. The child drags one into the
    // sky and the WHOLE village changes into that weather (a picture per weather, cross-faded: world/weather_bg for sunny and
    // weather_bg_cloudy|rainy|stormy|snowy|windy), with rain, snow or blown leaves in front, lightning in a storm and a real sound for
    // each weather. Any piece makes some weather (never an error); the round is done when it is the weather Eva asked for.
    // Rules: Rules/MakeWeather.cs; this draws them.
    //
    // Two pieces in a row that make the wrong weather, or a long quiet: the hand carries the right piece into the sky (the only help; the
    // coin is then Assisted).
    //
    // Layout in "picture space", the 1920 x 900 frame of the background world/weather_bg, x = px - 960, y = 450 - py: the horizon is at
    // about y +70, the pond lies at x 410..830, y -160..-45, the tray runs along y -345.
    public sealed class MakeWeatherScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private enum Particles { None, Rain, Storm, Snow, Leaves }

        private sealed class Piece
        {
            public SkyPiece Kind;
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
        public static readonly Vector2 SkyPoint = new Vector2(0f, 300f);
        public const float SkyMinY = -190f;                  // a piece let go higher than this is in the sky
        public const float PieceTapSize = 240f;
        private const float PieceArt = 180f;
        private const float TrayY = -345f;
        private const float TrayStep = 240f;
        private const float SkyPieceMax = 340f;
        private const float ReturnSpeed = 2600f;
        private const float EvaX = -640f, EvaFeetY = -205f, EvaHeight = 330f;


        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f, HandCarrySeconds = 1.2f, HintGapSeconds = 0.4f;
        private const float IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };
        private const int SpeckCount = 50;

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _skyField, _speckField, _trayField;
        private Image _backFade, _frontFade;
        private float _fade = 1f;
        private readonly Dictionary<WeatherKind, Sprite> _backgrounds = new Dictionary<WeatherKind, Sprite>();
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly List<Speck> _specks = new List<Speck>();
        private RectTransform _skyPiece;
        private Image _skyPieceImage, _flash;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;
        private Sprite _dropSprite, _flakeSprite, _leafSprite;

        private MakeWeatherRound _round;
        private WeatherKind[] _session;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        // What the village shows now and where it is heading (all eased in Tick).
        private Particles _mode;
        private WeatherKind _shown = WeatherKind.Sunny;
        private float _flashNow, _nextFlash;
        private bool _sunLife;                  // the sun was made: rays, flowers, critters and sparkles (SunnyLife)
        private float _life;                    // 0..1 how much of that shows
        private float _speckAlpha, _pieceAlpha, _pieceTarget;
        private Particles _fadingMode;          // what still falls while it fades out
        private SunnyLife _sunny;

        private float _skyPop;

        public MakeWeatherScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
        }

        // Read-only state for tests.
        public MakeWeatherRound Round => _round;
        public int RoundIndex => _roundIndex;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public WeatherKind ShownWeather => _shown;
        public bool RainFalling => _mode == Particles.Rain || _mode == Particles.Storm;
        public bool SnowFalling => _mode == Particles.Snow;
        public IReadOnlyList<DragItem> PieceDrags
        {
            get { var list = new List<DragItem>(); foreach (var piece in _pieces) list.Add(piece.Drag); return list; }
        }

        // The piece's middle, in picture space; PlacePiece puts it there.
        public Vector2 PiecePoint(int index) => Scene(_pieces[index].Drag.Rect.anchoredPosition);
        public void PlacePiece(int index, Vector2 point) => _pieces[index].Drag.Rect.anchoredPosition = Place(point);

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
            _dropSprite = EvaUi.Sprite("sciencelab/drop");
            _flakeSprite = EvaUi.Sprite("sciencelab/weathersky_snowflake");
            _leafSprite = EvaUi.Sprite("sciencelab/weathersky_leaf");
            LoadBackgrounds();
            AddPictureBackground();
            BuildEva();
            _sunny = new SunnyLife(Root, Place);
            _skyField = Container("Sky");
            BuildSkyPiece();
            _speckField = Container("Specks");
            BuildSpecks();
            _flash = FullImage("Flash", Color.white);
            _trayField = Container("Tray");
            BuildTray();
            _hand = new PointerHand(Root, _runner);
            BuildConfetti();
            BuildEndButtons();
        }

        public override void OnShow()
        {
            if (_game.Music != null) _game.Music.Level = 0.2f; // Eva and the weather sound must be heard
            _game.TutorialGuide.Refresh(_selfId);
            UpdatePlacement();
            StartNewSession();
        }

        private void StartNewSession()
        {
            _session = MakeWeather.CreateSession(new System.Random());
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
            _round = new MakeWeatherRound(_session[_roundIndex]);
            _roundOver = false;
            _helpRunning = false;
            _hinted = false;
            _announcing = false;
            _announceQueue.Clear();
            _idle = 0f;
            _confetti.SetActive(false);
            ResetVillage(_roundIndex == 0);
            _evaAnchor.anchoredPosition = Place(new Vector2(EvaX, EvaFeetY));
            foreach (var piece in _pieces)
            {
                piece.Held = false;
                piece.Wobble = 0f;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.localScale = Vector3.one;
                piece.Art.localRotation = Quaternion.identity;
            }
            _runner.StartCoroutine(SayAsk());
        }

        private IEnumerator SayAsk()
        {
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("weather_ask_" + MakeWeather.IdOf(_round.Asked));
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

        private void BuildSkyPiece()
        {
            var go = new GameObject("SkyPiece", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_skyField, false);
            _skyPiece = (RectTransform)go.transform;
            _skyPiece.anchorMin = _skyPiece.anchorMax = _skyPiece.pivot = new Vector2(0.5f, 0.5f);
            _skyPieceImage = go.GetComponent<Image>();
            _skyPieceImage.preserveAspect = true;
            _skyPieceImage.raycastTarget = false;
            go.SetActive(false);
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

        private Image FullImage(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = go.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0f);
            image.raycastTarget = false;
            return image;
        }

        private static string SpriteOf(SkyPiece kind)
        {
            switch (kind)
            {
                case SkyPiece.Sun: return "world/sun";
                case SkyPiece.Cloud: return "sciencelab/weathersky_cloud";
                case SkyPiece.RainCloud: return "sciencelab/weathersky_rain";
                case SkyPiece.StormCloud: return "sciencelab/weathersky_storm";
                case SkyPiece.Snowflake: return "sciencelab/weathersky_snowflake";
                default: return "sciencelab/weathersky_wind";
            }
        }

        private void BuildTray()
        {
            var first = -TrayStep * (MakeWeatherCount - 1) * 0.5f;
            for (var i = 0; i < MakeWeatherCount; i++)
            {
                var kind = (SkyPiece)i;
                var piece = new Piece { Kind = kind, Home = new Vector2(first + i * TrayStep, TrayY) };
                var art = EvaUi.Sprite(SpriteOf(kind));
                piece.Drag = DragItem.Create(_trayField, kind.ToString(), EvaUi.Sprite("icons/dot"), Place(piece.Home), PieceTapSize);
                piece.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // the tap area is invisible; the picture is its child
                var captured = piece;
                piece.Drag.BeginDrag += _ => OnBeginDrag(captured);
                piece.Drag.EndDrag += _ => OnEndDrag(captured);
                var go = new GameObject("Art", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(piece.Drag.transform, false);
                piece.Art = (RectTransform)go.transform;
                piece.Art.anchorMin = piece.Art.anchorMax = piece.Art.pivot = new Vector2(0.5f, 0.5f);
                piece.Art.anchoredPosition = Vector2.zero;
                var aspect = art.rect.width / art.rect.height;
                piece.Art.sizeDelta = aspect >= 1f ? new Vector2(PieceArt, PieceArt / aspect) : new Vector2(PieceArt * aspect, PieceArt);
                var image = go.GetComponent<Image>();
                image.sprite = art;
                image.preserveAspect = true;
                image.raycastTarget = false;
                _pieces.Add(piece);
            }
        }

        private const int MakeWeatherCount = 6;

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
            _skyField.gameObject.SetActive(!ended);
            _sunny.SetActive(!ended);
            _speckField.gameObject.SetActive(!ended);
            _trayField.gameObject.SetActive(!ended);
            _flash.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // Puts every picture of the village where it stands (picture space -> this screen's space).
        // A plain day: tulips shut, no puddles, no snow, nothing in the sky.
        // A plain sunny day with nothing in the sky.
        // Back to a plain sunny day with nothing in the sky: at once when the game starts, gently (the picture fades, the rain and the piece
        // fade out) between rounds.
        private void ResetVillage(bool snap)
        {
            _flashNow = 0f;
            _nextFlash = 1.5f;
            _sunLife = false;
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _pieceTarget = 0f;
            if (snap)
            {
                _mode = Particles.None;
                _speckAlpha = 0f;
                foreach (var speck in _specks) speck.Rect.gameObject.SetActive(false);
                _backFade.sprite = _backgrounds[WeatherKind.Sunny];
                _frontFade.sprite = _backgrounds[WeatherKind.Sunny];
                _frontFade.color = new Color(1f, 1f, 1f, 0f);
                _fade = 1f;
                _shown = WeatherKind.Sunny;
                _life = 0f;
                _pieceAlpha = 0f;
                _skyPiece.gameObject.SetActive(false);
                _sunny.Show(_life, _time);
                return;
            }
            ChangeBackground(WeatherKind.Sunny);
            SetMode(Particles.None);
        }

        // The picture of `made` fades in over the one showing now (a change in the middle of a fade keeps the older picture underneath).
        private void ChangeBackground(WeatherKind made)
        {
            if (made == _shown) return;
            if (_fade >= 0.5f) _backFade.sprite = _backgrounds[_shown];
            _frontFade.sprite = _backgrounds[made];
            _frontFade.color = new Color(1f, 1f, 1f, 0f);
            _fade = 0f;
            _shown = made;
        }

        // The picture of each weather: world/weather_bg (sunny) and world/weather_bg_<id>; a weather whose picture is not there yet shows
        // the sunny one.
        private void LoadBackgrounds()
        {
            var sunny = EvaUi.Sprite(_backgroundSprite);
            foreach (WeatherKind kind in System.Enum.GetValues(typeof(WeatherKind)))
            {
                var found = kind == WeatherKind.Sunny ? null : Resources.Load<Sprite>("Art/" + _backgroundSprite + "_" + MakeWeather.IdOf(kind));
                _backgrounds[kind] = found != null ? found : sunny;
            }
        }

        // --- The pieces -------------------------------------------------------------------------------------------------

        private void OnBeginDrag(Piece piece)
        {
            if (_roundOver) return;
            if (_helpRunning) CancelHint(); // a child who reaches for a piece never waits for the hint hand
            piece.Held = true;
            _idle = 0f;
        }

        private void OnEndDrag(Piece piece)
        {
            piece.Held = false;
            _idle = 0f;
            if (_roundOver) return;
            if (Scene(piece.Drag.Rect.anchoredPosition).y > SkyMinY) DropPiece(piece);
        }

        // The piece was let go in the sky (also what the hint does): the village shows its weather.
        private void DropPiece(Piece piece)
        {
            var result = _round.Drop(piece.Kind);
            var made = MakeWeather.Made(piece.Kind);
            ShowWeather(piece.Kind, made);
            Haptics.Tap();
            if (_game != null && _game.Sfx != null)
            {
                _game.Sfx.Weather((int)made);
                if (result == WeatherDrop.Right) _game.Sfx.Right();
            }
            Say("weather_say_" + MakeWeather.IdOf(made));
            if (result == WeatherDrop.Wrong)
            {
                piece.Wobble = 0.4f;
                Say("weather_ask_" + MakeWeather.IdOf(_round.Asked));
                if (_round.NeedsHint) StartHint();
            }
        }

        // The piece appears in the sky and the village starts to change.
        // The piece appears in the sky and the village changes into its weather.
        // The piece appears in the sky and the village changes into its weather.
        private void ShowWeather(SkyPiece kind, WeatherKind made)
        {
            var sprite = EvaUi.Sprite(SpriteOf(kind));
            _skyPieceImage.sprite = sprite;
            var aspect = sprite.rect.width / sprite.rect.height;
            _skyPiece.sizeDelta = aspect >= 1f ? new Vector2(SkyPieceMax, SkyPieceMax / aspect) : new Vector2(SkyPieceMax * aspect, SkyPieceMax);
            _skyPiece.gameObject.SetActive(true);
            _pieceTarget = 1f;
            _pieceAlpha = 1f;
            _skyPieceImage.color = Color.white;
            _skyPop = 0.4f;
            _sunLife = made == WeatherKind.Sunny;

            ChangeBackground(made);
            switch (made)
            {
                case WeatherKind.Rainy: SetMode(Particles.Rain); break;
                case WeatherKind.Stormy: SetMode(Particles.Storm); _nextFlash = 0.4f; break;
                case WeatherKind.Snowy: SetMode(Particles.Snow); break;
                case WeatherKind.Windy: SetMode(Particles.Leaves); break;
                default: SetMode(Particles.None); break;
            }
        }

        // Which falling or blowing little pictures fill the village.
        // Which falling or blowing little pictures fill the village; none: what falls now fades out.
        private void SetMode(Particles mode)
        {
            if (mode == Particles.None)
            {
                if (_mode != Particles.None) _fadingMode = _mode;
                _mode = Particles.None;
                return;
            }
            _mode = mode;
            _speckAlpha = 1f;
            var count = mode == Particles.Rain ? 30 : mode == Particles.Storm ? 50 : mode == Particles.Snow ? 32 : 9;
            var sprite = mode == Particles.Snow ? _flakeSprite : mode == Particles.Leaves ? _leafSprite : _dropSprite;
            var width = mode == Particles.Snow ? 38f : mode == Particles.Leaves ? 60f : 22f;
            for (var i = 0; i < _specks.Count; i++)
            {
                var speck = _specks[i];
                speck.Rect.gameObject.SetActive(i < count);
                if (i >= count) continue;
                speck.Image.color = Color.white;
                speck.Image.sprite = sprite;
                var aspect = sprite.rect.width / sprite.rect.height;
                speck.Rect.sizeDelta = new Vector2(width, width / aspect);
                speck.Pos = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-250f, 450f));
                speck.Speed = Random.Range(0.8f, 1.2f);
                speck.Phase = Random.Range(0f, 6.28f);
            }
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

        // One frame of the village. Public so tests can run it without a player loop.
        public void Tick(float seconds)
        {
            if (_round == null || _sessionEnded) return;
            _time += seconds;
            foreach (var piece in _pieces) UpdatePiece(piece, seconds);
            UpdateVillage(seconds);
            UpdateSpecks(seconds);
            UpdateIdle(seconds);
            CheckRoundEnd();
        }

        private void UpdatePiece(Piece piece, float seconds)
        {
            if (piece.Wobble > 0f)
            {
                piece.Wobble = Mathf.Max(0f, piece.Wobble - seconds);
                piece.Art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(piece.Wobble * 50f) * 14f * piece.Wobble * 2f);
            }
            else
            {
                piece.Art.localRotation = Quaternion.Euler(0f, 0f, piece.Held ? Mathf.Sin(_time * 12f) * 3f : 0f);
            }
            piece.Art.localScale = Vector3.one * (piece.Held ? 1.12f : 1f);
            if (!piece.Held && !_helpRunning)
            {
                var target = Place(piece.Home);
                var position = piece.Drag.Rect.anchoredPosition;
                if (position != target) piece.Drag.Rect.anchoredPosition = Vector2.MoveTowards(position, target, ReturnSpeed * seconds);
            }
        }

        private void UpdateVillage(float seconds)
        {
            // The picture of the new weather fades in over the old one.
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + seconds / 1.4f);
                _frontFade.color = new Color(1f, 1f, 1f, _fade);
            }

            // The piece in the sky floats about (the sun breathes); it fades out when the village goes back to a plain day.
            _pieceAlpha = Mathf.MoveTowards(_pieceAlpha, _pieceTarget, seconds / 0.9f);
            _skyPiece.gameObject.SetActive(_pieceAlpha > 0f);
            if (_pieceAlpha > 0f)
            {
                _skyPop = Mathf.Max(0f, _skyPop - seconds);
                var pop = 1f + 0.3f * Mathf.Sin(_skyPop / 0.4f * Mathf.PI);
                var sun = _shown == WeatherKind.Sunny;
                _skyPiece.anchoredPosition = Place(SkyPoint + new Vector2(Mathf.Sin(_time * 0.8f) * 25f, Mathf.Sin(_time * 1.5f) * 10f));
                _skyPiece.localScale = Vector3.one * pop * (sun ? 1f + 0.06f * Mathf.Sin(_time * 2.2f) : 1f);
                _skyPiece.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * (_mode == Particles.Leaves ? 6f : 1f)) * (_mode == Particles.Leaves ? 6f : 2f));
                _skyPieceImage.color = new Color(1f, 1f, 1f, _pieceAlpha);
            }

            // Lightning: a white flash every few seconds in a storm.
            if (_mode == Particles.Storm)
            {
                _nextFlash -= seconds;
                if (_nextFlash <= 0f) { _flashNow = 0.85f; _nextFlash = Random.Range(2f, 3.5f); }
            }
            _flashNow = Mathf.Max(0f, _flashNow - seconds * 2.5f);
            _flash.color = new Color(1f, 1f, 1f, _flashNow);

            _life = Mathf.MoveTowards(_life, _sunLife ? 1f : 0f, seconds / 1.5f);
            _sunny.Show(_life, _time);
        }

        private void UpdateSpecks(float seconds)
        {
            var mode = _mode != Particles.None ? _mode : _fadingMode;
            _speckAlpha = Mathf.MoveTowards(_speckAlpha, _mode == Particles.None ? 0f : 1f, seconds / 1.0f);
            if (mode == Particles.None) return;
            var hide = _mode == Particles.None && _speckAlpha <= 0f;
            foreach (var speck in _specks)
            {
                if (!speck.Rect.gameObject.activeSelf) continue;
                if (hide) { speck.Rect.gameObject.SetActive(false); continue; }
                speck.Image.color = new Color(1f, 1f, 1f, _speckAlpha);
                var angle = 0f;
                switch (mode)
                {
                    case Particles.Rain:
                    case Particles.Storm:
                    {
                        var fast = mode == Particles.Storm ? 1.4f : 1f;
                        speck.Pos += new Vector2(-110f, -950f * fast) * speck.Speed * seconds;
                        angle = 7f;
                        if (speck.Pos.y < -260f) speck.Pos = new Vector2(Random.Range(-900f, 1100f), 460f);
                        break;
                    }
                    case Particles.Snow:
                        speck.Pos += new Vector2(Mathf.Sin(_time * 1.3f + speck.Phase) * 30f, -150f * speck.Speed) * seconds;
                        angle = Mathf.Sin(_time + speck.Phase) * 25f;
                        if (speck.Pos.y < -260f) speck.Pos = new Vector2(Random.Range(-1000f, 1000f), 460f);
                        break;
                    default: // leaves blow across to the right
                        speck.Pos += new Vector2(520f * speck.Speed, Mathf.Cos(_time * 3f + speck.Phase) * 90f) * seconds;
                        angle = Mathf.Sin(_time * 5f + speck.Phase) * 40f;
                        if (speck.Pos.x > 1050f) speck.Pos = new Vector2(-1050f, Random.Range(-200f, 380f));
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
            foreach (var piece in _pieces) if (piece.Held) { _idle = 0f; return; }
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

        // The child took hold of a piece while the hint hand was out: the hint stops and the piece goes home.
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

        // The hand carries the right piece into the sky.
        private IEnumerator RunHint()
        {
            var piece = _pieces[(int)MakeWeather.PieceFor(_round.Asked)];
            _helpRunning = true;
            _hinted = true;
            _eva.SetTalking(true);
            _game.Voice.Say("weather_hint");

            var from = piece.Drag.Rect.anchoredPosition;
            var to = Place(SkyPoint);
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
            yield return new WaitForSeconds(HintGapSeconds);
            _hand.Pulse(false);
            _hand.Hide();
            _eva.SetTalking(false);
            _helpRunning = false;
            _idle = 0f;
            DropPiece(piece);
        }

        // --- Round and session end --------------------------------------------------------------------------------------

        private void CheckRoundEnd()
        {
            if (_roundOver || !_round.Done || _announcing || _skyPop > 0f) return;
            _roundOver = true;
            _runner.StartCoroutine(OnRoundComplete());
        }

        private IEnumerator OnRoundComplete()
        {
            yield return new WaitForSeconds(1.2f); // a moment to look at the weather the child made
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
            if (_roundIndex >= MakeWeather.RoundsPerSession)
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
            yield return _game.Hud.AnimateCoins(before, after, _game.Sfx, _skyPiece.position);
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
            _backFade.sprite = _backgrounds[WeatherKind.Sunny];
            _frontFade.sprite = _backgrounds[WeatherKind.Sunny];
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
