using System;
using System.Threading;
using UnityEngine.Scripting;

#if UNITY_ANDROID && !UNITY_EDITOR

namespace UnityEngine.InputSystem.Android
{
    [Preserve]
    class AndroidScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            return new AndroidScreenKeyboard();
        }
    }
}

#endif
