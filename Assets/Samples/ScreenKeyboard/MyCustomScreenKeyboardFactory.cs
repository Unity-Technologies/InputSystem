using System;
using System.Threading;
using UnityEngine.Scripting;


namespace UnityEngine.InputSystem
{
    [Preserve]
    public class MyCustomScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
#if UNITY_EDITOR
            return new EmulatedScreenKeyboard();
#elif UNITY_ANDROID
            return new Android.AndroidScreenKeyboard();
#elif UNITY_IOS
            return new iOS.iOSScreenKeyboard();
#endif
        }
    }
}
