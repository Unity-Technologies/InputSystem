# Synthetic Controls

A synthetic control is a control that doesn't correspond to an actual physical control on a device (for example the `left`, `right`, `up`, and `down` child Controls on a [`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)). These Controls synthesize input from other, actual physical Controls and present it in a different way (in this example, they allow you to treat the individual directions of a stick as buttons).

Whether a given Control is synthetic is indicated by its [`InputControl.synthetic`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_synthetic) property.

The system considers synthetic controls for [interactive rebinding](ActionBindings.md#interactive-rebinding) but always favors non-synthetic controls. If both a synthetic and a non-synthetic control that are a potential match exist, the non-synthetic control wins by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but also makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.