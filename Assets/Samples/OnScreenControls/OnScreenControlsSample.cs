// This example showcases how to use on-screen controls and showcases how to use
// OnScreenStick and OnScreenButton which are two types of on-screen controls included with the Input System.
// In this example, multiple OnScreenButton component instances and OnScreenStick component instances are
// attached to a Canvas to build an on-screen gamepad.
//
// The OnScreenControl game objects LeftStick, RightStick, A, B, X, Y have all been configured to associated
// with a gamepad layout. This implies that they generate input similar to a real hardware gamepad.
// Due to this, InputActions bound to gamepad controls such as the controls mentioned above will hence be
// triggered when the user interacts with the on-screen controls.
// This example uses actions defined in OnScreenControlsSampleActions.inputactions which are preconfigured to
// bind to gamepad controls. Hence this example will work the same way when using either the on-screen controls
// or a physical gamepad.
//
// Note that on-screen controls 1 and 2 on the other hand are bound to a keyboard control layout and therefore
// generates input events similar to a physical hardware keyboard.
//
// When actions defined for this sample are triggered, a log message is generated to show when the actions
// are performed.
//
// Note that actions for this example have been setup to allow either on-screen controls or physical gamepad/keyboard
// controls to be used interchangeably.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

public sealed class OnScreenControlsSample : MonoBehaviour
{
    public InputActionReference mode1, mode2, leftStick, rightStick, x, y, a, b;
    public OnScreenControlUpdateMode updateMode;

    void UpdateCurrentMode(OnScreenControlUpdateMode mode)
    {
        Debug.Log($"Switched to OnScreenControl Update Mode: {mode.ToString()}");
        foreach (var onScreenControl in GetComponentsInChildren<OnScreenControl>())
            onScreenControl.updateMode = mode;
    }

    void EnableAndLogPerformed(InputActionReference reference)
    {
        reference.action.Enable();
        reference.action.performed += Performed;
    }

    void Start()
    {
        UpdateCurrentMode(updateMode);
        EnableAndLogPerformed(mode1);
        mode1.action.performed += (_) => UpdateCurrentMode(OnScreenControlUpdateMode.QueueEvents);
        EnableAndLogPerformed(mode2);
        mode2.action.performed += (_) => UpdateCurrentMode(OnScreenControlUpdateMode.ChangeState);

        EnableAndLogPerformed(x);
        EnableAndLogPerformed(y);
        EnableAndLogPerformed(a);
        EnableAndLogPerformed(b);

        EnableAndLogPerformed(leftStick);
        EnableAndLogPerformed(rightStick);
    }

    void Performed(InputAction.CallbackContext context)
    {
        Debug.Log($"Performed action={context.action.name}, Time.frameCount={Time.frameCount}, context.time={context.time}, Time.time={Time.time}");
    }
}
