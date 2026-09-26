using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.App
{
    // Pans the map: forwards the drag, in canvas units, to the owner. It sits on the world container, so a drag that
    // starts on a building or on the backdrop still pans (uGUI then also cancels the building's click).
    public sealed class MapDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _viewport;
        private Action<Vector2> _pan;
        private Action _end;

        public void Init(RectTransform viewport, Action<Vector2> pan, Action end = null)
        {
            _viewport = viewport;
            _pan = pan;
            _end = end;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) _end?.Invoke();
        }

        // Touch pointer ids are not finger indices (they grow with every touch), so remember the one that started the drag.
        private int _pointerId;

        public void OnBeginDrag(PointerEventData eventData) => _pointerId = eventData.pointerId;

        public void OnDrag(PointerEventData eventData)
        {
            if (_viewport == null || _pan == null) return;
            if (eventData.pointerId != _pointerId) return; // only the finger that started the drag pans
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out var from);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out var to);
            _pan(to - from);
        }
    }
}
