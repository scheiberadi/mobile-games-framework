using System;
using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's first game (spec 4.1, build order: Pattern Completion first - defines the building's cheapest,
    // most Number-Hunt-like shape). Eva shows a repeating sequence of shapes with one blank at the end; the child
    // DRAGS the shape that continues it into the blank (answer-variety step 5; was a tap on the choice tile). The blank
    // grows and lights up while a shape is held near it, which does not say whether it is the right one. Right shape:
    // it settles into the blank. Wrong shape: the blank shakes, the shape springs back, a mistake on the help ladder
    // (hint = the hand carries the right shape to the blank and back, demonstration = it drops it in). Dropped away
    // from the blank: it springs back, no mistake. See PatternCompletionRoundGenerator (Rules/PatternCompletion.cs).
    public sealed class PatternCompletionScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // The sequence row (decorative - no TapTarget, the audit exempts nothing tappable) sits above the choice
        // tiles, both centred left of Eva (same region NumberHuntScreen's answer grid uses). Both rows keep their
        // whole height at y <= 165, clearing the Hud's Home/coin zones (y in [175,415], see NumberHuntScreen's own
        // layout comment) regardless of how wide the sequence row gets at high levels.
        private const int MaxSequenceTiles = PatternCompletionRoundGenerator.MaxShownLength + 1; // + the blank tile
        private const float SequenceTileSize = 90f;
        private const float SequenceGap = 15f;
        private const float SequenceCenterX = -200f;
        private const float SequenceY = 120f;

        private const int MaxChoiceTiles = 4;
        private const float ChoiceTileSize = 240f; // EvaUi.MinTap
        private const float ChoiceCenterX = -200f;
        private const float ChoiceY = -150f;
        private static readonly float[] ChoiceOffsets4 = { -390f, -130f, 130f, 390f };
        private static readonly float[] ChoiceOffsets3 = { -280f, 0f, 280f };
        private const float WobbleSeconds = 0.4f;
        private const float SnapRadius = 150f; // inclusive, around the blank tile centre
        private const float HoverScale = 1.3f;
        private const float SnapBackSeconds = 0.25f;
        private const float HandCarrySeconds = 1.2f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _sequenceField, _choiceField;
        private GameObject _endPanel;

        private RectTransform[] _sequenceTiles;
        private Image[] _sequenceImages;

        private RectTransform[] _choiceTiles;
        private Image[] _choiceImages;
        private DragItem[] _choiceItems;
        private Vector2[] _choiceHome;
        private int _placedChoice = -1;
        private bool _helpRunning;
        private Coroutine _hoverRoutine;
        private Image _blankRing;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private PatternCompletionRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _sequenceField = CreateFullRectContainer("SequenceField");
            _choiceField = CreateFullRectContainer("ChoiceField");
            BuildSequenceTiles();
            BuildChoiceTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.PatternCompletion);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = PatternCompletionRoundGenerator.Create(_game.Progress.PatternCompletionLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _helpRunning = false;

            ShowRoundSequence(_round);
            ShowRoundChoices(_round);
            SetChoicesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("pattern_find");
            _eva.SetTalking(false);
            SetChoicesInteractable(true);
        }

        // --- Sequence display (decorative, never tappable) --------------------------------------------------

        private void BuildSequenceTiles()
        {
            _sequenceTiles = new RectTransform[MaxSequenceTiles];
            _sequenceImages = new Image[MaxSequenceTiles];
            for (var i = 0; i < MaxSequenceTiles; i++)
            {
                var tile = new GameObject("SeqTile" + i, typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(_sequenceField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(SequenceTileSize, SequenceTileSize);

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;

                _sequenceTiles[i] = rect;
                _sequenceImages[i] = image;
            }

            // The ring that lights up around the blank while a shape is held close enough to drop into it.
            var ringObject = new GameObject("BlankRing", typeof(RectTransform), typeof(Image));
            ringObject.transform.SetParent(_sequenceField, false);
            var ringRect = (RectTransform)ringObject.transform;
            ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(SequenceTileSize * 1.5f, SequenceTileSize * 1.5f);
            _blankRing = ringObject.GetComponent<Image>();
            _blankRing.sprite = EvaUi.Sprite("icons/ring_thin");
            _blankRing.preserveAspect = true;
            _blankRing.raycastTarget = false;
            ringObject.SetActive(false);
        }

        private void ShowRoundSequence(PatternCompletionRound round)
        {
            var count = round.Sequence.Length + 1; // + the blank tile at the end
            var totalWidth = count * SequenceTileSize + (count - 1) * SequenceGap;
            var startX = SequenceCenterX - totalWidth / 2f + SequenceTileSize / 2f;
            for (var i = 0; i < MaxSequenceTiles; i++)
            {
                _sequenceTiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _sequenceTiles[i].anchoredPosition = new Vector2(startX + i * (SequenceTileSize + SequenceGap), SequenceY);
                var isBlank = i == count - 1;
                _sequenceImages[i].sprite = EvaUi.Sprite(isBlank ? "icons/blank_tile" : "pattern/shape_" + round.Sequence[i].ToLowerInvariant());
            }
        }

        // --- Choice tiles (dragged into the blank) ---------------------------------------------------------------

        private void BuildChoiceTiles()
        {
            _choiceTiles = new RectTransform[MaxChoiceTiles];
            _choiceImages = new Image[MaxChoiceTiles];
            _choiceItems = new DragItem[MaxChoiceTiles];
            _choiceHome = new Vector2[MaxChoiceTiles];

            for (var i = 0; i < MaxChoiceTiles; i++)
            {
                var index = i;
                var item = DragItem.Create(_choiceField, "Choice" + i, EvaUi.Sprite("icons/dot"), Vector2.zero, ChoiceTileSize);
                item.BeginDrag += _ => OnChoiceBeginDrag();
                item.EndDrag += _ => OnChoiceEndDrag(index);
                _choiceItems[i] = item;
                _choiceTiles[i] = item.Rect;
                _choiceImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRoundChoices(PatternCompletionRound round)
        {
            StopHover();
            _placedChoice = -1;
            var count = round.Choices.Length;
            var offsets = count == 3 ? ChoiceOffsets3 : ChoiceOffsets4;
            for (var i = 0; i < MaxChoiceTiles; i++)
            {
                _choiceTiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _choiceHome[i] = new Vector2(ChoiceCenterX + offsets[i], ChoiceY);
                _choiceTiles[i].anchoredPosition = _choiceHome[i];
                _choiceTiles[i].sizeDelta = new Vector2(ChoiceTileSize, ChoiceTileSize);
                _choiceImages[i].sprite = EvaUi.Sprite("pattern/shape_" + round.Choices[i].ToLowerInvariant());
                _choiceImages[i].color = Color.white;
                _choiceImages[i].raycastTarget = true;
                _choiceTiles[i].localRotation = Quaternion.identity;
                _choiceTiles[i].localScale = Vector3.one;
                _choiceItems[i].enabled = false;
            }
        }

        private void SetChoicesInteractable(bool interactable)
        {
            for (var i = 0; i < _choiceItems.Length; i++)
                if (_choiceItems[i].gameObject.activeSelf && i != _placedChoice) _choiceItems[i].enabled = interactable;
        }

        // The blank is the last sequence tile; it grows and shows a ring while a choice is held within snap range.
        private RectTransform BlankTile => _sequenceTiles[_round.Sequence.Length];

        private void OnChoiceBeginDrag()
        {
            if (_round == null || _roundOver || _helpRunning) return;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop());
        }

        private IEnumerator HoverLoop()
        {
            while (true)
            {
                SetBlankHover(HeldChoiceIsNearBlank());
                yield return null;
            }
        }

        private bool HeldChoiceIsNearBlank()
        {
            for (var i = 0; i < _choiceItems.Length; i++)
            {
                if (!_choiceItems[i].gameObject.activeSelf || i == _placedChoice) continue;
                if (IsNearBlank(_choiceTiles[i].anchoredPosition)) return true;
            }
            return false;
        }

        private bool IsNearBlank(Vector2 position) => (position - BlankTile.anchoredPosition).sqrMagnitude <= SnapRadius * SnapRadius;

        private void SetBlankHover(bool hovered)
        {
            if (_round == null) return;
            BlankTile.localScale = Vector3.one * (hovered ? HoverScale : 1f);
            _blankRing.gameObject.SetActive(hovered);
            if (hovered) _blankRing.rectTransform.anchoredPosition = BlankTile.anchoredPosition;
        }

        private void StopHover()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            if (_blankRing != null) _blankRing.gameObject.SetActive(false);
            if (_round != null && _sequenceTiles != null && _round.Sequence.Length < _sequenceTiles.Length) BlankTile.localScale = Vector3.one;
        }

        private void OnChoiceEndDrag(int index)
        {
            StopHover();
            if (_round == null || _roundOver || _helpRunning) return;
            if (!IsNearBlank(_choiceTiles[index].anchoredPosition)) { SnapBack(index); return; } // away from the blank: not an attempt

            if (_round.Choices[index] == _round.Answer) _runner.StartCoroutine(OnCorrectChoice(index));
            else
            {
                _runner.StartCoroutine(Wobble(BlankTile, WobbleSeconds));
                SnapBack(index);
                HandleMistake();
            }
        }

        private void SnapBack(int index) =>
            _runner.StartCoroutine(SlideTo(_choiceTiles[index], _choiceHome[index], SnapBackSeconds));

        private void HandleMistake()
        {
            _eva.Angry();
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

        // 2nd mistake: the hand carries the correct tile to the blank and back; nothing is placed.
        private IEnumerator RunHint()
        {
            _helpRunning = true;
            SetChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("pattern_drag_hint");

            var correct = CorrectChoiceIndex();
            var blank = BlankTile.anchoredPosition;
            yield return Carry(correct, _choiceHome[correct], blank, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(correct, blank, _choiceHome[correct], HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            SetChoicesInteractable(true);
        }

        // 3rd mistake: the hand carries the correct tile into the blank for the child.
        private IEnumerator RunDemonstrate()
        {
            _helpRunning = true;
            SetChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("pattern_drag_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("pattern_drag_demo") * 0.3f);

            var correct = CorrectChoiceIndex();
            yield return Carry(correct, _choiceHome[correct], BlankTile.anchoredPosition, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            yield return OnCorrectChoice(correct);
        }

        // The hand glides to the tile, grabs it, then moves with it to `to`, fingertip on the tile centre.
        private IEnumerator Carry(int choice, Vector2 from, Vector2 to, float seconds)
        {
            var rect = _choiceTiles[choice];
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from; // any snap-back slide has finished by now
            rect.SetAsLastSibling();
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var position = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                rect.anchoredPosition = position;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(position);
                yield return null;
            }
            rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
        }

        private int CorrectChoiceIndex()
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == _round.Answer) return i;
            return -1;
        }

        private IEnumerator OnCorrectChoice(int i)
        {
            _roundOver = true;
            _placedChoice = i;
            SetChoicesInteractable(false);
            StopHover();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            // The tile settles into the blank: the blank takes the shape picture and the dragged copy disappears.
            var blankImage = _sequenceImages[_round.Sequence.Length];
            blankImage.sprite = _choiceImages[i].sprite;
            _choiceTiles[i].gameObject.SetActive(false);

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(BlankTile, blankImage, 1.4f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.PatternCompletionLevel = DifficultyLadder.RecordRound(_game.Progress.PatternCompletionBuffer, _game.Progress.PatternCompletionLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), BlankTile.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= PatternCompletionRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

        private static IEnumerator SlideTo(RectTransform target, Vector2 to, float seconds)
        {
            var from = target.anchoredPosition;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                target.anchoredPosition = Vector2.Lerp(from, to, PointerHand.EaseInOut(t / seconds));
                yield return null;
            }
            target.anchoredPosition = to;
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

        // Never part of the first-run tutorial, so Home always goes back to the Playground list it was opened from.
        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _sequenceField.gameObject.SetActive(!ended);
            _choiceField.gameObject.SetActive(!ended);
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

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                var angle = Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay;
                target.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            target.localRotation = Quaternion.identity;
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
