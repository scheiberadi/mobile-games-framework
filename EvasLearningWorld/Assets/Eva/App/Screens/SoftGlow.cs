using UnityEngine;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // A soft round light (made in code: a white disc fading to nothing at its edge) that screens pulse behind the thing the child should look at.
    public static class SoftGlow
    {
        private static Sprite _sprite;

        public static Sprite Sprite()
        {
            if (_sprite != null) return _sprite;
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f)) / (n * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.15f - d) * Mathf.Clamp01(1.6f - d * 1.2f)));
                }
            texture.Apply();
            _sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _sprite;
        }

        // A glow image as the first child of this parent (behind the picture), off until a screen shows it.
        public static Image Create(Transform parent, float size)
        {
            var go = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = Sprite();
            image.color = new Color(1f, 0.95f, 0.5f, 0f);
            image.raycastTarget = false;
            go.SetActive(false);
            return image;
        }

        // The glow pulses: call every frame with the time while it shows.
        public static void Pulse(Image glow, float time)
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(time * 4f);
            glow.gameObject.SetActive(true);
            glow.color = new Color(1f, 0.95f, 0.5f, 0.55f + 0.35f * pulse);
            glow.rectTransform.localScale = Vector3.one * (0.9f + 0.18f * pulse);
        }
    }
}
