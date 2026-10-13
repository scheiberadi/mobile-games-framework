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
        }

        // --- Layout (picture space, canvas units) ---
        private const float PictureWidth = 1920f;
        public static readonly Vector2 EvaPoint = new Vector2(-790f, -215f);
        public const float EvaHeight = 420f;
        public static readonly Vector2 BuildPoint = new Vector2(150f, 130f);     // the middle of the finished build
        public const float BuildMaxWidth = 880f, BuildMaxHeight = 430f;
        public const float TrayY = -335f, TrayCentreX = 170f, CardPitch = 215f, CardBox = 220f, TapSize = 240f;
        private const float TrayHeight = 250f;
        private const float HeldMinSize = 150f;
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
        private CharacterRig _eva;
        private RectTransform _evaAnchor, _build, _trayField, _pieceField;
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
            BuildEva();
            BuildSlots();
            _trayField = Container("Tray");
            BuildTray();
            _pieceField = Container("Pieces");
            BuildPieces();
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
            _glowRoutine = null;
            _runner.StopAllCoroutines();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            _confetti.SetActive(false);
            _build.gameObject.SetActive(true);
            _build.localScale = Vector3.one;
            _build.localRotation = Quaternion.identity;
            foreach (var slot in _slots)
            {
                slot.Filled = false;
                slot.Dots.gameObject.SetActive(true);
                slot.Dots.color = Color.white;
                slot.Dots.rectTransform.localScale = Vector3.one;
                slot.Solid.gameObject.SetActive(false);
                slot.Solid.rectTransform.localScale = Vector3.one;
            }
            ShuffleTray();
            SetSessionEnded(false);
            _evaAnchor.anchoredPosition = Place(EvaPoint);
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
                var index = i;
                var part = _layout.parts[i];
                var piece = new Piece { PartId = part.part, Part = part };
                // The drag handle is a clear full-size tap target; the picture is a child that is a small card in the tray and grows in the hand.
                piece.Drag = DragItem.Create(_pieceField, "piece" + i, null, Vector2.zero, TapSize);
                piece.Drag.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                var aspect = (float)part.w / part.h;
                piece.Card = aspect >= 1f ? new Vector2(CardBox, CardBox / aspect) : new Vector2(CardBox * aspect, CardBox);
                piece.Art = ChildImage(piece.Drag.transform, "Art", piece.Card);
                piece.Art.sprite = EvaUi.Sprite(Art(part.part));
                var real = new Vector2(part.w, part.h) * _scale;
                var grow = Mathf.Max(1f, HeldMinSize / Mathf.Max(real.x, real.y));
                piece.Held = real * grow;
                piece.Drag.BeginDrag += _ => OnPieceBeginDrag(index);
                piece.Drag.EndDrag += _ => OnPieceEndDrag(index);
                _pieces.Add(piece);
            }
        }

        // The parts in a new mixed-up order, one card each.
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
            for (var k = 0; k < order.Count; k++)
            {
                var piece = _pieces[order[k]];
                piece.Home = new Vector2(TrayCentreX + (k - (order.Count - 1) * 0.5f) * CardPitch, TrayY);
                piece.Placed = false;
                piece.Drag.gameObject.SetActive(true);
                piece.Drag.enabled = true;
                piece.Art.rectTransform.sizeDelta = piece.Card;
                piece.Drag.Rect.anchoredPosition = Place(piece.Home);
                piece.Art.color = Color.white;
                piece.Drag.Rect.localScale = Vector3.one;
            }
            _trayBoard.anchoredPosition = Place(new Vector2(TrayCentreX, TrayY));
        }

        private static bool InBuildOrder(List<int> order)
        {
            for (var i = 0; i < order.Count; i++) if (order[i] != i) return false;
            return true;
        }

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
            while (_announcing) yield return null;
            yield return _game.Voice.SayAndWait("workshop_test_" + BuildLayout.GameName(_kind));
            var leaves = _kind == WorkshopBuildKind.Car || _kind == WorkshopBuildKind.Boat || _kind == WorkshopBuildKind.Rocket;
            _confetti.SetActive(true); // the fanfare and a burst of confetti
            Haptics.Win();
            _eva.Cheer();
            _rightLineIndex = _rightLineIndex % 3 + 1;
            var payout = CoinPayout.ForStep(_ladder.Step);
            _runner.StartCoroutine(PayCoins(payout));
            yield return DoTheThing();
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            if (leaves) _build.gameObject.SetActive(false);
            yield return _game.Voice.SayAndWait("count_done");
            _eva.Cheer();
            SetSessionEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
        }

        private IEnumerator PayCoins(int payout)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, _evaAnchor.position);
        }

        // The car drives off, the boat sails off, the rocket shakes and flies; the house, bridge and robot jump for joy.
        private IEnumerator DoTheThing()
        {
            var start = _build.anchoredPosition;
            switch (_kind)
            {
                case WorkshopBuildKind.Car:
                    yield return Move(start, start + new Vector2(1700f, 0f), 1.8f, 0f, 0f);
                    break;
                case WorkshopBuildKind.Boat:
                    yield return Move(start, start + new Vector2(1700f, 0f), 2.4f, 18f, 4f);
                    break;
                case WorkshopBuildKind.Rocket:
                    yield return Shake(start, 0.7f);
                    yield return Move(start, start + new Vector2(0f, 1100f), 1.5f, 0f, 0f);
                    break;
                case WorkshopBuildKind.Robot:
                    yield return Hop(start, 90f, 3);
                    break;
                default:
                    yield return Hop(start, 40f, 3);
                    break;
            }
        }

        private IEnumerator Move(Vector2 from, Vector2 to, float seconds, float bob, float bobTurns)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = t / seconds;
                var ease = k * k;
                var wave = bob * Mathf.Sin(k * bobTurns * Mathf.PI * 2f);
                _build.anchoredPosition = Vector2.Lerp(from, to, ease) + new Vector2(0f, wave);
                yield return null;
            }
            _build.anchoredPosition = to;
        }

        private IEnumerator Shake(Vector2 at, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    _build.anchoredPosition = at + new Vector2(Mathf.Sin(t * 70f) * 7f, Mathf.Cos(t * 83f) * 4f);
                    yield return null;
                }
            _build.anchoredPosition = at;
        }

        private IEnumerator Hop(Vector2 at, float height, int times)
        {
            const float seconds = 0.55f;
            for (var n = 0; n < times; n++)
            {
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    var k = t / seconds;
                    _build.anchoredPosition = at + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * height);
                    yield return null;
                }
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
