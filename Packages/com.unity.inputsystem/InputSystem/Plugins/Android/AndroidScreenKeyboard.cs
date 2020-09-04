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

        private AndroidJavaObject m_KeyboardObject;
        private AndroidScreenKeyboardCallbacks m_Callbacks;

        private AndroidScreenKeyboard()
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

            ms_Instance = null;
        }

        /// <summary>
        /// Note: We're not creating AndroidJavaObject in constructor, since AndroidScreenKeyboard is called when global static variables are initialized
        ///       But it seems you cannot create AndroidJavaObject's at this time yet
        /// </summary>
        /// <returns></returns>
        //private AndroidJavaObject GetOrCreateKeyboardObject()
        //{
        //    if (m_KeyboardObject == null)

        //    return m_KeyboardObject;
        //}

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

        internal override void SimulateKeyEvent(int keyCode)
        {
            m_KeyboardObject.Call("simulateKeyEvent", keyCode);
        }

        internal override bool logging
        {
            get
            {
                return m_KeyboardObject.Call<bool>("getLogging");
            }

            set
            {
                m_KeyboardObject.Call("setLogging", value);
            }
        }
    }
}
#endif
