#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using UnityEngine.InputSystem.OnScreen;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics record for tracking engagement with Input Action Asset editor(s).
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey, version: 2)]
#endif // UNITY_2023_2_OR_NEWER
    internal class OnScreenStickEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_onscreenstick_editor_destroyed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        /// <summary>
        /// Represents select configuration data of interest related to an <see cref="OnScreenStick"/> component.
        /// </summary>
        [Serializable]
        internal struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            public enum OnScreenStickBehaviour
            {
                RelativePositionWithStaticOrigin = 0,
                ExactPositionWithStaticOrigin = 1,
                ExactPositionWithDynamicOrigin = 2,
            }

            private static OnScreenStickBehaviour ToBehaviour(OnScreenStick.Behaviour value)
            {
                switch (value)
                {
                    case OnScreenStick.Behaviour.RelativePositionWithStaticOrigin:
                        return OnScreenStickBehaviour.RelativePositionWithStaticOrigin;
                    case OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin:
                        return OnScreenStickBehaviour.ExactPositionWithDynamicOrigin;
                    case OnScreenStick.Behaviour.ExactPositionWithStaticOrigin:
                        return OnScreenStickBehaviour.ExactPositionWithStaticOrigin;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }

            public Data(OnScreenStick value)
            {
                behavior = ToBehaviour(value.behaviour);
                movement_range = value.movementRange;
                dynamic_origin_range = value.dynamicOriginRange;
                use_isolated_input_actions = value.useIsolatedInputActions;
            }

            public OnScreenStickBehaviour behavior;
            public float movement_range;
            public float dynamic_origin_range;
            public bool use_isolated_input_actions;
        }

        private readonly UnityEditor.Editor m_Editor;

        public OnScreenStickEditorAnalytic(UnityEditor.Editor editor)
        {
            m_Editor = editor;
        }

#if UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            try
            {
                data = new Data(m_Editor.target as OnScreenStick);
                error = null;
            }
            catch (Exception e)
            {
                data = null;
                error = e;
            }
            return true;
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);
    }
}
#endif // UNITY_EDITOR
