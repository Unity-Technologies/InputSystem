
# Create Actions

The simplest way to create actions is to use the [Input Actions editor](ActionsEditor.md) in the Project Settings window. This is the primary recommended workflow and suitable for most scenarios.

However, because the input system API is very open, there are many other ways to create actions which may suit less common scenarios. For example, by loading actions from JSON data, or creating actions entirely in code.

### Create Actions using the Action editor

For information on how to create and edit Input Actions in the editor, see the [Input Actions editor](ActionsEditor.md). This is the recommended workflow if you want to organise all your input actions and bindings in one place, which applies across the whole of your project. This often the case for most types of game or app.

![Action Editor Window](Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input Actions Editor in the Project Settings window*


### Configure Actions

* To add a new Action, select the Add (+) icon in the header of the Action column.
* To rename an existing Action, either long-click the name, or right-click the Action Map and select __Rename__ from the context menu.
* To delete an existing Action, either right-click it and select __Delete__ from the context menu.
* To duplicate an existing Action, either right-click it and select __Duplicate__ from the context menu.





## Other ways to create Actions

The simplest way to create actions is to use the [Input Actions editor](ActionsEditor.md) to configure a set of actions in an asset, as described above. However, because the Input System package API is open and flexible, you can create actions using alternative techniques. These alternatives might be more suitable if you want to customize your project beyond the standard workflow.

### Creating Actions by declaring them in MonoBehaviours

As an alternative workflow, you can declare individual [Input Action](../api/UnityEngine.InputSystem.InputAction.html) and [Input Action Maps](../api/UnityEngine.InputSystem.InputActionMap.html) as fields directly inside `MonoBehaviour` components.

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

### Loading Actions from JSON

You can load Actions as JSON in the form of a set of Action Maps or as a full [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html). This also works at runtime in the Player.

```CSharp
// Load a set of action maps from JSON.
var maps = InputActionMap.FromJson(json);

// Load an entire InputActionAsset from JSON.
var asset = InputActionAsset.FromJson(json);
```

### Creating Actions in code

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


## Enabling actions

Actions have an **enabled** state, meaning you can enable or disable them to suit different situations.

If you have an Action Asset assigned as [project-wide](./ProjectWideActions.md), the actions it contains are enabled by default and ready to use.

For actions defined elsewhere, such as in an Action Asset not assigned as project-wide, or defined your own code, they begin in a disabled state, and you must enable them before they will respond to input.

You can enable actions individually, or as a group by enabling the Action Map which contains them.

```CSharp
// Enable a single action.
lookAction.Enable();

// Enable an en entire action map.
gameplayActions.Enable();
```

When you enable an Action, the Input System resolves its bindings, unless it has done so already, or if the set of devices that the Action can use has not changed. For more details about this process, see the documentation on [binding resolution](ActionBindings.md#binding-resolution).

You can't change certain aspects of the configuration, such Action Bindings, while an Action is enabled. To stop Actions or Action Maps from responding to input, call  [`Disable`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_Disable).

While enabled, an Action actively monitors the [Control(s)](Controls.md) it's bound to. If a bound Control changes state, the Action processes the change. If the Control's change represents an [Interaction](Interactions.md) change, the Action creates a response. All of this happens during the Input System update logic. Depending on the [update mode](Settings.md#update-mode) selected in the input settings, this happens once every frame, once every fixed update, or manually if updates are set to manual.
