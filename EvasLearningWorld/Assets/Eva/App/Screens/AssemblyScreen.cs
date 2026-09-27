using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shared BUILD -> TEST -> OBSERVE presenter (Workshop, M4.6, docs/kids-games/full-catalogue-plan.md
    // "8. Workshop"): reuses Dress for the Occasion's own slot/shelf/distractor shape and DragItem snap
    // mechanic (read that screen first) - a slot wants one specific part, same as a body-part slot there, and
    // a distractor dragged onto any slot is this game's mistake. Unlike Dress for the Occasion, filling every
    // slot does not end the round outright: it starts a short TEST animation (the assembled build pops and its
    // own test sprite/voice line plays - the car drives, the rocket launches, the ball rolls) before the usual
    // reward flow. One instance is built per Workshop game (registered under its own ScreenId, same as
    // `new MatchScreen(...)`), configured with that game's WorkshopBuildKind.
    public sealed class AssemblyScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float EvaHeight = 430f;
        private static readonly Vector2 EvaPosition = new Vector2(660f, 220f); // clear of the 5-item shelf below

        private const int MaxItems = 5;
        private const int SlotCount = 3;

        private const float HintGlowSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float SnapBackSeconds = 0.3f;
        private const float TestPopSeconds = 0.5f;
        private const float TestPopScale = 1.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly WorkshopBuildKind _kind;
        private readonly System.Func<PlayerProgress, int> _getLevel;
        private readonly System.Action<PlayerProgress, int> _setLevel;
        private readonly System.Func<PlayerProgress, System.Collections.Generic.List<bool>> _getBuffer;
        private readonly string _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _slotField, _pieceField, _testField;
        private Image[] _slotImages;
        private bool[] _slotFilled;
        private DragItem[] _pieceItems;
        private Image[] _pieceImages;
        private bool[] _placed;
        private Image _testImage;
        private Coroutine _itemGlowRoutine;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private AssemblyRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public AssemblyScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite, WorkshopBuildKind kind,
            System.Func<PlayerProgress, int> getLevel, System.Action<PlayerProgress, int> setLevel,
            System.Func<PlayerProgress, System.Collections.Generic.List<bool>> getBuffer,
            string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _kind = kind;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _slotField = CreateFullRectContainer("SlotField");
            _pieceField = CreateFullRectContainer("PieceField");
            _testField = CreateFullRectContainer("TestField");
            BuildSlots();
            BuildPieces();
            BuildTestImage();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(_selfId);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopItemGlow();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = WorkshopAssemblyRoundGenerator.Create(_kind, _getLevel(_game.Progress), _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _testImage.gameObject.SetActive(false);

            ShowRoundPieces(_round);
            SetPiecesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.IntroVoiceKey);
            _eva.SetTalking(false);
            SetPiecesInteractable(true);
        }

        // --- Slots and shelf pieces --------------------------------------------------------------------------

        private void BuildSlots()
        {
            var positions = WorkshopAssemblyRoundGenerator.SlotPositions();
            _slotImages = new Image[SlotCount];
            for (var i = 0; i < SlotCount; i++)
            {
                var go = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_slotField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(positions[i].X, positions[i].Y);
                rect.sizeDelta = new Vector2(WorkshopAssemblyRoundGenerator.SlotSize, WorkshopAssemblyRoundGenerator.SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("workshop/slot_outline");
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0.35f);
                _slotImages[i] = image;
            }
        }

        private void BuildPieces()
        {
            _pieceItems = new DragItem[MaxItems];
            _pieceImages = new Image[MaxItems];
            for (var i = 0; i < MaxItems; i++)
            {
                var index = i;
                var item = DragItem.Create(_pieceField, "item" + i, EvaUi.Sprite("workshop/part_car_body"), Vector2.zero, WorkshopAssemblyRoundGenerator.SlotSize);
                item.BeginDrag += _ => OnPieceBeginDrag(index);
                item.EndDrag += _ => OnPieceEndDrag(index);
                _pieceItems[i] = item;
                _pieceImages[i] = item.GetComponent<Image>();
            }
        }

        private void BuildTestImage()
        {
            var go = new GameObject("TestSprite", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_testField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, SlotYCenter());
            rect.sizeDelta = new Vector2(420f, 420f);
            _testImage = go.GetComponent<Image>();
            _testImage.preserveAspect = true;
            _testImage.raycastTarget = false;
            _testImage.gameObject.SetActive(false);
        }

        private static float SlotYCenter() => 40f; // matches WorkshopAssemblyRoundGenerator's own SlotY

        private void ShowRoundPieces(AssemblyRound round)
        {
            StopItemGlow();
            _placed = new bool[MaxItems];
            _slotFilled = new bool[SlotCount];
            for (var i = 0; i < MaxItems; i++)
            {
                var active = i < round.ShelfItems.Length;
                _pieceItems[i].gameObject.SetActive(active);
                if (!active) continue;
                var piece = round.ShelfItems[i];
                _pieceItems[i].Rect.anchoredPosition = new Vector2(piece.TrayPosition.X, piece.TrayPosition.Y);
                _pieceImages[i].sprite = EvaUi.Sprite("workshop/part_" + piece.PartId);
                _pieceImages[i].color = Color.white;
                _pieceItems[i].enabled = true;
            }
            foreach (var slotImage in _slotImages) slotImage.color = new Color(1f, 1f, 1f, 0.35f);
        }

        private void SetPiecesInteractable(bool interactable)
        {
            for (var i = 0; i < _round.ShelfItems.Length; i++)
                if (!_placed[i]) _pieceItems[i].enabled = interactable;
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnPieceBeginDrag(int i) { }

        private void OnPieceEndDrag(int i)
        {
            if (_round == null || _roundOver || i >= _round.ShelfItems.Length || _placed[i]) return;
            var item = _round.ShelfItems[i];
            var current = _pieceItems[i].Rect.anchoredPosition;

            var nearestSlot = NearestUnfilledSlotWithinRadius(current);
            if (nearestSlot == null) { SnapBackToTray(i); return; } // no placement attempt - just a shelf rearrange

            if (item.Correct && _round.Slots[nearestSlot.Value].PartId == item.PartId) PlacePiece(i, nearestSlot.Value);
            else
            {
                HandleMistake();
                SnapBackToTray(i);
            }
        }

        private int? NearestUnfilledSlotWithinRadius(Vector2 position)
        {
            var radius2 = WorkshopAssemblyRoundGenerator.SnapRadius * WorkshopAssemblyRoundGenerator.SnapRadius;
            int? nearest = null;
            var nearestDist2 = float.MaxValue;
            for (var i = 0; i < _round.Slots.Length; i++)
            {
                if (_slotFilled[i]) continue;
                var home = _round.Slots[i].Position;
                var dx = position.x - home.X;
                var dy = position.y - home.Y;
                var dist2 = dx * dx + dy * dy;
                if (dist2 <= radius2 && dist2 < nearestDist2) { nearest = i; nearestDist2 = dist2; }
            }
            return nearest;
        }

        private void SnapBackToTray(int i)
        {
            var tray = _round.ShelfItems[i].TrayPosition;
            _runner.StartCoroutine(SlideTo(_pieceItems[i].Rect, new Vector2(tray.X, tray.Y), SnapBackSeconds));
        }

        private void PlacePiece(int i, int slotIndex)
        {
            _placed[i] = true;
            _slotFilled[slotIndex] = true;
            var home = _round.Slots[slotIndex].Position;
            _pieceItems[i].Rect.anchoredPosition = new Vector2(home.X, home.Y);
            _pieceItems[i].enabled = false;
            _slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0f);
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_pieceItems[i].Rect, _pieceImages[i], 1.15f, 0.25f));

            if (AllSlotsFilled()) _runner.StartCoroutine(RunTest());
        }

        private bool AllSlotsFilled()
        {
            foreach (var filled in _slotFilled) if (!filled) return false;
            return true;
        }

        // The next unplaced correct part - the Hint/Demo target, same "next correct item" wording as Dress for
        // the Occasion.
        private int NextCorrectUnplacedIndex()
        {
            for (var i = 0; i < _round.ShelfItems.Length; i++)
                if (_round.ShelfItems[i].Correct && !_placed[i]) return i;
            return -1;
        }

        private int SlotIndexFor(string partId)
        {
            for (var i = 0; i < _round.Slots.Length; i++)
                if (_round.Slots[i].PartId == partId && !_slotFilled[i]) return i;
            return -1;
        }

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the correct next part glows on the shelf.
        private IEnumerator RunHint()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);
            var lead = _game.Voice.Duration(_hintVoiceKey) + Voice.BreathSeconds;

            var index = NextCorrectUnplacedIndex();
            if (index >= 0) _itemGlowRoutine = _runner.StartCoroutine(GlowItem(index, Mathf.Max(lead, HintGlowSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            StopItemGlow();
            SetPiecesInteractable(true);
        }

        private IEnumerator GlowItem(int index, float duration)
        {
            var image = _pieceImages[index];
            var original = image.color;
            var glow = new Color(1f, 0.9f, 0.4f, 1f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        private void StopItemGlow()
        {
            if (_itemGlowRoutine != null)
            {
                _runner.StopCoroutine(_itemGlowRoutine);
                _itemGlowRoutine = null;
            }
        }

        // 3rd mistake: the hand places the correct next part into its own slot, then hands control back for
        // the child to finish the rest (per the plan's own Demo note) - never the whole build at once, since
        // assembling is cumulative.
        private IEnumerator RunDemonstrate()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var index = NextCorrectUnplacedIndex();
            var slotIndex = index >= 0 ? SlotIndexFor(_round.ShelfItems[index].PartId) : -1;
            if (index >= 0 && slotIndex >= 0)
            {
                var from = _pieceItems[index].Rect.anchoredPosition;
                var home = _round.Slots[slotIndex].Position;
                yield return _hand.MoveTo(from, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(new Vector2(home.X, home.Y), HandMoveSeconds);
                _pieceItems[index].Rect.anchoredPosition = new Vector2(home.X, home.Y);
                _hand.Hide();
                PlacePiece(index, slotIndex);
            }
            SetPiecesInteractable(true);
        }

        // --- TEST -> OBSERVE ---------------------------------------------------------------------------------

        // Every slot is filled: the assembled build pops, its own test sprite/voice line plays (the car
        // drives, the rocket launches, the ball rolls to the target), then the usual reward flow runs - the
        // one thing this presenter adds beyond Dress for the Occasion's shape.
        private IEnumerator RunTest()
        {
            _hand.Hide();
            StopItemGlow();
            _testImage.sprite = EvaUi.Sprite(_round.TestSprite);
            _testImage.gameObject.SetActive(true);
            _testImage.color = Color.white;
            _testImage.rectTransform.localScale = Vector3.one;
            yield return PopPulse(_testImage.rectTransform, _testImage, TestPopScale, TestPopSeconds);
            yield return _game.Voice.SayAndWait(_round.TestVoiceKey);
            yield return OnBuildComplete();
        }

        private IEnumerator OnBuildComplete()
        {
            _roundOver = true;

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            var progress = _game.Progress;
            _setLevel(progress, DifficultyLadder.RecordRound(_getBuffer(progress), _getLevel(progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), Vector2.zero));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= WorkshopAssemblyRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        private IEnumerator EndSession()
        {
            yield return _game.Voice.SayAndWait("count_done");
            _eva.Cheer();
            SetSessionEnded(true);
            yield return _game.Voice.SayAndWait("count_again");
        }

        // --- End of session -----------------------------------------------------------------------------

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, GoHomeAfterSession);

            _endPanel.SetActive(false);
        }

        private void GoHomeAfterSession() => _game.Navigator.Show(_homeScreenId);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _slotField.gameObject.SetActive(!ended);
            _pieceField.gameObject.SetActive(!ended);
            _testField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            var anchor = new GameObject("EvaAnchor", typeof(RectTransform));
            anchor.transform.SetParent(Root, false);
            var anchorRect = (RectTransform)anchor.transform;
            anchorRect.anchorMin = anchorRect.anchorMax = anchorRect.pivot = new Vector2(0.5f, 0.5f);
            anchorRect.anchoredPosition = EvaPosition;
            anchorRect.sizeDelta = Vector2.zero;

            _eva = RigFactory.CreateEva(anchorRect, EvaHeight);
            var scale = _eva.Root.localScale;
            _eva.Root.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens (identical shapes to DressForOccasionScreen's - see there for the reasoning) --------

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
            var original = tint != null ? tint.color : Color.white;
            var glow = Color.Lerp(original, Color.white, 0.6f);
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, k);
                if (tint != null) tint.color = Color.Lerp(original, glow, k);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var k = t / half;
                target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, k);
                if (tint != null) tint.color = Color.Lerp(glow, original, k);
                yield return null;
            }
            target.localScale = Vector3.one;
            if (tint != null) tint.color = original;
        }

        private static IEnumerator BigCheer(RectTransform target, Vector3 baseScale)
        {
            const float peak = 1.18f, duration = 0.4f, half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(1f, peak, t / half);
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                target.localScale = baseScale * Mathf.Lerp(peak, 1f, t / half);
                yield return null;
            }
            target.localScale = baseScale;
        }

        // --- Helpers ----------------------------------------------------------------------------------------

        private RectTransform CreateFullRectContainer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
