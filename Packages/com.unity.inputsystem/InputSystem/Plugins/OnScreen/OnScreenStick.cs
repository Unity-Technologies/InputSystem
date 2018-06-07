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
        private InputControl<Vector2> m_StickControl;

        private void SendStickMovementToControl(Vector2 value)
        {
            if (m_StickControl == null)
                m_StickControl = (InputControl<Vector2>)m_Control;

            InputEventPtr eventPtr;
            var buffer = StateEvent.From(m_Control.device, out eventPtr);
            m_StickControl.WriteValueInto(eventPtr, value);

            // This isn't working, need to fix
            //  eventPtr.time = InputRuntime.s_Runtime.currentTime;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }

        public void OnPointerDown(PointerEventData data)
        {
        }

        public void OnDrag(PointerEventData data)
        {
            // Need to make this real.
            // right now just send it something for tests
            SendStickMovementToControl(new Vector2(0.0f, 0.5f));
        }

        public void OnPointerUp(PointerEventData data)
        {
            SendStickMovementToControl(new Vector2(0.0f, 0.0f));
        }
    }
}
