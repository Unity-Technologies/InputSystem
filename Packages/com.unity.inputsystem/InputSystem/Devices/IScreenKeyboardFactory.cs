using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    public interface IScreenKeyboardFactory
    {
        ScreenKeyboard Create();
    }
}