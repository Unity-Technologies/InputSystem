#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    // A BaseEventData with added device info.
    internal class ExtendedSubmitCancelEventData : BaseEventData, INavigationEventData
    {
        /// <summary>
        /// The <see cref="InputDevice"/> that generated the axis input.
        /// </summary>
        public InputDevice device { get; set; }

        public ExtendedSubmitCancelEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }
    }
}
#endif
