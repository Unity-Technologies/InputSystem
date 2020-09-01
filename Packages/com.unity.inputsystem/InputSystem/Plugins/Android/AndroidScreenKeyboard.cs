using System;
using System.Threading;
using UnityEngine.Scripting;

#if UNITY_EDITOR || UNITY_ANDROID

namespace UnityEngine.InputSystem.Android
{
    internal class AndroidScreenKeyboard : ScreenKeyboard
    {
        private static AndroidScreenKeyboard ms_Instance;

        public static AndroidScreenKeyboard instance
        {
            get
            {
                if (ms_Instance == null)
                    ms_Instance = new AndroidScreenKeyboard();
                return ms_Instance;
            }
        }

        class ScreenKeyboardCallbacks : AndroidJavaProxy
        {
            AndroidScreenKeyboard m_Parent;
            private int m_MainThreadId;
            public ScreenKeyboardCallbacks(AndroidScreenKeyboard parent)
                : base("com.unity.inputsystem.AndroidScreenKeyboard$IScreenKeyboardCallbacks")
            {
                m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
                m_Parent = parent;
            }

#if UNITY_ANDROID
            [Preserve]
#endif
            void OnTextChanged(string text)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnTextChanged was executed from incorrect thread");
                m_Parent.ReportInputFieldChange(text);
            }

#if UNITY_ANDROID
            [Preserve]
#endif
            void OnStateChanged(int state)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnStatusChanged was executed from incorrect thread");
                m_Parent.ReportStateChange((ScreenKeyboardState)state);
            }

#if UNITY_ANDROID
            [Preserve]
#endif
            void OnSelectionChanged(int start, int length)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnSelectionChanged was executed from incorrect thread");
                m_Parent.ReportSelectionChange(start, length);
            }
        }

        // Allow only one instance of java keyboard, because only one can be shown at the time
        private static AndroidJavaObject m_KeyboardObject;

        private static AndroidJavaObject GetOrCreateKeyboardObject()
        {
            if (m_KeyboardObject == null)
                m_KeyboardObject = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard");
            return m_KeyboardObject;
        }

        protected override void InternalShow()
        {
            GetOrCreateKeyboardObject().Call("show",
                new ScreenKeyboardCallbacks(this),
                (int)m_ShowParams.type,
                m_ShowParams.initialText,
                m_ShowParams.placeholderText,
                m_ShowParams.autocorrection,
                m_ShowParams.multiline,
                m_ShowParams.secure,
                m_ShowParams.alert,
                m_ShowParams.inputFieldHidden);
        }

        protected override void InternalHide()
        {
            if (m_KeyboardObject != null)
                m_KeyboardObject.Call("dismiss");
        }

        public override string inputFieldText
        {
            get
            {
                if (m_KeyboardObject != null)
                    return m_KeyboardObject.Call<string>("getText");
                return string.Empty;
            }
            set
            {
                if (m_KeyboardObject != null)
                    m_KeyboardObject.Call("setText", value);
            }
        }

        public override RangeInt selection
        {
            get
            {
                if (m_KeyboardObject != null)
                {
                    var combined = m_KeyboardObject.Call<long>("getSelection");
                    unchecked
                    {
                        return new RangeInt((int)(0xFFFFFFFF & combined), (int)(combined >> 32));
                    }
                }

                return new RangeInt(0, 0);
            }
            set
            {
                if (m_KeyboardObject != null)
                    m_KeyboardObject.Call("setSelection", value.start, value.length);
            }
        }

        internal override void SimulateKeyEvent(int keyCode)
        {
            m_KeyboardObject.Call("simulateKeyEvent", keyCode);
        }

        internal override bool logging
        {
            get
            {
                return GetOrCreateKeyboardObject().Call<bool>("getLogging");
            }

            set
            {
                GetOrCreateKeyboardObject().Call("setLogging", value);
            }
        }
    }
}
#endif
