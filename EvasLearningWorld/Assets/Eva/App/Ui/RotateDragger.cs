using System;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // A drag handle orbiting a piece: dragging it around the piece's own centre rotates the piece to match the
    // handle's angle (0 = straight up, clockwise), snapped to StepDegrees if set (0 = continuous, any angle).
    // The one new mechanic Rotate the Piece needs (spec 4.1) - distinct from PathDragger's one-degree-of-freedom
    // slide along a fixed line. HandleRect and PieceRect must be siblings under the same field (not parent and
    // child), so both share one fixed, unrotated coordinate frame - a handle parented under its own (rotating)
    // piece would have to measure the drag against a frame that changes with every drag update.
    public sealed class RotateDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform HandleRect { get; private set; }
        public RectTransform PieceRect { get; set; }
        public float Radius { get; set; } = 160f;

        // 0 = any angle; otherwise the handle only ever reports a multiple of this many degrees.
        public float StepDegrees { get; set; }

        // The last angle (degrees, 0-360, clockwise from straight up) a drag or SnapTo placed the piece at.
        public float Angle { get; private set; }

        // Raised with the new angle every time the drag moves. Not raised by SnapTo.
        public event Action<float> Rotated;

        // Raised when the finger lifts.
        public event Action Released;

        // Gates OnBeginDrag/OnDrag/OnEndDrag while a Hint/Demonstrate coroutine is animating the piece itself.
        public bool Enabled { get; set; } = true;

        private RectTransform _field;

        public static RotateDragger Create(RectTransform field, Sprite icon, float size)
        {
            var go = new GameObject("RotateHandle", typeof(RectTransform), typeof(Image), typeof(RotateDragger), typeof(TapTarget), typeof(PressFeedback));
            go.transform.SetParent(field, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;

            var dragger = go.GetComponent<RotateDragger>();
            dragger.HandleRect = rect;
            dragger._field = field;
            return dragger;
        }

        // Places the piece and handle at the given angle without raising Rotated - for the screen's own resets
        // and its Hint/Demonstrate animations.
        public void SnapTo(float angleDegrees)
        {
            Angle = RotateThePieceRoundGenerator.Normalize(angleDegrees);
            Apply();
        }

        public void OnBeginDrag(PointerEventData eventData) => Drag(eventData);
        public void OnDrag(PointerEventData eventData) => Drag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Enabled) return;
            Released?.Invoke();
        }

        private void Drag(PointerEventData eventData)
        {
            if (!Enabled || PieceRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_field, eventData.position, eventData.pressEventCamera, out var local);
            var centre = PieceRect.anchoredPosition;
            var angle = Mathf.Atan2(local.x - centre.x, local.y - centre.y) * Mathf.Rad2Deg;
            angle = RotateThePieceRoundGenerator.Normalize(angle);
            if (StepDegrees > 0f) angle = RotateThePieceRoundGenerator.Normalize(Mathf.Round(angle / StepDegrees) * StepDegrees);

            Angle = angle;
            Apply();
            Rotated?.Invoke(Angle);
        }

        private void Apply()
        {
            if (PieceRect != null) PieceRect.localRotation = Quaternion.Euler(0f, 0f, -Angle);
            var centre = PieceRect != null ? PieceRect.anchoredPosition : Vector2.zero;
            var rad = Angle * Mathf.Deg2Rad;
            HandleRect.anchoredPosition = centre + new Vector2(Mathf.Sin(rad) * Radius, Mathf.Cos(rad) * Radius);
        }
    }
}
