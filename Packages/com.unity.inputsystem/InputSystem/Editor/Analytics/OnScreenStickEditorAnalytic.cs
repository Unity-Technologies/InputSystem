#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.OnScreen;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics record for tracking engagement with Input Action Asset editor(s).
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class OnScreenStickEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "inputOnScreenStickEditor";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        /// <summary>
        /// Represents select configuration data of interest related to an <see cref="OnScreenStick"/> component.
        /// </summary>
        [Serializable]
        internal struct Data : IEquatable<Data>, UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
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
                movementRange = value.movementRange;
                dynamicOriginRange = value.dynamicOriginRange;
                useIsolatedInputActions = value.useIsolatedInputActions;
            }

            public OnScreenStickBehaviour behavior;
            public float movementRange;
            public float dynamicOriginRange;
            public bool useIsolatedInputActions;

            public bool Equals(Data other)
            {
                return behavior == other.behavior &&
                    movementRange.Equals(other.movementRange) &&
                    dynamicOriginRange.Equals(other.dynamicOriginRange) &&
                    useIsolatedInputActions == other.useIsolatedInputActions;
            }

            public override bool Equals(object obj)
            {
                return obj is Data other && Equals(other);
            }

            public override int GetHashCode()
            {
                // Note: Not using HashCode.Combine since not available in older C# versions
                var hashCode = behavior.GetHashCode();
                hashCode = (hashCode * 397) ^ movementRange.GetHashCode();
                hashCode = (hashCode * 397) ^ dynamicOriginRange.GetHashCode();
                hashCode = (hashCode * 397) ^ useIsolatedInputActions.GetHashCode();
                return hashCode;
            }
        }

        private readonly Data m_Data;

        public OnScreenStickEditorAnalytic(ref Data data)
        {
            m_Data = data;
        }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            data = m_Data;
            error = null;
            return true;
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);
    }
}
#endif
