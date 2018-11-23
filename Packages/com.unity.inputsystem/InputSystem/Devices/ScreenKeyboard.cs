using System;

namespace UnityEngine.Experimental.Input
{
    ////TODO: probably need a better name, so not to collide with com.unity.inputsystem\InputSystem\Plugins\OnScreen\OnScreenKeyboard.cs
    public abstract class ScreenKeyboard : Keyboard
    {
        public abstract void Show();

        public abstract void Hide();
    }
}
