using System;
using UnityEngine;
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
        public int m_MovementRange = 100;
        Camera m_EventCamera;
        Vector3 m_StartPos;
        Vector2 m_PointerDownPos;

        void Start()
        {
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            SendValueToControl(new Vector2(0.0f, 0.5f));
        }

        public void OnPointerUp(PointerEventData data)
        {
            SendValueToControl(Vector2.zero);
        }
    }
}
