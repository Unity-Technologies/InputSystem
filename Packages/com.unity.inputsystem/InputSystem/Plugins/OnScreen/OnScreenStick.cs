using System;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A stick control displayed on screen and moved around by touch or other pointer
    /// input.
    /// </summary>
    public class OnScreenStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        Vector3 m_StartPos;
        public int MovementRange = 50;
        public Vector2 StickPosition;

        void Start()
        {
            m_StartPos = transform.position;
            StickPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            Vector3 newPos = Vector3.zero;
            int delta = 0;

            delta = (int)(data.position.x - m_StartPos.x);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.x = delta;

            delta = (int)(data.position.y - m_StartPos.y);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.y = delta;

            StickPosition.x = newPos.x / MovementRange;
            StickPosition.y = newPos.y / MovementRange;

            SendValueToControl(StickPosition);

            transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
        }

        public void OnPointerUp(PointerEventData data)
        {
            transform.position = m_StartPos;
            StickPosition = Vector2.zero;
            SendValueToControl(StickPosition);
        }
    }
}
