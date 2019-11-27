#if UNITY_EDITOR || UNITY_ANDROID
using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Android
{
    public class AndroidScreenKeyboard : ScreenKeyboard
    {
        class ScreenKeyboardCallbacks : AndroidJavaProxy
        {
            AndroidScreenKeyboard m_Parent;
            public ScreenKeyboardCallbacks(AndroidScreenKeyboard parent)
                : base("com.unity.inputsystem.AndroidScreenKeyboard$IScreenKeyboardCallbacks")
            {
                m_Parent = parent;
            }

            void OnTextChanged(string text)
            {
                m_Parent.OnChangeInputField(text);
            }

            void OnStatusChanged(int status)
            {
                var newState = m_Parent.m_State;
                newState.Status = (ScreenKeyboardStatus)status;
                m_Parent.OnChangeState(newState);
            }
        }

        // Allow only one instance of java keyboard, because only one can be shown at the time
        private static AndroidJavaObject m_KeyboardObject;

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            if (m_KeyboardObject == null)
                m_KeyboardObject = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard");

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

        public override void Hide()
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
    }
}
#endif
