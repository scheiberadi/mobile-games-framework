using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Science Lab's Weather: "Make the weather". A meadow with trees, tulips and puddles; Eva asks for a weather ("Make it rainy!") and a
    // tray of six sky pieces lies along the bottom: the sun, a white cloud, a rain cloud, a storm cloud, a snowflake and the wind. The
    // child drags one into the sky and the village shows what it makes: the sun opens the tulips and dries the puddles, rain brings
    // drops and puddles, a storm adds lightning and shaking trees, snow brings flakes and white mounds, wind sways the trees and blows
    // leaves across, a cloud only darkens the day a little. Any piece makes some weather (never an error); the round is done when it is
    // the weather Eva asked for. Rules: Rules/MakeWeather.cs; this draws them.
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

        private sealed class Tulip
        {
            public RectTransform Rect;
            public Image Image;
            public bool Open;
            public float Delay;
            public float Pop;           // 1 -> 0 a little jump when it opens or closes
        }

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

        private static readonly Vector2[] TreeSpots = { new Vector2(-300f, -10f), new Vector2(230f, -25f) };
        private static readonly float[] TreeHeights = { 300f, 270f };
        private static readonly Vector2[] TulipSpots = { new Vector2(-130f, -110f), new Vector2(60f, -60f), new Vector2(-20f, -175f), new Vector2(-220f, -70f) };
        private const float TulipHeight = 150f;
        private static readonly Vector2[] PuddleSpots = { new Vector2(-420f, -160f), new Vector2(230f, -165f) };
        private static readonly float[] PuddleWidths = { 220f, 200f };
        private static readonly Vector2[] SnowSpots = { new Vector2(-100f, -125f), new Vector2(260f, -195f) };
        private static readonly float[] SnowWidths = { 460f, 300f };

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
        private RectTransform _evaAnchor, _sceneField, _skyField, _speckField, _trayField;
        private RectTransform[] _trees, _puddles, _snows;
        private readonly List<Tulip> _tulips = new List<Tulip>();
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly List<Speck> _specks = new List<Speck>();
        private RectTransform _skyPiece;
        private Image _skyPieceImage, _dim, _flash;
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;
        private Sprite _dropSprite, _flakeSprite, _leafSprite, _tulipOpenSprite, _tulipClosedSprite;

        private MakeWeatherRound _round;
        private WeatherKind[] _session;
        private int _roundIndex, _rightLineIndex;
        private bool _roundOver, _helpRunning, _hinted, _sessionEnded, _announcing;
        private float _idle, _time;
        private readonly Queue<string> _announceQueue = new Queue<string>();
        private Coroutine _hintRoutine;

        // What the village shows now and where it is heading (all eased in Tick).
        private Particles _mode;
        private bool _tulipsWantOpen;
        private float _puddleTarget, _puddleNow, _snowTarget, _snowNow, _dimTarget, _dimNow, _swayTarget, _swayNow, _flashNow, _nextFlash;
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
        public int TulipsOpen { get { var n = 0; foreach (var tulip in _tulips) if (tulip.Open) n++; return n; } }
        public float PuddleLevel => _puddleTarget;
        public float SnowLevel => _snowTarget;
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
            _tulipOpenSprite = EvaUi.Sprite("sciencelab/weatherthing_tulip_open");
            _tulipClosedSprite = EvaUi.Sprite("sciencelab/weatherthing_tulip_closed");
            AddPictureBackground();
            BuildEva();
            _sceneField = Container("Scene");
            BuildScene();
            _dim = FullImage("Dim", Color.black);
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
            ResetVillage();
            LayOut();
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

        private RectTransform Sprite(string name, string sprite, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_sceneField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }

        private static Vector2 FitHeight(RectTransform rect, float height)
        {
            var sprite = rect.GetComponent<Image>().sprite;
            var size = new Vector2(height * sprite.rect.width / sprite.rect.height, height);
            rect.sizeDelta = size;
            return size;
        }

        private static void FitWidth(RectTransform rect, float width)
        {
            var sprite = rect.GetComponent<Image>().sprite;
            rect.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);
        }

        private void BuildScene()
        {
            var bottom = new Vector2(0.5f, 0f);
            var middle = new Vector2(0.5f, 0.5f);
            _snows = new RectTransform[SnowSpots.Length];
            for (var i = 0; i < _snows.Length; i++)
            {
                _snows[i] = Sprite("Snow" + i, "sciencelab/weatherthing_snow", middle);
                FitWidth(_snows[i], SnowWidths[i]);
            }
            _puddles = new RectTransform[PuddleSpots.Length];
            for (var i = 0; i < _puddles.Length; i++)
            {
                _puddles[i] = Sprite("Puddle" + i, "sciencelab/weatherthing_puddle", middle);
                FitWidth(_puddles[i], PuddleWidths[i]);
            }
            for (var i = 0; i < TulipSpots.Length; i++)
            {
                var rect = Sprite("Tulip" + i, "sciencelab/weatherthing_tulip_closed", bottom);
                FitHeight(rect, TulipHeight);
                _tulips.Add(new Tulip { Rect = rect, Image = rect.GetComponent<Image>(), Delay = 0.25f * i });
            }
            _trees = new RectTransform[TreeSpots.Length];
            for (var i = 0; i < _trees.Length; i++)
            {
                _trees[i] = Sprite("Tree" + i, "sciencelab/weatherthing_tree", bottom);
                FitHeight(_trees[i], TreeHeights[i]);
            }
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
            _sceneField.gameObject.SetActive(!ended);
            _skyField.gameObject.SetActive(!ended);
            _speckField.gameObject.SetActive(!ended);
            _trayField.gameObject.SetActive(!ended);
            _dim.gameObject.SetActive(!ended);
            _flash.gameObject.SetActive(!ended);
            if (ended) _confetti.SetActive(false);
            _endPanel.SetActive(ended);
        }

        // Puts every picture of the village where it stands (picture space -> this screen's space).
        private void LayOut()
        {
            _evaAnchor.anchoredPosition = Place(new Vector2(EvaX, EvaFeetY));
            for (var i = 0; i < _trees.Length; i++) _trees[i].anchoredPosition = Place(TreeSpots[i]);
            for (var i = 0; i < _tulips.Count; i++) _tulips[i].Rect.anchoredPosition = Place(TulipSpots[i]);
            for (var i = 0; i < _puddles.Length; i++) _puddles[i].anchoredPosition = Place(PuddleSpots[i]);
            for (var i = 0; i < _snows.Length; i++) _snows[i].anchoredPosition = Place(SnowSpots[i]);
        }

        // A plain day: tulips shut, no puddles, no snow, nothing in the sky.
        private void ResetVillage()
        {
            _mode = Particles.None;
            _tulipsWantOpen = false;
            _puddleTarget = _puddleNow = _snowTarget = _snowNow = _dimTarget = _dimNow = _flashNow = 0f;
            _swayTarget = _swayNow = 0.15f;
            _nextFlash = 1.5f;
            _skyPiece.gameObject.SetActive(false);
            foreach (var tulip in _tulips)
            {
                tulip.Open = false;
                tulip.Pop = 0f;
                tulip.Image.sprite = _tulipClosedSprite;
                FitHeight(tulip.Rect, TulipHeight);
                tulip.Rect.localScale = Vector3.one;
            }
            ApplyPuddlesAndSnow();
            _dim.color = new Color(0f, 0f, 0f, 0f);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            foreach (var speck in _specks) speck.Rect.gameObject.SetActive(false);
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
                if (made == WeatherKind.Rainy || made == WeatherKind.Stormy) _game.Sfx.Splash();
                else _game.Sfx.Pop();
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
        private void ShowWeather(SkyPiece kind, WeatherKind made)
        {
            var sprite = EvaUi.Sprite(SpriteOf(kind));
            _skyPieceImage.sprite = sprite;
            var aspect = sprite.rect.width / sprite.rect.height;
            _skyPiece.sizeDelta = aspect >= 1f ? new Vector2(SkyPieceMax, SkyPieceMax / aspect) : new Vector2(SkyPieceMax * aspect, SkyPieceMax);
            _skyPiece.gameObject.SetActive(true);
            _skyPop = 0.4f;

            switch (made)
            {
                case WeatherKind.Sunny:
                    _tulipsWantOpen = true; _puddleTarget = 0f; _snowTarget = 0f; _dimTarget = 0f; _swayTarget = 0.15f; SetMode(Particles.None);
                    break;
                case WeatherKind.Cloudy:
                    _dimTarget = 0.12f; _swayTarget = 0.25f; SetMode(Particles.None);
                    break;
                case WeatherKind.Rainy:
                    _tulipsWantOpen = false; _puddleTarget = 1f; _snowTarget = 0f; _dimTarget = 0.2f; _swayTarget = 0.3f; SetMode(Particles.Rain);
                    break;
                case WeatherKind.Stormy:
                    _tulipsWantOpen = false; _puddleTarget = 1f; _snowTarget = 0f; _dimTarget = 0.4f; _swayTarget = 1.8f; SetMode(Particles.Storm);
                    _nextFlash = 0.4f;
                    break;
                case WeatherKind.Snowy:
                    _tulipsWantOpen = false; _puddleTarget = 0f; _snowTarget = 1f; _dimTarget = 0.1f; _swayTarget = 0.1f; SetMode(Particles.Snow);
                    break;
                default: // windy
                    _dimTarget = 0f; _swayTarget = 1.4f; SetMode(Particles.Leaves);
                    break;
            }
        }

        // Which falling or blowing little pictures fill the village.
        private void SetMode(Particles mode)
        {
            _mode = mode;
            var count = mode == Particles.Rain ? 30 : mode == Particles.Storm ? 50 : mode == Particles.Snow ? 32 : mode == Particles.Leaves ? 9 : 0;
            var sprite = mode == Particles.Snow ? _flakeSprite : mode == Particles.Leaves ? _leafSprite : _dropSprite;
            var width = mode == Particles.Snow ? 38f : mode == Particles.Leaves ? 60f : 22f;
            for (var i = 0; i < _specks.Count; i++)
            {
                var speck = _specks[i];
                speck.Rect.gameObject.SetActive(i < count);
                if (i >= count) continue;
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
            _puddleNow = Mathf.MoveTowards(_puddleNow, _puddleTarget, seconds * 0.5f);
            _snowNow = Mathf.MoveTowards(_snowNow, _snowTarget, seconds * 0.5f);
            _dimNow = Mathf.MoveTowards(_dimNow, _dimTarget, seconds * 0.5f);
            _swayNow = Mathf.MoveTowards(_swayNow, _swayTarget, seconds * 2f);
            ApplyPuddlesAndSnow();
            _dim.color = new Color(0f, 0f, 0f, _dimNow);

            // The tulips open (or shut) one after another.
            foreach (var tulip in _tulips)
            {
                if (tulip.Open != _tulipsWantOpen)
                {
                    tulip.Delay -= seconds;
                    if (tulip.Delay <= 0f)
                    {
                        tulip.Open = _tulipsWantOpen;
                        tulip.Image.sprite = tulip.Open ? _tulipOpenSprite : _tulipClosedSprite;
                        FitHeight(tulip.Rect, TulipHeight);
                        tulip.Pop = 1f;
                    }
                }
                else tulip.Delay = 0.25f * _tulips.IndexOf(tulip);
                tulip.Pop = Mathf.Max(0f, tulip.Pop - seconds * 3f);
                tulip.Rect.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(tulip.Pop * Mathf.PI));
                tulip.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 2f + tulip.Rect.anchoredPosition.x) * _swayNow * 4f);
            }

            // The trees sway; a storm shakes them.
            for (var i = 0; i < _trees.Length; i++)
            {
                var angle = Mathf.Sin(_time * (1.4f + _swayNow * 1.6f) + i * 1.7f) * _swayNow * 5f;
                if (_swayNow > 1.5f) angle += Mathf.Sin(_time * 23f + i) * 1.5f;
                _trees[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            // The piece in the sky floats about; a wind piece shakes and a storm piece rumbles.
            if (_skyPiece.gameObject.activeSelf)
            {
                _skyPop = Mathf.Max(0f, _skyPop - seconds);
                var pop = 1f + 0.3f * Mathf.Sin(_skyPop / 0.4f * Mathf.PI);
                _skyPiece.anchoredPosition = Place(SkyPoint + new Vector2(Mathf.Sin(_time * 0.8f) * 25f, Mathf.Sin(_time * 1.5f) * 10f));
                _skyPiece.localScale = Vector3.one * pop;
                _skyPiece.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * (_mode == Particles.Leaves ? 6f : 1f)) * (_mode == Particles.Leaves ? 6f : 2f));
            }

            // Lightning: a white flash every few seconds in a storm.
            if (_mode == Particles.Storm)
            {
                _nextFlash -= seconds;
                if (_nextFlash <= 0f) { _flashNow = 0.85f; _nextFlash = Random.Range(2f, 3.5f); }
            }
            _flashNow = Mathf.Max(0f, _flashNow - seconds * 2.5f);
            _flash.color = new Color(1f, 1f, 1f, _flashNow);
        }

        private void ApplyPuddlesAndSnow()
        {
            foreach (var puddle in _puddles)
            {
                puddle.gameObject.SetActive(_puddleNow > 0.01f);
                puddle.localScale = new Vector3(_puddleNow, _puddleNow, 1f);
            }
            foreach (var snow in _snows)
            {
                snow.gameObject.SetActive(_snowNow > 0.01f);
                snow.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, _snowNow);
                snow.GetComponent<Image>().color = new Color(1f, 1f, 1f, _snowNow);
            }
        }

        private void UpdateSpecks(float seconds)
        {
            if (_mode == Particles.None) return;
            foreach (var speck in _specks)
            {
                if (!speck.Rect.gameObject.activeSelf) continue;
                var angle = 0f;
                switch (_mode)
                {
                    case Particles.Rain:
                    case Particles.Storm:
                    {
                        var fast = _mode == Particles.Storm ? 1.4f : 1f;
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
