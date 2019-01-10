using System;

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    internal class AndroidScreenKeyboard : ScreenKeyboard
    {
        class ScreenKeyboardCallbacks : AndroidJavaProxy
        {
            AndroidScreenKeyboard m_Parent;
            public ScreenKeyboardCallbacks(AndroidScreenKeyboard parent)
                : base("com.unity.inputsystem.AndroidScreenKeyboard$IScreenKeyboardCallbacks")
            {
                m_Parent = parent;
            }

            void OnTextChanged(string text, int selectionStart, int selectionLength)
            {
                m_Parent.ChangeInputFieldText(new InputFieldEventArgs() { text = text, selection = new RangeInt(selectionStart, selectionLength) });
            }

            void OnStatusChanged(int status)
            {
                m_Parent.ChangeStatus((ScreenKeyboardStatus)status);
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
                showParams.alert);
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

        public override Rect occludingArea
        {
            get
            {
                //if (m_KeyboardObject != null)
                //    return m_KeyboardObject.Call<string>("getText");
                return Rect.zero;
            }
        }
    }
}
