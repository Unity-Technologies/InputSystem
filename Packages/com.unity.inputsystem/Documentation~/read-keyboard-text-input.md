# Read text input

To listen to text input, hook into [Keyboard.onTextInput](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_onTextInput). This delivers character-by-character input as reported by the platform, including input from on-screen keyboards. As a best practice, don't manually translate text input from key presses by trying to string together the characters corresponding to the keys.

The text input API doesn't allocate GC memory because it doesn't deliver fully composed strings.

## Working with input from input method editors

Some writing systems, such as some East-Asian scripts, are too complex to represent all characters as individual keys on a keyboard. For these layouts, operating systems implement input method editors (IME) to allow composing input strings by other methods, for instance by typing several keys to produce a single character. 

Unity's UI frameworks for text input support IME without any additional configuration. To build your own UI for text input, the [Keyboard](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Keyboard.html) class allows you to work with input from IME with APIs that have `IME` in their names. For more information, refer to the [Keyboard API documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Keyboard.html).