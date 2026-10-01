using System;
using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shared MATCH/SORT presenter (M4.4 Zoo & Farm + Geography, docs/kids-games/full-catalogue-plan.md
    // "6. Zoo & Farm"): one target - a picture, or nothing when MatchRound.TargetSprite is null (Animal ->
    // Sound plays a sound instead; Geography always speaks the target) - and up to 4 choice tiles, one
    // correct. One instance is built per game (registered under its own ScreenId, same as
    // `new BuildingScreen(BuildingId.Playground)`), configured with that game's round generator, ladder and
    // voice lines; only PromptVoiceKey/TargetVoiceKey vary round to round (carried on MatchRound itself).
    // Layout/animation/help-ladder shell is WordToImageScreen's own code, generalised instead of copied
    // eleven times, per the plan's explicit call to build this presenter once.
    public sealed class MatchScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }


        private const float TargetTileSize = 150f;
        private const float TargetX = -200f;
        private const float TargetY = 120f;

        private const int MaxChoiceTiles = 4;
        private const float ChoiceTileSize = 240f;
        private const float ChoiceCenterX = -200f;
        private const float ChoiceY = -150f;
        private static readonly float[] ChoiceOffsets4 = { -390f, -130f, 130f, 390f };
        private static readonly float[] ChoiceOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ChoiceOffsets2 = { -150f, 150f };
        private const float WobbleSeconds = 0.4f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<int, System.Random, string, MatchRound> _generateRound;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;
        private readonly int _roundsPerSession;
        private readonly string _hintVoiceKey, _demoVoiceKey;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _targetField, _choiceField;
        private Image _targetImage;
        private GameObject _endPanel;

        private RectTransform[] _choiceTiles;
        private Image[] _choiceImages;
        private Button[] _choiceButtons;
        private bool[] _choiceTried;
        private Coroutine _tilePulseRoutine;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousTarget;
        private MatchRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public MatchScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<int, System.Random, string, MatchRound> generateRound,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer,
            int roundsPerSession, string hintVoiceKey, string demoVoiceKey)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _generateRound = generateRound;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
            _roundsPerSession = roundsPerSession;
            _hintVoiceKey = hintVoiceKey;
            _demoVoiceKey = demoVoiceKey;
        }

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _targetField = CreateFullRectContainer("TargetField");
            _choiceField = CreateFullRectContainer("ChoiceField");
            BuildTargetTile();
            BuildChoiceTiles();
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
            _previousTarget = null;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = _generateRound(_getLevel(_game.Progress), _rng, _previousTarget);
            _previousTarget = _round.TargetId;
            _ladder = new HelpLadder();
            _roundOver = false;

            _targetImage.enabled = _round.TargetSprite != null;
            if (_round.TargetSprite != null) _targetImage.sprite = EvaUi.Sprite(_round.TargetSprite);
            ShowRoundChoices(_round);
            SetChoicesInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(_round.PromptVoiceKey);
            yield return _game.Voice.SayAndWait(_round.TargetVoiceKey);
            _eva.SetTalking(false);
            SetChoicesInteractable(true);
        }

        // --- Target tile (decorative, never tappable; hidden entirely for an audio/spoken-only round) --------

        private void BuildTargetTile()
        {
            var tile = new GameObject("TargetTile", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(_targetField, false);
            var rect = (RectTransform)tile.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(TargetX, TargetY);
            rect.sizeDelta = new Vector2(TargetTileSize, TargetTileSize);

            _targetImage = tile.GetComponent<Image>();
            _targetImage.preserveAspect = true;
            _targetImage.raycastTarget = false;
        }

        // --- Choice tiles -------------------------------------------------------------------------------------

        private void BuildChoiceTiles()
        {
            _choiceTiles = new RectTransform[MaxChoiceTiles];
            _choiceImages = new Image[MaxChoiceTiles];
            _choiceButtons = new Button[MaxChoiceTiles];

            for (var i = 0; i < MaxChoiceTiles; i++)
            {
                var tile = new GameObject("Choice" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                tile.transform.SetParent(_choiceField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(ChoiceTileSize, ChoiceTileSize);

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnChoiceTapped(choiceIndex));

                _choiceTiles[i] = rect;
                _choiceImages[i] = image;
                _choiceButtons[i] = button;
            }
        }

        private void ShowRoundChoices(MatchRound round)
        {
            StopTilePulse();
            _choiceTried = new bool[MaxChoiceTiles];
            var count = round.ChoiceSprites.Length;
            var offsets = count == 2 ? ChoiceOffsets2 : count == 3 ? ChoiceOffsets3 : ChoiceOffsets4;
            for (var i = 0; i < MaxChoiceTiles; i++)
            {
                _choiceTiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _choiceTiles[i].anchoredPosition = new Vector2(ChoiceCenterX + offsets[i], ChoiceY);
                _choiceImages[i].sprite = EvaUi.Sprite(round.ChoiceSprites[i]);
                _choiceImages[i].color = Color.white;
                _choiceTiles[i].localRotation = Quaternion.identity;
                _choiceTiles[i].localScale = Vector3.one;
                _choiceButtons[i].interactable = false;
            }
        }

        private void SetChoicesInteractable(bool interactable)
        {
            for (var i = 0; i < _choiceButtons.Length; i++)
                if (!_choiceTried[i]) _choiceButtons[i].interactable = interactable;
        }

        private void SetAllChoicesInteractable(bool interactable)
        {
            for (var i = 0; i < _choiceButtons.Length; i++)
                _choiceButtons[i].interactable = interactable;
        }

        private void SetOnlyChoiceInteractable(int index)
        {
            for (var i = 0; i < _choiceButtons.Length; i++)
                _choiceButtons[i].interactable = i == index;
        }

        private void StopTilePulse()
        {
            if (_tilePulseRoutine != null)
            {
                _runner.StopCoroutine(_tilePulseRoutine);
                _tilePulseRoutine = null;
            }
            if (_choiceTiles != null)
                foreach (var tile in _choiceTiles) tile.localScale = Vector3.one;
        }

        private void OnChoiceTapped(int i)
        {
            if (_round == null || _roundOver || !_choiceButtons[i].interactable) return;
            if (i == _round.CorrectIndex) _runner.StartCoroutine(OnCorrectChoice(i));
            else OnWrongChoice(i);
        }

        private void OnWrongChoice(int i)
        {
            _choiceTried[i] = true;
            _eva.Angry();
            _choiceButtons[i].interactable = false;
            _choiceImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_choiceTiles[i], WobbleSeconds));
                    break;
                case HelpStep.Hint:
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the hand points at (does not tap) the correct choice.
        private IEnumerator RunHint()
        {
            SetAllChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_hintVoiceKey);
            var leadRemaining = _game.Voice.Duration(_hintVoiceKey) + Voice.BreathSeconds;

            yield return _hand.MoveTo(_choiceTiles[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllChoicesInteractable(true);
        }

        // 3rd mistake: the hand taps the correct choice, then only that tile stays interactable.
        private IEnumerator RunDemonstrate()
        {
            SetAllChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say(_demoVoiceKey);
            yield return new WaitForSeconds(_game.Voice.Duration(_demoVoiceKey) + Voice.BreathSeconds);
            _eva.SetTalking(false);

            yield return _hand.MoveTo(_choiceTiles[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_choiceTiles[_round.CorrectIndex]));
            SetOnlyChoiceInteractable(_round.CorrectIndex);
        }

        private IEnumerator OnCorrectChoice(int i)
        {
            _roundOver = true;
            SetAllChoicesInteractable(false);
            StopTilePulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_choiceTiles[i], _choiceImages[i], 1.25f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            var progress = _game.Progress;
            _setLevel(progress, DifficultyLadder.RecordRound(_getBuffer(progress), _getLevel(progress), clean));
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _choiceTiles[i].position));

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

        private void GoHomeAfterSession() => _game.Navigator.Show(_homeScreenId);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _targetField.gameObject.SetActive(!ended);
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
