#if UNITY_EDITOR
using System;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Enumeration type identifying a Input System MonoBehavior component.
    /// </summary>
    [Serializable]
    internal enum InputSystemComponent
    {
        // Feature components
        PlayerInput = 1,
        PlayerInputManager = 2,
        OnScreenStick = 3,
        OnScreenButton = 4,
        VirtualMouseInput = 5,

        // Debug components
        TouchSimulation = 1000,

        // Integration components
        StandaloneInputModule = 2000,
        InputSystemUIInputModule = 2001,
    }

    /// <summary>
    /// Analytics record for tracking engagement with Input Component editor(s).
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class InputComponentEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_component_editor_closed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        /// <summary>
        /// The associated component type.
        /// </summary>
        private readonly InputSystemComponent m_Component;

        /// <summary>
        /// Represents component inspector editor data.
        /// </summary>
        /// <remarks>
        /// Ideally this struct should be readonly but then Unity cannot serialize/deserialize it.
        /// </remarks>
        [Serializable]
        public struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Creates a new <c>ComponentEditorData</c> instance.
            /// </summary>
            /// <param name="component">The associated component.</param>
            public Data(InputSystemComponent component)
            {
                this.component = component;
            }

            /// <summary>
            /// Defines the associated component.
            /// </summary>
            public InputSystemComponent component;
        }

        public InputComponentEditorAnalytic(InputSystemComponent component)
        {
            info = new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);
            m_Component = component;
        }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            data = new Data(m_Component);
            error = null;
            return true;
        }

        public InputAnalytics.InputAnalyticInfo info { get; }
    }
}
#endif
