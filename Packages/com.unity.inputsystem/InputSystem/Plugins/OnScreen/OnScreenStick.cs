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
        public Vector3 deltaPos;
        public float stickX = 0.0f;
        public float stickY = 0.0f;

        void Start()
        {
            m_StartPos = transform.position;
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            Vector3 newPos = Vector3.zero;

            {
                int delta = (int)(data.position.x - m_StartPos.x);
                delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
                newPos.x = delta;
            }

            {
                int delta = (int)(data.position.y - m_StartPos.y);
                delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
                newPos.y = delta;
            }

            deltaPos = newPos;

            stickX = deltaPos.x / MovementRange;
            stickY = deltaPos.y / MovementRange;

            SendValueToControl(new Vector2(stickX, stickY));
        }

        public void OnPointerUp(PointerEventData data)
        {
            transform.position = m_StartPos;
            deltaPos = Vector3.zero;
            SendValueToControl(Vector2.zero);
            stickX = 0.0f;
            stickY = 0.0f;
        }
    }
}
