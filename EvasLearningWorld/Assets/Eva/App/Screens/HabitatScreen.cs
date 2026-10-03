using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Zoo & Farm "Habitat" as a take-the-animal-home game. A round shows one animal (large, draggable, below) and
    // 3-4 habitats (pictures in a row above). The child drags the animal to a habitat; the habitat nearest the
    // finger grows and lights up (a neutral cue that does not say whether it is right). Release on a habitat:
    //   - its own: the animal hops in happily and Eva says where it lives ("Yes! It lives on the farm!");
    //   - a wrong one: the animal is NOT refused, it reacts with a matching sound and movement (a land animal
    //     in the water sinks in a splash and bubbles, a water animal out of the water flops about, any other one
    //     shivers with chattering teeth), then it returns to the child, who tries again. The wrong drop is a
    //     mistake on the help ladder.
    // Release away from every habitat: it just springs back, no mistake. One drop per round, 5 rounds a session.
    //
    // Same building blocks as DropSortScreen (DragItem, DropGeometry, PointerHand, HelpLadder, CoinPayout,
    // DifficultyLadder, end panel); the help ladder is per round: 1st mistake a retry, 2nd a hint (the hand carries
    // the animal to its habitat and back), 3rd a demonstration (the hand carries it and drops it for the child).
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): habitats (260, decorative) in a row at y 45 whose top edge (175)
    // clears the Hud's Home button; the animal (240, DragItem = TapTarget) at (-170, -245); Eva bottom-right.
    public sealed class HabitatScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxHabitats = 4;
        private const float CardSize = 260f;
        private const float AnimalSize = EvaUi.MinTap; // 240
        private const float RowCenterX = -170f;
        private const float CardY = 45f;
        private static readonly Vector2 AnimalHome = new Vector2(RowCenterX, -245f);
        private static readonly float[] Offsets4 = { -405f, -135f, 135f, 405f };
        private static readonly float[] Offsets3 = { -300f, 0f, 300f };
        // Where in a card the visiting animal stands (the lower half, on the ground) and how big it is there.
        private static readonly Vector2 VisitOffset = new Vector2(0f, -30f);
        private const float VisitScale = 0.62f;

        private const float SnapRadius = 130f;
        private const float HoverScale = 1.1f;
        private const float SnapBackSeconds = 0.25f;
        private const float ArriveSeconds = 0.3f;
        private const float HopSeconds = 0.3f;
        private const float ReactionSeconds = 1.1f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HandCarrySeconds = 1.2f;
        private const float HintRestSeconds = 0.5f;
        private const int BubbleCount = 5;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private const string AnimalSpritePrefix = "zoofarm/animal_";
        private const string HabitatSpritePrefix = "zoofarm/habitat_";
        private const string PromptKey = "zoofarm_prompt_habitat";
        private const string HintKey = "habitat_drag_hint";
        private const string DemoKey = "habitat_drag_demo";
        private const string HomeLinePrefix = "habitat_home_";

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _cardField, _animalField;
        private GameObject _endPanel;
        private PointerHand _hand;
        private Vector3 _evaBaseScale = Vector3.one;

        private RectTransform[] _cards;
        private Image[] _cardImages;
        private Image[] _cardRings;
        private WorldPoint[] _cardCentres;
        private Image[] _bubbles;
        private DragItem _animal;
        private Image _animalImage;

        private System.Random _rng;
        private int _roundIndex;
        private HabitatRound _round;
        private string _previousAnimal;
        private HelpLadder _ladder;
        private bool _roundOver, _helpRunning, _demonstratedThisRound, _placing;
        private int _hoverCard = -1;
        private Coroutine _hoverRoutine;

        public HabitatScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
        }

        // The round on screen now (null before the first round). Read-only; exposed so tests can drive the screen.
        public HabitatRound CurrentRound => _round;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _cardField = CreateFullRectContainer("CardField");
            _animalField = CreateFullRectContainer("AnimalField"); // after the habitats, so the dragged animal draws above them
            BuildCards();
            BuildBubbles();
            BuildAnimal();
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
            _previousAnimal = null;
            StopHover();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = HabitatRoundBuilder.Create(_getLevel(_game.Progress), _rng, _previousAnimal);
            _previousAnimal = _round.AnimalId;
            _ladder = new HelpLadder();
            _roundOver = false;
            _helpRunning = false;
            _placing = false;
            _demonstratedThisRound = false;
            ShowRound(_round);
            ShowAnimal();
            _animal.enabled = false;

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(PromptKey);
            _eva.SetTalking(false);
            if (!_roundOver && !_helpRunning && !_placing) _animal.enabled = true;
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildCards()
        {
            _cards = new RectTransform[MaxHabitats];
            _cardImages = new Image[MaxHabitats];
            _cardRings = new Image[MaxHabitats];
            for (var i = 0; i < MaxHabitats; i++)
            {
                var go = new GameObject("Habitat" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_cardField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(CardSize, CardSize);
                var image = go.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false; // decorative: never a TapTarget, never swallows the drag
                _cards[i] = rect;
                _cardImages[i] = image;

                var ringGo = new GameObject("HoverRing", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(go.transform, false);
                ringGo.transform.SetAsFirstSibling();
                var ringRect = (RectTransform)ringGo.transform;
                ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = new Vector2(0.5f, 0.5f);
                ringRect.anchoredPosition = Vector2.zero;
                ringRect.sizeDelta = new Vector2(CardSize * 1.25f, CardSize * 1.25f);
                var ring = ringGo.GetComponent<Image>();
                ring.sprite = EvaUi.Sprite("icons/ring_thin");
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ringGo.SetActive(false);
                _cardRings[i] = ring;
            }
        }

        // Small light-blue dots that rise off a sinking animal.
        private void BuildBubbles()
        {
            _bubbles = new Image[BubbleCount];
            for (var i = 0; i < BubbleCount; i++)
            {
                var go = new GameObject("Bubble" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_animalField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(34f, 34f);
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/dot");
                image.color = new Color(0.7f, 0.9f, 1f, 0.85f);
                image.raycastTarget = false;
                go.SetActive(false);
                _bubbles[i] = image;
            }
        }

        private void BuildAnimal()
        {
            _animal = DragItem.Create(_animalField, "habitatanimal", EvaUi.Sprite("icons/dot"), AnimalHome, AnimalSize);
            _animal.BeginDrag += _ => OnAnimalBeginDrag();
            _animal.EndDrag += _ => OnAnimalEndDrag();
            _animalImage = _animal.GetComponent<Image>();
        }

        private void ShowRound(HabitatRound round)
        {
            StopHoverRoutine();
            var count = round.HabitatIds.Length;
            var offsets = count == 4 ? Offsets4 : Offsets3;
            _cardCentres = new WorldPoint[count];
            for (var i = 0; i < MaxHabitats; i++)
            {
                var active = i < count;
                _cards[i].gameObject.SetActive(active);
                if (!active) continue;

                var x = RowCenterX + offsets[i];
                _cards[i].anchoredPosition = new Vector2(x, CardY);
                _cards[i].localScale = Vector3.one;
                _cards[i].localRotation = Quaternion.identity;
                _cardImages[i].sprite = EvaUi.Sprite(HabitatSpritePrefix + round.HabitatIds[i]);
                _cardImages[i].color = Color.white;
                _cardRings[i].gameObject.SetActive(false);
                _cardCentres[i] = new WorldPoint(x, CardY);
            }
            foreach (var bubble in _bubbles) bubble.gameObject.SetActive(false);
        }

        private void ShowAnimal()
        {
            _animal.gameObject.SetActive(true);
            _animal.Rect.anchoredPosition = AnimalHome;
            _animal.Rect.sizeDelta = new Vector2(AnimalSize, AnimalSize);
            _animal.Rect.localScale = Vector3.one;
            _animal.Rect.localRotation = Quaternion.identity;
            _animalImage.sprite = EvaUi.Sprite(AnimalSpritePrefix + _round.AnimalId);
            _animalImage.color = Color.white;
            _animalImage.raycastTarget = true;
            _runner.StartCoroutine(PopIn(_animal.Rect, 0.2f));
        }

        // --- Drag handling -----------------------------------------------------------------------------------

        private void OnAnimalBeginDrag()
        {
            if (_round == null || _roundOver || _helpRunning || _placing) return;
            StopHover();
            _hoverRoutine = _runner.StartCoroutine(HoverLoop());
        }

        // While the animal is held, the nearest habitat in snap range grows; every other habitat rests.
        private IEnumerator HoverLoop()
        {
            while (true)
            {
                var position = _animal.Rect.anchoredPosition;
                var nearest = DropGeometry.NearestWithinRadius(position.x, position.y, _cardCentres, SnapRadius);
                if (nearest != _hoverCard) SetHover(nearest);
                yield return null;
            }
        }

        private void SetHover(int card)
        {
            _hoverCard = card;
            for (var i = 0; i < _cardCentres.Length; i++)
            {
                var hovered = i == card;
                _cards[i].localScale = Vector3.one * (hovered ? HoverScale : 1f);
                _cardRings[i].gameObject.SetActive(hovered);
            }
        }

        private void StopHoverRoutine()
        {
            if (_hoverRoutine != null) { _runner.StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            _hoverCard = -1;
        }

        private void StopHover()
        {
            StopHoverRoutine();
            if (_cards == null) return;
            for (var i = 0; i < MaxHabitats; i++)
            {
                if (!_cards[i].gameObject.activeSelf) continue;
                _cards[i].localScale = Vector3.one;
                _cardRings[i].gameObject.SetActive(false);
            }
        }

        private void OnAnimalEndDrag()
        {
            StopHover();
            if (_round == null || _roundOver || _helpRunning || _placing) return;

            var position = _animal.Rect.anchoredPosition;
            var card = DropGeometry.NearestWithinRadius(position.x, position.y, _cardCentres, SnapRadius);
            if (card < 0) { _runner.StartCoroutine(SlideTo(_animal.Rect, AnimalHome, SnapBackSeconds)); return; } // empty space: no attempt, no mistake
            _runner.StartCoroutine(Visit(card));
        }

        // The animal walks into the habitat it was dropped on and reacts. Home ends the round; anything else
        // sends it back to the child as a mistake on the help ladder.
        private IEnumerator Visit(int card)
        {
            _placing = true;
            _animal.enabled = false;
            _animalImage.raycastTarget = false;
            var habitat = _round.HabitatIds[card];
            var reaction = HabitatRoundBuilder.ReactionFor(_round.AnimalId, habitat);

            var spot = _cards[card].anchoredPosition + VisitOffset;
            yield return Arrive(spot);

            if (reaction == HabitatReaction.Home)
            {
                _game.Sfx.Coin();
                _runner.StartCoroutine(PopPulse(_cards[card], 1.15f, 0.25f));
                _eva.SetTalking(true);
                _runner.StartCoroutine(Hops(spot, 2));
                yield return _game.Voice.SayAndWait(HomeLinePrefix + habitat);
                _eva.SetTalking(false);
                _placing = false;
                yield return OnRoundComplete(card);
                yield break;
            }

            switch (reaction)
            {
                case HabitatReaction.Sink: _game.Sfx.Splash(); yield return SinkReaction(spot); break;
                case HabitatReaction.Flop: _game.Sfx.Flop(); yield return FlopReaction(spot); break;
                default: _game.Sfx.Shiver(); yield return ShiverReaction(spot); break;
            }
            _animalImage.color = Color.white;
            _animal.Rect.localRotation = Quaternion.identity;
            foreach (var bubble in _bubbles) bubble.gameObject.SetActive(false);
            yield return SlideScale(_animal.Rect, AnimalHome, 1f, SnapBackSeconds * 1.5f);
            _placing = false;
            _animalImage.raycastTarget = true;
            _animal.enabled = true;
            HandleMistake();
        }

        // Slides the animal to `spot` in the habitat, shrinking to the visiting size.
        private IEnumerator Arrive(Vector2 spot) => SlideScale(_animal.Rect, spot, VisitScale, ArriveSeconds);

        private IEnumerator Hops(Vector2 spot, int hops)
        {
            for (var h = 0; h < hops; h++)
                for (var t = 0f; t < HopSeconds; t += Time.deltaTime)
                {
                    var lift = Mathf.Sin(t / HopSeconds * Mathf.PI) * 45f;
                    _animal.Rect.anchoredPosition = spot + new Vector2(0f, lift);
                    yield return null;
                }
            _animal.Rect.anchoredPosition = spot;
        }

        // A land animal in the water: it drops down and fades into the blue while bubbles rise, then bobs back up.
        private IEnumerator SinkReaction(Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var down = Mathf.Sin(Mathf.Min(k * 1.4f, 1f) * Mathf.PI * 0.5f); // sinks quickly, then rests
                var back = Mathf.Clamp01((k - 0.7f) / 0.3f);                       // bobs back up at the end
                var depth = down * (1f - back);
                _animal.Rect.anchoredPosition = spot + new Vector2(Mathf.Sin(k * 18f) * 6f * (1f - back), -depth * 70f);
                _animal.Rect.localScale = Vector3.one * (VisitScale * (1f - depth * 0.35f));
                _animalImage.color = Color.Lerp(Color.white, new Color(0.55f, 0.75f, 1f, 0.55f), depth);
                for (var b = 0; b < BubbleCount; b++)
                {
                    var rise = Mathf.Repeat(k * 1.6f - b * 0.12f, 1f);
                    var bubble = _bubbles[b];
                    bubble.gameObject.SetActive(k > b * 0.08f && k < 0.85f);
                    ((RectTransform)bubble.transform).anchoredPosition =
                        spot + new Vector2((b - 2) * 22f + Mathf.Sin(rise * 9f + b) * 8f, 10f + rise * 110f);
                    bubble.color = new Color(0.7f, 0.9f, 1f, 0.85f * (1f - rise));
                }
                yield return null;
            }
        }

        // A water animal out of the water: it flips from side to side and hops, gasping.
        private IEnumerator FlopReaction(Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var fade = 1f - k;
                _animal.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 7f * Mathf.PI * 2f) * 35f * fade + 10f * k);
                _animal.Rect.anchoredPosition = spot + new Vector2(0f, Mathf.Abs(Mathf.Sin(k * 5f * Mathf.PI)) * 40f * fade);
                yield return null;
            }
        }

        // Anywhere else: a small shiver, then a shake of the head ("this is not my home").
        private IEnumerator ShiverReaction(Vector2 spot)
        {
            for (var t = 0f; t < ReactionSeconds; t += Time.deltaTime)
            {
                var k = t / ReactionSeconds;
                var fade = 1f - k * 0.5f;
                _animal.Rect.anchoredPosition = spot + new Vector2(Mathf.Sin(k * 26f) * 9f * fade, 0f);
                _animal.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 9f) * 8f);
                yield return null;
            }
        }

        // --- Help ladder -------------------------------------------------------------------------------------

        private void HandleMistake()
        {
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry(); // the animal's reaction already said why
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    if (_demonstratedThisRound) _game.Sfx.Retry();
                    else _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the hand grabs the animal, carries it to its habitat, then back; nothing is placed.
        private IEnumerator RunHint()
        {
            _helpRunning = true;
            _animal.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(HintKey);

            var target = _cards[_round.CorrectIndex].anchoredPosition;
            yield return Carry(AnimalHome, target, HandCarrySeconds);
            _hand.Pulse(true);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Pulse(false);
            yield return Carry(target, AnimalHome, HandCarrySeconds * 0.6f);

            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            if (!_roundOver && !_placing) _animal.enabled = true;
        }

        // 3rd mistake: the hand carries the animal to its habitat and drops it there (placed for the child).
        private IEnumerator RunDemonstrate()
        {
            _helpRunning = true;
            _demonstratedThisRound = true;
            _animal.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say(DemoKey);
            yield return new WaitForSeconds(_game.Voice.Duration(DemoKey) * 0.3f);

            var target = _cards[_round.CorrectIndex].anchoredPosition;
            yield return Carry(AnimalHome, target, HandCarrySeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            _helpRunning = false;
            yield return Visit(_round.CorrectIndex);
        }

        // The hand glides to the animal, "grabs" it, then moves with it to `to`, fingertip on its centre.
        private IEnumerator Carry(Vector2 from, Vector2 to, float seconds)
        {
            var rect = _animal.Rect;
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

        private IEnumerator OnRoundComplete(int card)
        {
            _roundOver = true;
            _animal.enabled = false;
            _hand.Hide();

            var clean = !_demonstratedThisRound;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));

            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));
            yield return PayCoins(CoinPayout.ForStep(_ladder.Step), _cards[card].position);

            _roundIndex++;
            if (_roundIndex >= HabitatRoundBuilder.RoundsPerSession) yield return EndSession();
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
            _cardField.gameObject.SetActive(!ended);
            _animalField.gameObject.SetActive(!ended);
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

        private static IEnumerator PopIn(RectTransform target, float duration)
        {
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Scales the target up to `peak` times its current scale and back (its own scale at call time is the base).
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
