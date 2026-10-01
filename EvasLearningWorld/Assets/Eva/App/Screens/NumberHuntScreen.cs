using System;
using System.Collections;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The core playable round: Eva says a number, the child taps the matching numeral tile. A session is five
    // rounds (NumberHuntRoundGenerator.RoundsPerSession). Unlike Count, the tile IS the content - there is no
    // separate object-counting phase - so the help ladder is a simpler two-step escalation on top of the shared
    // HelpLadder: 1st mistake a gentle Retry, 2nd a Hint (the pointer hand points at, but does not tap, the
    // correct tile), 3rd a full Demonstrate (the hand taps the correct tile, then only that tile stays
    // interactable - the child still has to tap it themselves, per spec 4.6). See HelpLadder and CoinPayout
    // (Rules/HelpAndCoins.cs) for the payout each step still earns, and NumberHuntRoundGenerator (Rules/
    // NumberHunt.cs) for the level table and the confusable-numeral distractor rule.
    public sealed class NumberHuntScreen : ScreenBase
    {
        // Only used to host coroutines: its lifecycle is the screen's own Root, so hiding the screen
        // (Root.SetActive(false), done by Navigator on every screen switch) automatically halts any round in
        // flight instead of it continuing to talk or animate off-screen.
        private sealed class Runner : MonoBehaviour { }

        // Every tile is a real tap target (spec 4.10, no exemption here the way Count's counting-aid slots have
        // one - NoReadingAuditTests.EveryTapTargetIsAtLeast240UnitsSquare applies to every one of these), so the
        // size stays flat at MinTap for every tile count instead of Count's shrink-below-240 compromise at 5-6
        // tiles. All six are built once; ShowRoundAnswers shows and places the first Choices.Length of them.
        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const int TileNumeralFontSize = 70;
        private const float WobbleSeconds = 0.4f;

        // Hand-computed grid (not verified on a device - see the spike notes): frame is 1440x900, centred origin.
        // The companion pair sits bottom-right (CompanionLayout.Corner); the Hud reserves roughly x in [-690,-450] union [450,720],
        // y in [175,415], cleared by keeping every tile at y <= 120. CenterX/ColOffsets place each column;
        // TopY/BottomY/SingleY place each row.
        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float SingleY = -140f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _answerField;
        private GameObject _endPanel;

        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private TextMeshProUGUI[] _tileNumerals;
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private int? _previousTarget;
        private NumberHuntRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        // A new session starts every time the child opens Number Hunt (including a replay tap).
        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.NumberHunt);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousTarget = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            // BigCheer is a scale punch on Eva's root; leaving the screen mid-punch stops it, so restore her size.
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = NumberHuntRoundGenerator.Create(_game.Progress.NumberHuntLevel, _rng, _previousTarget);
            _previousTarget = _round.Target;
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowRoundAnswers(_round);
            SetAnswersInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("numhunt_find");
            yield return _game.Voice.SayAndWait("num_" + _round.Target);
            _eva.SetTalking(false);
            SetAnswersInteractable(true);
        }

        // --- Answer tiles -----------------------------------------------------------------------------------

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[MaxTiles];
            _tileImages = new Image[MaxTiles];
            _tileNumerals = new TextMeshProUGUI[MaxTiles];
            _tileButtons = new Button[MaxTiles];

            for (var i = 0; i < MaxTiles; i++)
            {
                var tile = new GameObject("Tile" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_answerField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(TileSize, TileSize);

                var image = tile.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.type = Image.Type.Simple;

                var numeral = EvaUi.Numeral(tile.transform, "Numeral", TileNumeralFontSize);
                var numeralRect = (RectTransform)numeral.transform;
                numeralRect.anchorMin = numeralRect.anchorMax = numeralRect.pivot = new Vector2(0.5f, 0.5f);
                numeralRect.anchoredPosition = Vector2.zero;
                numeralRect.sizeDelta = new Vector2(TileSize, 150f);

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileNumerals[i] = numeral;
                _tileButtons[i] = button;
            }
        }

        private void ShowRoundAnswers(NumberHuntRound round)
        {
            StopTilePulse();
            _tileTried = new bool[MaxTiles];
            var count = round.Choices.Length;
            var positions = PositionsFor(count);
            for (var i = 0; i < MaxTiles; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _tiles[i].anchoredPosition = positions[i];
                _tileNumerals[i].text = round.Choices[i].ToString();
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;
            }
        }

        // The hand-computed grid (see the class comment): 3 tiles in a single centred row; 4 in two rows of two;
        // 5 as a row of three over a row of two; 6 as two rows of three.
        private static Vector2[] PositionsFor(int count)
        {
            switch (count)
            {
                case 3:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], SingleY),
                        new Vector2(CenterX + ColOffsets3[1], SingleY),
                        new Vector2(CenterX + ColOffsets3[2], SingleY),
                    };
                case 4:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets2[0], TopY),
                        new Vector2(CenterX + ColOffsets2[1], TopY),
                        new Vector2(CenterX + ColOffsets2[0], BottomY),
                        new Vector2(CenterX + ColOffsets2[1], BottomY),
                    };
                case 5:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], TopY),
                        new Vector2(CenterX + ColOffsets3[1], TopY),
                        new Vector2(CenterX + ColOffsets3[2], TopY),
                        new Vector2(CenterX + ColOffsets2[0], BottomY),
                        new Vector2(CenterX + ColOffsets2[1], BottomY),
                    };
                case 6:
                    return new[]
                    {
                        new Vector2(CenterX + ColOffsets3[0], TopY),
                        new Vector2(CenterX + ColOffsets3[1], TopY),
                        new Vector2(CenterX + ColOffsets3[2], TopY),
                        new Vector2(CenterX + ColOffsets3[0], BottomY),
                        new Vector2(CenterX + ColOffsets3[1], BottomY),
                        new Vector2(CenterX + ColOffsets3[2], BottomY),
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(count));
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
        // dimmed) tile back into play so a 3rd mistake (Demonstrate) is reachable even with few tiles.
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
            if (_round == null || _roundOver || !_tileButtons[i].interactable) return;
            if (_round.Choices[i] == _round.Target) _runner.StartCoroutine(OnCorrectTile(i));
            else OnWrongTile(i);
        }

        private void OnWrongTile(int i)
        {
            _tileTried[i] = true;
            _eva.Angry();
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

        // 2nd mistake: tiles are disabled, the pointer hand moves to the correct tile and pulses (points, does
        // not tap it), then every tile (tried ones stay dimmed but become tappable again) is enabled once more.
        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("numhunt_hint");
            var leadRemaining = _game.Voice.Duration("numhunt_hint") + Voice.BreathSeconds;

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        // 3rd mistake: the hand moves to the correct tile and taps it, the tile gets an idle pulse, and only
        // that tile becomes interactable - the child still has to tap it themselves (spec 4.6).
        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("numhunt_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("numhunt_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[correctIndex]));
            SetOnlyTileInteractable(correctIndex);
        }

        private int CorrectTileIndex()
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == _round.Target) return i;
            return -1;
        }

        private IEnumerator OnCorrectTile(int i)
        {
            // Every tile, including wrong ones a Hint re-enabled: a stray tap during the cheer would start a
            // second Demonstrate or a second payout for the same round.
            _roundOver = true;
            SetAllTilesInteractable(false);
            StopTilePulse();
            _hand.Hide();

            // Read here (before anything else touches _ladder) so the same boolean drives both the bigger/warmer
            // Eva reaction below and the difficulty ladder's own "clean round" bookkeeping further down.
            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_tiles[i], _tileImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            // Difficulty ladder (spec 4.7) and coins are settled right at the correct tap, before the voice
            // lines, so leaving the screen during the cheer can no longer lose the round's result or its coins.
            _game.Progress.NumberHuntLevel = DifficultyLadder.RecordRound(_game.Progress.NumberHuntBuffer, _game.Progress.NumberHuntLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            // The number first (confirming what the child was asked to find), then the cheer. Reuses Count's
            // right-answer lines: they are generic enough ("Yes! That's right!" etc.) to not need Number Hunt's
            // own copies.
            yield return _game.Voice.SayAndWait("num_" + _round.Target);
            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= NumberHuntRoundGenerator.RoundsPerSession) yield return EndSession();
            else yield return RunRound();
        }

        // `fromWorldPosition` is the tapped tile's on-screen position: the coin-fly overlay's flight start. The
        // authoritative balance (AddCoins + Commit) happens synchronously, before the animation is even started,
        // so quitting mid-flight can never lose or desync a coin.
        private IEnumerator PayCoins(int payout, Vector2 fromWorldPosition)
        {
            var before = _game.Progress.Coins;
            _game.Progress.AddCoins(payout);
            _game.Commit();
            yield return _game.Hud.AnimateCoins(before, before + payout, _game.Sfx, fromWorldPosition);
        }

        private IEnumerator EndSession()
        {
            // Unlike Count, Number Hunt is never part of the first-run tutorial, so finishing a session here
            // never advances Progress.Tutorial.
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

        // Number Hunt is never part of the first-run tutorial (unlike Count), so Home always goes back to the
        // School activity list it was opened from - no tutorial branch needed.
        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.School);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _answerField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens -----------------------------------------------------------------------------------

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

        // A bigger, warmer scale bounce on Eva's own rig for a clean round (no Demonstrate this round), run
        // alongside the existing Cheer() animator trigger rather than adding a new rig state. `baseScale` is the
        // rig's own current scale (already mirrored to face left; see BuildEva), so this never flips or
        // otherwise disturbs that.
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

        // A continuous gentle breathing scale, used during Demonstrate to invite a tap on the correct tile once
        // it is revealed. Runs until the hosting Runner is stopped (screen hidden) or the caller explicitly
        // stops the returned Coroutine (see StopTilePulse).
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

        // Full-bleed (FullBleed.Attach): the picture fills the whole canvas, under a camera cutout too, unlike
        // the safe-area controls above it. Reuses school_bg (same as CountScreen) for visual consistency - this
        // is a second School-building activity, the same in-game room.
        private void AddSchoolBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/school_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
