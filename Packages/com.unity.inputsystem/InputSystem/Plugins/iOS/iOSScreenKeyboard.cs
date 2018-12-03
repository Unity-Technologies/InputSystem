using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Input.Plugins.iOS
{
    public class iOSScreenKeyboard : ScreenKeyboard
    {
        [DllImport("__Internal")]
        private static extern void _iOSScreenKeyboardShow(ScreenKeyboardShowParams showParams, int sizeOfShowParams);

        public override void Show(ScreenKeyboardShowParams showParams)
        {
            showParams.placeholderText = "sdsdsd";
            _iOSScreenKeyboardShow(showParams, Marshal.SizeOf(showParams));
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
            get
            {
                //if (m_KeyboardObject != null)
                //    return m_KeyboardObject.Call<string>("getText");
                return Rect.zero;
            }
        }
    }

}