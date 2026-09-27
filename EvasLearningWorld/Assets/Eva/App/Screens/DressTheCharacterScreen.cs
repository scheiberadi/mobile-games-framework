using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Dress the Character (Store, spec 4.3 dressing cluster): the cluster's first game, reusing DragItem's/
    // Jigsaw's own "snap when close to its own correct region" mechanic unchanged (read JigsawScreen first).
    // Unlike Jigsaw, every round is the same fixed shape - exactly 4 pieces, one per ClothingSlot, laid out in
    // two fixed rows of 4 (slots above, tray below) rather than a growing grid - so this screen has no
    // per-round active/inactive bookkeeping beyond which item sprite each slot shows. Hint glows the next empty
    // slot; Demo drags one piece home, then hands control back for the child to finish the rest (same
    // "demonstrate one, not the whole thing" shape as Jigsaw/Collect Everything, since placing is cumulative
    // here too).
    public sealed class DressTheCharacterScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float EvaHeight = 430f;
        private static readonly Vector2 EvaPosition = new Vector2(627f, -140f);

        private const float HintGlowSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private static readonly ClothingSlot[] SlotOrder = { ClothingSlot.Head, ClothingSlot.Top, ClothingSlot.Bottom, ClothingSlot.Feet };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _slotField, _pieceField;
        private Image[] _slotImages;
        private DragItem[] _pieceItems;
        private Image[] _pieceImages;
        private bool[] _placed;
        private int _lastTouchedIndex = -1;
        private Coroutine _slotGlowRoutine;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private DressTheCharacterRound _round;
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
            _game.TutorialGuide.Refresh(ScreenId.DressTheCharacter);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopSlotGlow();
            _lastTouchedIndex = -1;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = DressTheCharacterRoundGenerator.Create(_game.Progress.DressTheCharacterLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _lastTouchedIndex = -1;

            ShowRoundPieces(_round);
            SetPiecesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("dressthecharacter_find");
            _eva.SetTalking(false);
            SetPiecesInteractable(true);
        }

        // --- Slots and pieces -------------------------------------------------------------------------------

        private void BuildSlots()
        {
            _slotImages = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var slot = SlotOrder[i];
                var home = DressTheCharacterRoundGenerator.HomePositionFor(slot);
                var go = new GameObject("Slot" + slot, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_slotField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(home.X, home.Y);
                rect.sizeDelta = new Vector2(DressTheCharacterRoundGenerator.SlotSize, DressTheCharacterRoundGenerator.SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("dressup/slot_" + slot.ToString().ToLowerInvariant());
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0.35f);
                _slotImages[i] = image;
            }
        }

        private void BuildPieces()
        {
            _pieceItems = new DragItem[4];
            _pieceImages = new Image[4];
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var size = DressTheCharacterRoundGenerator.SlotSize;
                var item = DragItem.Create(_pieceField, "piece" + i, EvaUi.Sprite("dressup/cap"), Vector2.zero, size);
                item.BeginDrag += _ => OnPieceBeginDrag(index);
                item.EndDrag += _ => OnPieceEndDrag(index);
                _pieceItems[i] = item;
                _pieceImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRoundPieces(DressTheCharacterRound round)
        {
            StopSlotGlow();
            _placed = new bool[4];
            for (var i = 0; i < 4; i++)
            {
                var piece = round.Pieces[i];
                var slotIndex = System.Array.IndexOf(SlotOrder, piece.Slot);
                _slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0.35f);

                _pieceItems[i].Rect.anchoredPosition = new Vector2(piece.TrayPosition.X, piece.TrayPosition.Y);
                _pieceImages[i].sprite = EvaUi.Sprite("dressup/" + piece.ItemId);
                _pieceImages[i].color = Color.white;
                _pieceItems[i].enabled = true;
            }
        }

        private void SetPiecesInteractable(bool interactable)
        {
            for (var i = 0; i < 4; i++)
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
            var home = _round.Pieces[i].HomePosition;
            var current = _pieceItems[i].Rect.anchoredPosition;
            var dx = current.x - home.X;
            var dy = current.y - home.Y;
            if (dx * dx + dy * dy <= _round.SnapRadius * _round.SnapRadius) PlacePiece(i);
            else HandleMistake();
        }

        private void PlacePiece(int i)
        {
            _placed[i] = true;
            var home = _round.Pieces[i].HomePosition;
            _pieceItems[i].Rect.anchoredPosition = new Vector2(home.X, home.Y);
            _pieceItems[i].enabled = false;
            var slotIndex = System.Array.IndexOf(SlotOrder, _round.Pieces[i].Slot);
            _slotImages[slotIndex].color = new Color(1f, 1f, 1f, 0f);
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_pieceItems[i].Rect, _pieceImages[i], 1.15f, 0.25f));

            if (AllPlaced()) _runner.StartCoroutine(OnOutfitComplete());
        }

        private bool AllPlaced()
        {
            for (var i = 0; i < 4; i++) if (!_placed[i]) return false;
            return true;
        }

        // The currently-held (or, once released, most recently touched) piece if it's still unplaced;
        // otherwise the first unplaced piece in slot order - same shape as Jigsaw's TargetIndexForHelp.
        private int TargetIndexForHelp()
        {
            if (_lastTouchedIndex >= 0 && _lastTouchedIndex < 4 && !_placed[_lastTouchedIndex]) return _lastTouchedIndex;
            for (var i = 0; i < 4; i++) if (!_placed[i]) return i;
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

        // 2nd mistake: the next empty slot glows (per the plan's own Hint note for this game).
        private IEnumerator RunHint()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("dressthecharacter_hint");
            var lead = _game.Voice.Duration("dressthecharacter_hint") + Voice.BreathSeconds;

            var index = TargetIndexForHelp();
            if (index >= 0)
            {
                var slotIndex = System.Array.IndexOf(SlotOrder, _round.Pieces[index].Slot);
                _slotGlowRoutine = _runner.StartCoroutine(GlowSlot(slotIndex, Mathf.Max(lead, HintGlowSeconds)));
            }
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            StopSlotGlow();
            SetPiecesInteractable(true);
        }

        private IEnumerator GlowSlot(int index, float duration)
        {
            var image = _slotImages[index];
            var original = image.color;
            var glow = new Color(1f, 0.9f, 0.4f, 0.85f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        private void StopSlotGlow()
        {
            if (_slotGlowRoutine != null)
            {
                _runner.StopCoroutine(_slotGlowRoutine);
                _slotGlowRoutine = null;
            }
        }

        // 3rd mistake: the hand drags one piece home, then hands control back for the child to finish the rest
        // (per the plan's own Demo note) - never the whole outfit, since placing is cumulative.
        private IEnumerator RunDemonstrate()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("dressthecharacter_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("dressthecharacter_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var index = TargetIndexForHelp();
            if (index >= 0)
            {
                var from = _pieceItems[index].Rect.anchoredPosition;
                var home = _round.Pieces[index].HomePosition;
                yield return _hand.MoveTo(from, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(new Vector2(home.X, home.Y), HandMoveSeconds);
                _pieceItems[index].Rect.anchoredPosition = new Vector2(home.X, home.Y);
                _hand.Hide();
                PlacePiece(index);
            }
            SetPiecesInteractable(true);
        }

        private IEnumerator OnOutfitComplete()
        {
            _roundOver = true;
            StopSlotGlow();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.DressTheCharacterLevel = DifficultyLadder.RecordRound(_game.Progress.DressTheCharacterBuffer, _game.Progress.DressTheCharacterLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), Vector2.zero));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= DressTheCharacterRoundGenerator.RoundsPerSession) yield return EndSession();
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

        // --- Small tweens (identical shapes to JigsawScreen's - see there for the reasoning behind each) -------

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
