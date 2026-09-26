using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EvasLearningWorld.App
{
    // Pans the map: forwards the drag, in canvas units, to the owner. It sits on the world container, so a drag that
    // starts on a building or on the backdrop still pans (uGUI then also cancels the building's click).
    public sealed class MapDrag : MonoBehaviour, IDragHandler
    {
        private RectTransform _viewport;
        private Action<Vector2> _pan;

        public void Init(RectTransform viewport, Action<Vector2> pan)
        {
            _viewport = viewport;
            _pan = pan;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_viewport == null || _pan == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out var from);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out var to);
            _pan(to - from);
        }
    }
}
