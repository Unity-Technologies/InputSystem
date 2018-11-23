using System;

namespace UnityEngine.Experimental.Input
{
    public class AndroidScreenKeyboard : ScreenKeyboard
    {
        public override void Show()
        {
            var obj = new AndroidJavaObject("com.unity.inputsystem.AndroidScreenKeyboard");
            obj.Call("Show");
        }

        public override void Hide()
        {
            throw new NotImplementedException();
        }
    }
}
