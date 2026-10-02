using System.Collections;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Scrambled Word (M4 School, Literacy): JigsawScreen's own "snap when close to its own correct region" drag
    // mechanic (read that one first) with letters standing in for puzzle pieces - each letter has a fixed home
    // slot (its position in reading order) and starts scattered in the tray under a shuffled slot. A picture of
    // the target word ("wordtoimage/<word>", Word to Image's own sprite set) sits above the slots as a memory
    // aid, and Eva speaks the whole word aloud (word_<Word>) before the tiles go interactive - ordering letters
    // purely from a spoken word with no visual anchor would be too hard for this age group. Same two-step help
    // ladder as Jigsaw: Hint glows the correct slot for the currently-held/nearest letter, Demo drags exactly
    // that one letter home and hands control back for the rest.
    public sealed class ScrambledWordScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const int MaxLetters = 3;
        private const float HintGlowSeconds = 1.2f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        // The target word's picture, above the board - decorative memory aid, never a tap target.
        private const float TargetTileSize = 150f;
        private const float TargetY = 320f;

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;
        private RectTransform _targetField, _boardField, _letterField;
        private Image _targetImage;
        private Image[] _slotImages;
        private DragItem[] _letterItems;
        private Image[] _letterImages;
        private bool[] _placed;
        private int _lastTouchedIndex = -1;
        private Coroutine _slotGlowRoutine;
        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;

        private System.Random _rng;
        private int _roundIndex;
        private string _previousWord;
        private ScrambledWordRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddSchoolBackground();
            BuildEva();
            _targetField = CreateFullRectContainer("TargetField");
            _boardField = CreateFullRectContainer("BoardField");
            _letterField = CreateFullRectContainer("LetterField");
            BuildTargetTile();
            BuildSlots();
            BuildLetters();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.ScrambledWord);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _previousWord = null;
            _rightLineIndex = 0;
            StopSlotGlow();
            _lastTouchedIndex = -1;
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = ScrambledWordRoundGenerator.Create(_game.Progress.ScrambledWordLevel, _rng, _previousWord);
            _previousWord = _round.Word;
            _ladder = new HelpLadder();
            _roundOver = false;
            _lastTouchedIndex = -1;

            _targetImage.sprite = EvaUi.Sprite("wordtoimage/" + _round.Word);
            ShowRoundLetters(_round);
            SetLettersInteractable(false);

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("scrambledword_find");
            yield return _game.Voice.SayAndWait("word_" + _round.Word);
            _eva.SetTalking(false);
            SetLettersInteractable(true);
        }

        // --- Target tile (decorative, never tappable) ----------------------------------------------------------

        private void BuildTargetTile()
        {
            var tile = new GameObject("TargetTile", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(_targetField, false);
            var rect = (RectTransform)tile.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(ScrambledWordRoundGenerator.BoardCenter.X, TargetY);
            rect.sizeDelta = new Vector2(TargetTileSize, TargetTileSize);

            _targetImage = tile.GetComponent<Image>();
            _targetImage.preserveAspect = true;
            _targetImage.raycastTarget = false;
        }

        // --- Board and letters --------------------------------------------------------------------------------

        private void BuildSlots()
        {
            _slotImages = new Image[MaxLetters];
            for (var i = 0; i < MaxLetters; i++)
            {
                var go = new GameObject("Slot" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_boardField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(ScrambledWordRoundGenerator.SlotSize, ScrambledWordRoundGenerator.SlotSize);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.raycastTarget = false;
                _slotImages[i] = image;
            }
        }

        private void BuildLetters()
        {
            _letterItems = new DragItem[MaxLetters];
            _letterImages = new Image[MaxLetters];
            for (var i = 0; i < MaxLetters; i++)
            {
                var index = i;
                var item = DragItem.Create(_letterField, "letter" + i, EvaUi.Sprite("letters/a"), Vector2.zero, ScrambledWordRoundGenerator.SlotSize);
                item.BeginDrag += _ => OnLetterBeginDrag(index);
                item.EndDrag += _ => OnLetterEndDrag(index);
                _letterItems[i] = item;
                _letterImages[i] = item.GetComponent<Image>();
            }
        }

        private void ShowRoundLetters(ScrambledWordRound round)
        {
            StopSlotGlow();
            var count = round.Letters.Length;
            _placed = new bool[MaxLetters];

            for (var i = 0; i < MaxLetters; i++)
            {
                var active = i < count;
                _slotImages[i].gameObject.SetActive(active);
                _letterItems[i].gameObject.SetActive(active);
                if (!active) continue;

                var letter = round.Letters[i];
                var slotRect = (RectTransform)_slotImages[i].transform;
                slotRect.anchoredPosition = new Vector2(letter.HomePosition.X, letter.HomePosition.Y);
                _slotImages[i].color = new Color(1f, 1f, 1f, 0.35f);

                _letterItems[i].Rect.anchoredPosition = new Vector2(letter.TrayPosition.X, letter.TrayPosition.Y);
                _letterImages[i].sprite = EvaUi.Sprite("letters/" + letter.Letter);
                _letterImages[i].color = Color.white;
                _letterItems[i].enabled = true;
            }
        }

        private void SetLettersInteractable(bool interactable)
        {
            for (var i = 0; i < _round.Letters.Length; i++)
                if (!_placed[i]) _letterItems[i].enabled = interactable;
        }

        // --- Drag events ------------------------------------------------------------------------------------

        private void OnLetterBeginDrag(int i)
        {
            if (_round == null || _roundOver) return;
            _lastTouchedIndex = i;
        }

        private void OnLetterEndDrag(int i)
        {
            if (_round == null || _roundOver || _placed[i]) return;
            var home = _round.Letters[i].HomePosition;
            var current = _letterItems[i].Rect.anchoredPosition;
            var dx = current.x - home.X;
            var dy = current.y - home.Y;
            if (dx * dx + dy * dy <= _round.SnapRadius * _round.SnapRadius) PlaceLetter(i);
            else HandleMistake();
        }

        private void PlaceLetter(int i)
        {
            _placed[i] = true;
            var home = _round.Letters[i].HomePosition;
            _letterItems[i].Rect.anchoredPosition = new Vector2(home.X, home.Y);
            _letterItems[i].enabled = false;
            _slotImages[i].color = new Color(1f, 1f, 1f, 0f);
            _game.Sfx.Coin();
            _runner.StartCoroutine(PopPulse(_letterItems[i].Rect, _letterImages[i], 1.15f, 0.25f));

            if (AllPlaced()) _runner.StartCoroutine(OnWordComplete());
        }

        private bool AllPlaced()
        {
            for (var i = 0; i < _round.Letters.Length; i++) if (!_placed[i]) return false;
            return true;
        }

        // The currently-held (or, once released, most recently touched) letter if it's still unplaced;
        // otherwise the first unplaced letter in reading order.
        private int TargetIndexForHelp()
        {
            if (_lastTouchedIndex >= 0 && _lastTouchedIndex < _round.Letters.Length && !_placed[_lastTouchedIndex]) return _lastTouchedIndex;
            for (var i = 0; i < _round.Letters.Length; i++) if (!_placed[i]) return i;
            return -1;
        }

        private void HandleMistake()
        {
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

        // 2nd mistake: the correct slot for the currently-held/nearest letter glows.
        private IEnumerator RunHint()
        {
            SetLettersInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("scrambledword_hint");
            var lead = _game.Voice.Duration("scrambledword_hint") + Voice.BreathSeconds;

            var index = TargetIndexForHelp();
            if (index >= 0) _slotGlowRoutine = _runner.StartCoroutine(GlowSlot(index, Mathf.Max(lead, HintGlowSeconds)));
            yield return new WaitForSeconds(lead);
            _eva.SetTalking(false);
            yield return new WaitForSeconds(HintGlowSeconds);
            StopSlotGlow();
            SetLettersInteractable(true);
        }

        private IEnumerator GlowSlot(int index, float duration)
        {
            var image = _slotImages[index];
            var original = image.color;
            var glow = new Color(1f, 0.9f, 0.4f, 0.85f);
            for (var t = 0f; t < duration; t += Time.deltaTime)
            {
                image.color = Color.Lerp(original, glow, Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            image.color = original;
        }

        private void StopSlotGlow()
        {
            if (_slotGlowRoutine != null)
            {
                _runner.StopCoroutine(_slotGlowRoutine);
                _slotGlowRoutine = null;
            }
        }

        // 3rd mistake: the hand drags one letter home, then hands control back for the child to finish the
        // rest - never the whole word, since placement is cumulative.
        private IEnumerator RunDemonstrate()
        {
            SetLettersInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("scrambledword_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("scrambledword_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            var index = TargetIndexForHelp();
            if (index >= 0)
            {
                var from = _letterItems[index].Rect.anchoredPosition;
                var home = _round.Letters[index].HomePosition;
                yield return _hand.MoveTo(from, HandMoveSeconds);
                yield return _hand.Tap(HandTapSeconds);
                yield return _hand.MoveTo(new Vector2(home.X, home.Y), HandMoveSeconds);
                _letterItems[index].Rect.anchoredPosition = new Vector2(home.X, home.Y);
                _hand.Hide();
                PlaceLetter(index);
            }
            SetLettersInteractable(true);
        }

        private IEnumerator OnWordComplete()
        {
            _roundOver = true;
            StopSlotGlow();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.ScrambledWordLevel = DifficultyLadder.RecordRound(_game.Progress.ScrambledWordBuffer, _game.Progress.ScrambledWordLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step),
                new Vector2(ScrambledWordRoundGenerator.BoardCenter.X, ScrambledWordRoundGenerator.BoardCenter.Y)));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);

            _roundIndex++;
            if (_roundIndex >= ScrambledWordRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.School);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _targetField.gameObject.SetActive(!ended);
            _boardField.gameObject.SetActive(!ended);
            _letterField.gameObject.SetActive(!ended);
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
