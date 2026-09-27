using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EvasLearningWorld.App
{
    // A piece of furniture the child can pick up and move (House screen). While a finger is down it follows
    // the finger 1:1 in canvas units - screen-pixel deltas are divided by the parent canvas's own scale factor,
    // since the canvas is scaled with screen size - and it reports Begin/End back to whoever created it
    // (HouseScreen) instead of knowing anything about slots or snapping itself, so all placement rules live in
    // one place. Construction follows EvaUi.IconButton's pattern: a static factory building the GameObject and
    // its components, anchor/pivot/position/size, no separate "configure after creation" step.
    public sealed class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string ItemId { get; private set; }
        public RectTransform Rect { get; private set; }

        public event Action<DragItem> BeginDrag;
        public event Action<DragItem> EndDrag;

        private Canvas _canvas;

        public static DragItem Create(Transform parent, string itemId, Sprite icon, Vector2 position, float size)
        {
            var go = new GameObject("Drag_" + itemId, typeof(RectTransform), typeof(Image), typeof(DragItem), typeof(TapTarget), typeof(PressFeedback));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = true;

            var drag = go.GetComponent<DragItem>();
            drag.ItemId = itemId;
            drag.Rect = rect;
            drag._canvas = parent.GetComponentInParent<Canvas>();
            return drag;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            transform.SetAsLastSibling(); // draw above every other item and slot outline while it moves
            BeginDrag?.Invoke(this);
            // Unity starts the drag only after the pointer passed the (child-friendly, wide) drag threshold and OnDrag
            // reports per-frame deltas, so catch up the distance already travelled or the item trails the finger.
            var scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Rect.anchoredPosition += (eventData.position - eventData.pressPosition) / scale;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Rect.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData) => EndDrag?.Invoke(this);
    }
}
