using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem;

namespace UnityEngine
{
    public static partial class Input
    {
        public static InputActionAsset actions => InputSystem.InputSystem.settings.actions;

        public static float GetAxis(string axisName)
        {
            var action = axisName != null ? actions.FindAction(axisName) : null;
            if (action == null)
            {
                // We didn't find the action. See if the user is looking for "Vertical" and "Horizontal".
                // If so, we alternatively look for a "Move" actions and take its X or Y value if found.

                var isVertical = string.Equals(axisName, "Vertical", StringComparison.OrdinalIgnoreCase);
                var isHorizontal = !isVertical && string.Equals(axisName, "Horizontal", StringComparison.OrdinalIgnoreCase);

                if (isVertical || isHorizontal)
                {
                    var moveAction = actions.FindAction("Move");
                    if (moveAction != null)
                    {
                        if (isVertical)
                            return moveAction.ReadValue<Vector2>().y;
                        return moveAction.ReadValue<Vector2>().x;
                    }
                }
                else
                {
                    var isMouseX = string.Equals(axisName, "Mouse X", StringComparison.OrdinalIgnoreCase);
                    var isMouseY = !isMouseX && string.Equals(axisName, "Mouse Y", StringComparison.OrdinalIgnoreCase);

                    if (isMouseX || isMouseY)
                    {
                        var lookAction = actions.FindAction("Look");
                        if (lookAction != null)
                        {
                            if (isMouseY)
                                return lookAction.ReadValue<Vector2>().y;
                            return lookAction.ReadValue<Vector2>().x;
                        }
                    }
                }

                AxisNotFound(axisName);
            }

            return action.ReadValue<float>();
        }

        private static InputAction GetButtonAction(string buttonName)
        {
            var action = buttonName != null ? actions.FindAction(buttonName) : null;
            if (action == null)
            {
                if (buttonName == "Fire1")
                    action = actions.FindAction("Fire");

                if (action == null)
                    AxisNotFound(buttonName);
            }

            return action;
        }

        public static bool GetButton(string buttonName)
        {
            return GetButtonAction(buttonName).IsPressed();
        }

        public static bool GetButtonUp(string buttonName)
        {
            return GetButtonAction(buttonName).WasReleasedThisFrame();
        }

        public static bool GetButtonDown(string buttonName)
        {
            return GetButtonAction(buttonName).WasPressedThisFrame();
        }

        // We throw the same exceptions as the current native code.
        [DoesNotReturn]
        private static void AxisNotFound(string axisName)
        {
            throw new ArgumentException($"No input axis called {axisName} is defined; to set up input actions, go to: Edit -> Project Settings... -> Input", nameof(axisName));
        }
    }
}
