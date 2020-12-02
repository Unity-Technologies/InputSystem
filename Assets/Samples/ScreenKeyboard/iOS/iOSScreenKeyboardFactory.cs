using System;
using System.Threading;
using UnityEngine.Scripting;

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR

namespace UnityEngine.InputSystem.iOS
{
    [Preserve]
    class iOSScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            return new iOSScreenKeyboard();
        }
    }
}

#endif