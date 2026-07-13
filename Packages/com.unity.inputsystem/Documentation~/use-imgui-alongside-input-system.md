---
uid: input-system-imgui-alongside-input
---

# Use IMGUI alongside the Input System package

The Input System package doesn't support [Immediate Mode GUI](https://docs.unity3d.com/Manual/GUIScriptingGuide.html) (IMGUI) methods at runtime. However, if you need to use IMGUI for your UI, you can use legacy Input Manager input for IMGUI, and the Input System package for your in-game input.


When the Unity Editor's [**Active Input Handling**](https://docs.unity3d.com/Manual/class-PlayerSettings.html) setting is set to **Input System Package** (which is the default, when using the Input System package), the `OnGUI` methods in your player code don't receive any input events.


To restore functionality to runtime `OnGUI` methods, you can change the **Active Input Handling** setting to **Both**. Doing this means that Unity processes the input twice, which might introduce a small performance impact.


This only affects runtime (Play mode) `OnGUI` methods. Editor GUI code is unaffected and continues to receive input events.
