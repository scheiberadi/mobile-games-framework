using UnityEngine;

namespace EvasLearningWorld.App
{
    // Slides a cloud image across its (masked) window view, left to right, and wraps around forever.
    // Phase-based so the start position needs no layout: x runs from just outside the left edge to just
    // outside the right edge of the parent, whatever its width turns out to be.
    public sealed class CloudDrift : MonoBehaviour
    {
        public float Speed = 15f;   // canvas units per second
        public float Phase;         // 0..1 start position along the crossing

        private RectTransform _rect;
        private RectTransform _parent;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _parent = (RectTransform)transform.parent;
        }

        private void Update()
        {
            var size = _rect.rect.width;
            var span = _parent.rect.width + size;
            if (span <= 0f) return;
            Phase = (Phase + Speed * Time.deltaTime / span) % 1f;
            var p = _rect.anchoredPosition;
            p.x = -size * 0.5f + Phase * span;
            _rect.anchoredPosition = p;
        }
    }
}
