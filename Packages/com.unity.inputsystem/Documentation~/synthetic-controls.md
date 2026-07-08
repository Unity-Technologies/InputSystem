---
uid: input-system-synthetic-controls
---

# Synthetic controls

A synthetic control is an input control that doesn't correspond to an actual physical control on a device (for example the `left`, `right`, `up`, and `down` child controls on a [`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)). These controls synthesize input from other, actual physical controls and present it in a different way (in this example, they allow you to treat the individual directions of a stick as buttons).

The system considers synthetic controls for [interactive rebinding](rebind-action-runtime.md) but always favors non-synthetic controls. If both a synthetic and a non-synthetic control that are a potential match exist (for example, `<Gamepad>/leftStick/x` and `<Gamepad>/leftStick/left`), the non-synthetic control (`<Gamepad>/leftStick/x`) wins by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but also makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.

> [!NOTE]
> To query whether a control is synthetic, use the [`InputControl.synthetic`](xref:UnityEngine.InputSystem.InputControl.synthetic) property.
