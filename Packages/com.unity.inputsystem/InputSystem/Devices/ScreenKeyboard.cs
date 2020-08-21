using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
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

    public enum ScreenKeyboardStatus : uint
    {
        Done,
        Visible,
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

    // Input device requires us specifying none zero size state, otherwise you get
    // InvalidOperationException: Control '/AndroidScreenKeyboard' with layout 'AndroidScreenKeyboard' has no size set and has no children to compute size from
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenKeyboardState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('S', 'K', 'S', 'T');
        public FourCC format => Format;

        [InputControl(name = "dummy", layout = "Button")]
        public uint dummy;
    }

    // TODO: in case ScreenKeyboard doesn't have input field, it basically behaves the same as normal Keyboard
    //       the only difference only onTextInput is working, you won't get response from key control
    //       Nevertheless maybe it makes sense to derive from Keyboard here. Rene?
    public abstract class ScreenKeyboard : InputDevice
    {
        private const long kCommandReturnSuccess = 1;
        private const long kCommandReturnFailure = 0;

        // Note: Status cannot be a part of ScreenKeyboardState, since it defines if device is enabled or disabled
        //       If device is disabled, it cannot receive any events
        protected ScreenKeyboardStatus m_KeyboardStatus;
        protected ScreenKeyboardShowParams m_ShowParams;

        private InlinedArray<Action<ScreenKeyboardStatus>> m_StatusChangedListeners;
        private InlinedArray<Action<string>> m_InputFieldTextListeners;
        private InlinedArray<Action<RangeInt>> m_SelectionChangedListeners;

        public event Action<ScreenKeyboardStatus> stateChanged
        {
            add { m_StatusChangedListeners.Append(value); }
            remove { m_StatusChangedListeners.Remove(value); }
        }

        public event Action<string> inputFieldTextChanged
        {
            add { m_InputFieldTextListeners.Append(value); }
            remove { m_InputFieldTextListeners.Remove(value); }
        }

        public event Action<RangeInt> selectionChanged
        {
            add { m_SelectionChangedListeners.Append(value); }
            remove { m_SelectionChangedListeners.Remove(value); }
        }

        public ScreenKeyboardStatus status
        {
            get => m_KeyboardStatus;
        }

        public void Show()
        {
            Show(new ScreenKeyboardShowParams());
        }

        public void Show(ScreenKeyboardShowParams showParams)
        {
            m_ShowParams = showParams;
            InputSystem.EnableDevice(this);
        }

        public void Hide()
        {
            InputSystem.DisableDevice(this);
        }

        protected abstract void InternalShow();

        protected abstract void InternalHide();

        public override unsafe long ExecuteCommand<TCommand>(ref TCommand command)
        {
            if (command.typeStatic == EnableDeviceCommand.Type)
            {
                InternalShow();
                return kCommandReturnSuccess;
            }

            if (command.typeStatic == DisableDeviceCommand.Type)
            {
                InternalHide();
                return kCommandReturnSuccess;
            }

            if (command.typeStatic == QueryEnabledStateCommand.Type)
            {
                var cmd = (QueryEnabledStateCommand*)UnsafeUtility.AddressOf(ref command);
                cmd->isEnabled = m_KeyboardStatus == ScreenKeyboardStatus.Visible;

                return kCommandReturnSuccess;
            }

            return kCommandReturnFailure;
        }

        internal /*protected*/ void ReportInputFieldChange(string text)
        {
            foreach (var listener in m_InputFieldTextListeners)
                listener(text);
        }

        protected void ReportStatusChange(ScreenKeyboardStatus keyboardStatus)
        {
            if (keyboardStatus != m_KeyboardStatus)
            {
                m_KeyboardStatus = keyboardStatus;
                foreach (var listener in m_StatusChangedListeners)
                    listener(keyboardStatus);
            }

            // OnConfigurationChanged is required for properties like enabled, since they need requery enabled value
            OnConfigurationChanged();
        }

        protected void ReportSelectionChange(int start, int length)
        {
            var selection = new RangeInt(start, length);
            foreach (var listener in m_SelectionChangedListeners)
                listener(selection);
        }

        /// <summary>
        /// Modifies text in screen keyboard's input field.
        /// If screen keyboard doesn't have an input field, this property does nothing.
        /// </summary>
        public virtual string inputFieldText { set; get; }

        /// <summary>
        /// Returns portion of the screen which is covered by the keyboard.
        /// </summary>
        public virtual Rect occludingArea => Rect.zero;

        public virtual RangeInt selection { set; get; }
    }
}
