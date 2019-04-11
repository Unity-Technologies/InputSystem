    ////WIP

# Architecture

The input system has a layered architecture. Principally, it divides into a low-level and a high-level layer.

# Low-Level

![Low-Level Architecture](Images/InputArchitectureLowLevel.png)

## Runtime

The `IInputRuntime` interface represents the "input backend" that generates input for the current platform. By default, the runtime is tied to `UnityEngineInternal.Input.NativeInputSystem` and uses per-platform backends implemented natively in the Unity engine runtime.

# High-Level

# Plugins

The input system comes with a range of features that are implemented on top of the core input system features. All of these features are optional and are implemented as self-contained units considered to be "plugins". The public APIs of these modules are all under the `UnityEngine.Experimental.Input.Plugins` namespace.

|Plugin|Description|
|------|-----------|
|Android||
|DualShock||
|HID|Adds support for generic HID devices. Note that even without this plugin, HID devices can be used. They will, however, require dedicated device layouts in order to be recognized. The HID plugin adds the ability to generate device layouts on the fly from the information found in HID descriptors. The most important contribution is generic support for joysticks.<br><br>This plugin is only supported on platforms that support HID. See [HID-specific documentation](HID.md) for details.|
|iOS||
|OnScreen||
|PlayerInput||
|PS4||
|UI||
|Steam||
|Switch||
|Users||
|XInput||
|XR||

## Suppressing Default Plugin Registration

By default, the input system will register all of the built-in plugins that are applicable to the current platform. This behavior can be suppressed by defining the `UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION` scripting define for your project.
