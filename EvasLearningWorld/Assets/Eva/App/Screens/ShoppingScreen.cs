using System;
using System.Collections;
using EvasLearningWorld.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Shopping (M4.3, Store's second Activity - see ShoppingRoundGenerator's own class comment for why level
    // and mode are the same thing here). Own presenter, never the shared answer-tile-only shape of a single
    // other game: the problem display and the answer shape both change with ShoppingRound.Mode.
    //
    // Recognize/ExactPayment/Addition/Change reuse AdditionScreen's/WhichHasMoreScreen's answer-tile mechanic
    // (read those first) - MaxTiles=6, TileSize=240, the same PositionsFor(count) layouts - but a tile's content
    // is mode-dependent: a bare numeral (Recognize/Change), a single coin/note sprite (ExactPayment), or a pair
    // of coin sprites side by side (Addition's "combo" tiles). ComparePrices/Budget instead reuse
    // WhichHasMoreScreen's direct-tap group-button shape (read that one for the reasoning), generalised from a
    // fixed 2 buttons to up to 3 (Budget's three-item shelf), each button showing an item icon plus a price tag
    // (StoreScreen's own coin-icon-plus-digit convention).
    //
    // Unlike every other screen this session, the correct answer's index is read straight off
    // ShoppingRound.CorrectIndex (the generator already computed it) rather than recomputed here from the
    // round's raw fields - simpler and impossible to drift out of sync with the generator's own guarantee logic.
    //
    // Same two-step help ladder throughout: 1st mistake a gentle Retry, 2nd a Hint (hand points at, does not
    // tap, the correct answer), 3rd a full Demonstrate (hand taps it, only that answer stays interactable).
    // Real coin/note art and item art are a separate content-pipeline prerequisite the plan itself calls out;
    // placeholder sprites cover it meanwhile, same as every other placeholder catalogue this session.
    public sealed class ShoppingScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float ProblemY = 260f;

        // --- Problem display (Recognize/ExactPayment/Addition/Change) -----------------------------------------
        private const float MoneyImageSize = 200f;
        private const float PaidMoneyImageSize = 140f;
        private const float ItemIconSize = 130f;
        private const float PriceTagOffsetY = -100f;
        private const float CoinTagIconSize = 50f;
        private const int PriceFontSize = 60;
        private const float ItemLeftX = -160f;
        private const float PaidRightX = 160f;
        private const float BudgetTagIconSize = 70f;
        private const int BudgetFontSize = 80;

        // --- Answer tiles (Recognize/ExactPayment/Addition/Change) -------------------------------------------
        private const int MaxTiles = 6;
        private const float TileSize = 240f; // EvaUi.MinTap
        private const int TileNumeralFontSize = 70;
        private const float TileCoinSize = 150f;
        private const float ComboCoinSize = 90f;
        private const float ComboCoinOffsetX = 55f;
        private const float CenterX = -200f;
        private static readonly float[] ColOffsets3 = { -280f, 0f, 280f };
        private static readonly float[] ColOffsets2 = { -140f, 140f };
        private const float TopY = 0f;
        private const float BottomY = -280f;
        private const float SingleY = -140f;

        // --- Group buttons (ComparePrices/Budget) -------------------------------------------------------------
        private const int MaxGroupButtons = 3;
        private const float GroupButtonSize = 320f;
        private const float GroupButtonY = -40f;
        private static readonly float[] GroupOffsets2 = { -260f, 260f };
        private static readonly float[] GroupOffsets3 = { -340f, 0f, 340f };
        private const float GroupItemIconSize = 160f;
        private const float GroupItemOffsetY = 40f;
        private const float GroupPriceOffsetY = -70f;

        private const float WobbleSeconds = 0.4f;
        private const float HandMoveSeconds = 0.5f;
        private const float HandTapSeconds = 0.3f;
        private const float HintRestSeconds = 0.5f;

        private const float EndButtonSize = 260f;
        private static readonly Vector2[] EndButtonPositions = { new Vector2(-150f, -290f), new Vector2(150f, -290f) };

        private EvaGame _game;
        private Runner _runner;
        private CharacterRig _eva;

        private RectTransform _problemField;

        private RectTransform _answerField;
        private RectTransform[] _tiles;
        private Image[] _tileImages;
        private TextMeshProUGUI[] _tileNumerals;
        private RectTransform[] _tileCoin0;
        private Image[] _tileCoin0Image;
        private RectTransform[] _tileCoin1;
        private Image[] _tileCoin1Image;
        private Button[] _tileButtons;
        private bool[] _tileTried;
        private Coroutine _tilePulseRoutine;

        private RectTransform _groupField;
        private RectTransform[] _groupRects;
        private Image[] _groupImages;
        private Image[] _groupItemImages;
        private TextMeshProUGUI[] _groupPriceNumerals;
        private Button[] _groupButtons;
        private bool[] _groupTried;
        private Coroutine _groupPulseRoutine;

        private GameObject _endPanel;

        private PointerHand _hand;
        private bool _roundOver;
        private Vector3 _evaBaseScale = Vector3.one;
        private bool _usingGroups;

        private System.Random _rng;
        private int _roundIndex;
        private ShoppingRound _round;
        private HelpLadder _ladder;
        private int _rightLineIndex;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddStoreBackground();
            BuildEva();
            _problemField = CreateFullRectContainer("ProblemField");
            _answerField = CreateFullRectContainer("AnswerField");
            BuildAnswerTiles();
            _groupField = CreateFullRectContainer("GroupField");
            BuildGroupButtons();
            _hand = new PointerHand(Root, _runner);
            BuildEndButtons();
        }

        public override void OnShow()
        {
            _game.TutorialGuide.Refresh(ScreenId.Shopping);
            StartNewSession();
        }

        private void StartNewSession()
        {
            _rng = new System.Random();
            _roundIndex = 0;
            _rightLineIndex = 0;
            StopTilePulse();
            StopGroupPulse();
            if (_hand != null) _hand.Hide();
            if (_eva != null) _eva.Root.localScale = _evaBaseScale;
            SetSessionEnded(false);
            _runner.StartCoroutine(RunRound());
        }

        private IEnumerator RunRound()
        {
            _round = ShoppingRoundGenerator.Create(_game.Progress.ShoppingLevel, _rng);
            _ladder = new HelpLadder();
            _roundOver = false;
            _usingGroups = _round.Mode == ShoppingMode.ComparePrices || _round.Mode == ShoppingMode.Budget;

            ClearChildren(_problemField);
            _answerField.gameObject.SetActive(!_usingGroups);
            _groupField.gameObject.SetActive(_usingGroups);

            if (_usingGroups)
            {
                ShowProblemForGroups(_round);
                ShowGroupChoices(_round);
                SetGroupsInteractable(true);
            }
            else
            {
                ShowProblemForTiles(_round);
                ShowTileChoices(_round);
                SetTilesInteractable(true);
            }

            _eva.SetTalking(true);
            yield return _game.Voice.SayAndWait("shopping_find");
            _eva.SetTalking(false);
        }

        // --- Problem display: tile modes -----------------------------------------------------------------------

        private void ShowProblemForTiles(ShoppingRound round)
        {
            switch (round.Mode)
            {
                case ShoppingMode.Recognize:
                    BuildMoneyImage(round.Denomination, 0f);
                    break;
                case ShoppingMode.ExactPayment:
                case ShoppingMode.Addition:
                    BuildItemWithPriceTag(round.Item, round.Price, 0f);
                    break;
                case ShoppingMode.Change:
                    BuildItemWithPriceTag(round.Item, round.Price, ItemLeftX);
                    BuildMoneyImage(round.Paid, PaidRightX, PaidMoneyImageSize);
                    break;
            }
        }

        private void BuildMoneyImage(int denomination, float centerX, float size = MoneyImageSize)
        {
            var go = new GameObject("Money", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_problemField, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, ProblemY);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = EvaUi.Sprite(MoneySpriteKey(denomination));
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildItemWithPriceTag(CountObject item, int price, float centerX)
        {
            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Image));
            itemGo.transform.SetParent(_problemField, false);
            var itemRect = (RectTransform)itemGo.transform;
            itemRect.anchorMin = itemRect.anchorMax = itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = new Vector2(centerX, ProblemY);
            itemRect.sizeDelta = new Vector2(ItemIconSize, ItemIconSize);
            var itemImage = itemGo.GetComponent<Image>();
            itemImage.sprite = EvaUi.Sprite("objects/" + item.ToString().ToLowerInvariant());
            itemImage.preserveAspect = true;
            itemImage.raycastTarget = false;

            BuildPriceTag(_problemField, price, new Vector2(centerX, ProblemY + PriceTagOffsetY), CoinTagIconSize, PriceFontSize);
        }

        // A coin icon plus a digit, the same convention StoreScreen's shelf price row uses.
        private void BuildPriceTag(Transform parent, int price, Vector2 position, float coinSize, int fontSize)
        {
            var row = new GameObject("PriceTag", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = position;
            rowRect.sizeDelta = new Vector2(ItemIconSize, coinSize);

            var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(row.transform, false);
            var coinRect = (RectTransform)coin.transform;
            coinRect.anchorMin = coinRect.anchorMax = coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.anchoredPosition = new Vector2(-coinSize * 0.7f, 0f);
            coinRect.sizeDelta = new Vector2(coinSize, coinSize);
            var coinImage = coin.GetComponent<Image>();
            coinImage.sprite = EvaUi.Sprite("icons/coin");
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;

            var numeral = EvaUi.Numeral(row.transform, "Digit", fontSize);
            numeral.text = price.ToString();
            var numeralRect = (RectTransform)numeral.transform;
            numeralRect.anchorMin = numeralRect.anchorMax = numeralRect.pivot = new Vector2(0.5f, 0.5f);
            numeralRect.anchoredPosition = new Vector2(coinSize * 0.6f, 0f);
            numeralRect.sizeDelta = new Vector2(90f, coinSize);
        }

        private static string MoneySpriteKey(int denomination) =>
            "money/" + (ShoppingRoundGenerator.IsNote(denomination) ? "note_" : "coin_") + denomination;

        // --- Problem display: group modes ----------------------------------------------------------------------

        private void ShowProblemForGroups(ShoppingRound round)
        {
            if (round.Mode == ShoppingMode.Budget)
                BuildPriceTag(_problemField, round.Budget, new Vector2(0f, ProblemY), BudgetTagIconSize, BudgetFontSize);
            // ComparePrices needs no separate problem display - each group button already shows its own item and price.
        }

        // --- Answer tiles ---------------------------------------------------------------------------------------

        private void BuildAnswerTiles()
        {
            _tiles = new RectTransform[MaxTiles];
            _tileImages = new Image[MaxTiles];
            _tileNumerals = new TextMeshProUGUI[MaxTiles];
            _tileCoin0 = new RectTransform[MaxTiles];
            _tileCoin0Image = new Image[MaxTiles];
            _tileCoin1 = new RectTransform[MaxTiles];
            _tileCoin1Image = new Image[MaxTiles];
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

                var coin0 = new GameObject("Coin0", typeof(RectTransform), typeof(Image));
                coin0.transform.SetParent(tile.transform, false);
                var coin0Rect = (RectTransform)coin0.transform;
                coin0Rect.anchorMin = coin0Rect.anchorMax = coin0Rect.pivot = new Vector2(0.5f, 0.5f);
                coin0Rect.sizeDelta = new Vector2(TileCoinSize, TileCoinSize);
                var coin0Image = coin0.GetComponent<Image>();
                coin0Image.preserveAspect = true;
                coin0Image.raycastTarget = false;

                var coin1 = new GameObject("Coin1", typeof(RectTransform), typeof(Image));
                coin1.transform.SetParent(tile.transform, false);
                var coin1Rect = (RectTransform)coin1.transform;
                coin1Rect.anchorMin = coin1Rect.anchorMax = coin1Rect.pivot = new Vector2(0.5f, 0.5f);
                coin1Rect.sizeDelta = new Vector2(ComboCoinSize, ComboCoinSize);
                var coin1Image = coin1.GetComponent<Image>();
                coin1Image.preserveAspect = true;
                coin1Image.raycastTarget = false;

                var button = tile.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var choiceIndex = i;
                button.onClick.AddListener(() => OnTileTapped(choiceIndex));

                _tiles[i] = rect;
                _tileImages[i] = image;
                _tileNumerals[i] = numeral;
                _tileCoin0[i] = coin0Rect;
                _tileCoin0Image[i] = coin0Image;
                _tileCoin1[i] = coin1Rect;
                _tileCoin1Image[i] = coin1Image;
                _tileButtons[i] = button;
            }
        }

        private void ShowTileChoices(ShoppingRound round)
        {
            StopTilePulse();
            _tileTried = new bool[MaxTiles];
            var count = round.Mode == ShoppingMode.Addition ? round.ComboChoices.Length : round.Choices.Length;
            var positions = PositionsFor(count);
            for (var i = 0; i < MaxTiles; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                _tiles[i].anchoredPosition = positions[i];
                _tileImages[i].color = Color.white;
                _tiles[i].localRotation = Quaternion.identity;
                _tiles[i].localScale = Vector3.one;
                _tileButtons[i].interactable = false;

                switch (round.Mode)
                {
                    case ShoppingMode.Recognize:
                    case ShoppingMode.Change:
                        _tileNumerals[i].gameObject.SetActive(true);
                        _tileCoin0[i].gameObject.SetActive(false);
                        _tileCoin1[i].gameObject.SetActive(false);
                        _tileNumerals[i].text = round.Choices[i].ToString();
                        break;
                    case ShoppingMode.ExactPayment:
                        _tileNumerals[i].gameObject.SetActive(false);
                        _tileCoin1[i].gameObject.SetActive(false);
                        _tileCoin0[i].gameObject.SetActive(true);
                        _tileCoin0[i].anchoredPosition = Vector2.zero;
                        _tileCoin0[i].sizeDelta = new Vector2(TileCoinSize, TileCoinSize);
                        _tileCoin0Image[i].sprite = EvaUi.Sprite(MoneySpriteKey(round.Choices[i]));
                        break;
                    case ShoppingMode.Addition:
                        _tileNumerals[i].gameObject.SetActive(false);
                        _tileCoin0[i].gameObject.SetActive(true);
                        _tileCoin1[i].gameObject.SetActive(true);
                        _tileCoin0[i].anchoredPosition = new Vector2(-ComboCoinOffsetX, 0f);
                        _tileCoin0[i].sizeDelta = new Vector2(ComboCoinSize, ComboCoinSize);
                        _tileCoin0Image[i].sprite = EvaUi.Sprite(MoneySpriteKey(round.ComboChoices[i][0]));
                        _tileCoin1[i].anchoredPosition = new Vector2(ComboCoinOffsetX, 0f);
                        _tileCoin1Image[i].sprite = EvaUi.Sprite(MoneySpriteKey(round.ComboChoices[i][1]));
                        break;
                }
            }
        }

        // Same hand-computed grid shapes as AdditionScreen/NumberHuntScreen's PositionsFor.
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

        private void SetTilesInteractable(bool interactable)
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
            if (_round == null || _roundOver || _usingGroups || !_tileButtons[i].interactable) return;
            if (i == _round.CorrectIndex) _runner.StartCoroutine(OnCorrectTile(i));
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
                    _runner.StartCoroutine(Wobble(_tiles[i]));
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunTileHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunTileDemonstrate());
                    break;
            }
        }

        private IEnumerator RunTileHint()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shopping_hint");
            var leadRemaining = _game.Voice.Duration("shopping_hint") + Voice.BreathSeconds;

            yield return _hand.MoveTo(_tiles[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllTilesInteractable(true);
        }

        private IEnumerator RunTileDemonstrate()
        {
            SetAllTilesInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shopping_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("shopping_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            yield return _hand.MoveTo(_tiles[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _tilePulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_tiles[_round.CorrectIndex]));
            SetOnlyTileInteractable(_round.CorrectIndex);
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

            _game.Progress.ShoppingLevel = DifficultyLadder.RecordRound(_game.Progress.ShoppingBuffer, _game.Progress.ShoppingLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _tiles[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        // --- Group buttons (ComparePrices/Budget) ---------------------------------------------------------------

        private void BuildGroupButtons()
        {
            _groupRects = new RectTransform[MaxGroupButtons];
            _groupImages = new Image[MaxGroupButtons];
            _groupItemImages = new Image[MaxGroupButtons];
            _groupPriceNumerals = new TextMeshProUGUI[MaxGroupButtons];
            _groupButtons = new Button[MaxGroupButtons];

            for (var i = 0; i < MaxGroupButtons; i++)
            {
                var go = new GameObject("GroupButton" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
                go.transform.SetParent(_groupField, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(GroupButtonSize, GroupButtonSize);

                var image = go.GetComponent<Image>();
                image.sprite = EvaUi.Sprite("icons/tile");
                image.type = Image.Type.Simple;

                var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Image));
                itemGo.transform.SetParent(go.transform, false);
                var itemRect = (RectTransform)itemGo.transform;
                itemRect.anchorMin = itemRect.anchorMax = itemRect.pivot = new Vector2(0.5f, 0.5f);
                itemRect.anchoredPosition = new Vector2(0f, GroupItemOffsetY);
                itemRect.sizeDelta = new Vector2(GroupItemIconSize, GroupItemIconSize);
                var itemImage = itemGo.GetComponent<Image>();
                itemImage.preserveAspect = true;
                itemImage.raycastTarget = false;

                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var groupIndex = i;
                button.onClick.AddListener(() => OnGroupTapped(groupIndex));

                _groupRects[i] = rect;
                _groupImages[i] = image;
                _groupItemImages[i] = itemImage;
                _groupButtons[i] = button;

                // The price tag is built fresh each round (BuildPriceTag creates its own hierarchy), so only its
                // numeral needs to be tracked here for cleanup between rounds - see ShowGroupChoices.
            }
        }

        private void ShowGroupChoices(ShoppingRound round)
        {
            StopGroupPulse();
            var items = round.Mode == ShoppingMode.Budget ? round.Items : new[] { round.Item, round.Item2 };
            var prices = round.Mode == ShoppingMode.Budget ? round.Prices : new[] { round.Price, round.Price2 };
            var count = items.Length;
            var offsets = count == 3 ? GroupOffsets3 : GroupOffsets2;

            _groupTried = new bool[MaxGroupButtons];
            for (var i = 0; i < MaxGroupButtons; i++)
            {
                _groupRects[i].gameObject.SetActive(i < count);
                if (i >= count) continue;

                _groupRects[i].anchoredPosition = new Vector2(offsets[i], GroupButtonY);
                _groupImages[i].color = Color.white;
                _groupRects[i].localRotation = Quaternion.identity;
                _groupRects[i].localScale = Vector3.one;
                _groupButtons[i].interactable = false;

                _groupItemImages[i].sprite = EvaUi.Sprite("objects/" + items[i].ToString().ToLowerInvariant());

                var existingTag = _groupRects[i].Find("PriceTag");
                if (existingTag != null) DestroyChild(existingTag.gameObject);
                BuildPriceTag(_groupRects[i], prices[i], new Vector2(0f, GroupPriceOffsetY), CoinTagIconSize * 0.8f, (int)(PriceFontSize * 0.8f));
            }
        }

        private void SetGroupsInteractable(bool interactable)
        {
            for (var i = 0; i < _groupButtons.Length; i++)
                if (!_groupTried[i]) _groupButtons[i].interactable = interactable;
        }

        private void SetAllGroupsInteractable(bool interactable)
        {
            for (var i = 0; i < _groupButtons.Length; i++)
                _groupButtons[i].interactable = interactable;
        }

        private void SetOnlyGroupInteractable(int index)
        {
            for (var i = 0; i < _groupButtons.Length; i++)
                _groupButtons[i].interactable = i == index;
        }

        private void StopGroupPulse()
        {
            if (_groupPulseRoutine != null)
            {
                _runner.StopCoroutine(_groupPulseRoutine);
                _groupPulseRoutine = null;
            }
            if (_groupRects != null)
                foreach (var rect in _groupRects) rect.localScale = Vector3.one;
        }

        private void OnGroupTapped(int i)
        {
            if (_round == null || _roundOver || !_usingGroups || !_groupButtons[i].interactable) return;
            if (i == _round.CorrectIndex) _runner.StartCoroutine(OnCorrectGroup(i));
            else OnWrongGroup(i);
        }

        private void OnWrongGroup(int i)
        {
            _groupTried[i] = true;
            _eva.Angry();
            _groupButtons[i].interactable = false;
            _groupImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);

            var step = _ladder.RecordMistake();
            switch (step)
            {
                case HelpStep.Retry:
                    _game.Sfx.Retry();
                    _game.Voice.Say("count_retry");
                    _runner.StartCoroutine(Wobble(_groupRects[i]));
                    break;
                case HelpStep.Hint:
                    if (EvaUi.Sfx != null) EvaUi.Sfx.Hint();
                    _runner.StartCoroutine(RunGroupHint());
                    break;
                case HelpStep.Demonstrate:
                    _runner.StartCoroutine(RunGroupDemonstrate());
                    break;
            }
        }

        private IEnumerator RunGroupHint()
        {
            SetAllGroupsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shopping_hint");
            var leadRemaining = _game.Voice.Duration("shopping_hint") + Voice.BreathSeconds;

            yield return _hand.MoveTo(_groupRects[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            _hand.Pulse(true);
            if (leadRemaining > HandMoveSeconds) yield return new WaitForSeconds(leadRemaining - HandMoveSeconds);
            yield return new WaitForSeconds(HintRestSeconds);
            _eva.SetTalking(false);
            _hand.Hide();
            SetAllGroupsInteractable(true);
        }

        private IEnumerator RunGroupDemonstrate()
        {
            SetAllGroupsInteractable(false);
            _eva.SetTalking(true);
            _game.Voice.Say("shopping_demo");
            yield return new WaitForSeconds(_game.Voice.Duration("shopping_demo") + Voice.BreathSeconds);
            _eva.SetTalking(false);

            yield return _hand.MoveTo(_groupRects[_round.CorrectIndex].anchoredPosition, HandMoveSeconds);
            yield return _hand.Tap(HandTapSeconds);
            _hand.Hide();
            _groupPulseRoutine = _runner.StartCoroutine(IdlePulseLoop(_groupRects[_round.CorrectIndex]));
            SetOnlyGroupInteractable(_round.CorrectIndex);
        }

        private IEnumerator OnCorrectGroup(int i)
        {
            _roundOver = true;
            SetAllGroupsInteractable(false);
            StopGroupPulse();
            _hand.Hide();

            var clean = _ladder.Step != HelpStep.Demonstrate;

            _game.Sfx.Right();
            _eva.Cheer();
            _runner.StartCoroutine(PopPulse(_groupRects[i], _groupImages[i], 1.1f, 0.3f));
            if (clean) _runner.StartCoroutine(BigCheer(_eva.Root, _evaBaseScale));
            _rightLineIndex = _rightLineIndex % 3 + 1;

            _game.Progress.ShoppingLevel = DifficultyLadder.RecordRound(_game.Progress.ShoppingBuffer, _game.Progress.ShoppingLevel, clean);
            _runner.StartCoroutine(PayCoins(CoinPayout.ForStep(_ladder.Step), _groupRects[i].position));

            yield return _game.Voice.SayAndWait("count_right_" + _rightLineIndex);
            yield return AdvanceRound();
        }

        // --- Shared round flow ------------------------------------------------------------------------------

        private IEnumerator AdvanceRound()
        {
            _roundIndex++;
            if (_roundIndex >= ShoppingRoundGenerator.RoundsPerSession) yield return EndSession();
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

        private void GoHomeAfterSession() => _game.Navigator.Show(ScreenId.StoreActivities);

        private void SetSessionEnded(bool ended)
        {
            if (ended && _hand != null) _hand.Hide();
            _problemField.gameObject.SetActive(!ended);
            _answerField.gameObject.SetActive(!ended && !_usingGroups);
            _groupField.gameObject.SetActive(!ended && _usingGroups);
            _endPanel.SetActive(ended);
        }

        // --- Eva ------------------------------------------------------------------------------------------

        private void BuildEva()
        {
            _eva = AddCompanionPair(_game, CompanionLayout.Corner);
            _evaBaseScale = _eva.Root.localScale;
        }

        // --- Small tweens (identical shapes to AdditionScreen/WhichHasMoreScreen's - see there for the reasoning) ---

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

        private static IEnumerator Wobble(RectTransform target)
        {
            const float amplitude = 8f;
            const float cycles = 4f;
            for (var t = 0f; t < WobbleSeconds; t += Time.deltaTime)
            {
                var decay = 1f - t / WobbleSeconds;
                var angle = Mathf.Sin(t / WobbleSeconds * cycles * Mathf.PI * 2f) * amplitude * decay;
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

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) DestroyChild(parent.GetChild(i).gameObject);
        }

        private static void DestroyChild(GameObject child)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(child);
            else UnityEngine.Object.DestroyImmediate(child);
        }

        private void AddStoreBackground()
        {
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(Root, false);
            background.transform.SetAsFirstSibling();
            var rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            FullBleed.Attach(rect);
            var image = background.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("world/store_bg");
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }
    }
}
