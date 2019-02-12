    ////WIP

# Actions

>NOTE: Actions are a game-time only feature. They cannot be used in `EditorWindow` code.

Input actions are designed to separate the logical meaning of an input from the physical means (i.e. activity on an input device) by which the input is generated. Instead of writing input code like so:

```
    var look = new Vector2();

    var gamepad = Gamepad.current;
    if (gamepad != null)
        look = gamepad.rightStick.ReadValue();

    var mouse = Mouse.current;
    if (mouse != null)
        look = mouse.delta.ReadValue();
```

One can instead write code that is agnostic to where the input is coming from:

```
    myControls.gameplay.look.performed +=
        ctx => look = ctx.ReadValue<Vector2>();
```

The mapping can then be established graphically in the editor:

![Look Action Binding](Images/LookActionBinding.png)

This also makes it easier to let players to customize bindings at runtime.

## Terms and Concepts

The following terms and concepts are used through the input action system:

|Concept|Description|
|-------|-----------|
|Action|A "logical" input such as "Jump" or "Fire". I.e. an input action that can triggered by a player through one or more input devices.|
|Binding||
|Interation||
|Processor||
|Phase||
|Control Scheme||
|Action Map||
|Action Asset||

## Setting Up Actions

There are three different workflows for setting up actions for your game.

### Asset Workflow

#### Using `UnityEvents`

#### Using Interfaces

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

### Composite Bindings

Sometimes it is desirable to have several controls act in unison to mimick a different type of control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector control equivalent to mouse deltas or gamepad sticks. Another example is using two keys to form a 1D axis equivalent to a mouse scroll axis.

This effect can be achieved through "composite bindings", i.e. bindings that made up of other bindings. Composites themselves do not bind directly to controls but rather source values from other bindings that do and synthesize input on the fly from those values.

>NOTE: Actions set on bindings that are part of composites are ignored. The composite as a whole can trigger an action. Individual parts of the composite cannot.

### Conflict Resolution

## Control Schemes

## Continuous Actions

By default, actions will trigger only in response to input events. This means that, for example, an action bound to the left stick of a gamepad will only trigger when the left stick is moved. This behavior can be undesirable when an input is meant to register for as long as a control is actuated -- regardless of whether it actually changes value or not. In other words, it can be desirable to trigger the action associated with the left stick for as long as the left stick is moved out of its deadzone but regardless of whether the stick has actually been moved in a given frame.

![Continuous Action](Images/ContinuousAction.png)

When continuous mode is enabled, an action that goes into `Performed` phase will stay in the phase until it is `Cancelled`. Also, while in the `Performed` phase, an action in continuous mode will be `Performed` in a frame even if there is no input. The value returned by `ReadValue` will be the value of the control that was last used with the action.

## Pass-Through Actions

    ////TODO

## Phases

An action has a set of dictinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|Disabled|The action is disabled and will not receive input.|
|Waiting|The action is enabled and is actively waiting for input.|
|Started|Input has been received that started an interaction with the action.|
|Performed|An interaction with the action has been completed.|
|Cancelled|An interaction with the action has been cancelled.|

The current phase of an action can be read using `InputAction.phase`.

The `Started`, `Performed`, and `Cancelled` phases each have a callback associated with them:

```
    var action = new InputAction();

    action.started += ctx => /* Action was started */;
    action.performed += ctx => /* Action was performed */;
    action.cancelled += ctx => /* Action was started */;
```

Each callback receives a structure holding context information that can be used to query the current state of the action and to read out values from controls that triggered the action (`InputAction.CallbackContext.ReadValue`). Note that the contents of the structure are only valid for the duration of the callback. In particular, it is not safe to store the received context and later access its properties from outside the callback.

Note that an action will go through the phases even if no interaction has been set on a binding. In this case, the default behavior applies:

1. As soon as a bound control becomes actuated (has a magnitude greater than 0 or, if the bound control does not have an associated magnitude, if it moves out of its default state), the action goes from `Waiting` to `Started` and then immediately to `Performed` and back to `Started`.
2. For as long as the bound control remains actuated, the action stays in `Started` and will trigger `Performed` whenever the value of the control changes.
3. When the bound control stops being actuated, the action goes to `Cancelled` and then back to `Waiting`.

## Interactions

    ////TODO: move to its own page

An interaction drives an action based on specific input patterns. Interactions are placed on bindings and they source values from all the controls matched by the binding.

The following table shows all the interactions that are registerd by default. Additional interactions can be added to the system using `InputSystem.RegisterInteraction<T>()`. See ["Writing Custom Interactions"](#writing-custom-interactions) for details.

Some of the interactions behave differently when the action they are associated with through the binding is set to "continuous" mode (see `InputAction.continuous`). This is indicated in the table by a separate "... (continuous)" entry.

|Interaction|Started|Performed|Cancelled|
|-----------|-------|---------|---------|
|*Press* (PressOnly)| |Control crosses button press threshold; then will not perform again until button is first released again| |
|*Press* (ReleaseOnly)| |Control goes back below button press threshold after crossing it| |
|*Press* (PressAndRelease)| |Control crosses button press threshold|Control goes back below button threshold|
|*Press* (PressOnly; continuous)| |Control crosses button press threshold; then any time the control changes value or at least every frame| |
|*Press* (ReleaseOnly; continuous)| |No difference to non-continuous mode| |
|*Press* (PressAndRelease; continuous)| |Control cross button press threshold; then any time the control changes value or at least every frame|Control goes back below button threshold|
|*Hold*|Control Actuated|Held for >= `duration`|
|*Hold* (continuous)|Control Actuated|Held for >= `duration`; after that, every frame regardless of whether the bound control receives input in the frame or not.|
|*Tap*|Control Actuated|Control Released within `duration` (defaults to `InputSettings.defaultTapTime`) seconds|Control Released before `duration` seconds|
|*SlowTap*|Control Actuated|Control Released within `duration` (defaults to `InputSettings.defaultSlowTapTime`) seconds|Control Released before `duration` seconds|
|*MultiTap*|||
|*Passthrough*| |On every value change of control| |
|*Passthrough* (continuous)| |On every value change of control and additionally, if there is no value value change in a frame, performs with value from last frame| |

### `Press`

A `Press` interaction can be used to explicitly force button-like interaction.

Note that the `Press` interaction operates on control actuation, not on control values directly. This means that the press threshold will be evaluated against the magnitude of the control actuation. This means that it is possible to use

### `Hold`

A `Hold` requires a control to be held for a set duration before the action is triggered. The duration can either be set explicitly on the action or be left at default (`0`) in which case the default hold time setting applies (`InputSettings.defaultHoldTime`).

```
    // Create an action with a .3 second hold on the A button of the gamepad.
    var action = new InputAction();
    action.AddBinding("<Gamepad>/buttonSouth").WithInteraction("Hold(duration=0.3");
```

### `Tap`

### `SlowTap`

### `DoubleTap`

### `Passthrough`

This interaction is designed to revert the start/performed/cancelled interaction model to a simple, direct passthrough model where every value change on any bound control of an action performs the action. This can be useful, for example, to use an action to simply listen for activity of any kind.

```
// Set up an action that fires any time any button on the gamepad changes value.
var action = new InputAction(
    binding: "<Gamepad>/**/<Button>",
    interactions: "passthrough");
action.Enable();

action.performed +=
    ctx => Debug.Log($"Button {ctx.control} = {ctx.ReadValue<float>()}");

// Perform the action twice by actuating and then resetting the left trigger
// on a newly created gamepad.
var gamepad = InputSystem.AddDevice<Gamepad>();
InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
InputSystem.QueueStateEvent(gamepad, new GamepadState());
InputSystem.Update();
```

The `passthrough` interaction has no parameters.

The `passthrough` interaction can be used on a `continuous` action. In this case, the action will perform not only on every value change but will also perform if none of the bound controls receive a new value in a given frame. The value received will be the current value of the control.

### Multiple Interactions On Same Binding

### Writing Custom Interactions

The set of interactions is freely extensible. Newly added interactions are usable in the UI and data the same way that built-in interations are.

To implement

>NOTE: Interactions cannot currently orchestrate input between several actions and/or bindings. They are at this point restricted to operating on a single binding and the data that flows in through it.

Unlike processors, interations can be stateful, meaning that it is permissible to keep local state that mutates over time as input is received. The system may ask interactions to reset such state at certain points by invoking the `Reset()` method.

## Processors

### Writing Custom Processors

## Devices

By default, bindings will search through the global list of available devices given by `InputSystem.devices`. This means that when an action is enabled, they will bind to whatever controls are available in the system that match the bindings of the action.

This behavior can be overridden by restricting `InputActionAssets` or individual `InputActionMaps` to a specific set of devices. If this is done, binding resolution will take only the controls of the given devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```

## Enabling and Disabling Actions

## Responding to Actions

There are several different ways

### Global Callback

### Action Map Callback

### Per-Action Callbacks

### Tracing Actions

As when using `InputEventTrace` for events, actions can be traced in order to generate a log of all activity that happened on a particular set of actions. To do so, use `InputActionTrace`.

>NOTE: `InputActionTrace` allocates unmanaged memory and needs to be disposed of in order to not create memory leaks.

```CSharp
var trace = new InputActionTrace();

// Subscribe trace to single action.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myAction);

// Subscribe trace to entire action map.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myActionMap);

// Subscribe trace to all actions in the system.
trace.SubscribeToAll();

// Record a single triggering of an action.
myAction.performed +=
    ctx =>
    {
        if (ctx.ReadValue<float>() > 0.5f)
            trace.RecordAction(ctx);
    };

// Output trace to console.
Debug.Log(string.Join(",\n", trace));

// Walk through all recorded actions and then clear trace.
foreach (var record in trace)
{
    Debug.Log($"{record.action} was {record.phase} by control {record.control}");

    // To read out the value, you either have to know the value type or read the
    // value out as a generic byte buffer. Here we assume that the value type is
    // float.

    Debug.Log("Value: " + record.ReadValue<float>());

    // If it's okay to accept a GC hit, you can also read out values as objects.
    // In this case, you don't have to know the value type.

    Debug.Log("Value: " + record.ReadValueAsObject());
}
trace.Clear();

// Unsubscribe trace from everything.
trace.UnsubscribeFromAll();

// Release memory held by trace.
trace.Dispose();
```

Once recorded, a trace can be safely read from multiple threads as long as it is not concurrently being written to and as long as the action setup (i.e. the configuration data accessed by the trace) is not concurrently being changed on the main thread.

## Rebinding Actions

## Extending Actions

### Writing Custom Processors

### Writing Custom Interactions

### Writing Custom Composites

## Debugging Actions

### Action Processing

    TODO: go into detail about where and when actions are processed; also stuff like getting cancelled outside of input updates

## Using Actions with Multiple Players

It is possible to use the same action definitions for multiple local players. This setup is useful in a local co-op games, for example.
