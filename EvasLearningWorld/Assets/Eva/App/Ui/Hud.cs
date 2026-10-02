using System.Collections;
using EvasLearningWorld.Rules;
using MobileGamesFramework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Marks the speech bubble: the only object under which letters may appear, and it is hidden until tapped for.
    public sealed class Bubble : MonoBehaviour { }

    // Always-on overlay above the screens: home, coins, the optional speech bubble and (debug builds) a frame rate number.
    public sealed class Hud : MonoBehaviour
    {
        // The Home button's place and size; the Map's settings gear reuses them so the two are identical. HomeIconInset is
        // how far the picture of the Home, Back and gear icons is drawn in from the button edge (the tap area stays full size).
        public static readonly Vector2 HomeAnchor = new Vector2(0f, 1f);
        public static readonly Vector2 HomePosition = new Vector2(30f, 35f);
        public const float HomeSize = 240f;
        public const float HomeIconInset = 60f;

        private const float BubbleSeconds = 4f;
        private const float CoinFlySeconds = 0.55f;
        private const float CoinFlySize = 90f;
        private const float PiggySize = 110f;
        private const float HomeBackShift = 30f;
        private const int MaxFlyingCoins = 8;
        private const float CoinStaggerSeconds = 0.09f;
        private const float CoinArcHeight = 160f;
        private const float PiggyBumpSeconds = 0.18f;

        // Back (shown inside a game) sits right of Home, tap areas touching; the two pictures are nudged toward each other by HomeBackShift.
        public static readonly Vector2 BackPosition = new Vector2(HomePosition.x + HomeSize, HomePosition.y);

        private EvaGame _game;
        private Button _home;
        private Button _back;
        private RectTransform _coinIconRect;
        private TextMeshProUGUI _coins;
        private GameObject _bubble;
        private TextMeshProUGUI _bubbleText;
        private TextMeshProUGUI _fps;
        private Coroutine _hideBubble;
        private Coroutine _bump;
        private float _fpsTime;
        private int _fpsFrames;

        public void Build(EvaGame game, RectTransform root)
        {
            _game = game;

            _home = EvaUi.IconButton(root, "HomeButton", EvaUi.Sprite("icons/home"), HomeAnchor, HomePosition, HomeSize,
                GoHome);
            _back = EvaUi.IconButton(root, "BackButton", EvaUi.Sprite("icons/back"), HomeAnchor, BackPosition, HomeSize,
                GoBack);
            EvaUi.ShrinkIcon(_home, HomeIconInset, HomeBackShift);
            EvaUi.ShrinkIcon(_back, HomeIconInset, -HomeBackShift);

            var coinIcon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinIcon.transform.SetParent(root, false);
            var coinRect = (RectTransform)coinIcon.transform;
            SetCorner(coinRect, new Vector2(1f, 1f), new Vector2(-190f, -30f), new Vector2(PiggySize, PiggySize));
            coinRect.pivot = new Vector2(0.5f, 0.5f); // the piggy bank bumps around its own middle
            coinRect.anchoredPosition = new Vector2(-190f - PiggySize * 0.5f, -85f); // vertical centre = the coin number's (y -30 .. -140)
            var coinImage = coinIcon.GetComponent<Image>();
            // The piggy bank (icons/piggybank) is the coin counter's picture; the plain coin stays until that art exists.
            var piggy = Resources.Load<Sprite>("Art/icons/piggybank");
            coinImage.sprite = piggy != null ? piggy : EvaUi.Sprite("icons/coin");
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            _coinIconRect = coinRect;

            _coins = EvaUi.Numeral(root, "CoinCount", 84);
            _coins.alignment = TextAlignmentOptions.MidlineRight;
            SetCorner((RectTransform)_coins.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(150f, 110f));

            // The speech-bubble replay button is removed for now (it only helped when a line had been spoken).
            BuildBubble(root);

            // The debug APK is not a Unity development build (Debug.isDebugBuild is false), so it is also recognised by its ".dev" id.
            if (Debug.isDebugBuild || Application.identifier.EndsWith(".dev"))
            {
                _fps = EvaUi.Numeral(root, "FpsCounter", 56);
                _fps.alignment = TextAlignmentOptions.MidlineLeft;
                SetCorner((RectTransform)_fps.transform, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(200f, 90f));
            }

            SetHomeVisible(false);
            SetBackVisible(false);
            SetCoins(0);
            SetBubbleVisible(false);
        }

        // Home always goes to the Map. Inside a game the separate Back button steps to the building's game list.
        private void GoHome() => _game.Navigator.Show(ScreenId.Map);

        private void GoBack()
        {
            if (_game.Navigator.BackTarget.HasValue) _game.Navigator.Show(_game.Navigator.BackTarget.Value);
        }

        public void SetHomeVisible(bool visible) => _home.gameObject.SetActive(visible);

        public void SetBackVisible(bool visible) => _back.gameObject.SetActive(visible);

        // The debug frame-rate counter (if this build has one); hidden on the adult screens where it only looks like a stray number.
        public void SetFpsVisible(bool visible) { if (_fps != null) _fps.gameObject.SetActive(visible); }

        public void SetBubbleButtonVisible(bool visible) { } // no bubble button at the moment

        public void SetCoins(int coins) => _coins.text = coins.ToString();

        // Visually moves the coin total from `from` to `to`: up to MaxFlyingCoins coins fly in a short arc.
        // Gaining: they fly from `worldPosition` (the middle of the screen when null) into the piggy bank and each
        // landing adds its share to the shown total. Spending: they fly out of the piggy bank to `worldPosition`
        // (the thing being bought) and each take-off removes its share. Every coin plays Sfx.Coin() and bumps the
        // piggy. The underlying save is not touched here: EvaGame.Commit() already persisted the real total
        // before a caller starts this (see CountScreen/StoreScreen), so quitting mid-animation never loses or
        // desyncs a coin.
        public Coroutine AnimateCoins(int from, int to, Sfx sfx, Vector2? worldPosition = null) =>
            StartCoroutine(AnimateCoinsRoutine(from, to, sfx, worldPosition));

        private sealed class CoinRun
        {
            public int Shown;
            public int Pending;
        }

        private IEnumerator AnimateCoinsRoutine(int from, int to, Sfx sfx, Vector2? worldPosition)
        {
            SetCoins(from);
            var amount = Mathf.Abs(to - from);
            if (amount == 0 || _coinIconRect == null) yield break;

            var gaining = to > from;
            var flying = Mathf.Min(amount, MaxFlyingCoins);
            var other = worldPosition ?? ScreenCentre();
            var run = new CoinRun { Shown = from, Pending = flying };
            for (var i = 0; i < flying; i++)
            {
                var share = amount / flying + (i < amount % flying ? 1 : 0);
                StartCoroutine(FlyCoin(other, i * CoinStaggerSeconds, gaining ? share : -share, gaining, run, sfx));
            }
            while (run.Pending > 0) yield return null;
            SetCoins(to);
        }

        private Vector2 ScreenCentre()
        {
            var parent = (RectTransform)_coinIconRect.parent;
            return parent.TransformPoint(parent.rect.center);
        }

        private Vector2 PiggySlot() =>
            _coinIconRect.TransformPoint(new Vector3(0f, _coinIconRect.rect.height * 0.25f, 0f));

        // A short-lived coin Image that, after `delay`, flies from `other` into the slot on the piggy bank's back
        // (`intoPiggy`) or from that slot out to `other`, then destroys itself. `signedShare` coins are added to the
        // shown total when a coin lands in the piggy, or removed when it leaves it.
        private IEnumerator FlyCoin(Vector2 other, float delay, int signedShare, bool intoPiggy, CoinRun run, Sfx sfx)
        {
            var coin = new GameObject("FlyingCoin", typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(_coinIconRect.parent, false);
            var rect = (RectTransform)coin.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(CoinFlySize, CoinFlySize);
            coin.SetActive(false);

            var image = coin.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/coin");
            image.preserveAspect = true;
            image.raycastTarget = false;

            for (var t = 0f; t < delay; t += Time.deltaTime) yield return null;

            var jitter = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
            var startPos = intoPiggy ? other + jitter : PiggySlot();
            var endPos = intoPiggy ? PiggySlot() : other + jitter;
            var startScale = intoPiggy ? 1f : 0.6f;
            var endScale = intoPiggy ? 0.6f : 1f;
            if (!intoPiggy) Settle(run, signedShare, sfx); // the coin leaves the piggy: the total drops now
            coin.SetActive(true);
            for (var t = 0f; t < CoinFlySeconds; t += Time.deltaTime)
            {
                var k = t / CoinFlySeconds;
                var eased = k * k * (3f - 2f * k);
                var arc = Mathf.Sin(k * Mathf.PI) * CoinArcHeight;
                rect.position = Vector2.Lerp(startPos, endPos, eased) + new Vector2(0f, arc);
                rect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
                yield return null;
            }

            // Matches CountScreen.ClearChildren's own rule: edit-mode tests drive this screen's coroutines
            // synchronously outside play mode, where Destroy is illegal and DestroyImmediate is required.
            if (Application.isPlaying) Destroy(coin);
            else DestroyImmediate(coin);

            if (intoPiggy) Settle(run, signedShare, sfx); // the coin lands: the total rises now
            run.Pending--;
        }

        // Applies one coin's share to the shown total, plays its sound and bumps the piggy.
        private void Settle(CoinRun run, int signedShare, Sfx sfx)
        {
            run.Shown += signedShare;
            SetCoins(run.Shown);
            if (sfx != null) sfx.Coin();
            if (_bump != null) StopCoroutine(_bump);
            _bump = StartCoroutine(BumpPiggy());
        }

        // The piggy bank swells and settles back each time a coin drops in.
        private IEnumerator BumpPiggy()
        {
            for (var t = 0f; t < PiggyBumpSeconds; t += Time.deltaTime)
            {
                var k = t / PiggyBumpSeconds;
                _coinIconRect.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            _coinIconRect.localScale = Vector3.one;
            _bump = null;
        }

        public void SetBubbleVisible(bool visible)
        {
            _bubble.SetActive(visible);
            if (!visible && _hideBubble != null)
            {
                StopCoroutine(_hideBubble);
                _hideBubble = null;
            }
        }

        private void BuildBubble(RectTransform root)
        {
            _bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(Bubble));
            _bubble.transform.SetParent(root, false);
            SetCorner((RectTransform)_bubble.transform, new Vector2(1f, 0f), new Vector2(-290f, 30f), new Vector2(760f, 240f));
            var image = _bubble.GetComponent<Image>();
            image.sprite = RoundedRectSprite.Get();
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.95f);
            image.raycastTarget = false;

            var textObject = new GameObject("BubbleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_bubble.transform, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 16f);
            rect.offsetMax = new Vector2(-24f, -16f);
            _bubbleText = textObject.GetComponent<TextMeshProUGUI>();
            _bubbleText.fontSize = 44;
            _bubbleText.alignment = TextAlignmentOptions.Center;
            _bubbleText.color = new Color(0.2f, 0.15f, 0.1f);
            _bubbleText.raycastTarget = false;
        }

        // Anchor and pivot are the same point; see EvaUi.IconButton.
        private static void SetCorner(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Shows the average frames per second over the last second, as an integer.
        private void Update()
        {
            if (_fps == null) return;
            _fpsFrames++;
            _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime < 1f) return;
            _fps.text = Mathf.RoundToInt(_fpsFrames / _fpsTime).ToString();
            _fpsFrames = 0;
            _fpsTime = 0f;
        }
    }
}
