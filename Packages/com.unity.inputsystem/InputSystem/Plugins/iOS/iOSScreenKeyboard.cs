#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.iOS
{
    internal class iOSScreenKeyboard : ScreenKeyboard
    {
        private static iOSScreenKeyboard ms_Instance;

        public static iOSScreenKeyboard instance
        {
            get
            {
                if (ms_Instance == null)
                    ms_Instance = new iOSScreenKeyboard();
                return ms_Instance;
            }
        }

        internal delegate void OnTextChangedDelegate(string text);

        internal delegate void OnStateChangedDelegate(ScreenKeyboardState state);

        internal delegate void OnSelectionChangedDelegate(int start, int length);

        [StructLayout(LayoutKind.Sequential)]
        private struct iOSScreenKeyboardCallbacks
        {
            internal OnTextChangedDelegate onTextChanged;
            internal OnStateChangedDelegate onStateChanged;
            internal OnSelectionChangedDelegate onSelectionChanaged;
        }

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardShow(ref ScreenKeyboardShowParams showParams, int sizeOfShowParams, ref iOSScreenKeyboardCallbacks callbacks, int sizeOfCallbacks);

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardHide();

        [DllImport("__Internal")]
        private static extern Rect _iOSScreenKeyboardOccludingArea();

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardSetInputFieldText(string text);

        [DllImport("__Internal")]
        private static extern string _iOSScreenKeyboardGetInputFieldText();

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardSetSelection(int start, int length);

        [DllImport("__Internal")]
        private static extern long _iOSScreenKeyboardGetSelection();

        [MonoPInvokeCallback(typeof(OnTextChangedDelegate))]
        private static void OnTextChangedCallback(string text)
        {
            instance.ReportInputFieldChange(text);
        }

        [MonoPInvokeCallback(typeof(OnStateChangedDelegate))]
        private static void OnStateChangedCallback(ScreenKeyboardState state)
        {
            instance.ReportStateChange(state);
        }

        [MonoPInvokeCallback(typeof(OnSelectionChangedDelegate))]
        private static void OnSelectionChangedCallback(int start, int length)
        {
            instance.ReportSelectionChange(start, length);
        }

        protected override void InternalShow()
        {
            var callbacks = new iOSScreenKeyboardCallbacks()
            {
                onTextChanged = OnTextChangedCallback,
                onStateChanged = OnStateChangedCallback,
                onSelectionChanaged = OnSelectionChangedCallback
            };
            _iOSScreenKeyboardShow(ref m_ShowParams, Marshal.SizeOf(m_ShowParams), ref callbacks, Marshal.SizeOf(callbacks));
        }

        protected override void InternalHide()
        {
            _iOSScreenKeyboardHide();
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

        public override RangeInt selection
        {
            get
            {
                var combined = _iOSScreenKeyboardGetSelection();
                unchecked
                {
                    return new RangeInt((int)(0xFFFFFFFF & combined), (int)(combined >> 32));
                }
            }
            set
            {
                _iOSScreenKeyboardSetSelection(value.start, value.length);
            }
        }

        public override Rect occludingArea => _iOSScreenKeyboardOccludingArea();
    }
}
#endif
