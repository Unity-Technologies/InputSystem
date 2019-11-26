using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;

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

    public enum ScreenKeyboardStatus : byte
    {
        Visible,
        Done,
        Canceled,
        LostFocus
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ScreenKeyboardState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('S', 'K', 'S', ' ');

        [InputControl(name = "status", displayName = "Screen Keyboard Status", layout = "ScreenKeyboardStatus")]
        public ScreenKeyboardStatus status;

        public Rect occludingArea;

        public FourCC format
        {
            get { return kFormat; }
        }
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

    ////TODO: probably need a better name, so not to collide with com.unity.inputsystem\InputSystem\Plugins\OnScreen\OnScreenKeyboard.cs
    public class ScreenKeyboard : Keyboard
    {
        private static ScreenKeyboard m_ScreenKeyboard;
        internal InlinedArray<Action<ScreenKeyboardStatus>> m_StatusChangedListeners;

        public ScreenKeyboardStatusControl status { get; private set; }

        public static ScreenKeyboard GetInstance()
        {
            if (m_ScreenKeyboard != null)
                return m_ScreenKeyboard;
#if UNITY_ANDROID
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.Android.AndroidScreenKeyboard>();
#elif UNITY_WSA
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.WSA.WSAScreenKeyboard>();
#elif UNITY_IOS || UNITY_TVOS
            m_ScreenKeyboard = InputSystem.AddDevice<UnityEngine.InputSystem.iOS.iOSScreenKeyboard>();
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
            // TODO: initialize status done
        }

        protected override void FinishSetup()
        {
            status = GetChildControl<ScreenKeyboardStatusControl>("press");

            base.FinishSetup();
        }


        protected void OnChangeStatus()
        {
            var temp = status.ReadValue();
            foreach (var statusListener in m_StatusChangedListeners)
                statusListener(temp);
        }

        public event Action<ScreenKeyboardStatus> statusChanged
        {
            add { m_StatusChangedListeners.Append(value); }
            remove { m_StatusChangedListeners.Remove(value); }
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
