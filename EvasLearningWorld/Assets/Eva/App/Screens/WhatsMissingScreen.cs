using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Playground's third game (spec 4.1, build order: What's Missing? is 3rd). A group of shapes is shown, then
    // one is taken away; the child taps which one, among choices, is gone. Same TAP-THE-TARGET shell as
    // PatternCompletionScreen (sequence display row + choice tile row, help ladder, coin payout, difficulty
    // ladder, end panel), with an added show/hide beat: the full set is shown for the round's exposure time, then
    // one slot goes blank before the choices enable. See WhatsMissingRoundGenerator (Rules/WhatsMissing.cs) for
    // the level table.
    public sealed class WhatsMissingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        // Same geometry as PatternCompletionScreen (see there for the Hud-clearance reasoning): both rows keep
        // their whole height at y <= 165.
        private const int MaxSetTiles = 5;
        private const float SetTileSize = 90f;
        private const float SetGap = 15f;
        private const float SetCenterX = -200f;
        private const float SetY = 120f;

        private const int MaxChoiceTiles = 4;
        private const float ChoiceTileSize = 240f; // EvaUi.MinTap
        private const float ChoiceCenterX = -200f;
        private const float ChoiceY = -150f;
        private static readonly float[] ChoiceOffsets4 = { -390f, -130f, 130f, 390f };
        private static readonly float[] ChoiceOffsets3 = { -280f, 0f, 280f };
        private const float WobbleSeconds = 0.4f;
        private const float ReplaySeconds = 1.2f;

        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _setField, _choiceField;
        private GameObject _endPanel;

        private RectTransform[] _setTiles;
        private Image[] _setImages;

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
        private WhatsMissingRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPlaygroundBackground();
            BuildEva();
            _setField = CreateFullRectContainer("SetField");
            _choiceField = CreateFullRectContainer("ChoiceField");
            BuildSetTiles();
            BuildChoiceTiles();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.WhatsMissing);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopTilePulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = WhatsMissingRoundGenerator.Create(_game.Progress.WhatsMissingLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;

            ShowFullSet(_round);
            SetChoiceFieldVisible(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("whatsmissing_watch");
            var exposure = WhatsMissingRoundGenerator.ExposureSecondsFor(_game.Progress.WhatsMissingLevel);
            yield return new WaitForSeconds(exposure);
            HideMissingSlot(_round);

            SetChoiceFieldVisible(true);
            ShowRoundChoices(_round);
            SetChoicesInteractable(false);
            yield return _game.Voice.SayAndWait("whatsmissing_find");
            _eva.SetTalking(false);
            SetChoicesInteractable(true);
        }

        // --- Set display (decorative, never tappable) --------------------------------------------------------

        private void BuildSetTiles()
        {
            _setTiles = new RectTransform[MaxSetTiles];
            _setImages = new Image[MaxSetTiles];
            for (var i = 0; i < MaxSetTiles; i++)
            {
                var tile = new GameObject("SetTile" + i, typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(_setField, false);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(SetTileSize, SetTileSize);

                var image = tile.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;

                _setTiles[i] = rect;
                _setImages[i] = image;
            }
        }

        private void ShowFullSet(WhatsMissingRound round)
        {
            var count = round.Shown.Length;
            var totalWidth = count * SetTileSize + (count - 1) * SetGap;
            var startX = SetCenterX - totalWidth / 2f + SetTileSize / 2f;
            for (var i = 0; i < MaxSetTiles; i++)
            {
                _setTiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _setTiles[i].anchoredPosition = new Vector2(startX + i * (SetTileSize + SetGap), SetY);
                _setImages[i].sprite = EvaUi.Sprite("pattern/shape_" + round.Shown[i].ToLowerInvariant());
            }
        }

        private void HideMissingSlot(WhatsMissingRound round) => _setImages[round.MissingIndex].sprite = EvaUi.Sprite("icons/blank_tile");

        private void RestoreMissingSlot(WhatsMissingRound round) => _setImages[round.MissingIndex].sprite = EvaUi.Sprite("pattern/shape_" + round.Missing.ToLowerInvariant());

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

        private void ShowRoundChoices(WhatsMissingRound round)
        {
            StopTilePulse();
            _choiceTried = new bool[MaxChoiceTiles];
            var count = round.Choices.Length;
            var offsets = count == 3 ? ChoiceOffsets3 : ChoiceOffsets4;
            for (var i = 0; i < MaxChoiceTiles; i++)
            {
                _choiceTiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _choiceTiles[i].anchoredPosition = new Vector2(ChoiceCenterX + offsets[i], ChoiceY);
                _choiceImages[i].sprite = EvaUi.Sprite("pattern/shape_" + round.Choices[i].ToLowerInvariant());
                _choiceImages[i].color = Color.white;
                _choiceTiles[i].localRotation = Quaternion.identity;
                _choiceTiles[i].localScale = Vector3.one;
                _choiceButtons[i].interactable = false;
            }
        }

        private void SetChoiceFieldVisible(bool visible) => _choiceField.gameObject.SetActive(visible);

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
            if (_round.Choices[i] == _round.Missing) _runner.StartCoroutine(OnCorrectChoice(i));
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
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunDemonstrate());
                    break;
            }
        }

        // 2nd mistake: the original set briefly replays (the missing slot fills back in, then goes blank again)
        // before the choices re-enable, per the plan's own Hint note.
        private IEnumerator RunHint()
        {
            SetAllChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whatsmissing_hint");
            RestoreMissingSlot(_round);
            yield return new WaitForSeconds(Mathf.Max(ReplaySeconds, _game.Voice.Duration("whatsmissing_hint") + Voice.BreathSeconds));
            HideMissingSlot(_round);
            _eva.SetTalking(false);
            SetAllChoicesInteractable(true);
        }

        // 3rd mistake: the hand taps the correct choice, then only that tile stays interactable.
        private IEnumerator RunDemonstrate()
        {
            SetAllChoicesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("whatsmissing_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("whatsmissing_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var correctIndex = CorrectChoiceIndex();
            yield return _hand.MoveTo(_choiceTiles[correctIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_choiceTiles[correctIndex]));
            SetOnlyChoiceInteractable(correctIndex);
        }

        private int CorrectChoiceIndex()
        {
            for (var i = 0; i < _round.Choices.Length; i++)
                if (_round.Choices[i] == _round.Missing) return i;
            return -1;
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

            _game.Progress.WhatsMissingLevel = DifficultyLadder.RecordRound(_game.Progress.WhatsMissingBuffer, _game.Progress.WhatsMissingLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _choiceTiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= WhatsMissingRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.Playground);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _setField.gameObject.SetActive(!ended);
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
