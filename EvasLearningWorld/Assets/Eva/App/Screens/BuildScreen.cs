using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Workshop's build games (Build a Car / Rocket / House / Boat / Robot, Bridge Building): "Let's build a car!". The finished build is drawn in the middle of
    // the bench as dotted outlines, one for every part, each at the place its part goes. The parts wait in a tray along the bottom (mixed up, every part its
    // own card). The child drags a part out of the tray: it grows to its real size, and the dotted outlines it fits glow. Let go over an outline it fits and it
    // snaps in; the outline turns into the solid part. The last part makes the build do its thing (the car drives off, the rocket flies, the boat sails, the
    // house, bridge and robot jump for joy), with confetti and a coin.
    // Parts that come in pairs (wheels, windows, rails) fit either of their two outlines. A part let go over an outline it does not fit is a miss (retry, then a
    // glow on the right part, then Eva's hand puts the next part in). Letting go anywhere else just sends the part back to the tray.
    //
    // Levels (Rules/Workshop.cs BuildDifficulty, saved per game): 1 = every part has its outline; 2 = only the bigger half of the parts keep theirs, a small
    // picture of the finished build waits in the corner, and one part of another build is mixed into the tray (dropped on the build it is a miss); 3 = no
    // outlines (a part let go near its place still snaps in; the help ladder shows the outline again) and up to two parts of other builds in the tray.
    // The finished build is then played with: tap it and the car drives round, the rocket counts down and blasts off, the boat sails, the robot dances,
    // the house opens its door (the child's character walks over and opens it, there is a room inside) and a car drives over the bridge. The rocket has a
    // red launch button instead (3, 2, 1, blast off). The child's character and Eva stand bottom-left like in the other games (CompanionLayout.Bench).
    //
    // The pictures and where each part sits come from the importer tools/art-import/import-workshop-build.js (Resources/Art/workshop/build_<game>*.png/.json).
    // Layout in "picture space" like CookingMeasuresScreen (the 1920 x 900 frame of the background, x = px - 960, y = 450 - py).
    public sealed class BuildScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Slot
        {
            public BuildPart Part;
            public Vector2 Local;          // middle of the part, inside the build
            public Image Dots, Solid;
            public float Reach, WrongReach;
            public bool Filled;
            public bool Outlined;          // its dotted outline is on show (levels 2 and 3 hide some)
        }

        private sealed class Piece
        {
            public DragItem Drag;
            public Image Art;
            public string PartId;
            public BuildPart Part;
            public Vector2 Home;           // where its card is in the tray (picture space)
            public Vector2 Card;           // its size as a card
            public Vector2 Held;           // its size in the hand
            public bool Placed;
            public bool Decoy;             // a part of another build: it has no place here
            public Vector2 Size;           // its size in the build picture (for the card)
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 LaunchPoint = new Vector2(650f, -300f);      // the rocket's red button
        public const float LaunchSize = 280f;
        public static readonly Vector2 BuildPoint = new Vector2(150f, 130f);     // the middle of the finished build
        public const float BuildMaxWidth = 880f, BuildMaxHeight = 430f;
        public const float TrayY = -335f, TrayCentreX = 170f, CardPitch = 215f, CardBox = 220f, TapSize = 240f;
        private const float TrayHeight = 250f;
        private const float HeldMinSize = 150f;
        private const float TrayCardsSpan = 6f * CardPitch;          // the distance from the first to the last card with 7 cards (more cards squeeze in)
        public static readonly Vector2 BlueprintPoint = new Vector2(-820f, 235f);
        public const float BlueprintBox = 220f;
        private const int PlaysToFinish = 3;
        private const float PlayGiveUpSeconds = 25f, PlayHintSeconds = 6f;
        private const float SlotReachMin = 120f, SlotReachMax = 260f, WrongReachMin = 100f;

        private const float HandMoveSeconds = 0.5f, HandTapSeconds = 0.3f;
        private const float SnapBackSeconds = 0.3f, HintGlowSeconds = 1.2f, IdleHintSeconds = 30f;
        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly WorkshopBuildKind _kind;
        private readonly string _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva, _player;
        private CompanionPair _pair;
        private RectTransform _playerAnchor;
        private Vector3 _playerBaseScale = Vector3.one;
        private GameObject _launch;
        private TMPro.TextMeshProUGUI _countText;
        private Image _inside;
        private RectTransform _evaAnchor, _build, _trayField, _pieceField, _blueprint, _blueprintFrame;
        private GameObject _buildTap;
        private RectTransform _miniCar;
        private readonly List<Image> _miniWheels = new List<Image>();
        private readonly List<Piece> _decoyPool = new List<Piece>();
        private int _realPieces, _level, _plays;
        private bool _playing, _playBusy;
        private float _playIdle;
        private BuildLayout _layout;
        private float _scale = 1f;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Piece> _pieces = new List<Piece>();
        private GameObject _endPanel, _confetti;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private HelpLadder _ladder;
        private int _rightLineIndex, _idleHints, _held = -1, _hintSlot = -1;
        private bool _roundOver, _helpRunning, _sessionEnded, _announcing;
        private float _idle, _time;
        private Coroutine _glowRoutine;

        public BuildScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite, WorkshopBuildKind kind, string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _kind = kind;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        // Read-only state and handles for tests.
        public int SlotCount => _slots.Count;
        public int PieceCount => _pieces.Count;
        public string SlotPartId(int slot) => _slots[slot].Part.part;
        public bool SlotFilled(int slot) => _slots[slot].Filled;
        public bool SolidShown(int slot) => _slots[slot].Solid.gameObject.activeSelf;
        public bool DotsShown(int slot) => _slots[slot].Dots.gameObject.activeSelf;
        public string PieceId(int piece) => _pieces[piece].PartId;
        public bool PiecePlaced(int piece) => _pieces[piece].Placed;
        public DragItem PieceDrag(int piece) => _pieces[piece].Drag;
        public Vector2 PieceHome(int piece) => _pieces[piece].Home;
        public bool RoundOver => _roundOver;
        public bool Hinting => _helpRunning;
        public int Mistakes => _ladder == null ? 0 : _ladder.Mistakes;
        public float Scale => _scale;
        public BuildLayout Layout => _layout;
        public int IdleHintsGiven => _idleHints;
        public int Level => _level;
        public bool PieceIsDecoy(int piece) => _pieces[piece].Decoy;
        public bool SlotOutlined(int slot) => _slots[slot].Outlined;
        public bool BlueprintShown => _blueprint.gameObject.activeSelf;
        public RectTransform BlueprintRect => _blueprintFrame;
        public bool Playing => _playing;
        public bool PlayBusy => _playBusy;
        public int Plays => _plays;
        public RectTransform BuildTapRect => (RectTransform)_buildTap.transform;
        public bool TapAreaShown => _buildTap.activeSelf;
        public Vector2 CardSize(int piece) => _pieces[piece].Card;
        public bool LaunchShown => _launch.activeSelf;
        public RectTransform LaunchRect => (RectTransform)_launch.transform;
        public CompanionPair Pair => _pair;

        public static bool UseDevicePlacement = true;
        private float _scaleX = 1f;
        private Vector2 _shift;

        private Vector2 Place(Vector2 scene) => new Vector2(scene.x * _scaleX - _shift.x, scene.y);

        private Vector2 Scene(Vector2 local) => new Vector2((local.x + _shift.x) / _scaleX, local.y);

        // Where the middle of a slot is, in the screen's own coordinates (the same ones a dragged part's anchoredPosition uses).
        public Vector2 SlotPoint(int slot) => _build.anchoredPosition + _slots[slot].Local;

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
            _layout = BuildLayout.Load(_kind);
            if (_layout == null || _layout.parts == null || _layout.parts.Length == 0) throw new System.InvalidOperationException("No build layout for " + _kind);
            _scale = Mathf.Min(BuildMaxWidth / _layout.width, BuildMaxHeight / _layout.height);

            AddPictureBackground();
            BuildSlots();
            BuildMiniCar();
            BuildEva();
            BuildBlueprint();
            _trayField = Container("Tray");
            BuildTray();
            _pieceField = Container("Pieces");
            BuildPieces();
            BuildTapArea();
            BuildLaunchButton();
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

        public override void OnHide()
        {
            if (_runner != null) _runner.StopAllCoroutines();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _ladder = new HelpLadder();
            _idleHints = 0;
            _rightLineIndex = 0;
            _idle = 0f;
            _held = -1;
            _hintSlot = -1;
            _roundOver = _helpRunning = _sessionEnded = _announcing = false;
            _playing = _playBusy = false;
            _plays = 0;
            _playIdle = 0f;
            _buildTap.SetActive(false);
            _glowRoutine = null;
            _runner.StopAllCoroutines();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            if (_player != null) _player.Root.localScale = _playerBaseScale;
            _launch.SetActive(false);
            _countText.gameObject.SetActive(false);
            if (_inside != null) _inside.gameObject.SetActive(false);
            _confetti.SetActive(false);
            _build.gameObject.SetActive(true);
            _build.localScale = Vector3.one;
            _build.localRotation = Quaternion.identity;
            _level = _game.Progress == null ? BuildDifficulty.MinLevel : BuildDifficulty.LevelOf(_game.Progress, _kind);
            ChooseOutlines();
            foreach (var slot in _slots)
            {
                slot.Filled = false;
                slot.Dots.gameObject.SetActive(slot.Outlined);
                slot.Dots.color = Color.white;
                slot.Dots.rectTransform.localScale = Vector3.one;
                slot.Solid.gameObject.SetActive(false);
                slot.Solid.rectTransform.localScale = Vector3.one;
                slot.Solid.rectTransform.localRotation = Quaternion.identity;
                slot.Solid.rectTransform.anchoredPosition = slot.Local;
            }
            _miniCar.gameObject.SetActive(false);
            _blueprint.gameObject.SetActive(BuildDifficulty.ShowsPicture(_level));
            ChooseDecoys();
            ShuffleTray();
            SetSessionEnded(false);
            PlaceCompanions();
            _blueprint.anchoredPosition = Place(BlueprintPoint);
            PositionBuild();
            _runner.StartCoroutine(Loop());
            _runner.StartCoroutine(SayIntro());
        }

        private IEnumerator SayIntro()
        {
            _announcing = true;
            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("workshop_build_" + BuildLayout.GameName(_kind));
            _eva.SetTalking(false);
            _announcing = false;
        }

        // --- Building the screen ----------------------------------------------------------------------------------------

        private string Art(string part) => "workshop/build_" + BuildLayout.GameName(_kind) + "_" + part;

        private void BuildSlots()
        {
            var go = new GameObject("Build", typeof(RectTransform));
            go.transform.SetParent(Root, false);
            _build = (RectTransform)go.transform;
            _build.anchorMin = _build.anchorMax = _build.pivot = new Vector2(0.5f, 0.5f);
            _build.sizeDelta = Vector2.zero;
            var pad = _layout.pad;
            foreach (var part in _layout.parts)
            {
                var slot = new Slot { Part = part };
                slot.Local = new Vector2((part.x + part.w * 0.5f - _layout.width * 0.5f) * _scale, -(part.y + part.h * 0.5f - _layout.height * 0.5f) * _scale);
                var maxSide = Mathf.Max(part.w, part.h) * _scale;
                slot.Reach = Mathf.Clamp(maxSide * 0.5f + 40f, SlotReachMin, SlotReachMax);
                slot.WrongReach = Mathf.Max(WrongReachMin, maxSide * 0.5f);
                if (_kind == WorkshopBuildKind.House && part.part == "door")
                {
                    // The room behind the door: shown while the door is open.
                    _inside = ChildImage(_build, "Inside", new Vector2(part.w * _scale, part.h * _scale));
                    _inside.sprite = EvaUi.Sprite("workshop/build_house_inside");
                    _inside.rectTransform.anchoredPosition = slot.Local;
                    _inside.gameObject.SetActive(false);
                }
                slot.Dots = ChildImage(_build, "Dots_" + part.part, new Vector2((part.w + pad * 2) * _scale, (part.h + pad * 2) * _scale));
                slot.Dots.sprite = EvaUi.Sprite(Art(part.part + "_slot"));
                slot.Dots.rectTransform.anchoredPosition = slot.Local;
                slot.Solid = ChildImage(_build, "Solid_" + part.part, new Vector2(part.w * _scale, part.h * _scale));
                slot.Solid.sprite = EvaUi.Sprite(Art(part.part));
                slot.Solid.rectTransform.anchoredPosition = slot.Local;
                slot.Solid.gameObject.SetActive(false);
                _slots.Add(slot);
            }
        }

        // The bigger parts keep their outline on level 2, none do on level 3 (the part list order breaks ties).
        private void ChooseOutlines()
        {
            var order = new List<int>();
            for (var i = 0; i < _slots.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                var byArea = (_slots[b].Part.w * _slots[b].Part.h).CompareTo(_slots[a].Part.w * _slots[a].Part.h);
                return byArea != 0 ? byArea : a.CompareTo(b);
            });
            var keep = BuildDifficulty.OutlineCount(_level, _slots.Count);
            for (var rank = 0; rank < order.Count; rank++) _slots[order[rank]].Outlined = rank < keep;
        }

        // The cards that do not belong to this build: the first few of the pool, mixed into the tray after the real parts.
        private void ChooseDecoys()
        {
            _pieces.RemoveRange(_realPieces, _pieces.Count - _realPieces);
            var count = Mathf.Min(BuildDifficulty.DecoyCount(_level, _realPieces), _decoyPool.Count);
            for (var i = 0; i < _decoyPool.Count; i++)
            {
                var active = i < count;
                _decoyPool[i].Drag.gameObject.SetActive(active);
                if (active) _pieces.Add(_decoyPool[i]);
            }
        }

        // A small picture of the finished build in the corner (levels 2 and 3), made of the same part pictures.
        private void BuildBlueprint()
        {
            _blueprint = Child(Root, "Blueprint", Vector2.zero);
            var s = Mathf.Min(BlueprintBox / _layout.width, BlueprintBox / _layout.height);
            _blueprintFrame = Child(_blueprint, "Frame", new Vector2(_layout.width * s + 30f, _layout.height * s + 30f));
            var frame = _blueprintFrame.gameObject.AddComponent<Image>();
            frame.sprite = RoundedRectSprite.Get();
            frame.type = Image.Type.Sliced;
            frame.color = new Color(1f, 1f, 1f, 0.6f);
            frame.raycastTarget = false;
            foreach (var part in _layout.parts)
            {
                var size = new Vector2(part.w * s, part.h * s);
                var image = ChildImage(_blueprint, "Picture_" + part.part, size);
                image.sprite = EvaUi.Sprite(Art(part.part));
                image.rectTransform.anchoredPosition = new Vector2((part.x + part.w * 0.5f - _layout.width * 0.5f) * s, -(part.y + part.h * 0.5f - _layout.height * 0.5f) * s);
            }
            _blueprint.gameObject.SetActive(false);
        }

        // The bridge visitor: a little car made of the car own part pictures.
        private void BuildMiniCar()
        {
            _miniCar = Child(_build, "MiniCar", Vector2.zero);
            if (_kind == WorkshopBuildKind.Bridge)
            {
                var car = BuildLayout.Load(WorkshopBuildKind.Car);
                var s = Mathf.Min(200f / car.width, 200f / car.height) * 0.9f;
                foreach (var part in car.parts)
                {
                    var image = ChildImage(_miniCar, "Car_" + part.part, new Vector2(part.w * s, part.h * s));
                    image.sprite = EvaUi.Sprite("workshop/build_car_" + part.part);
                    image.rectTransform.anchoredPosition = new Vector2((part.x + part.w * 0.5f - car.width * 0.5f) * s, -(part.y + part.h * 0.5f - car.height * 0.5f) * s);
                    if (part.part == "wheel") _miniWheels.Add(image);
                }
                _miniCarHalfHeight = car.height * s * 0.5f;
                _miniCarHalfWidth = car.width * s * 0.5f;
            }
            _miniCar.gameObject.SetActive(false);
        }

        private float _miniCarHalfHeight, _miniCarHalfWidth;

        // Over the finished build: tap it to play with it.
        private void BuildTapArea()
        {
            _buildTap = new GameObject("BuildTap", typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget));
            _buildTap.transform.SetParent(Root, false);
            var rect = (RectTransform)_buildTap.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(TapSize, _layout.width * _scale), Mathf.Max(TapSize, _layout.height * _scale));
            var image = _buildTap.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            _buildTap.GetComponent<Button>().onClick.AddListener(TapBuild);
            _buildTap.SetActive(false);
        }

        private void BuildTray()
        {
            var back = Child(_trayField, "Board", new Vector2(_layout.parts.Length * CardPitch + 70f, TrayHeight));
            var image = back.gameObject.AddComponent<Image>();
            image.sprite = RoundedRectSprite.Get();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.36f, 0.2f, 0.08f, 0.55f);
            image.raycastTarget = false;
            _trayBoard = back;
        }

        private RectTransform _trayBoard;

        private void BuildPieces()
        {
            for (var i = 0; i < _layout.parts.Length; i++)
            {
                var part = _layout.parts[i];
                _pieces.Add(MakePiece(i, part.part, part, Art(part.part), false));
            }
            _realPieces = _pieces.Count;
            // The parts of other builds: made once, switched on by the level.
            var pool = BuildDifficulty.DecoyPool(_kind);
            for (var d = 0; d < pool.Length && d < 2; d++)
            {
                var cut = pool[d].IndexOf('_');
                if (!System.Enum.TryParse(pool[d].Substring(0, cut), true, out WorkshopBuildKind other)) continue;
                var otherLayout = BuildLayout.Load(other);
                var found = otherLayout == null ? null : System.Array.Find(otherLayout.parts, p => p.part == pool[d].Substring(cut + 1));
                if (found == null) continue;
                _decoyPool.Add(MakePiece(_realPieces + _decoyPool.Count, pool[d], found, "workshop/build_" + pool[d], true));
            }
        }

        private Piece MakePiece(int index, string partId, BuildPart part, string artPath, bool decoy)
        {
            var piece = new Piece { PartId = partId, Part = part, Decoy = decoy, Size = new Vector2(part.w, part.h) };
            // The drag handle is a clear full-size tap target; the picture is a child that is a small card in the tray and grows in the hand.
            piece.Drag = DragItem.Create(_pieceField, "piece" + index, null, Vector2.zero, TapSize);
            piece.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            piece.Card = CardOf(piece.Size, CardBox);
            piece.Art = ChildImage(piece.Drag.transform, "Art", piece.Card);
            piece.Art.sprite = EvaUi.Sprite(artPath);
            var real = piece.Size * _scale;
            var grow = Mathf.Max(1f, HeldMinSize / Mathf.Max(real.x, real.y));
            piece.Held = real * grow;
            piece.Drag.BeginDrag += _ => OnPieceBeginDrag(index);
            piece.Drag.EndDrag += _ => OnPieceEndDrag(index);
            if (decoy) piece.Drag.gameObject.SetActive(false);
            return piece;
        }

        private static Vector2 CardOf(Vector2 size, float box)
        {
            var aspect = size.x / size.y;
            return aspect >= 1f ? new Vector2(box, box / aspect) : new Vector2(box * aspect, box);
        }

        // The parts in a new mixed-up order, one card each (more than seven cards squeeze closer together).
        private void ShuffleTray()
        {
            var order = new List<int>();
            for (var i = 0; i < _pieces.Count; i++) order.Add(i);
            do
            {
                for (var i = order.Count - 1; i > 0; i--)
                {
                    var j = _rng.Next(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
            } while (_pieces.Count > 2 && InBuildOrder(order));
            var pitch = order.Count <= 7 ? CardPitch : TrayCardsSpan / (order.Count - 1);
            var box = Mathf.Min(CardBox, pitch + 5f);
            for (var k = 0; k < order.Count; k++)
            {
                var piece = _pieces[order[k]];
                piece.Home = new Vector2(TrayCentreX + (k - (order.Count - 1) * 0.5f) * pitch, TrayY);
                piece.Placed = false;
                piece.Drag.gameObject.SetActive(true);
                piece.Drag.enabled = true;
                piece.Card = CardOf(piece.Size, box);
                piece.Art.rectTransform.sizeDelta = piece.Card;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.color = Color.white;
                piece.Drag.Rect.localScale = Vector3.one;
            }
            _trayBoard.sizeDelta = new Vector2((order.Count - 1) * pitch + pitch + 70f, TrayHeight);
            _trayBoard.anchoredPosition = Place(new Vector2(TrayCentreX, TrayY));
        }

        private static bool InBuildOrder(List<int> order)
        {
            for (var i = 0; i < order.Count; i++) if (order[i] != i) return false;
            return true;
        }

        // The child's character and Eva, side by side at the bottom-left.
        private void BuildEva()
        {
            _pair = CompanionPair.Create(Root, _game, CompanionLayout.Bench);
            _eva = _pair.Eva;
            _player = _pair.Player;
            _evaAnchor = _pair.EvaAnchor;
            _playerAnchor = _pair.PlayerAnchor;
            _evaBaseScale = _eva.Root.localScale;
            _playerBaseScale = _player.Root.localScale;
        }

        private void PlaceCompanions()
        {
            var layout = CompanionLayout.Bench;
            _playerAnchor.anchoredPosition = Place(new Vector2(layout.PlayerX, layout.FeetY));
            _evaAnchor.anchoredPosition = Place(new Vector2(layout.EvaX, layout.FeetY));
        }

        // The rocket's big red button (the rocket is not tapped) and the numbers of its countdown.
        private void BuildLaunchButton()
        {
            var button = EvaUi.IconButton(Root, "LaunchButton", EvaUi.Sprite("icons/launch"), new Vector2(0.5f, 0.5f), Vector2.zero, LaunchSize, TapBuild);
            _launch = button.gameObject;
            _launch.SetActive(false);
            _countText = EvaUi.Numeral(Root, "Countdown", 380);
            _countText.color = new Color(1f, 0.5f, 0.1f);
            _countText.fontStyle = TMPro.FontStyles.Bold;
            var rect = _countText.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 400f);
            _countText.gameObject.SetActive(false);
        }

        private void BuildConfetti()
        {
            // An inactive WinCelebration: switching it on plays the fanfare and bursts the confetti.
            _confetti = new GameObject("BuildConfetti", typeof(RectTransform), typeof(WinCelebration));
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
                EndButtonPositions[1], EndButtonSize, GoHome);
            _endPanel.SetActive(false);
        }

        private void GoHome() => _game.Navigator.Show(_homeScreenId);

        private void SetSessionEnded(bool ended)
        {
            _sessionEnded = ended;
            if (ended && _hand != null) _hand.Hide();
            _trayField.gameObject.SetActive(!ended);
            _pieceField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Frames -------------------------------------------------------------------------------------------------------

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
            if (_sessionEnded || _slots.Count == 0) return;
            _time += seconds;
            PositionBuild();
            UpdateGlow();
            UpdateIdle(seconds);
        }

        private void PositionBuild()
        {
            if (!_roundOver) _build.anchoredPosition = Place(BuildPoint);
        }

        // The outlines the held part (or the hint) fits breathe; the others rest.
        private void UpdateGlow()
        {
            var wanted = _held >= 0 ? _pieces[_held].PartId : null;
            var pulse = Mathf.PingPong(_time * 1.6f, 1f);
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Filled) continue;
                var shown = slot.Outlined || i == _hintSlot; // a hidden outline shows up when the help points at it
                if (slot.Dots.gameObject.activeSelf != shown) slot.Dots.gameObject.SetActive(shown);
                if (!shown) continue;
                var lit = (wanted != null && slot.Part.part == wanted) || i == _hintSlot;
                slot.Dots.color = lit ? Color.Lerp(Color.white, new Color(1f, 0.9f, 0.35f, 1f), pulse) : new Color(1f, 1f, 1f, 0.9f);
                slot.Dots.rectTransform.localScale = Vector3.one * (lit ? 1f + 0.04f * pulse : 1f);
            }
        }

        // A game left open: Eva's hand shows what to do, at most twice per session.
        private void UpdateIdle(float seconds)
        {
            if (_roundOver || _helpRunning || _announcing || _held >= 0) { _idle = 0f; return; }
            _idle += seconds;
            if (_idle < IdleHintSeconds) return;
            _idle = 0f;
            if (!IdleHints.Take(ref _idleHints)) return;
            _runner.StartCoroutine(ShowWhere());
        }

        // --- Drag --------------------------------------------------------------------------------------------------------

        private void OnPieceBeginDrag(int i)
        {
            if (_roundOver || _pieces[i].Placed) return;
            _held = i;
            _idle = 0f;
            _pieces[i].Art.rectTransform.sizeDelta = _pieces[i].Held;
        }

        private void OnPieceEndDrag(int i) => Release(i);

        // The part is let go where it is: in a slot it fits, over one it does not fit, or nowhere.
        public void Release(int i)
        {
            if (_held == i) _held = -1;
            _idle = 0f;
            var piece = _pieces[i];
            if (_roundOver || piece.Placed) return;
            var at = piece.Drag.Rect.anchoredPosition;
            var fit = NearestSlot(at, piece.PartId, true);
            if (fit >= 0) { PlacePiece(i, fit); return; }
            var wrong = NearestSlot(at, piece.PartId, false);
            SnapBack(i);
            if (wrong >= 0) HandleMistake();
        }

        // The nearest open slot within reach that takes this part (matching) or that does not (not matching).
        private int NearestSlot(Vector2 at, string partId, bool matching)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var s = 0; s < _slots.Count; s++)
            {
                var slot = _slots[s];
                if (slot.Filled || (slot.Part.part == partId) != matching) continue;
                var distance = Vector2.Distance(at, SlotPoint(s));
                if (distance <= (matching ? slot.Reach : slot.WrongReach) && distance < bestDistance) { best = s; bestDistance = distance; }
            }
            return best;
        }

        private void SnapBack(int i)
        {
            var piece = _pieces[i];
            piece.Art.rectTransform.sizeDelta = piece.Card;
            _runner.StartCoroutine(SlideTo(piece.Drag.Rect, Place(piece.Home), SnapBackSeconds));
        }

        private void PlacePiece(int i, int slotIndex)
        {
            var piece = _pieces[i];
            var slot = _slots[slotIndex];
            piece.Placed = true;
            slot.Filled = true;
            piece.Drag.gameObject.SetActive(false);
            slot.Dots.gameObject.SetActive(false);
            slot.Solid.gameObject.SetActive(true);
            if (_hintSlot == slotIndex) _hintSlot = -1;
            if (_game.Sfx != null) _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(slot.Solid.rectTransform, slot.Solid, 1.1f, 0.25f));
            if (AllFilled()) _runner.StartCoroutine(Finish());
        }

        private bool AllFilled()
        {
            foreach (var slot in _slots) if (!slot.Filled) return false;
            return true;
        }

        // --- Help --------------------------------------------------------------------------------------------------------

        // The part the next help is about: the first open slot (the body before the little parts) and a card that fits it.
        private int NextSlot()
        {
            for (var s = 0; s < _slots.Count; s++) if (!_slots[s].Filled) return s;
            return -1;
        }

        private int CardFor(int slot)
        {
            if (slot < 0) return -1;
            for (var i = 0; i < _pieces.Count; i++) if (!_pieces[i].Placed && _pieces[i].PartId == _slots[slot].Part.part) return i;
            return -1;
        }

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    if (_game.Sfx != null) _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _helpRunning = true;
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _helpRunning = true;
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd miss: the card of the next part glows in the tray and its outline glows in the build.
        private IEnumerator RunHint()
        {
            SetInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);
            var lead = _game.Voice.Duration(_hintVoiceKey) + Voice.BreathSeconds;
            var slot = NextSlot();
            var card = CardFor(slot);
            if (card >= 0)
            {
                _hintSlot = slot;
                _glowRoutine = _runner.StartCoroutine(GlowCard(card, Mathf.Max(lead, HintGlowSeconds) + HintGlowSeconds));
            }
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            _hintSlot = -1;
            SetInteractable(true);
            _helpRunning = false;
        }

        private IEnumerator GlowCard(int index, float duration)
        {
            var image = _pieces[index].Art;
            var glow = new Color(1f, 0.9f, 0.4f, 1f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(Color.white, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = Color.white;
        }

        // 3rd miss: the hand puts the next part in place, then hands over (the coin is then Demonstrated).
        private IEnumerator RunDemonstrate()
        {
            SetInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);
            var slot = NextSlot();
            var card = CardFor(slot);
            if (slot >= 0 && card >= 0)
            {
                yield return _hand.MoveTo(Place(_pieces[card].Home), HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(SlotPoint(slot), HandMoveSeconds);
                _hand.Hide();
                PlacePiece(card, slot);
            }
            SetInteractable(true);
            _helpRunning = false;
        }

        // A long quiet: the hand shows where the next part goes (it does not place it).
        private IEnumerator ShowWhere()
        {
            _helpRunning = true;
            var slot = NextSlot();
            var card = CardFor(slot);
            if (slot >= 0 && card >= 0)
            {
                _hintSlot = slot;
                yield return _hand.MoveTo(Place(_pieces[card].Home), HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(SlotPoint(slot), HandMoveSeconds * 2f);
                yield return new WaitForSeconds(0.4f);
                _hand.Hide();
                _hintSlot = -1;
            }
            _helpRunning = false;
        }

        private void SetInteractable(bool interactable)
        {
            foreach (var piece in _pieces) if (!piece.Placed) piece.Drag.enabled = interactable;
        }

        // --- The build does its thing ---------------------------------------------------------------------------------------

        private IEnumerator Finish()
        {
            _roundOver = true;
            _held = -1;
            _hintSlot = -1;
            if (_hand != null) _hand.Hide();
            SetInteractable(false);
            _pieceField.gameObject.SetActive(false);
            _trayField.gameObject.SetActive(false);
            _blueprint.gameObject.SetActive(false);
            while (_announcing) yield return null;
            yield return _game.Voice.SayAndWait("workshop_test_" + BuildLayout.GameName(_kind));
            _confetti.SetActive(true); // the fanfare and a burst of confetti
            Haptics.Win();
            _eva.Cheer();
            _player.Cheer();
            _rightLineIndex = _rightLineIndex % 3 + 1;
            var step = _ladder.Step;
            var payout = CoinPayout.ForStep(step);
            BuildDifficulty.Record(_game.Progress, _kind, step);
            _runner.StartCoroutine(PayCoins(payout));
            yield return _game.Voice.SayAndWait(_kind == WorkshopBuildKind.Rocket ? "workshop_press_rocket" : "workshop_tap_" + BuildLayout.GameName(_kind));
            BeginPlay();
            // Play with the finished build until it has been tapped a few times (or nobody touches it for a while).
            while (_plays < PlaysToFinish && _playIdle < PlayGiveUpSeconds)
            {
                if (!_playBusy) _playIdle += Time.deltaTime;
                if (_plays == 0 && !_playBusy && !_helpRunning && _playIdle >= PlayHintSeconds)
                {
                    _playIdle = 0f;
                    _runner.StartCoroutine(ShowTapHint());
                }
                yield return null;
            }
            while (_playBusy) yield return null;
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return _game.Voice.SayAndWait("count_done");
            _eva.Cheer();
            _player.Cheer();
            SetSessionEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
        }

        // The finished build can be tapped (and stays tappable after the end buttons show up).
        public void BeginPlay()
        {
            _playing = true;
            _plays = 0;
            _playIdle = 0f;
            var rocket = _kind == WorkshopBuildKind.Rocket;
            ((RectTransform)_buildTap.transform).anchoredPosition = Place(BuildPoint);
            _buildTap.SetActive(!rocket);
            ((RectTransform)_launch.transform).anchoredPosition = Place(LaunchPoint);
            _launch.SetActive(rocket);
        }

        public void TapBuild()
        {
            if (!_playing || _playBusy) return;
            _plays++;
            _playIdle = 0f;
            if (_hand != null) _hand.Hide();
            _playBusy = true;
            _runner.StartCoroutine(PlayReaction());
        }

        private IEnumerator ShowTapHint()
        {
            _helpRunning = true;
            yield return _hand.MoveTo(Place(_kind == WorkshopBuildKind.Rocket ? LaunchPoint + new Vector2(-40f, -20f) : BuildPoint + new Vector2(-120f, -80f)), HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            yield return new WaitForSeconds(0.4f);
            if (_plays == 0) _hand.Hide();
            _helpRunning = false;
        }

        private IEnumerator PayCoins(int payout)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, _evaAnchor.position);
        }

        // --- Playing with the finished build ----------------------------------------------------------------------------------
        // The whole build moves as one piece (no part is turned or moved on its own, so nothing comes loose).

        private void Toy(string sound)
        {
            if (_game.Sfx != null) _game.Sfx.Toy(sound);
        }

        private IEnumerator PlayReaction()
        {
            if (_game.Sfx != null && _kind != WorkshopBuildKind.Rocket) _game.Sfx.Pop();
            switch (_kind)
            {
                case WorkshopBuildKind.Car: Toy("car_go"); yield return DriveAround(1.3f, 5f, 5f); break;
                case WorkshopBuildKind.Boat: Toy("boat_go"); yield return DriveAround(1.8f, 14f, 3f); break;
                case WorkshopBuildKind.Rocket: yield return BlastOff(); break;
                case WorkshopBuildKind.Robot: Toy("robot_dance"); yield return Dance(); break;
                case WorkshopBuildKind.House: yield return OpenTheDoor(); break;
                case WorkshopBuildKind.Bridge: Toy("car_cross"); yield return CarCrosses(); break;
            }
            _build.anchoredPosition = Place(BuildPoint);
            _build.localRotation = Quaternion.identity;
            _playBusy = false;
            _playIdle = 0f;
        }

        private List<Slot> SlotsOf(string part)
        {
            var found = new List<Slot>();
            foreach (var slot in _slots) if (slot.Part.part == part) found.Add(slot);
            return found;
        }

        // Out to the right, back in from the left.
        private IEnumerator DriveAround(float seconds, float bob, float bobTurns)
        {
            var start = Place(BuildPoint);
            yield return MoveAcross(start, start + new Vector2(1900f, 0f), seconds, bob, bobTurns, false);
            yield return MoveAcross(start - new Vector2(1900f, 0f), start, seconds, bob, bobTurns, true);
        }

        private IEnumerator MoveAcross(Vector2 from, Vector2 to, float seconds, float bob, float bobTurns, bool easeOut)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                var ease = easeOut ? 1f - (1f - k) * (1f - k) : k * k;
                var wave = bob * Mathf.Sin(k * bobTurns * Mathf.PI * 2f);
                _build.anchoredPosition = Vector2.Lerp(from, to, ease) + new Vector2(0f, wave);
                yield return null;
            }
            _build.anchoredPosition = to;
        }

        // The red button: 3, 2, 1 (shaking harder, Eva and the character cheering), blast off, up and away; it comes back down from the top.
        private IEnumerator BlastOff()
        {
            var start = Place(BuildPoint);
            var flames = SlotsOf("flame");
            Toy("rocket_rumble");
            var words = new[] { "rocket_three", "rocket_two", "rocket_one" };
            for (var n = 0; n < words.Length; n++)
            {
                _game.Voice.Say(words[n], 1f + 0.06f * n);
                ShowCount(3 - n);
                _eva.Cheer();
                _player.Cheer();
                yield return Shake(start, 1f, 1f + n * 0.9f);
            }
            _countText.gameObject.SetActive(false);
            _game.Voice.Say("rocket_blastoff", 1.12f);
            Toy("rocket_launch");
            _eva.Cheer();
            _player.Cheer();
            for (var t = 0f; t < 1.3f; t += Time.deltaTime)
            {
                var k = t / 1.3f;
                _build.anchoredPosition = Vector2.Lerp(start, start + new Vector2(0f, 1200f), k * k);
                foreach (var flame in flames) flame.Solid.rectTransform.localScale = new Vector3(1f, 1f + 0.4f * Mathf.PingPong(t * 12f, 1f), 1f);
                yield return null;
            }
            foreach (var flame in flames) flame.Solid.rectTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.4f);
            for (var t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                var k = t / 1.1f;
                _build.anchoredPosition = Vector2.Lerp(start + new Vector2(0f, 1200f), start, 1f - (1f - k) * (1f - k));
                yield return null;
            }
        }

        // A big number next to the rocket that pops in.
        private void ShowCount(int number)
        {
            _countText.text = number.ToString();
            _countText.rectTransform.anchoredPosition = Place(BuildPoint + new Vector2(-330f, 20f));
            _countText.gameObject.SetActive(true);
            _runner.StartCoroutine(PopNumber(_countText.rectTransform));
        }

        private static IEnumerator PopNumber(RectTransform target)
        {
            for (var t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, t / 0.3f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Three hops with the whole robot swaying from side to side.
        private IEnumerator Dance()
        {
            var start = Place(BuildPoint);
            const float seconds = 0.5f;
            for (var n = 0; n < 3; n++)
            {
                var side = n % 2 == 0 ? 1f : -1f;
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    var k = t / seconds;
                    _build.anchoredPosition = start + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 70f);
                    _build.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * 8f * side);
                    yield return null;
                }
            }
            _build.anchoredPosition = start;
            _build.localRotation = Quaternion.identity;
        }

        // The child's character walks over, the door swings open on a room inside, then closes and the character walks back.
        private IEnumerator OpenTheDoor()
        {
            var doors = SlotsOf("door");
            if (doors.Count == 0) yield break;
            var door = doors[0];
            var doorIndex = _slots.IndexOf(door);
            var doorMiddle = SlotPoint(doorIndex);
            var stand = new Vector2(doorMiddle.x - door.Part.w * _scale * 0.5f - 70f, doorMiddle.y - door.Part.h * _scale * 0.5f - 6f);
            var home = _playerAnchor.anchoredPosition;
            var small = _playerBaseScale * 0.6f;
            yield return Walk(home, stand, 1.4f, _playerBaseScale, small);
            _player.Wave();
            yield return new WaitForSeconds(0.4f);
            Toy("door_open");
            _inside.gameObject.SetActive(true);
            yield return SwingDoor(door, 0f, 0.9f, 0.85f);
            _player.Cheer();
            yield return new WaitForSeconds(1.2f);
            yield return SwingDoor(door, 0.9f, 0f, 0.7f);
            _inside.gameObject.SetActive(false);
            yield return Walk(stand, home, 1.4f, small, _playerBaseScale);
        }

        private IEnumerator Walk(Vector2 from, Vector2 to, float seconds, Vector3 scaleFrom, Vector3 scaleTo)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                _playerAnchor.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0f, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 7f)) * 14f);
                _player.Root.localScale = Vector3.Lerp(scaleFrom, scaleTo, k);
                yield return null;
            }
            _playerAnchor.anchoredPosition = to;
            _player.Root.localScale = scaleTo;
        }

        // The door narrows to its hinge (open 0 = shut, 0.9 = wide open), moving from one opening to the other.
        private IEnumerator SwingDoor(Slot door, float openFrom, float openTo, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                SetDoorOpen(door, Mathf.Lerp(openFrom, openTo, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetDoorOpen(door, openTo);
        }

        private void SetDoorOpen(Slot door, float open)
        {
            var widthScale = Mathf.Clamp(1f - open, 0.1f, 1f);
            door.Solid.rectTransform.localScale = new Vector3(widthScale, 1f, 1f);
            door.Solid.rectTransform.anchoredPosition = door.Local - new Vector2(door.Part.w * _scale * 0.5f * (1f - widthScale), 0f);
        }

        // A small car drives over the deck from one bank to the other.
        private IEnumerator CarCrosses()
        {
            var decks = SlotsOf("deck");
            if (decks.Count == 0) yield break;
            var deck = decks[0];
            var half = _layout.width * _scale * 0.5f;
            var y = deck.Local.y + deck.Part.h * _scale * 0.5f + _miniCarHalfHeight * 0.55f;
            _miniCar.gameObject.SetActive(true);
            const float seconds = 2.4f;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                _miniCar.anchoredPosition = new Vector2(Mathf.Lerp(-half - _miniCarHalfWidth, half + _miniCarHalfWidth, k), y + Mathf.Sin(k * Mathf.PI * 10f) * 3f);
                yield return null;
            }
            _miniCar.gameObject.SetActive(false);
        }

        private IEnumerator Shake(Vector2 at, float seconds, float strength = 1f)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                _build.anchoredPosition = at + new Vector2(Mathf.Sin(t * 70f) * 7f, Mathf.Cos(t * 83f) * 4f) * strength;
                yield return null;
            }
            _build.anchoredPosition = at;
        }

        // --- Small tweens (same shapes as the other drag screens) ----------------------------------------------------------

        private static IEnumerator SlideTo(RectTransform target, Vector2 destination, float duration)
        {
            var start = target.anchoredPosition;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.anchoredPosition = Vector2.Lerp(start, destination, t / duration);
                yield return null;
            }
            target.anchoredPosition = destination;
        }

        private static IEnumerator PopPulse(RectTransform target, Image tint, float peakScale, float duration)
        {
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, t / half);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // --- Helpers ----------------------------------------------------------------------------------------------------

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

        private static RectTransform Child(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static Image ChildImage(Transform parent, string name, Vector2 size)
        {
            var rect = Child(parent, name, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
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
