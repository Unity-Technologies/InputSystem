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
                for (var i = 0; i < m_Parent.m_TextInputListeners.length; ++i)
                    m_Parent.m_TextInputListeners[i](text[0]);
            }
        }

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            var obj = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard", new ScreenKeyboardCallbacks(this));
            obj.Call("Show");
        }

        public override void Hide()
        {
            //  throw new NotImplementedException();
        }
    }
}
