using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input.Layouts;

////TODO: pressure support
////TODO: custom icon for OnScreenButton component

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    /// <summary>
    /// A button that is visually represented on-screen and triggered by touch or other pointer
    /// input.
    /// </summary>
    [AddComponentMenu("Input/On-Screen Button")]
    public class OnScreenButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData data)
        {
            SendValueToControl(0.0f);
        }

        public void OnPointerDown(PointerEventData data)
        {
            SendValueToControl(1.0f);
        }

        /// <summary>
        /// If true, the button's value is driven from the pressure value of touch or pen input.
        /// </summary>
        /// <remarks>
        /// This essentially allows having trigger-like buttons as on-screen controls.
        /// </remarks>
        [SerializeField] private bool m_UsePressure;

        [InputControl(layout = "Button")]
        [SerializeField]
        internal string m_ControlPath;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }
    }
}
