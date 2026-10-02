using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's fourteenth and last game (spec 4.1: Tangram / Puzzle Blocks). Wired into Activities.cs.
    //
    // DRAG & DROP over a ghost silhouette, distinct from Jigsaw's photo reassembly - reuses DragItem plus
    // Jigsaw's own snap-to-region logic (JigsawScreen is this screen's closest sibling; read that one first)
    // with shape pieces of varied size instead of uniform cut-photo tiles. Hint glows the correct spot for the
    // currently-held/nearest piece; Demo places one shape then hands control back for the child to finish the
    // rest, the same "demonstrate one, not the whole thing" shape Jigsaw's and Collect Everything's Demo already
    // established. Real tangram geometry (actual triangle/parallelogram shapes with rotation-aware fitting) is a
    // separate content/design pass - see Rules/Tangram.cs's own class comment - so this uses placeholder square
    // tiles of varied size, same as every other game before real art exists. Pieces are smaller than the usual
    // 240-unit tap floor at higher shape counts, the same situation Jigsaw hit first: this screen reuses the
    // same "PuzzlePieceField" container name so NoReadingAuditTests.IsPuzzlePieceSlot exempts it too.
    public sealed class TangramScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxPieces = 7;
        private const float PieceMargin = 6f;
        private const float HintGlowSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _boardField, _pieceField;
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
        private TangramRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _boardField = CreateFullRectContainer("BoardField");
            // Same container name JigsawScreen uses, so NoReadingAuditTests.IsPuzzlePieceSlot's exemption from
            // the usual 240-unit tap floor applies here too (see class comment).
            _pieceField = CreateFullRectContainer("PuzzlePieceField");
            BuildSlots();
            BuildPieces();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Tangram);
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
            _round = TangramRoundGenerator.Create(_game.Progress.TangramLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _lastTouchedIndex = -1;

            ShowRoundPieces(_round);
            SetPiecesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("tangram_find");
            _eva.SetTalking(false);
            SetPiecesInteractable(true);
        }

        // --- Board and pieces --------------------------------------------------------------------------------

        private void BuildSlots()
        {
            _slotImages = new Image[MaxPieces];
            for (var i = 0; i < MaxPieces; i++)
            {
                var go = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_boardField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("tangram/ghost_shape");
                image.raycastTarget = false;
                _slotImages[i] = image;
            }
        }

        private void BuildPieces()
        {
            _pieceItems = new DragItem[MaxPieces];
            _pieceImages = new Image[MaxPieces];
            for (var i = 0; i < MaxPieces; i++)
            {
                var index = i;
                var item = DragItem.Create(_pieceField, "shape" + i, EvaUi.Sprite("tangram/shape_0"), Vector2.zero, 100f);
                item.BeginDrag += _ => OnPieceBeginDrag(index);
                item.EndDrag += _ => OnPieceEndDrag(index);
                _pieceItems[i] = item;
                _pieceImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRoundPieces(TangramRound round)
        {
            StopSlotGlow();
            var count = round.Pieces.Length;
            _placed = new bool[MaxPieces];

            for (var i = 0; i < MaxPieces; i++)
            {
                var active = i < count;
                _slotImages[i].gameObject.SetActive(active);
                _pieceItems[i].gameObject.SetActive(active);
                if (!active) continue;

                var piece = round.Pieces[i];
                var size = new Vector2(
                    (round.BasePieceWidth - PieceMargin) * piece.SizeScale,
                    (round.BasePieceHeight - PieceMargin) * piece.SizeScale);

                var slotRect = (RectTransform)_slotImages[i].transform;
                slotRect.anchoredPosition = new Vector2(piece.HomePosition.X, piece.HomePosition.Y);
                slotRect.sizeDelta = size;
                _slotImages[i].color = new Color(1f, 1f, 1f, 0.35f);

                _pieceItems[i].Rect.anchoredPosition = new Vector2(piece.TrayPosition.X, piece.TrayPosition.Y);
                _pieceItems[i].Rect.sizeDelta = size;
                _pieceImages[i].sprite = EvaUi.Sprite(piece.SpriteKey);
                _pieceImages[i].color = Color.white;
                _pieceItems[i].enabled = true;
            }
        }

        private void SetPiecesInteractable(bool interactable)
        {
            for (var i = 0; i < _round.Pieces.Length; i++)
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
            _slotImages[i].color = new Color(1f, 1f, 1f, 0f);
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_pieceItems[i].Rect, _pieceImages[i], 1.15f, 0.25f));

            if (AllPlaced()) _runner.StartCoroutine(OnPuzzleComplete());
        }

        private bool AllPlaced()
        {
            for (var i = 0; i < _round.Pieces.Length; i++) if (!_placed[i]) return false;
            return true;
        }

        private int TargetIndexForHelp()
        {
            if (_lastTouchedIndex >= 0 && _lastTouchedIndex < _round.Pieces.Length && !_placed[_lastTouchedIndex]) return _lastTouchedIndex;
            for (var i = 0; i < _round.Pieces.Length; i++) if (!_placed[i]) return i;
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
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the correct spot for the currently-held/nearest piece glows (per the plan's Hint note).
        private IEnumerator RunHint()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("tangram_hint");
            var lead = _game.Voice.Duration("tangram_hint") + Voice.BreathSeconds;

            var index = TargetIndexForHelp();
            if (index >= 0) _slotGlowRoutine = _runner.StartCoroutine(GlowSlot(index, Mathf.Max(lead, HintGlowSeconds)));
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

        // 3rd mistake: the hand places one shape, then hands control back for the child to finish the rest
        // (per the plan's Demo note) - never the whole silhouette, since placement is cumulative.
        private IEnumerator RunDemonstrate()
        {
            SetPiecesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("tangram_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("tangram_demo") + Voice.BreathSeconds);
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

        private IEnumerator OnPuzzleComplete()
        {
            _roundOver = true;
            StopSlotGlow();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.TangramLevel = DifficultyLadder.RecordRound(_game.Progress.TangramBuffer, _game.Progress.TangramLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), new Vector2(TangramRoundGenerator.BoardCenter.X, TangramRoundGenerator.BoardCenter.Y)));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= TangramRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _boardField.gameObject.SetActive(!ended);
            _pieceField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens (identical shapes to NumberHuntScreen's - see there for the reasoning behind each) ---

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

        private void AddPlaygroundBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/playground_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
