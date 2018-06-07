// display image from button
// allow setting image by dragging in image

// need to be able to have separate pressed and not-pressed images
//   (should this something that's doable with stock image support for controls?)

// allow not using an image at all but rather just have a screen area

// have any visual representation at all?

// should on-screen controls be proper UI elements? have them as prefabs?

using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A button that is visually represented on-screen and triggered by touch or other pointer
    /// input.
    /// </summary>
    public class OnScreenButton : OnScreenControl
    {
        /// <summary>
        /// If true, the button's value is driven from the pressure value of touch or pen input.
        /// </summary>
        /// <remarks>
        /// This essentially allows having trigger-like buttons as on-screen controls.
        /// </remarks>
        [SerializeField] private bool m_UsePressure;


        // This is just temporary code , currenty using as a testing utility to send event
        // The code in this method will be set to UI events but this method won't be callable later.
        public void SendButtonPushEventToControl()
        {
            // Take the mapped InputControl to the "button" and send an event
            // to the compatible DeveiceState


            // Is this the correct approah?  set up a case for any type of control?
            // Or will we limit OnScreenButtons to only GamePadButtons,  OnScreenKeyboards to only Keyboards? etc?

            if (m_Control.GetType() == typeof(KeyControl))
            {
                var key = (KeyControl)m_Control;
                InputSystem.QueueStateEvent(m_Control.m_Device, new KeyboardState(key.keyCode));
            }
            // do same for GamePad
            // .
            // .
            // .
            // so same for mouse etc etc

            InputSystem.Update();
        }
    }
}
