using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shared DRAG-TO-TARGET presenter (answer-variety Prototype A, docs/kids-games/answer-variety-prototypes.md).
    // Only Item to Shadow uses it so far. A round shows N targets (the shadows, not interactive) in a row and N
    // draggable items (the objects) below in a different order; the child drags each item onto the target it
    // belongs on. While an item is dragged, the nearest target within snap range grows and lights up - a neutral
    // "you are about to drop here" cue that does not say whether it is the right one. Release near the right
    // target: the item settles into it (the shadow is filled by the object) with a pop and a sound. Release near
    // a wrong target: that target shakes and the item springs back (a mistake on the help ladder). Release away
    // from every target: it just springs back, no mistake. A round ends when every item is placed.
    //
    // Reuses DragItem, PointerHand, HelpLadder/CoinPayout/DifficultyLadder and the end panel exactly as the tap
    // games do. The help ladder is per round: 1st mistake a retry, 2nd a hint (the hand carries the hint item
    // toward its target and back, placing nothing), 3rd a demonstration (the hand carries it and drops it for the
    // child); after that further mistakes are just retries. "Clean" means no demonstration fired that round.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): shadows (190, decorative, no TapTarget) in a row at y 70, clear
    // of the Hud's Home button (which reaches down to y 175); items (240, DragItem = TapTarget) in a row at y -190;
    // Eva at her usual place on the right. The item row uses the same 4/3-item x positions as the tap version.
    //
    // NOT compiled or run in the cloud container that wrote it: needs Unity compile, the layout audit and a
    // device look. See the prototype document's acceptance criteria.
    public sealed class DragToTargetScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxPairs = 4;
        private const float TargetSize = 190f;
        private const float ItemSize = EvaUi.MinTap; // 240
        private const float RowCenterX = -200f;
        private const float TargetY = 70f;
        private const float ItemY = -190f;
        private static readonly float[] Offsets4 = { -390f, -130f, 130f, 390f };
        private static readonly float[] Offsets3 = { -280f, 0f, 280f };

        // Inclusive snap range around a target's centre; smaller than half the 260-unit pitch so ranges barely overlap.
        private const float SnapRadius = 130f;
        private const float HoverScale = 1.15f;
        private const float SnapBackSeconds = 0.25f;
        private const float WobbleSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<int, System.Random, DragToTargetRound> _generateRound;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly int _roundsPerSession;
        private readonly string _spritePrefix, _targetSuffix;
        private readonly string _promptVoiceKey, _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _targetField, _itemField;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private RectTransform[] _targets;
        private Image[] _targetImages;
        private Image[] _targetRings;
        private DragItem[] _items;
        private Image[] _itemImages;
        private bool[] _placed;
        private Vector2[] _itemHome;
        private WorldPoint[] _targetCentres;
        // The centres a drop can hit: a copy of _targetCentres where an already-filled target is moved far away, so
        // dropping near a filled shadow counts as dropping in empty space (springs back, no mistake).
        private WorldPoint[] _hitCentres;
        private static readonly WorldPoint OutOfReach = new WorldPoint(100000f, 100000f);

        private System.Random _rng;
        private int _roundIndex;
        private DragToTargetRound _round;
        private HelpLadder _ladder;
        private bool _roundOver, _helpRunning, _demonstratedThisRound;
        private int _rightLineIndex;
        private int _hoverTarget = -1;
        private Coroutine _hoverRoutine;

        public DragToTargetScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<int, System.Random, DragToTargetRound> generateRound,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            int roundsPerSession, string spritePrefix, string targetSuffix,
            string promptVoiceKey, string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _generateRound = generateRound;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _roundsPerSession = roundsPerSession;
            _spritePrefix = spritePrefix;
            _targetSuffix = targetSuffix;
            _promptVoiceKey = promptVoiceKey;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        // The round on screen now (null before the first round). Read-only; exposed so tests can drive the screen.
        public DragToTargetRound CurrentRound => _round;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _targetField = CreateFullRectContainer("TargetField");
            _itemField = CreateFullRectContainer("ItemField"); // after the targets, so dragged items draw above them
            BuildTargets();
            BuildItems();
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
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = _generateRound(_getLevel(_game.Progress), _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _helpRunning = false;
            _demonstratedThisRound = false;
            ShowRound(_round);
            SetItemsInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_promptVoiceKey);
            _eva.SetTalking(false);
            SetItemsInteractable(true);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildTargets()
        {
            _targets = new RectTransform[MaxPairs];
            _targetImages = new Image[MaxPairs];
            _targetRings = new Image[MaxPairs];
            for (var i = 0; i < MaxPairs; i++)
            {
                var go = new GameObject("Target" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_targetField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TargetSize, TargetSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
                _targets[i] = rect;
                _targetImages[i] = image;

                // A glow ring behind the target, shown while an item hovers within snap range. It is a child so it
                // moves, scales and shakes with the target; the silhouettes are dark, so a tint alone would not show.
                var ringGo = new GameObject("HoverRing", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(go.transform, false);
                ringGo.transform.SetAsFirstSibling();
                var ringRect = (RectTransform)ringGo.transform;
                ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = new Vector2(0.5f, 0.5f);
                ringRect.anchoredPosition = Vector2.zero;
                ringRect.sizeDelta = new Vector2(TargetSize * 1.4f, TargetSize * 1.4f);
                var ring = ringGo.GetComponent<Image>();
                ring.sprite = EvaUi.Sprite("icons/ring_thin");
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ringGo.SetActive(false);
                _targetRings[i] = ring;
            }
        }

        private void BuildItems()
        {
            _items = new DragItem[MaxPairs];
            _itemImages = new Image[MaxPairs];
            _itemHome = new Vector2[MaxPairs];
            _placed = new bool[MaxPairs];
            for (var i = 0; i < MaxPairs; i++)
            {
                var index = i;
                var item = DragItem.Create(_itemField, "pair" + i, EvaUi.Sprite("icons/dot"), Vector2.zero, ItemSize);
                item.BeginDrag += _ => OnItemBeginDrag(index);
                item.EndDrag += _ => OnItemEndDrag(index);
                _items[i] = item;
                _itemImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRound(DragToTargetRound round)
        {
            StopHoverRoutine(); // every target is reset below, so no visual reset (and no read of the previous round) here
            var count = round.ItemKeys.Length;
            var offsets = count == 3 ? Offsets3 : Offsets4;
            _targetCentres = new WorldPoint[count];
            _hitCentres = new WorldPoint[count];
            for (var i = 0; i < MaxPairs; i++)
            {
                var active = i < count;
                _targets[i].gameObject.SetActive(active);
                _items[i].gameObject.SetActive(active);
                _placed[i] = false;
                if (!active) continue;

                var x = RowCenterX + offsets[i];
                _targets[i].anchoredPosition = new Vector2(x, TargetY);
                _targets[i].localScale = Vector3.one;
                _targets[i].localRotation = Quaternion.identity;
                _targetImages[i].sprite = EvaUi.Sprite(_spritePrefix + round.TargetKeys[i] + _targetSuffix);
                _targetImages[i].color = Color.white;
                _targetRings[i].gameObject.SetActive(false);
                _targetCentres[i] = new WorldPoint(x, TargetY);
                _hitCentres[i] = _targetCentres[i];

                _itemHome[i] = new Vector2(x, ItemY);
                _items[i].Rect.anchoredPosition = _itemHome[i];
                _items[i].Rect.sizeDelta = new Vector2(ItemSize, ItemSize);
                _items[i].Rect.localScale = Vector3.one;
                _itemImages[i].sprite = EvaUi.Sprite(_spritePrefix + round.ItemKeys[i]);
                _itemImages[i].color = Color.white;
                _itemImages[i].raycastTarget = true;
            }
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void SetItemsInteractable(bool interactable)
        {
            for (var i = 0; i < MaxPairs; i++)
                if (_items[i].gameObject.activeSelf && !_placed[i]) _items[i].enabled = interactable;
        }

        private void OnItemBeginDrag(int index)
        {
            if (_round == null || _roundOver || _helpRunning) return;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop(index));
        }

        // While an item is held, the nearest target in snap range grows; every other target rests.
        private IEnumerator HoverLoop(int index)
        {
            while (true)
            {
                var position = _items[index].Rect.anchoredPosition;
                var nearest = DropGeometry.NearestWithinRadius(position.x, position.y, _hitCentres, SnapRadius);
                if (nearest != _hoverTarget) SetHover(nearest);
                yield return null;
            }
        }

        private void SetHover(int target)
        {
            _hoverTarget = target;
            for (var i = 0; i < _targetCentres.Length; i++)
            {
                if (_placed[IndexOfItemForTarget(i)]) continue;
                var hovered = i == target;
                _targets[i].localScale = Vector3.one * (hovered ? HoverScale : 1f);
                _targetRings[i].gameObject.SetActive(hovered);
            }
        }

        private void StopHoverRoutine()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hoverTarget = -1;
        }

        private void StopHover()
        {
            StopHoverRoutine();
            if (_targets == null) return;
            for (var i = 0; i < MaxPairs; i++)
            {
                if (!_targets[i].gameObject.activeSelf) continue;
                if (_round != null && i < _targetCentres.Length && _placed[IndexOfItemForTarget(i)]) continue;
                _targets[i].localScale = Vector3.one;
                _targetRings[i].gameObject.SetActive(false);
            }
        }

        private void OnItemEndDrag(int index)
        {
            StopHover();
            if (_round == null || _roundOver || _helpRunning || _placed[index]) return;

            var position = _items[index].Rect.anchoredPosition;
            var target = DropGeometry.NearestWithinRadius(position.x, position.y, _hitCentres, SnapRadius);
            if (target < 0) { SnapBack(index); return; } // dropped in empty space: not an attempt, no mistake

            if (target == _round.TargetIndexOf(index)) PlaceItem(index);
            else
            {
                _runner.StartCoroutine(Wobble(_targets[target], WobbleSeconds));
                SnapBack(index);
                HandleMistake();
            }
        }

        private void SnapBack(int index) =>
            _runner.StartCoroutine(SlideTo(_items[index].Rect, _itemHome[index], SnapBackSeconds));

        private void PlaceItem(int index)
        {
            var target = _round.TargetIndexOf(index);
            _placed[index] = true;
            _items[index].enabled = false;
            _itemImages[index].raycastTarget = false;
            _items[index].Rect.anchoredPosition = _targets[target].anchoredPosition;
            _items[index].Rect.sizeDelta = new Vector2(TargetSize, TargetSize);
            _items[index].transform.SetSiblingIndex(0); // beneath still-moving items
            _hitCentres[target] = OutOfReach;
            _targetImages[target].color = new Color(1f, 1f, 1f, 0f); // the object now fills its shadow
            _targets[target].localScale = Vector3.one;
            _targetRings[target].gameObject.SetActive(false);

            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_items[index].Rect, 1.2f, 0.25f));

            if (AllPlaced()) _runner.StartCoroutine(OnRoundComplete());
        }

        private bool AllPlaced()
        {
            for (var i = 0; i < _round.ItemKeys.Length; i++) if (!_placed[i]) return false;
            return true;
        }

        // The target's item index (items and targets are paired by key; item order differs from target order).
        private int IndexOfItemForTarget(int targetIndex) => Array.IndexOf(_round.ItemKeys, _round.TargetKeys[targetIndex]);

        // --- Help ladder -------------------------------------------------------------------------------------

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _eva.Angry();
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    if (_demonstratedThisRound) { _game.Sfx.Retry(); _game.Voice.Say("count_retry"); }
                    else _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // The item the hint/demonstration helps: the round's own hint item while it is unplaced, else the first unplaced.
        private int NextUnplacedItem()
        {
            if (!_placed[_round.HintItemIndex]) return _round.HintItemIndex;
            for (var i = 0; i < _round.ItemKeys.Length; i++) if (!_placed[i]) return i;
            return -1;
        }

        // 2nd mistake: the hand grabs the item, carries it to its shadow, then back; nothing is placed for the child.
        private IEnumerator RunHint()
        {
            var item = NextUnplacedItem();
            if (item < 0) yield break;
            _helpRunning = true;
            SetItemsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);

            var target = _targets[_round.TargetIndexOf(item)].anchoredPosition;
            yield return Carry(item, _itemHome[item], target, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(item, target, _itemHome[item], HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            SetItemsInteractable(true);
        }

        // 3rd mistake: the hand carries the item to its shadow and drops it there (placed for the child).
        private IEnumerator RunDemonstrate()
        {
            var item = NextUnplacedItem();
            if (item < 0) yield break;
            _helpRunning = true;
            _demonstratedThisRound = true;
            SetItemsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) * 0.3f);

            var target = _targets[_round.TargetIndexOf(item)].anchoredPosition;
            yield return Carry(item, _itemHome[item], target, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            PlaceItem(item);
            if (!_roundOver) SetItemsInteractable(true);
        }

        // The hand glides to the item, "grabs" it, then moves with the item to `to`, fingertip on the item's centre.
        private IEnumerator Carry(int item, Vector2 from, Vector2 to, float seconds)
        {
            var rect = _items[item].Rect;
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from; // any snap-back slide has finished by now; start the carry exactly here
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

        // --- Round and session end ---------------------------------------------------------------------------

        private IEnumerator OnRoundComplete()
        {
            _roundOver = true;
            SetItemsInteractable(false);
            _hand.Hide();

            var clean = !_demonstratedThisRound;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            // The completed scene: every shadow filled, so let the whole row pop together.
            for (var i = 0; i < _round.ItemKeys.Length; i++)
                _runner.StartCoroutine(PopPulse(_items[i].Rect, 1.25f, 0.35f));

            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _items[_round.HintItemIndex].Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= _roundsPerSession) yield return EndSession();
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

        private void BuildEndButtons()
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform), typeof(WinCelebration));
            _endPanel.transform.SetParent(Root, false);
            var rect = (RectTransform)_endPanel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            EvaUi.IconButton(_endPanel.transform, "ReplayButton", EvaUi.Sprite("icons/replay"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[0], EndButtonSize, StartNewSession);
            EvaUi.IconButton(_endPanel.transform, "SessionHomeButton", EvaUi.Sprite("icons/home"), new Vector2(0.5f, 0.5f),
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(_homeScreenId));

            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _targetField.gameObject.SetActive(!ended);
            _itemField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ---------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens ------------------------------------------------------------------------------------

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

        // Scales the item up to `peak` times its current scale and back (its own scale at call time is the base).
        private static IEnumerator PopPulse(RectTransform target, float peak, float duration)
        {
            var baseScale = target.localScale;
            var half = duration * 0.5f;
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

        // --- Helpers -----------------------------------------------------------------------------------------

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
