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

    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenKeyboardState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('S', 'K', 'S', 'T');
        public FourCC format => Format;

        [InputControl(name = "status", displayName = "Status", layout = "ScreenKeyboardStatus")]
        public int status;
    }

    [InputControlLayout(stateType = typeof(ScreenKeyboardState))]
    public class ScreenKeyboard : InputDevice
    {
        private const long kCommandReturnSuccess = 1;
        private const long kCommandReturnFailure = 0;

        public ScreenKeyboardStatusControl status { get; set; }

        private InlinedArray<Action<ScreenKeyboardStatus>> m_StatusChangedListeners;
        private InlinedArray<Action<string>> m_InputFieldTextListeners;

        protected ScreenKeyboard()
        {

        }

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

        public void Show()
        {
            Show(new ScreenKeyboardShowParams());
        }

        public virtual void Show(ScreenKeyboardShowParams showParams)
        {
        }

        public virtual void Hide()
        {
        }

        public override unsafe long ExecuteCommand<TCommand>(ref TCommand command)
        {
            if (command.typeStatic == EnableDeviceCommand.Type)
            {
                Show();
                return kCommandReturnSuccess;
            }

            if (command.typeStatic == DisableDeviceCommand.Type)
            {
                Hide();
                return kCommandReturnSuccess;
            }

            if (command.typeStatic == QueryEnabledStateCommand.Type)
            {
                var cmd = (QueryEnabledStateCommand*) UnsafeUtility.AddressOf(ref command);
                cmd->isEnabled = status.ReadValue() == ScreenKeyboardStatus.Visible;

                return kCommandReturnSuccess;
            }

            return kCommandReturnFailure;
        }

        protected void OnChangeInputField(string text)
        {
            foreach (var listener in m_InputFieldTextListeners)
                listener(text);
        }

        protected void OnStatusChanged(ScreenKeyboardStatus keyboardStatus)
        {
            var stateChanged = keyboardStatus != status.ReadValue();

            if (stateChanged)
            {
                foreach (var listener in m_StatusChangedListeners)
                    listener(keyboardStatus);
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
        public virtual Rect occludingArea => Rect.zero;
        
        protected override void FinishSetup()
        {
            status = GetChildControl<ScreenKeyboardStatusControl>("status");
            base.FinishSetup();
        }
    }
}
