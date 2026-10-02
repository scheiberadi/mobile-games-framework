using System;
using System.Collections;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // One More / One Less (M4 School, Mathematics): this session's first true DRAG & DROP mechanic (every
    // earlier School game reused Number Hunt's own numeral-tile pick). Levels 1-3 show a pond holding
    // `StartCount` ducks; a single draggable duck is either dragged INTO the pond ("one more") or dragged OUT of
    // it to a tray beside it ("one less") - only ever one draggable duck per round, so success is a single
    // distance check against whichever target the round asks for, DragItem/JigsawScreen's own "snap within
    // radius" shape (read JigsawScreen first for that pattern). From level 4 the objects drop for a numeric
    // question ("one more/less than N?") answered among tiles, Addition/Subtraction's own answer-tile mechanic
    // exactly, with the classic mistake of answering with the original count guaranteed among the choices. Same
    // three-step help ladder as every other game, but the plan's own Hint/Demo for this one differ: Hint glows
    // the correct target (the pond or the tray) rather than pointing at a tile; Demo has the hand carry the duck
    // to the target once, then resets it to its start so the child still has to drag it themselves - never
    // FingerMazeScreen's "hand retraces, child repeats" shape (read that one for how a demoed drag resets).
    public sealed class OneMoreOneLessScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // --- Levels 1-3: drag the duck ------------------------------------------------------------------------
        private static readonly Vector2 PondCenter = new Vector2(-120f, -60f);
        private const float PondWidth = 480f;
        private const float PondHeight = 300f;
        private static readonly Vector2 TraySlot = new Vector2(320f, -60f);
        private const float TraySize = 200f;
        private const float DuckSize = 240f; // EvaUi.MinTap
        private const float PondIconSize = 70f;
        private const int PondIconsPerRow = 4;
        private const float PondIconPitch = 70f;
        private const float SnapRadius = 170f;
        private const float SpringBackSeconds = 0.35f;
        private const float DragMoveSeconds = 0.7f;

        // --- Levels 4-6: tap the numeral answer (Addition/Subtraction's own answer-tile shape) ----------------
        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const int TileNumeralFontSize = 70;
        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float SingleY = -140f;

        private const float WobbleSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;
        private const float HintGlowSeconds = 1.2f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;

        private RectTransform _pondField;
        private Image _pondImage;
        private Image _trayImage;
        private RectTransform _pondIconsRoot;
        private DragItem _duck;
        private Image _duckImage;
        private Vector2 _duckStart;
        private Vector2 _duckTarget;
        private Coroutine _zoneGlowRoutine;

        private RectTransform _problemField;
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
        private bool? _previousIsMore;
        private OneMoreOneLessRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _pondField = CreateFullRectContainer("PondField");
            BuildPond();
            _problemField = CreateFullRectContainer("ProblemField");
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.OneMoreOneLess);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousIsMore = null;
            _rightLineIndex = 0;
            StopTilePulse();
            StopZoneGlow();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = OneMoreOneLessRoundGenerator.Create(_game.Progress.OneMoreOneLessLevel, _rng, _previousIsMore);
            _previousIsMore = _round.IsMore;
            _ladder = new HelpLadder();
            _roundOver = false;

            _pondField.gameObject.SetActive(_round.UseDrag);
            _problemField.gameObject.SetActive(!_round.UseDrag);
            _answerField.gameObject.SetActive(!_round.UseDrag);

            if (_round.UseDrag)
            {
                ShowPond(_round);
                _duck.enabled = false;
            }
            else
            {
                ShowProblem(_round);
                ShowRoundAnswers(_round);
                SetAnswersInteractable(false);
            }

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.IsMore ? "onemoreoneless_more" : "onemoreoneless_less");
            _eva.SetTalking(false);

            if (_round.UseDrag) _duck.enabled = true;
            else SetAnswersInteractable(true);
        }

        // --- Levels 1-3: drag the duck -------------------------------------------------------------------------

        private void BuildPond()
        {
            var pondGo = new GameObject("Pond", typeof(RectTransform), typeof(Image));
            pondGo.transform.SetParent(_pondField, false);
            var pondRect = (RectTransform)pondGo.transform;
            pondRect.anchorMin = pondRect.anchorMax = pondRect.pivot = new Vector2(0.5f, 0.5f);
            pondRect.anchoredPosition = PondCenter;
            pondRect.sizeDelta = new Vector2(PondWidth, PondHeight);
            _pondImage = pondGo.GetComponent<Image>();
            _pondImage.sprite = EvaUi.Sprite("world/pond");
            _pondImage.type = Image.Type.Simple;
            _pondImage.raycastTarget = false;

            var trayGo = new GameObject("Tray", typeof(RectTransform), typeof(Image));
            trayGo.transform.SetParent(_pondField, false);
            var trayRect = (RectTransform)trayGo.transform;
            trayRect.anchorMin = trayRect.anchorMax = trayRect.pivot = new Vector2(0.5f, 0.5f);
            trayRect.anchoredPosition = TraySlot;
            trayRect.sizeDelta = new Vector2(TraySize, TraySize);
            _trayImage = trayGo.GetComponent<Image>();
            _trayImage.sprite = EvaUi.Sprite("icons/tray");
            _trayImage.preserveAspect = true;
            _trayImage.raycastTarget = false;

            _pondIconsRoot = new GameObject("PondIcons", typeof(RectTransform)).GetComponent<RectTransform>();
            _pondIconsRoot.SetParent(_pondField, false);
            _pondIconsRoot.anchorMin = _pondIconsRoot.anchorMax = _pondIconsRoot.pivot = new Vector2(0.5f, 0.5f);
            _pondIconsRoot.anchoredPosition = PondCenter;

            _duck = DragItem.Create(_pondField, "duck", EvaUi.Sprite("objects/duck"), Vector2.zero, DuckSize);
            _duckImage = _duck.GetComponent<Image>();
            _duck.BeginDrag += _ => OnDuckBeginDrag();
            _duck.EndDrag += _ => OnDuckEndDrag();
        }

        private void ShowPond(OneMoreOneLessRound round)
        {
            StopZoneGlow();
            ClearChildren(_pondIconsRoot);
            // The static ducks already settled in the pond: StartCount of them when the child is adding one
            // (the draggable duck starts outside, in the tray), or StartCount - 1 when the child is taking one
            // away (the draggable duck is the pond's own Nth duck, so the visible total still reads StartCount).
            var staticCount = round.IsMore ? round.StartCount : round.StartCount - 1;
            PopulateObjectGrid(_pondIconsRoot, round.Object, staticCount, PondIconSize, PondIconsPerRow, PondIconPitch);

            _duckStart = round.IsMore ? TraySlot : PondCenter;
            _duckTarget = round.IsMore ? PondCenter : TraySlot;
            _duck.Rect.anchoredPosition = _duckStart;
            _duck.Rect.localScale = Vector3.one;
            _duckImage.color = Color.white;
        }

        private void OnDuckBeginDrag()
        {
            StopZoneGlow();
        }

        private void OnDuckEndDrag()
        {
            if (_round == null || _roundOver || !_round.UseDrag) return;
            var current = _duck.Rect.anchoredPosition;
            var dx = current.x - _duckTarget.x;
            var dy = current.y - _duckTarget.y;
            if (dx * dx + dy * dy <= SnapRadius * SnapRadius) _runner.StartCoroutine(OnCorrectDrag());
            else _runner.StartCoroutine(SpringBackAndMistake());
        }

        private IEnumerator SpringBackAndMistake()
        {
            _duck.enabled = false;
            yield return LerpPosition(_duck.Rect, _duck.Rect.anchoredPosition, _duckStart, SpringBackSeconds);
            _duck.enabled = true;
            HandleDragMistake();
        }

        private void HandleDragMistake()
        {
            _eva.Angry();
            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_duck.Rect, WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunDragHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDragDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the correct target - the pond ("one more") or the tray ("one less") - glows.
        private IEnumerator RunDragHint()
        {
            _duck.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("onemoreoneless_hint");
            var lead = _game.Voice.Duration("onemoreoneless_hint") + Voice.BreathSeconds;

            var glowTarget = _round.IsMore ? _pondImage : _trayImage;
            _zoneGlowRoutine = _runner.StartCoroutine(GlowZone(glowTarget, Mathf.Max(lead, HintGlowSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            StopZoneGlow();
            _duck.enabled = true;
        }

        private IEnumerator GlowZone(Image image, float duration)
        {
            var original = image.color;
            var glow = new Color(1f, 0.9f, 0.4f, Mathf.Max(original.a, 0.85f));
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        private void StopZoneGlow()
        {
            if (_zoneGlowRoutine != null)
            {
                _runner.StopCoroutine(_zoneGlowRoutine);
                _zoneGlowRoutine = null;
            }
        }

        // 3rd mistake: the hand carries the duck to the target once, then resets it to its start - the child
        // still has to drag it themselves (per the plan's own "child repeats/confirms" note for this game).
        private IEnumerator RunDragDemonstrate()
        {
            _duck.enabled = false;
            _eva.SetTalking(true);
            _game.Voice.Say("onemoreoneless_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("onemoreoneless_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            yield return _hand.MoveTo(_duckStart, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            yield return LerpDuckWithHand(_duckStart, _duckTarget, DragMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _hand.Hide();
            _duck.Rect.anchoredPosition = _duckStart;
            _duck.enabled = true;
        }

        private IEnumerator LerpDuckWithHand(Vector2 from, Vector2 to, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / seconds);
                var pos = Vector2.Lerp(from, to, k);
                _duck.Rect.anchoredPosition = pos;
                _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(pos);
                yield return null;
            }
            _duck.Rect.anchoredPosition = to;
            _hand.Rect.anchoredPosition = PointerHand.HandPositionFor(to);
        }

        private static IEnumerator LerpPosition(RectTransform target, Vector2 from, Vector2 to, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                target.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            target.anchoredPosition = to;
        }

        private IEnumerator OnCorrectDrag()
        {
            _roundOver = true;
            _duck.enabled = false;
            StopZoneGlow();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _duck.Rect.anchoredPosition = _duckTarget;
            _runner.StartCoroutine(PopPulse(_duck.Rect, _duckImage, 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.OneMoreOneLessLevel = DifficultyLadder.RecordRound(_game.Progress.OneMoreOneLessBuffer, _game.Progress.OneMoreOneLessLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _duck.Rect.position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        // --- Levels 4-6: tap the numeral answer -----------------------------------------------------------------

        private void ShowProblem(OneMoreOneLessRound round)
        {
            ClearChildren(_problemField);
            var numeral = EvaUi.Numeral(_problemField, "StartNumeral", 90);
            var rect = (RectTransform)numeral.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 260f);
            rect.sizeDelta = new Vector2(150f, 120f);
            numeral.text = round.StartCount.ToString();
        }

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

        private void ShowRoundAnswers(OneMoreOneLessRound round)
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

        private void SetAnswersInteractable(bool interactable)
        {
            for (var i = 0; i < _tileButtons.Length; i++)
                if (!_tileTried[i]) _tileButtons[i].interactable = interactable;
        }

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
            if (_round == null || _roundOver || _round.UseDrag || !_tileButtons[i].interactable) return;
            if (_round.Choices[i] == _round.TargetCount) _runner.StartCoroutine(OnCorrectTile(i));
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
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        private IEnumerator RunHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("onemoreoneless_tilehint");
            var leadRemaining = _game.Voice.Duration("onemoreoneless_tilehint") + Voice.BreathSeconds;

            var correctIndex = CorrectTileIndex();
            yield return _hand.MoveTo(_tiles[correctIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        private IEnumerator RunDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("onemoreoneless_tiledemo");
            yield return new WaitForSeconds(_game.Voice.Duration("onemoreoneless_tiledemo") + Voice.BreathSeconds);
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
                if (_round.Choices[i] == _round.TargetCount) return i;
            return -1;
        }

        private IEnumerator OnCorrectTile(int i)
        {
            _roundOver = true;
            SetAllTilesInteractable(false);
            StopTilePulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_tiles[i], _tileImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.OneMoreOneLessLevel = DifficultyLadder.RecordRound(_game.Progress.OneMoreOneLessBuffer, _game.Progress.OneMoreOneLessLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        private IEnumerator AdvanceRound()
        {
            _roundIndex++;
            if (_roundIndex >= OneMoreOneLessRoundGenerator.RoundsPerSession) yield return EndSession();
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
                EndButtonPositions[1], EndButtonSize, GoHomeAfterSession);

            _endPanel.SetActive(false);
        }

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.School);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _pondField.gameObject.SetActive(false);
            _problemField.gameObject.SetActive(false);
            _answerField.gameObject.SetActive(false);
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

        private static void PopulateObjectGrid(Transform parent, CountObject obj, int count, float iconSize, int perRow, float pitch)
        {
            var rows = (count + perRow - 1) / perRow;
            for (var i = 0; i < count; i++)
            {
                var row = i / perRow;
                var inRow = row == rows - 1 ? count - row * perRow : perRow;
                var col = i % perRow;
                var x = (col - (inRow - 1) / 2f) * pitch;
                var y = ((rows - 1) / 2f - row) * pitch;

                var go = new GameObject("Object" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(iconSize, iconSize);
                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("objects/" + obj.ToString().ToLowerInvariant());
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

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
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

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
