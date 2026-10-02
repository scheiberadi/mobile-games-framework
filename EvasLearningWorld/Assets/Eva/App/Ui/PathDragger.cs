using System;
using EvasLearningWorld.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // Drags a character along a fixed corridor path (Rules/FingerMaze.cs). While a finger is down, the character
    // snaps to the nearest point on the path to the raw finger position rather than following the finger 1:1
    // (DragItem's free-form model) - a maze corridor only ever has one degree of freedom, progress along it, and
    // snapping is the forgiveness "wide corridors, no timer" calls for. Reused by every later NAVIGATION game
    // (Follow Numbers/Letters in Order, Shortest Path, Avoid Obstacles, Collect Everything); this component only
    // reports progress, the screen decides what it means (win condition, checkpoints, hazards).
    public sealed class PathDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Rect { get; private set; }
        public Image Image { get; private set; }

        // The last fraction (0-1) along the path SnapTo or a drag placed the character at.
        public float Fraction { get; private set; }

        // Raised with the new fraction (0-1) every time the drag moves. Not raised by SnapTo.
        public event Action<float> Progressed;

        // Raised when the finger lifts, whatever fraction was reached.
        public event Action Released;

        // Raised with the raw local drag point (in the field's own space, before it is snapped onto the path) on
        // every drag move. Most games only need the snapped Fraction/Progressed; Avoid Obstacles is the one
        // exception - it needs the finger's actual position to tell whether it strayed onto a hazard tile beside
        // the corridor, which the snapped position can never reflect (it always sits on the centreline).
        public event Action<WorldPoint> RawMoved;

        // Gates OnBeginDrag/OnDrag while a Hint/Demonstrate coroutine is animating the character itself.
        public bool Enabled { get; set; } = true;

        private RectTransform _field;
        private WorldPoint[] _path;

        public static PathDragger Create(RectTransform field, Sprite icon, float size)
        {
            var go = new GameObject("PathDragger", typeof(RectTransform), typeof(Image), typeof(PathDragger), typeof(TapTarget), typeof(PressFeedback));
            go.transform.SetParent(field, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;

            var dragger = go.GetComponent<PathDragger>();
            dragger.Rect = rect;
            dragger.Image = image;
            dragger._field = field;
            return dragger;
        }

        public void SetPath(WorldPoint[] path) => _path = path;

        // Places the character at the given fraction (0-1) along the current path without raising Progressed -
        // for the screen's own resets and its Hint/Demonstrate animations.
        public void SnapTo(float fraction)
        {
            if (_path == null || _path.Length == 0) return;
            Fraction = Mathf.Clamp01(fraction);
            var p = MapPath.PositionAt(_path, Fraction);
            Rect.anchoredPosition = new Vector2(p.X, p.Y);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Enabled && EvaUi.Sfx != null) EvaUi.Sfx.Pick();
            Drag(eventData);
        }
        public void OnDrag(PointerEventData eventData) => Drag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Enabled) return;
            if (EvaUi.Sfx != null) EvaUi.Sfx.Drop();
            Released?.Invoke();
        }

        private void Drag(PointerEventData eventData)
        {
            if (!Enabled || _path == null || _path.Length < 2) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_field, eventData.position, eventData.pressEventCamera, out var local);
            var point = new WorldPoint(local.x, local.y);
            RawMoved?.Invoke(point);
            var fraction = FingerMazePath.NearestFraction(_path, point);
            SnapTo(fraction);
            Progressed?.Invoke(fraction);
        }
    }
}
