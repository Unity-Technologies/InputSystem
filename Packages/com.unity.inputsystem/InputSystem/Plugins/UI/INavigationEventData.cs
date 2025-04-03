#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI

namespace UnityEngine.InputSystem.UI
{
    internal interface INavigationEventData
    {
        /// <summary>
        /// The <see cref="InputDevice"/> that generated the axis input.
        /// </summary>
        public InputDevice device { get; }
    }
}
#endif
