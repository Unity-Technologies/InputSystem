This sample demonstrates how to use the Input System APIs to set up a rebinding UI. The main file is
[RebindActionUI](./RebindActionUI.cs) which, aside from serving as an example, contains a reusable `MonoBehaviour`
component for composing rebinding UIs. The [RebindUIPrefab](./RebindUIPrefab.prefab) contains a ready-made prefab that
can be used as a simple drop-in setup for rebinding an individual action.

To demonstrate how to use images instead of textual display strings, take a look at
[GamepadIconsExample](./GamepadIconsExample.cs).

To demonstrate how to show dynamic UI texts based on input action bindings, see [ActionLabel](./ActionLabel.cs).

[InputActionIndicator](./InputActionIndicator.cs) and [InputActionIndicator.prefab](./InputActionIndicator.prefab)
shows how to make a simple UI indicator that shows whether an associated input action is enabled, disabled or
performed. This behavior has been added to this sample to add observability of actions triggered within gameplay,
menu and rebind contexts.

The [RebindSaveLoad](./RebindSaveLoad.cs) script demonstrates how to persist user rebinds in `PlayerPrefs` and how
to restore them.

In this sample, keyboard bindings for "Move" (default WASD) is rebound as a single composite. This means that
indivudual parts will get assigned one after the other. Another way of doing this is to set it up as four individual
button bindings and assign them individually as four partial bindings.

In this sample it is possible to directly rebind gamepad sticks in the gamepad control scheme. In practice, you
probably don't want to set up rebinding the sticks like this but rather have a "swap sticks" kind of toggle instead.
In this sample we have both variants for demonstration purposes. See [RebindActionUI.SwapBinding](./RebindActionUI.cs)
for a method that swaps two bindings of similar type.

The icons used in the sample are taken from
[Free Prompts Pack v4.0](https://opengameart.org/content/free-keyboard-and-controllers-prompts-pack) created by,
and made available to public domain by Nicolae Berbece.
Icons are licensed under [Creative Commons CC0](https://creativecommons.org/publicdomain/zero/1.0/).
