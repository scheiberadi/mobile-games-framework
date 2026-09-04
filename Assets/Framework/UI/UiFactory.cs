using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MobileGamesFramework.UI
{
    public static class UiFactory
    {
        public static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 900);
            // Match width, not a width/height blend: every screen built against this
            // canvas is laid out assuming a fixed 800-unit-wide reference. Blending in
            // height (the old 0.5) shrinks that effective width on narrow/tall phone
            // aspect ratios, clipping the widest rows left and right. Pinning to width
            // keeps horizontal layout consistent on every device; on taller phones the
            // only side effect is extra unused vertical margin, which is harmless here
            // since every screen is portrait-only and already fits well within height.
            scaler.matchWidthOrHeight = 0f;

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            return canvas;
        }

        public static void CreateBackground(Transform parent, Color top, Color bottom)
        {
            var backgroundObject = new GameObject("Background", typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();
            SetRect(backgroundObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var image = backgroundObject.GetComponent<Image>();
            image.sprite = GradientSprite.Get(top, bottom);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
        }

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.black;
            return text;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, bool interactable, UnityAction onClick, Vector2? anchor = null)
        {
            // anchor also doubles as the pivot: for the default center anchor (0.5,0.5)
            // that's a no-op (matches the old hardcoded behavior), but for an edge/corner
            // anchor like (0,1) it makes `position` read as "offset from that corner to
            // this box's own corner" - the only way to pin a button to the actual screen
            // edge regardless of a device's canvas height, instead of a fixed distance
            // from screen center that only looks corner-pinned on one aspect ratio.
            var anchorPoint = anchor ?? new Vector2(0.5f, 0.5f);

            var shadowObject = new GameObject(label + "ButtonShadow", typeof(Image));
            shadowObject.transform.SetParent(parent, false);
            SetRect(shadowObject.GetComponent<RectTransform>(), anchorPoint, anchorPoint, position + new Vector2(2, -3), size);
            shadowObject.GetComponent<RectTransform>().pivot = anchorPoint;
            var shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = RoundedRectSprite.Get();
            shadowImage.type = Image.Type.Sliced;
            shadowImage.color = new Color(0f, 0f, 0f, 0.18f);
            shadowObject.SetActive(interactable);

            var buttonObject = new GameObject(label + "Button", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            SetRect(buttonObject.GetComponent<RectTransform>(), anchorPoint, anchorPoint, position, size);
            buttonObject.GetComponent<RectTransform>().pivot = anchorPoint;

            var image = buttonObject.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.sprite = interactable
                ? RoundedRectSprite.GetGradient(new Color(0.98f, 0.85f, 0.35f), new Color(0.90f, 0.66f, 0.10f))
                : RoundedRectSprite.GetGradient(new Color(0.80f, 0.80f, 0.80f), new Color(0.65f, 0.65f, 0.65f));

            var button = buttonObject.GetComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(onClick);

            // Our sprite swap is the sole indicator of enabled/disabled - neutralize
            // Unity's own ColorTint multiply so it can't wash out or double up on that.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;

            var text = CreateText(buttonObject.transform, "Label", 22, TextAnchor.MiddleCenter);
            text.text = label;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        // Standard top-left back button used by every screen with a "back" action -
        // keeps placement consistent across games instead of each screen picking its own.
        public static Button CreateBackButton(Transform parent, UnityAction onClick)
        {
            // Anchored to the canvas's actual top-left corner (not a fixed offset from
            // center) so it sits flush in the corner on every device, regardless of how
            // tall the canvas ends up in canvas-units for that screen's aspect ratio.
            return CreateButton(parent, "Back", new Vector2(20, -20), new Vector2(110, 50), true, onClick, new Vector2(0f, 1f));
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            button.interactable = interactable;
            var image = button.GetComponent<Image>();
            image.sprite = interactable
                ? RoundedRectSprite.GetGradient(new Color(0.98f, 0.85f, 0.35f), new Color(0.90f, 0.66f, 0.10f))
                : RoundedRectSprite.GetGradient(new Color(0.80f, 0.80f, 0.80f), new Color(0.65f, 0.65f, 0.65f));

            // The shadow is named "<Button's own name>Shadow" and created as its sibling
            // in CreateButton - a disabled/non-interactive button shouldn't look "raised".
            var shadow = button.transform.parent != null ? button.transform.parent.Find(button.name + "Shadow") : null;
            if (shadow != null) shadow.gameObject.SetActive(interactable);
        }
    }
}
