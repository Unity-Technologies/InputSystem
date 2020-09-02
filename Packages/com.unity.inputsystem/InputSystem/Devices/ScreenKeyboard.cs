using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Enumeration of the different types of supported screen keyboards.
    /// </summary>
    public enum ScreenKeyboardType
    {
        /// <summary>
        /// The default keyboard layout of the target platform.
        /// </summary>
        Default = 0,

        /// <summary>
        /// 
        /// </summary>
        ASCIICapable = 1,

        /// <summary>
        /// Keyboard with numbers and punctuation mark keys.
        /// </summary>
        NumbersAndPunctuation = 2,

        /// <summary>
        /// Keyboard with keys for URL entry.
        /// </summary>
        URL = 3,

        /// <summary>
        /// Keyboard with standard numeric keys.
        /// </summary>
        NumberPad = 4,

        /// <summary>
        /// Keyboard with a layout suitable for typing telephone numbers.
        /// </summary>
        PhonePad = 5,

        /// <summary>
        /// Keyboard with alphanumeric keys.
        /// </summary>
        NamePhonePad = 6,

        /// <summary>
        /// Keyboard with additional keys suitable for typing email addresses.
        /// </summary>
        EmailAddress = 7,

        /// <summary>
        /// Keyboard with symbol keys often used on social media, such as Twitter.
        /// </summary>
        Social = 8,

        /// <summary>
        /// Keyboard with the "." key beside the space key, suitable for typing search terms.
        /// </summary>
        Search = 9
    }

    /// <summary>
    /// Screen keyboard state.
    /// </summary>
    public enum ScreenKeyboardState : uint
    {
        /// <summary>
        /// Screen keyboard is closed.
        /// </summary>
        Done,

        /// <summary>
        /// Screen keyboard is visible.
        /// </summary>
        Visible,

        /// <summary>
        /// Screen keyboard is closed due cancellation event, for ex., Cancel button was clicked.
        /// </summary>
        Canceled,

        /// <summary>
        /// Screen keyboard is closed due lost focus event.
        /// </summary>
        LostFocus
    }

    // Note: This struct is marshalled to iOS native code, don't change the layout
    //       Also don't use auto properties, since it messes up the layout
    /// <summary>
    /// Describes the appearance of screen keyboard.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenKeyboardShowParams
    {
        private ScreenKeyboardType m_Type;
        private string m_InitialText;
        private string m_PlaceholderText;
        private bool m_Autocorrection;
        private bool m_Multiline;
        private bool m_Secure;
        private bool m_Alert;
        private bool m_InputFieldHidden;

        /// <summary>
        /// The type of the keyboard.
        /// </summary>
        public ScreenKeyboardType type { get => m_Type; set => m_Type = value; }

        /// <summary>
        /// The default input field text which is present when keyboard is shown.
        /// </summary>
        public string initialText { get => m_InitialText; set => m_InitialText = value; }

        /// <summary>
        /// Placeholder text when input field text is empty.
        /// </summary>
        public string placeholderText { get => m_PlaceholderText; set => m_PlaceholderText = value; }

        /// <summary>
        /// Is auto correction applied?
        /// </summary>
        public bool autocorrection { get => m_Autocorrection; set => m_Autocorrection = value; }

        /// <summary>
        /// Can more than one line of text be entered in the input field ?
        /// </summary>
        public bool multiline { get => m_Multiline; set => m_Multiline = value; }

        /// <summary>
        /// Is the text masked (for passwords, etc)?
        /// </summary>
        public bool secure { get => m_Secure; set => m_Secure = value; }

        /// <summary>
        /// Is the keyboard opened in alert mode?
        /// Note: Only available on iOS
        /// </summary>
        public bool alert { get => m_Alert; set => m_Alert = value; }

        /// <summary>
        /// Is input field hidden?
        /// </summary>
        public bool inputFieldHidden { get => m_InputFieldHidden; set => m_InputFieldHidden = value; }
    }

    /// <summary>
    /// ScreenKeyboard base class for platform specific implementation.
    ///
    /// Known issues:
    /// * There's a screen keyboard behavior differences in regards to input and focus loss:
    ///   - [Android] When screen keyboard is shown, the main Unity window stops receiving input events, when clicking on Unity window, screen keyboard looses focus and closes itself
    ///     (While it's possible to keep screen keyboard opened on focus loss, currently there's no way to make Unity window keep receiving input events like touch while screen keyboard is shown,
    ///      this matches the behavior in old TouchScreenKeyboard implementation)
    ///   - [iOS] When screen keyboard is shown, the main Unity window will continue receiving input events, when clicking on Unity window, screen keyboard will continue to be shown
    ///   - [WSA] When screen keyboard is shown, the main Unity window will continue receiving input events, when clicking on Unity window, screen keyboard will continue to be shown
    /// </summary>
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
        /// If you specify a selection out of bounds of text length, the selection will not be set.
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

        /// <summary>
        /// Used for testing purposes, enable platform specific logging.
        /// </summary>
        internal virtual bool logging { get; set; }
    }
}
