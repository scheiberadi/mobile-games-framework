using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // COVERING presenter (Zoo & Farm Covering, Rules/Covering.cs): "which animals have feathers?". On the right a big picture of a texture (fur,
    // feathers, skin or scales) with a small bubble for each animal to find; on the left a grid of animals. Eva asks, in words only, to find
    // N animals with that texture. The child taps animals: a tapped animal fades and gets a green tick (tap again to take it back), and a
    // bubble on the right fills. At N the guess is checked:
    //   right  - the animals hop and call out, Eva cheers, coins, next texture with a fresh grid;
    //   wrong  - Eva says it is not quite right, check again.
    // Half a minute of nothing does the same kind of nudging. The game steps in more each time (CoveringGuess.Escalate): 1st it asks again,
    // 2nd it says what is wrong ("It's a cow! It doesn't have feathers!", the cow is taken back) or points at one animal still missing, 3rd it
    // picks the right animals itself (and counts as a demonstration for the level ladder and the coins).
    //
    // The ladder is recorded per round. A session is CoveringRules.RoundsPerSession textures.
    //
    // Layout (1440 x 900 frame, y in [-450, 450]): the animals on 240-unit tap tiles in two rows (y 50 and -190, 3 or 4 columns, clear of the
    // Home and Back buttons above the top row); the texture badge (380) at (500, 60); the bubbles below it at y -170, clear of the pair
    // standing bottom-right.
    public sealed class GuessCoveringScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private sealed class Cell
        {
            public Button Button;
            public Image Picture, Plate, Tick;
            public RectTransform Rect;
        }

        private const int MaxCells = CoveringRules.MaxGrid;
        private const int MaxSlots = 4;
        private const float CellSize = EvaUi.MinTap; // 240
        private const float ListCentreX = -210f;
        private static readonly float[] RowY = { 50f, -190f };
        private static readonly Vector2 BadgeCentre = new Vector2(500f, 60f);
        private const float BadgeSize = 380f;
        private const float TextureSize = 250f;
        private const float SlotY = -170f, SlotSize = 90f, SlotPitch = 105f;
        private const float FadedAlpha = 0.35f;
        private const float HopSeconds = 0.3f;
        private const float MaxSoundWait = 1.4f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private readonly ScreenId _selfId;
        private readonly ScreenId _homeScreenId;
        private readonly string _backgroundSprite;
        private readonly Func<PlayerProgress, int> _getLevel;
        private readonly Action<PlayerProgress, int> _setLevel;
        private readonly Func<PlayerProgress, List<bool>> _getBuffer;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _cellField, _badgeField;
        private Image _textureImage;
        private GameObject _endPanel;
        private Vector3 _evaBaseScale = Vector3.one;

        private readonly Cell[] _cells = new Cell[MaxCells];
        private Image[] _slots, _slotTicks;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousTexture;
        private CoveringRound _round;
        private CoveringGuess _guess;
        private bool _busy, _over;
        private float _lastActivity;
        private int _rightLineIndex;

        public GuessCoveringScreen(ScreenId selfId, ScreenId homeScreenId, string backgroundSprite,
            Func<PlayerProgress, int> getLevel, Action<PlayerProgress, int> setLevel, Func<PlayerProgress, List<bool>> getBuffer)
        {
            _selfId = selfId;
            _homeScreenId = homeScreenId;
            _backgroundSprite = backgroundSprite;
            _getLevel = getLevel;
            _setLevel = setLevel;
            _getBuffer = getBuffer;
        }

        // Read-only state, exposed so tests can drive the screen.
        public CoveringGuess Guess => _guess;
        public CoveringRound Round => _round;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddPictureBackground();
            BuildEva();
            _badgeField = CreateFullRectContainer("BadgeField");
            _cellField = CreateFullRectContainer("CellField");
            BuildBadge();
            BuildCells();
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
            _previousTexture = null;
            _over = false;
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(IdleWatch());
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = CoveringRules.Create(_getLevel(_game.Progress), _rng, _previousTexture);
            _previousTexture = _round.Texture;
            _guess = new CoveringGuess(_round, _rng);
            _lastActivity = Time.time;
            ShowRound();
            SetCellsEnabled(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait(CoveringRules.QuestionKey(_round));
            _eva.SetTalking(false);
            _lastActivity = Time.time;
            _busy = false;
            SetCellsEnabled(true);
        }

        // --- Building ----------------------------------------------------------------------------------------

        private void BuildBadge()
        {
            var plate = NewImage("BadgePlate", _badgeField, "icons/blank_tile", BadgeCentre, BadgeSize);
            _textureImage = NewImage("Texture", plate.rectTransform, null, Vector2.zero, TextureSize);

            _slots = new Image[MaxSlots];
            _slotTicks = new Image[MaxSlots];
            for (var i = 0; i < MaxSlots; i++)
            {
                _slots[i] = NewImage("Slot" + i, _badgeField, "icons/dot", Vector2.zero, SlotSize);
                _slotTicks[i] = NewImage("SlotTick", _slots[i].rectTransform, "icons/check", Vector2.zero, SlotSize);
            }
        }

        private void BuildCells()
        {
            for (var i = 0; i < MaxCells; i++)
            {
                var cell = new Cell();
                cell.Plate = NewImage("Plate" + i, _cellField, "icons/blank_tile", Vector2.zero, CellSize - 10f);
                var index = i;
                cell.Button = EvaUi.IconButton(_cellField, "Animal" + i, EvaUi.Sprite("icons/dot"), new Vector2(0.5f, 0.5f), Vector2.zero, CellSize, () => OnCellTap(index));
                cell.Rect = (RectTransform)cell.Button.transform;
                cell.Picture = cell.Button.GetComponent<Image>();
                cell.Tick = NewImage("Tick", cell.Rect, "icons/check", new Vector2(70f, -70f), 90f);
                cell.Picture.rectTransform.sizeDelta = new Vector2(CellSize, CellSize);
                _cells[i] = cell;
                cell.Button.gameObject.SetActive(false);
                cell.Plate.gameObject.SetActive(false);
            }
        }

        private static Image NewImage(string name, Transform parent, string sprite, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            if (sprite != null) image.sprite = EvaUi.Sprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = false; // decorative: never a TapTarget, never swallows a tap
            return image;
        }

        private void ShowRound()
        {
            _textureImage.sprite = EvaUi.Sprite("zoofarm/covering_" + _round.Texture);
            _runner.StartCoroutine(PopIn(_textureImage.rectTransform, 0.3f));

            var count = _round.AnimalIds.Length;
            var columns = count <= 6 ? 3 : 4;
            for (var i = 0; i < MaxCells; i++)
            {
                var active = i < count;
                _cells[i].Button.gameObject.SetActive(active);
                _cells[i].Plate.gameObject.SetActive(active);
                if (!active) continue;
                var column = i % columns;
                var position = new Vector2(ListCentreX + (column - (columns - 1) / 2f) * CellSize, RowY[i / columns]);
                _cells[i].Rect.anchoredPosition = position;
                _cells[i].Plate.rectTransform.anchoredPosition = position;
                _cells[i].Picture.sprite = EvaUi.Sprite("zoofarm/animal_" + _round.AnimalIds[i]);
                _cells[i].Picture.preserveAspect = true;
                _cells[i].Rect.localScale = Vector3.one;
                _cells[i].Rect.localRotation = Quaternion.identity;
                RefreshCell(i);
                _runner.StartCoroutine(PopIn(_cells[i].Rect, 0.25f + 0.03f * i));
            }
            RefreshSlots();
        }

        // A picked animal is faded and ticked.
        private void RefreshCell(int i)
        {
            var picked = _guess.Selected[i];
            _cells[i].Picture.color = new Color(1f, 1f, 1f, picked ? FadedAlpha : 1f);
            _cells[i].Tick.gameObject.SetActive(picked);
        }

        // One bubble for each animal to find; one fills for each animal picked.
        private void RefreshSlots()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                var active = i < _round.Wanted;
                _slots[i].gameObject.SetActive(active);
                if (!active) continue;
                _slots[i].rectTransform.anchoredPosition = new Vector2(BadgeCentre.x + (i - (_round.Wanted - 1) / 2f) * SlotPitch, SlotY);
                var filled = i < _guess.SelectedCount;
                _slots[i].color = filled ? new Color(0.55f, 0.85f, 0.45f) : new Color(1f, 0.96f, 0.86f);
                _slotTicks[i].gameObject.SetActive(filled);
            }
        }

        private void SetCellsEnabled(bool enabled)
        {
            for (var i = 0; i < MaxCells; i++) _cells[i].Button.interactable = enabled;
        }

        // --- The child's guess -------------------------------------------------------------------------------

        private void OnCellTap(int i)
        {
            if (_busy || _over || _guess == null) return;
            _lastActivity = Time.time;
            var verdict = _guess.Toggle(i);
            RefreshCell(i);
            RefreshSlots();
            _runner.StartCoroutine(PopPulse(_cells[i].Rect, 1.12f, 0.2f));
            if (verdict == Verdict.Right) _runner.StartCoroutine(Celebrate());
            else if (verdict == Verdict.Wrong) _runner.StartCoroutine(Step(true));
        }

        // Half a minute of nothing: the game steps in.
        private IEnumerator IdleWatch()
        {
            while (true)
            {
                yield return null;
                if (_busy || _over || _guess == null) continue;
                if (Time.time - _lastActivity < CoveringRules.IdleSeconds) continue;
                _lastActivity = Time.time;
                yield return Step(false);
            }
        }

        // The game steps in after a wrong guess (afterGuess) or a quiet half minute.
        private IEnumerator Step(bool afterGuess)
        {
            _busy = true;
            SetCellsEnabled(false);
            var notice = _guess.Escalate();
            _eva.SetTalking(true);

            if (notice.Level >= 3)
            {
                yield return _game.Voice.SayAndWait("covering_help");
                _eva.SetTalking(false);
                yield return TickTheRightOnes();
                yield return Celebrate();
                yield break;
            }

            if (notice.Level == 2 && notice.WrongIndex >= 0)
            {
                var wrong = notice.WrongIndex;
                _game.Sfx.Retry();
                _runner.StartCoroutine(Wobble(_cells[wrong].Rect, 0.5f));
                RefreshCell(wrong);
                RefreshSlots();
                yield return _game.Voice.SayAndWait("zoofarm_animal_" + _round.AnimalIds[wrong]);
                yield return _game.Voice.SayAndWait(CoveringRules.NotKey(_round.Texture));
            }
            else if (notice.Level == 2 && notice.HintIndex >= 0)
            {
                if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                _runner.StartCoroutine(PopPulse(_cells[notice.HintIndex].Rect, 1.25f, 0.35f));
                yield return _game.Voice.SayAndWait("zoofarm_food_hint");
            }
            else if (afterGuess)
            {
                _game.Sfx.Retry();
                _eva.Angry();
                yield return _game.Voice.SayAndWait("covering_check");
            }
            else yield return _game.Voice.SayAndWait(CoveringRules.QuestionKey(_round));

            _eva.SetTalking(false);
            _lastActivity = Time.time;
            _busy = false;
            SetCellsEnabled(true);
        }

        // The game picks the right animals itself, one by one.
        private IEnumerator TickTheRightOnes()
        {
            for (var i = 0; i < _round.AnimalIds.Length; i++)
            {
                RefreshCell(i);
                if (!_round.IsCorrect[i]) continue;
                _game.Sfx.Pick();
                _runner.StartCoroutine(PopPulse(_cells[i].Rect, 1.15f, 0.25f));
                yield return new WaitForSeconds(0.3f);
            }
            RefreshSlots();
        }

        // --- Round and session end ---------------------------------------------------------------------------

        // The animals that have the texture hop and call out one after another, Eva cheers, coins, and on to the next texture.
        private IEnumerator Celebrate()
        {
            _busy = true;
            SetCellsEnabled(false);
            var clean = !_guess.SolvedByGame;
            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            var payout = _guess.SolvedByGame ? CoinPayout.Demonstrated : (_guess.Notifications == 0 ? CoinPayout.Clean : CoinPayout.Assisted);
            _setLevel(_game.Progress, DifficultyLadder.RecordRound(_getBuffer(_game.Progress), _getLevel(_game.Progress), clean));
            _runner.StartCoroutine(PayCoins(payout, _textureImage.transform.position));

            for (var i = 0; i < _round.AnimalIds.Length; i++)
            {
                if (!_round.IsCorrect[i]) continue;
                var id = _round.AnimalIds[i];
                var heard = _game.Sfx.PlayAnimal(id);
                if (heard <= 0f && ZooFarmAnimals.All.Any(a => a.Id == id && a.RealmOf == Realm.Sea)) { _game.Sfx.Splash(); heard = 0.8f; } // a swimmer with no call of its own
                var wait = Mathf.Clamp(heard, HopSeconds * 2f, MaxSoundWait);
                _runner.StartCoroutine(Hops(_cells[i].Rect, _cells[i].Rect.anchoredPosition, Mathf.Clamp(Mathf.RoundToInt(wait / HopSeconds), 2, 5)));
                yield return new WaitForSeconds(wait);
            }

            _rightLineIndex = _rightLineIndex % 3 + 1;
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
            _over = true;
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
            _cellField.gameObject.SetActive(!ended);
            _badgeField.gameObject.SetActive(!ended);
            _endPanel.SetActive(ended);
        }

        // --- Eva ---------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens ------------------------------------------------------------------------------------

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

        // Scales the target up to `peak` times and back (its own scale at call time is the base).
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
