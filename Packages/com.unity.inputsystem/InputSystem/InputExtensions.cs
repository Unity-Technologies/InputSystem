////REVIEW: move everything from InputControlExtensions here?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Various useful extension methods.
    /// </summary>
    public static class InputExtensions
    {
        public static bool IsEndedOrCanceled(this TouchPhase phase)
        {
            return phase == TouchPhase.Canceled || phase == TouchPhase.Ended;
        }

        public static bool IsActive(this TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    return true;
            }
            return false;
        }

        public static bool IsModifierKey(this Key key)
        {
            switch (key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftMeta:
                case Key.RightMeta:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return true;
            }
            return false;
        }

        ////REVIEW: Is this a good idea? Ultimately it's up to any one keyboard layout to define this however it wants.
        public static bool IsTextInputKey(this Key key)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftMeta:
                case Key.RightMeta:
                case Key.ContextMenu:
                case Key.Escape:
                case Key.LeftArrow:
                case Key.RightArrow:
                case Key.UpArrow:
                case Key.DownArrow:
                case Key.Backspace:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                case Key.Insert:
                case Key.Delete:
                case Key.CapsLock:
                case Key.NumLock:
                case Key.PrintScreen:
                case Key.ScrollLock:
                case Key.Pause:
                case Key.None:
                case Key.Space:
                case Key.Enter:
                case Key.Tab:
                case Key.NumpadEnter:
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                case Key.F7:
                case Key.F8:
                case Key.F9:
                case Key.F10:
                case Key.F11:
                case Key.F12:
                case Key.OEM1:
                case Key.OEM2:
                case Key.OEM3:
                case Key.OEM4:
                case Key.OEM5:
                case Key.IMESelected:
                    return false;
            }
            return true;
        }
    }
}
