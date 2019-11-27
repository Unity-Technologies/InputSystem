#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.iOS
{
    public class iOSScreenKeyboard : ScreenKeyboard
    {
        internal delegate void OnTextChanged(int deviceId, string text);

        internal delegate void OnStatusChanged(int deviceId, ScreenKeyboardStatus status);

        [StructLayout(LayoutKind.Sequential)]
        private struct iOSScreenKeyboardCallbacks
        {
            internal int deviceId;
            internal OnTextChanged onTextChanged;
            internal OnStatusChanged onStatusChanged;
        }

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardShow(ref ScreenKeyboardShowParams showParams, int sizeOfShowParams, ref iOSScreenKeyboardCallbacks callbacks, int sizeOfCallbacks);

        [DllImport("__Internal")]
        private static extern Rect _iOSScreenKeyboardOccludingArea();

        [MonoPInvokeCallback(typeof(OnTextChanged))]
        private static void OnTextChangedCallback(int deviceId, string text)
        {
            var screenKeyboard = (iOSScreenKeyboard)InputSystem.GetDeviceById(deviceId);
            if (screenKeyboard == null)
                throw new Exception("OnTextChangedCallback: Failed to get iOSScreenKeyboard instance");

            screenKeyboard.OnChangeInputField(text);
        }

        [MonoPInvokeCallback(typeof(OnStatusChanged))]
        private static void OnStatusChangedCallback(int deviceId, ScreenKeyboardStatus status)
        {
            var screenKeyboard = (iOSScreenKeyboard)InputSystem.GetDeviceById(deviceId);
            if (screenKeyboard == null)
                throw new Exception("OnStatusChangedCallback: Failed to get iOSScreenKeyboard instance");

            var props = screenKeyboard.m_KeyboardProperties;
            props.Status = status;
            screenKeyboard.OnScreenKeyboardPropertiesChanged(props);
        }

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            var callbacks = new iOSScreenKeyboardCallbacks()
            {
                deviceId = deviceId,
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
                // TODO
                return string.Empty;
            }
            set
            {
            }
        }

        public override Rect occludingArea
        {
            get 
            {
                m_KeyboardProperties.OccludingArea = _iOSScreenKeyboardOccludingArea();
                return base.occludingArea;
            }
        }
    }
}
#endif
