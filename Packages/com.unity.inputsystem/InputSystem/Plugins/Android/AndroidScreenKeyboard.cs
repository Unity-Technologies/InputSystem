using System;

namespace UnityEngine.Experimental.Input
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
                Debug.Log("OnTextChanged: " + text);
                // TODO: fix this
                for (var i = 0; i < m_Parent.m_TextInputListeners.length; ++i)
                {
                    m_Parent.m_TextInputListeners[i](text[0]);
                }
            }
        }

        private AndroidJavaObject m_KeyboardObject;

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            m_KeyboardObject = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard",
                new ScreenKeyboardCallbacks(this),
                (int)showParams.type,
                showParams.initialText,
                showParams.placeholderText,
                showParams.autocorrection,
                showParams.multiline,
                showParams.secure,
                showParams.alert);
            m_KeyboardObject.Call("show");
        }

        public override void Hide()
        {
            if (m_KeyboardObject != null)
            {
                m_KeyboardObject.Call("dismiss");
                m_KeyboardObject = null;
            }
        }
    }
}
