using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Marks every tappable or draggable object. The no-reading audit requires it on anything interactive and
    // checks that its rectangle is at least EvaUi.MinTap square.
    public sealed class TapTarget : MonoBehaviour { }

    // Shrinks the object to 0.9 while a finger is on it.
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.9f;
        private Vector3 _restScale = Vector3.one;
        private bool _pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pressed) return;
            _pressed = true;
            _restScale = transform.localScale;
            transform.localScale = _restScale * PressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();
        private void OnDisable() => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            transform.localScale = _restScale;
        }
    }

    public static class EvaUi
    {
        // Minimum hit area in canvas units (about 100 dp on the S25 Ultra) for anything a 4 to 5 year old touches.
        public const float MinTap = 240f;

        // Set by EvaGame so every icon button can play the tap sound without each caller passing it in.
        public static Sfx Sfx { get; set; }

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        // Loads Resources/Art/<name>; while the art does not exist yet, a generated rounded rectangle whose colour
        // depends on the name stands in, so every screen works before any art is drawn.
        public static Sprite Sprite(string name)
        {
            if (SpriteCache.TryGetValue(name, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null) sprite = Placeholder(name);
            SpriteCache[name] = sprite;
            return sprite;
        }

        // Anchor and pivot are the same point, so position is the offset from that point to the same corner or edge of the
        // button: (0, 1) with (30, -30) sits 30 units in from the top-left corner. The size is raised to MinTap if smaller.
        public static Button IconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, float size, UnityAction onClick)
        {
            var side = Mathf.Max(size, MinTap);
            return IconButton(parent, name, icon, anchor, position, new Vector2(side, side), onClick);
        }

        // Same as above with a non-square tap box; each side is raised to MinTap if smaller.
        public static Button IconButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 position, Vector2 size, UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TapTarget), typeof(PressFeedback));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(Mathf.Max(size.x, MinTap), Mathf.Max(size.y, MinTap));

            var image = buttonObject.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (Sfx != null) Sfx.Tap();
                onClick?.Invoke();
            });
            return button;
        }

        // The tap area stays as it is; only the picture is drawn smaller (35 units in on every side).
        public static void ShrinkIcon(Button button, float inset = 35f)
        {
            var background = (Image)button.targetGraphic;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var rect = (RectTransform)iconObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = background.sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            background.color = Color.clear;
        }

        // A number label. Only digits may ever be put in it (the no-reading audit fails any other text).
        public static TextMeshProUGUI Numeral(Transform parent, string name, int fontSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.15f, 0.1f);
            text.raycastTarget = false;
            text.text = "0";
            return text;
        }

        private static Sprite Placeholder(string name)
        {
            const int size = 128, radius = 26, edge = 6;
            var hash = 17;
            foreach (var c in name) hash = hash * 31 + c;
            var fill = Color.HSVToRGB(Mathf.Abs(hash % 360) / 360f, 0.45f, 0.97f);
            var rim = Color.HSVToRGB(Mathf.Abs(hash % 360) / 360f, 0.6f, 0.75f);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var half = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                var dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                var distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                var color = distance > -edge ? rim : fill;
                color.a = Mathf.Clamp01(0.5f - distance);
                texture.SetPixel(x, y, color);
            }
            texture.Apply();
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
