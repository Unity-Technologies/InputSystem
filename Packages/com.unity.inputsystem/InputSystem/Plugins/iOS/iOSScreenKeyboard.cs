using System;
using System.Runtime.InteropServices;
using AOT;

namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    internal class iOSScreenKeyboard : ScreenKeyboard
    {
        internal delegate void OnTextChanged(string text, int selectionStart, int selectionLength);

        internal delegate void OnStatusChanged(ScreenKeyboardStatus status);
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct iOSScreenKeyboardCallbacks
        {
            internal OnTextChanged onTextChanged;
            internal OnStatusChanged onStatusChanged;
        }
        
        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardShow(ref ScreenKeyboardShowParams showParams, int sizeOfShowParams, ref iOSScreenKeyboardCallbacks callbacks, int sizeOfCallbacks);
        
        [DllImport("__Internal")]
        private static extern Rect _iOSScreenKeyboardOccludingArea();

        [MonoPInvokeCallback(typeof(OnTextChanged))]
        private static void OnTextChangedCallback(string text, int selectionStart, int selectionLength)
        {
            var screenKeyboard = (iOSScreenKeyboard) ScreenKeyboard.GetInstance();
            if (screenKeyboard == null)
                throw new Exception("OnTextChangedCallback: Failed to get iOSScreenKeyboard instance");
            screenKeyboard.ChangeInputFieldText(new InputFieldEventArgs() { text = text, selection = new RangeInt(selectionStart, selectionLength) });
        }

        [MonoPInvokeCallback(typeof(OnStatusChanged))]
        private static void OnStatusChangedCallback(ScreenKeyboardStatus status)
        {
            var screenKeyboard = (iOSScreenKeyboard) ScreenKeyboard.GetInstance();
            if (screenKeyboard == null)
                throw new Exception("OnStatusChangedCallback: Failed to get iOSScreenKeyboard instance");
            screenKeyboard.ChangeStatus(status);
        }

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            var callbacks = new iOSScreenKeyboardCallbacks()
            {
                onTextChanged = OnTextChangedCallback,
                onStatusChanged = OnStatusChangedCallback
            };
            _iOSScreenKeyboardShow(ref showParams, Marshal.SizeOf(showParams), ref callbacks, Marshal.SizeOf(callbacks));
        }

        public override void Hide()
        {
        }

        public override string inputFieldText
        {
            get
            {

                return string.Empty;
            }
            set
            {
     
            }
        }

        public override Rect occludingArea
        {
            get { return _iOSScreenKeyboardOccludingArea(); }
        }
    }

}