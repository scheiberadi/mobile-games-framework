using UnityEngine;

namespace EvasLearningWorld.App
{
    // Makes a RectTransform that lives under a safe-area panel cover the WHOLE canvas instead (under the camera
    // cutout and the bars too). Use it for scenery and backgrounds only; controls and text stay in the safe area.
    // The parent must be stretched to the safe area (a screen Root or a full-rect child of it).
    public sealed class FullBleed : MonoBehaviour
    {
        // Tests set this to simulate a phone cutout (pixels, like Screen.safeArea); null = the real safe area.
        public static Rect? SafeAreaOverride;

        private Rect _lastSafe;
        private Vector2 _lastCanvas;

        public static Rect CurrentSafeArea => SafeAreaOverride ?? Screen.safeArea;

        // How far the safe area is from each canvas edge, in canvas units (left, bottom, right, top).
        public static Vector4 Insets(Rect safe, float screenWidth, float screenHeight, Vector2 canvasSize)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return Vector4.zero;
            var kx = canvasSize.x / screenWidth;
            var ky = canvasSize.y / screenHeight;
            return new Vector4(safe.xMin * kx, safe.yMin * ky, (screenWidth - safe.xMax) * kx, (screenHeight - safe.yMax) * ky);
        }

        // Where the safe-area centre sits relative to the canvas centre, in canvas units.
        public static Vector2 CentreShift(Vector4 insets) => new Vector2((insets.x - insets.z) / 2f, (insets.y - insets.w) / 2f);

        // Tests simulate a phone: a safe area (pixels) inside a screen of this size. Null = the real device.
        public static Vector2Int? ScreenSizeOverride;

        private static Vector2Int ScreenSize => ScreenSizeOverride ?? new Vector2Int(Screen.width, Screen.height);

        // The insets for the canvas that contains `from` (zero when there is none).
        public static Vector4 CurrentInsets(Transform from)
        {
            var canvas = from != null ? from.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return Vector4.zero;
            var canvasRect = (RectTransform)canvas.rootCanvas.transform;
            return Insets(CurrentSafeArea, ScreenSize.x, ScreenSize.y, canvasRect.rect.size);
        }

        private void OnEnable() => Apply(true);
        private void Update() => Apply(false);

        public void Apply(bool force)
        {
            var safe = CurrentSafeArea;
            var canvasSize = Vector2.zero;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasSize = ((RectTransform)canvas.rootCanvas.transform).rect.size;
            if (!force && safe == _lastSafe && canvasSize == _lastCanvas) return;
            _lastSafe = safe;
            _lastCanvas = canvasSize;

            var insets = CurrentInsets(transform);
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-insets.x, -insets.y);
            rect.offsetMax = new Vector2(insets.z, insets.w);
        }

        // Adds the component to `rect` and applies it now.
        public static void Attach(RectTransform rect)
        {
            var bleed = rect.gameObject.AddComponent<FullBleed>();
            bleed.Apply(true);
        }
    }
}
