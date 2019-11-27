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

    public enum ScreenKeyboardStatus : uint
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

    ////TODO: probably need a better name, so not to collide with com.unity.inputsystem\InputSystem\Plugins\OnScreen\OnScreenKeyboard.cs
    public class ScreenKeyboard : Keyboard, IScreenKeyboardStateReceiver
    {
        protected ScreenKeyboardState m_State;
       // private static ScreenKeyboard m_ScreenKeyboard;
        private InlinedArray<Action<ScreenKeyboardStatus>> m_StatusChangedListeners;

        /*
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
        */

        protected ScreenKeyboard()
        {
            m_State = new ScreenKeyboardState()
            {
                Status = ScreenKeyboardStatus.Done,
                OccludingArea = Rect.zero
            };
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

        protected void OnChangeInputField(string text)
        {
            var e = IMECompositionEvent.Create(deviceId, text, -1);
            InputSystem.QueueEvent(ref e);
        }

        protected void OnChangeState(ScreenKeyboardState newState)
        {
            var e = ScreenKeyboardEvent.Create(deviceId, newState);
            InputSystem.QueueEvent(ref e);
        }

        public void OnScreenKeyboardStateChanged(ScreenKeyboardState state)
        {
            var statusChanged = state.Status != m_State.Status;
            m_State = state;
            foreach (var statusListener in m_StatusChangedListeners)
                statusListener(m_State.Status);
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
                return m_State.OccludingArea;
            }
        }

        /// <summary>
        /// Returns the state of the screen keyboard.
        /// </summary>
        public ScreenKeyboardStatus status
        {
            get
            {
                return m_State.Status;
            }
        }

    }
}
