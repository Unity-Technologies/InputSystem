using System;
using System.Threading;
using UnityEngine.Scripting;

#if (UNITY_EDITOR || UNITY_ANDROID) && !DISABLE_SCREEN_KEYBOARD

namespace UnityEngine.InputSystem.Android
{
    internal class AndroidScreenKeyboard : ScreenKeyboard
    {
        class AndroidScreenKeyboardCallbacks : AndroidJavaProxy
        {
            AndroidScreenKeyboard m_Parent;
            private int m_MainThreadId;
            public AndroidScreenKeyboardCallbacks(AndroidScreenKeyboard parent)
                : base("com.unity.inputsystem.AndroidScreenKeyboard$IScreenKeyboardCallbacks")
            {
                m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
                m_Parent = parent;
            }

            [Preserve]
            void OnTextChanged(string text)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnTextChanged was executed from incorrect thread");
                m_Parent.ReportInputFieldChange(text);
            }

            [Preserve]
            void OnStateChanged(int state)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnStatusChanged was executed from incorrect thread");
                m_Parent.ReportStateChange((ScreenKeyboardState)state);
            }

            [Preserve]
            void OnSelectionChanged(int start, int length)
            {
                if (Thread.CurrentThread.ManagedThreadId != m_MainThreadId)
                    throw new Exception("OnSelectionChanged was executed from incorrect thread");
                m_Parent.ReportSelectionChange(start, length);
            }
        }

        private AndroidJavaObject m_KeyboardObject;
        private readonly AndroidScreenKeyboardCallbacks m_Callbacks;

        internal AndroidScreenKeyboard()
        {
            m_Callbacks = new AndroidScreenKeyboardCallbacks(this);
            m_KeyboardObject = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard");
        }

        public override void Dispose()
        {
            if (m_KeyboardObject != null)
            {
                m_KeyboardObject.Dispose();
                m_KeyboardObject = null;
            }
        }

        protected override void InternalShow()
        {
            m_KeyboardObject.Call("show",
                m_Callbacks,
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

        public override Rect occludingArea
        {
            get
            {
                var area = m_KeyboardObject.Call<int[]>("getArea");
                return new Rect(area[0], area[1], area[2] - area[0], Screen.height - (area[3] - area[1]));
            }
        }

        public override void SimulateKeyEvent(int keyCode)
        {
            m_KeyboardObject.Call("simulateKeyEvent", keyCode);
        }

        public override bool logging
        {
            get => m_KeyboardObject.Call<bool>("getLogging");
            set => m_KeyboardObject.Call("setLogging", value);
        }
    }
}
#endif
