---
uid: input-system-keyboard-intro
---

# Keyboard devices introduction

The [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard) class defines a device with a set of key controls defined by the [`Key`](xref:UnityEngine.InputSystem.Key) enumeration.

The location of individual keys is agnostic to keyboard layout. This means that, for example, the **A** key is always the key to the right of the **Caps Lock** key, regardless of where the active keyboard layout places the key that generates the A character, or whether the layout has a key assigned to that character.

For a list of platforms that support keyboard devices, refer to [Supported devices reference](supported-devices-reference.md).

The [scripting API reference for the `Keyboard` class](xref:UnityEngine.InputSystem.Keyboard) lists all the properties for the individual key controls. Two controls, [`anyKey`](xref:UnityEngine.InputSystem.Keyboard.anyKey) and [`imeSelected`](xref:UnityEngine.InputSystem.Keyboard.imeSelected), don't directly map to individual keys. `anyKey` is a [synthetic](synthetic-controls.md) button control which reports whether any key on the keyboard is pressed, and `imeSelected`reports whether or not [IME](read-keyboard-text-input.md#working-with-input-from-input-method-editors) text processing is enabled.
