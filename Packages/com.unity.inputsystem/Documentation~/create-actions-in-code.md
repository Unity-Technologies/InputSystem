---
uid: input-system-create-actions-in-code
---

# Create actions in code

You can manually create and configure actions entirely in code, including assigning the bindings. This also works at runtime in the Player. For example:

```CSharp
// Create free-standing actions.
var lookAction = new InputAction("look", binding: "<Gamepad>/leftStick");
var moveAction = new InputAction("move", binding: "<Gamepad>/rightStick");

    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");

// Create an action map with actions.
var map = new InputActionMap("Gameplay");
var lookAction = map.AddAction("look");
lookAction.AddBinding("<Gamepad>/leftStick");

// Create an action asset.
var asset = ScriptableObject.CreateInstance<InputActionAsset>();
var gameplayMap = new InputActionMap("gameplay");
asset.AddActionMap(gameplayMap);
var lookAction = gameplayMap.AddAction("look", "<Gamepad>/leftStick");
```

Any action that you create in this way during Play mode doesn't persist in the input action asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset.
