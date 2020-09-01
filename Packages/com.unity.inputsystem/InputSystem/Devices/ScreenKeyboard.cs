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

    public enum ScreenKeyboardState : uint
    {
        Done,
        Visible,
        Canceled,
        LostFocus
    }

    // Note: ScreenKeyboardShowParams has to be a struct otherwise when passing it to objective C on iOS the data is all wrong, interestingly
    //       the size is correct
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
        /// </summary>
        public bool inputFieldHidden;
    }

    public abstract class ScreenKeyboard
    {
        protected ScreenKeyboardState m_KeyboardState;
        protected ScreenKeyboardShowParams m_ShowParams;

        private InlinedArray<Action<ScreenKeyboardState>> m_StatusChangedListeners;
        private InlinedArray<Action<string>> m_InputFieldTextListeners;
        private InlinedArray<Action<RangeInt>> m_SelectionChangedListeners;

        /// <summary>
        /// Subscribe to an event which is fired whenever screen keyboard state changes
        /// </summary>
        public event Action<ScreenKeyboardState> stateChanged
        {
            add { m_StatusChangedListeners.Append(value); }
            remove { m_StatusChangedListeners.Remove(value); }
        }

        /// <summary>
        /// Subscribe to an event which is fired whenever input field text changes
        /// This event is also fired when input field is hidden
        /// </summary>
        public event Action<string> inputFieldTextChanged
        {
            add { m_InputFieldTextListeners.Append(value); }
            remove { m_InputFieldTextListeners.Remove(value); }
        }

        /// <summary>
        /// Subscribe to an event which is fired whenever input field text selection changes.
        /// </summary>
        public event Action<RangeInt> selectionChanged
        {
            add { m_SelectionChangedListeners.Append(value); }
            remove { m_SelectionChangedListeners.Remove(value); }
        }

        /// <summary>
        /// Returns the state of the keyboard
        /// </summary>
        public ScreenKeyboardState state
        {
            get => m_KeyboardState;
        }

        /// <summary>
        /// Shows the screen keyboard with default options.
        /// </summary>
        public void Show()
        {
            Show(new ScreenKeyboardShowParams());
        }

        /// <summary>
        /// Show the screen keyboard with customized options.
        /// </summary>
        /// <param name="showParams"></param>
        public void Show(ScreenKeyboardShowParams showParams)
        {
            m_ShowParams = showParams;
            if (m_ShowParams.initialText == null)
                m_ShowParams.initialText = String.Empty;
            if (m_ShowParams.placeholderText == null)
                m_ShowParams.placeholderText = String.Empty;
            InternalShow();
        }

        /// <summary>
        /// Hide screen keyboard
        /// </summary>
        public void Hide()
        {
            InternalHide();
        }

        protected abstract void InternalShow();

        protected abstract void InternalHide();

        protected void ReportInputFieldChange(string text)
        {
            foreach (var listener in m_InputFieldTextListeners)
                listener(text);
        }

        protected void ReportStateChange(ScreenKeyboardState state)
        {
            if (state != m_KeyboardState)
            {
                m_KeyboardState = state;
                foreach (var listener in m_StatusChangedListeners)
                    listener(state);
            }
        }

        protected void ReportSelectionChange(int start, int length)
        {
            if (m_KeyboardState != ScreenKeyboardState.Visible)
                return;

            var selection = new RangeInt(start, length);
            foreach (var listener in m_SelectionChangedListeners)
                listener(selection);
        }

        /// <summary>
        /// Modifies text in screen keyboard's input field.
        /// You can use this propery even if input field is hidden
        /// </summary>
        public abstract string inputFieldText { set; get; }

        /// <summary>
        /// Returns portion of the screen which is covered by the keyboard.
        /// </summary>
        public virtual Rect occludingArea => Rect.zero;

        /// <summary>
        /// Modify selection of an input field text.
        /// When input field is hidden, this property does nothing and always returns (text.length, 0)
        /// </summary>
        public abstract RangeInt selection { set; get; }

        /// <summary>
        /// For testing purposes only.
        /// Simulate a key event
        /// </summary>
        /// <param name="keyCode">A platform specific key code.</param>
        internal virtual void SimulateKeyEvent(int keyCode)
        {
        }

        /// <summary>
        /// Used by internal testing only. From user perspective you should always clean up your listeners.
        /// </summary>
        internal void ClearListeners()
        {
            m_StatusChangedListeners.Clear();
            m_InputFieldTextListeners.Clear();
            m_SelectionChangedListeners.Clear();
        }
    }
}
