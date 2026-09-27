using System.Collections;
using System.Collections.Generic;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // The furniture shop: the six purchasable items (every catalog item except the free starter sofa) sit on
    // two shelves, each showing its price as a coin icon plus a digit, or - once owned - a green check
    // instead, dimmed. Tapping an item opens a full-screen confirm step (a big centred copy of the item, plus
    // a green check/red cross pair) so no purchase ever happens without the child tapping the check; the
    // shelf itself is hidden while that step is open. That single design choice is also what keeps every
    // position below inside the real on-device frame regardless of which of the six items was tapped - see
    // the class notes on ScreenBase/CreatorScreen/HouseScreen for why the production canvas is height-matched
    // (y in [-450, 450] is the exact on-device bound every element's full rect must stay inside, not a
    // nominal guide) and why every shelf item is kept at least 20 units of real x clearance from the Hud's
    // Home button (x in [-690, -450]) and Bubble button (x in [450, 690]) on the real, 1440-unit-wide
    // on-device frame (not a nominal 1600 - see the detailed math on PriceRowOffsetY below) - the same
    // approach CreatorScreen uses for its HeadRowY.
    public sealed class StoreScreen : ScreenBase
    {
        private sealed class Runner : MonoBehaviour { }

        private const float ItemSize = 260f; // within the brief's 240-280 range
        private static readonly float[] ItemX = { -290f, 0f, 290f };
        private const float Row1Y = 220f;
        private const float Row2Y = -170f;

        private const float CoinIconSize = 60f; // per the brief
        private const int PriceFontSize = 64;
        private const float OwnedBadgeSize = 70f;

        // Review-style check (the same one Task 9/10 eventually did for Creator/House): every item's full rect
        // (position +/- half size) checked against the y in [-450, 450] frame, and every simultaneously-visible
        // pair of tap targets/labels kept at least 20 units of real clearance apart.
        // Row1 item: y in [90, 350]. Row1 price (coin+digit, half height 30): y = 30, i.e. [0, 60] - 30 units
        // clear of the item's own bottom edge (90). Row2 item: y in [-300, -40]. Row2 price: y = -360, i.e.
        // [-390, -330] - 30 units clear of its item's bottom edge (-300), 40 units clear of Row1's price row
        // (bottom edge 0 vs Row2's item top edge -40), and 60 units inside the -450 floor at its own bottom
        // edge (-390).
        //
        // Horizontally: the production canvas is height-matched to a 900-tall reference, so the real on-device
        // frame is exactly 1440 units wide on every device, not a nominal 1600 (see NoReadingAuditTests.cs's
        // canvas setup and CreatorScreen's own Home-button comment, both computed against that same 1440
        // floor). Computed directly from Hud.cs's IconButton math: the Home button (anchor (0,1), position
        // (30,-30), size 240) occupies x in [-690, -450], y in [180, 420]; the Bubble button (anchor (1,0),
        // position (-30,30), size 240) occupies x in [450, 690], y in [-420, -180]. With ItemX = {-290, 0, 290}
        // and ItemSize = 260 (half 130): the left column's rect is x in [-420, -160] - 30 units clear of
        // Home's right edge (-450); the right column's rect is x in [160, 420] - 30 units clear of Bubble's
        // left edge (450). That clearance holds on the x axis alone regardless of either row's y, so (unlike
        // the House screen's living_corner slot) no shelf item's rect can ever be covered by a Hud icon. The
        // same 30-unit gap separates every pair of adjacent columns too (left column's right edge -160 to the
        // centre column's left edge -130; the centre column's right edge 130 to the right column's left edge
        // 160).
        private const float PriceRowOffsetY = -190f; // relative to the item's own y; see the comment above

        private const float SpotlightSize = 360f;
        private static readonly Vector2 SpotlightPosition = new Vector2(0f, 60f);
        private const float DialogButtonSize = 260f;
        private const float DialogButtonY = -280f;
        private static readonly Vector2 CheckPosition = new Vector2(-300f, DialogButtonY);
        private static readonly Vector2 CrossPosition = new Vector2(300f, DialogButtonY);
        // Spotlight rect: y in [-120, 240]. Check/cross rect: y in [-410, -150] - 30 units clear of the
        // spotlight's own bottom edge. The check button (x in [-430, -170]) never shares any y with the real
        // Home button (y in [180, 420]), so it clears regardless of x. The cross button (x in [170, 430]) has
        // a real 20-unit x gap to the real Bubble button's left edge (450) even though their y ranges do
        // overlap (Bubble's y in [-420, -180] sits inside the cross button's [-410, -150]).

        // The Shopping game's own entry icon (M4.3): the "Bubble button" clearance zone documented above was
        // reserved for a speech-bubble replay button that Hud.cs never actually built (SetBubbleButtonVisible
        // is a no-op stub - "no bubble button at the moment"; the real speech bubble lives elsewhere, at Hud's
        // own bottom-right corner, not this reserved zone), so this is genuinely free space rather than a
        // repurposing of a button that exists. Sized and placed to match: x in [450, 690], y in [-420, -180] -
        // 30 units clear of the right column's shelf items (x in [160, 420]) on the x axis alone, so it can
        // never overlap them regardless of either row's y, same reasoning as every other clearance on this
        // screen. Placed on the shelf layer (not a separate always-visible layer) so it hides along with the
        // shelf whenever the confirm dialog is open, matching every shelf item's own visibility rule.
        private const float ShoppingButtonSize = 240f;
        private static readonly Vector2 ShoppingButtonPosition = new Vector2(570f, -300f);

        private const float GrowSeconds = 0.22f;
        private const float WobbleSeconds = 0.4f;

        private static readonly Color NormalColor = Color.white;
        private static readonly Color OwnedColor = new Color(0.72f, 0.72f, 0.72f, 0.85f);

        private static readonly List<FurnitureItem> ShopItems = BuildShopItems();

        private EvaGame _game;
        private Runner _runner;
        private RectTransform _shelfLayer;
        private RectTransform _confirmLayer;
        private Image _spotlightImage;
        private Coroutine _buyRoutine;
        private Coroutine _growRoutine;
        private string _selectedItemId;

        private readonly Dictionary<string, Image> _itemImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, GameObject> _priceRows = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> _ownedBadges = new Dictionary<string, GameObject>();

        // Same one-per-session pattern as HouseScreen's house_placed line: the screen instance lives for the
        // whole session (Navigator builds it once and only shows/hides it), so a plain instance flag already
        // means "first time this session" without persisting anything.
        private bool _saidWelcomeThisSession;

        public override void Build(EvaGame game)
        {
            _game = game;
            _runner = Root.gameObject.AddComponent<Runner>();

            AddStoreBackground();

            _shelfLayer = CreateFullRectContainer("Shelf");
            BuildShelfItems();
            EvaUi.IconButton(_shelfLayer, "ShoppingButton", EvaUi.Sprite("icons/shopping"), new Vector2(0.5f, 0.5f),
                ShoppingButtonPosition, ShoppingButtonSize, () => _game.Navigator.Show(ScreenId.Shopping));

            _confirmLayer = CreateFullRectContainer("Confirm");
            BuildConfirmDialog();
            _confirmLayer.gameObject.SetActive(false);
        }

        public override void OnShow()
        {
            CloseConfirm(); // resync to the shelf in case a previous visit left the confirm step open
            RefreshShelf();
            if (!_saidWelcomeThisSession)
            {
                _saidWelcomeThisSession = true;
                _game.Voice.Say("store_welcome");
            }
            _game.Progress.Advance(TutorialEvent.EnteredStore);
            _game.Commit();
            // After Advance, so the guide reads the post-entry step (FirstPurchase once this is the tutorial's
            // entry into the store) - its own store_welcome line is skipped since Voice.LastKey already matches
            // the line said just above (see TutorialGuide.Refresh).
            _game.TutorialGuide.Refresh(ScreenId.Store);
        }

        private static List<FurnitureItem> BuildShopItems()
        {
            var items = new List<FurnitureItem>();
            foreach (var item in FurnitureCatalog.All)
                if (item.Id != FurnitureCatalog.StarterId) items.Add(item);
            return items;
        }

        // --- Shelf ---------------------------------------------------------------------------------------

        private void BuildShelfItems()
        {
            for (var i = 0; i < ShopItems.Count; i++)
            {
                var position = new Vector2(ItemX[i % 3], i < 3 ? Row1Y : Row2Y);
                BuildShelfItem(ShopItems[i], position);
            }
        }

        private void BuildShelfItem(FurnitureItem item, Vector2 position)
        {
            var id = item.Id;
            var button = EvaUi.IconButton(_shelfLayer, "Item_" + id, EvaUi.Sprite("objects/" + id),
                new Vector2(0.5f, 0.5f), position, ItemSize, () => OnItemTapped(id));
            _itemImages[id] = (Image)button.targetGraphic;

            var badgePosition = new Vector2(position.x, position.y + PriceRowOffsetY);
            _priceRows[id] = BuildPriceRow(item, badgePosition);
            _ownedBadges[id] = BuildOwnedBadge(id, badgePosition);
        }

        private GameObject BuildPriceRow(FurnitureItem item, Vector2 position)
        {
            var row = new GameObject("Price_" + item.Id, typeof(RectTransform));
            row.transform.SetParent(_shelfLayer, false);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = position;
            rowRect.sizeDelta = new Vector2(ItemSize, CoinIconSize);

            var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(row.transform, false);
            var coinRect = (RectTransform)coin.transform;
            coinRect.anchorMin = coinRect.anchorMax = coinRect.pivot = new Vector2(0.5f, 0.5f);
            coinRect.anchoredPosition = new Vector2(-40f, 0f);
            coinRect.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);
            var coinImage = coin.GetComponent<Image>();
            coinImage.sprite = EvaUi.Sprite("icons/coin");
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;

            var numeral = EvaUi.Numeral(row.transform, "Digit", PriceFontSize);
            numeral.text = item.Price.ToString();
            var numeralRect = (RectTransform)numeral.transform;
            numeralRect.anchorMin = numeralRect.anchorMax = numeralRect.pivot = new Vector2(0.5f, 0.5f);
            numeralRect.anchoredPosition = new Vector2(35f, 0f);
            numeralRect.sizeDelta = new Vector2(90f, CoinIconSize);

            return row;
        }

        private GameObject BuildOwnedBadge(string id, Vector2 position)
        {
            var badge = new GameObject("OwnedBadge_" + id, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(_shelfLayer, false);
            var rect = (RectTransform)badge.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(OwnedBadgeSize, OwnedBadgeSize);
            var image = badge.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/check");
            image.preserveAspect = true;
            image.raycastTarget = false;
            badge.SetActive(false);
            return badge;
        }

        // Redraws every shelf item's owned look from the current save: an owned item is dimmed with a check
        // badge instead of its price. An unaffordable item still shows its normal price - the price digit
        // already tells the child what it costs, and NotEnoughCoins is only reached by actually trying to
        // buy it (tapping the item, then the check), per the brief.
        private void RefreshShelf()
        {
            var owned = _game.Progress.Owned;
            foreach (var item in ShopItems)
            {
                var isOwned = owned.Contains(item.Id);
                _itemImages[item.Id].color = isOwned ? OwnedColor : NormalColor;
                _priceRows[item.Id].SetActive(!isOwned);
                _ownedBadges[item.Id].SetActive(isOwned);
            }
        }

        // --- Confirm dialog --------------------------------------------------------------------------------

        private void BuildConfirmDialog()
        {
            var spotlight = new GameObject("Spotlight", typeof(RectTransform), typeof(Image));
            spotlight.transform.SetParent(_confirmLayer, false);
            var rect = (RectTransform)spotlight.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = SpotlightPosition;
            rect.sizeDelta = new Vector2(SpotlightSize, SpotlightSize);
            _spotlightImage = spotlight.GetComponent<Image>();
            _spotlightImage.preserveAspect = true;
            _spotlightImage.raycastTarget = false;

            EvaUi.IconButton(_confirmLayer, "CheckButton", EvaUi.Sprite("icons/check"), new Vector2(0.5f, 0.5f),
                CheckPosition, DialogButtonSize, OnCheckTapped);
            EvaUi.IconButton(_confirmLayer, "CrossButton", EvaUi.Sprite("icons/cross"), new Vector2(0.5f, 0.5f),
                CrossPosition, DialogButtonSize, CloseConfirm);
        }

        // The shelf is inactive for as long as the confirm step is open, so this can only ever fire once per
        // step - no separate re-entrancy guard needed.
        private void OnItemTapped(string itemId)
        {
            _selectedItemId = itemId;
            _spotlightImage.sprite = EvaUi.Sprite("objects/" + itemId);
            _spotlightImage.color = Color.white;
            _spotlightImage.transform.localScale = Vector3.one;
            _spotlightImage.transform.localRotation = Quaternion.identity;

            _shelfLayer.gameObject.SetActive(false);
            _confirmLayer.gameObject.SetActive(true);
            _game.Voice.Say("store_buy_q");
            _growRoutine = _runner.StartCoroutine(GrowIn((RectTransform)_spotlightImage.transform));
        }

        private void OnCheckTapped()
        {
            if (_selectedItemId == null || _buyRoutine != null) return; // a purchase is already in flight
            _buyRoutine = _runner.StartCoroutine(DoBuy(_selectedItemId));
        }

        private IEnumerator DoBuy(string itemId)
        {
            var before = _game.Progress.Coins;
            var result = _game.Progress.TryBuy(itemId);
            switch (result)
            {
                case BuyResult.Bought:
                    _game.Sfx.Buy();
                    _game.Commit(); // the item joins the tray; also snaps the real coin total immediately -
                                     // see Hud.AnimateCoins, so quitting mid-animation can never desync the save.
                    _game.Voice.Say("store_bought");
                    yield return _game.Hud.AnimateCoins(before, _game.Progress.Coins, _game.Sfx, _spotlightImage.transform.position);
                    _game.Progress.Advance(TutorialEvent.ItemBought);
                    _game.Commit();
                    _buyRoutine = null;
                    CloseConfirm();
                    RefreshShelf();
                    _game.TutorialGuide.Refresh(ScreenId.Store);
                    break;

                case BuyResult.NotEnoughCoins:
                    _game.Sfx.Retry();
                    _game.Voice.Say("store_not_enough");
                    yield return Wobble((RectTransform)_spotlightImage.transform);
                    _buyRoutine = null;
                    break; // no state change: the dialog stays open so the child can try again or cancel

                case BuyResult.AlreadyOwned:
                    _game.Voice.Say("store_owned");
                    _buyRoutine = null;
                    CloseConfirm();
                    break;

                default: // UnknownItem: never reached from the shelf (the starter is excluded); safety net only
                    _buyRoutine = null;
                    CloseConfirm();
                    break;
            }
        }

        private void CloseConfirm()
        {
            if (_buyRoutine != null)
            {
                _runner.StopCoroutine(_buyRoutine);
                _buyRoutine = null;
            }
            if (_growRoutine != null)
            {
                _runner.StopCoroutine(_growRoutine);
                _growRoutine = null;
            }
            _spotlightImage.transform.localRotation = Quaternion.identity; // in case a wobble was interrupted
            _selectedItemId = null;
            _confirmLayer.gameObject.SetActive(false);
            _shelfLayer.gameObject.SetActive(true);
        }

        // Pops the spotlight icon in from small, reading as the tapped item lifting and growing into focus.
        private static IEnumerator GrowIn(RectTransform target)
        {
            for (var t = 0f; t < GrowSeconds; t += Time.deltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, PointerHand.EaseInOut(t / GrowSeconds));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // A soft rotation shake, settling back to upright (same choreography as CountScreen's wrong-tile wobble).
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

        // --- Background and layout helpers -----------------------------------------------------------------

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
    }
}
