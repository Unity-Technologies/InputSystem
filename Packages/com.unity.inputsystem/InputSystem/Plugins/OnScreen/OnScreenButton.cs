// display image from button
// allow setting image by dragging in image

// need to be able to have separate pressed and not-pressed images
//   (should this something that's doable with stock image support for controls?)

// allow not using an image at all but rather just have a screen area

// have any visual representation at all?

// should on-screen controls be proper UI elements? have them as prefabs?

using System;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A button that is visually represented on-screen and triggered by touch or other pointer
    /// input.
    /// </summary>
    public class OnScreenButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// If true, the button's value is driven from the pressure value of touch or pen input.
        /// </summary>
        /// <remarks>
        /// This essentially allows having trigger-like buttons as on-screen controls.
        /// </remarks>
        [SerializeField] private bool m_UsePressure;

        public void OnPointerUp(PointerEventData data)
        {
            SendValueToControl(0.0f);
        }

        public void OnPointerDown(PointerEventData data)
        {
            SendValueToControl(1.0f);
        }
    }
}
