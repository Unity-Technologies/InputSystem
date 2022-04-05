using System;
using UnityEngine.InputSystem;

namespace UnityEngine
{
    public static partial class Input
    {
        public static bool mousePresent => Mouse.current != null;
        public static Vector2 mousePosition => Pointer.current?.position.ReadValue() ?? default;

        public static bool GetMouseButton(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.IsPressed();
        }

        public static bool GetMouseButtonUp(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.WasReleasedThisFrame();
        }

        public static bool GetMouseButtonDown(int button)
        {
            var action = GetActionForMouseButton(button);
            if (action == null)
                return false;

            return action.WasPressedThisFrame();
        }

        private static InputAction GetActionForMouseButton(int button)
        {
            // Legacy input throws ArgumentException if button >= 7.
            if (button < 0 || button >= kMouseButtonCount)
                throw new ArgumentException("Invalid mouse button index.", nameof(button));

            // Only support 3 buttons for now.
            switch (button)
            {
                case 0:
                    return s_LeftMouseButtonAction;
                case 1:
                    return s_RightMouseButtonAction;
                case 2:
                    return s_MiddleMouseButtonAction;
                ////TODO: Old input Win32 backend maps 3=back(XBUTTON1) and 4=forward(XBUTTON2); find out what other platforms do
                case 3:
                    return s_BackMouseButtonAction;
                case 4:
                    return s_ForwardMouseButtonAction;
            }

            return null;
        }

        private static InputAction s_LeftMouseButtonAction;
        private static InputAction s_RightMouseButtonAction;
        private static InputAction s_MiddleMouseButtonAction;
        private static InputAction s_ForwardMouseButtonAction;
        private static InputAction s_BackMouseButtonAction;
    }
}
