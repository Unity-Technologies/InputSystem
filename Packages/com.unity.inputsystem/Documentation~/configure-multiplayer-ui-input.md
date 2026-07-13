---
uid: input-system-multiplayer-ui-config
---

# Configure multiplayer UI input

To enable multiplayer UI input:

1. Replace the project’s [Event System](https://docs.unity3d.com/Manual/script-EventSystem.html) component with the Input System's [Multiplayer Event System](xref:UnityEngine.InputSystem.UI.MultiplayerEventSystem) component.


For information on how to automatically configure the player's UI Input Module to use actions from the [Player Input](player-input-component.md) component, refer to documentation on [Player Input: UI Input](use-player-input-component-ui.md) to learn how.

To define mouse UI input behaviour for a Multiplayer Event System:

1. Create an empty GameObject.
1. In the Multiplayer Event System, set **Player Root** to the new GameObject.
1. For any UI selectables that you want the Multiplayer Event System to interact with, move their GameObjects in the hierarchy so that they are child GameObjects of the **Player Root** GameObject.
