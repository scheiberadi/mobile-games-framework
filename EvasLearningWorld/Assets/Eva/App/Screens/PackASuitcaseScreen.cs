using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Pack a Suitcase (Store, dressing cluster's 3rd and last game): reuses Dress for the Occasion's own
    // shelf/distractor shape and DragItem snap mechanic (read that screen first), but the target is a single
    // suitcase with 3 interchangeable slots rather than 3 body-part slots each wanting a specific item - any
    // correct item may land in any unfilled slot, first-come-first-served, since packing has no "which slot"
    // identity to match. A distractor dragged into the suitcase is this game's mistake, same as a wrong-slot
    // drag in Dress for the Occasion; the piece floats back to the shelf rather than sitting wherever it was
    // dropped. Hint glows the correct next item on the shelf; Demo drags it into the next open slot.
    public sealed class PackASuitcaseScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }


        private const int MaxItems = 5;
        private const int SlotCount = 3;

        private const float HintGlowSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float SnapBackSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _slotField, _pieceField;
        private Image[] _slotImages;
        private bool[] _slotFilled;
        private DragItem[] _pieceItems;
        private Image[] _pieceImages;
        private bool[] _placed;
        private int _lastTouchedIndex = -1;
        private Coroutine _itemGlowRoutine;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private PackASuitcaseRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddStoreBackground();
            BuildEva();
            _slotField = CreateFullRectContainer("SlotField");
            _pieceField = CreateFullRectContainer("PieceField");
            BuildSlots();
            BuildPieces();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.PackASuitcase);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopItemGlow();
            _lastTouchedIndex = -1;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = PackASuitcaseRoundGenerator.Create(_game.Progress.PackASuitcaseLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _lastTouchedIndex = -1;

            ShowRoundPieces(_round);
            SetPiecesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("trip_" + _round.Trip.ToString().ToLowerInvariant());
            _eva.SetTalking(false);
            SetPiecesInteractable(true);
        }

        // --- Suitcase slots and shelf pieces --------------------------------------------------------------

        private void BuildSlots()
        {
            var positions = PackASuitcaseRoundGenerator.SlotPositions();
            _slotImages = new Image[SlotCount];
            for (var i = 0; i < SlotCount; i++)
            {
                var go = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_slotField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(positions[i].X, positions[i].Y);
                rect.sizeDelta = new Vector2(PackASuitcaseRoundGenerator.SlotSize, PackASuitcaseRoundGenerator.SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("dressup/suitcase_slot");
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
                var item = DragItem.Create(_pieceField, "item" + i, EvaUi.Sprite("dressup/backpack"), Vector2.zero, PackASuitcaseRoundGenerator.SlotSize);
                item.BeginDrag += _ => OnPieceBeginDrag(index);
                item.EndDrag += _ => OnPieceEndDrag(index);
                _pieceItems[i] = item;
                _pieceImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRoundPieces(PackASuitcaseRound round)
        {
            StopItemGlow();
            _placed = new bool[MaxItems];
            _slotFilled = new bool[SlotCount];
            for (var i = 0; i < MaxItems; i++)
            {
                var piece = round.ShelfItems[i];
                _pieceItems[i].Rect.anchoredPosition = new Vector2(piece.TrayPosition.X, piece.TrayPosition.Y);
                _pieceImages[i].sprite = EvaUi.Sprite("dressup/" + piece.ItemId);
                _pieceImages[i].color = Color.white;
                _pieceItems[i].enabled = true;
            }
            foreach (var slotImage in _slotImages) slotImage.color = new Color(1f, 1f, 1f, 0.35f);
        }

        private void SetPiecesInteractable(bool interactable)
        {
            for (var i = 0; i < MaxItems; i++)
                if (!_placed[i]) _pieceItems[i].enabled = interactable;
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnPieceBeginDrag(int i)
        {
            if (_round == null || _roundOver) return;
            _lastTouchedIndex = i;
        }

        private void OnPieceEndDrag(int i)
        {
            if (_round == null || _roundOver || _placed[i]) return;
            var item = _round.ShelfItems[i];
            var current = _pieceItems[i].Rect.anchoredPosition;

            var nearestSlot = NearestUnfilledSlotWithinRadius(current);
            if (nearestSlot == null) { SnapBackToTray(i); return; } // no packing attempt - just a shelf rearrange

            if (item.Correct) PlacePiece(i, nearestSlot.Value);
            else
            {
                HandleMistake();
                SnapBackToTray(i);
            }
        }

        private int? NearestUnfilledSlotWithinRadius(Vector2 position)
        {
            var radius2 = PackASuitcaseRoundGenerator.SnapRadius * PackASuitcaseRoundGenerator.SnapRadius;
            int? nearest = null;
            var nearestDist2 = float.MaxValue;
            for (var i = 0; i < SlotCount; i++)
            {
                if (_slotFilled[i]) continue;
                var home = _round.SlotPositions[i];
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
            var home = _round.SlotPositions[slotIndex];
            _pieceItems[i].Rect.anchoredPosition = new Vector2(home.X, home.Y);
            _pieceItems[i].enabled = false;
            _slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0f);
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_pieceItems[i].Rect, _pieceImages[i], 1.15f, 0.25f));

            if (AllSlotsFilled()) _runner.StartCoroutine(OnSuitcaseComplete());
        }

        private bool AllSlotsFilled()
        {
            foreach (var filled in _slotFilled) if (!filled) return false;
            return true;
        }

        // The next unplaced correct item - the Hint/Demo target, same "next correct item" wording as Dress for
        // the Occasion.
        private int NextCorrectUnplacedIndex()
        {
            for (var i = 0; i < MaxItems; i++)
                if (_round.ShelfItems[i].Correct && !_placed[i]) return i;
            return -1;
        }

        private int NextOpenSlot()
        {
            for (var i = 0; i < SlotCount; i++) if (!_slotFilled[i]) return i;
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

        // 2nd mistake: the correct next item glows on the shelf (per the plan's own Hint note for this game).
        private IEnumerator RunHint()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("packasuitcase_hint");
            var lead = _game.Voice.Duration("packasuitcase_hint") + Voice.BreathSeconds;

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

        // 3rd mistake: the hand packs the correct next item into the next open slot, then hands control back
        // for the child to finish the rest (per the plan's own Demo note) - never the whole suitcase, since
        // packing is cumulative.
        private IEnumerator RunDemonstrate()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("packasuitcase_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("packasuitcase_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var index = NextCorrectUnplacedIndex();
            var slotIndex = NextOpenSlot();
            if (index >= 0 && slotIndex >= 0)
            {
                var from = _pieceItems[index].Rect.anchoredPosition;
                var home = _round.SlotPositions[slotIndex];
                yield return _hand.MoveTo(from, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(new Vector2(home.X, home.Y), HandMoveSeconds);
                _pieceItems[index].Rect.anchoredPosition = new Vector2(home.X, home.Y);
                _hand.Hide();
                PlacePiece(index, slotIndex);
            }
            SetPiecesInteractable(true);
        }

        private IEnumerator OnSuitcaseComplete()
        {
            _roundOver = true;
            StopItemGlow();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.PackASuitcaseLevel = DifficultyLadder.RecordRound(_game.Progress.PackASuitcaseBuffer, _game.Progress.PackASuitcaseLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), Vector2.zero));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= PackASuitcaseRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.StoreActivities);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _slotField.gameObject.SetActive(!ended);
            _pieceField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Side);
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

        private void AddStoreBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/store_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
