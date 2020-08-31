using System;
using System.Threading;
using UnityEngine.Scripting;

#if UNITY_EDITOR || UNITY_ANDROID

namespace UnityEngine.InputSystem.Android
{
    // TODO: check if stripping doesn't remove these methods
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
            void OnStatusChanged(int state)
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

        protected override void InternalShow()
        {
            if (m_KeyboardObject == null)
                m_KeyboardObject = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard");

            var showParams = m_ShowParams;
            m_KeyboardObject.Call("show",
                new ScreenKeyboardCallbacks(this),
                (int)showParams.type,
                showParams.initialText,
                showParams.placeholderText,
                showParams.autocorrection,
                showParams.multiline,
                showParams.secure,
                showParams.alert,
                showParams.inputFieldHidden);
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
    }
}
#endif
