#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_ENABLE_UI
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
        public const string kEventName = "input_virtualmouseinput_editor_destroyed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        [Serializable]
        internal struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Maps to <see cref="VirtualMouseInput.cursorMode"/>. Determines which cursor representation to use.
            /// </summary>
            public CursorMode cursor_mode;

            /// <summary>
            /// Maps to <see cref="VirtualMouseInput.cursorSpeed" />. Speed in pixels per second with which to move the cursor.
            /// </summary>
            public float cursor_speed;

            /// <summary>
            /// Maps to <see cref="VirtualMouseInput.cursorMode"/>. Multiplier for values received from <see cref="VirtualMouseInput.scrollWheelAction"/>.
            /// </summary>
            public float scroll_speed;

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
                cursor_mode = ToCursorMode(value.cursorMode);
                cursor_speed = value.cursorSpeed;
                scroll_speed = value.scrollSpeed;
            }
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

        private readonly UnityEditor.Editor m_Editor;

        public VirtualMouseInputEditorAnalytic(UnityEditor.Editor editor)
        {
            m_Editor = editor;
        }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            try
            {
                data = new Data(m_Editor.target as VirtualMouseInput);
                error = null;
            }
            catch (Exception e)
            {
                data = null;
                error = e;
            }
            return true;
        }
    }
}
#endif
