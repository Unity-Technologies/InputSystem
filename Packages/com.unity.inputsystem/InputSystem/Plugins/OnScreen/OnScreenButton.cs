// display image from button
// allow setting image by dragging in image

// need to be able to have separate pressed and not-pressed images
//   (should this something that's doable with stock image support for controls?)

// allow not using an image at all but rather just have a screen area

// have any visual representation at all?

// should on-screen controls be proper UI elements? have them as prefabs?

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
        private InputControl<float> m_ButtonControl;

        private void SendButtonPushToControl(float value)
        {
            if (m_ButtonControl == null)
                m_ButtonControl = (InputControl<float>)m_Control;

            InputEventPtr eventPtr;
            var buffer = StateEvent.From(m_Control.device, out eventPtr);
            m_ButtonControl.WriteValueInto(eventPtr, value);

            // This isn't working, need to fix
            //  eventPtr.time = InputRuntime.s_Runtime.currentTime;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }

        public void OnPointerUp(PointerEventData data)
        {
            SendButtonPushToControl(0.0f);
        }

        public void OnPointerDown(PointerEventData data)
        {
            SendButtonPushToControl(1.0f);
        }
    }
}
