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

    public enum ScreenKeyboardState : uint
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
    public class ScreenKeyboard : Keyboard, IScreenKeyboardCallbackReceiver
    {
        protected ScreenKeyboardProperties m_KeyboardProperties;
        private InlinedArray<Action<ScreenKeyboardState>> m_StatusChangedListeners;

        protected ScreenKeyboard()
        {
            m_KeyboardProperties = new ScreenKeyboardProperties()
            {
                State = ScreenKeyboardState.Done,
                OccludingArea = Rect.zero
            };
        }

        public event Action<ScreenKeyboardState> statusChanged
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
            // TODO: Create our own event or reuse composition event
            var e = IMECompositionEvent.Create(deviceId, text, -1);
            InputSystem.QueueEvent(ref e);
        }

        protected void OnInformationChange(ScreenKeyboardProperties newState)
        {
            var e = ScreenKeyboardEvent.Create(deviceId, newState);
            InputSystem.QueueEvent(ref e);
        }

        public void OnScreenKeyboardPropertiesChanged(ScreenKeyboardProperties keyboardProperties)
        {
            var statusChanged = keyboardProperties.State != m_KeyboardProperties.State;
            m_KeyboardProperties = keyboardProperties;
            foreach (var statusListener in m_StatusChangedListeners)
                statusListener(m_KeyboardProperties.State);
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
                return m_KeyboardProperties.OccludingArea;
            }
        }

        /// <summary>
        /// Returns the state of the screen keyboard.
        /// </summary>
        public ScreenKeyboardState state
        {
            get
            {
                return m_KeyboardProperties.State;
            }
        }

    }
}
