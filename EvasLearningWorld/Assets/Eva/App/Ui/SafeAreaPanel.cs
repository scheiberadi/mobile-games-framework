using UnityEngine;

namespace EvasLearningWorld.App
{
    // A stretched RectTransform whose anchors follow Screen.safeArea, re-applied when the safe area or resolution changes.
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private Vector2Int _lastSize;

        private void OnEnable() => Apply(true);
        private void Update() => Apply(false);

        public void Apply(bool force)
        {
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == _lastSafeArea && size == _lastSize) return;
            _lastSafeArea = safe;
            _lastSize = size;

            var rect = (RectTransform)transform;
            if (size.x <= 0 || size.y <= 0)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
            }
            else
            {
                rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
                rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            }
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
