---
uid: input-system-read-text-input
---

# Read text input

To receive text input from a keyboard, set up a callback on the [`Keyboard.onTextInput`](xref:UnityEngine.InputSystem.Keyboard.onTextInput) event. This delivers character-by-character input as reported by the platform, including input from on-screen keyboards. As a best practice, don't manually translate text input from key presses by trying to string together the characters corresponding to the keys.

The text input API doesn't allocate managed memory because it doesn't deliver fully composed strings.

## Working with input from input method editors

Some writing systems, such as some East-Asian scripts, are too complex to represent all characters as individual keys on a keyboard. For these layouts, operating systems implement input method editors (IME) to allow composing input strings by other methods, for instance by typing several keys to produce a single character.

Unity's UI frameworks for text input support IME without any additional configuration. To build your own UI for text input, the [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard) class allows you to work with input from IME with APIs that have `IME` in their names. For more information, refer to the [`Keyboard` API documentation](xref:UnityEngine.InputSystem.Keyboard).
