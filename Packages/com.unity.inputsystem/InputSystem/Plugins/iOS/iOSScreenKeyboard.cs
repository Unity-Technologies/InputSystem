#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.iOS
{
    public class iOSScreenKeyboard : ScreenKeyboard
    {
        internal delegate void OnTextChangedDelegate(int deviceId, string text);

        internal delegate void OnStateChangedDelegate(int deviceId, ScreenKeyboardState status);

        [StructLayout(LayoutKind.Sequential)]
        private struct iOSScreenKeyboardCallbacks
        {
            internal int deviceId;
            internal OnTextChangedDelegate onTextChanged;
            internal OnStateChangedDelegate onStateChanged;
        }

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardShow(ref ScreenKeyboardShowParams showParams, int sizeOfShowParams, ref iOSScreenKeyboardCallbacks callbacks, int sizeOfCallbacks);

        [DllImport("__Internal")]
        private static extern Rect _iOSScreenKeyboardOccludingArea();

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardSetInputFieldText(string text);

        [DllImport("__Internal")]
        private static extern string _iOSScreenKeyboardGetInputFieldText();

        [MonoPInvokeCallback(typeof(OnTextChangedDelegate))]
        private static void OnTextChangedCallback(int deviceId, string text)
        {
            var screenKeyboard = (iOSScreenKeyboard)InputSystem.GetDeviceById(deviceId);
            if (screenKeyboard == null)
                throw new Exception("OnTextChangedCallback: Failed to get iOSScreenKeyboard instance");

            screenKeyboard.OnChangeInputField(text);
        }

        [MonoPInvokeCallback(typeof(OnStateChangedDelegate))]
        private static void OnStateChangedCallback(int deviceId, ScreenKeyboardState state)
        {
            var screenKeyboard = (iOSScreenKeyboard)InputSystem.GetDeviceById(deviceId);
            if (screenKeyboard == null)
                throw new Exception("OnStateChangedCallback: Failed to get iOSScreenKeyboard instance");

            screenKeyboard.OnStateChanged(state);
        }

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            var callbacks = new iOSScreenKeyboardCallbacks()
            {
                deviceId = deviceId,
                onTextChanged = OnTextChangedCallback,
                onStateChanged = OnStateChangedCallback
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
                return _iOSScreenKeyboardGetInputFieldText();
            }
            set
            {
                _iOSScreenKeyboardSetInputFieldText(value);
            }
        }

        public override Rect occludingArea => _iOSScreenKeyboardOccludingArea();
    }
}
#endif
