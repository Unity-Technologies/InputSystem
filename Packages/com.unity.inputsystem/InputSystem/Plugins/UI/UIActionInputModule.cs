using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// Input module that takes its input from <see cref="InputAction">input actions</see>.
    /// </summary>
    public class UIActionInputModule : UIInputModule
    {
        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>
        [Tooltip("Action that delivers a Vector2 of screen coordinates.")]
        public InputActionProperty pointAction;

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        [Tooltip("Action that delivers a relative motion Vector2.")]
        public InputActionProperty moveAction;

        public InputActionProperty leftClickAction;
        public InputActionProperty middleClickAction;
        public InputActionProperty rightClickAction;

        public InputActionProperty scrollAction;

        public override void Process()
        {
        }
    }
}
