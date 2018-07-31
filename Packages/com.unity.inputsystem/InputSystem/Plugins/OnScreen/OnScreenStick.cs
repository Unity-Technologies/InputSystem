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
            m_StartPos = (transform as RectTransform).anchoredPosition;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas)
                m_Camera = canvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponentInParent<RectTransform>(), data.position, m_Camera, out m_PointerDownPos);
        }

        public void OnDrag(PointerEventData data)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponentInParent<RectTransform>(), data.position, m_Camera, out position);
            Vector2 delta = position - m_PointerDownPos;

            delta = Vector2.ClampMagnitude(delta, movementRange);
            (transform as RectTransform).anchoredPosition = m_StartPos + (Vector3)delta;

            var newPos = new Vector2(delta.x / movementRange, delta.y / movementRange);
            SendValueToControl(newPos);
        }

        public void OnPointerUp(PointerEventData data)
        {
            (transform as RectTransform).anchoredPosition = m_StartPos;
            SendValueToControl(Vector2.zero);
        }

        public int movementRange = 50;

        private Vector3 m_StartPos;
        private Vector2 m_PointerDownPos;
        private Camera m_Camera;
    }
}
