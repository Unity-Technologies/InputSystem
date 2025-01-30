# Switch gamepads

The Input System supports Switch Pro controllers on desktop computers via the [SwitchProControllerHID](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Switch.SwitchProControllerHID.html) class, which implements basic gamepad functionality.

This support doesn't currently work for Switch Pro controllers connected via wired USB. Instead, the Switch Pro controller must be connected via Bluetooth. This is due to the controller using a proprietary communication protocol on top of HID which does not allow treating the controller like any other HID.

For more information on platform support for Switch gamepads, refer to \[Platform support\].