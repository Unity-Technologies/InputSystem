    ////WIP

# Actions

>NOTE: Actions are a game-time only feature. They cannot be used in `EditorWindow` code.

Input actions are designed to separate the logical meaning of an input from the physical means (i.e. activity on an input device) by which the input is generated. Instead of writing input code like so:

    var look = new Vector2();

    var gamepad = Gamepad.current;
    if (gamepad != null)
        look = gamepad.rightStick.ReadValue();

    var mouse = Mouse.current;
    if (mouse != null)
        look = mouse.delta.ReadValue();

One can instead write code that is agnostic to where the input is coming from:

    myControls.gameplay.look.performed +=
        ctx => look = ctx.ReadValue<Vector2>();

The mapping can then be established graphically in the editor:

![Look Action Binding](Images/LookActionBinding.png)

This also makes it easier to let players to customize bindings at runtime.

## Workflows

There are three different workflows for setting up actions for your game.

### Component Workflow

The component workflow is good for prototyping as it does not require setting up an asset yet still allows to set up bindings graphically. However, it does require a certain amount of scripting.

To add actions directly to your component, simply declare fields that have type `InputAction` (make sure the fields are serialized).

```
public MyBehaviour : MonoBehaviour
{
    public InputAction fireAction;
    public InputAction lookAction;
    public InputAction moveAction;
}
```

### Asset Workflow

#### Using `UnityEvents`

#### Using Interfaces

### Scripting Workflow

Lastly, it is possible to create and set up input actions entirely in script.

```CSharp
var lookAction = new InputAction("look", binding: "<Gamepad>/leftStick");
var moveAction = new InputAction("move", binding: "<Gamepad>/rightStick");

lookAction.AddBinding("<Mouse>/delta");
moveAction.AddCompositeBinding("Dpad")
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");
```

## Action Maps

## Bindings

### Rebinding

## Control Schemes

## Interactions

|Interaction|Started|Performed|Cancelled|
|-----------|-------|---------|---------|
|Press|
|Tap|
|SlowTap|

### `Press`

### `Tap`

### `SlowTap`

### Writing Custom Interactions

If the built-in set of interactions does not fit your needs, you can easily write your own custom interactions.

## Processors

## Multiplayer

It is possible to use the same action definitions for multiple local players. This setup is useful in a local co-op games, for example.
