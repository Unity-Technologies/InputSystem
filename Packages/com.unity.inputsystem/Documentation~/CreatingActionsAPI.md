
# Creating Actions in code

While creating Actions using the [Actions Editor window](./ActionsEditor.md) is sufficient for most common scenarios, the API of the input system package is fully open, and allows you the ability to write code that does anything the input system itself does, including creating actions, configuring axes, setting up bindings, defining new types of custom interactions and processors dynamically at runtime.


## Defining actions in MonoBehaviours

As an alternative to creating actions using the [Actions Editor window](./ActionsEditor.md), you can declare individual [Input Action](../api/UnityEngine.InputSystem.InputAction.html) and [Input Action Maps](../api/UnityEngine.InputSystem.InputActionMap.html) as fields directly inside `MonoBehaviour` components.

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleScript : MonoBehaviour
{
    public InputAction move;
    public InputAction jump;
}
```

The result is similar to using an Actions defined in the Input Actions editor, except the Actions are defined in the GameObject's properties and saved as Scene or Prefab data, instead of in a dedicated Asset.

When you embed actions like this, by defining serialized InputAction fields in a MonoBehaviour, the GameObject's Inspector window displays an interface similar to the Actions column of the [Actions Editor](./ActionsEditor.md), which allows you to set up the bindings for those actions. For example:

![MyBehavior Inspector](Images/Workflow-EmbeddedActionsInspector.png)

* To add or remove Actions or Bindings, click the Add (+) or Remove (-) icon in the header.
* To edit Bindings, double-click them.<br>
* To edit Actions, double-click them in an Action Map, or click the gear icon on individual Action properties.<br>
* You can also right-click entries to bring up a context menu, and you can drag them. Hold the Alt key and drag an entry to duplicate it.

Unlike the project-wide actions in the Project Settings window, you must manually enable and disable Actions and Action Maps that are embedded in MonoBehaviour components.

When you use this workflow, the serialised action configurations are stored with the parent GameObject as part of the scene, opposite to being serialised with an Action Asset. This can be useful if you want to bundle the control bindings and behaviour together in a single monobehaviour or prefab, so it can be distributed together. However, this can also make it harder to organize your full set of control bindings if they are distributed across multiple prefabs or scenes.

## Loading Actions from JSON

You can load Actions as JSON in the form of a set of Action Maps or as a full [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html). This also works at runtime in the Player.

```CSharp
// Load a set of action maps from JSON.
var maps = InputActionMap.FromJson(json);

// Load an entire InputActionAsset from JSON.
var asset = InputActionAsset.FromJson(json);
```

## Creating Actions in code

You can manually create and configure Actions entirely in code, including assigning the bindings. This also works at runtime in the Player. For example:

```CSharp
// Create free-standing Actions.
var lookAction = new InputAction("look", binding: "<Gamepad>/leftStick");
var moveAction = new InputAction("move", binding: "<Gamepad>/rightStick");

lookAction.AddBinding("<Mouse>/delta");
moveAction.AddCompositeBinding("Dpad")
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");

// Create an Action Map with Actions.
var map = new InputActionMap("Gameplay");
var lookAction = map.AddAction("look");
lookAction.AddBinding("<Gamepad>/leftStick");

// Create an Action Asset.
var asset = ScriptableObject.CreateInstance<InputActionAsset>();
var gameplayMap = new InputActionMap("gameplay");
asset.AddActionMap(gameplayMap);
var lookAction = gameplayMap.AddAction("look", "<Gamepad>/leftStick");
```

Any action that you create in this way during Play mode do not persist in the Input Action Asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset.

