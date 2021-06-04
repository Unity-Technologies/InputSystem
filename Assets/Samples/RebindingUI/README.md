This sample demonstrates how to use the Input System APIs to set up a rebinding UI. The main file is [RebindActionUI](./RebindActionUI.cs) which, aside from serving as an example, contains a reusable `MonoBehaviour` component for composing rebinding UIs. The [RebindUIPrefab](./RebindUIPrefab.prefab) contains a ready-made prefab that can be used as a simple drop-in setup for rebinding an individual action.

 To demonstrate how to use images instead of textual display strings, take a look at [GamepadIconsExample](./GamepadIconsExample.cs).

 Finally, the [RebindSaveLoad](./RebindSaveLoad.cs) script demonstrates how to persist user rebinds in `PlayerPrefs` and how to restore them from there.

 The icons used in the sample are taken from [Free Prompts Pack](https://opengameart.org/content/free-keyboard-and-controllers-prompts-pack) made by Nicolae Berbece.
