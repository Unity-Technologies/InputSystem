using UnityEngine.EventSystems;

////TODO: custom icon for OnScreenStick component

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A stick control displayed on screen and moved around by touch or other pointer
    /// input.
    /// </summary>
    [AddComponentMenu("Input/On-Screen Stick")]
    public class OnScreenStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private void Start()
        {
            m_StartPos = ((RectTransform)transform).anchoredPosition;
        }

        public void OnPointerDown(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponentInParent<RectTransform>(), data.position, data.pressEventCamera, out m_PointerDownPos);
        }

        public void OnDrag(PointerEventData data)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponentInParent<RectTransform>(), data.position, data.pressEventCamera, out position);
            var delta = position - m_PointerDownPos;

            delta = Vector2.ClampMagnitude(delta, movementRange);
            ((RectTransform)transform).anchoredPosition = m_StartPos + (Vector3)delta;

            var newPos = new Vector2(delta.x / movementRange, delta.y / movementRange);
            SendValueToControl(newPos);
        }

        public void OnPointerUp(PointerEventData data)
        {
            ((RectTransform)transform).anchoredPosition = m_StartPos;
            SendValueToControl(Vector2.zero);
        }

        public int movementRange = 50;

        private Vector3 m_StartPos;
        private Vector2 m_PointerDownPos;
    }
}
