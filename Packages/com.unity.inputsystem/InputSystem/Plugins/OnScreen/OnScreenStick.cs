using System;
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
        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            // Need to make this real.
            // right now just send it something for tests
            SendStateEventToControl(new Vector2(0.0f, 0.5f));
        }

        public void OnPointerUp(PointerEventData data)
        {
            SendStateEventToControl(new Vector2(0.0f, 0.0f));
        }
    }
}
