using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;


namespace UnityEngine.InputSystem
{
    public enum ScreenKeyboardType
    {
        Default = 0,
        ASCIICapable = 1,
        NumbersAndPunctuation = 2,
        URL = 3,
        NumberPad = 4,
        PhonePad = 5,
        NamePhonePad = 6,
        EmailAddress = 7,
        Social = 8,
        Search = 9
    }

    public enum ScreenKeyboardStatus
    {
        Visible,
        Done,
        Canceled,
        LostFocus
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenKeyboardShowParams
    {
        public ScreenKeyboardType type;
        public string initialText;
        public string placeholderText;
        public bool autocorrection;
        public bool multiline;
        public bool secure;

        ////TODO: this one is iPhone specific?
        public bool alert;

        /// <summary>
        /// Show keyboard without input field?
        /// Only supported on iOS and Android.
        /// Note: TODO, review this If input field is hidden, you won't receive inputFieldTextChanged callback, instead you'll be receiving onTextInput
        /// </summary>
        public bool inputFieldHidden;
        ////TODO: no characterLimit here, because the logic for characterLimit is too complex when IME composition occurs, instead let user manage the text from OnTextChanged callbac
    }


    public class InputFieldEventArgs
    {
        public string text { set; get; }
    }
    ////TODO: probably need a better name, so not to collide with com.unity.inputsystem\InputSystem\Plugins\OnScreen\OnScreenKeyboard.cs
    public class ScreenKeyboard : Keyboard
    {
        private static ScreenKeyboard m_ScreenKeyboard;

        internal ScreenKeyboardStatus m_Status;
        internal InlinedArray<Action<ScreenKeyboardStatus>> m_StatusChangedListeners;
        internal InlinedArray<Action<InputFieldEventArgs>> m_InputFieldTextListeners;

        public static ScreenKeyboard GetInstance()
        {
            if (m_ScreenKeyboard != null)
                return m_ScreenKeyboard;
#if UNITY_ANDROID
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.Plugins.Android.AndroidScreenKeyboard>();
#elif UNITY_WSA
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.Plugins.WSA.WSAScreenKeyboard>();
#elif UNITY_IOS || UNITY_TVOS
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.Plugins.iOS.iOSScreenKeyboard>();
#elif UNITY_EDITOR
            // ToDo: Should we show something for Editor?
            m_ScreenKeyboard = new ScreenKeyboard();
#else
            throw new NotImplementedException("ScreenKeyboard is not implemented for this platform.");
#endif
            return m_ScreenKeyboard;
        }

        protected ScreenKeyboard()
        {
            m_Status = ScreenKeyboardStatus.Done;
        }

        protected void ChangeStatus(ScreenKeyboardStatus newStatus)
        {
            m_Status = newStatus;
            foreach (var statusListener in m_StatusChangedListeners)
                statusListener(newStatus);
        }

        public event Action<ScreenKeyboardStatus> statusChanged
        {
            add { m_StatusChangedListeners.Append(value); }
            remove { m_StatusChangedListeners.Remove(value); }
        }

        protected void ChangeInputField(InputFieldEventArgs text)
        {
            foreach (var inputFieldTextListener in m_InputFieldTextListeners)
                inputFieldTextListener(text);
        }

        public event Action<InputFieldEventArgs> inputFieldTextChanged
        {
            add { m_InputFieldTextListeners.Append(value); }
            remove { m_InputFieldTextListeners.Remove(value); }
        }


        public virtual void Show(ScreenKeyboardShowParams showParams)
        {
        }

        public void Show()
        {
            Show(new ScreenKeyboardShowParams());
        }

        public virtual void Hide()
        {
        }

        public ScreenKeyboardStatus status
        {
            get
            {
                return m_Status;
            }
        }

        /// <summary>
        /// Modifies text in screen keyboard's input field.
        /// If screen keyboard doesn't have an input field, this property does nothing.
        /// </summary>
        public virtual string inputFieldText { set; get; }

        /// <summary>
        /// Returns portion of the screen which is covered by the keyboard.
        /// </summary>
        public virtual Rect occludingArea
        {
            get
            {
                return Rect.zero;
            }
        }
    }
}
