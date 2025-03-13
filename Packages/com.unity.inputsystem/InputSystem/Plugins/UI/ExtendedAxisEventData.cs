#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    // AxisEventData has no ToString. Also added device info. Keeping
    // it internal for now.
    internal class ExtendedAxisEventData : AxisEventData, INavigationEventData
    {
        /// <summary>
        /// The <see cref="InputDevice"/> that generated the axis input.
        /// </summary>
        /// <seealso cref="Keyboard"/>
        /// <seealso cref="Gamepad"/>
        public InputDevice device { get; set; }

        public ExtendedAxisEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }

        public override string ToString()
        {
            return $"MoveDir: {moveDir}\nMoveVector: {moveVector}";
        }
    }
}
#endif
