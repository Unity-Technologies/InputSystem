#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.UI;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics record for tracking engagement with Input Action Asset editor(s).
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class VirtualMouseInputEditorAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "inputVirtualMouseInputEditor";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        [Serializable]
        internal struct Data : IEquatable<Data>, UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            public CursorMode cursorMode;
            public float cursorSpeed;
            public float scrollSpeed;

            public enum CursorMode
            {
                SoftwareCursor = 0,
                HardwareCursorIfAvailable = 1
            }

            private static CursorMode ToCursorMode(VirtualMouseInput.CursorMode value)
            {
                switch (value)
                {
                    case VirtualMouseInput.CursorMode.SoftwareCursor:
                        return CursorMode.SoftwareCursor;
                    case VirtualMouseInput.CursorMode.HardwareCursorIfAvailable:
                        return CursorMode.HardwareCursorIfAvailable;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }

            public Data(VirtualMouseInput value)
            {
                cursorMode = ToCursorMode(value.cursorMode);
                cursorSpeed = value.cursorSpeed;
                scrollSpeed = value.scrollSpeed;
            }

            public bool Equals(Data other)
            {
                return cursorMode == other.cursorMode && cursorSpeed.Equals(other.cursorSpeed) && scrollSpeed.Equals(other.scrollSpeed);
            }

            public override bool Equals(object obj)
            {
                return obj is Data other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int)cursorMode, cursorSpeed, scrollSpeed);
            }
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

        private readonly Data m_Data;

        public VirtualMouseInputEditorAnalytic(ref Data data)
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
    }
}
#endif
