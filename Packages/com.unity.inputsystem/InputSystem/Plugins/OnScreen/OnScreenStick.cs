using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A stick control displayed on screen and moved around by touch or other pointer
    /// input.
    /// </summary>
    public class OnScreenStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private void Start()
        {
            m_StartPos = transform.position;
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            var newPos = Vector3.zero;
            var delta = 0;

            delta = (int)(data.position.x - m_StartPos.x);
            delta = Mathf.Clamp(delta, -movementRange, movementRange);
            newPos.x = delta;

            delta = (int)(data.position.y - m_StartPos.y);
            delta = Mathf.Clamp(delta, -movementRange, movementRange);
            newPos.y = delta;

            newPos.x /= movementRange;
            newPos.y /= movementRange;

            SendValueToControl(newPos);

            transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
        }

        public void OnPointerUp(PointerEventData data)
        {
            transform.position = m_StartPos;
            SendValueToControl(Vector2.zero);
        }

        public int movementRange = 50;

        private Vector3 m_StartPos;
    }
}
