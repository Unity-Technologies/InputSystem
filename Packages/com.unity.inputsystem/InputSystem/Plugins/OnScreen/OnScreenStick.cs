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
            var newPos = Vector2.zero;

            ////REVIEW: is this Y up?

            ////REVIEW: this doesn't make sense; data.position and transform.position are not in the same coordinate space

            var deltaX = (int)(data.position.x - m_StartPos.x);
            deltaX = Mathf.Clamp(deltaX, -movementRange, movementRange);
            newPos.x = deltaX;

            var deltaY = (int)(data.position.y - m_StartPos.y);
            deltaY = Mathf.Clamp(deltaY, -movementRange, movementRange);
            newPos.y = deltaY;

            ////FIXME: this is setting up a square movement space, not a radial one; relies on normalization on the control to work

            newPos.x /= movementRange;
            newPos.y /= movementRange;

            SendValueToControl(newPos);

            transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z);
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
