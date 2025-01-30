# Simulate touch input

You can simulate touch input on other kinds of [Pointer](http://localhost:57437/com.unity.inputsystem@1.12/manual/pointers.html) devices such as [Mouse](http://localhost:57437/com.unity.inputsystem@1.12/manual/Mouse.html) and [Pen](http://localhost:57437/com.unity.inputsystem@1.12/manual/Pen.html) devices. 

To enable simulating touch input, perform one of the following:

* In the Unity Editor:  
  1. Open the [Input Debugger](http://localhost:57437/com.unity.inputsystem@1.12/manual/Debugging.html).  
  2. In the Options dropdown, select **Simulate Touch Input From Mouse or Pen.**  
* Add the [TouchSimulation](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html) MonoBehaviour to a GameObject in your scene. [TouchSimulation](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html) adds a [Touchscreen](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Touchscreen.html) device and automatically mirrors input on any [Pointer](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pointer.html) device to the virtual touchscreen device.  
* Call [TouchSimulation.Enable](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.html#UnityEngine_InputSystem_EnhancedTouch_TouchSimulation_Enable) somewhere in your startup code:

```c

   void OnEnable()
    {
        TouchSimulation.Enable();
    }

```
