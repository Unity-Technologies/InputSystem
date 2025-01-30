# Xbox gamepads

The Input System implements Xbox gamepads using the [XInputController](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.XInputController.html) class, which derives from [Gamepad](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html). On Windows and UWP, Unity uses the XInput API to connect to any type of supported XInput controller, including all Xbox One or Xbox 360-compatible controllers. These controllers are represented as an [XInputController](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.XInputController.html) instance. 

You can query the [XInputController.subType](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.XInputController.html#UnityEngine_InputSystem_XInput_XInputController_subType) property to get information about the type of controller (for example, a wheel or a gamepad).

On other platforms, Unity uses derived classes to represent Xbox controllers:

* [XboxGamepadMacOS](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.XboxGamepadMacOS.html): Any Xbox or compatible gamepad connected to macOS via USB using the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller).  
* [XboxOneGampadMacOSWireless](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless.html): An Xbox One controller connected to macOS via Bluetooth. Only the latest generation of Xbox One controllers supports Bluetooth. These controllers don't require any additional drivers in this scenario.  
* [XboxOneGampadiOS](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.iOS.XboxOneGampadiOS.html): An Xbox One controller connected to an iOS device via Bluetooth. Requires iOS 13 or higher.

XInput controllers on macOS require the installation of the [Xbox Controller Driver for macOS](https://github.com/360Controller/360Controller). This driver only supports USB connections, and doesn't support wireless dongles. However, the latest generation of Xbox One controllers natively support Bluetooth. macOS natively supports these controllers as HIDs without any additional drivers when connected via Bluetooth.

Unity supports Xbox controllers on WebGL in some browser and OS configurations, but treats them as basic [Gamepad](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Gamepad.html) or [Joystick](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Joystick.html) devices, and doesn't support rumble or any other Xbox-specific functionality.

For more information on platform support for Xbox gamepads, refer to \[Platform support\].