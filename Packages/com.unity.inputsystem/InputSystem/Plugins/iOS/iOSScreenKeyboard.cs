#if (UNITY_EDITOR || UNITY_IOS || UNITY_TVOS) && !DISABLE_SCREEN_KEYBOARD
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.iOS
{
    internal class iOSScreenKeyboard : ScreenKeyboard
    {
        private static iOSScreenKeyboard s_Instance;
        private static iOSScreenKeyboard instance
        {
            get
            {
                return s_Instance;
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

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardSetLogging(int enabled);

        [DllImport("__Internal")]
        private static extern int _iOSScreenKeyboardGetLogging();

        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardCleanup();

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

        internal iOSScreenKeyboard()
        {
            if (s_Instance != null)
                throw new Exception("Creating more than on iOSScreenKeyboard");
            s_Instance = this;
        }

        public override void Dispose()
        {
            if (s_Instance == null)
                throw new Exception("iOSScreenKeyboard was already disposed?");
            _iOSScreenKeyboardCleanup();
            s_Instance = null;
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

        public override bool logging
        {
            get
            {
                return _iOSScreenKeyboardGetLogging() != 0;
            }

            set
            {
                _iOSScreenKeyboardSetLogging(value ? 1 : 0);
            }
        }
    }
}
#endif
