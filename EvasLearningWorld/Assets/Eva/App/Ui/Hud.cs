using System.Collections;
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
        private const float BubbleSeconds = 4f;
        private const float CoinStepSeconds = 0.15f;
        private const float CoinFlySeconds = 0.4f;
        private const float CoinFlySize = 90f;

        private EvaGame _game;
        private Button _home;
        private RectTransform _coinIconRect;
        private TextMeshProUGUI _coins;
        private GameObject _bubble;
        private TextMeshProUGUI _bubbleText;
        private TextMeshProUGUI _fps;
        private Coroutine _hideBubble;
        private float _fpsTime;
        private int _fpsFrames;

        public void Build(EvaGame game, RectTransform root)
        {
            _game = game;

            _home = EvaUi.IconButton(root, "HomeButton", EvaUi.Sprite("icons/home"), new Vector2(0f, 1f), new Vector2(30f, 35f), 240f,
                () => _game.Navigator.Show(ScreenId.Map));

            var coinIcon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinIcon.transform.SetParent(root, false);
            var coinRect = (RectTransform)coinIcon.transform;
            SetCorner(coinRect, new Vector2(1f, 1f), new Vector2(-190f, -30f), new Vector2(110f, 110f));
            var coinImage = coinIcon.GetComponent<Image>();
            coinImage.sprite = EvaUi.Sprite("icons/coin");
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
            SetCoins(0);
            SetBubbleVisible(false);
        }

        public void SetHomeVisible(bool visible) => _home.gameObject.SetActive(visible);

        public void SetBubbleButtonVisible(bool visible) { } // no bubble button at the moment

        public void SetCoins(int coins) => _coins.text = coins.ToString();

        // Visually counts the displayed coin total from `from` up to `to` one at a time, playing Sfx.Coin() on
        // every step. The underlying save is not touched here: EvaGame.Commit() already persisted the real
        // total before a caller starts this (see CountScreen/StoreScreen), so quitting mid-animation never
        // loses or desyncs a coin. When `fromWorldPosition` is given, a coin icon also flies from there to the
        // HUD's own coin icon, purely cosmetic and layered alongside (never gating) the counter tick above -
        // it runs on its own coroutine that this method does not wait on.
        public Coroutine AnimateCoins(int from, int to, Sfx sfx, Vector2? fromWorldPosition = null) =>
            StartCoroutine(AnimateCoinsRoutine(from, to, sfx, fromWorldPosition));

        private IEnumerator AnimateCoinsRoutine(int from, int to, Sfx sfx, Vector2? fromWorldPosition)
        {
            if (fromWorldPosition.HasValue) StartCoroutine(FlyCoin(fromWorldPosition.Value));

            SetCoins(from);
            var step = to >= from ? 1 : -1;
            for (var coins = from; coins != to; coins += step)
            {
                yield return new WaitForSeconds(CoinStepSeconds);
                SetCoins(coins + step);
                if (sfx != null) sfx.Coin();
            }
        }

        // A short-lived coin Image that flies from `fromWorldPosition` to the HUD's own coin icon, then
        // destroys itself. Reuses the same sprite as the static icon; no new asset, no particle system.
        private IEnumerator FlyCoin(Vector2 fromWorldPosition)
        {
            if (_coinIconRect == null) yield break;

            var coin = new GameObject("FlyingCoin", typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(_coinIconRect.parent, false);
            var rect = (RectTransform)coin.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(CoinFlySize, CoinFlySize);
            rect.position = fromWorldPosition;

            var image = coin.GetComponent<Image>();
            image.sprite = EvaUi.Sprite("icons/coin");
            image.preserveAspect = true;
            image.raycastTarget = false;

            var startPos = rect.position;
            var endPos = _coinIconRect.position;
            for (var t = 0f; t < CoinFlySeconds; t += Time.deltaTime)
            {
                var k = t / CoinFlySeconds;
                rect.position = Vector3.Lerp(startPos, endPos, k);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, k);
                yield return null;
            }

            // Matches CountScreen.ClearChildren's own rule: edit-mode tests drive this screen's coroutines
            // synchronously outside play mode, where Destroy is illegal and DestroyImmediate is required.
            if (Application.isPlaying) Destroy(coin);
            else DestroyImmediate(coin);
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
