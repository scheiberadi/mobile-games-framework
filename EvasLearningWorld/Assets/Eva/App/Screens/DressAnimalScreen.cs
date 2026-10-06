using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // DRESS-THE-ANIMAL presenter (Zoo & Farm Covering, Rules/Covering.cs): an animal stands in the middle with a dark shadow over its
    // trunk (so only the head, legs, arms or wings and tail show) and a row of coverings below - fur, wool, feathers, skin, shell. The
    // child drags the covering that belongs to the animal onto it. The right one flies onto the trunk, the shadow lifts to show the
    // animal's own coat, the animal hops and calls out with its own sound, and Eva says what it is. A wrong one makes the animal shake its
    // head and the covering springs back (a mistake on the help ladder). Release away from the animal: it just springs back, no mistake.
    // A round ends when the shadow has lifted; a session is RoundsPerSession animals, the level ladder is recorded per round.
    //
    // Same building blocks as DropSortScreen and PairingScreen (DragItem, PointerHand, HelpLadder, CoinPayout, DifficultyLadder, the end panel).
    // Help ladder per round: 1st mistake a retry, 2nd a hint (the hand carries the right covering to the animal and back), 3rd a
    // demonstration (the hand carries it and puts it on); "clean" means no demonstration fired that round.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): the animal (400) at (-140, 100); the coverings (190, DragItem = TapTarget) in a row at
    // y -285, centred on x -140, clear of the pair standing bottom-right.
    public sealed class DressAnimalScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxChoices = 5;
        private const float CentreX = -140f;
        private static readonly Vector2 AnimalCentre = new Vector2(CentreX, 100f);
        private const float AnimalSize = 400f;
        private const float ItemSize = 190f;
        private const float ItemY = -285f;
        private const float SnapRadius = 240f;
        private const float HoverScale = 1.05f;
        private const float SnapBackSeconds = 0.25f;
        private const float FlySeconds = 0.35f;
        private const float RevealSeconds = 0.7f;
        private const float WobbleSeconds = 0.5f;
        private const float HopSeconds = 0.3f;
        private const float MaxSoundWait = 1.8f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly string _hintKey, _demoKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _animalField, _itemField;
        private RectTransform _animal;
        private Image _animalImage, _shadowImage, _ring;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private DragItem[] _items;
        private Image[] _itemImages;
        private Vector2[] _itemHomes;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousTarget;
        private MatchRound _round;
        private HelpLadder _ladder;
        private bool _roundOver, _helpRunning, _placing, _hovering;
        private bool _demonstratedThisRound;
        private int _rightLineIndex;
        private Coroutine _hoverRoutine;
        private int _dragging = -1;

        public DressAnimalScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            string hintKey, string demoKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _hintKey = hintKey;
            _demoKey = demoKey;
        }

        // Read-only state, exposed so tests can drive the screen.
        public MatchRound CurrentRound => _round;
        public bool ShadowShowing => _shadowImage != null && _shadowImage.color.a > 0.01f;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _animalField = CreateFullRectContainer("AnimalField");
            _itemField = CreateFullRectContainer("ItemField"); // after the animal, so a dragged covering draws above it
            BuildAnimal();
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
            _runner.StopAllCoroutines();
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            _previousTarget = null;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = CoveringRules.Create(_getLevel(_game.Progress), _rng, _previousTarget);
            _previousTarget = _round.TargetId;
            _ladder = new HelpLadder();
            _roundOver = false;
            _helpRunning = false;
            _placing = false;
            _demonstratedThisRound = false;
            ShowRound();
            SetItemsEnabled(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.PromptVoiceKey);
            _eva.SetTalking(false);
            if (!_roundOver && !_helpRunning && !_placing) SetItemsEnabled(true);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildAnimal()
        {
            var go = new GameObject("Animal", typeof(RectTransform));
            go.transform.SetParent(_animalField, false);
            _animal = (RectTransform)go.transform;
            _animal.anchorMin = _animal.anchorMax = _animal.pivot = new Vector2(0.5f, 0.5f);
            _animal.anchoredPosition = AnimalCentre;
            _animal.sizeDelta = new Vector2(AnimalSize, AnimalSize);

            _ring = NewImage("HoverRing", _animal, "icons/ring_thin", AnimalSize * 1.15f);
            _ring.gameObject.SetActive(false);
            _animalImage = NewImage("Picture", _animal, null, AnimalSize);
            _shadowImage = NewImage("Shadow", _animal, null, AnimalSize); // the same size and place: lies exactly over the animal
        }

        private static Image NewImage(string name, Transform parent, string sprite, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            if (sprite != null) image.sprite = EvaUi.Sprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
            return image;
        }

        private void BuildItems()
        {
            _items = new DragItem[MaxChoices];
            _itemImages = new Image[MaxChoices];
            _itemHomes = new Vector2[MaxChoices];
            for (var i = 0; i < MaxChoices; i++)
            {
                var slot = i;
                var item = DragItem.Create(_itemField, "covering" + i, EvaUi.Sprite("icons/dot"), new Vector2(CentreX, ItemY), ItemSize);
                item.BeginDrag += _ => OnBeginDrag(slot);
                item.EndDrag += _ => OnEndDrag(slot);
                _items[i] = item;
                _itemImages[i] = item.GetComponent<Image>();
                item.gameObject.SetActive(false);
            }
        }

        private void ShowRound()
        {
            StopHover();
            _animalImage.sprite = EvaUi.Sprite(_round.TargetSprite);
            _animalImage.color = Color.white;
            _shadowImage.sprite = EvaUi.Sprite("zoofarm/shadow_" + _round.TargetId);
            _shadowImage.color = Color.white;
            _animal.anchoredPosition = AnimalCentre;
            _animal.localScale = Vector3.one;
            _animal.localRotation = Quaternion.identity;
            _runner.StartCoroutine(PopIn(_animal, 0.3f));

            var count = _round.ChoiceSprites.Length;
            var pitch = count == 3 ? 270f : (count == 4 ? 230f : 190f);
            for (var i = 0; i < MaxChoices; i++)
            {
                var active = i < count;
                _items[i].gameObject.SetActive(active);
                if (!active) continue;
                _itemHomes[i] = new Vector2(CentreX + (i - (count - 1) / 2f) * pitch, ItemY);
                _items[i].Rect.anchoredPosition = _itemHomes[i];
                _items[i].Rect.sizeDelta = new Vector2(ItemSize, ItemSize);
                _items[i].Rect.localScale = Vector3.one;
                _items[i].Rect.localRotation = Quaternion.identity;
                _itemImages[i].sprite = EvaUi.Sprite(_round.ChoiceSprites[i]);
                _itemImages[i].color = Color.white;
                _itemImages[i].raycastTarget = true;
                _runner.StartCoroutine(PopIn(_items[i].Rect, 0.25f));
            }
        }

        private void SetItemsEnabled(bool enabled)
        {
            for (var i = 0; i < MaxChoices; i++)
                if (_items[i] != null) _items[i].enabled = enabled && i < (_round?.ChoiceSprites.Length ?? 0) && _items[i].gameObject.activeSelf;
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void OnBeginDrag(int slot)
        {
            if (_round == null || _roundOver || _helpRunning || _placing) return;
            _dragging = slot;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop(slot));
        }

        // While a covering is held, the animal grows and a ring lights up once it is close enough to drop on.
        private IEnumerator HoverLoop(int slot)
        {
            while (true)
            {
                var near = IsOverAnimal(_items[slot].Rect.anchoredPosition);
                if (near != _hovering) SetHover(near);
                yield return null;
            }
        }

        private static bool IsOverAnimal(Vector2 position) => (position - AnimalCentre).magnitude <= SnapRadius;

        private void SetHover(bool hovering)
        {
            _hovering = hovering;
            _animal.localScale = Vector3.one * (hovering ? HoverScale : 1f);
            _ring.gameObject.SetActive(hovering);
        }

        private void StopHover()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hovering = false;
            if (_animal != null) { _animal.localScale = Vector3.one; _ring.gameObject.SetActive(false); }
        }

        private void OnEndDrag(int slot)
        {
            StopHover();
            _dragging = -1;
            if (_round == null || _roundOver || _helpRunning || _placing) return;

            if (!IsOverAnimal(_items[slot].Rect.anchoredPosition)) { SnapBack(slot); return; } // empty space: not an attempt, no mistake
            if (slot == _round.CorrectIndex) _runner.StartCoroutine(PlaceCovering(slot));
            else
            {
                _runner.StartCoroutine(Wobble(_animal, WobbleSeconds));
                SnapBack(slot);
                HandleMistake();
            }
        }

        private void SnapBack(int slot) => _runner.StartCoroutine(SlideTo(_items[slot].Rect, _itemHomes[slot], SnapBackSeconds));

        // --- The right covering ------------------------------------------------------------------------------

        // The covering flies onto the animal's trunk, the shadow lifts, the animal hops and calls out, Eva says what it is.
        private IEnumerator PlaceCovering(int slot)
        {
            _placing = true;
            SetItemsEnabled(false);
            var item = _items[slot];

            var (cx, cy) = TrunkShadows.Centres.TryGetValue(_round.TargetId, out var centre) ? centre : (0.5f, 0.55f);
            var trunk = AnimalCentre + new Vector2((cx - 0.5f) * AnimalSize, (0.5f - cy) * AnimalSize);
            yield return SlideScale(item.Rect, trunk, 0.5f, FlySeconds);

            _game.Sfx.Coin();
            _runner.StartCoroutine(Fade(_shadowImage, RevealSeconds));
            _runner.StartCoroutine(Fade(_itemImages[slot], RevealSeconds * 0.8f));

            var heard = _game.Sfx.PlayAnimal(_round.TargetId);
            if (heard <= 0f && ZooFarmAnimals.All.Any(a => a.Id == _round.TargetId && a.RealmOf == Realm.Sea)) { _game.Sfx.Splash(); heard = 0.8f; } // a swimmer with no call of its own
            var wait = Mathf.Clamp(heard, RevealSeconds, MaxSoundWait);
            var hops = Mathf.Clamp(Mathf.RoundToInt(wait / HopSeconds), 2, 6);
            _runner.StartCoroutine(Hops(_animal, AnimalCentre, hops));
            yield return new WaitForSeconds(wait);

            item.gameObject.SetActive(false);
            _placing = false;
            yield return OnRoundComplete();
        }

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

        // 2nd mistake: the hand grabs the right covering, carries it to the animal, then back; nothing is placed.
        private IEnumerator RunHint()
        {
            _helpRunning = true;
            SetItemsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintKey);

            var slot = _round.CorrectIndex;
            yield return Carry(slot, _itemHomes[slot], AnimalCentre, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(slot, AnimalCentre, _itemHomes[slot], HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            if (!_roundOver && !_placing) SetItemsEnabled(true);
        }

        // 3rd mistake: the hand carries the right covering onto the animal and puts it on for the child.
        private IEnumerator RunDemonstrate()
        {
            _helpRunning = true;
            _demonstratedThisRound = true;
            SetItemsEnabled(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoKey) * 0.3f);

            var slot = _round.CorrectIndex;
            yield return Carry(slot, _itemHomes[slot], AnimalCentre, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            yield return PlaceCovering(slot);
        }

        // The hand glides to the covering, "grabs" it, then moves with it to `to`, fingertip on its centre.
        private IEnumerator Carry(int slot, Vector2 from, Vector2 to, float seconds)
        {
            var rect = _items[slot].Rect;
            yield return _hand.MoveTo(from, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            rect.anchoredPosition = from;
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
            SetItemsEnabled(false);
            _hand.Hide();

            var clean = !_demonstratedThisRound;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _animal.position));

            _game.Voice.Say(_round.TargetVoiceKey);
            yield return new WaitForSeconds(Mathf.Max(0.6f, _game.Voice.Duration(_round.TargetVoiceKey)));
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= CoveringRules.RoundsPerSession) yield return EndSession();
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
            _animalField.gameObject.SetActive(!ended);
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

        private static IEnumerator SlideScale(RectTransform target, Vector2 to, float scale, float seconds)
        {
            var from = target.anchoredPosition;
            var fromScale = target.localScale.x;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = PointerHand.EaseInOut(t / seconds);
                target.anchoredPosition = Vector2.Lerp(from, to, k);
                target.localScale = Vector3.one * Mathf.Lerp(fromScale, scale, k);
                yield return null;
            }
            target.anchoredPosition = to;
            target.localScale = Vector3.one * scale;
        }

        private static IEnumerator Fade(Image image, float seconds)
        {
            var from = image.color;
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                image.color = new Color(from.r, from.g, from.b, Mathf.Lerp(from.a, 0f, t / seconds));
                yield return null;
            }
            image.color = new Color(from.r, from.g, from.b, 0f);
        }

        private static IEnumerator Hops(RectTransform rect, Vector2 home, int hops)
        {
            for (var h = 0; h < hops; h++)
                for (var t = 0f; t < HopSeconds; t += Time.deltaTime)
                {
                    rect.anchoredPosition = home + new Vector2(0f, Mathf.Sin(t / HopSeconds * Mathf.PI) * 40f);
                    yield return null;
                }
            rect.anchoredPosition = home;
        }

        private static IEnumerator PopIn(RectTransform target, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private static IEnumerator Wobble(RectTransform target, float duration)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                var decay = 1f - t / duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t / duration * cycles * Mathf.PI * 2f) * amplitude * decay);
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
