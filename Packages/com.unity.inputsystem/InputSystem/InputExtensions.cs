////REVIEW: move everything from InputControlExtensions here?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Various useful extension methods.
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Return true if the given phase is <see cref="InputActionPhase.Started"/> or <see cref="InputActionPhase.Performed"/>.
        /// </summary>
        /// <param name="phase">An action phase.</param>
        /// <returns>True if the phase is started or performed.</returns>
        /// <seealso cref="InputAction.phase"/>
        public static bool IsInProgress(this InputActionPhase phase)
        {
            return phase == InputActionPhase.Started || phase == InputActionPhase.Performed;
        }

        /// <summary>
        /// Return true if the given phase is <see cref="TouchPhase.Canceled"/> or <see cref="TouchPhase.Ended"/>, i.e.
        /// if a touch with that phase would no longer be ongoing.
        /// </summary>
        /// <param name="phase">A touch phase.</param>
        /// <returns>True if the phase indicates a touch that has ended.</returns>
        /// <seealso cref="Controls.TouchControl.phase"/>
        public static bool IsEndedOrCanceled(this TouchPhase phase)
        {
            return phase == TouchPhase.Canceled || phase == TouchPhase.Ended;
        }

        /// <summary>
        /// Return true if the given phase is <see cref="TouchPhase.Began"/>, <see cref="UnityEngine.TouchPhase.Moved"/>, or
        /// <see cref="TouchPhase.Stationary"/>, i.e. if a touch with that phase would indicate an ongoing touch.
        /// </summary>
        /// <param name="phase">A touch phase.</param>
        /// <returns>True if the phase indicates a touch that is ongoing.</returns>
        /// <seealso cref="Controls.TouchControl.phase"/>
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

        /// <summary>
        /// Check if a <see cref="Key"/> enum value represents a modifier key.
        /// </summary>
        /// <param name="key">The key enum value you want to check.</param>
        /// <returns><c>true</c> if <paramref name="key"/> represents a modifier key, else <c>false</c>.</returns>
        /// <remarks>
        /// Modifier keys are any keys you can hold down to modify the output of other keys pressed simultaneously,
        /// such as the "shift" or "control" keys.
        /// </remarks>
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
        /// <summary>
        /// Check if a <see cref="Key"/> enum value represents key generating text input.
        /// </summary>
        /// <param name="key">The key enum value you want to check.</param>
        /// <returns><c>true</c> if <paramref name="key"/> represents a key generating non-whitespace text input, else <c>false</c>.</returns>
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
