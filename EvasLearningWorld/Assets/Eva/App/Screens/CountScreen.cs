using System.Collections;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The core playable round: count the objects, then tap the matching numeral tile. A session is five rounds
    // (CountRoundGenerator.RoundsPerSession). Eva's help ladder climbs with each wrong tap in a round: 1st
    // mistake is a gentle Retry, 2nd is a Hint (Eva and the pointer hand count the objects aloud), 3rd is a
    // full Demonstrate (the child counts a fresh tally with the hand's guidance, then taps the highlighted
    // answer) - see HelpLadder (Rules/HelpAndCoins.cs) and CoinPayout for the payout each step still earns.
    public sealed class CountScreen : ScreenBase
    {
        // Only used to host coroutines: its lifecycle is the screen's own Root, so hiding the screen
        // (Root.SetActive(false), done by Navigator on every screen switch) automatically halts any round in
        // flight instead of it continuing to talk or animate off-screen.
        private sealed class Runner : MonoBehaviour { }

        private const float EvaHeight = 560f;
        private static readonly Vector2 EvaPosition = new Vector2(520f, -120f);

        private const float ObjectHitSize = 240f; // EvaUi.MinTap
        private const float ObjectVisualSize = 200f;
        private const float BadgeSize = 64f;
        private const float PopInSeconds = 0.25f;

        private const float TileSize = 280f;
        private const int TileNumeralFontSize = 140;
        private const float DotSize = 60f;
        private const float WobbleSeconds = 0.4f;
        private static readonly Vector2[] TilePositions = { new Vector2(-430f, -290f), new Vector2(-140f, -290f), new Vector2(150f, -290f) };

        // Eva's help ladder (Task 8): timings for the pointer hand's moves and taps while she counts aloud.
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        // Standard die-face pip layouts, 1 to 5, centred on (0, 0) inside the dots area.
        private static readonly Vector2[][] DiePips =
        {
            new[] { Vector2.zero },
            new[] { new Vector2(-45f, 45f), new Vector2(45f, -45f) },
            new[] { new Vector2(-45f, 45f), Vector2.zero, new Vector2(45f, -45f) },
            new[] { new Vector2(-45f, 45f), new Vector2(45f, 45f), new Vector2(-45f, -45f), new Vector2(45f, -45f) },
            new[] { new Vector2(-45f, 45f), new Vector2(45f, 45f), Vector2.zero, new Vector2(-45f, -45f), new Vector2(45f, -45f) }
        };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _objectField;
        private RectTransform _answerField;
        private GameObject _endPanel;

        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private TextMeshProUGUI[] _tileNumerals;
        private RectTransform[] _tileDots;
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private RectTransform[] _objectIconRects;
        private Image[] _objectGlowImages;
        private GameObject[] _objectBadges;
        private Coroutine[] _objectPulseRoutines;

        private PointerHand _hand;
        private bool _demonstrating;
        private CountTally _demoTally;

        private System.Random _rng;
        private int _roundIndex;
        private CountObject? _previousObject;
        private CountRound _round;
        private CountTally _tally;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _objectField = CreateFullRectContainer("ObjectField");
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        // A new session starts every time the child walks into the school (including a replay tap). Task 12's
        // guide is still told about every screen show (per its own brief), even though FirstGame - the only
        // step ever active here - has no table row: TutorialGuide.Plan defaults to no line and no pointing for
        // it, since Tasks 7/8 already own this screen's own help ladder.
        public override void OnShow()
        {
            // Review fix (controller ruling): the missing EnteredSchool event - without it Progress.Tutorial
            // never leaves GoToSchool, so every table row past it (GoToStore, FirstPurchase, PlacePurchase,
            // Done) could never trigger in real gameplay. Advance before Refresh, matching StoreScreen.OnShow's
            // EnteredStore/Refresh order, so the guide reads the post-entry step.
            _game.Progress.Advance(TutorialEvent.EnteredSchool);
            _game.Commit();
            _game.TutorialGuide.Refresh(ScreenId.School);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousObject = null;
            _rightLineIndex = 0;
            _demonstrating = false;
            _demoTally = null;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = CountRoundGenerator.Create(_game.Progress.DifficultyLevel, _rng, _previousObject);
            _previousObject = _round.Object;
            _tally = new CountTally(_round.Quantity);
            _ladder = new HelpLadder();
            _demonstrating = false;

            BuildObjectsForRound(_round);
            ShowRoundAnswers(_round);
            SetAnswersInteractable(false);

            // First ever session: before the first round's question, Eva and the hand count the objects aloud
            // once, unprompted (the same choreography as the Hint step, but with its own voice line), so the
            // child sees the counting motion before ever being asked to answer.
            if (_roundIndex == 0 && !_game.Progress.CountIntroSeen)
            {
                yield return RunHandCount("count_intro");
                _game.Progress.CountIntroSeen = true;
                _game.Commit();
            }

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("count_q_" + NameFor(_round.Object));
            _eva.SetTalking(false);
            SetAnswersInteractable(true);
        }

        // --- Objects (the counting aid) -------------------------------------------------------------------

        private void BuildObjectsForRound(CountRound round)
        {
            ClearChildren(_objectField);
            var positions = CountLayout.Positions(round.Quantity);
            _objectIconRects = new RectTransform[positions.Length];
            _objectGlowImages = new Image[positions.Length];
            _objectBadges = new GameObject[positions.Length];
            _objectPulseRoutines = new Coroutine[positions.Length];
            for (var i = 0; i < positions.Length; i++)
                BuildObjectSlot(i, round.Object, positions[i]);
        }

        private void BuildObjectSlot(int index, CountObject obj, Vector2 position)
        {
            var slot = new GameObject("Object" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
            slot.transform.SetParent(_objectField, false);
            var rect = (RectTransform)slot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(ObjectHitSize, ObjectHitSize);

            var catcher = slot.GetComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var glowObject = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowObject.transform.SetParent(slot.transform, false);
            var glowRect = (RectTransform)glowObject.transform;
            glowRect.anchorMin = glowRect.anchorMax = glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(ObjectHitSize - 10f, ObjectHitSize - 10f);
            var glowImage = glowObject.GetComponent<Image>();
            glowImage.sprite = RoundedRectSprite.Get();
            glowImage.type = Image.Type.Sliced;
            glowImage.color = new Color(1f, 0.85f, 0.3f, 0f);
            glowImage.raycastTarget = false;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slot.transform, false);
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(ObjectVisualSize, ObjectVisualSize);
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = EvaUi.Sprite("objects/" + NameFor(obj));
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var badgeObject = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(slot.transform, false);
            var badgeRect = (RectTransform)badgeObject.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-4f, -4f);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            var badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.sprite = EvaUi.Sprite("icons/check");
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
            badgeObject.SetActive(false);

            var button = slot.GetComponent<Button>();
            button.targetGraphic = catcher;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnObjectTapped(index, iconRect, iconImage, glowImage, badgeObject));

            _objectIconRects[index] = iconRect;
            _objectGlowImages[index] = glowImage;
            _objectBadges[index] = badgeObject;

            _runner.StartCoroutine(PopIn(iconRect, PopInSeconds));
        }

        private void OnObjectTapped(int index, RectTransform iconRect, Image iconImage, Image glowImage, GameObject badge)
        {
            if (_demonstrating)
            {
                OnDemoObjectTapped(index, iconRect, iconImage, glowImage, badge);
                return;
            }

            // The first tap on an object not yet counted glows it, marks it with a tick badge and speaks the
            // running number. Tapping an already-counted object only bounces it: the spoken count never advances,
            // never wraps and a completed tally does nothing further (the child still has to pick the answer).
            if (_tally == null) return;
            if (_tally.IsCounted(index))
            {
                _runner.StartCoroutine(PopPulse(iconRect, null, 1.08f, 0.15f));
                return;
            }
            if (_tally.TryCount(index, out var number))
            {
                badge.SetActive(true);
                _runner.StartCoroutine(PopPulse(iconRect, iconImage, 1.2f, 0.3f));
                _runner.StartCoroutine(GlowPulse(glowImage, 0.5f));
                _game.Voice.Say("num_" + number);
            }
        }

        // --- Demonstrate (3rd mistake): the child counts with a fresh tally, guided by the hand -------------

        // Tapping an object not yet counted by the fresh demo tally counts it; an already-counted object (or
        // anywhere without one) does nothing, per CountTally.TryCount's own rules.
        private void OnDemoObjectTapped(int index, RectTransform iconRect, Image iconImage, Image glowImage, GameObject badge)
        {
            if (_demoTally == null || !_demoTally.TryCount(index, out var number)) return;
            StopObjectPulse(index, iconRect);
            badge.SetActive(true);
            _runner.StartCoroutine(PopPulse(iconRect, iconImage, 1.2f, 0.3f));
            _runner.StartCoroutine(GlowPulse(glowImage, 0.5f));
            _game.Voice.Say("num_" + number);
            if (_demoTally.IsComplete) _runner.StartCoroutine(RunDemoAnswer());
            else _runner.StartCoroutine(MoveHandToNextDemoObject());
        }

        private IEnumerator MoveHandToNextDemoObject()
        {
            _hand.Pulse(false);
            var nextIndex = NextUncountedIndex();
            if (nextIndex < 0) yield break;
            yield return _hand.MoveTo(CountLayout.Positions(_round.Quantity)[nextIndex], HandMoveSeconds);
            _hand.Pulse(true);
        }

        // Once every object is counted: Eva announces the answer step, the hand moves to (and the tile itself
        // pulses at) the correct tile, every other tile is disabled, and only the correct tile can be tapped -
        // tapping it pays the Demonstrated tier via the normal OnCorrectTile path.
        private IEnumerator RunDemoAnswer()
        {
            _hand.Pulse(false);
            _eva.SetTalking(true);
            _game.Voice.Say("count_demo_answer");
            yield return new WaitForSeconds(_game.Voice.Duration("count_demo_answer"));
            _eva.SetTalking(false);

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[correctIndex]));
            SetOnlyTileInteractable(correctIndex);
        }

        private int NextUncountedIndex()
        {
            for (var i = 0; i < _round.Quantity; i++)
                if (!_demoTally.IsCounted(i)) return i;
            return -1;
        }

        private int CorrectTileIndex()
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == _round.Quantity) return i;
            return -1;
        }

        private void ResetObjectBadgesForDemo()
        {
            for (var i = 0; i < _objectBadges.Length; i++)
            {
                _objectBadges[i].SetActive(false);
                var color = _objectGlowImages[i].color;
                color.a = 0f;
                _objectGlowImages[i].color = color;
                _objectIconRects[i].localScale = Vector3.one;
            }
        }

        private void StartAllObjectPulses()
        {
            for (var i = 0; i < _objectIconRects.Length; i++)
                // Skip anything already counted (the child can tap faster than this gets called): pulsing it
                // now would start an idle-pulse coroutine that StopObjectPulse never gets a chance to stop.
                if (!_demoTally.IsCounted(i))
                    _objectPulseRoutines[i] = _runner.StartCoroutine(IdlePulseLoop(_objectIconRects[i]));
        }

        private void StopObjectPulse(int index, RectTransform iconRect)
        {
            if (_objectPulseRoutines != null && index >= 0 && index < _objectPulseRoutines.Length && _objectPulseRoutines[index] != null)
            {
                _runner.StopCoroutine(_objectPulseRoutines[index]);
                _objectPulseRoutines[index] = null;
            }
            iconRect.localScale = Vector3.one;
        }

        // --- Answer tiles -----------------------------------------------------------------------------------

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[3];
            _tileImages = new Image[3];
            _tileNumerals = new TextMeshProUGUI[3];
            _tileDots = new RectTransform[3];
            _tileButtons = new Button[3];

            for (var i = 0; i < 3; i++)
            {
                var tile = new GameObject("Tile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_answerField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = TilePositions[i];
                rect.sizeDelta = new Vector2(TileSize, TileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.type = Image.Type.Simple;

                var numeral = EvaUi.Numeral(tile.transform, "Numeral", TileNumeralFontSize);
                var numeralRect = (RectTransform)numeral.transform;
                numeralRect.anchorMin = numeralRect.anchorMax = numeralRect.pivot = new Vector2(0.5f, 0.5f);
                numeralRect.anchoredPosition = new Vector2(0f, 55f);
                numeralRect.sizeDelta = new Vector2(240f, 150f);

                var dotsObject = new GameObject("Dots", typeof(RectTransform));
                dotsObject.transform.SetParent(tile.transform, false);
                var dotsRect = (RectTransform)dotsObject.transform;
                dotsRect.anchorMin = dotsRect.anchorMax = dotsRect.pivot = new Vector2(0.5f, 0.5f);
                dotsRect.anchoredPosition = new Vector2(0f, -65f);
                dotsRect.sizeDelta = new Vector2(240f, 120f);

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileNumerals[i] = numeral;
                _tileDots[i] = dotsRect;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundAnswers(CountRound round)
        {
            StopTilePulse();
            _tileTried = new bool[3];
            for (var i = 0; i < 3; i++)
            {
                var value = round.Choices[i];
                _tileNumerals[i].text = value.ToString();
                ArrangeDots(_tileDots[i], value);
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;
            }
        }

        private void ArrangeDots(RectTransform container, int count)
        {
            ClearChildren(container);
            var pips = DiePips[Mathf.Clamp(count - 1, 0, DiePips.Length - 1)];
            foreach (var offset in pips)
            {
                var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(container, false);
                var rect = (RectTransform)dot.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = offset;
                rect.sizeDelta = new Vector2(DotSize, DotSize);
                var image = dot.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/dot");
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        // Only tiles not yet tried this round are touched (used at round start and after a correct answer, so
        // a wrong tile from earlier this round never becomes tappable again through this path).
        private void SetAnswersInteractable(bool interactable)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                if (!_tileTried[i]) _tileButtons[i].interactable = interactable;
        }

        // Every tile, tried or not - used by the Hint step, which must bring a previously-tried (and still
        // dimmed) tile back into play: with only 3 tiles and 2 wrong ones, the 3rd mistake (Demonstrate) can
        // only happen if the child can tap a wrong tile a second time.
        private void SetAllTilesInteractable(bool interactable)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                _tileButtons[i].interactable = interactable;
        }

        private void SetOnlyTileInteractable(int index)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                _tileButtons[i].interactable = i == index;
        }

        private void StopTilePulse()
        {
            if (_tilePulseRoutine != null)
            {
                _runner.StopCoroutine(_tilePulseRoutine);
                _tilePulseRoutine = null;
            }
            if (_tiles != null)
                foreach (var tile in _tiles) tile.localScale = Vector3.one;
        }

        private void OnTileTapped(int i)
        {
            if (_round == null || !_tileButtons[i].interactable) return;
            if (_round.Choices[i] == _round.Quantity) _runner.StartCoroutine(OnCorrectTile(i));
            else OnWrongTile(i);
        }

        private void OnWrongTile(int i)
        {
            _tileTried[i] = true;
            _tileButtons[i].interactable = false;
            _tileImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_tiles[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: tiles are disabled, Eva and the hand count the objects aloud, then every tile (tried
        // ones stay dimmed but become tappable again - see SetAllTilesInteractable) is enabled once more.
        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            yield return RunHandCount("count_hint");
            SetAllTilesInteractable(true);
        }

        // Shared choreography for the Hint step and the first-session intro: says `leadVoiceKey`, then walks
        // the hand to each object in reading order, tapping it while the matching num_<k> line plays, and
        // finally rests on the last object for HintRestSeconds before hiding. Does not touch tile interactability -
        // callers decide whether/when tiles come back.
        private IEnumerator RunHandCount(string leadVoiceKey)
        {
            _eva.SetTalking(true);
            _game.Voice.Say(leadVoiceKey);
            var positions = CountLayout.Positions(_round.Quantity);
            for (var k = 0; k < positions.Length; k++)
            {
                yield return _hand.MoveTo(positions[k], HandMoveSeconds);
                _game.Voice.Say("num_" + (k + 1));
                yield return _hand.Tap(HandTapSeconds);
            }
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
        }

        // 3rd mistake: the child counts a fresh tally with the hand's guidance, then taps the highlighted
        // correct tile. See OnDemoObjectTapped/RunDemoAnswer for the rest of the choreography.
        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _demonstrating = true;
            _demoTally = new CountTally(_round.Quantity);
            ResetObjectBadgesForDemo();

            _eva.SetTalking(true);
            _game.Voice.Say("count_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("count_demo"));
            _eva.SetTalking(false);

            // A quick child can finish counting before Eva's line is even done: RunDemoAnswer has already
            // taken over by the time we get here, so starting fresh pulses (or moving the hand back to a
            // "next" object that no longer exists) would only fight it.
            if (_demoTally.IsComplete) yield break;
            StartAllObjectPulses();
            yield return MoveHandToNextDemoObject();
        }

        private IEnumerator OnCorrectTile(int i)
        {
            SetAnswersInteractable(false);
            StopTilePulse();
            _demonstrating = false;
            _hand.Hide();

            // Read here (before anything else touches _ladder) so the same boolean drives both the bigger/warmer
            // Eva reaction below and the difficulty ladder's own "clean round" bookkeeping further down - one
            // definition of "clean", used consistently (Task 3 already established this exact check for the
            // ladder; presentation reuses it rather than inventing a stricter one).
            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            // Juice (spec 4.4), all layered on top of / concurrent with the existing cheer path, never yielded on,
            // so input is free again as soon as the voice lines below finish - none of this gates the next round.
            _runner.StartCoroutine(PopPulse(_tiles[i], _tileImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _eva.Root.localScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return _game.Voice.SayAndWait("num_" + _round.Quantity);

            // Difficulty ladder (spec 4.3): record this round's outcome and evaluate the rolling window before
            // PayCoins's own Commit() below, so a level change rides along on the same per-round save as the
            // coin payout instead of needing a separate commit (matches the existing per-round commit cadence).
            _game.Progress.DifficultyLevel = DifficultyLadder.RecordRound(_game.Progress.DifficultyBuffer, _game.Progress.DifficultyLevel, clean);
            yield return PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position);

            _roundIndex++;
            if (_roundIndex >= CountRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

        // `fromWorldPosition` is the tapped tile's on-screen position: the coin-fly overlay's flight start.
        // The authoritative balance (AddCoins + Commit) happens synchronously, before the animation is even
        // started - see Hud.AnimateCoins - so quitting mid-flight can never lose or desync a coin.
        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        private IEnumerator EndSession()
        {
            _game.Progress.Advance(TutorialEvent.RoundsFinished);
            _game.Commit();
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
                EndButtonPositions[1], EndButtonSize, () => _game.Navigator.Show(ScreenId.Map));

            _endPanel.SetActive(false);
        }

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _objectField.gameObject.SetActive(!ended);
            _answerField.gameObject.SetActive(!ended);
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
            // The object field sits to Eva's left; the rig faces right by default, so mirror it around its own
            // (bottom-centre) pivot to face left, towards the objects she is asking about.
            var scale = _eva.Root.localScale;
            _eva.Root.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
        }

        // --- Small tweens -----------------------------------------------------------------------------------

        private static IEnumerator PopIn(RectTransform target, float duration)
        {
            target.localScale = Vector3.zero;
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Clamp01(t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Scales `target` up to `peakScale` and back to 1; when `tint` is given, also pulses its colour towards white.
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

        private static IEnumerator GlowPulse(Image glow, float duration)
        {
            var half = duration * 0.5f;
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var color = glow.color;
                color.a = Mathf.Lerp(0f, 0.6f, t / half);
                glow.color = color;
                yield return null;
            }
            for (var t = 0f; t < half; t += Time.deltaTime)
            {
                var color = glow.color;
                color.a = Mathf.Lerp(0.6f, 0f, t / half);
                glow.color = color;
                yield return null;
            }
            var final = glow.color;
            final.a = 0f;
            glow.color = final;
        }

        // A soft rotation shake, settling back to upright.
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

        // A bigger, warmer scale bounce on Eva's own rig for a clean round (no Demonstrate this round),
        // run alongside the existing Cheer() animator trigger rather than adding a new rig state -
        // same shape as CreatorScreen.Hop's selection bounce, just a taller peak and longer hold so it reads
        // as extra warmth. `baseScale` is the rig's own current scale (already mirrored to face left; see
        // BuildEva), so this never flips or otherwise disturbs that.
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

        // A continuous gentle breathing scale, used during Demonstrate to invite a tap: on the not-yet-counted
        // objects, and later on the correct tile once it is revealed. Runs until the hosting Runner is stopped
        // (screen hidden) or the caller explicitly stops the returned Coroutine (see StopObjectPulse/StopTilePulse).
        private static IEnumerator IdlePulseLoop(RectTransform target)
        {
            const float period = 0.7f;
            const float peakScale = 1.12f;
            while (true)
            {
                for (var t = 0f; t < period; t += Time.deltaTime)
                {
                    var k = t / period;
                    var scale = k < 0.5f ? Mathf.Lerp(1f, peakScale, k * 2f) : Mathf.Lerp(peakScale, 1f, (k - 0.5f) * 2f);
                    target.localScale = Vector3.one * scale;
                    yield return null;
                }
            }
        }

        // --- Helpers ----------------------------------------------------------------------------------------

        private static string NameFor(CountObject obj) => obj.ToString().ToLowerInvariant();

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

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }

        // Filling exactly the safe area; see MapScreen.AddMapBackground for why a real image cannot use the
        // huge -1500..1500 offset that ScreenBase.AddBackground uses for a solid colour.
        private void AddSchoolBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/school_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
